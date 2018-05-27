using System.Collections.Generic;
using System.IO;
using Jazornowsky.QuantumStorage.model;
using Jazornowsky.QuantumStorage.service;
using Jazornowsky.QuantumStorage.utils;
using UnityEngine;
using Random = System.Random;

//More advanced manipulation of lists/collections
//using FortressCraft.Community.Utilities;    //The community tools Pack! Highly recommend for useful functions: https://github.com/steveman0/FCECommunityTools/tree/UIUtil

namespace Jazornowsky.QuantumStorage
{
    public class QuantumStorageControllerMachine : MachineEntity, PowerConsumerInterface
    {
        public static readonly string MachineName = "Quantum Storage Controller";
        private static readonly float MaxPower = 1000.0F;
        private static readonly float MinOperatingPower = 200.0F;
        private static readonly float MaximumDeliveryRate = 500.0F;
        private static readonly float NetworkScanPeriod = 5; // sec
        private static readonly float PowerConsumptionPeriod = 1; // sec
        private static readonly float LowPowerMissionPeriod = 30; // sec
        private static readonly float StorageFullMissionPeriod = 30; // sec

        private readonly MachineSides _machineSides = new MachineSides();
        private readonly StorageControllerService _storageControllerService;
        private readonly StorageControllerPowerService _storageControllerPowerService;

        private readonly MachinePower
            _machinePower = new MachinePower(MaxPower, MaximumDeliveryRate, MinOperatingPower);

        private readonly MachineStorage _machineStorage = new MachineStorage();

        private bool _notifications = true;
        private float _secondsPassedFromLastNetworkScan = 0;
        private float _secondsPassedFromPowerConsumption = 0;
        private float _secondsPassedFromLowPowerMission = 0;
        private float _secondsPassedFromStorageFullMission = 0;

        public QuantumStorageControllerMachine(MachineEntityCreationParameters parameters) : base(parameters)
        {
            mbNeedsLowFrequencyUpdate = true;
            mbNeedsUnityUpdate = true;
            PositionUtils.SetupSidesPositions(parameters.Flags, _machineSides);
            _storageControllerService = new StorageControllerService(this, _machineStorage, _machineSides);
            _storageControllerPowerService = new StorageControllerPowerService(this, _machinePower, _machineSides);
        }

        public override void OnUpdateRotation(byte newFlags)
        {
            base.OnUpdateRotation(newFlags);
            PositionUtils.SetupSidesPositions(newFlags, _machineSides);
        }

        public int GetRemainigCapacity()
        {
            return _machineStorage.GetRemainingCapacity();
        }

        public int GetMaxCapacity()
        {
            return _machineStorage.MaxCapacity;
        }

        public List<ItemBase> GetItems()
        {
            return _machineStorage.Items;
        }

        public void AddItem(ref ItemBase item)
        {
            _storageControllerService.AddItem(ref item);
        }

        public ItemBase GetItem(int index)
        {
            return _machineStorage.Items[index];
        }

        public void TakeItem(ref ItemBase item)
        {
            _storageControllerService.TakeItem(ref item);
        }

        public override void LowFrequencyUpdate()
        {
            if (!_machinePower.HasPower())
            {
                if (_notifications)
                {
                    _secondsPassedFromLowPowerMission += LowFrequencyThread.mrPreviousUpdateTimeStep;
                    if (_secondsPassedFromLowPowerMission >= LowPowerMissionPeriod)
                    {
                        MissionManager.instance.RemoveMission("Quantum Storage is low on power.");
                        MissionManager.instance.AddMission("Quantum Storage is low on power.", 5f,
                            Mission.ePriority.eOptional);
                        _secondsPassedFromLowPowerMission = 0;
                    }
                }

                return;
            }

            if (_notifications && _machineStorage.IsFull())
            {
                _secondsPassedFromStorageFullMission += LowFrequencyThread.mrPreviousUpdateTimeStep;
                if (_secondsPassedFromStorageFullMission >= StorageFullMissionPeriod)
                {
                    MissionManager.instance.RemoveMission("Quantum Storage is full.");
                    MissionManager.instance.AddMission("Quantum Storage is full.", 5f, Mission.ePriority.eOptional);
                    _secondsPassedFromStorageFullMission = 0;
                }
            }

            _secondsPassedFromLastNetworkScan += LowFrequencyThread.mrPreviousUpdateTimeStep;
            _secondsPassedFromPowerConsumption += LowFrequencyThread.mrPreviousUpdateTimeStep;

            if ((_secondsPassedFromPowerConsumption >= PowerConsumptionPeriod))
            {
                _machinePower.ConsumePower();
                _machinePower.PreviousUpdatePower = _machinePower.CurrentPower;
                _secondsPassedFromPowerConsumption = 0;
            }

            if (_secondsPassedFromLastNetworkScan < NetworkScanPeriod && !Dirty)
            {
                return;
            }
            else
            {
                _secondsPassedFromLastNetworkScan = 0;
            }

            _storageControllerService.UpdateStorage();
        }

        public override string GetPopupText()
        {
            string txt = DisplayUtils.MachineDisplay(MachineName);
            txt += DisplayUtils.PowerDisplay(_machinePower);
            if (_machinePower.HasPower())
            {
                txt += DisplayUtils.StorageDisplay(_machineStorage);
            }

            txt += "Q to ";
            txt += _notifications ? "disable" : "enable";
            txt += " notifications.\n";

            if (Input.GetButtonDown("Interact") && (UIManager.AllowInteracting))
            {
                UIManager.ForceNGUIUpdate = 0.1f;
                AudioHUDManager.instance.HUDIn();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                ToggleNotifications();
            }

            return txt;
        }

        private void ToggleNotifications()
        {
            if (_notifications)
            {
                _notifications = false;
            }
            else
            {
                _notifications = true;
            }
        }

        public override void ReadNetworkUpdate(BinaryReader reader)
        {
            Dirty = true;
        }

        public override int GetVersion()
        {
            return QuantumStorageMod.Version;
        }

        public override bool ShouldSave()
        {
            return true;
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(_machinePower.CurrentPower);
        }

        public override void Read(BinaryReader reader, int entityVersion)
        {
            _machinePower.CurrentPower = reader.ReadSingle();
        }

        public override void UnityUpdate()
        {
            base.UnityUpdate();
        }

        public float GetMaxPower()
        {
            return _machinePower.MaxPower;
        }

        public bool HasPower()
        {
            return _machinePower.HasPower();
        }

        public float GetRemainingPowerCapacity()
        {
            return _storageControllerPowerService.GetRemainingPowerCapacity();
        }

        public float GetMaximumDeliveryRate()
        {
            return _machinePower.MaximumDeliveryRate;
        }

        public bool DeliverPower(float amount)
        {
            return _storageControllerPowerService.DeliverPower(amount);
        }

        public bool WantsPowerFromEntity(SegmentEntity entity)
        {
            return true;
        }

        public bool IsFull()
        {
            return _machineStorage.IsFull();
        }

        public bool Dirty { get; set; }
    }
}
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
        private const float MaxPower = 1000.0F;
        public static readonly float MinOperatingPower = 200.0F;
        public static readonly float MaximumDeliveryRate = 500.0F;
        public static readonly float NetworkScanPeriod = 5; // sec
        private const float PowerConsumptionPeriod = 1; // sec
        private const float LowPowerMissionPeriod = 30; // sec
        private const float StorageFullMissionPeriod = 30; // sec

        private readonly MachineSides _machineSides = new MachineSides();
        private readonly StorageControllerService _storageControllerService;
        private readonly StorageControllerPowerService _storageControllerPowerService;
        private readonly MachineStorage _machineStorage = new MachineStorage();

        private readonly MachinePower
            _machinePower = new MachinePower(MaxPower, MaximumDeliveryRate, MinOperatingPower);

        private bool _notifications = true;
        public bool _anotherControllerDetected;
        private float _secondsPassedFromLastNetworkScan = 0;
        private float _secondsPassedFromPowerConsumption = 0;
        private float _secondsPassedFromLowPowerMission = 0;
        private float _secondsPassedFromStorageFullMission = 0;
        private bool _inputEnabled = true;
        private bool _outputEnabled = true;

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
                ProcessNotification(true, "Quantum Storage Controller", ref _secondsPassedFromLowPowerMission,
                    LowPowerMissionPeriod, MissionUtils.AddLowPowerMission);
                return;
            }

            if (_anotherControllerDetected)
            {
                ProcessNetworkScan();
                return;
            }

            ProcessNotification(_machineStorage.IsFull(), "Quantum Storage", ref _secondsPassedFromStorageFullMission,
                StorageFullMissionPeriod, MissionUtils.AddLowStorageMission);

            ProcessPowerConsumption();
            ProcessNetworkScan();
        }

        private void ProcessPowerConsumption()
        {
            _secondsPassedFromPowerConsumption += LowFrequencyThread.mrPreviousUpdateTimeStep;
            if ((_secondsPassedFromPowerConsumption >= PowerConsumptionPeriod))
            {
                _machinePower.ConsumePower();
                _machinePower.PreviousUpdatePower = _machinePower.CurrentPower;
                _secondsPassedFromPowerConsumption = 0;
            }
        }

        private void ProcessNetworkScan()
        {
            _secondsPassedFromLastNetworkScan += LowFrequencyThread.mrPreviousUpdateTimeStep;
            if (_secondsPassedFromLastNetworkScan >= NetworkScanPeriod || Dirty)
            {
                _storageControllerService.UpdateStorage();
                _secondsPassedFromLastNetworkScan = 0;
            }
        }

        private void ProcessNotification(bool trigger, string notificationName, ref float cooldown,
            float cooldownTrigger,
            MissionUtils.AddMachineMission addMachineMission)
        {
            if (!_notifications || !trigger)
            {
                return;
            }

            cooldown += LowFrequencyThread.mrPreviousUpdateTimeStep;
            if (cooldown >= cooldownTrigger)
            {
                addMachineMission(notificationName);
                cooldown = 0;
            }
        }

        public override string GetPopupText()
        {
            string txt = DisplayUtils.MachineDisplay(MachineName);
            txt += DisplayUtils.PowerDisplay(_machinePower);
            if (IsOperating())
            {
                txt += DisplayUtils.StorageDisplay(_machineStorage);
            }

            if (_anotherControllerDetected)
            {
                txt = "ANOTHER CONTROLLER DETECTED - ONLY ONE CONTROLLER CAN BE PRESENT\n";
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
            writer.Write(_inputEnabled);
            writer.Write(_outputEnabled);
        }

        public override void Read(BinaryReader reader, int entityVersion)
        {
            _machinePower.CurrentPower = reader.ReadSingle();
            _inputEnabled = reader.ReadBoolean();
            _outputEnabled = reader.ReadBoolean();
        }

        public override void UnityUpdate()
        {
            base.UnityUpdate();
        }

        public float GetMaxPower()
        {
            return _machinePower.MaxPower;
        }

        public bool IsOperating()
        {
            return HasPower() && !_anotherControllerDetected;
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

        public MachineStorage GetMachineStorage()
        {
            return _machineStorage;
        }

        public void ToggleInput()
        {
            _inputEnabled = !_inputEnabled;
        }

        public bool IsInputEnabled()
        {
            return _inputEnabled;
        }

        public void ToggleOutput()
        {
            _outputEnabled = !_outputEnabled;
        }

        public bool IsOutputEnabled()
        {
            return _outputEnabled;
        }

        public bool Dirty { get; set; }
    }
}
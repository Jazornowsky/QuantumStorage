using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jazornowsky.QuantumStorage.model;
using Jazornowsky.QuantumStorage.utils;

namespace Jazornowsky.QuantumStorage.service
{
    class StorageControllerPowerService
    {
        private const string LogName = "StorageControllerPowerService";

        private readonly QuantumStorageControllerMachine _quantumStorageController;
        private readonly MachinePower _machinePower;
        private readonly MachineSides _machineSides;

        public StorageControllerPowerService(QuantumStorageControllerMachine quantumStorageController,
            MachinePower machinePower, MachineSides machineSides)
        {
            _quantumStorageController = quantumStorageController;
            _machinePower = machinePower;
            _machineSides = machineSides;
        }

        private List<PowerStorageInterface> GetAdjacentBatteries()
        {
            List<PowerStorageInterface> powerStoages = new List<PowerStorageInterface>();

            PositionUtils.GetSegmentPos(_machineSides.Front,
                _quantumStorageController.mnX, _quantumStorageController.mnY, _quantumStorageController.mnZ,
                out long segmentX, out long segmentY, out long segmentZ);
            GetAdjacentBattery(segmentX, segmentY, segmentZ, ref powerStoages);

            PositionUtils.GetSegmentPos(_machineSides.Right,
                _quantumStorageController.mnX, _quantumStorageController.mnY, _quantumStorageController.mnZ,
                out segmentX, out segmentY, out segmentZ);
            GetAdjacentBattery(segmentX, segmentY, segmentZ, ref powerStoages);

            PositionUtils.GetSegmentPos(_machineSides.Left,
                _quantumStorageController.mnX, _quantumStorageController.mnY, _quantumStorageController.mnZ,
                out segmentX, out segmentY, out segmentZ);

            PositionUtils.GetSegmentPos(_machineSides.Top,
                _quantumStorageController.mnX, _quantumStorageController.mnY, _quantumStorageController.mnZ,
                out segmentX, out segmentY, out segmentZ);

            PositionUtils.GetSegmentPos(_machineSides.Bottom,
                _quantumStorageController.mnX, _quantumStorageController.mnY, _quantumStorageController.mnZ,
                out segmentX, out segmentY, out segmentZ);

            return powerStoages;
        }

        private void GetAdjacentBattery(long segmentX, long segmentY, long segmentZ,
            ref List<PowerStorageInterface> powerStoages)
        {
            Segment adjacentBattery = _quantumStorageController.AttemptGetSegment(segmentX, segmentY, segmentZ);
            if (adjacentBattery != null && CubeHelper.HasEntity(adjacentBattery.GetCube(segmentX, segmentY, segmentZ)))
            {
                if (adjacentBattery.SearchEntity(segmentX, segmentY, segmentZ) is PowerStorageInterface powerStorage)
                {
                    powerStoages.Add(powerStorage);
                }
            }
        }

        public bool DeliverPower(float amount)
        {
            if (amount <= GetRemainingPowerCapacity())
            {
                _machinePower.CurrentPower += amount;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool HasPower()
        {
            return _machinePower.HasPower();
        }

        public float GetRemainingPowerCapacity()
        {
            return _machinePower.MaxPower - _machinePower.CurrentPower;
        }
    }
}

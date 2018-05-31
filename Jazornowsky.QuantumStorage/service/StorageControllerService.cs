using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jazornowsky.QuantumStorage.model;
using Jazornowsky.QuantumStorage.utils;

namespace Jazornowsky.QuantumStorage.service
{
    class StorageControllerService
    {
        private const string LogName = "StorageControllerService";

        private readonly QuantumStorageControllerMachine _quantumStorageController;
        private readonly MachineStorage _machineStorage;
        private readonly MachineSides _machineSides;

        public StorageControllerService(QuantumStorageControllerMachine quantumStorageController,
            MachineStorage machineStorage, MachineSides machineSides)
        {
            _quantumStorageController = quantumStorageController;
            _machineStorage = machineStorage;
            _machineSides = machineSides;
        }

        public void UpdateStorage()
        {
            IQuantumStorage connectedStorage = GetConnectedStorage();
            if (connectedStorage != null)
            {
                List<ItemBase> items = new List<ItemBase>();
                List<IQuantumStorage> storages = new List<IQuantumStorage> {connectedStorage};
                List<IQuantumIo> ios = new List<IQuantumIo>();
                connectedStorage.GetConnectedStorages(ref storages, ref ios);
                _machineStorage.StorageBlockCount = storages.Count;
                _machineStorage.MaxCapacity = 0;
                _machineStorage.ItemCount = 0;
                foreach (var storage in storages)
                {
                    _machineStorage.MaxCapacity += storage.GetCapacity();
                    _machineStorage.ItemCount += storage.GetItemCount();
                    items.AddRange(storage.GetItems());
                }

                foreach (var quantumIo in ios)
                {
                    quantumIo.SetControllerPos(_quantumStorageController.mnX, _quantumStorageController.mnY, _quantumStorageController.mnZ);
                }

                _machineStorage.Items = items;
            }
        }

        private IQuantumStorage GetConnectedStorage()
        {
            PositionUtils.GetSegmentPos(_machineSides.Back,
                _quantumStorageController.mnX, _quantumStorageController.mnY, _quantumStorageController.mnZ,
                out long segmentX, out long segmentY, out long segmentZ);
            Segment segment = _quantumStorageController.AttemptGetSegment(segmentX, segmentY, segmentZ);
            if (segment == null)
            {
                return null;
            }

            if (CubeHelper.HasEntity(segment.GetCube(segmentX, segmentY, segmentZ)))
            {
                if (segment.SearchEntity(segmentX, segmentY, segmentZ) is IQuantumStorage quantumStorage)
                {
                    return quantumStorage;
                }
            }

            return null;
        }

        public void AddItem(ref ItemBase item)
        {
            IQuantumStorage storage = GetConnectedStorage();
            List<IQuantumStorage> storages = new List<IQuantumStorage>(_machineStorage.StorageBlockCount);
            storages.Add(storage);
            storage.GetConnectedStorages(ref storages);
            foreach (IQuantumStorage connectedStorage in storages)
            {
                if (connectedStorage.IsFull())
                {
                    continue;
                }
                connectedStorage.AddItem(ref item);
                if (item == null || item.GetAmount() == 0)
                {
                    break;
                }
            }

            _quantumStorageController.Dirty = true;
        }

        public void TakeItem(ref ItemBase item)
        {
            var adjacentStorage = GetConnectedStorage();
            List<IQuantumStorage> storages = new List<IQuantumStorage>(_machineStorage.StorageBlockCount);
            storages.Add(adjacentStorage);
            LogUtils.LogDebug(LogName, "StorageCount: " + storages.Count);
            adjacentStorage.GetConnectedStorages(ref storages);
            foreach (var connectedStorage in storages)
            {
                LogUtils.LogDebug(LogName,
                    "Trying to take item from storage with item count: " + connectedStorage.GetItemCount());
                var itemNewInstance = connectedStorage.TakeItem(item);
                if (itemNewInstance != null && itemNewInstance.GetAmount() == 0)
                {
                    item = itemNewInstance;
                    return;
                }
            }
        }
    }
}
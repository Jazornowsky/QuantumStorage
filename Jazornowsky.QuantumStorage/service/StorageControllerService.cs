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
            SegmentEntity connectedStorage = GetConnectedStorage();
            if (connectedStorage != null)
            {
                List<ItemBase> items = new List<ItemBase>();
                List<SegmentEntity> segmentsEntity = new List<SegmentEntity> {connectedStorage};
                (connectedStorage as IQuantumStorage).GetConnectedSegments(ref segmentsEntity);
                _machineStorage.StorageBlockCount = 0;
                _machineStorage.MaxCapacity = 0;
                _machineStorage.ItemCount = 0;
                int controllerCount = 0;
                foreach (var segment in segmentsEntity)
                {
                    if (segment is IQuantumStorage storageEntity)
                    {
                        _machineStorage.StorageBlockCount++;
                        _machineStorage.MaxCapacity += storageEntity.GetCapacity();
                        _machineStorage.ItemCount += storageEntity.GetItemCount();
                        items.AddRange(storageEntity.GetItems());
                    } else if (segment is IQuantumIo ioEntity)
                    {
                        ioEntity.SetControllerPos(_quantumStorageController.mnX, _quantumStorageController.mnY, _quantumStorageController.mnZ);
                    } else if (segment is QuantumStorageControllerMachine)
                    {
                        controllerCount++;
                    }
                }

                if (controllerCount <= 1)
                {
                    _quantumStorageController._anotherControllerDetected = false;
                }
                else
                {
                    _quantumStorageController._anotherControllerDetected = true;
                }

                _machineStorage.Items = items;
            }
        }

        private SegmentEntity GetConnectedStorage()
        {
            PositionUtils.GetSegmentPos(_machineSides.Back,
                _quantumStorageController.mnX, _quantumStorageController.mnY, _quantumStorageController.mnZ,
                out long segmentX, out long segmentY, out long segmentZ);
            Segment segment = _quantumStorageController.AttemptGetSegment(segmentX, segmentY, segmentZ);
            if (segment == null || !CubeHelper.HasEntity(segment.GetCube(segmentX, segmentY, segmentZ)))
            {
                return null;
            }

            var segmentEntity = segment.SearchEntity(segmentX, segmentY, segmentZ);
            if (segmentEntity is IQuantumStorage)
            {
                return segmentEntity;
            }

            return null;
        }

        public bool AddItem(ref ItemBase item)
        {
            SegmentEntity adjacentEntity = GetConnectedStorage();
            if (adjacentEntity == null)
            {
                return false;
            }

            List<SegmentEntity> segmentEntities = new List<SegmentEntity>();
            segmentEntities.Add(adjacentEntity);
            (adjacentEntity as IQuantumStorage).GetConnectedSegments(ref segmentEntities);
            
            foreach (SegmentEntity segmentEntity in segmentEntities)
            {
                if (!(segmentEntity is IQuantumStorage storageEntity))
                {
                    continue;
                }

                if (storageEntity.IsFull())
                {
                    continue;
                }

                storageEntity.AddItem(ref item);
                if (item == null || item.GetAmount() == 0)
                {
                    _quantumStorageController.Dirty = true;
                    return true;
                }
            }

            return false;
        }

        public bool TakeItem(ref ItemBase item)
        {
            var adjacentEntity = GetConnectedStorage();
            if (adjacentEntity == null)
            {
                return false;
            }
            List<SegmentEntity> adjacentSegmentEntities = new List<SegmentEntity>();
            adjacentSegmentEntities.Add(adjacentEntity);
            (adjacentEntity as IQuantumStorage).GetConnectedSegments(ref adjacentSegmentEntities);
            foreach (var segmentEntity in adjacentSegmentEntities)
            {
                if (!(segmentEntity is IQuantumStorage storageEntity))
                {
                    continue;
                }

                var itemNewInstance = storageEntity.TakeItem(item);
                if (itemNewInstance != null && itemNewInstance.GetAmount() == 0)
                {
                    item = itemNewInstance;
                    return true;
                }
            }
            return false;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jazornowsky.QuantumStorage.model;
using Jazornowsky.QuantumStorage.utils;
using UnityEngine;

namespace Jazornowsky.QuantumStorage.service
{
    class StorageIoService
    {
        private const string LogName = "StorageIoService";

        private readonly QuantumOutputPortMachine _machine;
        private readonly MachineSides _machineSides;
        private QuantumStorageControllerMachine _quantumStorageController;

        public StorageIoService(QuantumOutputPortMachine machine, MachineSides machineSides)
        {
            _machine = machine;
            _machineSides = machineSides;
        }

        public QuantumStorageControllerMachine GetStorageController()
        {
            Segment segment =
                _machine.AttemptGetSegment(_machine.GetControllerPos()[0], _machine.GetControllerPos()[1],
                    _machine.GetControllerPos()[2]);
            if (segment != null &&
                CubeHelper.HasEntity(segment.GetCube(_machine.GetControllerPos()[0],
                    _machine.GetControllerPos()[1], _machine.GetControllerPos()[2])) &&
                segment.SearchEntity(_machine.GetControllerPos()[0], _machine.GetControllerPos()[1],
                    _machine.GetControllerPos()[2]) is QuantumStorageControllerMachine storageController)
            {
                return storageController;
            }

            return null;
        }

        public ItemConsumerInterface GetItemConsumer()
        {
            PositionUtils.GetSegmentPos(_machineSides.Back, _machine.mnX, _machine.mnY, _machine.mnZ, out long segmentX, out long segmentY,
                out long segmentZ);
            Segment adjacentSegment = _machine.AttemptGetSegment(segmentX, segmentY, segmentZ);
            if (adjacentSegment != null &&
                CubeHelper.HasEntity(adjacentSegment.GetCube(segmentX, segmentY, segmentZ)) &&
                adjacentSegment.SearchEntity(segmentX, segmentY, segmentZ) is ItemConsumerInterface adjacentItemConsumer)
            {
                return adjacentItemConsumer;
            }
            else
            {
                return null;
            }
        }

        private QuantumStorageControllerMachine GetAdjacentStorageController(Vector3 side)
        {
            PositionUtils.GetSegmentPos(side, _machine.mnX, _machine.mnY, _machine.mnZ,
                out long segmentX, out long segmentY, out long segmentZ);

            Segment adjacentSegment = _machine.AttemptGetSegment(segmentX, segmentY, segmentZ);
            if (adjacentSegment != null &&
                CubeHelper.HasEntity(adjacentSegment.GetCube(segmentX, segmentY, segmentZ)) &&
                adjacentSegment.SearchEntity(segmentX, segmentY, segmentZ) is QuantumStorageControllerMachine
                    adjacentController)
            {
                return adjacentController;
            }
            else
            {
                return null;
            }
        }

/*public void AddItem(ref ItemBase item)
{
    IQuantumStorage storage = GetController();
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
}

public void TakeItem(ref ItemBase item)
{
    var adjacentStorage = GetController();
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
}*/
    }
}
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

        private readonly AbstractQuantumIoMachine _machine;
        private readonly MachineSides _machineSides;

        public StorageIoService(AbstractQuantumIoMachine machine, MachineSides machineSides)
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
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jazornowsky.QuantumStorage.model;
using Jazornowsky.QuantumStorage.utils;
using UnityEngine;

namespace Jazornowsky.QuantumStorage
{
    class QuantumStorageMachine : MachineEntity, IQuantumStorage
    {
        private const string MachineName = "Quantum Storage";

        private readonly int _maxCapacity;
        private GameObject[] _storageBars;
        private bool _gameObjectsInitialised;
        private bool _objectColorInitialised;
        private List<ItemBase> _items;
        public MachineSides MachineSides = new MachineSides();

        public QuantumStorageMachine(MachineEntityCreationParameters parameters, int maxCapacity) : base(parameters)
        {
            mbNeedsUnityUpdate = true;
            _maxCapacity = maxCapacity;
            _items = new List<ItemBase>(_maxCapacity);
            PositionUtils.SetupSidesPositions(parameters.Flags, MachineSides);
        }

        public override void OnUpdateRotation(byte newFlags)
        {
            base.OnUpdateRotation(newFlags);
            PositionUtils.SetupSidesPositions(newFlags, MachineSides);
        }

        public bool IsFull()
        {
            return _items.GetItemCount() >= _maxCapacity;
        }

        public int GetCapacity()
        {
            return _maxCapacity;
        }

        public int GetItemCount()
        {
            return _items.GetItemCount();
        }

        public List<SegmentEntity> GetConnectedSegments(ref List<SegmentEntity> storages)
        {
            List<SegmentEntity> adjacentSegments = new List<SegmentEntity>();

            ProcessConnectedStorage(MachineSides.Front, ref storages, adjacentSegments);
            ProcessConnectedStorage(MachineSides.Back, ref storages, adjacentSegments);
            ProcessConnectedStorage(MachineSides.Right, ref storages, adjacentSegments);
            ProcessConnectedStorage(MachineSides.Left, ref storages, adjacentSegments);
            ProcessConnectedStorage(MachineSides.Top, ref storages, adjacentSegments);
            ProcessConnectedStorage(MachineSides.Bottom, ref storages, adjacentSegments);

            return adjacentSegments;
        }

        private void ProcessConnectedStorage(Vector3 side, ref List<SegmentEntity> segments, List<SegmentEntity> adjacentSegments)
        {
            PositionUtils.GetSegmentPos(side, mnX, mnY, mnZ, out long segmentX, out long segmentY,
                out long segmentZ);
            Segment adjacentSegment = AttemptGetSegment(segmentX, segmentY, segmentZ);
            if (adjacentSegment != null &&
                CubeHelper.HasEntity(adjacentSegment.GetCube(segmentX, segmentY, segmentZ)))
            {
                var segmentEntity = adjacentSegment.SearchEntity(segmentX, segmentY, segmentZ);
                if (segmentEntity is IQuantumStorage quantumStorageEntity)
                {
                    adjacentSegments.Add(segmentEntity);
                    if (!segments.Contains(segmentEntity))
                    {
                        segments.Add(segmentEntity);
                        quantumStorageEntity.GetConnectedSegments(ref segments);
                    }
                } else if (segmentEntity is IQuantumIo || segmentEntity is QuantumStorageControllerMachine)
                {
                    if (!segments.Contains(segmentEntity))
                    {
                        segments.Add(segmentEntity);
                    }
                }
            }
        }

        public List<ItemBase> GetItems()
        {
            return _items;
        }

        public ItemBase TakeItem(ItemBase item)
        {
            ItemBase newItemInstance = ItemBaseUtils.RemoveListItem(item, ref _items);
            RequestImmediateNetworkUpdate();
            return newItemInstance;
        }

        public void AddItem(ref ItemBase item)
        {
            item = item.AddListItem(ref _items, false, _maxCapacity);
            RequestImmediateNetworkUpdate();
        }

        public override string GetPopupText()
        {
            return DisplayUtils.MachineDisplay(MachineName + " " + _maxCapacity / 1024 + "k");
        }

        public override bool ShouldSave()
        {
            return true;
        }

        public override int GetVersion()
        {
            return QuantumStorageMod.Version;
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(_items.Count);
            foreach (var item in _items)
            {
                ItemFile.SerialiseItem(item, writer);
            }
        }

        public override void Read(BinaryReader reader, int entityVersion)
        {
            int itemCount = reader.ReadInt32();
            for (int index = 0; index < itemCount; index++)
            {
                _items.Add(ItemFile.DeserialiseItem(reader));
            }
        }

        public override void ReadNetworkUpdate(BinaryReader reader)
        {
            int itemCount = reader.ReadInt32();
            _items = new List<ItemBase>();
            for (int index = 0; index < itemCount; index++)
            {
                _items.Add(ItemFile.DeserialiseItem(reader));
            }
        }

        public override void WriteNetworkUpdate(BinaryWriter writer)
        {
            writer.Write(_items.Count);
            foreach (var item in _items)
            {
                ItemFile.SerialiseItem(item, writer);
            }
        }

        public override bool ShouldNetworkUpdate()
        {
            return true;
        }

        public override void UnityUpdate()
        {
            InitObjectColor();
            DisableEffects();
        }

        private void DisableEffects()
        {
            if (!GameObjectsInitialized() || _gameObjectsInitialised)
            {
                return;
            }

            mWrapper.mGameObjectList[0].gameObject.transform.Search("Power Ready").gameObject.SetActive(false);
            mWrapper.mGameObjectList[0].gameObject.transform.Search("Charging").gameObject.SetActive(false);
            mWrapper.mGameObjectList[0].gameObject.transform.Search("Waypoint Direction").gameObject.SetActive(false);
            _gameObjectsInitialised = true;
        }

        private void InitObjectColor()
        {
            if (!GameObjectsInitialized() || _objectColorInitialised)
            {
                return;
            }

            var meshTransform = mWrapper.mGameObjectList[0].transform.Find("Mesh");
            if (meshTransform?.gameObject?.GetComponent<MeshRenderer>()?.material == null)
            {
                return;
            }
            meshTransform.gameObject.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.magenta);
            _objectColorInitialised = true;
        }

        private bool GameObjectsInitialized()
        {
            return mWrapper?.mGameObjectList != null &&
                   mWrapper.mGameObjectList.Count > 0;
        }
    }
}
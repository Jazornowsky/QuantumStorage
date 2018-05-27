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

        public List<IQuantumStorage> GetConnectedStorages(ref List<IQuantumStorage> storages)
        {
            var quantumIos = new List<IQuantumIo>();
            return GetConnectedStorages(ref storages, ref quantumIos);
        }

        public List<IQuantumStorage> GetConnectedStorages(ref List<IQuantumStorage> storages, ref List<IQuantumIo> quantumIos)
        {
            List<IQuantumStorage> adjacentStorages = new List<IQuantumStorage>();

            ProcessConnectedStorage(MachineSides.Front, ref storages, adjacentStorages, ref quantumIos);
            ProcessConnectedStorage(MachineSides.Back, ref storages, adjacentStorages, ref quantumIos);
            ProcessConnectedStorage(MachineSides.Right, ref storages, adjacentStorages, ref quantumIos);
            ProcessConnectedStorage(MachineSides.Left, ref storages, adjacentStorages, ref quantumIos);
            ProcessConnectedStorage(MachineSides.Top, ref storages, adjacentStorages, ref quantumIos);
            ProcessConnectedStorage(MachineSides.Bottom, ref storages, adjacentStorages, ref quantumIos);

            return adjacentStorages;
        }

        private void ProcessConnectedStorage(Vector3 side, ref List<IQuantumStorage> storages, List<IQuantumStorage> adjacentStorages, ref List<IQuantumIo> quantumIos)
        {
            PositionUtils.GetSegmentPos(side, mnX, mnY, mnZ, out long segmentX, out long segmentY,
                out long segmentZ);
            Segment adjacentSegment = AttemptGetSegment(segmentX, segmentY, segmentZ);
            if (adjacentSegment != null &&
                CubeHelper.HasEntity(adjacentSegment.GetCube(segmentX, segmentY, segmentZ)))
            {
                if (adjacentSegment.SearchEntity(segmentX, segmentY, segmentZ) is IQuantumStorage adjacentStorage)
                {
                    adjacentStorages.Add(adjacentStorage);
                    if (!storages.Contains(adjacentStorage))
                    {
                        storages.Add(adjacentStorage);
                        adjacentStorage.GetConnectedStorages(ref storages, ref quantumIos);
                    }
                } else if (quantumIos != null && adjacentSegment.SearchEntity(segmentX, segmentY, segmentZ) is IQuantumIo adjacentIo)
                {
                    if (!quantumIos.Contains(adjacentIo))
                    {
                        quantumIos.Add(adjacentIo);
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
            
            var teleporterPart = mWrapper.mGameObjectList[0].transform.Find("Mesh").gameObject;
            MeshRenderer render2 = teleporterPart.GetComponent<MeshRenderer>();
            render2.material.SetColor("_Color", Color.magenta);
            _objectColorInitialised = true;
        }

        private bool GameObjectsInitialized()
        {
            return mWrapper?.mGameObjectList != null &&
                   mWrapper.mGameObjectList.Count > 0;
        }
    }
}
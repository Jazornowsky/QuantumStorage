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
        private bool _storageBarsInitialised;
        private bool _sphereInitialised;
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
            List<IQuantumStorage> adjacentStorages = new List<IQuantumStorage>();

            PositionUtils.GetSegmentPos(MachineSides.Front, mnX, mnY, mnZ, out long segmentX, out long segmentY,
                out long segmentZ);
            ProcessConnectedStorage(ref storages, segmentX, segmentY, segmentZ, adjacentStorages);

            PositionUtils.GetSegmentPos(MachineSides.Back, mnX, mnY, mnZ, out segmentX, out segmentY, out segmentZ);
            ProcessConnectedStorage(ref storages, segmentX, segmentY, segmentZ, adjacentStorages);

            PositionUtils.GetSegmentPos(MachineSides.Right, mnX, mnY, mnZ, out segmentX, out segmentY, out segmentZ);
            ProcessConnectedStorage(ref storages, segmentX, segmentY, segmentZ, adjacentStorages);

            PositionUtils.GetSegmentPos(MachineSides.Left, mnX, mnY, mnZ, out segmentX, out segmentY, out segmentZ);
            ProcessConnectedStorage(ref storages, segmentX, segmentY, segmentZ, adjacentStorages);

            PositionUtils.GetSegmentPos(MachineSides.Top, mnX, mnY, mnZ, out segmentX, out segmentY, out segmentZ);
            ProcessConnectedStorage(ref storages, segmentX, segmentY, segmentZ, adjacentStorages);

            PositionUtils.GetSegmentPos(MachineSides.Bottom, mnX, mnY, mnZ, out segmentX, out segmentY, out segmentZ);
            ProcessConnectedStorage(ref storages, segmentX, segmentY, segmentZ, adjacentStorages);

            return adjacentStorages;
        }

        private void ProcessConnectedStorage(ref List<IQuantumStorage> storages, long segmentX, long segmentY,
            long segmentZ,
            List<IQuantumStorage> adjacentStorages)
        {
            Segment adjacentStorageSegment = AttemptGetSegment(segmentX, segmentY, segmentZ);
            if (adjacentStorageSegment != null &&
                CubeHelper.HasEntity(adjacentStorageSegment.GetCube(segmentX, segmentY, segmentZ)) &&
                adjacentStorageSegment.SearchEntity(segmentX, segmentY, segmentZ) is IQuantumStorage adjacentStorage)
            {
                adjacentStorages.Add(adjacentStorage);
                if (!storages.Contains(adjacentStorage))
                {
                    storages.Add(adjacentStorage);
                    adjacentStorage.GetConnectedStorages(ref storages);
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
            return newItemInstance;
        }

        public void AddItem(ref ItemBase item)
        {
            item = item.AddListItem(ref _items, false, _maxCapacity);
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

        public void InitStorageBars()
        {
            if (mWrapper?.mGameObjectList?.Count == null)
            {
                return;
            }

            var mWrapperMGameObject = mWrapper.mGameObjectList[0];
            if (mWrapperMGameObject == null || mWrapperMGameObject.gameObject == null)
            {
                return;
            }

            var powerStorageBars = mWrapperMGameObject.gameObject.GetComponentsInChildren<PowerStorageBar>();
            if (powerStorageBars == null)
            {
                return;
            }

            _storageBars = new GameObject[powerStorageBars.Length];

            for (int index = 0; index < powerStorageBars.Length; index++)
            {
                if (powerStorageBars[index] == null || powerStorageBars[index].gameObject == null)
                {
                    return;
                }

                _storageBars[index] = powerStorageBars[index].gameObject;
            }

            _storageBarsInitialised = true;
        }

        public void InitSphere()
        {
            var sphereHolder = mWrapper.mGameObjectList[0].transform.Search("_SphereHolder");
            if (sphereHolder == null)
            {
                return;
            }

            sphereHolder.gameObject.SetActive(true);

            LODGroup lodGroup = mWrapper.mGameObjectList[0].GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                return;
            }

            lodGroup.enabled = true;

            sphereHolder.gameObject.GetComponent<RotateConstantlyScript>().gameObject.GetComponent<LODGroup>().enabled =
                true;

            _sphereInitialised = true;
        }

        public override void UnityUpdate()
        {
            if (!_storageBarsInitialised)
            {
                InitStorageBars();
                return;
            }

            if (!_sphereInitialised)
            {
                InitSphere();
            }

            if (!mSegment.mbOutOfView && AmInSameRoom())
            {
                float barsSize = ((float) _items.GetItemCount()) / ((float) _maxCapacity);
                for (int index = 0; index < _storageBars.Length; index++)
                {
                    _storageBars[index].transform.localScale = new Vector3(barsSize, 1f, 1f);
                }
            }

            AudioSource component = this.mWrapper.mGameObjectList[0].gameObject.GetComponent<AudioSource>();
            if ((double) this.mDistanceToPlayer < 8.0)
            {
                component.pitch = 0.4F;
//                component.priority = 72 + (int) ((double) this.mDistanceToPlayer * 8.0);
                component.volume = 0.8f;
            }
        }
    }
}
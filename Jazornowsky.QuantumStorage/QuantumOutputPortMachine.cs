using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jazornowsky.QuantumStorage.model;
using Jazornowsky.QuantumStorage.service;
using Jazornowsky.QuantumStorage.utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jazornowsky.QuantumStorage
{
    class QuantumOutputPortMachine : MachineEntity, ItemSupplierInterface, IQuantumIo
    {
        public static readonly string MachineName = "Quantum Output Port";

        private readonly MachineSides _machineSides = new MachineSides();
        private readonly StorageIoService _storageIoService;
        private GameObject _holoPreview;
        private GameObject _holoCubePreview;
        private bool _holoPreviewDirty;
        private bool _linkedToGo;
        private long[] _controllerPos = new long[3];
        public ItemBase Exemplar;

        public QuantumOutputPortMachine(MachineEntityCreationParameters parameters) : base(parameters)
        {
            mbNeedsLowFrequencyUpdate = true;
            mbNeedsUnityUpdate = true;
            PositionUtils.SetupSidesPositions(parameters.Flags, _machineSides);
            _storageIoService = new StorageIoService(this, _machineSides);
        }

        public override void OnUpdateRotation(byte newFlags)
        {
            base.OnUpdateRotation(newFlags);
            PositionUtils.SetupSidesPositions(newFlags, _machineSides);
        }

        public override string GetPopupText()
        {
            string txt = DisplayUtils.MachineDisplay(MachineName);
            if ((_controllerPos[0] == 0 && _controllerPos[1] == 0 && _controllerPos[2] == 0) ||
                _storageIoService.GetStorageController() == null)
            {
                txt += "QUANTUM STORAGE CONTROLLER NOT FOUND.\n";
            }

            if (!_storageIoService.GetStorageController().HasPower())
            {
                txt += "QUANTUM STORAGE CONTROLLER HAS NO POWER.\n";
            }

            if (Input.GetButtonDown("Interact") && (UIManager.AllowInteracting))
            {
                UIManager.ForceNGUIUpdate = 0.1f;
                AudioHUDManager.instance.HUDIn();
            }

            return txt;
        }

        public bool PeekItem(StorageUserInterface sourceEntity, out ItemBase item, out ushort cubeType,
            out ushort cubeValue)
        {
            item = null;
            cubeType = 0;
            cubeValue = 0;

            if (Exemplar == null)
            {
                return false;
            }

            var exemplarCopy = Exemplar.NewInstance();
            exemplarCopy.SetAmount(1);
            var itemBase = _storageIoService.GetStorageController().GetItems()
                .Find(x => x.mnItemID == exemplarCopy.mnItemID);
            if (itemBase != null && itemBase.GetAmount() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryTakeItem(StorageUserInterface sourceEntity, out ItemBase item, out ushort cubeType,
            out ushort cubeValue,
            bool sendImmediateNetworkUpdate)
        {
            item = null;
            cubeType = 0;
            cubeValue = 0;

            if (Exemplar == null)
            {
                return false;
            }

            var exemplarCopy = Exemplar.NewInstance();
            exemplarCopy.SetAmount(1);
            var itemBase = _storageIoService.GetStorageController().GetItems()
                .Find(x => x.mnItemID == exemplarCopy.mnItemID);
            if (itemBase != null && itemBase.GetAmount() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void LowFrequencyUpdate()
        {
            var itemConsumer = _storageIoService.GetItemConsumer();
            if (_storageIoService.GetStorageController().HasPower() && itemConsumer != null && Exemplar != null)
            {
                var exemplarCopy = Exemplar.NewInstance();
                var itemToGive = Exemplar.NewInstance();
                exemplarCopy.SetAmount(1);
                itemToGive.SetAmount(1);
                if (exemplarCopy != null && exemplarCopy.GetAmount() > 0)
                {
                    var itemBase = _storageIoService.GetStorageController().GetItems()
                        .Find(x => x.mnItemID == exemplarCopy.mnItemID);
                    LogUtils.LogDebug(MachineName, "itemBase: " + itemBase.GetDisplayString());
                    if (itemBase != null && itemBase.GetAmount() > 0 && itemConsumer.TryDeliverItem(this, itemToGive, 0, 0, true))
                    {
                        _storageIoService.GetStorageController().TakeItem(ref exemplarCopy);
                    }
                }
            }
        }

        public long[] GetControllerPos()
        {
            return _controllerPos;
        }

        public void SetControllerPos(long x, long y, long z)
        {
            _controllerPos[0] = x;
            _controllerPos[1] = y;
            _controllerPos[2] = z;
        }

        public void SetExemplar(ItemBase lItem)
        {
            /*if (this.mSourceCrate != null && (double)this.mSourceCrate.mrOutputLockTimer > 0.0)
                Debug.LogWarning((object)"Warning, changing exemplar whilst we have a SourceCrate locked!");*/
            if (this.Exemplar == null || lItem == null)
            {
                _holoPreviewDirty = true;
                if (lItem == null)
                    Debug.Log((object) "MSOP Cleared Exemplar");
            }
            else if (lItem.mType == ItemType.ItemCubeStack)
            {
                if (this.Exemplar.mType == ItemType.ItemCubeStack)
                {
                    if ((lItem as ItemCubeStack).mCubeType == (this.Exemplar as ItemCubeStack).mCubeType &&
                        (lItem as ItemCubeStack).mCubeValue == (this.Exemplar as ItemCubeStack).mCubeValue)
                    {
                        return;
                    }

                    _holoPreviewDirty = true;
                }
                else
                {
                    _holoPreviewDirty = true;
                }
            }
            else
            {
                if (lItem.mnItemID == Exemplar.mnItemID)
                    return;
                _holoPreviewDirty = true;
            }

            Exemplar = lItem;
            MarkDirtyDelayed();
            if (Exemplar == null)
                FloatingCombatTextManager.instance.QueueText(mnX, mnY, mnZ, 1.05f,
                    PersistentSettings.GetString("Cleared!"), Color.cyan, 1f, 64f);
            else
                FloatingCombatTextManager.instance.QueueText(mnX, mnY, mnZ, 1.05f,
                    PersistentSettings.GetString("Currently_outputting") + " " + ItemManager.GetItemName(Exemplar),
                    Color.cyan, 1f, 64f);
        }

        public override void UnitySuspended()
        {
            if (_holoPreview != null)
                Object.Destroy(_holoPreview);
            _holoPreview = null;
        }

        public override void UnityUpdate()
        {
            /*if (!_linkedToGo)
            {
                if (mWrapper?.mGameObjectList == null)
                    return;
                if (mWrapper.mGameObjectList[0].gameObject == (Object) null)
                    Debug.LogError((object) "MSIP missing game object #0 (GO)?");
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                Color color = Color.white;
                if (mValue == 0)
                    color = Color.green;
                if (mValue == 1)
                    color = Color.cyan;
                if (mValue == 2)
                    color = Color.magenta;
                properties.SetColor("_GlowColor", color * 2f);
//                OutputHopperObject = mWrapper.mGameObjectList[0].gameObject.transform.Search("Output Hopper").gameObject;
                _holoCubePreview = mWrapper.mGameObjectList[0].gameObject.transform.Search("HoloCube").gameObject;
//                NoItem = this.mWrapper.mGameObjectList[0].gameObject.transform.Search("NoItemSet").gameObject;
//                SetTier(this.NoItem);
                _holoCubePreview.SetActive(false);
                _linkedToGo = true;
                _holoPreviewDirty = true;
            }
            else
            {
                if (_holoPreviewDirty)
                {
                    if (_holoPreview != null)
                    {
                        Object.Destroy(_holoPreview);
                        _holoPreview = null;
                    }

                    if (Exemplar != null)
                    {
//                        NoItem.SetActive(false);
                        if (this.Exemplar.mType == ItemType.ItemCubeStack)
                        {
                            _holoCubePreview.SetActive(true);
//                            SetTier(_holoCubePreview);
                        }
                        else
                        {
                            int index = (int) ItemEntry.mEntries[Exemplar.mnItemID].Object;
                            _holoPreview = Object.Instantiate<GameObject>(
                                SpawnableObjectManagerScript.instance.maSpawnableObjects[index],
                                mWrapper.mGameObjectList[0].gameObject.transform.position +
                                new Vector3(0.0f, 1.5f, 0.0f), Quaternion.identity);
                            _holoPreview.transform.parent = mWrapper.mGameObjectList[0].gameObject.transform;
                            if (_holoPreview.GetComponent<SetIngotMPB>() != null)
                                Object.Destroy(_holoPreview.GetComponent<SetIngotMPB>());
//                            SetTier(_holoPreview);
                            _holoPreview.gameObject.AddComponent<RotateConstantlyScript>();
                            _holoPreview.gameObject.GetComponent<RotateConstantlyScript>().YRot = 1f;
                            _holoPreview.gameObject.GetComponent<RotateConstantlyScript>().XRot = 0.35f;
                            _holoPreview.SetActive(true);
                            _holoCubePreview.SetActive(false);
                        }
                    }
                    else
                    {
                        _holoCubePreview.SetActive(false);
//                        NoItem.SetActive(true);
                    }

                    _holoPreviewDirty = false;
                }

                if (this.mbWellBehindPlayer || this.mSegment.mbOutOfView)
                {
                    if (_holoPreview != null && _holoPreview.activeSelf)
                        _holoPreview.SetActive(false);
                    /*if (OutputHopperObject.activeSelf)
                        OutputHopperObject.SetActive(false);
                    if (Exemplar == null)
                    {
                        if (NoItem.activeSelf)
                            NoItem.SetActive(false);
                    }#1#
                    else if (Exemplar.mType == ItemType.ItemCubeStack)
                    {
                        if (_holoCubePreview.activeSelf)
                            _holoCubePreview.SetActive(false);
                    }
                    else if (_holoPreview.activeSelf)
                        _holoPreview.SetActive(false);
                }
                else
                {
                    if (_holoPreview != null && !_holoPreview.activeSelf)
                        this._holoPreview.SetActive(true);
                    /*if (!OutputHopperObject.activeSelf)
                        OutputHopperObject.SetActive(true);
                    if (this.Exemplar == null)
                    {
                        if (!NoItem.activeSelf)
                            NoItem.SetActive(true);
                    }#1#
                    else if (Exemplar.mType == ItemType.ItemCubeStack)
                    {
                        if (!_holoCubePreview.activeSelf)
                            _holoCubePreview.SetActive(true);
                    }
                    else if (!_holoPreview.activeSelf)
                        _holoPreview.SetActive(true);
                }
            }*/
        }

        public override HoloMachineEntity CreateHolobaseEntity(Holobase holobase)
        {
            HolobaseEntityCreationParameters parameters = new HolobaseEntityCreationParameters(this);
            parameters.AddVisualisation(holobase.mPreviewCube).Color = Color.yellow;
            return holobase.CreateHolobaseEntity(parameters);
        }

        public override bool ShouldNetworkUpdate()
        {
            return true;
        }

        public override void ReadNetworkUpdate(BinaryReader reader)
        {
            Exemplar = ItemFile.DeserialiseItem(reader);
        }

        public override void WriteNetworkUpdate(BinaryWriter writer)
        {
            ItemFile.SerialiseItem(Exemplar, writer);
        }

        public override void Write(BinaryWriter writer)
        {
            ItemFile.SerialiseItem(Exemplar, writer);
        }

        public override void Read(BinaryReader reader, int entityVersion)
        {
            Exemplar = ItemFile.DeserialiseItem(reader);
        }
    }
}
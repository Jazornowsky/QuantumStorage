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
        private bool _hooverInitialised;
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
            var quantumStorageController = _storageIoService.GetStorageController();
            if ((_controllerPos[0] == 0 && _controllerPos[1] == 0 && _controllerPos[2] == 0) ||
                quantumStorageController == null)
            {
                txt += "QUANTUM STORAGE CONTROLLER NOT FOUND.\n";
            }

            if (quantumStorageController != null && !quantumStorageController.HasPower())
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
            UpdateOutput();
        }

        private void UpdateOutput()
        {
            var itemConsumer = _storageIoService.GetItemConsumer();
            if (!_storageIoService.GetStorageController().HasPower() || itemConsumer == null ||
                Exemplar == null)
            {
                return;
            }

            var exemplarCopy = Exemplar.NewInstance();
            var itemToGive = Exemplar.NewInstance();
            exemplarCopy.SetAmount(1);
            itemToGive.SetAmount(1);

            if (exemplarCopy == null || exemplarCopy.GetAmount() <= 0)
            {
                return;
            }

            var itemInStorage = _storageIoService.GetStorageController().GetItems()
                .Find(x => x.mnItemID == exemplarCopy.mnItemID);

            if (itemInStorage != null && itemInStorage.GetAmount() > 0 &&
                itemConsumer.TryDeliverItem(this, itemToGive, 0, 0, true))
            {
                _storageIoService.GetStorageController().TakeItem(ref exemplarCopy);
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
            if (Exemplar == null || lItem == null)
            {
                _holoPreviewDirty = true;
                if (lItem == null) { 
                    Debug.Log((object) "MSOP Cleared Exemplar");
                }
            }
            else if (lItem.mType == ItemType.ItemCubeStack)
            {
                if (Exemplar.mType == ItemType.ItemCubeStack)
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
                if (lItem.mnItemID == Exemplar.mnItemID) { 
                    return;
                }
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
            UpdateMeshText();
            UpdateWorkLight();
            InitHoover();
        }

        private void UpdateMeshText()
        {
            if (mDistanceToPlayer >= 12.0 || !AmInSameRoom())
            {
                return;
            }

            if (!GameObjectsInitialized())
            {
                return;
            }

            string meshText = "OUTPUT:\n";
            if (Exemplar != null)
            {
                meshText += "" + Exemplar.GetDisplayString();
            }
            else
            {
                meshText += "NONE";
            }

            TextMesh textMesh = mWrapper.mGameObjectList[0].gameObject.transform.Search("Storage Text")
                .GetComponent<TextMesh>();
            if (textMesh != null)
            {
                Renderer renderer = textMesh.GetComponent<Renderer>();
                textMesh.text = meshText;
                renderer.enabled = true;
            }
        }

        private void UpdateWorkLight()
        {
            if (!GameObjectsInitialized())
            {
                return;
            }
            Light light = mWrapper.mGameObjectList[0].transform.Search("HooverGraphic").GetComponent<Light>();
            var maxLightDistance = 64f;

            bool flag = !mbWellBehindPlayer;

            if (mDistanceToPlayer > (double) maxLightDistance)
            {
                flag = false;
            }
            if (flag)
            {
                if (!light.enabled)
                {
                    light.enabled = true;
                    light.range = 0.05f;
                }
                light.color = Color.Lerp(light.color, Color.magenta, Time.deltaTime);
                light.range += 0.1f;

                if (light.range > 1.0)
                {
                    light.range = 1f;
                }
            }

            if (!light.enabled)
            {
                return;
            }
            if (light.range < 0.150000005960464)
            {
                light.enabled = false;
            }
            else
            {
                light.range *= 0.95f;
            }
        }

        private void InitHoover()
        {
            if (!GameObjectsInitialized() || _hooverInitialised)
            {
                return;
            }
            var hooverPart = mWrapper.mGameObjectList[0].transform.Search("HooverGraphic")
                .GetComponent<ParticleSystem>();
            hooverPart.SetEmissionRate(0.0f);
            _hooverInitialised = true;
        }

        private bool GameObjectsInitialized()
        {
            return mWrapper.mGameObjectList != null && mWrapper.mGameObjectList.Count > 0;
        }

        public override HoloMachineEntity CreateHolobaseEntity(Holobase holobase)
        {
            HolobaseEntityCreationParameters parameters = new HolobaseEntityCreationParameters(this);
            parameters.AddVisualisation(holobase.mPreviewCube).Color = Color.yellow;
            return holobase.CreateHolobaseEntity(parameters);
        }

        public override bool ShouldSave()
        {
            return true;
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
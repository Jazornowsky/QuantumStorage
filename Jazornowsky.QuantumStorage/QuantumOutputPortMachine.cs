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
    class QuantumOutputPortMachine : AbstractQuantumIoMachine, ItemSupplierInterface
    {
        public static readonly string MachineName = "Quantum Output Port";

        public ItemBase Exemplar;

        public QuantumOutputPortMachine(MachineEntityCreationParameters parameters) : base(parameters)
        {
        }

        public override string GetPopupText()
        {
            string txt = DisplayUtils.MachineDisplay(MachineName);
            var quantumStorageController = StorageIoService.GetStorageController();
            if ((ControllerPos[0] == 0 && ControllerPos[1] == 0 && ControllerPos[2] == 0) ||
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
            var itemBase = StorageIoService.GetStorageController().GetItems()
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
            var itemBase = StorageIoService.GetStorageController().GetItems()
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
            var itemConsumer = StorageIoService.GetItemConsumer();
            var storageController = StorageIoService.GetStorageController();
            if (storageController == null || !StorageIoService.GetStorageController().HasPower() ||
                itemConsumer == null || Exemplar == null)
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

            var itemInStorage = StorageIoService.GetStorageController().GetItems()
                .Find(x => x.mnItemID == exemplarCopy.mnItemID);

            if (itemInStorage != null && itemInStorage.GetAmount() > 0 &&
                itemConsumer.TryDeliverItem(this, itemToGive, 0, 0, true))
            {
                StorageIoService.GetStorageController().TakeItem(ref exemplarCopy);
            }
        }

        public void SetExemplar(ItemBase lItem)
        {
            if (lItem.mType == ItemType.ItemCubeStack)
            {
                if (Exemplar.mType == ItemType.ItemCubeStack)
                {
                    if ((lItem as ItemCubeStack).mCubeType == (this.Exemplar as ItemCubeStack).mCubeType &&
                        (lItem as ItemCubeStack).mCubeValue == (this.Exemplar as ItemCubeStack).mCubeValue)
                    {
                        return;
                    }
                }
            }
            else
            {
                if (lItem.mnItemID == Exemplar.mnItemID)
                {
                    return;
                }
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

        public override void UnityUpdate()
        {
            base.UnityUpdate();
            UpdateMeshText();
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
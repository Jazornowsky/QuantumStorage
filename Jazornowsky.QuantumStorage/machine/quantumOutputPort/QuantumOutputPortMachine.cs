using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jazornowsky.QuantumStorage.machine.quantumOutputPort;
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

        protected internal ItemBase Exemplar;
        protected internal bool Enabled;
        private bool ItemTaken; // is item taken from Quantum Storage and currently held by QuantumOutputPort
        private QuantumOutputPortPopuTextManager QuantumOutputPortPopupTextManager;

        public QuantumOutputPortMachine(MachineEntityCreationParameters parameters) : base(parameters)
        {
            ItemTaken = false;
            Enabled = true;
            QuantumOutputPortPopupTextManager = new QuantumOutputPortPopuTextManager(this);
        }

        public override string GetPopupText()
        {
            return QuantumOutputPortPopupTextManager.getPopupText();
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
            out ushort cubeValue, bool sendImmediateNetworkUpdate)
        {
            item = null;
            cubeType = 0;
            cubeValue = 0;

            if (!Enabled)
            {
                return false;
            }

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
            if (storageController == null || !storageController.IsOperating() ||
                !storageController.IsOutputEnabled() || !Enabled ||
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

            if (!ItemTaken)
            {
                var itemInStorage = StorageIoService.GetStorageController().GetItems()
                    .Find(x => x.Compare(exemplarCopy));
                if (itemInStorage == null || itemInStorage.GetAmount() <= 0)
                {
                    return;
                }

                exemplarCopy.SetAmount(1);
                ItemTaken = StorageIoService.GetStorageController().TakeItem(ref exemplarCopy);
                if (!ItemTaken)
                {
                    return;
                }
            }

            if (itemConsumer.TryDeliverItem(this, itemToGive, 0, 0, true))
            {
                ItemTaken = false;
                MarkDirtyDelayed();
            }
        }

        public void SetExemplar(ItemBase lItem)
        {
            if (lItem == null)
            {
                Exemplar = null;
                MarkDirtyDelayed();
                FloatingCombatTextManager.instance.QueueText(mnX, mnY, mnZ,
                    QuantumStorageModSettings.MachinesFloatingTextScale,
                    "Cleared!",
                    QuantumStorageModSettings.MachinesFloatingTextColor,
                    QuantumStorageModSettings.MachinesFloatingTextDuration,
                    QuantumStorageModSettings.MachinesFloatingTextDistance);
                return;
            }

            if (lItem?.mType == null)
            {
                return;
            }

            if (lItem.mType == ItemType.ItemCubeStack)
            {
                if (Exemplar != null && Exemplar.mType == ItemType.ItemCubeStack)
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
                if (Exemplar != null && lItem.mnItemID == Exemplar.mnItemID)
                {
                    return;
                }
            }

            Exemplar = lItem;
            MarkDirtyDelayed();
            FloatingCombatTextManager.instance.QueueText(mnX, mnY, mnZ,
                QuantumStorageModSettings.MachinesFloatingTextScale,
                PersistentSettings.GetString("Currently_outputting") + " " + ItemManager.GetItemName(Exemplar),
                    QuantumStorageModSettings.MachinesFloatingTextColor,
                    QuantumStorageModSettings.MachinesFloatingTextDuration,
                    QuantumStorageModSettings.MachinesFloatingTextDistance);
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

            string meshText;
            if (Enabled)
            {
                meshText = "OUTPUT:\n";
                if (Exemplar != null)
                {
                    meshText += "" + Exemplar.GetDisplayString();
                }
                else
                {
                    meshText += "NONE";
                }
            }
            else
            {
                meshText = "OFF";
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

        public override void Write(BinaryWriter writer)
        {
            ItemFile.SerialiseItem(Exemplar, writer);
            writer.Write(ItemTaken);
        }

        public override void Read(BinaryReader reader, int entityVersion)
        {
            Exemplar = ItemFile.DeserialiseItem(reader);
            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                ItemTaken = reader.ReadBoolean();
            }
        }

        public override void OnDelete()
        {
            if (!WorldScript.mbIsServer)
            {
                return;
            }

            System.Random random = new System.Random();
            if (ItemTaken && Exemplar != null)
            {
                Vector3 velocity = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f);
                ItemManager.instance.DropItem(Exemplar, this.mnX, this.mnY, this.mnZ, velocity);
                Exemplar = null;
                ItemTaken = false;
            }

            base.OnDelete();
        }

        public void ToggleStatus()
        {
            Enabled = !Enabled;
            MarkDirtyDelayed();
        }
    }
}
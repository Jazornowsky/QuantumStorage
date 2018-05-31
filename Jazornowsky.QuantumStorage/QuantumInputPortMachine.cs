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
    class QuantumInputPortMachine : AbstractQuantumIoMachine, ItemConsumerInterface
    {
        public static readonly string MachineName = "Quantum Input Port";

        private ItemBase _incomingItem;
        private Direction _nextInsertDirection = Direction.LEFT;

        public QuantumInputPortMachine(MachineEntityCreationParameters parameters) : base(parameters)
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

        public override void LowFrequencyUpdate()
        {
            var storageController = StorageIoService.GetStorageController();
            if (storageController == null || _incomingItem == null || !storageController.IsOperating() || !storageController.IsInputEnabled())
            {
                return;
            }


            LogUtils.LogDebug(MachineName, "Adding item to QSS: " + _incomingItem.GetDisplayString());
            storageController.AddItem(ref _incomingItem);
            if (_incomingItem == null || _incomingItem.GetAmount() == 0)
            {
                _incomingItem = null;
                return;
            }
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

            string meshText = "INPUT:\n";
            if (_incomingItem != null)
            {
                meshText += "" + _incomingItem.GetDisplayString();
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

        public override void Write(BinaryWriter writer)
        {
            ItemFile.SerialiseItem(_incomingItem, writer);
        }

        public override void Read(BinaryReader reader, int entityVersion)
        {
            _incomingItem = ItemFile.DeserialiseItem(reader);
        }

        public bool TryDeliverItem(StorageUserInterface sourceEntity, ItemBase item, ushort cubeType, ushort cubeValue,
            bool sendImmediateNetworkUpdate)
        {
            if (_incomingItem != null || sourceEntity == null || (item == null && cubeType == 0 && cubeValue == 0))
            {
                return false;
            }

            if (item == null)
            {
                item = ItemManager.SpawnCubeStack(cubeType, cubeValue, 1);
            }

            var storageController = StorageIoService.GetStorageController();
            if (storageController == null || !storageController.HasPower())
            {
                return false;
            }

            if (_nextInsertDirection == Direction.LEFT)
            {
                _nextInsertDirection = Direction.RIGHT;
            } else if (_nextInsertDirection == Direction.RIGHT)
            {
                _nextInsertDirection = Direction.FRONT;
            } else if (_nextInsertDirection == Direction.FRONT)
            {
                _nextInsertDirection = Direction.LEFT;
            }
            

            SegmentEntity segment = (SegmentEntity)sourceEntity;
            PositionUtils.GetSegmentPos(MachineSides.Left, mnX, mnY, mnZ, out long x, out long y, out long z);

            if (_nextInsertDirection == Direction.LEFT && PositionUtils.IsSegmentPositionEqual(segment, x, y, z))
            {
                _incomingItem = item;
            }
            PositionUtils.GetSegmentPos(MachineSides.Right, mnX, mnY, mnZ, out x, out y, out z);
            if (_nextInsertDirection == Direction.RIGHT && PositionUtils.IsSegmentPositionEqual(segment, x, y, z))
            {
                _incomingItem = item;
            }
            PositionUtils.GetSegmentPos(MachineSides.Front, mnX, mnY, mnZ, out x, out y, out z);
            if (_nextInsertDirection == Direction.FRONT && PositionUtils.IsSegmentPositionEqual(segment, x, y, z))
            {
                _incomingItem = item;
            }

            if (_incomingItem == null)
            {
                return false;
            }

            return true;
        }
    }
}
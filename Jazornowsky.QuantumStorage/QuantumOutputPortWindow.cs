using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jazornowsky.QuantumStorage
{
    class QuantumOutputPortWindow : BaseMachineWindow
    {
        public static bool Dirty;
        private bool _itemSearch;

        public QuantumOutputPortWindow()
        {
        }

        public override void SpawnWindow(SegmentEntity targetEntity)
        {
            if (!(targetEntity is QuantumOutputPortMachine))
            {
                UIManager.RemoveUIRules("Machine");
            }
            else
            {
                manager.SetTitle("Quantum Output Port");
                if (!_itemSearch)
                {
                    manager.AddIcon("outputitem", "empty", Color.white, 10, 0);
                    manager.AddBigLabel("outputtitle", PersistentSettings.GetString("Choose_output"), Color.white,
                        70, 0);
                    manager.AddButton("chooseitem", PersistentSettings.GetString("Choose_Item"), 100, 75);
                }
                else
                {
                    ItemSearchWindow.SpawnWindow((BaseMachineWindow) this);
                }

                Dirty = true;
            }
        }

        public override void UpdateMachine(SegmentEntity targetEntity)
        {
            QuantumOutputPortMachine quantumOutputPort = targetEntity as QuantumOutputPortMachine;
            if (quantumOutputPort == null)
            {
                GenericMachinePanelScript.instance.Hide();
                UIManager.RemoveUIRules("Machine");
            }
            else
            {
                if (!Dirty)
                {
                    return;
                }

                if (_itemSearch)
                {
                    if (ItemSearchWindow.UpdateMachine((BaseMachineWindow) this))
                    {
                        Dirty = true;
                        return;
                    }
                }
                else
                {
                    ItemBase exemplar = quantumOutputPort.Exemplar;
                    if (exemplar != null)
                    {
                        string itemName = ItemManager.GetItemName(exemplar);
                        manager.UpdateIcon("outputitem", ItemManager.GetItemIcon(exemplar), Color.white);
                        manager.UpdateLabel("outputtitle", itemName, Color.white);
                    }
                }

                Dirty = false;
            }
        }

        public override bool ButtonClicked(string name, SegmentEntity targetEntity)
        {
            QuantumOutputPortMachine port = targetEntity as QuantumOutputPortMachine;
            if (name == "outputitem")
            {
                if (port.Exemplar != null)
                {
                    QuantumOutputPortWindow.SetExemplar(WorldScript.mLocalPlayer, port, (ItemBase) null);
                    manager.RedrawWindow();
                }

                return true;
            }

            if (name == "chooseitem")
            {
                _itemSearch = true;
                ItemSearchWindow.SetupUIRules();
                Redraw(targetEntity);
                return true;
            }

            if (ItemSearchWindow.HandleButtonPress(this, name, out var selectedItem))
            {
                _itemSearch = false;
                manager.RedrawWindow();
            }

            if (selectedItem == null)
            {
                return false;
            }

            SetExemplar(WorldScript.mLocalPlayer, port, selectedItem);
            return true;
        }

        public override bool ButtonRightClicked(string name, SegmentEntity targetEntity)
        {
            if (name != "outputitem")
                return base.ButtonRightClicked(name, targetEntity);
            _itemSearch = true;
            ItemSearchWindow.SetupUIRules();
            Redraw(targetEntity);
            return true;
        }

        public override void HandleItemDrag(string name, ItemBase draggedItem,
            DragAndDropManager.DragRemoveItem dragDelegate, SegmentEntity targetEntity)
        {
            QuantumOutputPortMachine port = targetEntity as QuantumOutputPortMachine;
            if (name != "outputitem" ||
                manager.mWindowLookup[name + "_icon"].GetComponent<UISprite>().spriteName != "empty")
                return;
            SetExemplar(WorldScript.mLocalPlayer, port, draggedItem);
            manager.RedrawWindow();
        }

        public override void OnClose(SegmentEntity targetEntity)
        {
            _itemSearch = false;
            ItemSearchWindow.TerminateSearchWindow();
        }

        public static bool SetExemplar(Player player, QuantumOutputPortMachine port, ItemBase exemplar)
        {
            port.SetExemplar(exemplar);
            port.MarkDirtyDelayed();
            if (!WorldScript.mbIsServer)
                NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumOutputPortWindowKey,
                    nameof(SetExemplar), null, exemplar, port, 0.0f);
            return true;
        }

        public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
        {
            QuantumOutputPortMachine target = nic.target as QuantumOutputPortMachine;
            switch (nic.command)
            {
                case "SetExemplar":
                    QuantumOutputPortWindow.SetExemplar(player, target, nic.itemContext);
                    break;
            }

            NetworkInterfaceResponse interfaceResponse = new NetworkInterfaceResponse();
            interfaceResponse.entity = (SegmentEntity) target;
            interfaceResponse.inventory = player.mInventory;
            return interfaceResponse;
        }
    }
}
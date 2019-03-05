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
        private const string ChooseItemButton = "chooseItemButton";
        private const string ToggleStatusButton = "toggleStatusButton";
        private const string OutputItemIcon = "outputItemIcon";
        private const string OutputTitleLabel = "outputTitleLabel";
        private const string OutputStatusLabel = "outputStatusLabel";
        private const int ButtonsRowStart = 40;
        private const int TextHeight = 30;
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
                    manager.AddIcon(OutputItemIcon, "empty", Color.white, 10, 0);
                    manager.AddBigLabel(OutputTitleLabel, PersistentSettings.GetString("Choose_output"), Color.white, 70, 0);
                    manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, OutputStatusLabel,
                        string.Empty, Color.white, false, 10, TextHeight * 2);
                    manager.AddButton(ChooseItemButton, PersistentSettings.GetString("Choose_Item"), 100,
                        ButtonsRowStart + QuantumStorageModSettings.ButtonHeight * 2);
                    manager.AddButton(ToggleStatusButton, "Toggle item output", 100,
                        ButtonsRowStart + QuantumStorageModSettings.ButtonHeight * 3);
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
            QuantumOutputPortMachine outputPort = targetEntity as QuantumOutputPortMachine;
            if (outputPort == null)
            {
                GenericMachinePanelScript.instance.Hide();
                UIManager.RemoveUIRules("Machine");
                return;
            }

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
                ItemBase exemplar = outputPort.Exemplar;
                if (exemplar != null)
                {
                    string itemName = ItemManager.GetItemName(exemplar);
                    manager.UpdateIcon(OutputItemIcon, ItemManager.GetItemIcon(exemplar), Color.white);
                    manager.UpdateLabel(OutputTitleLabel, itemName, Color.white);
                }

                manager.UpdateLabel(OutputStatusLabel, outputPort.Enabled ? "Output enabled" : "Output disabled", Color.white);
            }

            Dirty = false;
        }

        public override bool ButtonClicked(string name, SegmentEntity targetEntity)
        {
            utils.LogUtils.LogDebug("QuantumOutputPortWindow", "ButtonClicked name: " + name);
            QuantumOutputPortMachine outputPort = targetEntity as QuantumOutputPortMachine;
            if (name == OutputItemIcon)
            {
                utils.LogUtils.LogDebug("QuantumOutputPortWindow", "ButtonClicked outputitem");
                if (outputPort.Exemplar != null)
                {
                    utils.LogUtils.LogDebug("QuantumOutputPortWindow", "ButtonClicked outputitem - exemplar in outputPort not null");
                    SetExemplar(WorldScript.mLocalPlayer, outputPort, (ItemBase) null);
                    manager.RedrawWindow();
                }
                return true;
            }

            if (name == ChooseItemButton)
            {
                _itemSearch = true;
                ItemSearchWindow.SetupUIRules();
                Redraw(targetEntity);
                return true;
            }

            if (name == ToggleStatusButton)
            {
                ToggleStatus(WorldScript.mLocalPlayer, outputPort);
                manager.RedrawWindow();
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

            SetExemplar(WorldScript.mLocalPlayer, outputPort, selectedItem);
            return true;
        }

        public override bool ButtonRightClicked(string name, SegmentEntity targetEntity)
        {
            utils.LogUtils.LogDebug("QuantumOutputPortWindow", "ButtonClicked name: " + name);
            if (name != OutputItemIcon)
            { 
                return base.ButtonRightClicked(name, targetEntity);
            }

            QuantumOutputPortMachine outputPort = targetEntity as QuantumOutputPortMachine;

            if (name == OutputItemIcon)
            {
                QuantumOutputPortWindow.SetExemplar(WorldScript.mLocalPlayer, outputPort, (ItemBase) null);
                manager.RedrawWindow();
                return true;
            }

            _itemSearch = true;
            ItemSearchWindow.SetupUIRules();
            Redraw(targetEntity);
            return true;
        }

        public override void HandleItemDrag(string name, ItemBase draggedItem,
            DragAndDropManager.DragRemoveItem dragDelegate, SegmentEntity targetEntity)
        {
            QuantumOutputPortMachine outputPort = targetEntity as QuantumOutputPortMachine;
            if (name != OutputItemIcon ||
                manager.mWindowLookup[name + "_icon"].GetComponent<UISprite>().spriteName != "empty")
                return;
            SetExemplar(WorldScript.mLocalPlayer, outputPort, draggedItem);
            manager.RedrawWindow();
        }

        public override void OnClose(SegmentEntity targetEntity)
        {
            _itemSearch = false;
            ItemSearchWindow.TerminateSearchWindow();
        }

        public static bool SetExemplar(Player player, QuantumOutputPortMachine outputPort, ItemBase exemplar)
        {
            outputPort.SetExemplar(exemplar);
            outputPort.MarkDirtyDelayed();
            if (!WorldScript.mbIsServer)
                NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumOutputPortWindowKey,
                    nameof(SetExemplar), null, exemplar, outputPort, 0.0f);
            return true;
        }

        public static bool ToggleStatus(Player player, QuantumOutputPortMachine outputPort)
        {
            outputPort.ToggleStatus();
            if (!WorldScript.mbIsServer)
                NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumOutputPortWindowKey,
                    nameof(ToggleStatus), null, null, outputPort, 0.0f);
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
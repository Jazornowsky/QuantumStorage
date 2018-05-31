using System.Collections.Generic;
using Jazornowsky.QuantumStorage.model;
using UnityEngine;

namespace Jazornowsky.QuantumStorage
{
    internal class QuantumStorageControllerWindow : BaseMachineWindow
    {
        public const string LogName = "QuantumIoPortWindow";
        private const string StorageSizeLabel = "storageSize";
        private const string StatusLabel = "status";
        private const string InputStatusButton = "inputButton";
        private const string InputStatusLabel = "inputStatus";
        private const string OutputStatusButton = "outputButton";
        private const string OutputStatusLabel = "outputStatus";
        private const string InputRuleAddButton = "inputRuleAdd";
        private const string InputRuleRemoveButton = "inputRuleRemove";
        private const string InputRuleItemIcon = "inputRuleItemIcon";
        private const string InputRuleItemLabel = "inputRuleItemLabel";
        private const string InputRuleIncreaseButton = "inputRuleIncrease";
        private const string InputRuleReduceButton = "inputRuleReduce";
        private bool _itemSearch;
        public static bool Dirty;

        public override void SpawnWindow(SegmentEntity targetEntity)
        {
            if (!(targetEntity is QuantumStorageControllerMachine controller))
            {
                GenericMachinePanelScript.instance.Hide();
                UIManager.RemoveUIRules("Machine");
                return;
            }

            if (_itemSearch)
            {
                ItemSearchWindow.SpawnWindow(this);
                Dirty = true;
                return;
            }

            var textHeight = 30;
            var buttonHeight = 35;
            var itemWidth = 60;
            var itemHeight = 80;
            var buttonWidth = 120;
            var buttonRowStart = textHeight * 4 + buttonHeight;
            var itemRuleRowStart = buttonRowStart + buttonHeight * 2 + itemHeight;
            
            manager.SetTitle(QuantumStorageControllerMachine.MachineName);

            manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, StorageSizeLabel,
                string.Empty, Color.white,
                false, 10, textHeight);

            manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, StatusLabel,
                string.Empty, Color.white,
                false, 10, textHeight * 2);

            manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, InputStatusLabel,
                string.Empty, Color.white,
                false, 10, textHeight * 3);

            manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, OutputStatusLabel,
                string.Empty, Color.white,
                false, 10, textHeight * 4);

            manager.AddButton(InputStatusButton, "Toggle item input", 10, buttonRowStart);
            manager.AddButton(OutputStatusButton, "Toggle item output", 10, buttonRowStart + buttonHeight);
            manager.AddButton(InputRuleAddButton, "Add input item rule", 10, buttonRowStart + buttonHeight*2);

            for (int i = 0; i < controller.GetItemInputRules().Count; i++)
            {
                manager.AddIcon(InputRuleItemIcon + i, "empty", Color.white, 10, itemRuleRowStart + itemHeight * i);
                manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, InputRuleItemLabel + i, string.Empty, Color.white, false, 28, itemRuleRowStart + itemHeight * i + 17);
                manager.AddButton(InputRuleReduceButton + i, "-" + QuantumStorageControllerMachine.DefaultInputRuleStep, itemWidth + 10, itemRuleRowStart + itemHeight * i);
                manager.AddButton(InputRuleIncreaseButton + i, "+" + QuantumStorageControllerMachine.DefaultInputRuleStep, buttonWidth + itemWidth + 10, itemRuleRowStart + itemHeight * i);
                manager.AddButton(InputRuleRemoveButton + i, "X", buttonWidth / 2 + itemWidth + 10, itemRuleRowStart + itemHeight * i + buttonHeight);
            }

            Dirty = true;
        }

        public override void UpdateMachine(SegmentEntity targetEntity)
        {
            if (!(targetEntity is QuantumStorageControllerMachine quantumStorageController))
            {
                GenericMachinePanelScript.instance.Hide();
                UIManager.RemoveUIRules("Machine");
                return;
            }

            if (_itemSearch)
            {
                if (ItemSearchWindow.UpdateMachine(this))
                {
                    Dirty = true;
                }
            }
            else
            {
                WindowUpdate(quantumStorageController);
            }

        }

        private void WindowUpdate(QuantumStorageControllerMachine controller)
        {
            if (controller.HasPower())
            {
                manager.UpdateLabel(StorageSizeLabel,
                    controller.GetItems().GetItemCount() + "/" +
                    controller.GetMaxCapacity(),
                    Color.white);

                manager.UpdateLabel(InputStatusLabel,
                    controller.IsInputEnabled() ? "Input enabled" : "Input disabled", Color.white);

                manager.UpdateLabel(OutputStatusLabel,
                    controller.IsOutputEnabled() ? "Output enabled" : "Output disabled", Color.white);

                manager.UpdateLabel(StatusLabel, "POWER OK", Color.white);
            }

            if (!controller.HasPower())
            {
                manager.UpdateLabel(StorageSizeLabel, string.Empty, Color.white);
                manager.UpdateLabel(InputStatusLabel, string.Empty, Color.white);
                manager.UpdateLabel(OutputStatusLabel, string.Empty, Color.white);
                manager.UpdateIcon(InputRuleItemIcon, "empty", Color.white);
                manager.UpdateLabel(StatusLabel, "LOW POWER", Color.red);
            }

            if (controller.HasPower() && !controller.IsOperating())
            {
                manager.UpdateLabel(StorageSizeLabel, string.Empty, Color.white);
                manager.UpdateLabel(InputStatusLabel, string.Empty, Color.white);
                manager.UpdateLabel(OutputStatusLabel, string.Empty, Color.white);
                manager.UpdateIcon(InputRuleItemIcon, "empty", Color.white);
                manager.UpdateLabel(StatusLabel, "ERROR - ANOTHER CONTROLLER DETECTED", Color.red);
            }

            for (int i = 0; i < controller.GetItemInputRules().Count; i++)
            {
                if (!controller.IsOperating())
                {
                    manager.UpdateIcon(InputRuleItemIcon + i, "empty", Color.white);
                    manager.UpdateLabel(InputRuleItemLabel + i, string.Empty, Color.white);
                }
                else
                {
                    var itemIcon = ItemManager.GetItemIcon(controller.GetItemInputRules()[i].Item);
                    var limit = controller.GetItemInputRules()[i].MaxInput;
                    manager.UpdateIcon(InputRuleItemIcon + i, itemIcon, Color.white);
                    manager.UpdateLabel(InputRuleItemLabel + i, "" + limit, Color.white);
                }
            }

            Dirty = false;
        }

        public override bool ButtonClicked(string name, SegmentEntity targetEntity)
        {
            var controller = targetEntity as QuantumStorageControllerMachine;

            if (controller == null || !controller.IsOperating()) return false;

            if (name.Equals(InputStatusButton))
            {
                controller.ToggleInput();

                if (!WorldScript.mbIsServer)
                {
                    NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumStorageControllerWindowKey,
                        "ToggleInput",
                        null, null, controller, 0.0f);
                }
                Dirty = true;
                return true;
            }

            if (name.Equals(OutputStatusButton))
            {
                controller.ToggleOutput();

                if (!WorldScript.mbIsServer)
                {
                    NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumStorageControllerWindowKey,
                        "ToggleOutput",
                        null, null, controller, 0.0f);
                }
                Dirty = true;
                return true;
            }

            if (name.Contains(InputRuleIncreaseButton))
            {
                int.TryParse(name.Replace(InputRuleIncreaseButton, string.Empty), out var itemSlot);

                if (itemSlot == -1)
                {
                    return false;
                }

                controller.IncreaseItemInputRuleLimit(controller.GetItemInputRules()[itemSlot]);
                if (!WorldScript.mbIsServer)
                {
                    NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumStorageControllerWindowKey,
                        "IncreaseItemRule",
                        null, controller.GetItemInputRules()[itemSlot].Item, controller, 0.0f);
                }
                Dirty = true;
                return true;
            }

            if (name.Contains(InputRuleReduceButton))
            {
                int.TryParse(name.Replace(InputRuleReduceButton, string.Empty), out var itemSlot);

                if (itemSlot == -1)
                {
                    return false;
                }

                controller.ReduceItemInputRuleLimit(controller.GetItemInputRules()[itemSlot]);
                if (!WorldScript.mbIsServer)
                {
                    NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumStorageControllerWindowKey,
                        "ReduceItemRule",
                        null, controller.GetItemInputRules()[itemSlot].Item, controller, 0.0f);
                }
                Dirty = true;
                return true;
            }

            if (name.Contains(InputRuleRemoveButton))
            {
                int.TryParse(name.Replace(InputRuleRemoveButton, string.Empty), out var itemSlot);

                if (itemSlot == -1)
                {
                    return false;
                }

                controller.RemoveItemInputRule(controller.GetItemInputRules()[itemSlot]);

                if (!WorldScript.mbIsServer)
                {
                    NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumStorageControllerWindowKey,
                        "RemoveItemRule",
                        controller.GetItemInputRules()[itemSlot].MaxInput.ToString(), controller.GetItemInputRules()[itemSlot].Item, controller, 0.0f);
                }
                Redraw(targetEntity);
            }

            if (name.Equals(InputRuleAddButton))
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
            else
            {
                var itemInputRule = new ItemInputRule();
                itemInputRule.MaxInput = QuantumStorageControllerMachine.DefaultInputRuleStep;
                itemInputRule.Item = selectedItem;
                controller.AddItemInputRule(itemInputRule);
                if (!WorldScript.mbIsServer)
                {
                    NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumStorageControllerWindowKey,
                        "AddItemRule",
                        itemInputRule.MaxInput.ToString(), selectedItem, controller, 0.0f);
                }
                Redraw(targetEntity);
            }

            return false;
        }

        public override bool ButtonRightClicked(string name, SegmentEntity targetEntity)
        {
            if (name != InputRuleAddButton)
                return base.ButtonRightClicked(name, targetEntity);
            _itemSearch = true;
            ItemSearchWindow.SetupUIRules();
            Redraw(targetEntity);
            return true;
        }

        public override void ButtonEnter(string name, SegmentEntity targetEntity)
        {
            var quantumStorageController = targetEntity as QuantumStorageControllerMachine;

            if (quantumStorageController == null || !quantumStorageController.HasPower()) return;
        }

        /*public static bool TakeItems(Player player, QuantumStorageControllerMachine quantumStorageController,
            ItemBase item)
        {
            if (!player.mbIsLocalPlayer)
            {
                NetworkRedraw = true;
            }

            if (quantumStorageController.GetItems().Count > 0)
            {
                quantumStorageController.TakeItem(ref item);
                if (item != null)
                {
                    Debug.Log((object) ("Removing Item from StorageHopper for " + player.mUserName));
                    if (!player.mInventory.AddItem(itemBase))
                    {
                        if (!quantumStorageController.AddItem(itemBase))
                            ItemManager.instance.DropItem(itemBase, player.mnWorldX, player.mnWorldY, player.mnWorldZ,
                                Vector3.zero);
                        return false;
                    }

                    if (player.mbIsLocalPlayer)
                    {
                        Color lCol = Color.green;
                        if (itemBase.mType == ItemType.ItemCubeStack)
                        {
                            ItemCubeStack itemCubeStack = itemBase as ItemCubeStack;
                            if (CubeHelper.IsGarbage(itemCubeStack.mCubeType))
                                lCol = Color.red;
                            if (CubeHelper.IsSmeltableOre(itemCubeStack.mCubeType))
                                lCol = Color.green;
                        }

                        if (itemBase.mType == ItemType.ItemStack)
                            lCol = Color.cyan;
                        if (itemBase.mType == ItemType.ItemSingle)
                            lCol = Color.white;
                        if (itemBase.mType == ItemType.ItemCharge)
                            lCol = Color.magenta;
                        if (itemBase.mType == ItemType.ItemDurability)
                            lCol = Color.yellow;
                        if (itemBase.mType == ItemType.ItemLocation)
                            lCol = Color.gray;
                        FloatingCombatTextManager.instance.QueueText(quantumStorageController.mnX,
                            quantumStorageController.mnY + 1L, quantumStorageController.mnZ, 1f,
                            player.GetItemName(itemBase), lCol, 1.5f, 64f);
                    }

                    player.mInventory.VerifySuitUpgrades();
                    if (!WorldScript.mbIsServer)
                        NetworkManager.instance.SendInterfaceCommand(nameof(StorageHopperWindowNew), nameof(TakeItems),
                            (string) null, itemBase, (SegmentEntity) quantumStorageController, 0.0f);
                    return true;
                }
            }

            return false;
        }*/

        public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
        {
            if (!(nic.target is QuantumStorageControllerMachine target)) return null;

            var command = nic.command;
            if (command != null)
            {
                var dictionary = new Dictionary<string, int>(2);
                dictionary.Add("ToggleInput", 1);
                dictionary.Add("ToggleOutput", 2);
                dictionary.Add("AddItemRule", 3);
                dictionary.Add("RemoveItemRule", 4);
                dictionary.Add("ReduceItemRule", 5);
                dictionary.Add("IncreaseItemRule", 6);

                if (dictionary.TryGetValue(command, out var num))
                    switch (num)
                    {
                        case 1:
                            target.ToggleInput();
                            break;
                        case 2:
                            target.ToggleOutput();
                            break;
                        case 3:
                            var itemInputRule = new ItemInputRule();
                            itemInputRule.Item = nic.itemContext;
                            itemInputRule.MaxInput = int.Parse(nic.payload);
                            target.AddItemInputRule(itemInputRule);
                            break;
                        case 4:
                            var itemInputRuleToRemove = new ItemInputRule();
                            itemInputRuleToRemove.Item = nic.itemContext;
                            itemInputRuleToRemove.MaxInput = int.Parse(nic.payload);
                            target.RemoveItemInputRule(itemInputRuleToRemove);
                            break;
                        case 5:
                            var itemInputRuleToReduce = new ItemInputRule();
                            itemInputRuleToReduce.Item = nic.itemContext;
                            target.ReduceItemInputRuleLimit(itemInputRuleToReduce);
                            break;
                        case 6:
                            var itemInputRuleToIncrease = new ItemInputRule();
                            itemInputRuleToIncrease.Item = nic.itemContext;
                            target.IncreaseItemInputRuleLimit(itemInputRuleToIncrease);
                            break;

                    }
            }

            var interfaceResponse = new NetworkInterfaceResponse();
            interfaceResponse.entity = target;
            interfaceResponse.inventory = player.mInventory;
            return interfaceResponse;
        }
    }
}
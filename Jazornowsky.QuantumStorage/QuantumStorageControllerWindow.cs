using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Handbook;
using Jazornowsky.QuantumStorage.utils;
using UnityEngine;

namespace Jazornowsky.QuantumStorage
{
    class QuantumStorageControllerWindow : BaseMachineWindow
    {
        public const String LogName = "QuantumIoPortWindow";
        private const string StorageSizeLabel = "storageSize";
        private const string StatusLabel = "status";
        private const string InputStatusButton = "inputButton";
        private const string InputStatusLabel = "inputStatus";
        private const string OutputStatusButton = "outputButton";
        private const string OutputStatusLabel = "outputStatus";
        public static bool Dirty;

        public int SlotCount = 0;

        public override void SpawnWindow(SegmentEntity targetEntity)
        {
            if (!(targetEntity is QuantumStorageControllerMachine quantumStorageController))
            {
                GenericMachinePanelScript.instance.Hide();
                UIManager.RemoveUIRules("Machine");
                return;
            }
            
            int textHeight = 30;
            int buttonRowStart = textHeight * 5;

            List<ItemBase> items = quantumStorageController.GetItems();
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
            manager.AddButton(OutputStatusButton, "Toggle item output", 10, buttonRowStart + textHeight);

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
            
            WindowUpdate(quantumStorageController);
        }

        private void WindowUpdate(QuantumStorageControllerMachine quantumStorageController)
        {
            if (quantumStorageController.HasPower())
            {
                manager.UpdateLabel(StorageSizeLabel,
                    quantumStorageController.GetItems().GetItemCount() + "/" +
                    quantumStorageController.GetMaxCapacity(),
                    Color.white);

                manager.UpdateLabel(StatusLabel, "POWER OK", Color.white);
            }

            if (!quantumStorageController.HasPower())
            {
                manager.UpdateLabel(StorageSizeLabel, String.Empty, Color.white);
                manager.UpdateLabel(StatusLabel, "LOW POWER", Color.red);
            }

            if (!quantumStorageController.IsOperating())
            {
                manager.UpdateLabel(StorageSizeLabel, String.Empty, Color.white);
                manager.UpdateLabel(StatusLabel, "ERROR - ANOTHER CONTROLLER DETECTED", Color.red);
            }

            manager.UpdateLabel(InputStatusLabel,
                quantumStorageController.IsInputEnabled() ? "Input enabled" : "Input disabled", Color.white);

            manager.UpdateLabel(OutputStatusLabel,
                quantumStorageController.IsOutputEnabled() ? "Output enabled" : "Output disabled", Color.white);

            Dirty = false;
        }

        public override bool ButtonClicked(string name, SegmentEntity targetEntity)
        {
            var controller = targetEntity as QuantumStorageControllerMachine;

            if (controller == null || !controller.IsOperating())
            {
                return false;
            }

            if (name.Equals(InputStatusButton))
            {
                controller.ToggleInput();
                return true;
            }

            if (name.Equals(OutputStatusButton))
            {
                controller.ToggleOutput();
                return true;
            }

            return false;
        }

        public override void ButtonEnter(string name, SegmentEntity targetEntity)
        {
            QuantumStorageControllerMachine quantumStorageController = targetEntity as QuantumStorageControllerMachine;

            if (quantumStorageController == null || !quantumStorageController.HasPower())
            {
                return;
            }
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
            if (!(nic.target is QuantumStorageControllerMachine target))
            {
                return null;
            }

            string command = nic.command;
            if (command != null)
            {
                Dictionary<string, int> dictionary = new Dictionary<string, int>(2);
                dictionary.Add("TakeItem", 1);
                dictionary.Add("StoreItem", 2);

                if (dictionary.TryGetValue(command, out var num))
                {
                    switch (num)
                    {
                        case 1:
                            break;
                        case 2:
                            break;
                    }
                }
            }

            NetworkInterfaceResponse interfaceResponse = new NetworkInterfaceResponse();
            interfaceResponse.entity = target;
            interfaceResponse.inventory = player.mInventory;
            return interfaceResponse;
        }
    }
}
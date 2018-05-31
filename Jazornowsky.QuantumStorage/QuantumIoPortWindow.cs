using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Jazornowsky.QuantumStorage
{
    internal class QuantumIoPortWindow : BaseMachineWindow
    {
        public const string LogName = "QuantumIoPortWindow";
        private const string StorageSizeLabel = "storageSize";
        private const string StatusLabel = "status";
        public static bool Dirty;
        public static bool NetworkRedraw;

        public int SlotCount;

        public override void SpawnWindow(SegmentEntity targetEntity)
        {
            if (!(targetEntity is QuantumIoPortMachine quantumIoPort))
            {
                GenericMachinePanelScript.instance.Hide();
                UIManager.RemoveUIRules("Machine");
                return;
            }

            var itemWidth = 60;
            var itemHeight = 60;
            var textHeight = 30;
            var itemRowStart = textHeight * 3;

            var items = quantumIoPort.GetController().GetItems();
            manager.SetTitle(QuantumIoPortMachine.MachineName);

            manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, StorageSizeLabel,
                string.Empty, Color.white,
                false, 10, textHeight);

            manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, StatusLabel,
                string.Empty, Color.white,
                false, 10, textHeight * 2);

            SlotCount = 0;
            for (var index = 0; index < items.Count(); index++)
            {
                var line = index / 5;
                var column = index % 5;
                manager.AddIcon("iconItem" + index, "empty", Color.white, column * itemWidth + 10,
                    line * itemHeight + itemRowStart + 10);
                manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "labelItem" + index,
                    string.Empty, Color.white, false, column * itemWidth + 28, line * itemHeight + itemRowStart + 17);
                SlotCount++;
            }

            {
                var line = items.Count() / 5;
                var column = items.Count() % 5;
                manager.AddIcon("iconItem" + items.Count, "empty", Color.white, column * itemWidth + 10,
                    line * itemHeight + itemRowStart + 10);
                manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth,
                    "labelItem" + items.Count,
                    string.Empty, Color.white, false, column * itemWidth + 28, line * itemHeight + itemRowStart + 17);
            }
            Dirty = true;
        }

        public override void UpdateMachine(SegmentEntity targetEntity)
        {
            if (!(targetEntity is QuantumIoPortMachine quantumIoPort))
            {
                GenericMachinePanelScript.instance.Hide();
                UIManager.RemoveUIRules("Machine");
                return;
            }

            if (quantumIoPort.GetController().GetItems().Count != SlotCount)
                Redraw(targetEntity);
            else
                WindowUpdate(quantumIoPort.GetController());
        }

        private void WindowUpdate(QuantumStorageControllerMachine quantumStorageController)
        {
            var items = quantumStorageController.GetItems();

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
                manager.UpdateLabel(StorageSizeLabel, string.Empty, Color.white);
                manager.UpdateLabel(StatusLabel, "LOW POWER", Color.red);
            }

            if (!quantumStorageController.IsOperating())
            {
                manager.UpdateLabel(StorageSizeLabel, string.Empty, Color.white);
                manager.UpdateLabel(StatusLabel, "ERROR - ANOTHER CONTROLLER DETECTED", Color.red);
            }

            for (var index = 0; index < items.Count; index++)
            {
                if (!quantumStorageController.IsOperating())
                {
                    manager.UpdateIcon("iconItem" + index, "empty", Color.white);
                    manager.UpdateLabel("labelItem" + index, string.Empty, Color.white);
                    manager.UpdateIcon("iconItem" + items.Count, "empty", Color.white);
                    manager.UpdateLabel("labelItem" + items.Count, string.Empty, Color.white);
                    continue;
                }

                manager.UpdateIcon("iconItem" + items.Count, "empty", Color.white);

                var itemBase = items[index];
                if (itemBase != null)
                {
                    var itemIcon = ItemManager.GetItemIcon(itemBase);
                    var currentStackSize = ItemManager.GetCurrentStackSize(itemBase);

                    var label = currentStackSize != 100
                        ? (currentStackSize >= 10
                            ? " " + currentStackSize
                            : "   " + currentStackSize)
                        : currentStackSize.ToString();
                    manager.UpdateIcon("iconItem" + index, itemIcon, Color.white);
                    manager.UpdateLabel("labelItem" + index, label, Color.white);
                }
            }

            Dirty = false;
        }

        public override void HandleItemDrag(string name, ItemBase draggedItem,
            DragAndDropManager.DragRemoveItem dragDelegate, SegmentEntity targetEntity)
        {
            var quantumIoPort = targetEntity as QuantumIoPortMachine;
            var controller = quantumIoPort.GetController();
            if (controller == null || !controller.IsOperating() || controller.IsFull()) return;

            var itemToStore = ItemManager.CloneItem(draggedItem);
            var currentStackSize = ItemManager.GetCurrentStackSize(itemToStore);

            if (controller.GetRemainigCapacity() < currentStackSize)
                ItemManager.SetItemCount(itemToStore, controller.GetRemainigCapacity());

            StoreItem(WorldScript.mLocalPlayer, controller, itemToStore);
            AudioHUDManager.instance.OrePickup();
            WorldScript.mLocalPlayer.mInventory.VerifySuitUpgrades();
            WorldScript.mLocalPlayer.mInventory.MarkEverythingDirty();
            InventoryPanelScript.MarkDirty();
            controller.Dirty = true;
            NetworkRedraw = true;
            UIManager.ForceNGUIUpdate = 0.1f;
        }

        public static void StoreItem(Player player, QuantumStorageControllerMachine quantumStorageController,
            ItemBase itemToStore)
        {
            var itemToStoreCopy = itemToStore.NewInstance();
            if (player == WorldScript.mLocalPlayer &&
                !WorldScript.mLocalPlayer.mInventory.RemoveItemByExample(itemToStore, true))
                return;

            quantumStorageController.AddItem(ref itemToStore);
            quantumStorageController.Dirty = true;

            if (itemToStore != null && itemToStore.GetAmount() > 0)
                if (player == WorldScript.mLocalPlayer)
                    WorldScript.mLocalPlayer.mInventory.AddItem(itemToStore);


            if (!WorldScript.mbIsServer)
            {
                NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumIoPortWindowKey,
                    "StoreItem",
                    null, itemToStoreCopy, quantumStorageController, 0.0f);
            }
        }

        public override bool ButtonClicked(string name, SegmentEntity targetEntity)
        {
            var quantumIoPort = targetEntity as QuantumIoPortMachine;
            var controller = quantumIoPort.GetController();
            if (controller == null || !controller.IsOperating()) return false;

            if (name.Contains("iconItem"))
            {
                int.TryParse(name.Replace("iconItem", string.Empty), out var itemSlot);
                if (itemSlot > -1 && itemSlot < SlotCount)
                {
                    var amount = 100;
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        amount = 10;
                    else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) amount = 1;

                    var itemBase = ItemManager.CloneItem(controller.GetItem(itemSlot));
                    if (amount < ItemManager.GetCurrentStackSize(itemBase))
                        ItemManager.SetItemCount(itemBase, amount);
                    else
                        amount = ItemManager.GetCurrentStackSize(itemBase);

                    if (TakeItem(WorldScript.mLocalPlayer, controller, itemBase))
                    {
                        UIManager.ForceNGUIUpdate = 0.1f;
                        AudioHUDManager.instance.OrePickup();
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TakeItem(Player player, QuantumStorageControllerMachine quantumStorageController,
            ItemBase itemBase)
        {
            var itemBaseCopy = itemBase.NewInstance();
            var amount = itemBase.GetAmount();
            quantumStorageController.TakeItem(ref itemBase);

            itemBase.SetAmount(amount - itemBase.GetAmount());

            if (!player.mInventory.AddItem(itemBase))
            {
                quantumStorageController.AddItem(ref itemBase);
                if (itemBase.GetAmount() > 0)
                    ItemManager.instance.DropItem(itemBase,
                        WorldScript.mLocalPlayer.mnWorldX, WorldScript.mLocalPlayer.mnWorldY,
                        WorldScript.mLocalPlayer.mnWorldZ,
                        Vector3.zero);

                return false;
            }

            quantumStorageController.Dirty = true;

            if (!WorldScript.mbIsServer)
                NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumIoPortWindowKey,
                    "TakeItem",
                    null, itemBaseCopy, quantumStorageController, 0.0f);

            return true;
        }

        public override void ButtonEnter(string name, SegmentEntity targetEntity)
        {
            var quantumIoPort = targetEntity as QuantumIoPortMachine;
            var controller = quantumIoPort.GetController();
            if (controller == null || !controller.IsOperating()) return;

            if (name.Contains("iconItem"))
            {
                int.TryParse(name.Replace("iconItem", string.Empty), out var slot);
                if (slot > -1 && slot < SlotCount)
                {
                    var item = controller.GetItem(slot);


                    if (item == null) return;

                    if (HotBarManager.mbInited)
                    {
                        HotBarManager.SetCurrentBlockLabel(ItemManager.GetItemName(item));
                    }
                    else
                    {
                        if (!SurvivalHotBarManager.mbInited) return;

                        var name1 = WorldScript.mLocalPlayer.mResearch.IsKnown(item)
                            ? ItemManager.GetItemName(item)
                            : PersistentSettings.GetString("Unknown_Material");
                        var currentStackSize = ItemManager.GetCurrentStackSize(item);
                        if (currentStackSize > 1)
                            SurvivalHotBarManager.SetCurrentBlockLabel(string.Format("{0} {1}",
                                currentStackSize, name1));
                        else
                            SurvivalHotBarManager.SetCurrentBlockLabel(name1);
                    }
                }
            }
        }

        public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand nic)
        {
            if (!(nic.target is QuantumStorageControllerMachine target)) return null;

            var command = nic.command;
            if (command != null)
            {
                var dictionary = new Dictionary<string, int>(2);
                dictionary.Add("TakeItem", 1);
                dictionary.Add("StoreItem", 2);

                if (dictionary.TryGetValue(command, out var num))
                    switch (num)
                    {
                        case 1:
                            TakeItem(player, target, nic.itemContext);
                            break;
                        case 2:
                            StoreItem(player, target, nic.itemContext);
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
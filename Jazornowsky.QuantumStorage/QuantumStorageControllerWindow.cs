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
        public const String LogName = "QuantumStorageControllerWindow";
        public static bool Dirty;
        public static bool NetworkRedraw;

        public int SlotCount = 0;

        public override void SpawnWindow(SegmentEntity targetEntity)
        {
            if (!(targetEntity is QuantumStorageControllerMachine quantumStorageController))
            {
                GenericMachinePanelScript.instance.Hide();
                UIManager.RemoveUIRules("Machine");
                return;
            }

            int itemWidth = 60;
            int itemHeight = 60;
            int textHeight = 30;
            int itemRowStart = itemHeight * 3;

            List<ItemBase> items = quantumStorageController.GetItems();
            manager.SetTitle(QuantumStorageControllerMachine.MachineName);

            this.manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, LogName + "storageSize",
                string.Empty, Color.white,
                false, 10, textHeight);

            this.manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, LogName + "powerStatus",
                string.Empty, Color.white,
                false, 10, textHeight * 2);

            SlotCount = 0;
            for (int index = 0; index < items.Count(); index++)
            {
                int line = index / 5;
                int column = index % 5;
                this.manager.AddIcon("iconItem" + index, "empty", Color.white, column * itemWidth + 10,
                    line * itemHeight + itemRowStart + 10);
                this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, "labelItem" + index,
                    string.Empty, Color.white, false, column * itemWidth + 28, line * itemHeight + itemRowStart + 17);
                SlotCount++;
            }

            {
                int line = items.Count() / 5;
                int column = items.Count() % 5;
                this.manager.AddIcon("iconItem" + items.Count, "empty", Color.white, column * itemWidth + 10,
                    line * itemHeight + itemRowStart + 10);
                this.manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth,
                    "labelItem" + items.Count,
                    string.Empty, Color.white, false, column * itemWidth + 28, line * itemHeight + itemRowStart + 17);
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

            if (quantumStorageController.GetItems().Count != SlotCount)
            {
                Redraw(targetEntity);
            }
            else
            {
                WindowUpdate(quantumStorageController);
            }
        }

        private void WindowUpdate(QuantumStorageControllerMachine quantumStorageController)
        {
            List<ItemBase> items = quantumStorageController.GetItems();

            if (quantumStorageController.HasPower())
            {
                this.manager.UpdateLabel(LogName + "storageSize",
                    quantumStorageController.GetItems().GetItemCount() + "/" +
                    quantumStorageController.GetMaxCapacity(),
                    Color.white);

                this.manager.UpdateLabel(LogName + "powerStatus", "POWER OK", Color.white);
            }
            else
            {
                this.manager.UpdateLabel(LogName + "storageSize", String.Empty, Color.white);

                this.manager.UpdateLabel(LogName + "powerStatus", "LOW POWER", Color.red);
            }

            for (int index = 0; index < items.Count; index++)
            {
                if (!quantumStorageController.HasPower())
                {
                    this.manager.UpdateIcon("iconItem" + index, "empty", Color.white);
                    this.manager.UpdateLabel("labelItem" + index, String.Empty, Color.white);
                    this.manager.UpdateIcon("iconItem" + items.Count, "empty", Color.white);
                    this.manager.UpdateLabel("labelItem" + items.Count, String.Empty, Color.white);
                    continue;
                }
                else
                {
                    this.manager.UpdateIcon("iconItem" + items.Count, "empty", Color.white);
                }

                ItemBase itemBase = items[index];
                if (itemBase != null)
                {
                    string itemIcon = ItemManager.GetItemIcon(itemBase);
                    int currentStackSize = ItemManager.GetCurrentStackSize(itemBase);

                    string label = currentStackSize != 100
                        ? (currentStackSize >= 10
                            ? " " + currentStackSize.ToString()
                            : "   " + currentStackSize.ToString())
                        : currentStackSize.ToString();
                    this.manager.UpdateIcon("iconItem" + index, itemIcon, Color.white);
                    this.manager.UpdateLabel("labelItem" + index, label, Color.white);
                }
            }

            Dirty = false;
        }

        public override void HandleItemDrag(string name, ItemBase draggedItem,
            DragAndDropManager.DragRemoveItem dragDelegate, SegmentEntity targetEntity)
        {
            QuantumStorageControllerMachine quantumStorageController = targetEntity as QuantumStorageControllerMachine;
            if (!quantumStorageController.HasPower() || quantumStorageController.IsFull())
            {
                return;
            }

            ItemBase itemToStore = ItemManager.CloneItem(draggedItem);
            int currentStackSize = ItemManager.GetCurrentStackSize(itemToStore);

            if (quantumStorageController.GetRemainigCapacity() < currentStackSize)
            {
                ItemManager.SetItemCount(itemToStore, quantumStorageController.GetRemainigCapacity());
            }

            StoreItem(WorldScript.mLocalPlayer, quantumStorageController, itemToStore);
            WorldScript.mLocalPlayer.mInventory.VerifySuitUpgrades();
            WorldScript.mLocalPlayer.mInventory.MarkEverythingDirty();
            InventoryPanelScript.MarkDirty();
            quantumStorageController.Dirty = true;
            NetworkRedraw = true;
            UIManager.ForceNGUIUpdate = 0.1f;
        }

        public static void StoreItem(Player player, QuantumStorageControllerMachine quantumStorageController,
            ItemBase itemToStore)
        {
            var itemToStoreCopy = itemToStore.NewInstance();
            if (player == WorldScript.mLocalPlayer &&
                !WorldScript.mLocalPlayer.mInventory.RemoveItemByExample(itemToStore, true))
            {
                Debug.Log(("Player " + player.mUserName + " doesnt have " + itemToStore));
                return;
            }

            quantumStorageController.AddItem(ref itemToStore);

            if (itemToStore != null && itemToStore.GetAmount() > 0)
            {
                Debug.LogWarning("Bad thing that used to be unhandled! Thread interaccess probably caused this to screw up!");
                if (player == WorldScript.mLocalPlayer)
                {
                    WorldScript.mLocalPlayer.mInventory.AddItem(itemToStore);
                }
            }

            if (player == WorldScript.mLocalPlayer)
            {
                player.mInventory.MarkEverythingDirty();
                quantumStorageController.Dirty = true;
                UIManager.ForceNGUIUpdate = 0.1f;
                AudioHUDManager.instance.OrePickup();
                NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumStorageControllerWindowKey,
                    "StoreItem",
                    null, itemToStoreCopy, quantumStorageController, 0.0f);
            }
        }

        public override bool ButtonClicked(string name, SegmentEntity targetEntity)
        {
            var quantumStorageController = targetEntity as QuantumStorageControllerMachine;
            if (name.Contains("iconItem"))
            {
                int.TryParse(name.Replace("iconItem", string.Empty), out var itemSlot);
                if (itemSlot > -1 && itemSlot < this.SlotCount)
                {
                    int amount = 100;
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        amount = 10;
                    }
                    else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        amount = 1;
                    }

                    ItemBase itemBase = ItemManager.CloneItem(quantumStorageController.GetItem(itemSlot));
                    if (amount < ItemManager.GetCurrentStackSize(itemBase))
                    {
                        ItemManager.SetItemCount(itemBase, amount);
                    }
                    else
                    {
                        amount = ItemManager.GetCurrentStackSize(itemBase);
                    }

                    return TakeItem(WorldScript.mLocalPlayer, quantumStorageController, itemBase);
                }
            }

            return false;
        }

        public static bool TakeItem(Player player, QuantumStorageControllerMachine quantumStorageController,
            ItemBase itemBase)
        {
            var itemBaseCopy = itemBase.NewInstance();
            int amount = itemBase.GetAmount();
            LogUtils.LogDebug(LogName, "Trying to take item from storage: " + itemBase.GetDisplayString());
            LogUtils.LogDebug(LogName, "Item amount to take: " + itemBase.GetAmount());
            quantumStorageController.TakeItem(ref itemBase);
            LogUtils.LogDebug(LogName, "Item amount left after: " + itemBase.GetAmount());

            itemBase.SetAmount(amount - itemBase.GetAmount());

            if (!player.mInventory.AddItem(itemBase))
            {
                quantumStorageController.AddItem(ref itemBase);
                if (itemBase.GetAmount() > 0)
                {
                    ItemManager.instance.DropItem(itemBase,
                        WorldScript.mLocalPlayer.mnWorldX, WorldScript.mLocalPlayer.mnWorldY,
                        WorldScript.mLocalPlayer.mnWorldZ,
                        Vector3.zero);
                }

                return false;
            }

            quantumStorageController.Dirty = true;
            UIManager.ForceNGUIUpdate = 0.1f;
            AudioHUDManager.instance.OrePickup();

            if (player == WorldScript.mLocalPlayer)
            {
                NetworkManager.instance.SendInterfaceCommand(QuantumStorageMod.QuantumStorageControllerWindowKey,
                    "TakeItem",
                    null, itemBaseCopy, quantumStorageController, 0.0f);
            }

            return true;
        }

        public override void ButtonEnter(string name, SegmentEntity targetEntity)
        {
            QuantumStorageControllerMachine quantumStorageController = targetEntity as QuantumStorageControllerMachine;

            if (quantumStorageController == null || !quantumStorageController.HasPower())
            {
                return;
            }

            if (name.Contains("iconItem"))
            {
                int.TryParse(name.Replace("iconItem", string.Empty), out var slot);
                if (slot > -1 && slot < this.SlotCount)
                {
                    var item = quantumStorageController.GetItem(slot);


                    if (item == null)
                    {
                        return;
                    }

                    if (HotBarManager.mbInited)
                    {
                        HotBarManager.SetCurrentBlockLabel(ItemManager.GetItemName(item));
                    }
                    else
                    {
                        if (!SurvivalHotBarManager.mbInited)
                        {
                            return;
                        }

                        string name1 = WorldScript.mLocalPlayer.mResearch.IsKnown(item)
                            ? ItemManager.GetItemName(item)
                            : PersistentSettings.GetString("Unknown_Material");
                        int currentStackSize = ItemManager.GetCurrentStackSize(item);
                        if (currentStackSize > 1)
                        {
                            SurvivalHotBarManager.SetCurrentBlockLabel(string.Format("{0} {1}",
                                (object) currentStackSize, (object) name1));
                        }
                        else
                        {
                            SurvivalHotBarManager.SetCurrentBlockLabel(name1);
                        }
                    }
                }
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
            LogUtils.LogDebug("HandleNetworkCommand", "Start");
            if (!(nic.target is QuantumStorageControllerMachine target))
            {
                LogUtils.LogDebug("HandleNetworkCommand", "Machine is null");
                return null;
            }

            string command = nic.command;
            LogUtils.LogDebug("HandleNetworkCommand", "Command: " + command);
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
                            LogUtils.LogDebug("HandleNetworkCommand",
                                "Take item: " + nic.itemContext.GetDisplayString());
                            LogUtils.LogDebug("HandleNetworkCommand", "Target: " + target);
                            QuantumStorageControllerWindow.TakeItem(player, target, nic.itemContext);
                            break;
                        case 2:
                            LogUtils.LogDebug("HandleNetworkCommand",
                                "Store item: " + nic.itemContext.GetDisplayString());
                            LogUtils.LogDebug("HandleNetworkCommand", "Target: " + target);
                            QuantumStorageControllerWindow.StoreItem(player, target, nic.itemContext);
                            break;
                    }
                }
            }

            LogUtils.LogDebug("HandleNetworkCommand", "Process end");

            NetworkInterfaceResponse interfaceResponse = new NetworkInterfaceResponse();
            interfaceResponse.entity = target;
            interfaceResponse.inventory = player.mInventory;
            return interfaceResponse;
        }
    }
}
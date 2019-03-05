using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jazornowsky.QuantumStorage.machine.quantumIoPort
{
    public static class QuantumIoPortItemSearch
    {
        private static List<ItemBase> SearchResults;
        private static int Counter;
        private static string EntryString;
        private static int UpdateTreshold = 5;
        private static int UpdateWindowCooldown;

        public static void SpawnWindow(BaseMachineWindow sourcewindow)
        {
            UpdateWindowCooldown = 0;
            sourcewindow.manager.AddButton("searchcancel", PersistentSettings.GetString("Cancel"), 100, 0);
            sourcewindow.manager.AddBigLabel("searchtitle", PersistentSettings.GetString("Enter_Search_Term"), Color.white, 50, 40);
            sourcewindow.manager.AddBigLabel("searchtext", "_", Color.cyan, 50, 65);
            if (SearchResults == null)
                return;
            int count = SearchResults.Count;
            for (int index = 0; index < count; ++index)
            {
                sourcewindow.manager.AddIcon("itemicon" + index, "empty", Color.white, 10, 100 + 60 * index);
                sourcewindow.manager.AddBigLabel("iteminfo" + index, PersistentSettings.GetString("Inventory_Item"), Color.white, 60, 90 + 60 * index);
                sourcewindow.manager.AddLabel(GenericMachineManager.LabelType.OneLineFullWidth, "itemcount" + index, String.Empty, Color.white, false, 60, 120 + 60 * index);
            }
        }

        public static bool UpdateMachine(BaseMachineWindow sourcewindow)
        {
            if (SearchResults == null)
            {
                ++Counter;
                foreach (char ch in Input.inputString)
                {
                    if ((int)ch == (int)"\b"[0])
                    {
                        if (EntryString.Length != 0)
                            EntryString = EntryString.Substring(0, EntryString.Length - 1);
                    }
                    else
                    {
                        if (ch == "\n"[0] || ch == "\r"[0])
                        {
                            SearchResults = new List<ItemBase>();
                            for (int index = 0; index < ItemEntry.mEntries.Length; ++index)
                            {
                                if (ItemEntry.mEntries[index] != null && PersistentSettings.GetString(ItemEntry.mEntries[index].Name).ToLower().Contains(EntryString.ToLower()))
                                    SearchResults.Add(ItemManager.SpawnItem(ItemEntry.mEntries[index].ItemID));
                            }
                            for (int index1 = 0; index1 < TerrainData.mEntries.Length; ++index1)
                            {
                                bool flag = false;
                                TerrainDataEntry mEntry = TerrainData.mEntries[index1];
                                if (mEntry != null && !mEntry.Hidden)
                                {
                                    if (PersistentSettings.GetString(mEntry.Name).ToLower().Contains(EntryString.ToLower()))
                                    {
                                        int count = mEntry.Values.Count;
                                        for (int index2 = 0; index2 < count; ++index2)
                                        {
                                            if (PersistentSettings.GetString(mEntry.Values[index2].Name).ToLower().Contains(EntryString.ToLower()))
                                            {
                                                SearchResults.Add(ItemManager.SpawnCubeStack(mEntry.CubeType, mEntry.Values[index2].Value, 1));
                                                flag = true;
                                            }
                                        }
                                        if (!flag && string.IsNullOrEmpty(mEntry.PickReplacement))
                                            SearchResults.Add(ItemManager.SpawnCubeStack(mEntry.CubeType, mEntry.DefaultValue, 1));
                                    }
                                    if ((EntryString.ToLower().Contains("component") || EntryString.ToLower().Contains("placement") || EntryString.ToLower().Contains("multi")) && mEntry.CubeType == (ushort)600)
                                    {
                                        int count = mEntry.Values.Count;
                                        for (int index2 = 0; index2 < count; ++index2)
                                            SearchResults.Add(ItemManager.SpawnCubeStack(600, mEntry.Values[index2].Value, 1));
                                    }
                                }
                            }
                            if (SearchResults.Count == 0)
                                SearchResults = null;
                            UIManager.mbEditingTextField = false;
                            UIManager.RemoveUIRules("TextEntry");
                            sourcewindow.manager.RedrawWindow();
                            return true;
                        }
                        EntryString += ch;
                    }
                }
                sourcewindow.manager.UpdateLabel("searchtext", EntryString + (Counter % 20 <= 10 ? string.Empty : "_"), Color.cyan);
                return true;
            }

            sourcewindow.manager.UpdateLabel("searchtitle", PersistentSettings.GetString("Searching_for"), Color.white);
            sourcewindow.manager.UpdateLabel("searchtext", EntryString, Color.cyan);

            if (!(sourcewindow is QuantumIoPortWindow target))
            {
                return false;
            }
            else
            {
                if (UpdateWindowCooldown > 0)
                {
                    --UpdateWindowCooldown;
                    return true;
                }
                else
                {
                    UpdateWindowCooldown = UpdateTreshold;
                }
                var quantumIoPortWindow = (QuantumIoPortWindow)sourcewindow;
                int count1 = SearchResults.Count;
                for (int index = 0; index < count1; ++index)
                {
                    ItemBase searchResult = SearchResults[index];
                    string itemName = ItemManager.GetItemName(searchResult);
                    string itemIcon = ItemManager.GetItemIcon(searchResult);

                    ItemBase itemInInventory = quantumIoPortWindow.QuantumIoPort.StorageIoService.GetStorageController().GetItems()
                        .Find(x => x.Compare(searchResult));
                    int count = itemInInventory == null ? 0 : itemInInventory.GetAmount();

                    sourcewindow.manager.UpdateIcon("itemicon" + index, itemIcon, Color.white);
                    sourcewindow.manager.UpdateLabel("iteminfo" + index, itemName, Color.white);
                    sourcewindow.manager.UpdateLabel("itemcount" + index, "Item count in storage: " + count, Color.white);
                }
                return false;
            }
        }

        public static bool HandleButtonPress(BaseMachineWindow sourcewindow, string name, out ItemBase selectedItem)
        {
            selectedItem = null;
            if (name == "searchcancel")
            {
                SearchResults = null;
                UIManager.mbEditingTextField = false;
                UIManager.RemoveUIRules("TextEntry");
                EntryString = string.Empty;
                sourcewindow.manager.RedrawWindow();
                return true;
            }
            if (name.Contains("itemicon"))
            {
                int result = -1;
                int.TryParse(name.Replace("itemicon", string.Empty), out result);
                if (result > -1)
                {
                    selectedItem = SearchResults[result];
                    SearchResults = null;
                    EntryString = string.Empty;
                    sourcewindow.manager.RedrawWindow();
                    return true;
                }
            }
            return false;
        }

        public static void SetupUIRules()
        {
            UIManager.mbEditingTextField = true;
            UIManager.AddUIRules("TextEntry", UIRules.RestrictMovement | UIRules.RestrictLooking | UIRules.RestrictBuilding | UIRules.RestrictInteracting | UIRules.SetUIUpdateRate);
            GenericMachinePanelScript.instance.Scroll_Bar.GetComponent<UIScrollBar>().value = 0.0f;
        }

        public static void TerminateSearchWindow()
        {
            SearchResults = (List<ItemBase>)null;
            EntryString = string.Empty;
            UIManager.mbEditingTextField = false;
            UIManager.RemoveUIRules("TextEntry");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jazornowsky.QuantumStorage.model
{
    public class MachineStorage
    {
        private List<ItemBase> _items;
        public int ItemCount { get; set; }
        public int MaxCapacity { get; set; }
        public int StorageBlockCount { get; set; }

        public MachineStorage()
        {
            ItemCount = 0;
            MaxCapacity = 0;
            StorageBlockCount = 0;
            _items = new List<ItemBase>();
        }

        public bool IsFull()
        {
            return ItemCount >= MaxCapacity;
        }

        public void GroupItemsById()
        {
            List<ItemBase> grouppedItems = new List<ItemBase>();
            if (_items.Count > 0)
            {
                AddListItemFromIndex(0, ref grouppedItems);
            }

            _items = grouppedItems;
        }

        private void AddListItemFromIndex(int index, ref List<ItemBase> grouppedItems)
        {
            while (true)
            {
                _items[index].AddListItem(ref grouppedItems, false);
                _items.RemoveAt(index);
                if (_items.Count > 0)
                {
                    continue;
                }

                break;
            }
        }

        public List<ItemBase> GetItemsByType(int itemIndex, int itemId)
        {
            List<ItemBase> grouppedItem = new List<ItemBase>();
            ItemBase item = _items[itemIndex];
            item.AddListItem(ref grouppedItem, false);
            for (int index = itemIndex + 1; index < _items.Count; index++)
            {
                if (_items[itemIndex].mnItemID == itemId)
                {
                    _items[index].AddListItem(ref grouppedItem, false);
                    _items.RemoveAt(itemIndex);
                }
            }

            return grouppedItem;
        }

        public int GetRemainingCapacity()
        {
            return MaxCapacity - ItemCount;
        }

        public List<ItemBase> Items
        {
            get => _items;
            set
            {
                _items = value;
                GroupItemsById();
            }
        }
    }
}
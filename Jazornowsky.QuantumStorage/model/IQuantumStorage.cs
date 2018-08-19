using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jazornowsky.QuantumStorage.model
{
    interface IQuantumStorage
    {
        int GetCapacity();

        bool IsFull();

        List<ItemBase> GetItems();

        ItemBase TakeItem(ItemBase item);

        void AddItem(ref ItemBase item, bool force = false);

        int GetItemCount();

        List<SegmentEntity> GetConnectedSegments(ref List<SegmentEntity> segments);
    }
}

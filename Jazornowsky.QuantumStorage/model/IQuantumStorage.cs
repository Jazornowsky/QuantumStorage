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

        void AddItem(ref ItemBase item);

        int GetItemCount();

        List<IQuantumStorage> GetConnectedStorages(ref List<IQuantumStorage> storages);

        List<IQuantumStorage> GetConnectedStorages(ref List<IQuantumStorage> storages, ref List<IQuantumIo> quantumIos);
    }
}

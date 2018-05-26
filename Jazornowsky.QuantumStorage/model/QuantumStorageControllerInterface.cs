using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jazornowsky.QuantumStorage.model
{
    interface QuantumStorageControllerInterface
    {
        bool AddItem(ItemBase item);

        ItemBase GetItem(ItemBase itemBase, ushort cubeType, ushort cubeValue);
    }
}

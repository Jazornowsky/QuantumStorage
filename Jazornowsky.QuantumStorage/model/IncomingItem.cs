using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jazornowsky.QuantumStorage
{
    public class IncomingItem
    {
        public ItemBase item;
        public ushort cubeType;
        public ushort cubeValue;

        public IncomingItem(ItemBase item, ushort cubeType, ushort cubeValue)
        {
            this.item = item;
            this.cubeType = cubeType;
            this.cubeValue = cubeValue;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jazornowsky.QuantumStorage.utils
{
    class ItemUtils
    {
        public static int GetItemAmoutToRetrieve()
        {
            var amount = 100;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                amount = 10;
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                amount = 1;
            }
            return amount;
        }
    }
}

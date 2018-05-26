using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jazornowsky.QuantumStorage.utils
{
    class ItemBaseUtils
    {
        public static ItemBase RemoveListItem(ItemBase item, ref List<ItemBase> sourcelist)
        {
            if (item == null)
            {
                return (ItemBase) null;
            }

            List<ItemBase> list = sourcelist.ToList<ItemBase>();
            if (item.IsStack())
            {
                int amount1 = item.GetAmount();
                for (int index = 0; index < list.Count; index++)
                {
                    ItemBase original = list[index];
                    int amount2 = original.GetAmount();
                    if (original.Compare(item) && amount2 > amount1)
                    {
                        original.DecrementStack(amount1);
                        sourcelist = list;
                        item.SetAmount(0);
                        return item.NewInstance();
                    } else if (original.Compare(item) && amount2 == amount1)
                    {
                        list.Remove(original);
                        sourcelist = list;
                        item.SetAmount(0);
                        return item.NewInstance();
                    }
                    else if (original.Compare(item))
                    {
                        item.SetAmount(item.GetAmount() - original.GetAmount());
                        list.Remove(original);
                        sourcelist = list;
                        return item.NewInstance();
                    }
                }
            }
            else
            {
                for (int index = 0; index < list.Count; index++)
                {
                    ItemBase original = list[index];
                    if (original.Compare(item))
                    {
                        list.Remove(original);
                        sourcelist = list;
                        return original;
                    }
                }
            }

            sourcelist = list;
            return (ItemBase) null;
        }
    }
}
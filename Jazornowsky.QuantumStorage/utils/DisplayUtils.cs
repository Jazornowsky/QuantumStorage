using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jazornowsky.QuantumStorage.model;

namespace Jazornowsky.QuantumStorage.utils
{
    class DisplayUtils
    {
        public static string MachineDisplay(string s)
        {
            return s + "\n";
        }

        public static string PowerDisplay(MachinePower machinePower)
        {
            string txt = "Desires: " + machinePower.MinOperatingPower + "PPS. Current PPS: " + machinePower.GetPps() + "PPS\n";
            if (!machinePower.HasPower())
            {
                txt += "NO POWER\n";
            }
            else
            {
                txt += "Power: " + machinePower.CurrentPower + "/" + machinePower.MaxPower + "\n";
            }

            return txt;
        }

        public static string StorageDisplay(MachineStorage machineStorage)
        {
            string txt = "";
            if (machineStorage.StorageBlockCount == 0)
            {
                txt = "NO STORAGE CONNECTED - PLACE STORAGE BEHIND CONTROLLER";
            }
            else
            {
                txt += "Item count: " + machineStorage.ItemCount + "/" + machineStorage.MaxCapacity + "\n";
                txt += "Storage blocks count: " + machineStorage.StorageBlockCount + "\n";
            }

            return txt;
        }
    }
}
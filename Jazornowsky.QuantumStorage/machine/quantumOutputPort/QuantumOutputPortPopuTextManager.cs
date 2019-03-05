using Jazornowsky.QuantumStorage.service;
using Jazornowsky.QuantumStorage.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jazornowsky.QuantumStorage.machine.quantumOutputPort
{

    class QuantumOutputPortPopuTextManager
    {
        private readonly QuantumOutputPortMachine QuantumOutputPortMachine;

        public QuantumOutputPortPopuTextManager(QuantumOutputPortMachine quantumOutputPortMachine)
        {
            QuantumOutputPortMachine = quantumOutputPortMachine;
        }

        public string getPopupText()
        {
            var storageController = QuantumOutputPortMachine.StorageIoService.GetStorageController();
            string txt = DisplayUtils.MachineDisplay(QuantumOutputPortMachine.MachineName);

            if ((QuantumOutputPortMachine.ControllerPos[0] == 0 && QuantumOutputPortMachine.ControllerPos[1] == 0 && QuantumOutputPortMachine.ControllerPos[2] == 0) ||
                storageController == null)
            {
                txt += "QUANTUM STORAGE CONTROLLER NOT FOUND.\n";
            }
            if (storageController != null && !storageController.HasPower())
            {
                txt += "QUANTUM STORAGE CONTROLLER HAS NO POWER.\n";
            }
            txt += "\nOutput status: ";
            if (!QuantumOutputPortMachine.Enabled)
            {
                txt += "disabled\n";
            }
            else
            {
                txt += "enabled\n";
            }

            txt += "Output item: ";
            if (QuantumOutputPortMachine.Exemplar != null)
            {
                txt += ItemManager.GetItemName(QuantumOutputPortMachine.Exemplar);
            }
            else
            {
                txt += "none";
            }

            if (Input.GetButtonDown("Interact") && (UIManager.AllowInteracting))
            {
                UIManager.ForceNGUIUpdate = 0.1f;
                AudioHUDManager.instance.HUDIn();
            }

            return txt;
        }
    }

    
}

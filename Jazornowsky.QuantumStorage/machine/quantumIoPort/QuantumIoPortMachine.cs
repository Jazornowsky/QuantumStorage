using System;
using Jazornowsky.QuantumStorage.utils;
using UnityEngine;

namespace Jazornowsky.QuantumStorage
{
    internal class QuantumIoPortMachine : AbstractQuantumIoMachine
    {
        public static readonly string MachineName = "Quantum I/O Port";

        public QuantumIoPortMachine(MachineEntityCreationParameters parameters) : base(parameters)
        {
        }

        public override string GetPopupText()
        {
            var txt = DisplayUtils.MachineDisplay(MachineName);
            var quantumStorageController = StorageIoService.GetStorageController();
            if (quantumStorageController != null && quantumStorageController.IsOperating())
            {
                txt += DisplayUtils.StorageDisplay(quantumStorageController.GetMachineStorage());
            }
            if (ControllerPos[0] == 0 && ControllerPos[1] == 0 && ControllerPos[2] == 0 ||
                quantumStorageController == null)
            {
                txt += "QUANTUM STORAGE CONTROLLER NOT FOUND.\n";
            }

            if (quantumStorageController != null && !quantumStorageController.HasPower())
            {
                txt += "QUANTUM STORAGE CONTROLLER HAS NO POWER.\n";
            }

            if (Input.GetButtonDown("Interact") && UIManager.AllowInteracting)
            {
                UIManager.ForceNGUIUpdate = 0.1f;
                AudioHUDManager.instance.HUDIn();
            }

            return txt;
        }

        public override void UnityUpdate()
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jazornowsky.QuantumStorage.model
{
    class MachinePower
    {
        public float MaxPower { get; }
        public float CurrentPower { get; set; }
        public float PreviousUpdatePower { get; set; }
        public float MinOperatingPower { get; }
        public float MaximumDeliveryRate { get; set; }

        public MachinePower(float maxPower, float maximumDeliveryRate, float minOperatingPower)
        {
            MaxPower = maxPower;
            MaximumDeliveryRate = maximumDeliveryRate;
            MinOperatingPower = minOperatingPower;
            CurrentPower = 0;
        }

        public bool HasPower()
        {
            if (CurrentPower < MinOperatingPower)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void ConsumePower()
        {
            if (CurrentPower >= MinOperatingPower)
            {
                CurrentPower -= MinOperatingPower;
            }
        }

        public float GetPps()
        {
            return CurrentPower - PreviousUpdatePower;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jazornowsky.QuantumStorage.model
{
    interface IQuantumIo
    {
        void SetControllerPos(long x, long y, long z);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Jazornowsky.QuantumStorage.utils
{
    class GameObjectUtils
    {
        private static readonly string LogName = "GameObjectUtils";

        public static void LogAllChildrenTransformsName(Transform transform)
        {
            for (int index = 0; index < transform.childCount; index++)
            {
                LogUtils.LogDebug(LogName, "Children name: " + transform.GetChild(index).name);
            }
        }
    }
}

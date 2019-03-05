using System;
using UnityEngine;

namespace Jazornowsky.QuantumStorage.utils
{
    public class LogUtils
    {
        public static void LogDebug(String msg)
        {
            //if (Debug.isDebugBuild)
            //{
                Debug.Log(msg);
            //}
        }

        public static void LogDebug(String machineName, String msg)
        {
            LogDebug(machineName + " - " + msg);
        }
    }
}
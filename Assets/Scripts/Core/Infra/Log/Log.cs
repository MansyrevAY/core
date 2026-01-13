using System;
using UnityEngine;

namespace Core.Infra.Log
{
    public static class Log
    {
        public static void Info(string message)
        {
            Debug.Log(message);
        }

        public static void Error(string message)
        {
            Debug.LogError(message);
        }

        public static void Warning(string message)
        {
            Debug.LogWarning(message);
        }

        public static void Exception(Exception e)
        {
            Debug.LogException(e);
        }
    }
}

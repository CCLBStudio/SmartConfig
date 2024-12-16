using System;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    public class ScJsonEnum
    {
        public static bool ToEnum<T>(string enumElement, out T result) where T : Enum
        {
            bool success = Enum.TryParse(typeof(T), enumElement, out var r);
            result = (T)r;
        
            if (!success)
            {
                Debug.LogError($"Unable to parse enum value {enumElement} into enum type {typeof(T).Name}.");
                return false;
            }

            return true;
        }
    }
}

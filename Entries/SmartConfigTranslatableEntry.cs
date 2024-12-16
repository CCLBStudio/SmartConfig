using System;
using System.Collections.Generic;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class SmartConfigTranslatableEntry : SmartConfigEntry
    {
        public Dictionary<SystemLanguage, string> value;
    }
}
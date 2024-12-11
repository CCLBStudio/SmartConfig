using System;
using System.Collections.Generic;
using UnityEngine;

namespace CCLBStudio.RemoteConfig
{
    [Serializable]
    public class RemoteConfigTranslatableEntry : RemoteConfigEntry
    {
        public Dictionary<SystemLanguage, string> value;
    }
}
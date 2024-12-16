using System;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class SmartConfigEntry
    {
        public string key;
        public SmartConfigValueType type;
        public string category;
    }
}
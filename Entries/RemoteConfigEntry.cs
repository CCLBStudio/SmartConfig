using System;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class RemoteConfigEntry
    {
        public string key;
        public RemoteConfigValueType type;
        public string category;
    }
}
using System;

namespace CCLBStudio.RemoteConfig
{
    [Serializable]
    public class RemoteConfigEntry
    {
        public string key;
        public RemoteConfigValueType type;
        public string category;
    }
}
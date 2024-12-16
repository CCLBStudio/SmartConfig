using System;
using System.Collections.Generic;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class SmartConfigJson
    {
        public int version;
        public List<SmartConfigPlatformEntryJson> platforms;
        public List<SmartConfigEntryJson> entries;
    }

    [Serializable]
    public class SmartConfigEntryJson
    {
        public string key;
        public string type;
        public string value;
        public string category;
    }

    [Serializable]
    public class SmartConfigPlatformEntryJson
    {
        public string platform;
        public List<SmartConfigEntryJson> entries;
    }
}
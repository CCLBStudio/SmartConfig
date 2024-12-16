using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

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
        [JsonConverter(typeof(StringEnumConverter))]
        public SmartConfigValueType type;
        public object value;
        public string category;
    }

    [Serializable]
    public class SmartConfigPlatformEntryJson
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RuntimePlatform platform;
        public List<SmartConfigEntryJson> entries;
    }
}
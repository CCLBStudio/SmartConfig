using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace CCLBStudio.RemoteConfig
{
    [Serializable]
    public class RemoteConfigJson
    {
        public int version;
        public List<RemoteConfigPlatformEntryJson> platforms;
        public List<RemoteConfigEntryJson> entries;
    }

    [Serializable]
    public class RemoteConfigEntryJson
    {
        public string key;
        [JsonConverter(typeof(StringEnumConverter))]
        public RemoteConfigValueType type;
        public object value;
        public string category;
    }

    [Serializable]
    public class RemoteConfigPlatformEntryJson
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RuntimePlatform platform;
        public List<RemoteConfigEntryJson> entries;
    }
}
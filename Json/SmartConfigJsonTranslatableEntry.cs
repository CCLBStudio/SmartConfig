using System;
using System.Collections.Generic;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class SmartConfigJsonTranslatableDictionary
    {
        public List<SmartConfigJsonTranslatableEntry> translations;

        public SmartConfigJsonTranslatableDictionary(List<SmartConfigKeyValuePair<SystemLanguage, string>> pairs)
        {
            translations = new List<SmartConfigJsonTranslatableEntry>(pairs.Count);
            foreach (var pair in pairs)
            {
                translations.Add(new SmartConfigJsonTranslatableEntry
                {
                    language = pair.Key.ToString(),
                    content = pair.Value
                });
            }
        }
    }
    
    [Serializable]
    public class SmartConfigJsonTranslatableEntry
    {
        public string language;
        public string content;
    }
}

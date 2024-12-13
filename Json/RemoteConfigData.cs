using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class RemoteConfigData
    {
        public List<RemoteConfigEntry> allEntries;
        public List<SystemLanguage> allLanguages;
        public List<string> allCategories;

        public Dictionary<RuntimePlatform, List<RemoteConfigEntry>> platformEntries;
        public Dictionary<string, RemoteConfigIntEntry> intEntries;
        public Dictionary<string, RemoteConfigFloatEntry> floatEntries;
        public Dictionary<string, RemoteConfigBoolEntry> boolEntries;
        public Dictionary<string, RemoteConfigStringEntry> stringEntries;
        public Dictionary<string, RemoteConfigTranslatableEntry> translatableEntries;

        /// <summary>
        /// Build the remote config data from the relevant json information.
        /// </summary>
        /// <param name="json">The json to build from</param>
        public RemoteConfigData(string json)
        {
            RemoteConfigJson rc = JsonConvert.DeserializeObject<RemoteConfigJson>(json);
            platformEntries = new Dictionary<RuntimePlatform, List<RemoteConfigEntry>>();
            allEntries = new List<RemoteConfigEntry>();
            allLanguages = new List<SystemLanguage>();
            allCategories = new List<string>();
            intEntries = new Dictionary<string, RemoteConfigIntEntry>();
            floatEntries = new Dictionary<string, RemoteConfigFloatEntry>();
            boolEntries = new Dictionary<string, RemoteConfigBoolEntry>();
            stringEntries = new Dictionary<string, RemoteConfigStringEntry>();
            translatableEntries = new Dictionary<string, RemoteConfigTranslatableEntry>();

            foreach (var jsonPlatform in rc.platforms)
            {
                if (platformEntries.ContainsKey(jsonPlatform.platform))
                {
                    Debug.LogError($"Platform {jsonPlatform.platform} is already in the platforms dictionary !");
                    continue;
                }

                var platformEntryList = new  List<RemoteConfigEntry>(jsonPlatform.entries.Count);
                platformEntries[jsonPlatform.platform] = platformEntryList;

                foreach (var jsonEntry in jsonPlatform.entries)
                {
                    switch (jsonEntry.type)
                {
                    case RemoteConfigValueType.Int:
                        var intEntry = CreateIntEntryFrom(jsonEntry);
                        if (intEntry == null)
                        {
                            continue;
                        }
                        
                        platformEntryList.Add(intEntry);
                        break;
                    
                    case RemoteConfigValueType.Float:
                        var floatEntry = CreateFloatEntryFrom(jsonEntry);
                        if (floatEntry == null)
                        {
                            continue;
                        }
                        
                        platformEntryList.Add(floatEntry);
                        break;
                    
                    case RemoteConfigValueType.Bool:
                        var boolEntry = CreateBoolEntryFrom(jsonEntry);
                        if (boolEntry == null)
                        {
                            continue;
                        }
                        
                        platformEntryList.Add(boolEntry);
                        break;
                    
                    case RemoteConfigValueType.String:
                        var stringEntry = CreateStringEntryFrom(jsonEntry);
                        if (stringEntry == null)
                        {
                            continue;
                        }
                        
                        platformEntryList.Add(stringEntry);
                        break;
                    
                    case RemoteConfigValueType.Translatable:
                        var translatableEntry = CreateTranslatableEntryFrom(jsonEntry);
                        if (translatableEntry == null)
                        {
                            continue;
                        }
                        
                        platformEntryList.Add(translatableEntry);
                        break;
                }
                }
            }

            foreach (var jsonEntry in rc.entries)
            {
                #if UNITY_EDITOR
                if (!string.IsNullOrEmpty(jsonEntry.category) && !allCategories.Contains(jsonEntry.category))
                {
                    allCategories.Add(jsonEntry.category);
                }
                #endif
                
                switch (jsonEntry.type)
                {
                    case RemoteConfigValueType.Int:
                        var intEntry = CreateIntEntryFrom(jsonEntry);
                        if (intEntry == null)
                        {
                            continue;
                        }
                        
                        TryAddEntryTo(intEntry, allEntries, intEntries);
                        break;
                    
                    case RemoteConfigValueType.Float:
                        var floatEntry = CreateFloatEntryFrom(jsonEntry);
                        if (floatEntry == null)
                        {
                            continue;
                        }
                        
                        TryAddEntryTo(floatEntry, allEntries, floatEntries);
                        break;
                    
                    case RemoteConfigValueType.Bool:
                        var boolEntry = CreateBoolEntryFrom(jsonEntry);
                        if (boolEntry == null)
                        {
                            continue;
                        }
                        
                        TryAddEntryTo(boolEntry, allEntries, boolEntries);
                        break;
                    
                    case RemoteConfigValueType.String:
                        var stringEntry = CreateStringEntryFrom(jsonEntry);
                        if (stringEntry == null)
                        {
                            continue;
                        }
                        
                        TryAddEntryTo(stringEntry, allEntries, stringEntries);
                        break;
                    
                    case RemoteConfigValueType.Translatable:
                        var translatableEntry = CreateTranslatableEntryFrom(jsonEntry);
                        if (translatableEntry == null)
                        {
                            continue;
                        }
                        
                        TryAddEntryTo(translatableEntry, allEntries, translatableEntries);
                        break;
                }
            }
        }

        private void TryAddEntryTo<TValue>(RemoteConfigEntry entry, List<RemoteConfigEntry> list, IDictionary<string, TValue> dictionary = null)
        {
            list.Add(entry);
            
            if (dictionary == null)
            {
                return;
            }
            
            Type targetType = typeof(TValue);

            try
            {
                TValue castedValue = (TValue)Convert.ChangeType(entry, targetType);
                if (!dictionary.TryAdd(entry.key, castedValue))
                {
                    Debug.LogError($"Unable to add entry {entry.key} to the entries dictionary !");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Problem adding entry {entry.key} in dictionary. Error : {e.Message}");
            }
        }
        
        private RemoteConfigIntEntry CreateIntEntryFrom(RemoteConfigEntryJson entry)
        {
            if (!int.TryParse(entry.value.ToString(), out int i))
            {
                Debug.LogError($"Entry {entry.key} is tagged as {entry.type} but its value is of type {entry.value.GetType().Name} !");
                return null;
            }
            
            return new RemoteConfigIntEntry
            {
                key = entry.key,
                type = entry.type,
                category = entry.category,
                value = i
            };
        }
        
        private RemoteConfigFloatEntry CreateFloatEntryFrom(RemoteConfigEntryJson entry)
        {
            if (!float.TryParse(entry.value.ToString(), out float f))
            {
                Debug.LogError($"Entry {entry.key} is tagged as {entry.type} but its value is of type {entry.value.GetType().Name} !");
                return null;
            }
            
            return new RemoteConfigFloatEntry
            {
                key = entry.key,
                type = entry.type,
                category = entry.category,
                value = f
            };
        }
        
        private RemoteConfigBoolEntry CreateBoolEntryFrom(RemoteConfigEntryJson entry)
        {
            if (entry.value is not bool b)
            {
                Debug.LogError($"Entry {entry.key} is tagged as {entry.type} but its value is of type {entry.value.GetType().Name} !");
                return null;
            }
            
            return new RemoteConfigBoolEntry
            {
                key = entry.key,
                type = entry.type,
                category = entry.category,
                value = b
            };
        }
        
        private RemoteConfigStringEntry CreateStringEntryFrom(RemoteConfigEntryJson entry)
        {
            if (entry.value is not string s)
            {
                Debug.LogError($"Entry {entry.key} is tagged as {entry.type} but its value is of type {entry.value.GetType().Name} !");
                return null;
            }
            
            return new RemoteConfigStringEntry
            {
                key = entry.key,
                type = entry.type,
                category = entry.category,
                value = s
            };
        }
        
        private RemoteConfigTranslatableEntry CreateTranslatableEntryFrom(RemoteConfigEntryJson entry)
        {
            try
            {
                var jsonTranslations = JsonConvert.DeserializeObject<Dictionary<string, string>>(entry.value.ToString());
                Dictionary<SystemLanguage, string> translations = new Dictionary<SystemLanguage, string>();
                foreach (var kvp in jsonTranslations)
                {
                    if (Enum.TryParse<SystemLanguage>(kvp.Key, false, out var lang))
                    {
                        translations[lang] = kvp.Value;
                        if (!allLanguages.Contains(lang))
                        {
                            allLanguages.Add(lang);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Enable to cast {kvp.Key} into a SystemLanguage value !");
                    }
                }
                        
                return new RemoteConfigTranslatableEntry
                {
                    key = entry.key,
                    type = entry.type,
                    category = entry.category,
                    value = translations
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to generate the translatable entry for key {entry.key}. Error : {e.Message}");
                return null;
            }
        }
    }
}
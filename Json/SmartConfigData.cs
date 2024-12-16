using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class SmartConfigData
    {
        public List<SmartConfigEntry> allEntries;
        public List<SystemLanguage> allLanguages;
        public List<string> allCategories;

        public Dictionary<RuntimePlatform, List<SmartConfigEntry>> platformEntries;
        public Dictionary<string, SmartConfigIntEntry> intEntries;
        public Dictionary<string, SmartConfigFloatEntry> floatEntries;
        public Dictionary<string, SmartConfigBoolEntry> boolEntries;
        public Dictionary<string, SmartConfigStringEntry> stringEntries;
        public Dictionary<string, SmartConfigTranslatableEntry> translatableEntries;

        /// <summary>
        /// Build the remote config data from the relevant json information.
        /// </summary>
        /// <param name="json">The json to build from</param>
        public SmartConfigData(string json)
        {
            SmartConfigJson rc = JsonConvert.DeserializeObject<SmartConfigJson>(json);
            platformEntries = new Dictionary<RuntimePlatform, List<SmartConfigEntry>>();
            allEntries = new List<SmartConfigEntry>();
            allLanguages = new List<SystemLanguage>();
            allCategories = new List<string>();
            intEntries = new Dictionary<string, SmartConfigIntEntry>();
            floatEntries = new Dictionary<string, SmartConfigFloatEntry>();
            boolEntries = new Dictionary<string, SmartConfigBoolEntry>();
            stringEntries = new Dictionary<string, SmartConfigStringEntry>();
            translatableEntries = new Dictionary<string, SmartConfigTranslatableEntry>();

            foreach (var jsonPlatform in rc.platforms)
            {
                if (platformEntries.ContainsKey(jsonPlatform.platform))
                {
                    Debug.LogError($"Platform {jsonPlatform.platform} is already in the platforms dictionary !");
                    continue;
                }

                var platformEntryList = new  List<SmartConfigEntry>(jsonPlatform.entries.Count);
                platformEntries[jsonPlatform.platform] = platformEntryList;

                foreach (var jsonEntry in jsonPlatform.entries)
                {
                    switch (jsonEntry.type)
                {
                    case SmartConfigValueType.Int:
                        var intEntry = CreateIntEntryFrom(jsonEntry);
                        if (intEntry == null)
                        {
                            continue;
                        }
                        
                        platformEntryList.Add(intEntry);
                        break;
                    
                    case SmartConfigValueType.Float:
                        var floatEntry = CreateFloatEntryFrom(jsonEntry);
                        if (floatEntry == null)
                        {
                            continue;
                        }
                        
                        platformEntryList.Add(floatEntry);
                        break;
                    
                    case SmartConfigValueType.Bool:
                        var boolEntry = CreateBoolEntryFrom(jsonEntry);
                        if (boolEntry == null)
                        {
                            continue;
                        }
                        
                        platformEntryList.Add(boolEntry);
                        break;
                    
                    case SmartConfigValueType.String:
                        var stringEntry = CreateStringEntryFrom(jsonEntry);
                        if (stringEntry == null)
                        {
                            continue;
                        }
                        
                        platformEntryList.Add(stringEntry);
                        break;
                    
                    case SmartConfigValueType.Translatable:
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
                    case SmartConfigValueType.Int:
                        var intEntry = CreateIntEntryFrom(jsonEntry);
                        if (intEntry == null)
                        {
                            continue;
                        }
                        
                        TryAddEntryTo(intEntry, allEntries, intEntries);
                        break;
                    
                    case SmartConfigValueType.Float:
                        var floatEntry = CreateFloatEntryFrom(jsonEntry);
                        if (floatEntry == null)
                        {
                            continue;
                        }
                        
                        TryAddEntryTo(floatEntry, allEntries, floatEntries);
                        break;
                    
                    case SmartConfigValueType.Bool:
                        var boolEntry = CreateBoolEntryFrom(jsonEntry);
                        if (boolEntry == null)
                        {
                            continue;
                        }
                        
                        TryAddEntryTo(boolEntry, allEntries, boolEntries);
                        break;
                    
                    case SmartConfigValueType.String:
                        var stringEntry = CreateStringEntryFrom(jsonEntry);
                        if (stringEntry == null)
                        {
                            continue;
                        }
                        
                        TryAddEntryTo(stringEntry, allEntries, stringEntries);
                        break;
                    
                    case SmartConfigValueType.Translatable:
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

        private void TryAddEntryTo<TValue>(SmartConfigEntry entry, List<SmartConfigEntry> list, IDictionary<string, TValue> dictionary = null)
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
        
        private SmartConfigIntEntry CreateIntEntryFrom(SmartConfigEntryJson entry)
        {
            if (!int.TryParse(entry.value.ToString(), out int i))
            {
                Debug.LogError($"Entry {entry.key} is tagged as {entry.type} but its value is of type {entry.value.GetType().Name} !");
                return null;
            }
            
            return new SmartConfigIntEntry
            {
                key = entry.key,
                type = entry.type,
                category = entry.category,
                value = i
            };
        }
        
        private SmartConfigFloatEntry CreateFloatEntryFrom(SmartConfigEntryJson entry)
        {
            if (!float.TryParse(entry.value.ToString(), out float f))
            {
                Debug.LogError($"Entry {entry.key} is tagged as {entry.type} but its value is of type {entry.value.GetType().Name} !");
                return null;
            }
            
            return new SmartConfigFloatEntry
            {
                key = entry.key,
                type = entry.type,
                category = entry.category,
                value = f
            };
        }
        
        private SmartConfigBoolEntry CreateBoolEntryFrom(SmartConfigEntryJson entry)
        {
            if (entry.value is not bool b)
            {
                Debug.LogError($"Entry {entry.key} is tagged as {entry.type} but its value is of type {entry.value.GetType().Name} !");
                return null;
            }
            
            return new SmartConfigBoolEntry
            {
                key = entry.key,
                type = entry.type,
                category = entry.category,
                value = b
            };
        }
        
        private SmartConfigStringEntry CreateStringEntryFrom(SmartConfigEntryJson entry)
        {
            if (entry.value is not string s)
            {
                Debug.LogError($"Entry {entry.key} is tagged as {entry.type} but its value is of type {entry.value.GetType().Name} !");
                return null;
            }
            
            return new SmartConfigStringEntry
            {
                key = entry.key,
                type = entry.type,
                category = entry.category,
                value = s
            };
        }
        
        private SmartConfigTranslatableEntry CreateTranslatableEntryFrom(SmartConfigEntryJson entry)
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
                        
                return new SmartConfigTranslatableEntry
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
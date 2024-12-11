using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CCLBStudio.RemoteConfig
{
    [CreateAssetMenu(fileName = "RemoteConfigService", menuName = "CCLB Studio/Remote Config/Service SO")]
    public class RemoteConfigService : ScriptableObject
    {
#region Editor Properties
#if UNITY_EDITOR
 
        public static string DefaultLanguageProperty => nameof(defaultLanguage);
        public static string LocalTranslationFileProperty => nameof(localTranslationFile);
        public static string ExistingLanguagesProperty => nameof(existingLanguages);

        public List<RemoteConfigKeyValuePair<string, int>> keyUses = new();

#endif
#endregion

#region Editor Methods
#if UNITY_EDITOR
        
        /// <summary>
        /// --- EDITOR ONLY --- Accessor for the local json file.
        /// </summary>
        public TextAsset LocalTranslationFile
        {
            get => localTranslationFile;
            set => localTranslationFile = value;
        }

        // public void ImportFromOldRc()
        // {
        //     if (!oldRc || !localTranslationFile)
        //     {
        //         Debug.LogError("At least one file is missing.");
        //         return;
        //     }
        //     
        //     RemoteConfigJson jsonData = new RemoteConfigJson
        //     {
        //         platforms = new List<RemoteConfigPlatformEntryJson>(),
        //         entries = new List<RemoteConfigEntryJson>(),
        //         version = 1 // tmp
        //     };
        //
        //     string content = oldRc.text;
        //     
        //     var records = content.Split(RemoteKey.LineDelimiter);
        //     foreach (var record in records)
        //     {
        //         var fields = record.Split(RemoteKey.FieldDelimiter);
        //         var index = fields[0].ToLower();
        //         fields[0] = fields[0].Replace( " ", "" );
        //
        //         RemoteKey key = null;
        //         if (fields.Length == 3)
        //         {
        //             key = new RemoteKey { name = fields[0], type = fields[1], value = fields[2].TrimEnd() };
        //         }
        //         
        //         else if (fields.Length > 3)
        //         {
        //             key = new RemoteKey { name = fields[0], type = fields[1], value = fields[2].TrimEnd() +";"+fields[3].TrimEnd() };
        //         }
        //
        //         if (key == null)
        //         {
        //             Debug.LogError("Unable to build key from record " + record);
        //             continue;
        //         }
        //         
        //         Debug.Log($"Key : {key.name}, type : {key.type}, value : {key.value}");
        //
        //         switch (key.type)
        //         {
        //             case "int":
        //                 bool success = int.TryParse(key.value, out int val);
        //                 if (!success)
        //                 {
        //                     Debug.LogError($"Unable to load int value from key {key.name}");
        //                     continue;
        //                 }
        //                 
        //                 jsonData.entries.Add(new RemoteConfigEntryJson
        //                 {
        //                     key = key.name,
        //                     type = RemoteConfigValueType.Int,
        //                     value = val
        //                 });
        //                 break;
        //             
        //             case "bool":
        //                 jsonData.entries.Add(new RemoteConfigEntryJson
        //                 {
        //                     key = key.name,
        //                     type = RemoteConfigValueType.Bool,
        //                     value = key.value.ToLower() == "true"
        //                 });
        //                 break;
        //             
        //             case "string":
        //                 Dictionary<string, string> jsonTranslations = new Dictionary<string, string>();
        //                 jsonTranslations.Add(SystemLanguage.French.ToString(), key.value);
        //                 jsonData.entries.Add(new RemoteConfigEntryJson
        //                 {
        //                     key = key.name,
        //                     type = RemoteConfigValueType.Translatable,
        //                     value = jsonTranslations
        //                 });
        //                 break;
        //         }
        //     }
        //     
        //     string json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
        //     string relativePath = AssetDatabase.GetAssetPath(localTranslationFile);
        //     string p = IOExtender.RelativeToAbsolutePath(relativePath);
        //     File.WriteAllText(p, json);
        //
        //     EditorUtility.SetDirty(this);
        //     AssetDatabase.SaveAssetIfDirty(this);
        //     AssetDatabase.Refresh();
        // }

        private void TrackKeyUsage(string key)
        {
            int index = keyUses.FindIndex(x => x.Key == key);
            if (index < 0)
            {
                keyUses.Add(new RemoteConfigKeyValuePair<string, int>(key, 1));
            }
            else
            {
                keyUses[index].Value++;
            }

            EditorUtility.SetDirty(this);
        }

#endif
        #endregion

        public SystemLanguage CurrentLanguage => _currentlySelectedLanguage ?? defaultLanguage;
        public const string FileName = "RemoteConfig.json";
        public RemoteConfigTransferStrategy TransferStrategy => transferStrategy;

        [SerializeField] private RemoteConfigTransferStrategy transferStrategy;
        [SerializeField] private SystemLanguage defaultLanguage = SystemLanguage.English;
        [SerializeField] private TextAsset localTranslationFile;
        [SerializeField] private List<SystemLanguage> existingLanguages;

        [NonSerialized] private Dictionary<string, int> _runtimeIntValues;
        [NonSerialized] private Dictionary<string, float> _runtimeFloatValues;
        [NonSerialized] private Dictionary<string, bool> _runtimeBoolValues;
        [NonSerialized] private Dictionary<string, string> _runtimeStringValues;
        [NonSerialized] private RemoteConfigData _runtimeRc;
        [NonSerialized] private SystemLanguage? _currentlySelectedLanguage;
        [NonSerialized] private SystemLanguage _currentlyTranslatedLanguage;

        #region Initialization

        public void Initialize()
        {
            _runtimeIntValues = new Dictionary<string, int>();
            _runtimeFloatValues = new Dictionary<string, float>();
            _runtimeBoolValues = new Dictionary<string, bool>();
            _runtimeStringValues = new Dictionary<string, string>();

            _runtimeRc = null;
            _currentlySelectedLanguage = null;
            _currentlyTranslatedLanguage = SystemLanguage.Unknown;
        }

        #endregion

        #region Loading Methods

        public void LoadRemoteConfig(Action onSuccess, Action onFail)
        {
            if (!transferStrategy)
            {
                Debug.LogError("No transfer strategy ! Unable to download !");
                onFail?.Invoke();
                return;
            }
            
            transferStrategy.DownloadJson(null
                , json =>
                {
                    OnRemoteConfigFetched(json);
                    onSuccess?.Invoke();
                }
                , () =>
                {
                    bool loadedLocalFile = OnRemoteConfigFetchFailed();
                    if (loadedLocalFile)
                    {
                        onSuccess?.Invoke();
                    }
                    else
                    {
                        onFail?.Invoke();
                    }
                });
        }

        private void OnRemoteConfigFetched(string json)
        {
            Debug.Log("--- REMOTE CONFIG --- Successfully downloaded the remote config file ! Loading...");
            _runtimeRc = new RemoteConfigData(json);
            LoadFrom(_runtimeRc);
        }

        private bool OnRemoteConfigFetchFailed()
        {
            Debug.LogError("--- REMOTE CONFIG --- Unable to download the remote config file. Checking for local file...");
            
            if (!localTranslationFile)
            {
                Debug.LogError("--- REMOTE CONFIG --- No local translation file, unable to load local remote config !");
                return false;
            }
            
            _runtimeRc = new RemoteConfigData(localTranslationFile.text);
            LoadFrom(_runtimeRc);
            return true;
        }

        private void LoadPlatformSettings(RemoteConfigData rc)
        {
            if (!rc.platformEntries.ContainsKey(Application.platform))
            {
                Debug.LogError($"--- REMOTE CONFIG --- No platform settings form current platform {Application.platform} !");
                return;
            }

            var platformSettings = rc.platformEntries[Application.platform];
            foreach (var entry in platformSettings)
            {
                switch (entry.type)
                {
                    case RemoteConfigValueType.Int:
                        var intEntry = (RemoteConfigIntEntry)entry;
                        if (!_runtimeIntValues.TryAdd(intEntry.key, intEntry.value))
                        {
                            Debug.LogError($"--- REMOTE CONFIG --- int dictionary already contains the key {entry.key} !");
                        }
                        break;
                    
                    case RemoteConfigValueType.Float:
                        var floatEntry = (RemoteConfigFloatEntry)entry;
                        if (!_runtimeFloatValues.TryAdd(floatEntry.key, floatEntry.value))
                        {
                            Debug.LogError($"--- REMOTE CONFIG --- float dictionary already contains the key {entry.key} !");
                        }
                        break;
                    
                    case RemoteConfigValueType.Bool:
                        var boolEntry = (RemoteConfigBoolEntry)entry;
                        if (!_runtimeBoolValues.TryAdd(boolEntry.key, boolEntry.value))
                        {
                            Debug.LogError($"--- REMOTE CONFIG --- bool dictionary already contains the key {entry.key} !");
                        }
                        break;
                    
                    case RemoteConfigValueType.String:
                        var stringEntry = (RemoteConfigStringEntry)entry;
                        if (!_runtimeStringValues.TryAdd(stringEntry.key, stringEntry.value))
                        {
                            Debug.LogError($"--- REMOTE CONFIG --- string dictionary already contains the key {entry.key} !");
                        }
                        break;
                    
                    case RemoteConfigValueType.Translatable:
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void LoadFrom(RemoteConfigData rc)
        {
            existingLanguages = rc.allLanguages;

            _runtimeIntValues = rc.intEntries.ToDictionary(x => x.Key, x => x.Value.value);
            _runtimeBoolValues = rc.boolEntries.ToDictionary(x => x.Key, x => x.Value.value);
            _runtimeStringValues = rc.stringEntries.ToDictionary(x => x.Key, x => x.Value.value);
            _runtimeFloatValues = rc.floatEntries.ToDictionary(x => x.Key, x => x.Value.value);

            LoadPlatformSettings(rc);
            SelectLanguage(_currentlySelectedLanguage ?? defaultLanguage);
        }

        #endregion

        #region Language Specific Methods

        /// <summary>
        /// Can the provided key be translated ?
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key is present in the string dictionary. False otherwise.</returns>
        public bool CanTranslate(string key)
        {
            return _runtimeStringValues.ContainsKey(key);
        }

        /// <summary>
        /// Refresh the translation dictionary to match the values for the desired language.
        /// </summary>
        /// <param name="lang"></param>
        public void SelectLanguage(SystemLanguage lang)
        {
            if (_currentlyTranslatedLanguage == lang)
            {
                Debug.Log($"--- REMOTE CONFIG --- Language {lang.ToString()} is already the selected language.");
                return;
            }

            _currentlySelectedLanguage = lang;

            if (_runtimeRc == null)
            {
                return;
            }

            SetTranslatableValuesForLanguage(lang);
        }

        private void SetTranslatableValuesForLanguage(SystemLanguage lang)
        {
            foreach (var key in _runtimeRc.translatableEntries.Keys)
            {
                if (!_runtimeRc.translatableEntries[key].value.ContainsKey(lang))
                {
                    Debug.LogError($"--- REMOTE CONFIG --- Missing language {lang.ToString()} for entry {key} !");
                    continue;
                }

                _runtimeStringValues[key] = _runtimeRc.translatableEntries[key].value[lang];
            }

            _currentlyTranslatedLanguage = lang;
        }

        #endregion

        #region Data Access Methods

        public bool GetBool(string key)
        {
            if (!_runtimeBoolValues.ContainsKey(key))
            {
                Debug.LogWarning($"--- REMOTE CONFIG --- Key {key} is not present in the boolean dictionary !");
                return false;
            }
            
#if UNITY_EDITOR
            TrackKeyUsage(key);
#endif
            
            return _runtimeBoolValues[key];
        }

        public float GetFloat(string key)
        {
            if (!_runtimeFloatValues.ContainsKey(key))
            {
                Debug.LogWarning($"--- REMOTE CONFIG --- Key {key} is not present in the float dictionary !");
                return 0f;
            }
            
#if UNITY_EDITOR
            TrackKeyUsage(key);
#endif

            return _runtimeFloatValues[key];
        }
        
        public string GetString(string key)
        {
            if (!_runtimeStringValues.ContainsKey(key))
            {
                Debug.LogWarning($"--- REMOTE CONFIG --- Key {key} is not present in the string dictionary !");
                return string.Empty;
            }
            
#if UNITY_EDITOR
            TrackKeyUsage(key);
#endif

            return _runtimeStringValues[key];
        }
        
        public int GetInt(string key)
        {
            if (!_runtimeIntValues.ContainsKey(key))
            {
                Debug.LogWarning($"--- REMOTE CONFIG --- Key {key} is not present in the int dictionary !");
                return -1;
            }
            
#if UNITY_EDITOR
            TrackKeyUsage(key);
#endif

            return _runtimeIntValues[key];
        }

        #endregion
    }
}

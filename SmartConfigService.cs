using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [CreateAssetMenu(fileName = "RemoteConfigService", menuName = "CCLB Studio/Remote Config/Service SO")]
    public class SmartConfigService : ScriptableObject
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
        public List<SystemLanguage> ExistingLanguages => existingLanguages;
        public const string FileName = "RemoteConfig.json";
        public RemoteConfigTransferStrategy TransferStrategy => transferStrategy;

        [Tooltip("If TRUE, the service with automatically initialize itself during the OnEnable event.")]
        [SerializeField] private bool autoInitialize = true;
        [Tooltip("Determine what action will be performed when the service is initialized. None = nothing, LoadFromCloud = download the remote file and load it, LoadFromLocalFile = loading from the local json file.")]
        [SerializeField] private InitializeAction onInitialized = InitializeAction.None;
        [Tooltip("The script holding your logic to upload/download your json file. See documentation for more information.")]
        [SerializeField] private RemoteConfigTransferStrategy transferStrategy;
        [Tooltip("The default language to use if none has been specified with the SelectLanguage() method.")]
        [SerializeField] private SystemLanguage defaultLanguage = SystemLanguage.English;
        [Tooltip("You local json file.")]
        [SerializeField] private TextAsset localTranslationFile;
        [Tooltip("A list holding all the languages present in your last loaded json file. Purely informative.")]
        [SerializeField] private List<SystemLanguage> existingLanguages;

        [NonSerialized] private Dictionary<string, int> _runtimeIntValues;
        [NonSerialized] private Dictionary<string, float> _runtimeFloatValues;
        [NonSerialized] private Dictionary<string, bool> _runtimeBoolValues;
        [NonSerialized] private Dictionary<string, string> _runtimeStringValues;
        [NonSerialized] private RemoteConfigData _runtimeRc;
        [NonSerialized] private SystemLanguage? _currentlySelectedLanguage;
        [NonSerialized] private SystemLanguage _currentlyTranslatedLanguage;
        [NonSerialized] private List<IRemoteConfigListener> _listeners;

        private enum InitializeAction {None, LoadFromCloud, LoadFromLocalFile}

        #region Unity Events

        private void OnEnable()
        {
            if (autoInitialize)
            {
                Initialize();
            }
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            _runtimeIntValues = new Dictionary<string, int>();
            _runtimeFloatValues = new Dictionary<string, float>();
            _runtimeBoolValues = new Dictionary<string, bool>();
            _runtimeStringValues = new Dictionary<string, string>();
            _listeners = new List<IRemoteConfigListener>();

            _runtimeRc = null;
            _currentlySelectedLanguage = null;
            _currentlyTranslatedLanguage = SystemLanguage.Unknown;

            switch (onInitialized)
            {
                case InitializeAction.None:
                    break;
                
                case InitializeAction.LoadFromCloud:
                    LoadFromCloud(null, null, null);
                    break;
                
                case InitializeAction.LoadFromLocalFile:
                    LoadFromLocalFile();
                    break;
            }
        }

        #endregion

        #region Loading Methods

        public void LoadFromCloud(Action<float> onProgress, Action onSuccess, Action onFail)
        {
            if (!transferStrategy)
            {
                Debug.LogError("No transfer strategy ! Unable to download !");
                onFail?.Invoke();
                return;
            }
            
            transferStrategy.DownloadJson(onProgress
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
            
            LoadFromLocalFile();
            return true;
        }

        public void LoadFromLocalFile()
        {
            if (!localTranslationFile)
            {
                Debug.LogError("--- REMOTE CONFIG --- No local translation file, unable to load local remote config !");
                return;
            }
            
            _runtimeRc = new RemoteConfigData(localTranslationFile.text);
            LoadFrom(_runtimeRc);
        }

        private void LoadPlatformSettings(RemoteConfigData rc)
        {
            if (!rc.platformEntries.ContainsKey(Application.platform))
            {
                Debug.Log($"--- REMOTE CONFIG --- No platform settings form current platform {Application.platform} !");
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
            NotifyRemoteConfigLoaded();
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
            NotifyRemoteConfigLanguageSelected();
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

        public bool GetBool(string key, out bool value)
        {
            if (!_runtimeBoolValues.ContainsKey(key))
            {
                Debug.LogWarning($"--- REMOTE CONFIG --- Key {key} is not present in the boolean dictionary !");
                value = false;
                return false;
            }
            
#if UNITY_EDITOR
            TrackKeyUsage(key);
#endif
            
            value = _runtimeBoolValues[key];
            return true;
        }

        public bool GetFloat(string key, out float value)
        {
            if (!_runtimeFloatValues.ContainsKey(key))
            {
                Debug.LogWarning($"--- REMOTE CONFIG --- Key {key} is not present in the float dictionary !");
                value = -1f;
                return false;
            }
            
#if UNITY_EDITOR
            TrackKeyUsage(key);
#endif

            value = _runtimeFloatValues[key];
            return true;
        }
        
        public bool GetString(string key, out string value)
        {
            if (!_runtimeStringValues.ContainsKey(key))
            {
                Debug.LogWarning($"--- REMOTE CONFIG --- Key {key} is not present in the string dictionary !");
                value = string.Empty;
                return false;
            }
            
#if UNITY_EDITOR
            TrackKeyUsage(key);
#endif

            value = _runtimeStringValues[key];
            return true;
        }
        
        public bool GetInt(string key, out int value)
        {
            if (!_runtimeIntValues.ContainsKey(key))
            {
                Debug.LogWarning($"--- REMOTE CONFIG --- Key {key} is not present in the int dictionary !");
                value = -1;
                return false;
            }
            
#if UNITY_EDITOR
            TrackKeyUsage(key);
#endif

            value = _runtimeIntValues[key];
            return true;
        }

        #endregion

        #region Listener Methods

        public void AddListener(IRemoteConfigListener l)
        {
            if (!_listeners.Contains(l))
            {
                _listeners.Add(l);
            }
        }

        public void RemoveListener(IRemoteConfigListener l)
        {
            int i = _listeners.FindIndex(x => x == l);
            if (i >= 0)
            {
                _listeners.RemoveAt(i);
            }
        }

        private void NotifyRemoteConfigLoaded()
        {
            foreach (var l in _listeners)
            {
                l.OnRemoteConfigLoaded();
            }
        }

        private void NotifyRemoteConfigLanguageSelected()
        {
            foreach (var l in _listeners)
            {
                l.OnRemoteConfigLanguageSelected();
            }
        }

        #endregion
    }
}

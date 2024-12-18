using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CCLBStudio.SmartConfig
{
    public class SmartConfigEditorData : ScriptableObject
    {
        public SmartConfigEditWindowSettings settings;
        public List<SmartConfigEditorLanguage> allLanguages= new ();
        public List<SmartConfigKeyValuePair<string, string>> allCategories = new ();
        public List<SmartConfigKeyValuePair<RuntimePlatform, List<SmartConfigEditorEntry>>> platformEntries= new ();
        public List<SmartConfigEditorEntry> allAppEntries= new ();
        [SerializeField] private SmartConfigService currentlySelectedService;

        [NonSerialized] private Dictionary<string, SmartConfigEditorEntry> _allValidAppEntries;
        [NonSerialized] private Dictionary<RuntimePlatform, Dictionary<string, SmartConfigEditorEntry>> _allValidPlatformEntries;

        [NonSerialized] private Dictionary<string, List<SmartConfigEditorEntry>> _categoryEntries;

        [NonSerialized] private List<SmartConfigEditorEntry> _appIntEntries;
        [NonSerialized] private List<SmartConfigEditorEntry> _appFloatEntries;
        [NonSerialized] private List<SmartConfigEditorEntry> _appBoolEntries;
        [NonSerialized] private List<SmartConfigEditorEntry> _appStringEntries;
        [NonSerialized] private List<SmartConfigEditorEntry> _appTranslatableEntries;
        [NonSerialized] private Dictionary<SmartConfigEditorEntry, int> _appEntriesIndexes;


        #region Initialization Methods

        public void Initialize()
        {
            if (!settings)
            {
                settings = ScEditorExtender.LoadScriptableAsset<SmartConfigEditWindowSettings>();
            }
            
            foreach (var editorEntry in allAppEntries.Concat(platformEntries.SelectMany(pair => pair.Value)))
            {
                editorEntry.Refresh();
            }
            
            BuildAll();
        }

        public void LoadFrom(SmartConfigData sc)
        {
            allLanguages = new List<SmartConfigEditorLanguage>(sc.allLanguages.Count);
            platformEntries = new List<SmartConfigKeyValuePair<RuntimePlatform, List<SmartConfigEditorEntry>>>();
            allAppEntries = new List<SmartConfigEditorEntry>(sc.allEntries.Count);
            
            allCategories.RemoveAll(x => !sc.allCategories.Contains(x.Key));
            foreach (var category in sc.allCategories)
            {
                int index = allCategories.FindIndex(x => x.Key == category);
                if (index < 0)
                {
                    allCategories.Add(new SmartConfigKeyValuePair<string, string>(category, string.Empty));
                }
            }

            foreach (var lang in sc.allLanguages)
            {
                allLanguages.Add(new SmartConfigEditorLanguage
                {
                    language = lang,
                    flag = settings.languageFlags.Find(x => x.Key == lang)?.Value,
                    twoLettersIsoDisplay = lang.ToCultureInfo().TwoLetterISOLanguageName.ToUpper(),
                    languageName = ObjectNames.NicifyVariableName(lang.ToString())
                });
            }

            foreach (var platformEntry in sc.platformEntries)
            {
                List<SmartConfigEditorEntry> editorEntries = new List<SmartConfigEditorEntry>(platformEntry.Value.Count);
                foreach (var entry in platformEntry.Value)
                {
                    var editorEntry = new SmartConfigEditorEntry(entry, this);
                    editorEntries.Add(editorEntry);
                }

                platformEntries.Add(new SmartConfigKeyValuePair<RuntimePlatform, List<SmartConfigEditorEntry>>(platformEntry.Key, editorEntries));
            }
            
            foreach (var entry in sc.allEntries)
            {
                var editorEntry = new SmartConfigEditorEntry(entry, this);
                allAppEntries.Add(editorEntry);
            }

            allAppEntries.TrimExcess();
            BuildAll();
            WriteOnDisk();
        }

        private void BuildAll()
        {
            BuildAndCheckValidAppEntries();
            BuildAndCheckValidPlatformEntries();
            BuildAndCheckCategoryEntries();
            
            BuildAppEntryIndexes();
            BuildAllFilteredEntries();
        }

        #endregion

        #region Service Selection Methods

        public void NotifyNewServiceSelected(SmartConfigService service)
        {
            currentlySelectedService = service;
        }

        #endregion

        #region Json Write Methods

        public void WriteJson()
        {
            if (!currentlySelectedService)
            {
                EditorUtility.DisplayDialog("Current Service Is Null !", "No current service selected. This is not supposed to happen. You can manually fill the \"Current Service\" field of this object or close and reopen the editor window.", "Ok");
                return;
            }

            if (allAppEntries.Any(x => x.isValid == false))
            {
                if (EditorUtility.DisplayDialog("Bad Key Found", "Some app entry keys are not valid. You should correct those keys before writing your json files, otherwise they won't be taken into consideration.",
                        "Abort Json Generation", "Continue Anyway"))
                {
                    return;
                }
            }
            
            SmartConfigJson jsonData = new SmartConfigJson
            {
                platforms = new List<SmartConfigPlatformEntryJson>(platformEntries.Count),
                entries = new List<SmartConfigEntryJson>(allAppEntries.Count),
                version = 1 // tmp
            };
            foreach (var pair in platformEntries)
            {
                var platformConfig = new SmartConfigPlatformEntryJson
                {
                    platform = pair.Key.ToString(),
                    entries = new List<SmartConfigEntryJson>(pair.Value.Count)
                };

                foreach (SmartConfigEditorEntry editorEntry in pair.Value)
                {
                    platformConfig.entries.Add(editorEntry.ToSmartConfigJson());
                }
                
                jsonData.platforms.Add(platformConfig);
            }

            foreach (var editorEntry in allAppEntries.Where(x => x.isValid))
            {
                jsonData.entries.Add(editorEntry.ToSmartConfigJson());
            }

            string json = JsonUtility.ToJson(jsonData, true);
            string absolutePath = GetJsonFileAbsolutePath(currentlySelectedService.LocalTranslationFile);
            File.WriteAllText(absolutePath, json);
            
            WriteOnDisk();
            AssetDatabase.Refresh();
        }

        #endregion

        #region Json Upload / Download Methods

        public void UploadJson(Action onUploadSucceeded, Action<float> onProgress, Action onUploadFailed)
        {
            if (!currentlySelectedService)
            {
                EditorUtility.DisplayDialog("Current Service Is Null !", "No current service selected. This is not supposed to happen. You can manually fill the \"Current Service\" field of the SmartConfigEditorData object or close and reopen the editor window.", "Ok");
                Debug.LogError("Current service is null.");
                onUploadFailed?.Invoke();
                return;
            }

            currentlySelectedService.TransferStrategy.UploadJson(currentlySelectedService.LocalTranslationFile.text, onProgress, onUploadSucceeded, onUploadFailed);
        }

        public void DownloadJson(Action onDataRefreshed, Action<float> onProgress, Action onDownloadFailed)
        {
            if (!currentlySelectedService)
            {
                EditorUtility.DisplayDialog("Current Service Is Null !", "No current service selected. This is not supposed to happen. You can manually fill the \"Current Service\" field of the SmartConfigEditorData object or close and reopen the editor window.", "Ok");
                Debug.LogError("Current service is null.");
                onDownloadFailed?.Invoke();
                return;
            }

            currentlySelectedService.TransferStrategy.DownloadJson(onProgress
                , json => { OnSmartJsonFetched(json, onDataRefreshed); }
                , () => OnSmartJsonDownloadFailed(onDownloadFailed));
        }

        private void OnSmartJsonFetched(string json, Action onDataRefreshed)
        {
            var sc = new SmartConfigData(json);
            LoadFrom(sc);
            WriteJson();
            onDataRefreshed?.Invoke();
        }

        private void OnSmartJsonDownloadFailed(Action onFail)
        {
            Debug.LogError("Unable to fetch the remote json.");
            onFail?.Invoke();
        }

        #endregion

        #region Categories Methods

        public bool TryAddNewCategory(string newCategory)
        {
            if (allCategories.Any(x => x.Key == newCategory))
            {
                return false;
            }
            
            allCategories.Add(new SmartConfigKeyValuePair<string, string>(newCategory, string.Empty));

            if (_categoryEntries == null)
            {
                BuildAndCheckCategoryEntries();
            }
            else
            {
                _categoryEntries.Add(newCategory, new List<SmartConfigEditorEntry>());
            }
            
            SetDirty();
            return true;
        }

        public void NotifyEntryCategoryChanged(SmartConfigEditorEntry editorEntry, int index)
        {
            if (index <= 0)
            {
                ResetEntryCategory(editorEntry);
                SetDirty();
                return;
            }
            
            editorEntry.categoryIndex = index;
            editorEntry.category = allCategories[index - 1].Key;

            BuildAndCheckCategoryEntries();
            SetDirty();
        }

        public void NotifyMultipleEntriesCategoryChanged(List<SmartConfigEditorEntry> editorEntries, int index)
        {
            foreach (var editorEntry in editorEntries)
            {
                if (index <= 0)
                {
                    ResetEntryCategory(editorEntry);
                }
                else
                {
                    editorEntry.categoryIndex = index;
                    editorEntry.category = allCategories[index - 1].Key;
                }
            }
            
            BuildAndCheckCategoryEntries();
            SetDirty();
        }

        public void NotifyCategoryPrefixChanged(int index)
        {
            if (_categoryEntries == null)
            {
                BuildAndCheckCategoryEntries();
            }
            else if (_categoryEntries!.TryGetValue(allCategories[index].Key, out var categoryEntries))
            {
                foreach (var editorEntry in categoryEntries)
                {
                    CheckEntryCategoryPrefix(editorEntry);
                }
            }

            SetDirty();
        }
        
        public void NotifyCategoryDeleted(int index)
        {
            if (_categoryEntries == null)
            {
                BuildAndCheckCategoryEntries();
            }

            if (_categoryEntries!.TryGetValue(allCategories[index].Key, out var categoryEntries))
            {
                foreach (var editorEntry in categoryEntries)
                {
                    ResetEntryCategory(editorEntry);
                }

                _categoryEntries.Remove(allCategories[index].Key);
            }
            
            allCategories.RemoveAt(index);

            foreach (var categoryEntriesPair in _categoryEntries)
            {
                int newIndex = allCategories.FindIndex(x => x.Key == categoryEntriesPair.Key);
                foreach (var editorEntry in categoryEntriesPair.Value)
                {
                    editorEntry.categoryIndex = newIndex + 1;
                }
            }
            
            SetDirty();
        }

        private void CheckEntryCategoryPrefix(SmartConfigEditorEntry editorEntry)
        {
            if (string.IsNullOrEmpty(editorEntry.category) || editorEntry.categoryIndex <= 0)
            {
                ResetEntryCategory(editorEntry);
                return;
            }

            if (allCategories[editorEntry.categoryIndex - 1].Key != editorEntry.category)
            {
                Debug.LogError($"Problem with category for entry {editorEntry.key}. The entry category do not match the relative category at provided index. Resetting entry category...");
                ResetEntryCategory(editorEntry);
                return;
            }

            editorEntry.respectCategoryPrefix = !string.IsNullOrEmpty(allCategories[editorEntry.categoryIndex - 1].Value) && editorEntry.key.StartsWith(allCategories[editorEntry.categoryIndex - 1].Value);
        }
        
        private void BuildAndCheckCategoryEntries()
        {
            _categoryEntries = new Dictionary<string, List<SmartConfigEditorEntry>>(allCategories.Count);
            var allEntriesWithCategory = allAppEntries.Where(x => !string.IsNullOrEmpty(x.category));
            Dictionary<string, int> categoryIndexes = new Dictionary<string, int>();

            foreach (var editorEntry in allEntriesWithCategory)
            {
                if (!_categoryEntries.ContainsKey(editorEntry.category))
                {
                    _categoryEntries.Add(editorEntry.category, new List<SmartConfigEditorEntry>());
                }
                
                _categoryEntries[editorEntry.category].Add(editorEntry);
                categoryIndexes.TryAdd(editorEntry.category, Mathf.Max(0, allCategories.FindIndex(x => x.Key == editorEntry.category)));
                editorEntry.categoryIndex = categoryIndexes[editorEntry.category] + 1;
                
                CheckEntryCategoryPrefix(editorEntry);
            }
        }

        private void ResetEntryCategory(SmartConfigEditorEntry editorEntry)
        {
            editorEntry.categoryIndex = 0;
            editorEntry.category = string.Empty;
            editorEntry.respectCategoryPrefix = true;
        }

        #endregion

        #region Language Methods

        public void NotifyLanguageAdded(SystemLanguage newLanguage)
        {
            var newLang = new SmartConfigEditorLanguage
            {
                language = newLanguage,
                flag = settings.languageFlags.Find(x => x.Key == newLanguage)?.Value,
                twoLettersIsoDisplay = newLanguage.ToCultureInfo().TwoLetterISOLanguageName.ToUpper(),
                languageName = ObjectNames.NicifyVariableName(newLanguage.ToString())
            };
            
            allLanguages.Add(newLang);

            foreach (var entry in allAppEntries)
            {
                entry.NotifyLanguageAdded(newLang);
            }
            
            SetDirty();
        }

        public void RemoveLanguage(int index)
        {
            foreach (var entry in allAppEntries)
            {
                entry.NotifyLanguageRemoved(allLanguages[index]);
            }
            
            allLanguages.RemoveAt(index);
            SetDirty();
        }

        #endregion

        #region Platform Methods

        public void NotifyRemovePlatformEntry(RuntimePlatform platform, int index)
        {
            int i = platformEntries.FindIndex(x => x.Key == platform);
            if (i < 0)
            {
                Debug.LogError($"There is no platform data for platform {platform.ToString()}");
                return;
            }

            var entries = platformEntries[i].Value;

            if (index < 0 || index >= entries.Count)
            {
                Debug.LogError($"Index {index} is not valid. Min = 0, max = {(entries.Count - 1).ToString()}");
                return;
            }

            entries.RemoveAt(index);
            BuildAndCheckValidPlatformEntries();
            SetDirty();
        }

        public void AddNewPlatform(RuntimePlatform newPlatform)
        {
            platformEntries.Add(new SmartConfigKeyValuePair<RuntimePlatform, List<SmartConfigEditorEntry>>(newPlatform, new List<SmartConfigEditorEntry>
            {
                new SmartConfigEditorEntry(SmartConfigValueType.String, this)
                {
                    key = "app_prod_versions",
                    stringValue = "0.1.0"
                },
                new SmartConfigEditorEntry(SmartConfigValueType.String, this)
                {
                    key = "app_review_versions",
                    stringValue = "0.0.0"
                },
                new SmartConfigEditorEntry(SmartConfigValueType.Bool, this)
                {
                    key = "app_maintenance_mode_enabled",
                    boolValue = false
                },
                new SmartConfigEditorEntry(SmartConfigValueType.String, this)
                {
                    key = "app_update_url",
                    stringValue = string.Empty
                },
            }));
            
            SetDirty();
        }

        public void RemovePlatform(RuntimePlatform platform)
        {
            if (EditorUtility.DisplayDialog("Platform Deletion Confirmation", $"You are about to delete platform {platform}. Are you sure ?", "Yes", "Cancel"))
            {
                int index = platformEntries.FindIndex(x => x.Key == platform);
                if(index >= 0)
                {
                    platformEntries.RemoveAt(index);
                    SetDirty();
                }
            }
        }

        public void AddNewPlatformEntry(RuntimePlatform platform)
        {
            int index = platformEntries.FindIndex(x => x.Key == platform);
            if (index < 0)
            {
                return;
            }

            var entries = platformEntries[index].Value;

            SmartConfigEditorEntry newEntry = new SmartConfigEditorEntry(SmartConfigValueType.String, this);
            entries.Add(newEntry);
            CheckNewPlatformEntryValidity(platform, newEntry);
            SetDirty();
        }

        #endregion

        #region App Entries Methods

        private void BuildAndCheckValidAppEntries()
        {
            _allValidAppEntries = new Dictionary<string, SmartConfigEditorEntry>(allAppEntries.Count);

            foreach (var entry in allAppEntries)
            {
                CheckNewAppEntryValidity(entry);
            }
        }

        private void BuildAndCheckValidPlatformEntries()
        {
            _allValidPlatformEntries = new Dictionary<RuntimePlatform, Dictionary<string, SmartConfigEditorEntry>>(platformEntries.Count);
            
            foreach (var pair in platformEntries)
            {
                _allValidPlatformEntries[pair.Key] = new Dictionary<string, SmartConfigEditorEntry>(pair.Value.Count);
                foreach (var editorEntry in pair.Value)
                {
                    CheckNewPlatformEntryValidity(pair.Key, editorEntry);
                }
            }
        }

        private void CheckNewAppEntryValidity(SmartConfigEditorEntry editorEntry)
        {
            if (_allValidAppEntries == null)
            {
                BuildAndCheckValidAppEntries();
            }
            
            if (_allValidAppEntries!.ContainsKey(editorEntry.key) || string.IsNullOrEmpty(editorEntry.key))
            {
                editorEntry.isValid = false;
                return;
            }

            editorEntry.isValid = true;
            _allValidAppEntries.Add(editorEntry.key, editorEntry);
        }

        private void CheckNewPlatformEntryValidity(RuntimePlatform platform, SmartConfigEditorEntry editorEntry)
        {
            if (_allValidPlatformEntries == null)
            {
                BuildAndCheckValidPlatformEntries();
            }

            if (!_allValidPlatformEntries!.ContainsKey(platform))
            {
                Debug.LogError($"Unable to check validity for platform {platform} since this platform is not in the dictionary.");
                return;
            }

            var platformDictionary = _allValidPlatformEntries[platform];
            if (platformDictionary.ContainsKey(editorEntry.key) || string.IsNullOrEmpty(editorEntry.key))
            {
                editorEntry.isValid = false;
                return;
            }

            editorEntry.isValid = true;
            platformDictionary.Add(editorEntry.key, editorEntry);
        }

        private void CheckAppEntryKey(string oldKey, SmartConfigEditorEntry editorEntry)
        {
            if (_allValidAppEntries == null)
            {
                BuildAndCheckValidAppEntries();
            }

            if (_allValidAppEntries!.ContainsKey(oldKey))
            {
                if (_allValidAppEntries[oldKey] == editorEntry)
                {
                    _allValidAppEntries.Remove(oldKey);
                }
                
                CheckNewAppEntryValidity(editorEntry);
            }
            else
            {
                CheckNewAppEntryValidity(editorEntry);
            }
        }

        private void CheckPlatformEntryKey(RuntimePlatform platform,  string oldKey, SmartConfigEditorEntry editorEntry)
        {
            if (_allValidPlatformEntries == null)
            {
                BuildAndCheckValidPlatformEntries();
            }

            if (!_allValidPlatformEntries!.ContainsKey(platform))
            {
                Debug.LogError($"Unable to check key validity for platform {platform} since this platform is not in the dictionary.");
                return;
            }
            
            var platformDictionary = _allValidPlatformEntries[platform];
            if (platformDictionary!.ContainsKey(oldKey))
            {
                if (platformDictionary[oldKey] == editorEntry)
                {
                    platformDictionary.Remove(oldKey);
                }
                
                CheckNewPlatformEntryValidity(platform, editorEntry);
            }
            else
            {
                CheckNewPlatformEntryValidity(platform, editorEntry);
            }
        }

        public void NotifyAppEntryKeyChanged(string oldKey, SmartConfigEditorEntry editorEntry)
        {
            CheckAppEntryKey(oldKey, editorEntry);
            CheckEntryCategoryPrefix(editorEntry);
            SetDirty();
        }

        public void NotifyPlatformEntryKeyChanged(RuntimePlatform platform, string oldKey, SmartConfigEditorEntry editorEntry)
        {
            CheckPlatformEntryKey(platform, oldKey, editorEntry);
            SetDirty();
        }

        public void NotifyEntryTypeChanged(SmartConfigEditorEntry editorEntry)
        {
            editorEntry.NotifyTypeChange();
            SetDirty();
        }

        public void NotifyNewAppEntryAdded(SmartConfigValueType type)
        {
            SmartConfigEditorEntry newEntry = new SmartConfigEditorEntry(type, this);
            allAppEntries.Add(newEntry);

            AddNewEntryIndex(newEntry);
            CheckNewAppEntryValidity(newEntry);
            DirtyAllFilterEntries();
            SetDirty();
        }

        public void NotifyAppEntryDeleted(SmartConfigEditorEntry editorEntry)
        {
            int index = GetAppEntryIndex(editorEntry);
            if (index >= 0)
            {
                allAppEntries.RemoveAt(index);
            }
            
            BuildAppEntryIndexes();
            BuildAndCheckValidAppEntries();
            DirtyAllFilterEntries();
            SetDirty();
        }

        #endregion

        #region Index Methods

        private int GetAppEntryIndex(SmartConfigEditorEntry editorEntry)
        {
            if (_appEntriesIndexes == null)
            {
                BuildAppEntryIndexes();
            }

            return _appEntriesIndexes!.TryGetValue(editorEntry, out int index) ? index : -1;
        }

        private void BuildAppEntryIndexes()
        {
            _appEntriesIndexes = new Dictionary<SmartConfigEditorEntry, int>(allAppEntries.Count);

            for (int i = 0; i < allAppEntries.Count; i++)
            {
                _appEntriesIndexes[allAppEntries[i]] = i;
            }
        }

        private void AddNewEntryIndex(SmartConfigEditorEntry editorEntry)
        {
            if (_appEntriesIndexes == null)
            {
                return;
            }

            if (_appEntriesIndexes.ContainsKey(editorEntry))
            {
                Debug.LogError("New editor entry is already present in the index dictionary !");
                return;
            }
            
            _appEntriesIndexes.Add(editorEntry, allAppEntries.Count - 1);
        }

        #endregion

        #region Filter Methods

        public List<SmartConfigEditorEntry> GetAppEntriesForFilter(SmartConfigValueTypeFilter filter)
        {
            return filter switch
            {
                SmartConfigValueTypeFilter.None => allAppEntries,
                SmartConfigValueTypeFilter.Int => GetAppIntEntries(),
                SmartConfigValueTypeFilter.Float => GetAppFloatEntries(),
                SmartConfigValueTypeFilter.Bool => GetAppBoolEntries(),
                SmartConfigValueTypeFilter.String => GetAppStringEntries(),
                SmartConfigValueTypeFilter.Translatable => GetAppTranslatableEntries(),
                _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
            };
        }

        public void DirtyAllFilterEntries()
        {
            _appIntEntries = null;
            _appFloatEntries = null;
            _appBoolEntries = null;
            _appStringEntries = null;
            _appTranslatableEntries = null;
        }

        private void BuildAllFilteredEntries()
        {
            _appIntEntries = new List<SmartConfigEditorEntry>();
            _appFloatEntries = new List<SmartConfigEditorEntry>();
            _appBoolEntries = new List<SmartConfigEditorEntry>();
            _appStringEntries = new List<SmartConfigEditorEntry>();
            _appTranslatableEntries = new List<SmartConfigEditorEntry>();

            foreach (var editorEntry in allAppEntries)
            {
                switch (editorEntry.type)
                {
                    case SmartConfigValueType.Int:
                        _appIntEntries.Add(editorEntry);
                        break;
                    
                    case SmartConfigValueType.Float:
                        _appFloatEntries.Add(editorEntry);
                        break;
                    
                    case SmartConfigValueType.Bool:
                        _appBoolEntries.Add(editorEntry);
                        break;
                    
                    case SmartConfigValueType.String:
                        _appStringEntries.Add(editorEntry);
                        break;
                    
                    case SmartConfigValueType.Translatable:
                        _appTranslatableEntries.Add(editorEntry);
                        break;
                }
            }
        }

        private List<SmartConfigEditorEntry> GetAppIntEntries()
        {
            return _appIntEntries ??= allAppEntries.Where(x => x.type == SmartConfigValueType.Int).ToList();
        }

        private List<SmartConfigEditorEntry> GetAppFloatEntries()
        {
            return _appFloatEntries ??= allAppEntries.Where(x => x.type == SmartConfigValueType.Float).ToList();
        }
        
        private List<SmartConfigEditorEntry> GetAppBoolEntries()
        {
            return _appBoolEntries ??= allAppEntries.Where(x => x.type == SmartConfigValueType.Bool).ToList();
        }
        
        private List<SmartConfigEditorEntry> GetAppStringEntries()
        {
            return _appStringEntries ??= allAppEntries.Where(x => x.type == SmartConfigValueType.String).ToList();
        }
        
        private List<SmartConfigEditorEntry> GetAppTranslatableEntries()
        {
            return _appTranslatableEntries ??= allAppEntries.Where(x => x.type == SmartConfigValueType.Translatable).ToList();
        }

        #endregion

        #region Tools

        public new void SetDirty()
        {
            EditorUtility.SetDirty(this);
        }

        public bool IsDirty()
        {
            return EditorUtility.IsDirty(this);
        }

        public void WriteOnDisk()
        {
            SetDirty();
            AssetDatabase.SaveAssetIfDirty(this);
        }

        private string GetJsonFileAbsolutePath(Object localFileAsset)
        {
            string relativePath = AssetDatabase.GetAssetPath(localFileAsset);
            return ScIOExtender.RelativeToAbsolutePath(relativePath);
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ReaaliStudio.Utils.Extensions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CCLBStudio.RemoteConfig
{
    public class RemoteConfigEditorData : ScriptableObject
    {
        public RemoteConfigEditWindowSettings settings;
        public List<RemoteConfigEditorLanguage> allLanguages;
        public List<RemoteConfigKeyValuePair<string, string>> allCategories = new ();
        public List<RemoteConfigKeyValuePair<RuntimePlatform, List<RemoteConfigEditorEntry>>> platformEntries;
        public List<RemoteConfigEditorEntry> allAppEntries;
        
        [NonSerialized] private Dictionary<string, RemoteConfigEditorEntry> _allValidAppEntries;
        [NonSerialized] private Dictionary<RuntimePlatform, Dictionary<string, RemoteConfigEditorEntry>> _allValidPlatformEntries;

        [NonSerialized] private Dictionary<string, List<RemoteConfigEditorEntry>> _categoryEntries;

        [NonSerialized] private List<RemoteConfigEditorEntry> _appIntEntries;
        [NonSerialized] private List<RemoteConfigEditorEntry> _appFloatEntries;
        [NonSerialized] private List<RemoteConfigEditorEntry> _appBoolEntries;
        [NonSerialized] private List<RemoteConfigEditorEntry> _appStringEntries;
        [NonSerialized] private List<RemoteConfigEditorEntry> _appTranslatableEntries;
        [NonSerialized] private Dictionary<RemoteConfigEditorEntry, int> _appEntriesIndexes;

        #region Initialization Methods

        public void Initialize()
        {
            if (!settings)
            {
                settings = EditorExtender.LoadScriptableAsset<RemoteConfigEditWindowSettings>();
            }
            
            foreach (var editorEntry in allAppEntries.Concat(platformEntries.SelectMany(pair => pair.Value)))
            {
                editorEntry.Refresh();
            }
            
            BuildAll();
        }

        public void LoadFrom(RemoteConfigData rc)
        {
            allLanguages = new List<RemoteConfigEditorLanguage>(rc.allLanguages.Count);
            platformEntries = new List<RemoteConfigKeyValuePair<RuntimePlatform, List<RemoteConfigEditorEntry>>>();
            allAppEntries = new List<RemoteConfigEditorEntry>(rc.allEntries.Count);
            
            allCategories.RemoveAll(x => !rc.allCategories.Contains(x.Key));
            foreach (var category in rc.allCategories)
            {
                int index = allCategories.FindIndex(x => x.Key == category);
                if (index < 0)
                {
                    allCategories.Add(new RemoteConfigKeyValuePair<string, string>(category, string.Empty));
                }
            }

            foreach (var lang in rc.allLanguages)
            {
                allLanguages.Add(new RemoteConfigEditorLanguage
                {
                    language = lang,
                    flag = settings.languageFlags.Find(x => x.Key == lang)?.Value
                });
            }

            foreach (var platformEntry in rc.platformEntries)
            {
                List<RemoteConfigEditorEntry> editorEntries = new List<RemoteConfigEditorEntry>(platformEntry.Value.Count);
                foreach (var entry in platformEntry.Value)
                {
                    var editorEntry = new RemoteConfigEditorEntry(entry, this);
                    editorEntries.Add(editorEntry);
                }

                platformEntries.Add(new RemoteConfigKeyValuePair<RuntimePlatform, List<RemoteConfigEditorEntry>>(platformEntry.Key, editorEntries));
            }
            
            foreach (var entry in rc.allEntries)
            {
                var editorEntry = new RemoteConfigEditorEntry(entry, this);
                allAppEntries.Add(editorEntry);
            }

            allAppEntries.TrimExcess();
            BuildAll();
            SetDirty();
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

        #region Json Write Methods

        public void WriteJson()
        {
            var service = GetRcService();
            if (!service)
            {
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
            
            RemoteConfigJson jsonData = new RemoteConfigJson
            {
                platforms = new List<RemoteConfigPlatformEntryJson>(platformEntries.Count),
                entries = new List<RemoteConfigEntryJson>(allAppEntries.Count),
                version = 1 // tmp
            };
            foreach (var pair in platformEntries)
            {
                var platformConfig = new RemoteConfigPlatformEntryJson
                {
                    platform = pair.Key,
                    entries = new List<RemoteConfigEntryJson>(pair.Value.Count)
                };

                foreach (RemoteConfigEditorEntry editorEntry in pair.Value)
                {
                    platformConfig.entries.Add(editorEntry.ToRemoteConfigJson());
                }
                
                jsonData.platforms.Add(platformConfig);
            }

            foreach (var editorEntry in allAppEntries.Where(x => x.isValid))
            {
                jsonData.entries.Add(editorEntry.ToRemoteConfigJson());
            }

            string json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            string absolutePath = GetJsonFileAbsolutePath(service.LocalTranslationFile);
            File.WriteAllText(absolutePath, json);
            
            WriteOnDisk();
            AssetDatabase.Refresh();
        }

        #endregion

        #region Json Upload / Download Methods

        public void UploadJson(Action onUploadSucceeded, Action<float> onProgress, Action onUploadFailed)
        {
            var rcService = GetRcService();
            if (!rcService)
            {
                Debug.LogError("Remote config service is null !");
                return;
            }

            rcService.TransferStrategy.UploadJson(rcService.LocalTranslationFile.text, onProgress, onUploadSucceeded, onUploadFailed);
        }

        public void DownloadJson(Action onDataRefreshed, Action<float> onProgress, Action onDownloadFailed)
        {
            var rcService = GetRcService();
            if (!rcService)
            {
                Debug.LogError("Remote config service is null !");
                return;
            }
            
            rcService.TransferStrategy.DownloadJson(onProgress
                , json => { OnRemoteJsonFetched(json, onDataRefreshed); }
                , () => OnRemoteJsonDownloadFailed(onDownloadFailed));
        }

        private void OnRemoteJsonFetched(string json, Action onDataRefreshed)
        {
            var rc = new RemoteConfigData(json);
            LoadFrom(rc);
            WriteJson();
            onDataRefreshed?.Invoke();
        }

        private void OnRemoteJsonDownloadFailed(Action onFail)
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
            
            allCategories.Add(new RemoteConfigKeyValuePair<string, string>(newCategory, string.Empty));

            if (_categoryEntries == null)
            {
                BuildAndCheckCategoryEntries();
            }
            else
            {
                _categoryEntries.Add(newCategory, new List<RemoteConfigEditorEntry>());
            }
            
            SetDirty();
            return true;
        }

        public void NotifyEntryCategoryChanged(RemoteConfigEditorEntry editorEntry, int index)
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

        private void CheckEntryCategoryPrefix(RemoteConfigEditorEntry editorEntry)
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
            _categoryEntries = new Dictionary<string, List<RemoteConfigEditorEntry>>(allCategories.Count);
            var allEntriesWithCategory = allAppEntries.Where(x => !string.IsNullOrEmpty(x.category));
            Dictionary<string, int> categoryIndexes = new Dictionary<string, int>();

            foreach (var editorEntry in allEntriesWithCategory)
            {
                if (!_categoryEntries.ContainsKey(editorEntry.category))
                {
                    _categoryEntries.Add(editorEntry.category, new List<RemoteConfigEditorEntry>());
                }
                
                _categoryEntries[editorEntry.category].Add(editorEntry);
                categoryIndexes.TryAdd(editorEntry.category, Mathf.Max(0, allCategories.FindIndex(x => x.Key == editorEntry.category)));
                editorEntry.categoryIndex = categoryIndexes[editorEntry.category] + 1;
                
                CheckEntryCategoryPrefix(editorEntry);
            }
        }

        private void ResetEntryCategory(RemoteConfigEditorEntry editorEntry)
        {
            editorEntry.categoryIndex = 0;
            editorEntry.category = string.Empty;
            editorEntry.respectCategoryPrefix = true;
        }

        #endregion

        #region Language Methods

        public void NotifyLanguageAdded(SystemLanguage newLanguage)
        {
            var newLang = new RemoteConfigEditorLanguage
            {
                language = newLanguage,
                flag = settings.languageFlags.Find(x => x.Key == newLanguage)?.Value
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
            //platformEntries.Add(newPlatform, new List<RemoteConfigEditorEntry>
            platformEntries.Add(new RemoteConfigKeyValuePair<RuntimePlatform, List<RemoteConfigEditorEntry>>(newPlatform, new List<RemoteConfigEditorEntry>
            {
                new RemoteConfigEditorEntry(RemoteConfigValueType.String, this)
                {
                    key = "app_prod_versions",
                    stringValue = "0.1.0"
                },
                new RemoteConfigEditorEntry(RemoteConfigValueType.String, this)
                {
                    key = "app_review_versions",
                    stringValue = "0.0.0"
                },
                new RemoteConfigEditorEntry(RemoteConfigValueType.Bool, this)
                {
                    key = "app_maintenance_mode_enabled",
                    boolValue = false
                },
                new RemoteConfigEditorEntry(RemoteConfigValueType.String, this)
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

            RemoteConfigEditorEntry newEntry = new RemoteConfigEditorEntry(RemoteConfigValueType.String, this);
            entries.Add(newEntry);
            CheckNewPlatformEntryValidity(platform, newEntry);
            SetDirty();
        }

        #endregion

        #region App Entries Methods

        private void BuildAndCheckValidAppEntries()
        {
            _allValidAppEntries = new Dictionary<string, RemoteConfigEditorEntry>(allAppEntries.Count);

            foreach (var entry in allAppEntries)
            {
                CheckNewAppEntryValidity(entry);
            }
        }

        private void BuildAndCheckValidPlatformEntries()
        {
            _allValidPlatformEntries = new Dictionary<RuntimePlatform, Dictionary<string, RemoteConfigEditorEntry>>(platformEntries.Count);
            
            foreach (var pair in platformEntries)
            {
                _allValidPlatformEntries[pair.Key] = new Dictionary<string, RemoteConfigEditorEntry>(pair.Value.Count);
                foreach (var editorEntry in pair.Value)
                {
                    CheckNewPlatformEntryValidity(pair.Key, editorEntry);
                }
            }
        }

        private void CheckNewAppEntryValidity(RemoteConfigEditorEntry editorEntry)
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

        private void CheckNewPlatformEntryValidity(RuntimePlatform platform, RemoteConfigEditorEntry editorEntry)
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

        private void CheckAppEntryKey(string oldKey, RemoteConfigEditorEntry editorEntry)
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

        private void CheckPlatformEntryKey(RuntimePlatform platform,  string oldKey, RemoteConfigEditorEntry editorEntry)
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

        public void NotifyAppEntryKeyChanged(string oldKey, RemoteConfigEditorEntry editorEntry)
        {
            CheckAppEntryKey(oldKey, editorEntry);
            CheckEntryCategoryPrefix(editorEntry);
            SetDirty();
        }

        public void NotifyPlatformEntryKeyChanged(RuntimePlatform platform, string oldKey, RemoteConfigEditorEntry editorEntry)
        {
            CheckPlatformEntryKey(platform, oldKey, editorEntry);
            SetDirty();
        }

        public void NotifyEntryTypeChanged(RemoteConfigEditorEntry editorEntry)
        {
            editorEntry.NotifyTypeChange();
            SetDirty();
        }

        public void NotifyNewAppEntryAdded(RemoteConfigValueType type)
        {
            RemoteConfigEditorEntry newEntry = new RemoteConfigEditorEntry(type, this);
            allAppEntries.Add(newEntry);

            AddNewEntryIndex(newEntry);
            CheckNewAppEntryValidity(newEntry);
            DirtyAllFilterEntries();
            SetDirty();
        }

        public void NotifyAppEntryDeleted(RemoteConfigEditorEntry editorEntry)
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

        private int GetAppEntryIndex(RemoteConfigEditorEntry editorEntry)
        {
            if (_appEntriesIndexes == null)
            {
                BuildAppEntryIndexes();
            }

            return _appEntriesIndexes!.TryGetValue(editorEntry, out int index) ? index : -1;
        }

        private void BuildAppEntryIndexes()
        {
            _appEntriesIndexes = new Dictionary<RemoteConfigEditorEntry, int>(allAppEntries.Count);

            for (int i = 0; i < allAppEntries.Count; i++)
            {
                _appEntriesIndexes[allAppEntries[i]] = i;
            }
        }

        private void AddNewEntryIndex(RemoteConfigEditorEntry editorEntry)
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

        public List<RemoteConfigEditorEntry> GetAppEntriesForFilter(RemoteConfigValueTypeFilter filter)
        {
            return filter switch
            {
                RemoteConfigValueTypeFilter.None => allAppEntries,
                RemoteConfigValueTypeFilter.Int => GetAppIntEntries(),
                RemoteConfigValueTypeFilter.Float => GetAppFloatEntries(),
                RemoteConfigValueTypeFilter.Bool => GetAppBoolEntries(),
                RemoteConfigValueTypeFilter.String => GetAppStringEntries(),
                RemoteConfigValueTypeFilter.Translatable => GetAppTranslatableEntries(),
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
            _appIntEntries = new List<RemoteConfigEditorEntry>();
            _appFloatEntries = new List<RemoteConfigEditorEntry>();
            _appBoolEntries = new List<RemoteConfigEditorEntry>();
            _appStringEntries = new List<RemoteConfigEditorEntry>();
            _appTranslatableEntries = new List<RemoteConfigEditorEntry>();

            foreach (var editorEntry in allAppEntries)
            {
                switch (editorEntry.type)
                {
                    case RemoteConfigValueType.Int:
                        _appIntEntries.Add(editorEntry);
                        break;
                    
                    case RemoteConfigValueType.Float:
                        _appFloatEntries.Add(editorEntry);
                        break;
                    
                    case RemoteConfigValueType.Bool:
                        _appBoolEntries.Add(editorEntry);
                        break;
                    
                    case RemoteConfigValueType.String:
                        _appStringEntries.Add(editorEntry);
                        break;
                    
                    case RemoteConfigValueType.Translatable:
                        _appTranslatableEntries.Add(editorEntry);
                        break;
                }
            }
        }

        private List<RemoteConfigEditorEntry> GetAppIntEntries()
        {
            return _appIntEntries ??= allAppEntries.Where(x => x.type == RemoteConfigValueType.Int).ToList();
        }

        private List<RemoteConfigEditorEntry> GetAppFloatEntries()
        {
            return _appFloatEntries ??= allAppEntries.Where(x => x.type == RemoteConfigValueType.Float).ToList();
        }
        
        private List<RemoteConfigEditorEntry> GetAppBoolEntries()
        {
            return _appBoolEntries ??= allAppEntries.Where(x => x.type == RemoteConfigValueType.Bool).ToList();
        }
        
        private List<RemoteConfigEditorEntry> GetAppStringEntries()
        {
            return _appStringEntries ??= allAppEntries.Where(x => x.type == RemoteConfigValueType.String).ToList();
        }
        
        private List<RemoteConfigEditorEntry> GetAppTranslatableEntries()
        {
            return _appTranslatableEntries ??= allAppEntries.Where(x => x.type == RemoteConfigValueType.Translatable).ToList();
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

        private RemoteConfigService GetRcService()
        {
            var service = EditorExtender.LoadScriptableAsset<RemoteConfigService>();
            if (!service || !service.LocalTranslationFile)
            {
                Debug.LogError("Problem with remote config service ! Unable to write the json file.");
                return null;
            }

            return service;
        }

        private string GetJsonFileAbsolutePath(Object localFileAsset)
        {
            string relativePath = AssetDatabase.GetAssetPath(localFileAsset);
            return IOExtender.RelativeToAbsolutePath(relativePath);
        }

        #endregion
    }
}
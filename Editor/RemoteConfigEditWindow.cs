using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReaaliStudio.Utils.Extensions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CCLBStudio.RemoteConfig
{
    public class RemoteConfigEditWindow : EditorWindow
    {
        private RemoteConfigService _remoteConfigService;
        private RemoteConfigEditWindowSettings _settings;
        [NonSerialized] private bool _init;
        
        private RemoteConfigValueTypeFilter _typeFilter = RemoteConfigValueTypeFilter.None;
        private string[] _sortingCategories;
        private int _currentCategoryFilter;
        private bool _isEditingNewCategory;
        private bool _clickedNewCategoryThisFrame;
        private string _newCategoryName;
        private const string CategoryEditFocusName = "CategoryTextField";
        private bool _requireFilterFlush;
        private string _searchFilter;
        
        private readonly string[] _tabs = { "Languages And Platforms", "App Entries", "Usage Report" };
        private int _selectedTab = 0;
        [NonSerialized] private int _previouslySelectedTable = -1;

        private Dictionary<int, List<RemoteConfigEditorEntry>> _usageReportEntries;
        private List<int> _orderedReportKeys;
        [NonSerialized] private bool _initializedUsageReport;

        private RemoteConfigEditorData _editorData;
        private Dictionary<RuntimePlatform, bool> _platformFoldouts;
        private List<RemoteConfigEditorEntry> _entriesToDraw;

        private GUIStyle _columnsLabelStyle;
        private GUIStyle _headerButtonsLabelStyle;
        private GUIStyle _deleteDataEntryButtonStyle;
        private GUIStyle _deletePlatformEntryButtonStyle;
        private GUIStyle _deleteLanguageButtonStyle;
        private GUIStyle _languageStyle;
        private GUIStyle _sectionFoldoutStyle;
        private GUIStyle _commonHeaderButtonStyle;
        private GUIStyle _searchStyle;

        private GUIContent _syncFromCloudContent;
        private GUIContent _saveToJsonContent;
        private GUIContent _uploadContent;
        private GUIContent _saveLocalContent;
        private GUIContent _syncFromJsonContent;

        private Vector2 _scrollPos;
        private Rect _scrollViewRect;
        private Rect _scrollContentRect;
        private Rect _deleteBtnRect;
        private Rect _transferProgressRect;
        private float _transferProgress;
        private float _scrollSize;
        private const float CullingTolerance = 5f;
        private float _cullingPosY;
        private float _cullingTop;
        private float _cullingBot;
        private float _entryHeight;

        private Color _baseGUIColor;
        private float _appEntriesSectionStartPosY;
        private float _appEntryStartPosY;

        private bool _isUploading;
        private bool _isDownloading;

        private bool _languagesSectionUnfolded;
        private bool _appEntriesSectionUnfolded;
        private bool _categoriesSectionUnfolded;
        private bool _platformSectionUnfolded;
        private const string SectionLangUnfolded = "rc_section_lang_unfolded";
        private const string SectionAppEntriesUnfolded = "rc_section_data_unfolded";
        private const string SectionCategoriesUnfolded = "rc_section_categories_unfolded";
        private const string SectionPlatformUnfolded = "rc_section_platform_unfolded";
        private const string LastOpenTab = "rc_last_tab_open";

        public static void ShowWindow()
        {
            var window = (RemoteConfigEditWindow) GetWindow(typeof(RemoteConfigEditWindow));
            window.minSize = new Vector2(1200, 500);
        }

        #region Unity Events

        private void OnEnable()
        {
            _remoteConfigService = EditorExtender.LoadScriptableAsset<RemoteConfigService>();
            _editorData = EditorExtender.LoadScriptableAsset<RemoteConfigEditorData>();
            _settings = EditorExtender.LoadScriptableAsset<RemoteConfigEditWindowSettings>();

            _scrollViewRect = new Rect();
            _scrollContentRect = new Rect();
            _deleteBtnRect = new Rect();
            _transferProgressRect = new Rect(0, 0, _settings.headerButtonWidth, 10);

            _baseGUIColor = GUI.color;
        }

        private void OnGUI()
        {
            if (!CheckAssets())
            {
                return;
            }

            if (!_init)
            {
                SetupStyles();
                SetupContents();
                SetupEditorPrefs();
                Initialize();
            }

            if (_requireFilterFlush)
            {
                _requireFilterFlush = false;
                _editorData.DirtyAllFilterEntries();
                RefreshDrawingData();
            }

            DrawCommonHeader();

            EditorGUI.BeginChangeCheck();
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);
            bool newTabSelected = _previouslySelectedTable != _selectedTab;
            _previouslySelectedTable = _selectedTab;
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt(LastOpenTab, _selectedTab);
            }
            
            switch (_selectedTab)
            {
                case 0:
                    DrawLanguagesSection();
                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    DrawPlatformSection();
                    break;
                
                case 1:
                    if (newTabSelected)
                    {
                        RefreshDrawingData();
                    }
                    DrawCategorySection();
                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    DrawAppEntriesSection();
                    break;
                
                case 2:
                    if (newTabSelected)
                    {
                        RefreshReportEntries();
                    }
                    DrawUsageReportSection();
                    break;
            }
        }

        private void OnDestroy()
        {
            if (!_editorData || !_editorData.IsDirty())
            {
                return;
            }

            if (EditorUtility.DisplayDialog("Modifications not saved !", "You have unsaved changes. Do you want to save those modifications ?", "Save And Quit", "Quit Without Saving"))
            {
                _editorData.WriteOnDisk();
            }
        }

        #endregion

        #region Initialization

        private bool CheckAssets()
        {
            if (!_remoteConfigService)
            {
                _remoteConfigService = EditorExtender.LoadScriptableAsset<RemoteConfigService>();
                EditorGUILayout.HelpBox("Unable to load the Remote Config Service !", MessageType.Error);
                return false;
            }

            if (!_settings)
            {
                _settings = EditorExtender.LoadScriptableAsset<RemoteConfigEditWindowSettings>();
                EditorGUILayout.HelpBox("Unable to load the settings file !", MessageType.Error);
                return false;
            }

            if (!_remoteConfigService.LocalTranslationFile)
            {
                EditorGUILayout.HelpBox("There is no local translation file. This file holds all the remote config data for the current version and is what will be uploaded on the server. Click the button below to create a empty one.", MessageType.Warning);
                if (GUILayout.Button("Create Empty Json"))
                {
                    CreateAndBindEmptyJson();
                }
                
                return false;
            }

            if (!_editorData)
            {
                CreateAndBindEditorDataSaver();
            }

            return true;
        }

        private void SetupSortingCategories()
        {
            _sortingCategories = new string[_editorData.allCategories.Count + 1];
            _sortingCategories[0] = "Default";
            for (int i = 0; i < _editorData.allCategories.Count; i++)
            {
                _sortingCategories[i + 1] = _editorData.allCategories[i].Key;
            }
        }

        private void Initialize()
        {
            _editorData.Initialize();
            CreatePlatformFoldoutStates();
            RefreshDrawingData();
            SetupSortingCategories();
            _init = true;
        }

        private void SetupEditorPrefs()
        {
            _appEntriesSectionUnfolded = EditorPrefs.GetBool(SectionAppEntriesUnfolded, true);
            _categoriesSectionUnfolded = EditorPrefs.GetBool(SectionCategoriesUnfolded, true);
            _languagesSectionUnfolded = EditorPrefs.GetBool(SectionLangUnfolded, true);
            _platformSectionUnfolded = EditorPrefs.GetBool(SectionPlatformUnfolded, true);
            _selectedTab = EditorPrefs.GetInt(LastOpenTab, 0);
        }

        private void CreatePlatformFoldoutStates()
        {
            _platformFoldouts = new Dictionary<RuntimePlatform, bool>(_editorData.platformEntries.Count);
            //foreach (var platform in _editorData.platformEntries.GetKeys())
            foreach (var platform in _editorData.platformEntries.Select(x => x.Key))
            {
                _platformFoldouts[platform] = false;
            }
        }

        private void SetupStyles()
        {
            _searchStyle = GUI.skin.FindStyle("ToolbarSearchTextField");
            
            _columnsLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };

            _headerButtonsLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
            };
            
            _deleteDataEntryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = _settings.lineHeight,
                fixedWidth = 35f,
                padding = new RectOffset(0,0,0,0)
            };

            _deletePlatformEntryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = EditorGUIUtility.singleLineHeight,
                fixedWidth = EditorGUIUtility.singleLineHeight
            };

            _deleteLanguageButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = _settings.languageLineHeight,
                fixedWidth = _settings.languageLineHeight,
                padding = new RectOffset(3,3,3,3)
            };

            _commonHeaderButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = _settings.headerButtonHeight,
                fixedWidth = _settings.headerButtonWidth
            };

            _languageStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = _settings.languageFontSize,
                fontStyle = FontStyle.Bold
            };

            _sectionFoldoutStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = EditorGUIUtility.singleLineHeight,
                normal =
                {
                    textColor = _settings.foldoutNameColor,
                }, 
                onNormal =
                {
                    textColor = _settings.foldoutNameColor,
                },
                
                margin = new RectOffset(5,0,0,0),
            };
        }

        private void SetupContents()
        {
            _syncFromCloudContent = new GUIContent(_settings.syncFromCloudTexture, _settings.syncFromCloudTooltip);
            _saveToJsonContent = new GUIContent(_settings.saveToJsonTexture, _settings.saveToJsonTooltip);
            _uploadContent = new GUIContent(_settings.uploadTexture, _settings.uploadTooltip);
            _saveLocalContent = new GUIContent(_settings.saveLocalTexture, _settings.saveLocalTooltip);
            _syncFromJsonContent = new GUIContent(_settings.syncFromJsonTexture, _settings.syncFromJsonTooltip);
        }

        #endregion

        #region Drawing Methods

        #region Common Header Methods

        private void DrawCommonHeader()
        {
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            DrawSyncFromCloud();
            GUILayout.Space(_settings.spaceBetweenHeaderButtons);
            DrawSaveToJson();
            GUILayout.Space(_settings.spaceBetweenHeaderButtons);
            DrawSaveLocal();
            GUILayout.Space(_settings.spaceBetweenHeaderButtons);
            DrawSyncFromJson();
            GUILayout.Space(_settings.spaceBetweenHeaderButtons);
            DrawUpload();
            GUILayout.FlexibleSpace();
            
            GUILayout.EndHorizontal();
        }

        private void DrawSyncFromCloud()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("From Cloud", _headerButtonsLabelStyle, GUILayout.Width(_settings.headerButtonWidth));

            GUI.enabled = !(_isUploading || _isDownloading);
            if (GUILayout.Button(_syncFromCloudContent, _commonHeaderButtonStyle))
            {
                if (!_remoteConfigService.TransferStrategy)
                {
                    EditorUtility.DisplayDialog("No Transfer Strategy !", "There is no transfer strategy binded in your remote config service. Please create and bind one to perform upload / download.", "Ok");
                }
                else
                {
                    _transferProgress = 0f;
                    _isDownloading = true;
                    _editorData.DownloadJson(OnDownloadSucceeded, OnDownloadProgressed, OnDownloadFailed);
                }
            }
            GUI.enabled = true;

            if (_isDownloading)
            {
                var rect = GUILayoutUtility.GetLastRect();
                _transferProgressRect.x = rect.x;
                _transferProgressRect.y = rect.yMax - _transferProgressRect.height;
                
                EditorGUI.ProgressBar(_transferProgressRect, _transferProgress, $"{_transferProgress * 100f:F1}%");
            }
            
            GUILayout.EndVertical();
        }

        private void DrawSaveToJson()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Write Json", _headerButtonsLabelStyle, GUILayout.Width(_settings.headerButtonWidth));
            if (GUILayout.Button(_saveToJsonContent, _commonHeaderButtonStyle))
            {
                _editorData.WriteJson();
            }
            GUILayout.EndVertical();
        }

        private void DrawUpload()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("To Cloud", _headerButtonsLabelStyle, GUILayout.Width(_settings.headerButtonWidth));
            
            GUI.enabled = !(_isUploading || _isDownloading);
            if (GUILayout.Button(_uploadContent, _commonHeaderButtonStyle))
            {
                if (!_remoteConfigService.TransferStrategy)
                {
                    EditorUtility.DisplayDialog("No Transfer Strategy !", "There is no transfer strategy binded in your remote config service. Please create and bind one to perform upload / download.", "Ok");
                }
                else
                {
                    _transferProgress = 0f;
                    _isUploading = true;
                    _editorData.UploadJson(OnUploadSucceeded, OnUploadProgressed, OnUploadFailed);
                }
            }
            GUI.enabled = true;
            
            if (_isUploading)
            {
                var rect = GUILayoutUtility.GetLastRect();
                _transferProgressRect.x = rect.x;
                _transferProgressRect.y = rect.yMax - _transferProgressRect.height;
                
                EditorGUI.ProgressBar(_transferProgressRect, _transferProgress, $"{_transferProgress * 100f:F1}%");
            }
            
            GUILayout.EndVertical();
        }

        private void DrawSaveLocal()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Save Changes", _headerButtonsLabelStyle, GUILayout.Width(_settings.headerButtonWidth));
            if (GUILayout.Button(_saveLocalContent, _commonHeaderButtonStyle))
            {
                _editorData.WriteOnDisk();
            }
            GUILayout.EndVertical();
        }

        private void DrawSyncFromJson()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Local Sync", _headerButtonsLabelStyle, GUILayout.Width(_settings.headerButtonWidth));
            if (GUILayout.Button(_syncFromJsonContent, _commonHeaderButtonStyle))
            {
                var rc = new RemoteConfigData(_remoteConfigService.LocalTranslationFile.text);
                _editorData.LoadFrom(rc);
                RefreshDrawingData();
            }
            GUILayout.EndVertical();
        }

        private void OnDownloadProgressed(float progress)
        {
            _transferProgress = progress;
        }

        private void OnDownloadSucceeded()
        {
            _isDownloading = false;
            RefreshDrawingData();
        }
        
        private void OnDownloadFailed()
        {
            _isDownloading = false;
            EditorUtility.DisplayDialog("Download failed !", "An error occured while downloading the file. See console for more details.", "Ok");
        }
        
        private void OnUploadProgressed(float progress)
        {
            _transferProgress = progress;
        }
        
        private void OnUploadSucceeded()
        {
            _isUploading = false;
        }
        
        private void OnUploadFailed()
        {
            _isUploading = false;
            EditorUtility.DisplayDialog("Upload failed !", "An error occured while uploading the file. See console for more details.", "Ok");
        }

        #endregion

        #region Scroll View Methods

        private void BeginScrollView()
        {
            _scrollViewRect.y = _appEntriesSectionStartPosY;
            _scrollViewRect.width = position.width - EditorGUIUtility.standardVerticalSpacing;
            _scrollViewRect.height = position.height - _appEntriesSectionStartPosY - EditorGUIUtility.standardVerticalSpacing - EditorGUIUtility.singleLineHeight;

            _scrollContentRect.width = _scrollViewRect.width - 20;
            _scrollContentRect.y = _scrollViewRect.y - EditorGUIUtility.singleLineHeight - _settings.spacingBetweenElements;
            _scrollContentRect.height = _scrollSize + _settings.spacingBetweenElements + _settings.bottomScrollOffset;
            
            _scrollPos = GUI.BeginScrollView(_scrollViewRect, _scrollPos, _scrollContentRect);
        }

        private void ScrollToBottom()
        {
            _scrollPos.y = _scrollContentRect.yMin + _scrollContentRect.height;
        }

        private void RefreshScrollSize()
        {
            _scrollSize = _entriesToDraw!.Sum(x => x.GetValueHeight() + _settings.spacingBetweenElements);
        }

        #endregion

        #region Language Methods

        private void DrawLanguagesSection()
        {
            EditorGUI.BeginChangeCheck();
            DrawSectionFoldout("Languages", ref _languagesSectionUnfolded);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(SectionLangUnfolded, _languagesSectionUnfolded);
            }

            if (_languagesSectionUnfolded)
            {
                DrawLanguagesContent();
            }
        }

        private void DrawLanguagesContent()
        {
            EditorGUI.indentLevel++;

            for (int i = 0; i < _editorData.allLanguages.Count; i++)
            {
                DrawLanguageEntry(i);
            }

            if (GUILayout.Button("+ Add New Language"))
            {
                GenericMenu menu = new GenericMenu();
                var values = Enum.GetValues(typeof(SystemLanguage));
                foreach (var elem in values)
                {
                    menu.AddItem(new GUIContent(elem.ToString()), false, AddLanguage, elem);
                }
                
                menu.ShowAsContext();
            }
            
            EditorGUI.indentLevel--;
        }

        private void AddLanguage(object systemLang)
        {
            SystemLanguage lang = (SystemLanguage)systemLang;

            if (_editorData.allLanguages.Find(x => x.language == lang) != null)
            {
                EditorUtility.DisplayDialog("Language Already Added !"
                    , $"Language {lang.ToString()} is already present. Please select another language."
                    , "Ok");
                return;
            }

            _editorData.NotifyLanguageAdded(lang);
        }

        private void DrawLanguageEntry(int index)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 18);
            
            if (GUILayout.Button(_settings.deleteBtnTexture, _deleteLanguageButtonStyle))
            {
                _editorData.RemoveLanguage(index);
            }
            else
            {
                if (_editorData.allLanguages[index].flag)
                {
                    GUILayout.Label(_editorData.allLanguages[index].flag, GUILayout.Height(_deleteLanguageButtonStyle.fixedHeight), GUILayout.Width(50));
                }
                GUILayout.Label(_editorData.allLanguages[index].language.ToString(), _languageStyle, GUILayout.Height(_settings.languageLineHeight));
            }
            
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Platform Methods

        private void DrawPlatformSection()
        {
            EditorGUI.BeginChangeCheck();
            DrawSectionFoldout("Platforms", ref _platformSectionUnfolded);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(SectionPlatformUnfolded, _platformSectionUnfolded);
            }

            if (_platformSectionUnfolded)
            {
                DrawPlatformContent();
            }
        }

        private void DrawPlatformContent()
        {
            if (_platformFoldouts == null)
            {
                CreatePlatformFoldoutStates();
            }

            EditorGUI.indentLevel++;
            for(int i = 0; i < _editorData.platformEntries.Count; i++)
            {
                var pair = _editorData.platformEntries[i];
                if (!_platformFoldouts!.TryGetValue(pair.Key, out bool unfolded))
                {
                    _platformFoldouts.Add(pair.Key, false);
                    continue;
                }

                _platformFoldouts[pair.Key] = EditorGUILayout.Foldout(unfolded, pair.Key.ToString(), true);
                if (unfolded)
                {
                    for(int j = 0; j < pair.Value.Count; j++)
                    {
                        var entry = pair.Value[j];
                        EditorGUI.BeginChangeCheck();
                        DrawPlatformEntry(pair.Key, entry, j);
                        if (EditorGUI.EndChangeCheck())
                        {
                            _editorData.SetDirty();
                        }
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("+ Add New Entry"))
                    {
                        _editorData.AddNewPlatformEntry(pair.Key);
                    }

                    if (GUILayout.Button("- Delete Platform"))
                    {
                        _editorData.RemovePlatform(pair.Key);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button("+ Add New Platform"))
            {
                GenericMenu menu = new GenericMenu();
                var values = Enum.GetValues(typeof(RuntimePlatform));
                foreach (var elem in values)
                {
                    menu.AddItem(new GUIContent(elem.ToString()), false, AddPlatform, elem);
                }
                
                menu.ShowAsContext();
            }
            
            EditorGUI.indentLevel--;
        }

        private void AddPlatform(object runtimePlatform)
        {
            RuntimePlatform platform = (RuntimePlatform)runtimePlatform;
            int index = _editorData.platformEntries.FindIndex(x => x.Key == platform);

            if (index >= 0)
            {
                EditorUtility.DisplayDialog("Platform Already Added !"
                    , $"Platform {platform.ToString()} is already present. Please select another platform."
                    , "Ok");
                return;
            }

            _editorData.AddNewPlatform(platform);
        }

        private void DrawPlatformEntry(RuntimePlatform platform, RemoteConfigEditorEntry entry, int index)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 18);
            if (GUILayout.Button("X", _deletePlatformEntryButtonStyle))
            {
                _editorData.NotifyRemovePlatformEntry(platform, index);
            }
            else
            {
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 50f;
                
                EditorGUI.BeginChangeCheck();
                string oldKey = entry.key;
                entry.key = EditorGUILayout.TextField("Key", entry.key, GUILayout.Width(_settings.keyWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    _editorData.NotifyPlatformEntryKeyChanged(platform, oldKey, entry);
                }

                if (!entry.isValid)
                {
                    EditorGUILayout.HelpBox("This key is not valid !", MessageType.Error);
                }
                else
                {
                    entry.type = (RemoteConfigValueType)EditorGUILayout.EnumPopup("Type", entry.type, GUILayout.Width(_settings.typeWidth));
                    float valueWidth = _settings.valueWidth + 100f;

                    switch (entry.type)
                    {
                        case RemoteConfigValueType.Int:
                            entry.intValue = EditorGUILayout.IntField("Value", entry.intValue, GUILayout.Width(valueWidth));
                            break;
                    
                        case RemoteConfigValueType.Float:
                            entry.floatValue = EditorGUILayout.FloatField("Value", entry.floatValue, GUILayout.Width(valueWidth));
                            break;
                    
                        case RemoteConfigValueType.Bool:
                            entry.boolValue = EditorGUILayout.Toggle("Value", entry.boolValue, GUILayout.Width(valueWidth));

                            break;
                    
                        case RemoteConfigValueType.String:
                            entry.stringValue = EditorGUILayout.TextField("Value", entry.stringValue, GUILayout.Width(valueWidth));
                            break;
                    
                        case RemoteConfigValueType.Translatable:
                            EditorGUILayout.HelpBox("Translatable is not currently supported as platform entry.", MessageType.Error);
                            break;
                    }
                }

                EditorGUIUtility.labelWidth = labelWidth;
            }
            
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Category Methods

        private void DrawCategorySection()
        {
            EditorGUI.BeginChangeCheck();
            DrawSectionFoldout("Sorting Categories", ref _categoriesSectionUnfolded);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(SectionCategoriesUnfolded, _categoriesSectionUnfolded);
            }

            if (!_categoriesSectionUnfolded)
            {
                return;
            }
            
            DrawCategoriesContent();
        }

        private void DrawCategoriesContent()
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60;
            EditorGUI.indentLevel++;
            
            for(int i = 0; i < _editorData.allCategories.Count; i++)
            {
                var pair = _editorData.allCategories[i];
                GUILayout.BeginHorizontal();
                
                GUILayout.Space(EditorGUI.indentLevel * 15);
                if (GUILayout.Button("X", GUILayout.Width(EditorGUIUtility.singleLineHeight)))
                {
                    DeleteCategory(i);
                }
                
                EditorGUILayout.LabelField(pair.Key + " :", EditorStyles.boldLabel, GUILayout.Width(100));
                GUILayout.Space(_settings.spacingBetweenElements);
                
                EditorGUI.BeginChangeCheck();
                pair.Value = EditorGUILayout.TextField("Prefix", pair.Value, GUILayout.Width(250));
                if (EditorGUI.EndChangeCheck())
                {
                    _editorData.NotifyCategoryPrefixChanged(i);
                }
                
                GUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
            EditorGUIUtility.labelWidth = labelWidth;
            GUILayout.Space(_settings.spacingBetweenElements);
            DrawNewCategoryArea();
        }
        
        private void DeleteCategory(int index)
        {
            _editorData.NotifyCategoryDeleted(index);
            _currentCategoryFilter = Mathf.Max(0, _currentCategoryFilter - 1);
            RefreshDrawingData();
            SetupSortingCategories();
        }
        
        private void DrawNewCategoryArea()
        {
            if (!_isEditingNewCategory)
            {
                if (GUILayout.Button("+ Add New Category"))
                {
                    _isEditingNewCategory = true;
                    _clickedNewCategoryThisFrame = true;
                    _newCategoryName = string.Empty;
                }

                return;
            }
 
            GUI.SetNextControlName(CategoryEditFocusName);
            _newCategoryName = EditorGUILayout.TextField(_newCategoryName);

            if (_clickedNewCategoryThisFrame)
            {
                _clickedNewCategoryThisFrame = false;
                GUI.FocusControl(CategoryEditFocusName);
            }
                
            bool isCurrentlyFocus = GUI.GetNameOfFocusedControl() == CategoryEditFocusName;

            if (!isCurrentlyFocus || (Event.current.type == EventType.Used && Event.current.keyCode == KeyCode.Return))
            {
                string newCategoryName = ObjectNames.NicifyVariableName(_newCategoryName.Trim());
                if (!string.IsNullOrEmpty(newCategoryName) && _editorData.TryAddNewCategory(newCategoryName))
                {
                    SetupSortingCategories();
                }

                _isEditingNewCategory = false;
            }
        }

        #endregion

        #region Usage Report Methods

        private void DrawUsageReportSection()
        {
            if (!_initializedUsageReport)
            {
                RefreshReportEntries();
                _initializedUsageReport = true;
            }

            Rect reportRect = EditorGUILayout.GetControlRect();
            _appEntriesSectionStartPosY = reportRect.y;

            BeginScrollView();
            DrawAllUsageReportEntries(ref reportRect);
            GUI.EndScrollView();
        }

        private void RefreshReportEntries()
        {
            _usageReportEntries = new Dictionary<int, List<RemoteConfigEditorEntry>> { { 0, new List<RemoteConfigEditorEntry>() } };
            Dictionary<string, RemoteConfigEditorEntry> addedEntries = new Dictionary<string, RemoteConfigEditorEntry>();

            foreach (var editorEntry in _editorData.allAppEntries.Concat(_editorData.platformEntries.SelectMany(x => x.Value)))
            {
                if (addedEntries.ContainsKey(editorEntry.key))
                {
                    continue;
                }
                
                addedEntries.Add(editorEntry.key, editorEntry);
                
                int index = _remoteConfigService.keyUses.FindIndex(x => x.Key == editorEntry.key);
                if (index < 0)
                {
                    _usageReportEntries[0].Add(editorEntry);
                    continue;
                }

                var trackedPair = _remoteConfigService.keyUses[index];
                if (_usageReportEntries.TryGetValue(trackedPair.Value, out var editorEntries))
                {
                    editorEntries.Add(editorEntry);
                }
                else
                {
                    _usageReportEntries[trackedPair.Value] = new List<RemoteConfigEditorEntry> { editorEntry };
                }
            }

            _orderedReportKeys = _usageReportEntries.Keys.OrderBy(x => x).ToList();
            _scrollSize = _usageReportEntries.Sum(x => x.Value.Count * (EditorGUIUtility.singleLineHeight + _settings.keyUsageLineSizeOffset + EditorGUIUtility.standardVerticalSpacing));
        }

        private void DrawAllUsageReportEntries(ref Rect rect)
        {
            rect.height = EditorGUIUtility.singleLineHeight + _settings.keyUsageLineSizeOffset;
            rect.width = _scrollContentRect.width;
            float posX = rect.x;
            float width = rect.width;
            float height = rect.height;
            float halfDiff = _settings.keyUsageLineSizeOffset / 2f;

            _cullingTop = _scrollPos.y;
            _cullingBot = _cullingTop + _scrollViewRect.height;
            _cullingPosY = 0f;
            _entryHeight = height;

            foreach (var key in _orderedReportKeys)
            {
                var list = _usageReportEntries[key];
                foreach (var editorEntry in list)
                {
                    bool shouldDraw = _cullingPosY + _entryHeight >= _cullingTop - CullingTolerance && _cullingPosY <= _cullingBot + CullingTolerance;
                    
                    if (shouldDraw)
                    {
                        GUI.Box(rect, "", EditorStyles.helpBox);

                        rect.height = EditorGUIUtility.singleLineHeight;
                        rect.x += 5;
                        rect.y += halfDiff;
                        rect.width = _settings.keyUsageKeyWidth;
                        GUI.Label(rect, $"Key \"{editorEntry.key}\"");

                        rect.x += rect.width + 10;
                        rect.width = _settings.keyUsageCountWidth;
                        GUI.Label(rect, $"Has been used {key.ToString()} times");

                        rect.x += rect.width + 10;
                        rect.width = _settings.keyUsageDeleteButtonWidth;
                        if (GUI.Button(rect, "- Delete Entry"))
                        {
                            _editorData.NotifyAppEntryDeleted(editorEntry);
                            RefreshReportEntries();
                        }
                        
                        rect.y -= halfDiff;
                    }

                    rect.x = posX;
                    rect.width = width;
                    rect.height = height;
                    rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                    _cullingPosY += rect.height + EditorGUIUtility.standardVerticalSpacing;
                }
            }
        }

        #endregion

        #region App Entries Methods

        private void RefreshDrawingList()
        {
            _entriesToDraw = _editorData.GetAppEntriesForFilter(_typeFilter);
            if (_currentCategoryFilter > 0)
            {
                _entriesToDraw = _entriesToDraw.FindAll(x => x.categoryIndex == _currentCategoryFilter);
            }

            if (!string.IsNullOrEmpty(_searchFilter))
            {
                _entriesToDraw = _entriesToDraw.FindAll(x => x.key.IndexOf(_searchFilter.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        private void DrawAppEntriesSection()
        {
            EditorGUI.BeginChangeCheck();
            DrawSectionFoldout("App Entries", ref _appEntriesSectionUnfolded);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(SectionAppEntriesUnfolded, _appEntriesSectionUnfolded);
            }

            if (!_appEntriesSectionUnfolded)
            {
                return;
            }
            
            if (_entriesToDraw == null)
            {
                RefreshDrawingData();
            }
                
            DrawAppEntryContent();
        }

        private void DrawAppEntryContent()
        {
            DrawAppEntriesOptions();
            DrawColumnsHeader();

            Rect entryRect = EditorGUILayout.GetControlRect();
            BeginScrollView();
            DrawAppEntries(ref entryRect);
            GUI.EndScrollView();
            
            DrawAddNewAppEntryButton(ref entryRect);
        }

        private void DrawAddNewAppEntryButton(ref Rect rect)
        {
            rect.width = position.width - 10;
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y = _scrollViewRect.yMin + _scrollViewRect.height;
            if(GUI.Button(rect, "+ Add New App Entry"))
            {
                GenericMenu menu = new GenericMenu();
                var values = Enum.GetValues(typeof(RemoteConfigValueType));
                foreach (var elem in values)
                {
                    menu.AddItem(new GUIContent(elem.ToString()), false, AddNewAppEntry, elem);
                }
                
                menu.ShowAsContext();
            }
        }

        private void AddNewAppEntry(object type)
        {
            RemoteConfigValueType t = (RemoteConfigValueType)type;
            _editorData.NotifyNewAppEntryAdded(t);
            RefreshDrawingData();
            ScrollToBottom();
        }
        
        private void DrawAppEntriesOptions()
        {
            GUILayout.Space(_settings.spacingBetweenElements);
            EditorGUI.indentLevel++;
            GUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Width(_settings.keyWidth + _settings.typeWidth + _settings.valueWidth + _settings.categoryWidth));
            
            GUILayout.Space(15);
            EditorGUI.BeginChangeCheck();
            _searchFilter = GUILayout.TextField(_searchFilter, _searchStyle, GUILayout.Width(_settings.keyWidth));
            if (EditorGUI.EndChangeCheck())
            {
                RefreshDrawingData();
            }
            
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 80f;
            
            EditorGUI.BeginChangeCheck();
            _typeFilter = (RemoteConfigValueTypeFilter)EditorGUILayout.EnumPopup("Filter By", _typeFilter, GUILayout.Width(_settings.typeWidth + EditorGUIUtility.labelWidth));
            if (EditorGUI.EndChangeCheck())
            {
                RefreshDrawingData();
            }
            
            EditorGUI.BeginChangeCheck();
            _currentCategoryFilter = EditorGUILayout.Popup("Category", _currentCategoryFilter, _sortingCategories, GUILayout.Width(_settings.typeWidth + EditorGUIUtility.labelWidth));
            if (EditorGUI.EndChangeCheck())
            {
                RefreshDrawingData();
            }
            
            EditorGUILayout.LabelField($"Filtered Elements : {_entriesToDraw.Count.ToString()}", EditorStyles.boldLabel);

            EditorGUIUtility.labelWidth = labelWidth;
            GUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
            GUILayout.Space(_settings.spacingBetweenElements);
        }

        private void DrawColumnsHeader()
        {
            var labelRect = EditorGUILayout.GetControlRect();
            float lineHeight = labelRect.height + _settings.spacingBetweenElements;

            labelRect.width = _settings.keyWidth;
            EditorGUI.LabelField(labelRect, "Key", _columnsLabelStyle);
            DrawLine(new Vector3(labelRect.xMax, labelRect.yMin, 0), new Vector3(labelRect.xMax, labelRect.yMin + lineHeight, 0f));
            
            labelRect.x += _settings.keyWidth;
            labelRect.width = _settings.typeWidth;
            EditorGUI.LabelField(labelRect, "Type", _columnsLabelStyle);
            DrawLine(new Vector3(labelRect.xMax, labelRect.yMin, 0), new Vector3(labelRect.xMax, labelRect.yMin + lineHeight, 0f));
            
            labelRect.x += _settings.typeWidth;
            labelRect.width = _settings.valueWidth;
            EditorGUI.LabelField(labelRect, "Value", _columnsLabelStyle);
            DrawLine(new Vector3(labelRect.xMax, labelRect.yMin, 0), new Vector3(labelRect.xMax, labelRect.yMin + lineHeight, 0f));
            
            labelRect.x += _settings.valueWidth;
            labelRect.width = _settings.categoryWidth;
            EditorGUI.LabelField(labelRect, "Category", _columnsLabelStyle);
            DrawLine(new Vector3(labelRect.xMax, labelRect.yMin, 0), new Vector3(labelRect.xMax, labelRect.yMin + lineHeight, 0f));

            _appEntriesSectionStartPosY = labelRect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + _settings.spacingBetweenElements;
        }

        #endregion

        #region App Entries Data Methods

        private void DrawAppEntries(ref Rect entryRect)
        {
            _deleteBtnRect.x = entryRect.x;
            _deleteBtnRect.y = entryRect.y;
            _deleteBtnRect.height = _deleteDataEntryButtonStyle.fixedHeight;
            _deleteBtnRect.width = _deleteDataEntryButtonStyle.fixedWidth;

            _cullingTop = _scrollPos.y;
            _cullingBot = _cullingTop + _scrollViewRect.height;
            _cullingPosY = 0f;

            for(int i = 0; i < _entriesToDraw.Count; i++)
            {
                var editorEntry = _entriesToDraw[i];
                
                _entryHeight = editorEntry.GetValueHeight() + _settings.spacingBetweenElements;
                bool shouldDraw = _cullingPosY + _entryHeight >= _cullingTop - CullingTolerance && _cullingPosY <= _cullingBot + CullingTolerance;

                DrawAppEntryBoundingBox(editorEntry, ref entryRect, shouldDraw);
                DrawAppEntry(editorEntry, ref entryRect, shouldDraw);

                _cullingPosY += _entryHeight;
            }
        }

        private void DrawAppEntryBoundingBox(RemoteConfigEditorEntry entry, ref Rect rect, bool shouldDraw)
        {
            if (!shouldDraw)
            {
                return;
            }
            
            rect.height = entry.GetValueHeight();
            rect.width = _settings.keyWidth + _settings.typeWidth + _settings.valueWidth + _settings.categoryWidth;
            GUI.Box(rect, "", EditorStyles.helpBox);

            rect.height = EditorGUIUtility.singleLineHeight;
        }

        private void DrawAppEntry(RemoteConfigEditorEntry entry, ref Rect rect, bool shouldDraw)
        { 
            float startPosX = rect.x;
            float startHeight = rect.height;
            _appEntryStartPosY = rect.y;
            rect.y += _settings.boxBorderOffset;
            
            
            DrawDeletionButton(entry, ref rect, shouldDraw);
            DrawKeyEntry(entry, ref rect, shouldDraw);
            if (!entry.isValid)
            {
                DrawBadEntryMessage(ref rect, shouldDraw);
            }
            else
            {
                DrawTypeEntry(entry, ref rect, shouldDraw);
                DrawValueEntry(entry, ref rect, shouldDraw);
                DrawCategoryEntry(entry, ref rect, shouldDraw);
            }

            rect.y += _settings.lineHeight + _settings.boxBorderOffset + _settings.spacingBetweenElements;
            rect.x = startPosX;
            rect.height = startHeight;
        }

        private void DrawKeyEntry(RemoteConfigEditorEntry entry, ref Rect rect, bool shouldDraw)
        {
            float deleteBtnOffset = _deleteDataEntryButtonStyle.fixedWidth + _settings.boxBorderOffset * 2;
            rect.x += deleteBtnOffset;
            rect.height = _settings.lineHeight;
            rect.width = _settings.keyWidth - deleteBtnOffset - _settings.boxBorderOffset;

            if (!shouldDraw)
            {
                return;
            }

            if (Event.current.type == EventType.KeyDown || Event.current.type == EventType.KeyUp)
            {
                EditorGUI.BeginChangeCheck();
                string oldKey = entry.key;
            
                entry.key = EditorGUI.TextField(rect, entry.key);
                if (EditorGUI.EndChangeCheck())
                {
                    _editorData.NotifyAppEntryKeyChanged(oldKey, entry);
                }
            }
            else
            {
                EditorGUI.TextField(rect, entry.key);
            }
        }

        private void DrawBadEntryMessage(ref Rect rect, bool shouldDraw)
        {
            rect.x += rect.width + 10 + _settings.typeWidth;
            rect.width = _settings.keyWidth;
            rect.height = _settings.lineHeight;

            if (!shouldDraw)
            {
                return;
            }
            
            EditorGUI.HelpBox(rect, "This key is not valid !", MessageType.Error);
        }

        private void DrawTypeEntry(RemoteConfigEditorEntry entry, ref Rect rect, bool shouldDraw)
        {
            rect.x += rect.width + 10;
            rect.width = _settings.typeWidth - 10;
            rect.height = EditorGUIUtility.singleLineHeight;

            if (!shouldDraw)
            {
                return;
            }
            
            EditorGUI.BeginChangeCheck();
            entry.type = (RemoteConfigValueType)EditorGUI.EnumPopup(rect, entry.type);
            if (EditorGUI.EndChangeCheck())
            {
                _editorData.NotifyEntryTypeChanged(entry);
                RefreshDrawingData();
                _requireFilterFlush = _typeFilter != RemoteConfigValueTypeFilter.None;
            }
        }

        private void DrawValueEntry(RemoteConfigEditorEntry entry, ref Rect rect, bool shouldDraw)
        {
            rect.x += rect.width + 10;
            rect.width = _settings.valueWidth - 10;

            EditorGUI.BeginChangeCheck();
            entry.DrawValueEntry(ref rect, shouldDraw);
            if (EditorGUI.EndChangeCheck())
            {
                _editorData.SetDirty();
            }
        }

        private void DrawCategoryEntry(RemoteConfigEditorEntry editorEntry, ref Rect rect, bool shouldDraw)
        {
            rect.x += rect.width + 10;
            float posY = rect.y;
            rect.y = _appEntryStartPosY + _settings.boxBorderOffset;
            rect.width = _settings.categoryWidth - 10;
            rect.height = EditorGUIUtility.singleLineHeight;

            if (shouldDraw)
            {
                EditorGUI.BeginChangeCheck();
                int index = EditorGUI.Popup(rect, editorEntry.categoryIndex, _sortingCategories);
                if (EditorGUI.EndChangeCheck())
                {
                    _editorData.NotifyEntryCategoryChanged(editorEntry, index);
                    RefreshDrawingData();
                }
                
                if (!editorEntry.respectCategoryPrefix)
                {
                    rect.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                    rect.height = _settings.lineHeight - (EditorGUIUtility.standardVerticalSpacing * 2 + EditorGUIUtility.singleLineHeight + _settings.fixBadCategoryPrefixHeight);
                    EditorGUI.HelpBox(rect, "This key does not respect the prefix for the selected category.", MessageType.Warning);
                    
                    rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                    rect.height = _settings.fixBadCategoryPrefixHeight;
                    if (GUI.Button(rect, "Fix"))
                    {
                        string prefix = _editorData.allCategories.Find(x => x.Key == editorEntry.category).Value;
                        string oldKey = editorEntry.key;
                        editorEntry.key = $"{prefix}{editorEntry.key}";
                        _editorData.NotifyAppEntryKeyChanged(oldKey, editorEntry);
                    }
                }
            }

            rect.y = posY;
        }

        private void DrawDeletionButton(RemoteConfigEditorEntry entry, ref Rect rect, bool shouldDraw)
        {
            _deleteBtnRect.y = rect.y;
            _deleteBtnRect.x = rect.x + _settings.boxBorderOffset;
            if (!shouldDraw)
            {
                return;
            }
            
            if (GUI.Button(_deleteBtnRect, _settings.deleteBtnTexture, _deleteDataEntryButtonStyle))
            {
                _editorData.NotifyAppEntryDeleted(entry);
                RefreshDrawingData();
            }
        }

        #endregion

        #endregion

        #region Tools
        
        private void DrawLine(Vector3 from, Vector3 to)
        {
            Handles.BeginGUI();
            Handles.color = _settings.lineSeparationColor;
            Handles.DrawLine(from, to);
            Handles.EndGUI();
        }

        private void RefreshDrawingData()
        {
            RefreshDrawingList();
            RefreshScrollSize();
        }
        
        private void DrawSectionFoldout(string content, ref bool flag)
        {
            GUI.backgroundColor = _settings.foldoutBackgroundColor;
            float w = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = position.width - 15;
            flag = EditorGUILayout.Foldout(flag, content, true, _sectionFoldoutStyle);
            GUI.backgroundColor = _baseGUIColor;
            EditorGUIUtility.labelWidth = w;
        }

        private void Save(Object toSave)
        {
            EditorUtility.SetDirty(toSave);
            AssetDatabase.SaveAssetIfDirty(toSave);
        }

        private void CreateAndBindEmptyJson()
        {
            string servicePath = AssetDatabase.GetAssetPath(_remoteConfigService);
            string directoryPath = Path.GetDirectoryName(servicePath);
            string projectRelativePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directoryPath!, RemoteConfigService.FileName));
            
            string absolutePath = IOExtender.RelativeToAbsolutePath(projectRelativePath);
            File.WriteAllText(absolutePath, "{\n \"version\": 1,\n \"platforms\": [], \n \"entries\": [] \n}");
            
            AssetDatabase.Refresh();

            _remoteConfigService.LocalTranslationFile = AssetDatabase.LoadAssetAtPath<TextAsset>(projectRelativePath);
            Save(_remoteConfigService);
        }

        private void CreateAndBindEditorDataSaver()
        {
            string servicePath = AssetDatabase.GetAssetPath(_remoteConfigService);
            string directoryPath = Path.GetDirectoryName(servicePath) + "/Editor";
            string projectRelativePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directoryPath!, "RC-EditorDataSaver.asset"));

            var so = CreateInstance<RemoteConfigEditorData>();
            AssetDatabase.CreateAsset(so, projectRelativePath);
            AssetDatabase.Refresh();

            _editorData = AssetDatabase.LoadAssetAtPath<RemoteConfigEditorData>(projectRelativePath);
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CCLBStudio.RemoteConfig
{
    [Serializable]
    public class RemoteConfigEditorEntry
    {
        public string key;
        public RemoteConfigValueType type;
        public string category = string.Empty;
        public int categoryIndex = 0;
        public bool respectCategoryPrefix = true;
        public bool isValid = true;

        public int intValue;
        public bool boolValue;
        public string stringValue;
        public float floatValue;
        public List<RemoteConfigKeyValuePair<SystemLanguage, string>> translatableValue = new();

        [SerializeField] private RemoteConfigEditorData editorData;
        [SerializeField] private RemoteConfigEditWindowSettings editSettings;

        private delegate void DrawingDelegate(ref Rect rect, bool shouldDraw);
        private DrawingDelegate _drawingMethod;
        private Func<float> _valueHeight;
        private Dictionary<SystemLanguage, CultureInfo> _languageCultureInfos;
        private Rect _langLabelRect;

        public RemoteConfigEditorEntry(RemoteConfigValueType type, RemoteConfigEditorData editorData)
        {
            this.editorData = editorData;
            editSettings = editorData.settings;
            this.type = type;
            key = string.Empty;
            Refresh();
        }

        public RemoteConfigEditorEntry(RemoteConfigEntry entry, RemoteConfigEditorData editorData)
        {
            this.editorData = editorData;
            editSettings = editorData.settings;
            key = entry.key;
            type = entry.type;
            category = entry.category;

            switch (type)
            {
                case RemoteConfigValueType.Int:
                    intValue = ((RemoteConfigIntEntry)entry).value;
                    break;
                
                case RemoteConfigValueType.Float:
                    floatValue = ((RemoteConfigFloatEntry)entry).value;
                    break;
                
                case RemoteConfigValueType.Bool:
                    boolValue = ((RemoteConfigBoolEntry)entry).value;
                    break;
                
                case RemoteConfigValueType.String:
                    stringValue = ((RemoteConfigStringEntry)entry).value;
                    break;
                
                case RemoteConfigValueType.Translatable:
                    //translatableValue = new SerializableDictionary<SystemLanguage, string>(((RemoteConfigTranslatableEntry)entry).value);
                    translatableValue = new List<RemoteConfigKeyValuePair<SystemLanguage, string>>();
                    var translatableEntry = (RemoteConfigTranslatableEntry)entry;
                    foreach (var pair in translatableEntry.value)
                    {
                        translatableValue.Add(new RemoteConfigKeyValuePair<SystemLanguage, string>(pair.Key, pair.Value));
                    }

                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Refresh();
        }

        public void Refresh()
        {
            editSettings = editorData.settings;

            RefreshLanguages();
            RefreshDrawingMethods();
        }

        private void RefreshLanguages()
        {
            foreach (var lang in editorData.allLanguages)
            {
                int index = translatableValue.FindIndex(x => x.Key == lang.language);
                if (index < 0)
                {
                    translatableValue.Add(new RemoteConfigKeyValuePair<SystemLanguage, string>(lang.language, string.Empty));
                }
            }

            List<SystemLanguage> toDelete = new List<SystemLanguage>();
            foreach (var lang in translatableValue.Select(x => x.Key))
            {
                if (editorData.allLanguages.Find(x => x.language == lang) == null)
                {
                    toDelete.Add(lang);
                }
            }

            foreach (var lang in toDelete)
            {
                int index = translatableValue.FindIndex(x => x.Key == lang);
                translatableValue.RemoveAt(index);
            }
        }

        #region Data Events

        private void RefreshDrawingMethods()
        {
            switch (type)
            {
                case RemoteConfigValueType.Int:
                    _drawingMethod = DrawIntField;
                    _valueHeight = GetDefaultHeight;
                    break;
                
                case RemoteConfigValueType.Float:
                    _drawingMethod = DrawFloatField;
                    _valueHeight = GetDefaultHeight;
                    break;
                
                case RemoteConfigValueType.Bool:
                    _drawingMethod = DrawBoolField;
                    _valueHeight = GetDefaultHeight;
                    break;
                
                case RemoteConfigValueType.String:
                    _drawingMethod = DrawStringField;
                    _valueHeight = GetDefaultHeight;
                    break;
                
                case RemoteConfigValueType.Translatable:
                    _langLabelRect = new Rect(0f, 0f, 25f, 20f);
                    _drawingMethod = DrawTranslatableField;
                    _valueHeight = GetTranslatableHeight;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void NotifyTypeChange()
        {
            RefreshDrawingMethods();
        }

        public void NotifyLanguageAdded(RemoteConfigEditorLanguage newLanguage)
        {
            int index = translatableValue.FindIndex(x => x.Key == newLanguage.language);
            if (index >= 0)
            {
                return;
            }

            translatableValue.Add(new RemoteConfigKeyValuePair<SystemLanguage, string>(newLanguage.language, string.Empty));
        }

        public void NotifyLanguageRemoved(RemoteConfigEditorLanguage removedLanguage)
        {
            int index = translatableValue.FindIndex(x => x.Key == removedLanguage.language);
            translatableValue.RemoveAt(index);
        }

        #endregion

        #region Drawing Methods
        
        public void DrawValueEntry(ref Rect rect, bool shouldDraw)
        {
            if (_drawingMethod == null)
            {
                RefreshDrawingMethods();
            }
            
            _drawingMethod?.Invoke(ref rect, shouldDraw);
        }

        public float GetValueHeight()
        {
            if (_valueHeight == null)
            {
                RefreshDrawingMethods();
            }
            
            return _valueHeight!.Invoke();
        }

        private float GetDefaultHeight() => editSettings.lineHeight + editSettings.boxBorderOffset * 2;

        private float GetTranslatableHeight() =>Mathf.Max(GetDefaultHeight(),  isValid ? translatableValue.Count * editSettings.lineHeight + (translatableValue.Count - 1) * EditorGUIUtility.standardVerticalSpacing + editSettings.boxBorderOffset * 2 : GetDefaultHeight());

        private void DrawIntField(ref Rect rect, bool shouldDraw)
        {
            if (!shouldDraw)
            {
                return;
            }
            intValue = EditorGUI.IntField(rect, intValue);
        }
        
        private void DrawFloatField(ref Rect rect, bool shouldDraw)
        {
            if (!shouldDraw)
            {
                return;
            }
            floatValue = EditorGUI.FloatField(rect, floatValue);
        }
        
        private void DrawBoolField(ref Rect rect, bool shouldDraw)
        {
            if (!shouldDraw)
            {
                return;
            }
            boolValue = EditorGUI.Toggle(rect, boolValue);
        }
        
        private void DrawStringField(ref Rect rect, bool shouldDraw)
        {
            if (!shouldDraw)
            {
                return;
            }
            stringValue = EditorGUI.TextField(rect, stringValue);
        }
        
        private void DrawTranslatableField(ref Rect rect, bool shouldDraw)
        {
            int index = 0;
            _langLabelRect.x = rect.x;
            _langLabelRect.y = rect.y;
            rect.x += _langLabelRect.width;
            rect.width -= _langLabelRect.width;
            rect.height = editSettings.lineHeight;

            float startRectPosY = rect.y;
            float startY = rect.y + rect.height / 3f;

            if (editorData.allLanguages.Count <= 0)
            {
                EditorGUI.HelpBox(rect, "No language added !", MessageType.Warning);
            }
            else
            {
                foreach (var lang in editorData.allLanguages)
                {
                    float rectOffset = index * (rect.height + EditorGUIUtility.standardVerticalSpacing);
                    _langLabelRect.y = startY + rectOffset;
                    rect.y = startRectPosY + rectOffset;
                
                    if (shouldDraw)
                    {
                        EditorGUI.LabelField(_langLabelRect, GetCultureInfo(lang.language).TwoLetterISOLanguageName.ToUpper());
                        int i = translatableValue.FindIndex(x => x.Key == lang.language);
                        translatableValue[i].Value = EditorGUI.TextArea(rect, translatableValue[i].Value, EditorStyles.textArea);
                    }

                    index++;
                }
            }
        }

        private CultureInfo GetCultureInfo(SystemLanguage lang)
        {
            _languageCultureInfos ??= new Dictionary<SystemLanguage, CultureInfo>(editorData.allLanguages.Count);
            
            if (!_languageCultureInfos.ContainsKey(lang))
            {
                var infos = lang.ToCultureInfo();
                _languageCultureInfos.Add(lang, infos);
                return infos;
            }
            
            if (_languageCultureInfos[lang] == null)
            {
                var infos = lang.ToCultureInfo();
                _languageCultureInfos[lang] = infos;
                return infos;
            }

            return _languageCultureInfos[lang];
        }

        #endregion

        #region Json Methods

        public RemoteConfigEntryJson ToRemoteConfigJson()
        {
            return new RemoteConfigEntryJson
            {
                key = key,
                type = type,
                category = category,
                value = GetValueObject()
            };
        }

        private object GetValueObject()
        {
            switch (type)
            {
                case RemoteConfigValueType.Int:
                    return intValue;
                
                case RemoteConfigValueType.Float:
                    return floatValue;
                
                case RemoteConfigValueType.Bool:
                    return boolValue;
                
                case RemoteConfigValueType.String:
                    return stringValue;
                
                case RemoteConfigValueType.Translatable:
                    Dictionary<string, string> jsonTranslations = new Dictionary<string, string>(translatableValue.Count);

                    foreach (var pair in translatableValue)
                    {
                        jsonTranslations.Add(pair.Key.ToString(), pair.Value);
                    }
                    
                    return jsonTranslations;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}
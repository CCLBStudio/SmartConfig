using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class SmartConfigEditorEntry
    {
        public string key;
        public SmartConfigValueType type;
        public string category = string.Empty;
        public int categoryIndex = 0;
        public bool respectCategoryPrefix = true;
        public bool isValid = true;

        public int intValue;
        public bool boolValue;
        public string stringValue;
        public float floatValue;
        public List<SmartConfigKeyValuePair<SystemLanguage, string>> translatableValue = new();

        [SerializeField] private SmartConfigEditorData editorData;
        [SerializeField] private SmartConfigEditWindowSettings editSettings;

        private delegate void DrawingDelegate(ref Rect rect, bool shouldDraw);
        private DrawingDelegate _drawingMethod;
        private Func<float> _valueHeight;
        private Rect _langLabelRect;

        public SmartConfigEditorEntry(SmartConfigValueType type, SmartConfigEditorData editorData)
        {
            this.editorData = editorData;
            editSettings = editorData.settings;
            this.type = type;
            key = string.Empty;
            Refresh();
        }

        public SmartConfigEditorEntry(SmartConfigEntry entry, SmartConfigEditorData editorData)
        {
            this.editorData = editorData;
            editSettings = editorData.settings;
            key = entry.key;
            type = entry.type;
            category = entry.category;

            switch (type)
            {
                case SmartConfigValueType.Int:
                    intValue = ((SmartConfigIntEntry)entry).value;
                    break;
                
                case SmartConfigValueType.Float:
                    floatValue = ((SmartConfigFloatEntry)entry).value;
                    break;
                
                case SmartConfigValueType.Bool:
                    boolValue = ((SmartConfigBoolEntry)entry).value;
                    break;
                
                case SmartConfigValueType.String:
                    stringValue = ((SmartConfigStringEntry)entry).value;
                    break;
                
                case SmartConfigValueType.Translatable:
                    translatableValue = new List<SmartConfigKeyValuePair<SystemLanguage, string>>();
                    var translatableEntry = (SmartConfigTranslatableEntry)entry;
                    foreach (var pair in translatableEntry.value)
                    {
                        translatableValue.Add(new SmartConfigKeyValuePair<SystemLanguage, string>(pair.Key, pair.Value));
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
                    translatableValue.Add(new SmartConfigKeyValuePair<SystemLanguage, string>(lang.language, string.Empty));
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
                case SmartConfigValueType.Int:
                    _drawingMethod = DrawIntField;
                    _valueHeight = GetDefaultHeight;
                    break;
                
                case SmartConfigValueType.Float:
                    _drawingMethod = DrawFloatField;
                    _valueHeight = GetDefaultHeight;
                    break;
                
                case SmartConfigValueType.Bool:
                    _drawingMethod = DrawBoolField;
                    _valueHeight = GetDefaultHeight;
                    break;
                
                case SmartConfigValueType.String:
                    _drawingMethod = DrawStringField;
                    _valueHeight = GetDefaultHeight;
                    break;
                
                case SmartConfigValueType.Translatable:
                    _langLabelRect = new Rect(0f, 0f, 25f, 25f * 0.6667f);
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

        public void NotifyLanguageAdded(SmartConfigEditorLanguage newLanguage)
        {
            int index = translatableValue.FindIndex(x => x.Key == newLanguage.language);
            if (index >= 0)
            {
                return;
            }

            translatableValue.Add(new SmartConfigKeyValuePair<SystemLanguage, string>(newLanguage.language, string.Empty));
        }

        public void NotifyLanguageRemoved(SmartConfigEditorLanguage removedLanguage)
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
            float startY = rect.y + rect.height / 4f;

            if (editorData.allLanguages.Count <= 0)
            {
                EditorGUI.HelpBox(rect, "No language added !", MessageType.Warning);
            }
            else
            {
                foreach (SmartConfigEditorLanguage lang in editorData.allLanguages)
                {
                    float rectOffset = index * (rect.height + EditorGUIUtility.standardVerticalSpacing);
                    _langLabelRect.y = startY + rectOffset;
                    rect.y = startRectPosY + rectOffset;
                
                    if (shouldDraw)
                    {
                        EditorGUI.LabelField(_langLabelRect, lang.twoLettersIsoDisplay);
                        _langLabelRect.y += _langLabelRect.height;
                        _langLabelRect.x -= 4f;
                        EditorGUI.DrawPreviewTexture(_langLabelRect, lang.flag);
                        _langLabelRect.x += 4f;

                        int i = translatableValue.FindIndex(x => x.Key == lang.language);
                        translatableValue[i].Value = EditorGUI.TextArea(rect, translatableValue[i].Value, EditorStyles.textArea);
                    }

                    index++;
                }
            }
        }

        #endregion

        #region Json Methods

        public SmartConfigEntryJson ToSmartConfigJson()
        {
            return new SmartConfigEntryJson
            {
                key = key,
                type = type.ToString(),
                category = category,
                value = GetValueObject()
            };
        }

        private string GetValueObject()
        {
            switch (type)
            {
                case SmartConfigValueType.Int:
                    return intValue.ToString();
                
                case SmartConfigValueType.Float:
                    return floatValue.ToString(CultureInfo.InvariantCulture);
                
                case SmartConfigValueType.Bool:
                    return boolValue.ToString();
                
                case SmartConfigValueType.String:
                    return stringValue;
                
                case SmartConfigValueType.Translatable:
                    SmartConfigJsonTranslatableDictionary jsonTranslations = new SmartConfigJsonTranslatableDictionary(translatableValue);
                    string serializedJson = JsonUtility.ToJson(jsonTranslations);
                    return serializedJson;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}
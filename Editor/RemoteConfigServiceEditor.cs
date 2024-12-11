using System.Collections.Generic;
using ReaaliStudio.Utils.Extensions;
using UnityEditor;
using UnityEngine;

namespace CCLBStudio.RemoteConfig
{
    [CustomEditor(typeof(RemoteConfigService))]
    public class RemoteConfigServiceEditor : Editor
    {
        private SerializedProperty _defaultLanguage;
        private SerializedProperty _localTranslationFile;
        private SerializedProperty _existingLanguages;
        private List<SerializedProperty> _remainingProperties;

        private GUIStyle _buttonStyle;
        private bool _init;
    
        private void OnEnable()
        {
            _defaultLanguage = serializedObject.FindProperty(RemoteConfigService.DefaultLanguageProperty);
            _localTranslationFile = serializedObject.FindProperty(RemoteConfigService.LocalTranslationFileProperty);
            _existingLanguages = serializedObject.FindProperty(RemoteConfigService.ExistingLanguagesProperty);

            _remainingProperties = EditorExtender.GetSelfPropertiesExcluding(serializedObject, RemoteConfigService.DefaultLanguageProperty,
                RemoteConfigService.LocalTranslationFileProperty, RemoteConfigService.ExistingLanguagesProperty);
        }
    
        public override void OnInspectorGUI()
        {
            if (!_init)
            {
                SetupStyles();
                _init = true;
            }
            
            serializedObject.Update();
            EditorExtender.DrawScriptField(serializedObject);
            
            GUILayout.Space(10);
            
            EditorGUILayout.PropertyField(_defaultLanguage);
            EditorGUILayout.PropertyField(_localTranslationFile);

            GUI.enabled = false;
            EditorGUILayout.PropertyField(_existingLanguages);
            GUI.enabled = true;
            
            foreach (var p in _remainingProperties)
            {
                EditorGUILayout.PropertyField(p);
            }

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Edit Remote Config", _buttonStyle))
            {
                RemoteConfigEditWindow.ShowWindow();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void SetupStyles()
        {
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 50f,
                fixedWidth = 400f,
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.yellow
                }
            };
        }
    }
}

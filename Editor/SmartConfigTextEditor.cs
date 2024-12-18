using CCLBStudio.SmartConfig;
using UnityEditor;

namespace ReaaliStudio.Core.Services.SmartConfig
{
    [CustomEditor(typeof(SmartConfigText))]
    public class SmartConfigTextEditor : Editor
    {
        private SerializedProperty _keyType;
        private SerializedProperty _key;
        private SerializedProperty _keyBridge;
        private SerializedProperty _service;
        
        private void OnEnable()
        {
            _keyType = serializedObject.FindProperty(SmartConfigText.KeyTypeProperty);
            _key = serializedObject.FindProperty(SmartConfigText.KeyProperty);
            _keyBridge = serializedObject.FindProperty(SmartConfigText.KeyBridgeProperty);
            _service = serializedObject.FindProperty(SmartConfigText.ServiceProperty);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ScEditorExtender.DrawScriptField(serializedObject);

            EditorGUILayout.PropertyField(_service);
            EditorGUILayout.PropertyField(_keyType);

            SmartConfigText.SmartConfigTextKeyType keyType = (SmartConfigText.SmartConfigTextKeyType)_keyType.enumValueIndex;
            EditorGUILayout.PropertyField(keyType == SmartConfigText.SmartConfigTextKeyType.Literal ? _key : _keyBridge);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
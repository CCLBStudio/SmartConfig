using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    public static class RcEditorExtender
    {
        public static List<SerializedProperty> GetSelfPropertiesExcluding(SerializedObject serializedObject, params string[] toExclude)
        {
            var fields = serializedObject.targetObject.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            List<SerializedProperty> properties = new List<SerializedProperty>();
            foreach (var f in fields)
            {
                if (toExclude != null && toExclude.Any(f.Name.Contains))
                {
                    continue;
                }

                var property = serializedObject.FindProperty(f.Name);
                if (property != null)
                {
                    properties.Add(property);
                }
            }

            return properties;
        }
        
        public static void DrawScriptField(SerializedObject serializedObject)
        {
            GUI.enabled = false;
            if (serializedObject.targetObject is MonoBehaviour behaviour)
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(behaviour), typeof(MonoBehaviour), false);
            }
            else
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((ScriptableObject)serializedObject.targetObject), typeof(ScriptableObject), false);
            }
            GUI.enabled = true;
            GUILayout.Space(5);
        }
        
        public static T LoadScriptableAsset<T>() where T : ScriptableObject
        {
            string[] assetPath = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            if (assetPath.Length <= 0)
            {
                Debug.Log($"There is no asset of type {typeof(T).Name} in the project.");
                return null;
            }

            T result = (T)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetPath[0]), typeof(T));

            return result;
        }
    }
}

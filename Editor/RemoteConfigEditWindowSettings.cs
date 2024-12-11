using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EditorCools;
using UnityEditor;
using UnityEngine;

namespace CCLBStudio.RemoteConfig
{
    [CreateAssetMenu(menuName = "CCLB Studio/Remote Config/Editor/Settings")]
    public class RemoteConfigEditWindowSettings : ScriptableObject
    {
        [Header("Languages Settings")]
        [Min(5)] public int languageFontSize = 15;
        [Min(10f)] public float languageLineHeight = 15f;
            
        [Header("Foldout Settings")]
        public Color foldoutNameColor = Color.yellow;
        public Color foldoutBackgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Common Header Settings")]
        public float headerButtonWidth = 70;
        public float headerButtonHeight = 50;
        public Texture syncFromCloudTexture;
        public Texture saveToJsonTexture;
        public Texture uploadTexture;
        public Texture saveLocalTexture;
        public Texture syncFromJsonTexture;
        public string syncFromCloudTooltip = "Fetch the Json file on cloud and override all current data with it.";
        public string saveToJsonTooltip = "Overwrite the local Json file with the current data.";
        public string uploadTooltip = "Upload the local Json file to the remote server, overriding any file present on cloud.";
        public string saveLocalTooltip = "Save the current data locally, but does NOT overwrite the local json.";
        public string syncFromJsonTooltip = "Refresh the current data with the local Json file.";
        public float spaceBetweenHeaderButtons = 15f;
        
        [Header("App Entries Settings")]
        public Texture deleteBtnTexture;
        [Min(50f)] public float keyWidth = 300f;
        [Min(50f)] public float typeWidth = 300f;
        [Min(50f)] public float valueWidth = 300f;
        [Min(50f)] public float categoryWidth = 300f;
        [Min(50f)] public float lineHeight = 100f;
        [Min(1f)] public float spacingBetweenElements = 2f;
        [Min(0f)] public float bottomScrollOffset = 100f;
        [Range(0, 20)] public float boxBorderOffset = 6;
        public Color lineSeparationColor = Color.gray;
        [Range(18, 36)] public float fixBadCategoryPrefixHeight = 25;

        [Header("Usage Report Settings")]
        [Min(50)] public float keyUsageKeyWidth = 100f;
        [Min(50)] public float keyUsageCountWidth = 100f;
        [Min(50)] public float keyUsageDeleteButtonWidth = 100f;
        [Min(0)] public float keyUsageLineSizeOffset = 6;

        [Header("Language Flags")]
        public List<RemoteConfigKeyValuePair<SystemLanguage, Texture>> languageFlags;
        [SerializeField] private string flagsFolder;
        
        [Button]
        private void BuildFlagPairs()
        {
            string absoluteFolderPath = IOExtender.RelativeToAbsolutePath(flagsFolder);
            languageFlags = new List<RemoteConfigKeyValuePair<SystemLanguage, Texture>>();
            foreach (SystemLanguage lang in Enum.GetValues(typeof(SystemLanguage)))
            {
                if (lang == SystemLanguage.Unknown)
                {
                    continue;
                }

                string twoLetters = lang.ToTwoLettersCountry().ToLower();
                string flagPath = $"{absoluteFolderPath}/{twoLetters}.png";
                if (File.Exists(flagPath))
                {
                    languageFlags.Add(new RemoteConfigKeyValuePair<SystemLanguage, Texture>(lang, AssetDatabase.LoadAssetAtPath<Texture>(IOExtender.AbsoluteToProjectRelativePath(flagPath))));
                }
                else
                {
                    Debug.LogError($"No flag for {lang} at path {flagPath} !");
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        [Button]
        private void RemoveUnusedFlags()
        {
            string absoluteFolderPath = IOExtender.RelativeToAbsolutePath(flagsFolder);
            foreach (var path in Directory.EnumerateFiles(absoluteFolderPath))
            {
                string n = Path.GetFileNameWithoutExtension(path);
                if (languageFlags.Any(x => x.Value.name.Contains(n)))
                {
                    continue;
                }

                File.Delete(path);
            }
            
            AssetDatabase.Refresh();
        }
    }
}
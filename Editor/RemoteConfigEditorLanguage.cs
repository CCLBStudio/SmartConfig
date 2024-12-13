using System;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class RemoteConfigEditorLanguage
    {
        public SystemLanguage language;
        public string twoLettersIsoDisplay;
        public string languageName;
        public Texture flag;
    }
}
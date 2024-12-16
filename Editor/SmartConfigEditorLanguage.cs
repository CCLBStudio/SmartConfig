using System;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [Serializable]
    public class SmartConfigEditorLanguage
    {
        public SystemLanguage language;
        public string twoLettersIsoDisplay;
        public string languageName;
        public Texture flag;
    }
}
using System;
using UnityEngine;

namespace CCLBStudio.RemoteConfig
{
    [Serializable]
    public class RemoteConfigEditorLanguage
    {
        public SystemLanguage language;
        public string twoLettersIsoDisplay;
        public Texture flag;
    }
}
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [CreateAssetMenu(fileName = "NewSmartConfigKeyBridge", menuName = "CCLB Studio/Smart Config/Key Bridge")]
    public class SmartConfigKeyBridge : ScriptableObject
    {
        public string key;
    }
}
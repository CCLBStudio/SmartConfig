using System.Globalization;
using TMPro;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class SmartConfigText : MonoBehaviour, ISmartConfigListener
    {
        #region Editor
        #if UNITY_EDITOR

        public static string KeyTypeProperty => nameof(keyType);
        public static string KeyProperty => nameof(key);
        public static string KeyBridgeProperty => nameof(keyBridge);
        public static string ServiceProperty => nameof(service);
        
        #endif
        #endregion
        
        [SerializeField] private SmartConfigService service;
        [SerializeField] private SmartConfigTextKeyType keyType = SmartConfigTextKeyType.Literal;
        [SerializeField] private string key;
        [SerializeField] private SmartConfigKeyBridge keyBridge;
        private TextMeshProUGUI _tmPro;
        public enum SmartConfigTextKeyType {Literal, Bridge}

        protected void Start()
        {
            if (!service)
            {
                Debug.LogError("Smart Config Service is null !");
                Destroy(this);
                return;
            }
        
            _tmPro = GetComponent<TextMeshProUGUI>();
            ApplySmartConfigKey();
            service.AddListener(this);
        }

        private void OnDestroy()
        {
            service.RemoveListener(this);
        }

        private void ApplySmartConfigKey()
        {
            if (!_tmPro || !service)
            {
                Debug.LogError("Can't translate text !");
                return;
            }

            if (keyType == SmartConfigTextKeyType.Bridge && !keyBridge)
            {
                Debug.LogError($"You configured you config text to use a key bridge but you have not provided a bridge. Unable to read key. Happened on objet {name} in scene {gameObject.scene.name}.");
                return;
            }

            string configKey = keyType == SmartConfigTextKeyType.Literal ? key : keyBridge.key;
            
            if (service.GetString(configKey, out string value))
            {
                _tmPro.text = value;
                return;
            }
            
            if (service.GetBool(configKey, out bool b))
            {
                _tmPro.text = b.ToString();
                return;
            }
            
            if (service.GetFloat(configKey, out float f))
            {
                _tmPro.text = f.ToString(CultureInfo.InvariantCulture);
                return;
            }
            
            if (service.GetInt(configKey, out int i))
            {
                _tmPro.text = i.ToString();
            }
        }

        public void OnConfigLoaded()
        {
        }

        public void OnConfigLanguageSelected()
        {
            ApplySmartConfigKey();
        }
    }
}

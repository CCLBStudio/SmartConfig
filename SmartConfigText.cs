using System.Globalization;
using TMPro;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class SmartConfigText : MonoBehaviour, ISmartConfigListener
    {
        [SerializeField] private SmartConfigService service;
        [SerializeField] private string key;
        private TextMeshProUGUI _tmPro;

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

            if (service.GetString(key, out string value))
            {
                _tmPro.text = value;
                return;
            }
            
            if (service.GetBool(key, out bool b))
            {
                _tmPro.text = b.ToString();
                return;
            }
            
            if (service.GetFloat(key, out float f))
            {
                _tmPro.text = f.ToString(CultureInfo.InvariantCulture);
                return;
            }
            
            if (service.GetInt(key, out int i))
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

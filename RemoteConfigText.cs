using TMPro;
using UnityEngine;

namespace CCLBStudio.SmartConfig
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class RemoteConfigText : MonoBehaviour, IRemoteConfigListener
    {
        [SerializeField] private RemoteConfigService service;
        [SerializeField] private string key;
        private TextMeshProUGUI _tmPro;

        protected void Start()
        {
            if (!service)
            {
                Debug.LogError("Remote Config Service is null !");
                Destroy(this);
                return;
            }
        
            _tmPro = GetComponent<TextMeshProUGUI>();
            ApplyRemoteConfigKey();
            service.AddListener(this);
        }

        private void OnDestroy()
        {
            service.RemoveListener(this);
        }

        private void ApplyRemoteConfigKey()
        {
            if (!_tmPro || !service)
            {
                Debug.LogError("Can't translate text !");
                return;
            }

            if (service.GetString(key, out string value))
            {
                _tmPro.text = value;
            }
        }

        public void OnRemoteConfigLoaded()
        {
        }

        public void OnRemoteConfigLanguageSelected()
        {
            ApplyRemoteConfigKey();
        }
    }
}

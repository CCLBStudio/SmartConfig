using TMPro;
using UnityEngine;

namespace CCLBStudio.SmartConfig.Demo
{
    public class RemoteConfigTester : MonoBehaviour
    {
        [SerializeField] private RemoteConfigService service;
        [SerializeField] private SystemLanguage language;
        [SerializeField] private TextMeshProUGUI text;
    
        void Start()
        {
            service.SelectLanguage(language);
            service.LoadFromCloud(null, DownloadSucceeded, DownloadFailed);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                service.SelectLanguage(service.CurrentLanguage == SystemLanguage.English ? SystemLanguage.French : SystemLanguage.English);
                if (text.GetComponent<RemoteConfigText>()) // return in case we have a remote config text taking care of the text display
                {
                    return;
                }
            
                if (service.GetString("app_my_translatable", out string value))
                {
                    text.text = value;
                }
            }
        }

        private void DownloadSucceeded()
        {
            Debug.Log("Remote config successfully downloaded from cloud and loaded into the service !");
            if (text.GetComponent<RemoteConfigText>()) // return in case we have a remote config text taking care of the text display
            {
                return;
            }
        
            if (service.GetString("app_my_translatable", out string value))
            {
                text.text = value;
            }
        }

        private void DownloadFailed()
        {
            Debug.Log("Failed to download remote config. No data loaded.");
        }
    }
}

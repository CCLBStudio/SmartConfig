using TMPro;
using UnityEngine;

namespace CCLBStudio.SmartConfig.Demo
{
    public class SmartConfigTester : MonoBehaviour
    {
        [SerializeField] private SmartConfigService service;
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
                if (text.GetComponent<SmartConfigText>()) // return in case we have a smart config text taking care of the text display
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
            Debug.Log("Smart config successfully downloaded from cloud and loaded into the service !");
            if (text.GetComponent<SmartConfigText>()) // return in case we have a smart config text taking care of the text display
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
            Debug.Log("Failed to download smart config. No data loaded.");
        }
    }
}

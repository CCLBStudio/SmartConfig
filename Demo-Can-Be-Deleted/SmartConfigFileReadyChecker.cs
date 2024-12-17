using TMPro;
using UnityEngine;

namespace CCLBStudio.SmartConfig.Demo
{
    public class SmartConfigFileReadyChecker : MonoBehaviour, ISmartConfigListener
    {
        [SerializeField] private SmartConfigService service;

        private void Start()
        {
            service.AddListener(this);
        }

        public void OnConfigLoaded()
        {
            GetComponent<TextMeshProUGUI>().text = "Config file ready : true !";
        }

        public void OnConfigLanguageSelected()
        {
        }
    }
}

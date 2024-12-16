using CCLBStudio.SmartConfig;
using TMPro;
using UnityEngine;

public class SmartConfigFileReadyChecker : MonoBehaviour, ISmartConfigListener
{
    [SerializeField] private SmartConfigService service;

    private void Start()
    {
        service.AddListener(this);
    }

    public void OnRemoteConfigLoaded()
    {
        GetComponent<TextMeshProUGUI>().text = "Config file ready : true !";
    }

    public void OnRemoteConfigLanguageSelected()
    {
    }
}

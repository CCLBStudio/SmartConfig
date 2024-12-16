namespace CCLBStudio.SmartConfig
{
    public interface ISmartConfigListener
    {
        public void OnRemoteConfigLoaded();
        public void OnRemoteConfigLanguageSelected();
    }
}

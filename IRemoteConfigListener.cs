namespace CCLBStudio.SmartConfig
{
    public interface IRemoteConfigListener
    {
        public void OnRemoteConfigLoaded();
        public void OnRemoteConfigLanguageSelected();
    }
}

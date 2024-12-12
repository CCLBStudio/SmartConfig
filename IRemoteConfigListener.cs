namespace CCLBStudio.RemoteConfig
{
    public interface IRemoteConfigListener
    {
        public void OnRemoteConfigLoaded();
        public void OnRemoteConfigLanguageSelected();
    }
}

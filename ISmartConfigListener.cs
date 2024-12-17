namespace CCLBStudio.SmartConfig
{
    public interface ISmartConfigListener
    {
        public void OnConfigLoaded();
        public void OnConfigLanguageSelected();
    }
}

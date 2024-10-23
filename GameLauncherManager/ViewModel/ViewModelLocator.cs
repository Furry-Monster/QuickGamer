using SimpleMvvm.Locator;

namespace GameLauncherManager.ViewModel
{
    public class ViewModelLocator : ViewModelLocatorBase
    {
        public ViewModelLocator()
        {
            Register<MainWindowViewModel>();
            Register<PluginSelectorViewModel>();
        }

        public MainWindowViewModel MainWindowViewModel
        {
            get => GetInstance<MainWindowViewModel>();
        }

        public PluginSelectorViewModel PluginSelectorViewModel
        {
            get => GetInstance<PluginSelectorViewModel>();
        }
    }
}
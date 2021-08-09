using SongBrowser.UI.ViewControllers;
using Zenject;

namespace SongBrowser.Installers
{
    class SongBrowserMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<SettingsViewController>().AsSingle();
        }
    }
}

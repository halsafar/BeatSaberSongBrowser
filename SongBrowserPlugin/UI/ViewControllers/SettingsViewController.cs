using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using SongBrowser.Configuration;
using System;
using System.ComponentModel;
using Zenject;

namespace SongBrowser.UI.ViewControllers
{
    public class SettingsViewController : IInitializable, IDisposable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;   

        [UIValue("random-instant-queue")]
        public bool DefaultAllowDuplicates
        {
            get => PluginConfig.Instance.RandomInstantQueue;
            set => PluginConfig.Instance.RandomInstantQueue = value;
        }      

        public void Initialize() => BSMLSettings.instance.AddSettingsMenu(nameof(SongBrowser), "SongBrowser.UI.Views.Settings.bsml", this);
        public void Dispose() => BSMLSettings.instance.RemoveSettingsMenu(this);
    }
}

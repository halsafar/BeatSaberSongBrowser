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
        public bool RandomInstantQueue
        {
            get => PluginConfig.Instance.RandomInstantQueue;
            set => PluginConfig.Instance.RandomInstantQueue = value;
        }

        [UIValue("save-levelid-per-collection")]
        public bool SaveLevelIdPerCollection
        {
            get => PluginConfig.Instance.SaveLevelIdPerCollection;
            set => PluginConfig.Instance.SaveLevelIdPerCollection = value;
        }

        public void Initialize() => BSMLSettings.instance.AddSettingsMenu(nameof(SongBrowser), "SongBrowser.UI.Views.Settings.bsml", this);
        public void Dispose() => BSMLSettings.instance.RemoveSettingsMenu(this);
    }
}

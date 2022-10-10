using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using SongBrowser.Configuration;
using System;
using System.ComponentModel;
using Zenject;

namespace SongBrowser.UI.ViewControllers
{
    public class SettingsViewController : IInitializable, IDisposable
    {
        [UIValue("random-instant-queue")]
        public bool DefaultAllowDuplicates
        {
            get => PluginConfig.Instance.RandomInstantQueueSong;
            set => PluginConfig.Instance.RandomInstantQueueSong = value;
        }

        [UIValue("experimental-scrape-meta-data")]
        public bool ScrapeSongMetaData
        {
            get => PluginConfig.Instance.ExperimentalScrapeSongMetaData;
            set => PluginConfig.Instance.ExperimentalScrapeSongMetaData = value;
        }

        public void Initialize() => BSMLSettings.instance.AddSettingsMenu(nameof(SongBrowser), "SongBrowser.UI.Views.Settings.bsml", this);
        public void Dispose() => BSMLSettings.instance.RemoveSettingsMenu(this);
    }
}

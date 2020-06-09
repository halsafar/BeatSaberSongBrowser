using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser.DataAccess
{
    [Serializable]
    public enum SongSortMode
    {
        Default,
        Author,
        Original,
        Newest,        
        YourPlayCount,
        Difficulty,
        Random,
        PP,
        UpVotes,
        Rating,
        Heat,
        PlayCount,
        Stars,

        // Deprecated
        Favorites,
        Playlist,
        Search
    }

    static class SongSortModeMethods
    {
        public static bool NeedsScoreSaberData(this SongSortMode s)
        {
            switch (s)
            {
                case SongSortMode.UpVotes:
                case SongSortMode.Rating:
                case SongSortMode.PlayCount:
                case SongSortMode.Heat:
                case SongSortMode.PP:
                case SongSortMode.Stars:
                    return true;
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public enum SongFilterMode
    {
        None,
        Favorites,
        Playlist,
        Search,
        Ranked,
        Unranked,

        // For other mods that extend SongBrowser
        Custom
    }

    [Serializable]
    public class SongBrowserSettings
    {
        public static readonly Encoding Utf8Encoding = Encoding.UTF8;
        public static readonly XmlSerializer SettingsSerializer = new XmlSerializer(typeof(SongBrowserSettings));
        public static readonly String DefaultConvertedFavoritesPlaylistName = "SongBrowserPluginFavorites.json";
        public static readonly String MigratedFavoritesPlaylistName = "SongBrowserPluginFavorites_Migrated.json";
        public static readonly String CUSTOM_SONGS_LEVEL_COLLECTION_NAME = "Custom Levels";

        public SongSortMode sortMode = default(SongSortMode);
        public SongFilterMode filterMode = default(SongFilterMode);

        public List<String> searchTerms = default(List<String>);

        public String currentLevelId = default(String);
        public String currentDirectory = default(String);
        public String currentLevelCollectionName = default(String);

        public bool randomInstantQueue = false;
        public bool deleteNumberedSongFolder = true;
        public int randomSongSeed;
        public bool invertSortResults = false;

        [XmlIgnore]
        [NonSerialized]
        public bool DisableSavingSettings = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SongBrowserSettings()
        {
            searchTerms = new List<string>();
        }

        /// <summary>
        /// Helper to acquire settings path at runtime.
        /// </summary>
        /// <returns></returns>
        public static String SettingsPath()
        {
            return Path.Combine(Environment.CurrentDirectory, "song_browser_settings.xml");
        }

        /// <summary>
        /// Backup settings file location.
        /// </summary>
        /// <returns></returns>
        public static String SettingsBackupPath()
        {
            return Path.Combine(Environment.CurrentDirectory, "song_browser_settings.xml.bak");
        }

        /// <summary>
        /// Path to the common favorites file location.
        /// </summary>
        /// <returns></returns>
        public static String DownloaderFavoritesFilePath()
        {
            return Path.Combine(Environment.CurrentDirectory, "favoriteSongs.cfg");
        }

        /// <summary>
        /// Load the settings file for this plugin.
        /// If we fail to load return Default settings.
        /// </summary>
        /// <returns>SongBrowserSettings</returns>
        public static SongBrowserSettings Load()
        {
            Logger.Trace("Load()");
            SongBrowserSettings retVal = null;

            // No Settings file.
            String settingsFilePath = SongBrowserSettings.SettingsPath();
            if (File.Exists(settingsFilePath))
            {
                // Deserialization from JSON
                FileStream fs = null;
                try
                {
                    fs = File.OpenRead(settingsFilePath);
                    XmlSerializer serializer = new XmlSerializer(typeof(SongBrowserSettings));
                    retVal = (SongBrowserSettings)serializer.Deserialize(fs);

                    // Success loading, sane time to make a backup
                    retVal.SaveBackup();
                }
                catch (Exception e)
                {
                    Logger.Exception("Unable to deserialize song browser settings file, using default settings: ", e);
                    retVal = new SongBrowserSettings
                    {
                        DisableSavingSettings = true
                    };
                }
                finally
                {
                    if (fs != null) { fs.Close(); }
                }
            }
            else
            {
                Logger.Debug("Settings file does not exist, returning defaults: " + settingsFilePath);
                retVal = new SongBrowserSettings();
            }

            // check if the playlist directory exists, make it otherwise.
            String playlistDirPath = Path.Combine(Environment.CurrentDirectory, "Playlists");
            if (!Directory.Exists(playlistDirPath))
            {
                Directory.CreateDirectory(playlistDirPath);
            }

            MigrateFavorites();
            ApplyFixes(retVal);

            return retVal;
        }

        /// <summary>
        /// Fix potential breakages in settings.
        /// </summary>
        /// <param name="settings"></param>
        private static void ApplyFixes(SongBrowserSettings settings)
        {
            if (String.Equals(settings.currentLevelCollectionName, "CustomMaps"))
            {
                settings.currentLevelCollectionName = "ModdedCustomMaps";
            }
            else if (String.Equals(settings.currentLevelCollectionName, "ModdedCustomMaps"))
            {
                settings.currentLevelCollectionName = SongBrowserSettings.CUSTOM_SONGS_LEVEL_COLLECTION_NAME;
            }

            settings.Save();
        }

        /// <summary>
        /// Migrate old favorites into new system.
        /// </summary>
        public static void MigrateFavorites()
        {
            String migratedPlaylistPath = Path.Combine(Environment.CurrentDirectory, "Playlists", MigratedFavoritesPlaylistName);
            String oldFavoritesPath = Path.Combine(Environment.CurrentDirectory, "Playlists", DefaultConvertedFavoritesPlaylistName);

            // Skip if already migrated or if the song browser favorites do not exist
            if (!File.Exists(oldFavoritesPath) || File.Exists(migratedPlaylistPath))
            {
                return;
            }

            Logger.Info("Migrating [{0}] into the In-Game favorites.", oldFavoritesPath);

            Playlist oldFavorites = Playlist.LoadPlaylist(oldFavoritesPath);
            PlayerDataModel playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault();
            foreach (PlaylistSong song in oldFavorites.songs)
            {
                string levelID = CustomLevelLoader.kCustomLevelPrefixId + song.hash;
                Logger.Info("Migrating song into ingame favorites: {0}", levelID);
                playerData.playerData.favoritesLevelIds.Add(levelID);
            }

            Logger.Info("Moving [{0}->{1}] into the In-Game favorites.", oldFavoritesPath, migratedPlaylistPath);
            File.Move(oldFavoritesPath, migratedPlaylistPath);

            playerData.Save();
        }

        /// <summary>
        /// Save this Settings insance to file.
        /// </summary>
        public void Save()
        {
            this._Save(SongBrowserSettings.SettingsPath());
        }

        /// <summary>
        /// Save a backup.
        /// </summary>
        public void SaveBackup()
        {
            this._Save(SongBrowserSettings.SettingsBackupPath());
        }

        /// <summary>
        /// Save helper.
        /// </summary>
        private void _Save(String path)
        {
            if (this.DisableSavingSettings)
            {
                Logger.Info("Saving settings has been disabled...");
                return;
            }

            if (searchTerms.Count > 10)
            {
                searchTerms.RemoveRange(10, searchTerms.Count - 10);
            }

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                Indent = true,
                NewLineOnAttributes = true,
                NewLineHandling = System.Xml.NewLineHandling.Entitize
            };

            using (var stream = new StreamWriter(path, false, Utf8Encoding))
            {
                using (var writer = XmlWriter.Create(stream, settings))
                {
                    SettingsSerializer.Serialize(writer, this);
                }
            }
        }
    }
}

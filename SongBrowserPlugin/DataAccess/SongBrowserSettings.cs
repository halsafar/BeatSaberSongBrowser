using SongBrowserPlugin.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using SongBrowserPlugin.Logging;
using Logger = SongBrowserPlugin.Logging.Logger;

namespace SongBrowserPlugin.DataAccess
{
    [Serializable]
    public enum SongSortMode
    {
        Default,
        Author,
        Original,
        Newest,        
        PlayCount,
        Difficulty,
        Random,
        PP,

        // Deprecated
        Favorites,
        Playlist,
        Search
    }

    [Serializable]
    public enum SongFilterMode
    {
        None,
        Favorites,
        Playlist,
        Search
    }

    [Serializable]
    public class SongBrowserSettings
    {
        public static readonly Encoding Utf8Encoding = Encoding.UTF8;
        public static readonly XmlSerializer SettingsSerializer = new XmlSerializer(typeof(SongBrowserSettings));
        public static readonly String DefaultConvertedFavoritesPlaylistName = "SongBrowserPluginFavorites.json";

        public SongSortMode sortMode = default(SongSortMode);
        public SongFilterMode filterMode = default(SongFilterMode);
       
        private HashSet<String> _favorites = default(HashSet<String>);

        public List<String> searchTerms = default(List<String>);

        public String currentLevelId = default(String);
        public String currentDirectory = default(String);
        public String currentPlaylistFile = default(String);
        public String currentEditingPlaylistFile = default(String);
        public String currentLevelPackName = default(String);

        public bool randomInstantQueue = false;
        public bool deleteNumberedSongFolder = true;
        public int randomSongSeed;
        public bool invertSortResults = false;

        [XmlIgnore]
        [NonSerialized]
        public bool DisableSavingSettings = false;

        [XmlArray(@"favorites")]
        public HashSet<String> Favorites
        {
            get
            {
                return _favorites;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SongBrowserSettings()
        {
            searchTerms = new List<string>();
            _favorites = new HashSet<String>();
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

            // Load Downloader favorites but only once, we'll convert them once, empty the song_browser_setting.xml favorites and never load it again.
            String playlistPath = Path.Combine(Environment.CurrentDirectory, "Playlists", DefaultConvertedFavoritesPlaylistName);
            if (!File.Exists(playlistPath))
            {
                if (File.Exists(SongBrowserSettings.DownloaderFavoritesFilePath()))
                {
                    String[] downloaderFavorites = File.ReadAllLines(SongBrowserSettings.DownloaderFavoritesFilePath());
                    retVal.Favorites.UnionWith(downloaderFavorites);
                }

                Playlist p = new Playlist
                {
                    playlistTitle = "Song Browser Favorites",
                    playlistAuthor = "SongBrowserPlugin",
                    fileLoc = "",
                    image = Base64Sprites.PlaylistIconB64,
                    songs = new List<PlaylistSong>(),
                };
                p.CreateNew(playlistPath);
            }

            if (String.IsNullOrEmpty(retVal.currentEditingPlaylistFile))
            {
                retVal.currentEditingPlaylistFile = playlistPath;
            }

            
            return retVal;
        }

        /// <summary>
        /// Favorites used to exist as part of the song_browser_settings.xml
        /// This makes little sense now.  This is the upgrade path.
        /// Convert all existing favorites to the best of our effort into a playlist.
        /// </summary>
        /// <param name="levelIdToCustomLevel"></param>
        /// <param name="levelIdToSongVersion"></param>
        public void ConvertFavoritesToPlaylist(Dictionary<String, SongLoaderPlugin.OverrideClasses.CustomLevel> levelIdToCustomLevel,
                                               Dictionary<string, string> levelIdToSongVersion)
        {
            // Check if we have favorites to convert to the playlist
            if (this.Favorites.Count <= 0)
            {
                return;
            }

            // check if the playlist exists
            String playlistPath = Path.Combine(Environment.CurrentDirectory, "Playlists", DefaultConvertedFavoritesPlaylistName);
            bool playlistExists = false;
            if (File.Exists(playlistPath))
            {
                playlistExists = true;
            }

            // abort here if playlist already exits.
            if (playlistExists)
            {
                Logger.Info("Not converting song_browser_setting.xml favorites because {0} already exists...", playlistPath);
                return;
            }

            Logger.Info("Converting {0} Favorites in song_browser_settings.xml to {1}...", this.Favorites.Count, playlistPath);

            // written like this in case we ever want to support adding to this playlist
            Playlist p = null;
            if (playlistExists)
            {
                p = Playlist.LoadPlaylist(playlistPath);
            }
            else
            {
                p = new Playlist
                {
                    playlistTitle = "Song Browser Favorites",
                    playlistAuthor = "SongBrowserPlugin",
                    fileLoc = "",
                    image = Base64Sprites.PlaylistIconB64,
                    songs = new List<PlaylistSong>(),
                };
            }

            List<String> successfullyRemoved = new List<string>();
            this.Favorites.RemoveWhere(levelId =>
            {
                PlaylistSong playlistSong = new PlaylistSong
                {
                    levelId = levelId
                };

                if (levelIdToCustomLevel.ContainsKey(levelId) && levelIdToSongVersion.ContainsKey(levelId))
                {
                    playlistSong.songName = levelIdToCustomLevel[levelId].songName;
                    playlistSong.key = levelIdToSongVersion[levelId];
                }
                else
                {
                    // No easy way to look up original songs... They will still work but have wrong song name in playlist.  
                    playlistSong.songName = levelId;
                    playlistSong.key = "";
                }

                p.songs.Add(playlistSong);

                return true;
            });

            p.SavePlaylist(playlistPath);

            if (String.IsNullOrEmpty(this.currentEditingPlaylistFile))
            {
                this.currentEditingPlaylistFile = playlistPath;
            }

            this.Save();
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

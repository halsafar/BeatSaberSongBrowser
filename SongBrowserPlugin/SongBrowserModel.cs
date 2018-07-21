using SongBrowserPlugin.DataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SongBrowserPlugin
{
    public class SongBrowserModel
    {
        private Logger _log = new Logger("SongBrowserModel");
        
        private SongBrowserSettings _settings;

        private List<SongLoaderPlugin.OverrideClasses.CustomLevel> _sortedSongs;
        private List<SongLoaderPlugin.OverrideClasses.CustomLevel> _originalSongs;
        private Dictionary<String, SongLoaderPlugin.OverrideClasses.CustomLevel> _levelIdToCustomLevel;

        private SongSortMode _cachedSortMode = default(SongSortMode);
        private Dictionary<String, double> _cachedLastWriteTimes;
        private DateTime _cachedCustomSongDirLastWriteTIme = DateTime.MinValue;
        private int _customSongDirTotalCount = -1;

        public SongBrowserSettings Settings
        {
            get
            {
                return _settings;
            }
        }

        public List<SongLoaderPlugin.OverrideClasses.CustomLevel> SortedSongList
        {
            get
            {
                return _sortedSongs;
            }
        }

        public Dictionary<String, SongLoaderPlugin.OverrideClasses.CustomLevel> LevelIdToCustomSongInfos
        {
            get
            {
                return _levelIdToCustomLevel;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SongBrowserModel()
        {
            _cachedLastWriteTimes = new Dictionary<String, double>();
        }

        /// <summary>
        /// Init this model.
        /// </summary>
        /// <param name="songSelectionMasterView"></param>
        /// <param name="songListViewController"></param>
        public void Init()
        {
            _settings = SongBrowserSettings.Load();
            _log.Info("Settings loaded, sorting mode is: {0}", _settings.sortMode);
        }

        /// <summary>
        /// Get the song cache from the game.
        /// </summary>
        public void UpdateSongLists()
        {
            String customSongsPath = Path.Combine(Environment.CurrentDirectory, "CustomSongs");
            DateTime currentLastWriteTIme = File.GetLastWriteTimeUtc(customSongsPath);
            string[] directories = Directory.GetDirectories(customSongsPath);
            int directoryCount = directories.Length;
            int fileCount = Directory.GetFiles(customSongsPath, "*").Length;
            int currentTotalCount = directoryCount + fileCount;

            if (_cachedCustomSongDirLastWriteTIme == null || 
                DateTime.Compare(currentLastWriteTIme, _cachedCustomSongDirLastWriteTIme) != 0 ||
                currentTotalCount != this._customSongDirTotalCount)
            {
                _log.Debug("Custom Song directory has changed. Fetching new songs. Sorting song list.");

                this._customSongDirTotalCount = directoryCount + fileCount;

                // Get LastWriteTimes
                var Epoch = new DateTime(1970, 1, 1);

                //_log.Debug("Directories: " + directories);
                foreach (string dir in directories)
                {
                    // Flip slashes, match SongLoaderPlugin
                    string slashed_dir = dir.Replace("\\", "/");

                   //_log.Debug("Fetching LastWriteTime for {0}", slashed_dir);
                    _cachedLastWriteTimes[slashed_dir] = (File.GetLastWriteTimeUtc(dir) - Epoch).TotalMilliseconds;
                }

                // Update song Infos
                this.UpdateSongInfos();
                
                // Get new songs
                _cachedCustomSongDirLastWriteTIme = currentLastWriteTIme;
                _cachedSortMode = _settings.sortMode;
                
                this.ProcessSongList();
            }
            else if (_settings.sortMode != _cachedSortMode)
            {
                _log.Debug("Sort mode has changed.  Sorting song list.");
                _cachedSortMode = _settings.sortMode;
                this.ProcessSongList();
            }
            else
            {
                _log.Debug("Songs List and/or sort mode has not changed.");
            }
        }

        /// <summary>
        /// Get the song infos from SongLoaderPluging
        /// </summary>
        private void UpdateSongInfos()
        {
            _log.Trace("UpdateSongInfos()");
            _originalSongs = SongLoaderPlugin.SongLoader.CustomLevels;
            _sortedSongs = _originalSongs;
            _levelIdToCustomLevel = _originalSongs.ToDictionary(x => x.levelID, x => x);

            _log.Debug("Song Browser knows about {0} songs from SongLoader...", _sortedSongs.Count);
        }
        
        /// <summary>
        /// Sort the song list based on the settings.
        /// </summary>
        private void ProcessSongList()
        {
            _log.Trace("ProcessSongList()");
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Weights used for keeping the original songs in order
            // Invert the weights from the game so we can order by descending and make LINQ work with us...
            /*  Level4, Level2, Level9, Level5, Level10, Level6, Level7, Level1, Level3, Level8, */
            Dictionary<string, int> weights = new Dictionary<string, int>
            {
                ["Level4"] = 10,
                ["Level2"] = 9,
                ["Level9"] = 8,
                ["Level5"] = 7,
                ["Level10"] = 6,
                ["Level6"] = 5,
                ["Level7"] = 4,
                ["Level1"] = 3,
                ["Level3"] = 2,
                ["Level8"] = 1
            };

            switch (_settings.sortMode)
            {
                case SongSortMode.Favorites:
                    _log.Info("Sorting song list as favorites");
                    _sortedSongs = _originalSongs
                        .AsQueryable()
                        .OrderBy(x => _settings.favorites.Contains(x.levelID) == false)
                        .ThenBy(x => x.songName)
                        .ThenBy(x => x.songAuthorName)
                        .ToList();
                    break;
                case SongSortMode.Original:
                    _log.Info("Sorting song list as original");
                    _sortedSongs = _originalSongs
                        .AsQueryable()
                        .OrderByDescending(x => weights.ContainsKey(x.levelID) ? weights[x.levelID] : 0)
                        .ThenBy(x => x.songName)
                        .ToList();
                    break;
                case SongSortMode.Newest:
                    _log.Info("Sorting song list as newest.");
                    _sortedSongs = _originalSongs
                        .AsQueryable()
                        .OrderBy(x => weights.ContainsKey(x.levelID) ? weights[x.levelID] : 0)
                        .ThenByDescending(x => x.levelID.StartsWith("Level") ? weights[x.levelID] : _cachedLastWriteTimes[_levelIdToCustomLevel[x.levelID].customSongInfo.path])
                        .ToList();
                    break;
                case SongSortMode.Author:
                    _log.Info("Sorting song list by author");
                    _sortedSongs = _originalSongs
                        .AsQueryable()
                        .OrderBy(x => x.songAuthorName)
                        .ThenBy(x => x.songName)
                        .ToList();
                    break;
                case SongSortMode.Default:
                default:
                    _log.Info("Sorting song list as default (songName)");
                    _sortedSongs = _originalSongs
                        .AsQueryable()
                        .OrderBy(x => x.songName)
                        .ThenBy(x => x.songAuthorName)
                        .ToList();
                    break;
            }

            stopwatch.Stop();
            _log.Info("Sorting songs took {0}ms", stopwatch.ElapsedMilliseconds);
        }        
    }
}

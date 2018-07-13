using HMUI;
using SongBrowserPlugin.DataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SongBrowserPlugin
{
    public class SongBrowserModel
    {
        private Logger _log = new Logger("SongBrowserModel");

        private List<SongLoaderPlugin.CustomSongInfo> _customSongInfos;
        private Dictionary<String, SongLoaderPlugin.CustomSongInfo> _levelIdToCustomSongInfo;
        private SongBrowserSettings _settings;

        //private SongSelectionMasterViewController _songSelectionMasterView;
        //private SongListViewController _songListViewController;

        private IBeatSaberSongList _beatSaberSongAccessor;

        private List<LevelStaticData> _sortedSongs;
        private List<LevelStaticData> _originalSongs;    
        private SongSortMode _cachedSortMode = default(SongSortMode);

        private DateTime _cachedCustomSongDirLastWriteTIme = DateTime.MinValue;

        public SongBrowserSettings Settings
        {
            get
            {
                return _settings;
            }
        }

        public List<LevelStaticData> SortedSongList
        {
            get
            {
                return _sortedSongs;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SongBrowserModel()
        {

        }

        /// <summary>
        /// Init this model.
        /// </summary>
        /// <param name="songSelectionMasterView"></param>
        /// <param name="songListViewController"></param>
        public void Init(IBeatSaberSongList beatSaberSongAccessor /*SongSelectionMasterViewController songSelectionMasterView, SongListViewController songListViewController*/)
        {
            _beatSaberSongAccessor = beatSaberSongAccessor;
            _settings = SongBrowserSettings.Load();

            //_songSelectionMasterView = songSelectionMasterView;
            //_songListViewController = songListViewController;
        }

        /// <summary>
        /// Get the song cache from the game.
        /// </summary>
        public void UpdateSongLists(bool updateSongInfos)
        {
            String customSongsPath = Path.Combine(Environment.CurrentDirectory, "CustomSongs");
            DateTime currentLastWriteTIme = File.GetLastWriteTimeUtc(customSongsPath);
            if (_cachedCustomSongDirLastWriteTIme == null || DateTime.Compare(currentLastWriteTIme, _cachedCustomSongDirLastWriteTIme) != 0)
            {
                _log.Debug("Custom Song directory has changed. Fetching new songs. Sorting song list.");
                _cachedCustomSongDirLastWriteTIme = currentLastWriteTIme;
                _cachedSortMode = _settings.sortMode;
                _originalSongs = this._beatSaberSongAccessor.AcquireSongList();
                if (updateSongInfos)
                {
                    this.UpdateSongInfos();
                }
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
            _customSongInfos = ReflectionUtil.InvokeMethod<SongLoaderPlugin.SongLoader>(SongLoaderPlugin.SongLoader.Instance, "RetrieveAllSongs", null) as List<SongLoaderPlugin.CustomSongInfo>;
            _levelIdToCustomSongInfo = _customSongInfos.ToDictionary(x => x.GetIdentifier(), x => x);
        }
        
        /// <summary>
        /// Sort the song list based on the settings.
        /// </summary>
        private void ProcessSongList()
        {
            _log.Debug("ProcessSongList()");
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

            var Epoch = new DateTime(1970, 1, 1);

            switch (_settings.sortMode)
            {
                case SongSortMode.Favorites:
                    _log.Debug("Sorting song list as favorites");
                    _sortedSongs = _originalSongs
                        .AsQueryable()
                        .OrderBy(x => _settings.favorites.Contains(x.levelId) == false)
                        .ThenBy(x => x.songName)
                        .ThenBy(x => x.authorName)
                        .ToList();
                    break;
                case SongSortMode.Original:
                    _log.Debug("Sorting song list as original");
                    _sortedSongs = _originalSongs
                        .AsQueryable()
                        .OrderByDescending(x => weights.ContainsKey(x.levelId) ? weights[x.levelId] : 0)
                        .ThenBy(x => x.songName)
                        .ToList();
                    break;
                case SongSortMode.Newest:
                    _log.Debug("Sorting song list as newest.");
                    _sortedSongs = _originalSongs
                        .AsQueryable()
                        .OrderBy(x => weights.ContainsKey(x.levelId) ? weights[x.levelId] : 0)
                        .ThenByDescending(x => x.levelId.StartsWith("Level") ? weights[x.levelId] : (File.GetLastWriteTimeUtc(_levelIdToCustomSongInfo[x.levelId].path) - Epoch).TotalMilliseconds)
                        .ToList();
                    break;
                case SongSortMode.Default:
                default:
                    _log.Debug("Sorting song list as default");
                    _sortedSongs = _originalSongs
                        .AsQueryable()
                        .OrderBy(x => x.authorName)
                        .ThenBy(x => x.songName)
                        .ToList();
                    break;
            }

            stopwatch.Stop();
            _log.Info("Sorting songs took {0}ms", stopwatch.ElapsedMilliseconds);

            this._beatSaberSongAccessor.OverwriteBeatSaberSongList(_sortedSongs);
        }        
    }
}

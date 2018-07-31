using SongBrowserPlugin.DataAccess;
using SongLoaderPlugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SongBrowserPlugin
{
    class DirectoryNode
    {
        public string Key { get; private set; }
        public Dictionary<String, DirectoryNode> Nodes;
        public List<StandardLevelSO> Levels;

        public DirectoryNode(String key)
        {
            Key = key;
            Nodes = new Dictionary<string, DirectoryNode>();
            Levels = new List<StandardLevelSO>();
        }
    }

    public class SongBrowserModel
    {
        private const String CUSTOM_SONGS_DIR = "CustomSongs";

        public static String LastSelectedLevelId { get; set; }

        private Logger _log = new Logger("SongBrowserModel");
        
        // song_browser_settings.xml
        private SongBrowserSettings _settings;

        // song list management
        private List<StandardLevelSO> _sortedSongs;
        private List<StandardLevelSO> _originalSongs;
        private Dictionary<String, SongLoaderPlugin.OverrideClasses.CustomLevel> _levelIdToCustomLevel;
        private SongLoaderPlugin.OverrideClasses.CustomLevelCollectionSO _gameplayModeCollection;    
        private Dictionary<String, double> _cachedLastWriteTimes;
        private Dictionary<string, int> _weights;
        private Dictionary<String, DirectoryNode> _directoryTree;
        private String _currentDirectory = CUSTOM_SONGS_DIR;       

        public bool InvertingResults { get; private set; }

        /// <summary>
        /// Get the settings the model is using.
        /// </summary>
        public SongBrowserSettings Settings
        {
            get
            {
                return _settings;
            }
        }

        /// <summary>
        /// Get the sorted song list for the current working directory.
        /// </summary>
        public List<StandardLevelSO> SortedSongList
        {
            get
            {
                return _sortedSongs;
            }
        }

        /// <summary>
        /// Map LevelID to Custom Level info.  
        /// </summary>
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

            // Weights used for keeping the original songs in order
            // Invert the weights from the game so we can order by descending and make LINQ work with us...
            /*  Level4, Level2, Level9, Level5, Level10, Level6, Level7, Level1, Level3, Level8, Level11 */
            _weights = new Dictionary<string, int>
            {
                ["Level4"] = 11,
                ["Level2"] = 10,
                ["Level9"] = 9,
                ["Level5"] = 8,
                ["Level10"] = 7,
                ["Level6"] = 6,
                ["Level7"] = 5,
                ["Level1"] = 4,
                ["Level3"] = 3,
                ["Level8"] = 2,
                ["Level11"] = 1
            };
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
        /// Easy invert of toggling.
        /// </summary>
        public void ToggleInverting()
        {
            this.InvertingResults = !this.InvertingResults;
        }

        /// <summary>
        /// Get the song cache from the game.
        /// TODO: This might not even be necessary anymore.  Need to test interactions with BeatSaverDownloader.
        /// </summary>
        public void UpdateSongLists(GameplayMode gameplayMode)
        {
            String customSongsPath = Path.Combine(Environment.CurrentDirectory, CUSTOM_SONGS_DIR);
            String cachedSongsPath = Path.Combine(customSongsPath, ".cache");
            DateTime currentLastWriteTIme = File.GetLastWriteTimeUtc(customSongsPath);
            IEnumerable<string> directories = Directory.EnumerateDirectories(customSongsPath, "*.*", SearchOption.AllDirectories);

            // Get LastWriteTimes
            var Epoch = new DateTime(1970, 1, 1);
            foreach (string dir in directories)
            {
                // Flip slashes, match SongLoaderPlugin
                string slashed_dir = dir.Replace("\\", "/");

                //_log.Debug("Fetching LastWriteTime for {0}", slashed_dir);
                _cachedLastWriteTimes[slashed_dir] = (File.GetLastWriteTimeUtc(dir) - Epoch).TotalMilliseconds;
            }

            // Update song Infos, directory tree, and sort
            this.UpdateSongInfos(gameplayMode);
            this.UpdateDirectoryTree(customSongsPath);
            this.ProcessSongList(gameplayMode);                       
        }

        /// <summary>
        /// Get the song infos from SongLoaderPluging
        /// </summary>
        private void UpdateSongInfos(GameplayMode gameplayMode)
        {
            _log.Trace("UpdateSongInfos for Gameplay Mode {0}", gameplayMode);

            // Get the level collection from song loader
            SongLoaderPlugin.OverrideClasses.CustomLevelCollectionsForGameplayModes collections = SongLoaderPlugin.SongLoader.Instance.GetPrivateField<SongLoaderPlugin.OverrideClasses.CustomLevelCollectionsForGameplayModes>("_customLevelCollectionsForGameplayModes");
            _gameplayModeCollection = collections.GetCollection(gameplayMode) as SongLoaderPlugin.OverrideClasses.CustomLevelCollectionSO;
            _originalSongs = collections.GetLevels(gameplayMode).ToList();
            _sortedSongs = _originalSongs;
            _levelIdToCustomLevel = new Dictionary<string, SongLoaderPlugin.OverrideClasses.CustomLevel>();
            foreach (var level in SongLoader.CustomLevels)
            {
                if (!_levelIdToCustomLevel.Keys.Contains(level.levelID))
                    _levelIdToCustomLevel.Add(level.levelID, level);
            }

            _log.Debug("Song Browser knows about {0} songs from SongLoader...", _sortedSongs.Count);
        }

        /// <summary>
        /// Make the directory tree.
        /// </summary>
        /// <param name="customSongsPath"></param>
        private void UpdateDirectoryTree(String customSongsPath)
        {
            // Determine folder mapping
            Uri customSongDirUri = new Uri(customSongsPath);
            _directoryTree = new Dictionary<string, DirectoryNode>();
            _directoryTree[CUSTOM_SONGS_DIR] = new DirectoryNode(CUSTOM_SONGS_DIR);
           
            foreach (StandardLevelSO level in _originalSongs)
            {
                if (level.levelID.Length < 32) continue;
                AddItemToDirectoryTree(customSongDirUri, level);
            }
        }

        /// <summary>
        /// Add a song to directory tree.  Determine its place in the tree by walking the split directory path.
        /// </summary>
        /// <param name="customSongDirUri"></param>
        /// <param name="level"></param>
        private void AddItemToDirectoryTree(Uri customSongDirUri, StandardLevelSO level)
        {
            DirectoryNode currentNode = _directoryTree[CUSTOM_SONGS_DIR];
            CustomSongInfo songInfo = _levelIdToCustomLevel[level.levelID].customSongInfo;
            Uri customSongUri = new Uri(songInfo.path);
            Uri pathDiff = customSongDirUri.MakeRelativeUri(customSongUri);
            string relPath = Uri.UnescapeDataString(pathDiff.OriginalString);
            string[] paths = relPath.Split('/');

            string curPath = paths[0];
            for (int i = 1; i < paths.Length; i++)
            {
                string path = paths[i];

                if (path == Path.GetFileName(songInfo.path))
                {
                    currentNode.Levels.Add(level);
                    break;
                }
                else if (currentNode.Nodes.ContainsKey(path))
                {                    
                    currentNode = currentNode.Nodes[path];
                }
                else
                {
                    currentNode.Nodes[path] = new DirectoryNode(path);
                }
            }
        }
        
        /// <summary>
        /// Sort the song list based on the settings.
        /// </summary>
        private void ProcessSongList(GameplayMode gameplayMode)
        {
            _log.Trace("ProcessSongList()");

            // This has come in handy many times for debugging issues with Newest.
            /*foreach (StandardLevelSO level in _originalSongs)
            {
                if (_levelIdToCustomLevel.ContainsKey(level.levelID))
                {
                    _log.Debug("HAS KEY {0}: {1}", _levelIdToCustomLevel[level.levelID].customSongInfo.path, level.levelID);
                }
                else
                {
                    _log.Debug("Missing KEY: {0}", level.levelID);
                }
            }*/
            
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<StandardLevelSO> songList = _directoryTree[_currentDirectory].Levels;

            switch (_settings.sortMode)
            {
                case SongSortMode.Favorites:
                    SortFavorites(songList);
                    break;
                case SongSortMode.Original:
                    SortOriginal(songList);
                    break;
                case SongSortMode.Newest:
                    SortNewest(songList);
                    break;
                case SongSortMode.Author:
                    SortAuthor(songList);
                    break;
                case SongSortMode.PlayCount:
                    SortPlayCount(songList, gameplayMode);
                    break;
                case SongSortMode.Random:
                    SortRandom(songList);
                    break;
                case SongSortMode.Search:
                    SortSearch(songList);
                    break;
                case SongSortMode.Default:
                default:
                    SortSongName(songList);
                    break;
            }

            if (this.InvertingResults && _settings.sortMode != SongSortMode.Random)
            {
                _sortedSongs.Reverse();
            }

            stopwatch.Stop();
            _log.Info("Sorting songs took {0}ms", stopwatch.ElapsedMilliseconds);
        }    
        
        private void SortFavorites(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list as favorites");
            _sortedSongs = levels
                .AsQueryable()
                .OrderBy(x => _settings.favorites.Contains(x.levelID) == false)
                .ThenBy(x => x.songName)
                .ThenBy(x => x.songAuthorName)
                .ToList();
        }

        private void SortOriginal(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list as original");
            _sortedSongs = levels
                .AsQueryable()
                .OrderByDescending(x => _weights.ContainsKey(x.levelID) ? _weights[x.levelID] : 0)
                .ThenBy(x => x.songName)
                .ToList();
        }

        private void SortNewest(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list as newest.");
            _sortedSongs = levels
                .AsQueryable()
                .OrderBy(x => _weights.ContainsKey(x.levelID) ? _weights[x.levelID] : 0)
                .ThenByDescending(x => x.levelID.StartsWith("Level") ? _weights[x.levelID] : _cachedLastWriteTimes[_levelIdToCustomLevel[x.levelID].customSongInfo.path])
                .ToList();
        }

        private void SortAuthor(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list by author");
            _sortedSongs = levels
                .AsQueryable()
                .OrderBy(x => x.songAuthorName)
                .ThenBy(x => x.songName)
                .ToList();
        }

        private void SortPlayCount(List<StandardLevelSO> levels, GameplayMode gameplayMode)
        {
            _log.Info("Sorting song list by playcount");
            // Build a map of levelId to sum of all playcounts and sort.
            PlayerDynamicData playerData = GameDataModel.instance.gameDynamicData.GetCurrentPlayerDynamicData();
            IEnumerable<LevelDifficulty> difficultyIterator = Enum.GetValues(typeof(LevelDifficulty)).Cast<LevelDifficulty>();

            Dictionary<string, int>  levelIdToPlayCount = new Dictionary<string, int>();
            foreach (var level in _originalSongs)
            {
                if (!levelIdToPlayCount.ContainsKey(level.levelID))
                {
                    int playCountSum = difficultyIterator.Sum(difficulty => playerData.GetPlayerLevelStatsData(level.levelID, difficulty, gameplayMode).playCount);
                    levelIdToPlayCount.Add(level.levelID, playCountSum);
                }
            }

            _sortedSongs = levels
                .AsQueryable()
                .OrderByDescending(x => levelIdToPlayCount[x.levelID])
                .ThenBy(x => x.songName)
                .ToList();
        }

        private void SortRandom(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list by random");

            System.Random rnd = new System.Random(Guid.NewGuid().GetHashCode());

            _sortedSongs = levels
                .AsQueryable()
                .OrderBy(x => rnd.Next())
                .ToList();
        }

        private void SortSearch(List<StandardLevelSO> levels)
        {
            // Make sure we can actually search.
            if (this._settings.searchTerms.Count <= 0)
            {
                _log.Error("Tried to search for a song with no valid search terms...");
                SortSongName(levels);
                return;
            }
            string searchTerm = this._settings.searchTerms[0];
            if (String.IsNullOrEmpty(searchTerm))
            {
                _log.Error("Empty search term entered.");
                SortSongName(levels);
                return;
            }

            _log.Info("Sorting song list by search term: {0}", searchTerm);
            //_originalSongs.ForEach(x => _log.Debug($"{x.songName} {x.songSubName} {x.songAuthorName}".ToLower().Contains(searchTerm.ToLower()).ToString()));

            _sortedSongs = levels
                .AsQueryable()
                .Where(x => $"{x.songName} {x.songSubName} {x.songAuthorName}".ToLower().Contains(searchTerm.ToLower()))
                .ToList();
            //_sortedSongs.ForEach(x => _log.Debug(x.levelID));
        }

        private void SortSongName(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list as default (songName)");
            _sortedSongs = levels
                .AsQueryable()
                .OrderBy(x => x.songName)
                .ThenBy(x => x.songAuthorName)
                .ToList();
        }
    }
}

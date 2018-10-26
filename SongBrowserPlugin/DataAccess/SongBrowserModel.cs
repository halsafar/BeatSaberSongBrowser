﻿using SongBrowserPlugin.DataAccess;
using SongBrowserPlugin.DataAccess.FileSystem;
using SongBrowserPlugin.UI;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SongBrowserPlugin
{
    public class SongBrowserModel
    {
        private const String CUSTOM_SONGS_DIR = "CustomSongs";

        private readonly DateTime EPOCH = new DateTime(1970, 1, 1);

        private readonly Regex SONG_PATH_VERSION_REGEX = new Regex(@".*/(?<version>[0-9]*-[0-9]*)/");

        private Logger _log = new Logger("SongBrowserModel");

        // song_browser_settings.xml
        private SongBrowserSettings _settings;

        // song list management
        private double _customSongDirLastWriteTime = 0;
        private List<StandardLevelSO> _filteredSongs;
        private List<StandardLevelSO> _sortedSongs;
        private List<StandardLevelSO> _originalSongs;
        private Dictionary<String, SongLoaderPlugin.OverrideClasses.CustomLevel> _levelIdToCustomLevel;
        private Dictionary<String, double> _cachedLastWriteTimes;
        private Dictionary<string, int> _weights;
        private Dictionary<LevelDifficulty, int> _difficultyWeights;
        private Dictionary<string, ScoreSaberData> _levelIdToScoreSaberData = null;
        private Dictionary<string, int> _levelIdToPlayCount;
        private Dictionary<string, string> _levelIdToSongVersion;
        private Dictionary<String, DirectoryNode> _directoryTree;
        private Stack<DirectoryNode> _directoryStack = new Stack<DirectoryNode>();

        private GameplayMode _currentGamePlayMode;

        public static Action<List<CustomLevel>> didFinishProcessingSongs;

        /// <summary>
        /// Toggle whether inverting the results.
        /// </summary>
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
        /// Map LevelID to score saber data.
        /// </summary>
        public Dictionary<string, ScoreSaberData> LevelIdToScoreSaberData
        {
            get
            {
                return _levelIdToScoreSaberData;
            }
        }

        /// <summary>
        /// How deep is the directory stack.
        /// </summary>
        public int DirStackSize
        {
            get
            {
                return _directoryStack.Count;
            }
        }

        /// <summary>
        /// Get the last selected (stored in settings) level id.
        /// </summary>
        public String LastSelectedLevelId
        {
            get
            {
                return _settings.currentLevelId;
            }

            set
            {
                _settings.currentLevelId = value;
                _settings.Save();
            }
        }

        /// <summary>
        /// Get the last known directory the user visited.
        /// </summary>
        public String CurrentDirectory
        {
            get
            {
                return _settings.currentDirectory;
            }

            set
            {
                _settings.currentDirectory = value;
                _settings.Save();
            }
        }

        private Playlist _currentPlaylist;

        /// <summary>
        /// Manage the current playlist if one exists.
        /// </summary>
        public Playlist CurrentPlaylist
        {
            get
            {
                if (_currentPlaylist == null)
                {
                    _currentPlaylist = PlaylistsReader.ParsePlaylist(this._settings.currentPlaylistFile);
                }

                return _currentPlaylist;
            }

            set
            {
                _settings.currentPlaylistFile = value.playlistPath;
                _currentPlaylist = value;
            }
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        public SongBrowserModel()
        {
            _levelIdToCustomLevel = new Dictionary<string, SongLoaderPlugin.OverrideClasses.CustomLevel>();
            _cachedLastWriteTimes = new Dictionary<String, double>();
            _levelIdToScoreSaberData = new Dictionary<string, ScoreSaberData>();
            _levelIdToPlayCount = new Dictionary<string, int>();
            _levelIdToSongVersion = new Dictionary<string, string>();

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
                ["Level11"] = 1,

                ["Level4OneSaber"] = 12,
                ["Level1OneSaber"] = 11,
                ["Level2OneSaber"] = 10,
                ["Level9OneSaber"] = 9,
                ["Level7OneSaber"] = 8,
            };

            _difficultyWeights = new Dictionary<LevelDifficulty, int>
            {
                [LevelDifficulty.Easy] = int.MaxValue - 4,
                [LevelDifficulty.Normal] = int.MaxValue - 3,
                [LevelDifficulty.Hard] = int.MaxValue - 2,
                [LevelDifficulty.Expert] = int.MaxValue - 1,
                [LevelDifficulty.ExpertPlus] = int.MaxValue,
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
            /*if (SongLoader.CustomLevels.Count > 0)
            {
                SongBrowserApplication.MainProgressBar.ShowMessage("Processing songs...");
            }*/

            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Get the level collection from song loader
            LevelCollectionsForGameplayModes levelCollections = Resources.FindObjectsOfTypeAll<LevelCollectionsForGameplayModes>().FirstOrDefault();
            List<LevelCollectionsForGameplayModes.LevelCollectionForGameplayMode> levelCollectionsForGameModes = ReflectionUtil.GetPrivateField<LevelCollectionsForGameplayModes.LevelCollectionForGameplayMode[]>(levelCollections, "_collections").ToList();

            // Stash everything we need
            _originalSongs = levelCollections.GetLevels(gameplayMode).ToList();
            _sortedSongs = _originalSongs;
            _currentGamePlayMode = gameplayMode;

            // Calculate some information about the custom song dir
            String customSongsPath = Path.Combine(Environment.CurrentDirectory, CUSTOM_SONGS_DIR);
            double currentCustomSongDirLastWriteTIme = (File.GetLastWriteTimeUtc(customSongsPath) - EPOCH).TotalMilliseconds;
            bool customSongDirChanged = false;
            if (_customSongDirLastWriteTime != currentCustomSongDirLastWriteTIme)
            {
                customSongDirChanged = true;
                _customSongDirLastWriteTime = currentCustomSongDirLastWriteTIme;
            }

            // This operation scales well
            IEnumerable<string> directories = Directory.EnumerateDirectories(customSongsPath, "*.*", SearchOption.AllDirectories);

            // Get LastWriteTimes   
            Stopwatch lastWriteTimer = new Stopwatch();
            lastWriteTimer.Start();
            foreach (var level in SongLoader.CustomLevels)
            {
                // If we already know this levelID, don't both updating it.
                // SongLoader should filter duplicates but in case of failure we don't want to crash

                if (!_cachedLastWriteTimes.ContainsKey(level.levelID) || customSongDirChanged)
                {
                    _cachedLastWriteTimes[level.levelID] = (File.GetLastWriteTimeUtc(level.customSongInfo.path) - EPOCH).TotalMilliseconds;
                }

                if (!_levelIdToCustomLevel.ContainsKey(level.levelID))
                {
                    _levelIdToCustomLevel.Add(level.levelID, level);
                }

                if (!_levelIdToSongVersion.ContainsKey(level.levelID))
                {
                    Match m = SONG_PATH_VERSION_REGEX.Match(level.customSongInfo.path);
                    if (m.Success)
                    {
                        String version = m.Groups["version"].Value;
                        _levelIdToSongVersion.Add(level.levelID, version);
                    }
                }
            }

            lastWriteTimer.Stop();
            _log.Info("Determining song download time and determining mappings took {0}ms", lastWriteTimer.ElapsedMilliseconds);

            // Update song Infos, directory tree, and sort
            this.UpdateScoreSaberDataMapping();
            this.UpdatePlayCounts(_currentGamePlayMode);
            this.UpdateDirectoryTree(customSongsPath);
            this.ProcessSongList();

            if (SongLoader.CustomLevels.Count > 0)
            {
                didFinishProcessingSongs?.Invoke(SongLoader.CustomLevels);
            }

            timer.Stop();
            _log.Info("Updating songs infos took {0}ms", timer.ElapsedMilliseconds);
            _log.Debug("Song Browser knows about {0} songs from SongLoader...", _originalSongs.Count);
        }

        /// <summary>
        /// Update the gameplay play counts.
        /// </summary>
        /// <param name="gameplayMode"></param>
        private void UpdatePlayCounts(GameplayMode gameplayMode)
        {
            // Build a map of levelId to sum of all playcounts and sort.
            PlayerDynamicData playerData = GameDataModel.instance.gameDynamicData.GetCurrentPlayerDynamicData();
            IEnumerable<LevelDifficulty> difficultyIterator = Enum.GetValues(typeof(LevelDifficulty)).Cast<LevelDifficulty>();

            foreach (var level in _originalSongs)
            {
                if (!_levelIdToPlayCount.ContainsKey(level.levelID))
                {
                    // Skip folders
                    if (level.levelID.StartsWith("Folder_"))
                    {
                        _levelIdToPlayCount.Add(level.levelID, 0);
                    }
                    else
                    {
                        int playCountSum = 0;
                        foreach (LevelDifficulty difficulty in difficultyIterator)
                        {
                            PlayerLevelStatsData stats = playerData.GetPlayerLevelStatsData(level.levelID, difficulty, gameplayMode);
                            playCountSum += stats.playCount;
                        }
                        _levelIdToPlayCount.Add(level.levelID, playCountSum);
                    }
                }
            }
        }

        /// <summary>
        /// Parse the current pp data file.
        /// Public so controllers can decide when to update it.
        /// </summary>
        public void UpdateScoreSaberDataMapping()
        {
            _log.Trace("UpdateScoreSaberDataMapping()");

            ScoreSaberDataFile scoreSaberDataFile = ScoreSaberDatabaseDownloader.ScoreSaberDataFile;

            // bail
            if (scoreSaberDataFile == null)
            {
                _log.Warning("Cannot fetch song difficulty data tsv file from DuoVR");
                return;
            }

            foreach (var level in SongLoader.CustomLevels)
            {
                // Skip
                if (_levelIdToScoreSaberData.ContainsKey(level.levelID))
                {
                    continue;
                }

                ScoreSaberData scoreSaberData = null;
                
                // try to version match first
                if (_levelIdToSongVersion.ContainsKey(level.levelID))
                {
                    String version = _levelIdToSongVersion[level.levelID];
                    if (scoreSaberDataFile.SongVersionToScoreSaberData.ContainsKey(version))
                    {
                        scoreSaberData = scoreSaberDataFile.SongVersionToScoreSaberData[version];
                    }
                }

                // fallback to name matching 
                if (scoreSaberData == null)
                {
                    if (scoreSaberDataFile.SongNameToScoreSaberData.ContainsKey(level.songName))
                    {
                        scoreSaberData = scoreSaberDataFile.SongNameToScoreSaberData[level.songName];
                    }
                }

                if (scoreSaberData != null)
                {
                    //_log.Debug("{0} = {1}pp", level.songName, pp);
                    _levelIdToScoreSaberData.Add(level.levelID, scoreSaberData);
                }
            }            
        }

        /// <summary>
        /// Make the directory tree.
        /// </summary>
        /// <param name="customSongsPath"></param>
        private void UpdateDirectoryTree(String customSongsPath)
        {
            // Determine folder mapping
            Uri customSongDirUri = new Uri(customSongsPath);

            _directoryTree = new Dictionary<string, DirectoryNode>
            {
                [CUSTOM_SONGS_DIR] = new DirectoryNode(CUSTOM_SONGS_DIR)
            };

            if (_settings.folderSupportEnabled)
            {
                foreach (StandardLevelSO level in _originalSongs)
                {
                    AddItemToDirectoryTree(customSongDirUri, level);
                }
            }
            else
            {
                _directoryTree[CUSTOM_SONGS_DIR].Levels = _originalSongs;
            }
                    
            // Determine starting location
            DirectoryNode currentNode = _directoryTree[CUSTOM_SONGS_DIR];
            _directoryStack.Push(currentNode);

            // Try to navigate directory path
            if (!String.IsNullOrEmpty(this.CurrentDirectory))
            {
                String[] paths = this.CurrentDirectory.Split('/');
                for (int i = 1; i < paths.Length; i++)
                {
                    if (currentNode.Nodes.ContainsKey(paths[i]))
                    {
                        currentNode = currentNode.Nodes[paths[i]];
                        _directoryStack.Push(currentNode);
                    }
                }
            }

            //PrintDirectory(_directoryTree[CUSTOM_SONGS_DIR], 1);
        }

        /// <summary>
        /// Add a song to directory tree.  Determine its place in the tree by walking the split directory path.
        /// </summary>
        /// <param name="customSongDirUri"></param>
        /// <param name="level"></param>
        private void AddItemToDirectoryTree(Uri customSongDirUri, StandardLevelSO level)
        {
            //_log.Debug("Processing item into directory tree: {0}", level.levelID);
            DirectoryNode currentNode = _directoryTree[CUSTOM_SONGS_DIR];
            
            // Just add original songs to root and bail
            if (level.levelID.Length < 32)
            {
                currentNode.Levels.Add(level);
                return;
            }

            CustomSongInfo songInfo = _levelIdToCustomLevel[level.levelID].customSongInfo;            
            Uri customSongUri = new Uri(songInfo.path);
            Uri pathDiff = customSongDirUri.MakeRelativeUri(customSongUri);
            string relPath = Uri.UnescapeDataString(pathDiff.OriginalString);
            string[] paths = relPath.Split('/');
            Sprite folderIcon = Base64Sprites.Base64ToSprite(Base64Sprites.FolderIcon);

            // Prevent cache directory from building into the tree, will add all its leafs to root.
            bool forceIntoRoot = false;
            //_log.Debug("Processing path: {0}", songInfo.path);
            if (paths.Length > 2)
            {
                forceIntoRoot = paths[1].Contains(".cache");
                Regex r = new Regex(@"^\d{1,}-\d{1,}");
                if (r.Match(paths[1]).Success)
                {
                    forceIntoRoot = true;
                }
            }

            for (int i = 1; i < paths.Length; i++)
            {
                string path = paths[i];                

                if (path == Path.GetFileName(songInfo.path))
                {
                    //_log.Debug("\tLevel Found Adding {0}->{1}", currentNode.Key, level.levelID);
                    currentNode.Levels.Add(level);
                    break;
                }
                else if (currentNode.Nodes.ContainsKey(path))
                {                    
                    currentNode = currentNode.Nodes[path];
                }
                else if (!forceIntoRoot)
                {
                    currentNode.Nodes[path] = new DirectoryNode(path);
                    FolderLevel folderLevel = new FolderLevel();
                    folderLevel.Init(relPath, path, folderIcon);

                    //_log.Debug("\tAdding folder level {0}->{1}", currentNode.Key, path);
                    currentNode.Levels.Add(folderLevel);

                    _cachedLastWriteTimes[folderLevel.levelID] = (File.GetLastWriteTimeUtc(relPath) - EPOCH).TotalMilliseconds;

                    currentNode = currentNode.Nodes[path];
                }
            }
        }

        /// <summary>
        /// Push a dir onto the stack.
        /// </summary>
        public void PushDirectory(IStandardLevel level)
        {
            DirectoryNode currentNode = _directoryStack.Peek();
            _log.Debug("Pushing directory {0}", level.songName);

            if (!currentNode.Nodes.ContainsKey(level.songName))
            {
                _log.Debug("Trying to push a directory that doesn't exist at this level.");
                return;
            }

            _directoryStack.Push(currentNode.Nodes[level.songName]);

            this.CurrentDirectory = level.levelID;
                
            ProcessSongList();            
        }

        /// <summary>
        /// Pop a dir off the stack.
        /// </summary>
        public void PopDirectory()
        {
            if (_directoryStack.Count > 1)
            {
                _directoryStack.Pop();
                String currentDir = "";
                foreach (DirectoryNode node in _directoryStack)
                {
                    currentDir = node.Key + "/" + currentDir;
                }
                this.CurrentDirectory = "Folder_" + currentDir;
                ProcessSongList();
            }      
        }

        /// <summary>
        /// Print the directory structure parsed.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="depth"></param>
        private void PrintDirectory(DirectoryNode node, int depth)
        {
            Console.WriteLine("Dir: {0}".PadLeft(depth*4, ' '), node.Key);
            node.Levels.ForEach(x => Console.WriteLine("{0}".PadLeft((depth + 1)*4, ' '), x.levelID));
            foreach (KeyValuePair<string, DirectoryNode> childNode in node.Nodes)
            {
                PrintDirectory(childNode.Value, depth + 1);
            }            
        }
        
        /// <summary>
        /// Sort the song list based on the settings.
        /// </summary>
        public void ProcessSongList()
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

            if (_directoryStack.Count <= 0)
            {
                _log.Debug("Cannot process songs yet, songs infos have not been processed...");
                return;
            }

            // Playlist filter will load the original songs.
            if (this._settings.filterMode == SongFilterMode.Playlist && this.CurrentPlaylist != null)
            {
                _originalSongs = null;
            }
            else
            {
                _log.Debug("Showing songs for directory: {0}", _directoryStack.Peek().Key);
                _originalSongs = _directoryStack.Peek().Levels;
            }

            // filter
            _log.Debug("Starting filtering songs...");
            Stopwatch stopwatch = Stopwatch.StartNew();

            switch (_settings.filterMode)
            {
                case SongFilterMode.Favorites:
                    FilterFavorites(_originalSongs);
                    break;
                case SongFilterMode.Search:
                    FilterSearch(_originalSongs);
                    break;
                case SongFilterMode.Playlist:
                    FilterPlaylist();
                    break;
                case SongFilterMode.None:
                default:
                    _log.Info("No song filter selected...");
                    _filteredSongs = _originalSongs;
                    break;
            }

            stopwatch.Stop();
            _log.Info("Filtering songs took {0}ms", stopwatch.ElapsedMilliseconds);

            // sort
            _log.Debug("Starting to sort songs...");
            stopwatch = Stopwatch.StartNew();

            switch (_settings.sortMode)
            {
                case SongSortMode.Original:
                    SortOriginal(_filteredSongs);
                    break;
                case SongSortMode.Newest:
                    SortNewest(_filteredSongs);
                    break;
                case SongSortMode.Author:
                    SortAuthor(_filteredSongs);
                    break;
                case SongSortMode.PlayCount:
                    SortPlayCount(_filteredSongs, _currentGamePlayMode);
                    break;
                case SongSortMode.PP:
                    SortPerformancePoints(_filteredSongs);
                    break;
                case SongSortMode.Difficulty:
                    SortDifficulty(_filteredSongs);
                    break;
                case SongSortMode.Random:
                    SortRandom(_filteredSongs);
                    break;
                case SongSortMode.Default:
                default:
                    SortSongName(_filteredSongs);
                    break;
            }

            if (this.InvertingResults && _settings.sortMode != SongSortMode.Random)
            {
                _sortedSongs.Reverse();
            }

            stopwatch.Stop();
            _log.Info("Sorting songs took {0}ms", stopwatch.ElapsedMilliseconds);

            //_sortedSongs.ForEach(x => _log.Debug(x.levelID));
        }    
        
        private void FilterFavorites(List<StandardLevelSO> levels)
        {
            _log.Info("Filtering song list as favorites");
            _filteredSongs = levels
                .Where(x => _settings.Favorites.Contains(x.levelID))
                .ToList();
        }

        private void FilterSearch(List<StandardLevelSO> levels)
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

            _log.Info("Filtering song list by search term: {0}", searchTerm);
            //_originalSongs.ForEach(x => _log.Debug($"{x.songName} {x.songSubName} {x.songAuthorName}".ToLower().Contains(searchTerm.ToLower()).ToString()));

            _filteredSongs = levels
                .Where(x => $"{x.songName} {x.songSubName} {x.songAuthorName}".ToLower().Contains(searchTerm.ToLower()))
                .ToList();
        }

        private void FilterPlaylist()
        {
            // bail if no playlist, usually means the settings stored one the user then moved.
            if (this.CurrentPlaylist == null)
            {
                _log.Error("Trying to load a null playlist...");
                _filteredSongs = _originalSongs;
                return;
            }

            _log.Debug("Filtering songs for playlist: {0}", this.CurrentPlaylist.playlistTitle);            
            List<String> playlistKeysOrdered = this.CurrentPlaylist.songs.Select(x => x.Key).Distinct().ToList();
            Dictionary<String, int>playlistKeyToIndex = playlistKeysOrdered.Select((val, index) => new { Index = index, Value = val }).ToDictionary(i => i.Value, i => i.Index);
            LevelCollectionsForGameplayModes levelCollections = Resources.FindObjectsOfTypeAll<LevelCollectionsForGameplayModes>().FirstOrDefault();
            var levels = levelCollections.GetLevels(_currentGamePlayMode);

            var songList = levels.Where(x => !x.levelID.StartsWith("Level_") && _levelIdToSongVersion.ContainsKey(x.levelID) && playlistKeyToIndex.ContainsKey(_levelIdToSongVersion[x.levelID])).ToList();
            
            _originalSongs = songList;
            _filteredSongs = _originalSongs
                .OrderBy(x => playlistKeyToIndex[_levelIdToSongVersion[x.levelID]])
                .ToList();
            
            _log.Debug("Playlist filtered song count: {0}", _filteredSongs.Count);
        }

        private void SortOriginal(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list as original");
            _sortedSongs = levels;
        }

        private void SortNewest(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list as newest.");
            _sortedSongs = levels
                .OrderBy(x => _weights.ContainsKey(x.levelID) ? _weights[x.levelID] : 0)
                .ThenByDescending(x => x.levelID.StartsWith("Level") ? _weights[x.levelID] : _cachedLastWriteTimes[x.levelID])
                .ToList();
        }

        private void SortAuthor(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list by author");
            _sortedSongs = levels
                .OrderBy(x => x.songAuthorName)
                .ThenBy(x => x.songName)
                .ToList();
        }

        private void SortPlayCount(List<StandardLevelSO> levels, GameplayMode gameplayMode)
        {
            _log.Info("Sorting song list by playcount");
            _sortedSongs = levels
                .OrderByDescending(x => _levelIdToPlayCount[x.levelID])
                .ThenBy(x => x.songName)
                .ToList();
        }

        private void SortPerformancePoints(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list by performance points...");

            _sortedSongs = levels
                .OrderByDescending(x => _levelIdToScoreSaberData.ContainsKey(x.levelID) ? _levelIdToScoreSaberData[x.levelID].maxPp : 0)
                .ToList();
        }

        private void SortDifficulty(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list by difficulty...");

            IEnumerable<LevelDifficulty> difficultyIterator = Enum.GetValues(typeof(LevelDifficulty)).Cast<LevelDifficulty>();
            Dictionary<string, int>  levelIdToDifficultyValue = new Dictionary<string, int>();
            foreach (var level in levels)
            {
                if (!levelIdToDifficultyValue.ContainsKey(level.levelID))
                {
                    // Skip folders
                    if (level.levelID.StartsWith("Folder_"))
                    {
                        levelIdToDifficultyValue.Add(level.levelID, 0);
                    }
                    else
                    {
                        int difficultyValue = 0;
                        foreach (LevelDifficulty difficulty in difficultyIterator)
                        {
                            IStandardLevelDifficultyBeatmap beatmap = level.GetDifficultyLevel(difficulty);
                            if (beatmap != null)
                            {
                                difficultyValue += _difficultyWeights[difficulty];
                                break;
                            }
                        }
                        levelIdToDifficultyValue.Add(level.levelID, difficultyValue);
                    }
                }
            }

            _sortedSongs = levels
                .OrderBy(x => levelIdToDifficultyValue[x.levelID])
                .ThenBy(x => x.songName)
                .ToList();
        }

        private void SortRandom(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list by random...");

            System.Random rnd = new System.Random(Guid.NewGuid().GetHashCode());

            _sortedSongs = levels
                .OrderBy(x => rnd.Next())
                .ToList();
        }        

        private void SortSongName(List<StandardLevelSO> levels)
        {
            _log.Info("Sorting song list as default (songName)");
            _sortedSongs = levels
                .OrderBy(x => x.songName)
                .ThenBy(x => x.songAuthorName)
                .ToList();
        }
    }
}

using SongBrowserPlugin.DataAccess;
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
using Logger = SongBrowserPlugin.Logging.Logger;

namespace SongBrowserPlugin
{
    public class SongBrowserModel
    {
        private const String CUSTOM_SONGS_DIR = "CustomSongs";

        private readonly DateTime EPOCH = new DateTime(1970, 1, 1);

        // song_browser_settings.xml
        private SongBrowserSettings _settings;

        // song list management
        private double _customSongDirLastWriteTime = 0;
        private List<BeatmapLevelSO> _filteredSongs;
        private List<BeatmapLevelSO> _sortedSongs;
        private List<BeatmapLevelSO> _originalSongs;
        private Dictionary<String, SongLoaderPlugin.OverrideClasses.CustomLevel> _levelIdToCustomLevel;
        private Dictionary<String, double> _cachedLastWriteTimes;
        private Dictionary<string, int> _weights;
        private Dictionary<BeatmapDifficulty, int> _difficultyWeights;
        private Dictionary<string, ScoreSaberData> _levelIdToScoreSaberData = null;
        private Dictionary<string, int> _levelIdToPlayCount;
        private Dictionary<string, string> _levelIdToSongVersion;
        private Dictionary<string, BeatmapLevelSO> _keyToSong;
        private Dictionary<String, DirectoryNode> _directoryTree;
        private Stack<DirectoryNode> _directoryStack = new Stack<DirectoryNode>();

        public BeatmapCharacteristicSO CurrentBeatmapCharacteristicSO;

        public static Action<List<CustomLevel>> didFinishProcessingSongs;

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
        public List<BeatmapLevelSO> SortedSongList
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
                _settings.currentPlaylistFile = value.fileLoc;
                _currentPlaylist = value;
            }
        }

        public Playlist CurrentEditingPlaylist;

        public HashSet<String> CurrentEditingPlaylistLevelIds;


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
            _keyToSong = new Dictionary<string, BeatmapLevelSO>();

            // Weights used for keeping the original songs in order
            // Invert the weights from the game so we can order by descending and make LINQ work with us...
            /*  Level4, Level2, Level9, Level5, Level10, Level6, Level7, Level1, Level3, Level8, Level11 */
            _weights = new Dictionary<string, int>
            {
                ["OneHopeLevel"] = 12,
                ["100Bills"] = 11,
                ["Escape"] = 10,
                ["Legend"] = 9,
                ["BeatSaber"] = 8,
                ["AngelVoices"] = 7,
                ["CountryRounds"] = 6,
                ["BalearicPumping"] = 5,
                ["Breezer"] = 4,
                ["CommercialPumping"] = 3,
                ["TurnMeOn"] = 2,
                ["LvlInsane"] = 1,

                ["100BillsOneSaber"] = 12,
                ["EscapeOneSaber"] = 11,
                ["LegendOneSaber"] = 10,
                ["BeatSaberOneSaber"] = 9,
                ["CommercialPumpingOneSaber"] = 8,
                ["TurnMeOnOneSaber"] = 8,
            };

            _difficultyWeights = new Dictionary<BeatmapDifficulty, int>
            {
                [BeatmapDifficulty.Easy] = int.MaxValue - 4,
                [BeatmapDifficulty.Normal] = int.MaxValue - 3,
                [BeatmapDifficulty.Hard] = int.MaxValue - 2,
                [BeatmapDifficulty.Expert] = int.MaxValue - 1,
                [BeatmapDifficulty.ExpertPlus] = int.MaxValue,
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
            Logger.Info("Settings loaded, sorting mode is: {0}", _settings.sortMode);
        }

        /// <summary>
        /// Easy invert of toggling.
        /// </summary>
        public void ToggleInverting()
        {
            this.Settings.invertSortResults = !this.Settings.invertSortResults;
        }

        /// <summary>
        /// Get the song cache from the game.
        /// </summary>
        public void UpdateSongLists(BeatmapCharacteristicSO gameplayMode)
        {
            // give up
            if (gameplayMode == null)
            {
                Logger.Debug("Always null first time if user waits for SongLoader event, which they should...");
                return;
            }

            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Get the level collection from song loader
            BeatmapLevelCollectionSO levelCollections = Resources.FindObjectsOfTypeAll<BeatmapLevelCollectionSO>().FirstOrDefault();

            // Stash everything we need
            _originalSongs = levelCollections.GetLevelsWithBeatmapCharacteristic(gameplayMode).ToList();

            Logger.Debug("Got {0} songs from level collections...", _originalSongs.Count);
            //_originalSongs.ForEach(x => Logger.Debug("{0} by {1} = {2}", x.name, x.levelAuthorName, x.levelID));

            _sortedSongs = _originalSongs;
            CurrentBeatmapCharacteristicSO = gameplayMode;

            // Calculate some information about the custom song dir
            String customSongsPath = Path.Combine(Environment.CurrentDirectory, CUSTOM_SONGS_DIR);
            String revSlashCustomSongPath = customSongsPath.Replace('\\', '/');
            double currentCustomSongDirLastWriteTIme = (File.GetLastWriteTimeUtc(customSongsPath) - EPOCH).TotalMilliseconds;
            bool customSongDirChanged = false;
            if (_customSongDirLastWriteTime != currentCustomSongDirLastWriteTIme)
            {
                customSongDirChanged = true;
                _customSongDirLastWriteTime = currentCustomSongDirLastWriteTIme;
            }

            // This operation scales well
            if (!Directory.Exists(customSongsPath))
            {
                Logger.Error("CustomSong directory is missing...");
                return;
            }
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
                    DirectoryInfo info = new DirectoryInfo(level.customSongInfo.path);
                    string currentDirectoryName = info.Name;

                    String version = level.customSongInfo.path.Replace(revSlashCustomSongPath, "").Replace(currentDirectoryName, "").Replace("/", "");
                    if (!String.IsNullOrEmpty(version))
                    {
                        //Logger.Debug("MATCH");
                        _levelIdToSongVersion.Add(level.levelID, version);
                        _keyToSong.Add(version, level);
                    }
                }
            }

            lastWriteTimer.Stop();
            Logger.Info("Determining song download time and determining mappings took {0}ms", lastWriteTimer.ElapsedMilliseconds);

            // Update song Infos, directory tree, and sort
            this.UpdateScoreSaberDataMapping();
            this.UpdatePlayCounts();
            this.UpdateDirectoryTree(customSongsPath);

            // Check if we need to upgrade settings file favorites
            try
            {
                this.Settings.ConvertFavoritesToPlaylist(_levelIdToCustomLevel, _levelIdToSongVersion);
            }
            catch (Exception e)
            {
                Logger.Exception("FAILED TO CONVERT FAVORITES TO PLAYLIST!", e);
            }

            // load the current editing playlist or make one
            if (!String.IsNullOrEmpty(this.Settings.currentEditingPlaylistFile))
            {
                CurrentEditingPlaylist = PlaylistsReader.ParsePlaylist(this.Settings.currentEditingPlaylistFile);
            }

            if (CurrentEditingPlaylist == null)
            {
                CurrentEditingPlaylist = new Playlist
                {
                    playlistTitle = "Song Browser Favorites",
                    playlistAuthor = "SongBrowserPlugin",
                    fileLoc = this.Settings.currentEditingPlaylistFile,
                    image = Base64Sprites.PlaylistIconB64,
                    songs = new List<PlaylistSong>(),
                };
            }

            CurrentEditingPlaylistLevelIds = new HashSet<string>();
            foreach (PlaylistSong ps in CurrentEditingPlaylist.songs)
            {
                CurrentEditingPlaylistLevelIds.Add(ps.levelId);
            }            

            // Actually sort and filter
            this.ProcessSongList();

            // Signal complete
            if (SongLoader.CustomLevels.Count > 0)
            {
                didFinishProcessingSongs?.Invoke(SongLoader.CustomLevels);
            }

            timer.Stop();

            Logger.Info("Updating songs infos took {0}ms", timer.ElapsedMilliseconds);
            Logger.Debug("Song Browser knows about {0} songs from SongLoader...", _originalSongs.Count);
        }

        /// <summary>
        /// Update the gameplay play counts.
        /// </summary>
        /// <param name="gameplayMode"></param>
        private void UpdatePlayCounts()
        {
            // Build a map of levelId to sum of all playcounts and sort.
            PlayerDataModelSO playerData = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().FirstOrDefault();
            IEnumerable<BeatmapDifficulty> difficultyIterator = Enum.GetValues(typeof(BeatmapDifficulty)).Cast<BeatmapDifficulty>();

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
                        foreach (BeatmapDifficulty difficulty in difficultyIterator)
                        {
                            PlayerLevelStatsData stats = playerData.currentLocalPlayer.GetPlayerLevelStatsData(level.levelID, difficulty, this.CurrentBeatmapCharacteristicSO);
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
            Logger.Trace("UpdateScoreSaberDataMapping()");

            ScoreSaberDataFile scoreSaberDataFile = ScoreSaberDatabaseDownloader.ScoreSaberDataFile;

            // bail
            if (scoreSaberDataFile == null)
            {
                Logger.Warning("Cannot fetch song difficulty for score saber data...");
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

                if (scoreSaberData != null)
                {
                    //Logger.Debug("{0} = {1}pp", level.songName, pp);
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
                foreach (BeatmapLevelSO level in _originalSongs)
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
        private void AddItemToDirectoryTree(Uri customSongDirUri, BeatmapLevelSO level)
        {
            //Logger.Debug("Processing item into directory tree: {0}", level.levelID);
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
            Sprite folderIcon = Base64Sprites.FolderIcon;

            // Prevent cache directory from building into the tree, will add all its leafs to root.
            bool forceIntoRoot = false;
            //Logger.Debug("Processing path: {0}", songInfo.path);
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
                    //Logger.Debug("\tLevel Found Adding {0}->{1}", currentNode.Key, level.levelID);
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

                    //Logger.Debug("\tAdding folder level {0}->{1}", currentNode.Key, path);
                    currentNode.Levels.Add(folderLevel);

                    _cachedLastWriteTimes[folderLevel.levelID] = (File.GetLastWriteTimeUtc(relPath) - EPOCH).TotalMilliseconds;

                    currentNode = currentNode.Nodes[path];
                }
            }
        }

        /// <summary>
        /// Push a dir onto the stack.
        /// </summary>
        public void PushDirectory(IBeatmapLevel level)
        {
            DirectoryNode currentNode = _directoryStack.Peek();
            Logger.Debug("Pushing directory {0}", level.songName);

            if (!currentNode.Nodes.ContainsKey(level.songName))
            {
                Logger.Debug("Trying to push a directory that doesn't exist at this level.");
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
        /// Add Song to Editing Playlist
        /// </summary>
        /// <param name="songInfo"></param>
        public void AddSongToEditingPlaylist(IBeatmapLevel songInfo)
        {
            if (this.CurrentEditingPlaylist == null)
            {
                return;
            }

            this.CurrentEditingPlaylist.songs.Add(new PlaylistSong()
            {
                songName = songInfo.songName,
                levelId = songInfo.levelID,
                key = _levelIdToSongVersion.ContainsKey(songInfo.levelID) ? _levelIdToSongVersion[songInfo.levelID] : songInfo.levelID,                
            });

            this.CurrentEditingPlaylistLevelIds.Add(songInfo.levelID);

            PlaylistWriter.WritePlaylist(this.CurrentEditingPlaylist, this.CurrentEditingPlaylist.fileLoc);
        }

        /// <summary>
        /// Remove Song from editing playlist
        /// </summary>
        /// <param name="levelId"></param>
        public void RemoveSongFromEditingPlaylist(IBeatmapLevel songInfo)
        {
            if (this.CurrentEditingPlaylist == null)
            {
                return;
            }

            this.CurrentEditingPlaylist.songs.RemoveAll(x => x.levelId == songInfo.levelID);
            this.CurrentEditingPlaylistLevelIds.Remove(songInfo.levelID);

            PlaylistWriter.WritePlaylist(this.CurrentEditingPlaylist, this.CurrentEditingPlaylist.fileLoc);
        }
        
        /// <summary>
        /// Sort the song list based on the settings.
        /// </summary>
        public void ProcessSongList()
        {
            Logger.Trace("ProcessSongList()");

            // This has come in handy many times for debugging issues with Newest.
            /*foreach (BeatmapLevelSO level in _originalSongs)
            {
                if (_levelIdToCustomLevel.ContainsKey(level.levelID))
                {
                    Logger.Debug("HAS KEY {0}: {1}", _levelIdToCustomLevel[level.levelID].customSongInfo.path, level.levelID);
                }
                else
                {
                    Logger.Debug("Missing KEY: {0}", level.levelID);
                }
            }*/

            if (_directoryStack.Count <= 0)
            {
                Logger.Debug("Cannot process songs yet, songs infos have not been processed...");
                return;
            }

            // Playlist filter will load the original songs.
            if (this._settings.filterMode == SongFilterMode.Playlist && this.CurrentPlaylist != null)
            {
                _originalSongs = null;
            }
            else
            {
                Logger.Debug("Showing songs for directory: {0}", _directoryStack.Peek().Key);
                _originalSongs = _directoryStack.Peek().Levels;
            }

            // filter
            Logger.Debug("Starting filtering songs...");
            Stopwatch stopwatch = Stopwatch.StartNew();

            switch (_settings.filterMode)
            {
                case SongFilterMode.Favorites:
                    FilterFavorites();
                    break;
                case SongFilterMode.Search:
                    FilterSearch(_originalSongs);
                    break;
                case SongFilterMode.Playlist:
                    FilterPlaylist();
                    break;
                case SongFilterMode.None:
                default:
                    Logger.Info("No song filter selected...");
                    _filteredSongs = _originalSongs;
                    break;
            }

            stopwatch.Stop();
            Logger.Info("Filtering songs took {0}ms", stopwatch.ElapsedMilliseconds);

            // sort
            Logger.Debug("Starting to sort songs...");
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
                    SortPlayCount(_filteredSongs);
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

            if (this.Settings.invertSortResults && _settings.sortMode != SongSortMode.Random)
            {
                _sortedSongs.Reverse();
            }

            stopwatch.Stop();
            Logger.Info("Sorting songs took {0}ms", stopwatch.ElapsedMilliseconds);

            //_sortedSongs.ForEach(x => Logger.Debug(x.levelID));
        }    
        
        /// <summary>
        /// For now the editing playlist will be considered the favorites playlist.
        /// Users can edit the settings file themselves.
        /// </summary>
        private void FilterFavorites()
        {
            Logger.Info("Filtering song list as favorites playlist...");
            if (this.CurrentEditingPlaylist != null)
            {
                this.CurrentPlaylist = this.CurrentEditingPlaylist;
            }
            this.FilterPlaylist();
        }

        private void FilterSearch(List<BeatmapLevelSO> levels)
        {
            // Make sure we can actually search.
            if (this._settings.searchTerms.Count <= 0)
            {
                Logger.Error("Tried to search for a song with no valid search terms...");
                SortSongName(levels);
                return;
            }
            string searchTerm = this._settings.searchTerms[0];
            if (String.IsNullOrEmpty(searchTerm))
            {
                Logger.Error("Empty search term entered.");
                SortSongName(levels);
                return;
            }

            Logger.Info("Filtering song list by search term: {0}", searchTerm);
            _originalSongs.ForEach(x => Logger.Debug($"{x.songName} {x.songSubName} {x.songAuthorName}".ToLower().Contains(searchTerm.ToLower()).ToString()));

            _filteredSongs = levels
                .Where(x => $"{x.songName} {x.songSubName} {x.songAuthorName}".ToLower().Contains(searchTerm.ToLower()))
                .ToList();
        }

        private void FilterPlaylist()
        {
            // bail if no playlist, usually means the settings stored one the user then moved.
            if (this.CurrentPlaylist == null)
            {
                Logger.Error("Trying to load a null playlist...");
                _filteredSongs = _originalSongs;
                this.Settings.filterMode = SongFilterMode.None;
                return;
            }

            Logger.Debug("Filtering songs for playlist: {0}", this.CurrentPlaylist.playlistTitle);            
            BeatmapLevelCollectionSO levelCollections = Resources.FindObjectsOfTypeAll<BeatmapLevelCollectionSO>().FirstOrDefault();
            var levels = levelCollections.beatmapLevels;
            //var levels = levelCollections.GetLevelsWithBeatmapCharacteristic(CurrentBeatmapCharacteristicSO);

            //Dictionary<String, BeatmapLevelSO> levelDict = levels.Select((val, index) => new { LevelId = val.levelID, Level = val }).ToDictionary(i => i.LevelId, i => i.Level);
            Dictionary<String, BeatmapLevelSO> levelDict = new Dictionary<string, BeatmapLevelSO>();
            foreach (BeatmapLevelSO level in levels)
            {
                if (!levelDict.ContainsKey(level.levelID))
                {
                    levelDict.Add(level.levelID, level);
                }
            }

            List<BeatmapLevelSO> songList = new List<BeatmapLevelSO>();
            foreach (PlaylistSong ps in this.CurrentPlaylist.songs)
            {
                if (!String.IsNullOrEmpty(ps.levelId))
                {
                    if (levelDict.ContainsKey(ps.levelId))
                    {
                        songList.Add(levelDict[ps.levelId]);
                    }
                }
                else if (!ps.key.StartsWith("Level_") && _keyToSong.ContainsKey(ps.key))
                {
                    songList.Add(_keyToSong[ps.key]);
                }
            }

            _originalSongs = songList;
            _filteredSongs = _originalSongs;
            
            Logger.Debug("Playlist filtered song count: {0}", _filteredSongs.Count);
        }

        private void SortOriginal(List<BeatmapLevelSO> levels)
        {
            Logger.Info("Sorting song list as original");
            _sortedSongs = levels;
        }

        private void SortNewest(List<BeatmapLevelSO> levels)
        {
            Logger.Info("Sorting song list as newest.");
            _sortedSongs = levels
                .OrderBy(x => _weights.ContainsKey(x.levelID) ? _weights[x.levelID] : 0)
                .ThenByDescending(x => !_levelIdToCustomLevel.ContainsKey(x.levelID) ? (_weights.ContainsKey(x.levelID) ? _weights[x.levelID] : 0) : _cachedLastWriteTimes[x.levelID])
                .ToList();
        }

        private void SortAuthor(List<BeatmapLevelSO> levels)
        {
            Logger.Info("Sorting song list by author");
            _sortedSongs = levels
                .OrderBy(x => x.songAuthorName)
                .ThenBy(x => x.songName)
                .ToList();
        }

        private void SortPlayCount(List<BeatmapLevelSO> levels)
        {
            Logger.Info("Sorting song list by playcount");
            _sortedSongs = levels
                .OrderByDescending(x => _levelIdToPlayCount[x.levelID])
                .ThenBy(x => x.songName)
                .ToList();
        }

        private void SortPerformancePoints(List<BeatmapLevelSO> levels)
        {
            Logger.Info("Sorting song list by performance points...");

            _sortedSongs = levels
                .OrderByDescending(x => _levelIdToScoreSaberData.ContainsKey(x.levelID) ? _levelIdToScoreSaberData[x.levelID].maxPp : 0)
                .ToList();
        }

        private void SortDifficulty(List<BeatmapLevelSO> levels)
        {
            Logger.Info("Sorting song list by difficulty...");

            IEnumerable<BeatmapDifficulty> difficultyIterator = Enum.GetValues(typeof(BeatmapDifficulty)).Cast<BeatmapDifficulty>();
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

                        // Get the beatmap difficulties
                        var difficulties = level.difficultyBeatmapSets
                            .Where(x => x.beatmapCharacteristic == this.CurrentBeatmapCharacteristicSO)
                            .SelectMany(x => x.difficultyBeatmaps);

                        foreach (IDifficultyBeatmap difficultyBeatmap in difficulties)
                        {                            
                            difficultyValue += _difficultyWeights[difficultyBeatmap.difficulty];
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

        private void SortRandom(List<BeatmapLevelSO> levels)
        {
            Logger.Info("Sorting song list by random (seed={0})...", this.Settings.randomSongSeed);

            System.Random rnd = new System.Random(this.Settings.randomSongSeed);

            _sortedSongs = levels
                .OrderBy(x => rnd.Next())
                .ToList();
        }        

        private void SortSongName(List<BeatmapLevelSO> levels)
        {
            Logger.Info("Sorting song list as default (songName)");
            _sortedSongs = levels
                .OrderBy(x => x.songName)
                .ThenBy(x => x.songAuthorName)
                .ToList();
        }
    }
}

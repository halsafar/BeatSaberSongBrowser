using SongBrowserPlugin.DataAccess;
using SongBrowserPlugin.UI;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private List<BeatmapLevelSO> _sortedSongs;
        private Dictionary<String, SongLoaderPlugin.OverrideClasses.CustomLevel> _levelIdToCustomLevel;
        private Dictionary<String, double> _cachedLastWriteTimes;
        private Dictionary<string, int> _weights;
        private Dictionary<BeatmapDifficulty, int> _difficultyWeights;
        private Dictionary<string, ScoreSaberData> _levelIdToScoreSaberData = null;
        private Dictionary<string, int> _levelIdToPlayCount;
        private Dictionary<string, string> _levelIdToSongVersion;
        private Dictionary<string, BeatmapLevelSO> _keyToSong;

        public BeatmapCharacteristicSO CurrentBeatmapCharacteristicSO;

        public static Action<List<CustomLevel>> didFinishProcessingSongs;

        private IBeatmapLevelPack _currentLevelPack;
        private Dictionary<string, List<BeatmapLevelSO>>  _levelPackToSongs;

        private bool _isPreviewLevelPack;

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
                    _currentPlaylist = Playlist.LoadPlaylist(this._settings.currentPlaylistFile);
                }

                return _currentPlaylist;
            }

            set
            {
                _settings.currentPlaylistFile = value.fileLoc;
                _currentPlaylist = value;
            }
        }

        public bool IsCurrentLevelPackPreview
        {
            get
            {
                return _isPreviewLevelPack;
            }
        }

        public IBeatmapLevelPack CurrentLevelPack
        {
            get
            {
                return _currentLevelPack;
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
            _levelPackToSongs = new Dictionary<string, List<BeatmapLevelSO>>();

            CurrentEditingPlaylistLevelIds = new HashSet<string>();

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
        public void UpdateLevelRecords()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

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
                    // Always use the newest date.
                    var lastWriteTime = File.GetLastWriteTimeUtc(level.customSongInfo.path);
                    var lastCreateTime = File.GetCreationTimeUtc(level.customSongInfo.path);
                    var lastTime = lastWriteTime > lastCreateTime ? lastWriteTime : lastCreateTime;
                    _cachedLastWriteTimes[level.levelID] = (lastTime - EPOCH).TotalMilliseconds;
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
                        _levelIdToSongVersion.Add(level.levelID, version);

                        if (!_keyToSong.ContainsKey(version))
                        {
                            _keyToSong.Add(version, level);
                        }
                    }
                }
            }

            lastWriteTimer.Stop();
            Logger.Info("Determining song download time and determining mappings took {0}ms", lastWriteTimer.ElapsedMilliseconds);

            // Update song Infos, directory tree, and sort
            this.UpdateScoreSaberDataMapping();
            this.UpdatePlayCounts();

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
                Logger.Debug("Loading playlist for editing: {0}", this.Settings.currentEditingPlaylistFile);
                CurrentEditingPlaylist = Playlist.LoadPlaylist(this.Settings.currentEditingPlaylistFile);
                PlaylistsCollection.MatchSongsForPlaylist(CurrentEditingPlaylist);
            }

            if (CurrentEditingPlaylist == null)
            {
                Logger.Debug("Current editing playlist does not exit, create...");
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
                CurrentEditingPlaylistLevelIds.Add(ps.level.levelID);
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
        }

        /// <summary>
        /// Get a copy of the unfiltered, unsorted list of songs from level packs.
        /// </summary>
        public void UpdateLevelPackOriginalLists()
        {
            BeatmapLevelPackSO[] levelPacks = Resources.FindObjectsOfTypeAll<BeatmapLevelPackSO>();
            foreach (BeatmapLevelPackSO levelPack in levelPacks)
            {
                Logger.Debug("Attempting to get song list from levelPack: {0}...", levelPack);
                var beatmapLevelPack = levelPack as BeatmapLevelPackSO;

                // TODO - need to rethink interface here, not all level packs can be cast this high, some sort functions need it.
                //      - this helps prevent DLC from breaking everything
                if (beatmapLevelPack == null)
                {
                    continue;
                }

                _levelPackToSongs[levelPack.packName] = (beatmapLevelPack.beatmapLevelCollection as BeatmapLevelCollectionSO).GetPrivateField<BeatmapLevelSO[]>("_beatmapLevels").ToList();
                Logger.Debug("Got {0} songs from level collections...", _levelPackToSongs[levelPack.packName].Count);
                //_levelPackToSongs[levelPack.packName].ForEach(x => Logger.Debug("{0} by {1} = {2}", x.name, x.levelAuthorName, x.levelID));
            }
        }

        /// <summary>
        /// SongLoader doesn't fire event when we delete a song.
        /// </summary>
        /// <param name="levelPack"></param>
        /// <param name="levelId"></param>
        public void RemoveSongFromLevelPack(IBeatmapLevelPack levelPack, String levelId)
        {
            if (!_levelPackToSongs.ContainsKey(levelPack.packName))
            {
                Logger.Debug("Trying to remove song from level pack [{0}] but we do not have any information on it...", levelPack.packName);
                return;
            }

            _levelPackToSongs[levelPack.packName].RemoveAll(x => x.levelID == levelId);
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

            foreach (KeyValuePair<string, List<BeatmapLevelSO>> entry in _levelPackToSongs)
            {
                foreach (var level in entry.Value)
                {
                    if (!_levelIdToPlayCount.ContainsKey(level.levelID))
                    {
                        // Skip folders
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
                Logger.Warning("Score saber data is not ready yet...");
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
            this.CurrentEditingPlaylist.SavePlaylist();
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

            this.CurrentEditingPlaylist.songs.RemoveAll(x => x.level.levelID == songInfo.levelID);
            this.CurrentEditingPlaylistLevelIds.Remove(songInfo.levelID);

            this.CurrentEditingPlaylist.SavePlaylist();
        }

        /// <summary>
        /// Resets all the level packs back to their original values.
        /// </summary>
        /// <param name="pack"></param>
        private void ResetLevelPacks()
        {
            Logger.Debug("Setting level packs back to their original values!");

            BeatmapLevelPackSO[] levelPacks = Resources.FindObjectsOfTypeAll<BeatmapLevelPackSO>();
            foreach (BeatmapLevelPackSO levelPack in levelPacks)
            {
                if (!_levelPackToSongs.ContainsKey(levelPack.packName))
                {
                    Logger.Debug("We know nothing about pack: {0}", levelPack.packName);
                    continue;
                }

                var levels = _levelPackToSongs[levelPack.packName].ToArray();
                ReflectionUtil.SetPrivateField(levelPack.beatmapLevelCollection, "_beatmapLevels", levels);
            }
        }

        /// <summary>
        /// Set current level pack, reset all packs just in case.
        /// </summary>
        /// <param name="pack"></param>
        public void SetCurrentLevelPack(IBeatmapLevelPack pack)
        {
            Logger.Debug("Setting current level pack [{0}]: {1}", pack.packID, pack.packName);

            this.ResetLevelPacks();

            this._currentLevelPack = pack;

            var beatmapLevelPack = pack as BeatmapLevelPackSO;
            if (beatmapLevelPack == null)
            {
                Logger.Debug("DLC Detected...  Disabling SongBrowser...");
                _isPreviewLevelPack = true;
            }
            else
            {
                Logger.Debug("Owned level pack...  Enabling SongBrowser...");
                _isPreviewLevelPack = false;
            }

            this.Settings.currentLevelPackId = pack.packID;
            this.Settings.Save();
        }

        /// <summary>
        /// Overwrite the current level pack.
        /// </summary>
        private void OverwriteCurrentLevelPack()
        {
            Logger.Debug("Overwriting levelPack [{0}] beatmapLevelCollection", this._currentLevelPack);
            IBeatmapLevelPack levelPack = this._currentLevelPack;
            var levels = _sortedSongs.ToArray();
            ReflectionUtil.SetPrivateField(levelPack.beatmapLevelCollection, "_beatmapLevels", levels);
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

            // TODO - remove as part of unifying song list interface
            if (_isPreviewLevelPack)
            {
                return;
            }

            if (_levelPackToSongs.Count == 0)
            {
                Logger.Debug("Cannot process songs yet, level packs have not been processed...");
                return;
            }

            if (this._currentLevelPack == null || !this._levelPackToSongs.ContainsKey(this._currentLevelPack.packName))
            {
                Logger.Debug("Cannot process songs yet, no level pack selected...");
                return;
            }

            // Playlist filter will load the original songs.
            List<BeatmapLevelSO> unsortedSongs = null;
            List<BeatmapLevelSO> filteredSongs = null;
            if (this._settings.filterMode == SongFilterMode.Playlist && this.CurrentPlaylist != null)
            {
                unsortedSongs = null;
            }
            else
            {
                Logger.Debug("Using songs from level pack: {0}", this._currentLevelPack.packName);
                unsortedSongs = new List<BeatmapLevelSO>(_levelPackToSongs[this._currentLevelPack.packName]);
            }

            // filter
            Logger.Debug("Starting filtering songs...");
            Stopwatch stopwatch = Stopwatch.StartNew();

            switch (_settings.filterMode)
            {
                case SongFilterMode.Favorites:
                    filteredSongs = FilterFavorites();
                    break;
                case SongFilterMode.Search:
                    filteredSongs = FilterSearch(unsortedSongs);
                    break;
                case SongFilterMode.Playlist:
                    filteredSongs = FilterPlaylist();
                    break;
                case SongFilterMode.None:
                default:
                    Logger.Info("No song filter selected...");
                    filteredSongs = unsortedSongs;
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
                    SortOriginal(filteredSongs);
                    break;
                case SongSortMode.Newest:
                    SortNewest(filteredSongs);
                    break;
                case SongSortMode.Author:
                    SortAuthor(filteredSongs);
                    break;
                case SongSortMode.PlayCount:
                    SortPlayCount(filteredSongs);
                    break;
                case SongSortMode.PP:
                    SortPerformancePoints(filteredSongs);
                    break;
                case SongSortMode.Difficulty:
                    SortDifficulty(filteredSongs);
                    break;
                case SongSortMode.Random:
                    SortRandom(filteredSongs);
                    break;
                case SongSortMode.Default:
                default:
                    SortSongName(filteredSongs);
                    break;
            }

            if (this.Settings.invertSortResults && _settings.sortMode != SongSortMode.Random)
            {
                _sortedSongs.Reverse();
            }

            stopwatch.Stop();
            Logger.Info("Sorting songs took {0}ms", stopwatch.ElapsedMilliseconds);

            this.OverwriteCurrentLevelPack();
            //_sortedSongs.ForEach(x => Logger.Debug(x.levelID));
        }    
        
        /// <summary>
        /// For now the editing playlist will be considered the favorites playlist.
        /// Users can edit the settings file themselves.
        /// </summary>
        private List<BeatmapLevelSO> FilterFavorites()
        {
            Logger.Info("Filtering song list as favorites playlist...");
            if (this.CurrentEditingPlaylist != null)
            {
                this.CurrentPlaylist = this.CurrentEditingPlaylist;
            }
            return this.FilterPlaylist();
        }

        private List<BeatmapLevelSO> FilterSearch(List<BeatmapLevelSO> levels)
        {
            // Make sure we can actually search.
            if (this._settings.searchTerms.Count <= 0)
            {
                Logger.Error("Tried to search for a song with no valid search terms...");
                SortSongName(levels);
                return levels;
            }
            string searchTerm = this._settings.searchTerms[0];
            if (String.IsNullOrEmpty(searchTerm))
            {
                Logger.Error("Empty search term entered.");
                SortSongName(levels);
                return levels;
            }

            Logger.Info("Filtering song list by search term: {0}", searchTerm);

            var terms = searchTerm.Split(' ');
            foreach (var term in terms)
            {
                levels = levels.Intersect(
                    levels
                        .Where(x => $"{x.songName} {x.songSubName} {x.songAuthorName}".ToLower().Contains(term.ToLower()))
                        .ToList(
                    )
                ).ToList();
            }

            return levels;
        }

        private List<BeatmapLevelSO> FilterPlaylist()
        {
            // bail if no playlist, usually means the settings stored one the user then moved.
            if (this.CurrentPlaylist == null)
            {
                Logger.Error("Trying to load a null playlist...");
                this.Settings.filterMode = SongFilterMode.None;
                return null;
            }

            // Get song keys
            PlaylistsCollection.MatchSongsForPlaylist(this.CurrentPlaylist, true);

            Logger.Debug("Filtering songs for playlist: {0}", this.CurrentPlaylist.playlistTitle);            

            Dictionary<String, BeatmapLevelSO> levelDict = new Dictionary<string, BeatmapLevelSO>();
            foreach (KeyValuePair<string, List<BeatmapLevelSO>> entry in _levelPackToSongs)
            {
                foreach (BeatmapLevelSO level in entry.Value)
                {
                    if (!levelDict.ContainsKey(level.levelID))
                    {
                        levelDict.Add(level.levelID, level);
                    }
                }
            }

            List<BeatmapLevelSO> songList = new List<BeatmapLevelSO>();
            foreach (PlaylistSong ps in this.CurrentPlaylist.songs)
            {
                if (ps.level != null)
                {
                    songList.Add(levelDict[ps.level.levelID]);
                }
                else
                {
                    Logger.Warning("Could not find song in playlist: {0}", ps.songName);
                }
            }
            
            Logger.Debug("Playlist filtered song count: {0}", songList.Count);
            return songList;
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

            _sortedSongs = levels
                .OrderBy(x => levelIdToDifficultyValue[x.levelID])
                .ThenBy(x => x.songName)
                .ToList();
            _sortedSongs = levels;
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

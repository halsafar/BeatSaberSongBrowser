using SongBrowser.DataAccess;
using SongBrowser.UI;
using SongCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser
{
    public class SongBrowserModel
    {
        private readonly String CUSTOM_SONGS_DIR = Path.Combine("Beat Saber_Data", "CustomLevels");

        private readonly DateTime EPOCH = new DateTime(1970, 1, 1);

        // song_browser_settings.xml
        private SongBrowserSettings _settings;

        // song list management
        private double _customSongDirLastWriteTime = 0;
        private Dictionary<String, CustomPreviewBeatmapLevel> _levelIdToCustomLevel;
        private Dictionary<String, double> _cachedLastWriteTimes;
        private Dictionary<string, int> _weights;
        private Dictionary<BeatmapDifficulty, int> _difficultyWeights;
        private Dictionary<string, ScoreSaberData> _levelIdToScoreSaberData = null;
        private Dictionary<string, int> _levelIdToPlayCount;
        private Dictionary<string, string> _levelIdToSongVersion;

        public BeatmapCharacteristicSO CurrentBeatmapCharacteristicSO;

        public static Action<Dictionary<string, CustomPreviewBeatmapLevel>> didFinishProcessingSongs;

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

        /// <summary>
        /// Current editing playlist
        /// </summary>
        public Playlist CurrentEditingPlaylist;

        /// <summary>
        /// HashSet of LevelIds for quick lookup
        /// </summary>
        public HashSet<String> CurrentEditingPlaylistLevelIds;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SongBrowserModel()
        {
            _levelIdToCustomLevel = new Dictionary<string, CustomPreviewBeatmapLevel>();
            _cachedLastWriteTimes = new Dictionary<String, double>();
            _levelIdToScoreSaberData = new Dictionary<string, ScoreSaberData>();
            _levelIdToPlayCount = new Dictionary<string, int>();
            _levelIdToSongVersion = new Dictionary<string, string>();

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
                [BeatmapDifficulty.Easy] = 1,
                [BeatmapDifficulty.Normal] = 2,
                [BeatmapDifficulty.Hard] = 4,
                [BeatmapDifficulty.Expert] = 8,
                [BeatmapDifficulty.ExpertPlus] = 16,
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

            // Map some data for custom songs
            Regex r = new Regex(@"(\d+-\d+)", RegexOptions.IgnoreCase);
            Stopwatch lastWriteTimer = new Stopwatch();
            lastWriteTimer.Start();
            foreach (KeyValuePair<string, CustomPreviewBeatmapLevel> level in SongCore.Loader.CustomLevels)
            {
                // If we already know this levelID, don't both updating it.
                // SongLoader should filter duplicates but in case of failure we don't want to crash
                if (!_cachedLastWriteTimes.ContainsKey(level.Value.levelID) || customSongDirChanged)
                {
                    // Always use the newest date.
                    var lastWriteTime = File.GetLastWriteTimeUtc(level.Value.customLevelPath);
                    var lastCreateTime = File.GetCreationTimeUtc(level.Value.customLevelPath);
                    var lastTime = lastWriteTime > lastCreateTime ? lastWriteTime : lastCreateTime;
                    _cachedLastWriteTimes[level.Value.levelID] = (lastTime - EPOCH).TotalMilliseconds;
                }

                if (!_levelIdToCustomLevel.ContainsKey(level.Value.levelID))
                {
                    _levelIdToCustomLevel.Add(level.Value.levelID, level.Value);
                }

                if (!_levelIdToSongVersion.ContainsKey(level.Value.levelID))
                {
                    DirectoryInfo info = new DirectoryInfo(level.Value.customLevelPath);
                    string currentDirectoryName = info.Name;
                    
                    Match m = r.Match(level.Value.customLevelPath);
                    if (m.Success)
                    {
                        String version = m.Groups[1].Value;
                        Logger.Debug("SongKey: {0}={1}", level.Value.levelID, version);
                        _levelIdToSongVersion.Add(level.Value.levelID, version);
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
            if (CurrentEditingPlaylist == null && !String.IsNullOrEmpty(this.Settings.currentEditingPlaylistFile))
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
                    playlistAuthor = "SongBrowser",
                    fileLoc = this.Settings.currentEditingPlaylistFile,
                    image = Base64Sprites.PlaylistIconB64,
                    songs = new List<PlaylistSong>(),
                };
            }

            CurrentEditingPlaylistLevelIds = new HashSet<string>();
            foreach (PlaylistSong ps in CurrentEditingPlaylist.songs)
            {
                // Sometimes we cannot match a song
                if (ps.level == null)
                {
                    continue;
                }

                CurrentEditingPlaylistLevelIds.Add(ps.level.levelID);
            }

            // Signal complete
            if (SongCore.Loader.CustomLevels.Count > 0)
            {
                didFinishProcessingSongs?.Invoke(SongCore.Loader.CustomLevels);
            }

            timer.Stop();

            Logger.Info("Updating songs infos took {0}ms", timer.ElapsedMilliseconds);
        }

        /// <summary>
        /// SongLoader doesn't fire event when we delete a song.
        /// </summary>
        /// <param name="levelPack"></param>
        /// <param name="levelId"></param>
        public void RemoveSongFromLevelPack(IBeatmapLevelPack levelPack, String levelId)
        {
            levelPack.beatmapLevelCollection.beatmapLevels.ToList().RemoveAll(x => x.levelID == levelId);
        }

        /// <summary>
        /// Update the gameplay play counts.
        /// </summary>
        /// <param name="gameplayMode"></param>
        private void UpdatePlayCounts()
        {
            // Reset current playcounts
            _levelIdToPlayCount = new Dictionary<string, int>();

            // Build a map of levelId to sum of all playcounts and sort.
            PlayerDataModelSO playerData = Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().FirstOrDefault();            
            foreach (var levelData in playerData.currentLocalPlayer.levelsStatsData)
            {
                int currentCount = 0;
                if (!_levelIdToPlayCount.ContainsKey(levelData.levelID))
                {
                    _levelIdToPlayCount.Add(levelData.levelID, currentCount);
                }

                _levelIdToPlayCount[levelData.levelID] += (currentCount + levelData.playCount);
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

            foreach (var level in SongCore.Loader.CustomLevels)
            {
                // Skip
                if (_levelIdToScoreSaberData.ContainsKey(level.Value.levelID))
                {
                    continue;
                }

                ScoreSaberData scoreSaberData = null;

                // try to version match first
                if (_levelIdToSongVersion.ContainsKey(level.Value.levelID))
                {
                    String version = _levelIdToSongVersion[level.Value.levelID];
                    if (scoreSaberDataFile.SongVersionToScoreSaberData.ContainsKey(version))
                    {
                        scoreSaberData = scoreSaberDataFile.SongVersionToScoreSaberData[version];
                    }
                }

                if (scoreSaberData != null)
                {
                    //Logger.Debug("{0} = {1}pp", level.songName, pp);
                    _levelIdToScoreSaberData.Add(level.Value.levelID, scoreSaberData);
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
                hash = songInfo.levelID,
                key = _levelIdToSongVersion.ContainsKey(songInfo.levelID) ? _levelIdToSongVersion[songInfo.levelID] : "",
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

            this.CurrentEditingPlaylist.songs.RemoveAll(x => x.level != null && x.level.levelID == songInfo.levelID);
            this.CurrentEditingPlaylistLevelIds.RemoveWhere(x => x == songInfo.levelID);

            this.CurrentEditingPlaylist.SavePlaylist();
        }

        /// <summary>
        /// Overwrite the current level pack.
        /// </summary>
        private void OverwriteCurrentLevelPack(IBeatmapLevelPack pack, List<IPreviewBeatmapLevel> sortedLevels)
        {
            Logger.Debug("Overwriting levelPack [{0}] beatmapLevelCollection.levels", pack);
            if (pack.packID == PluginConfig.CUSTOM_SONG_LEVEL_PACK_ID)
            {
                Logger.Debug("Overwriting CustomBeatMap Collection...");
                var newLevels = sortedLevels.Select(x => x as CustomPreviewBeatmapLevel);
                ReflectionUtil.SetPrivateField(pack.beatmapLevelCollection, "_customPreviewBeatmapLevels", newLevels.ToArray());
            }
            else
            {
                // Hack to see if level pack is purchased or not.
                BeatmapLevelPackSO beatmapLevelPack = pack as BeatmapLevelPackSO;
                if (beatmapLevelPack != null)
                {
                    Logger.Debug("Owned level pack...");
                    var newLevels = sortedLevels.Select(x => x as BeatmapLevelSO);
                    ReflectionUtil.SetPrivateField(pack.beatmapLevelCollection, "_beatmapLevels", newLevels.ToArray());
                }
                else
                {
                    Logger.Debug("DLC Detected...");
                    var newLevels = sortedLevels.Select(x => x as PreviewBeatmapLevelSO);
                    ReflectionUtil.SetPrivateField(pack.beatmapLevelCollection, "_beatmapLevels", newLevels.ToArray());
                }

                Logger.Debug("Overwriting Regular Collection...");
            }
        }

        /// <summary>
        /// Sort the song list based on the settings.
        /// </summary>
        public void ProcessSongList(IBeatmapLevelPack pack)
        {
            Logger.Trace("ProcessSongList()");

            List<IPreviewBeatmapLevel> unsortedSongs = null;
            List<IPreviewBeatmapLevel> filteredSongs = null;
            List<IPreviewBeatmapLevel> sortedSongs = null;

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

            // Abort
            if (pack == null)
            {
                Logger.Debug("Cannot process songs yet, no level pack selected...");
                return;
            }
            
            // fetch unsorted songs.
            if (this._settings.filterMode == SongFilterMode.Playlist && this.CurrentPlaylist != null)
            {
                unsortedSongs = null;
            }
            else
            {
                Logger.Debug("Using songs from level pack: {0}", pack.packID);
                unsortedSongs = pack.beatmapLevelCollection.beatmapLevels.ToList();
            }

            // filter
            Logger.Debug("Starting filtering songs...");
            Stopwatch stopwatch = Stopwatch.StartNew();

            switch (_settings.filterMode)
            {
                case SongFilterMode.Favorites:
                    filteredSongs = FilterFavorites(pack);
                    break;
                case SongFilterMode.Search:
                    filteredSongs = FilterSearch(unsortedSongs);
                    break;
                case SongFilterMode.Playlist:
                    filteredSongs = FilterPlaylist(pack);
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
                    sortedSongs = SortOriginal(filteredSongs);
                    break;
                case SongSortMode.Newest:
                    sortedSongs = SortNewest(filteredSongs);
                    break;
                case SongSortMode.Author:
                    sortedSongs = SortAuthor(filteredSongs);
                    break;
                case SongSortMode.PlayCount:
                    sortedSongs = SortPlayCount(filteredSongs);
                    break;
                case SongSortMode.PP:
                    sortedSongs = SortPerformancePoints(filteredSongs);
                    break;
                case SongSortMode.Difficulty:
                    sortedSongs = SortDifficulty(filteredSongs);
                    break;
                case SongSortMode.Random:
                    sortedSongs = SortRandom(filteredSongs);
                    break;
                case SongSortMode.Default:
                default:
                    sortedSongs = SortSongName(filteredSongs);
                    break;
            }

            if (this.Settings.invertSortResults && _settings.sortMode != SongSortMode.Random)
            {
                sortedSongs.Reverse();
            }

            stopwatch.Stop();
            Logger.Info("Sorting songs took {0}ms", stopwatch.ElapsedMilliseconds);

            this.OverwriteCurrentLevelPack(pack, sortedSongs);
            //_sortedSongs.ForEach(x => Logger.Debug(x.levelID));
        }

        /// <summary>
        /// For now the editing playlist will be considered the favorites playlist.
        /// Users can edit the settings file themselves.
        /// </summary>
        private List<IPreviewBeatmapLevel> FilterFavorites(IBeatmapLevelPack pack)
        {
            Logger.Info("Filtering song list as favorites playlist...");
            if (this.CurrentEditingPlaylist != null)
            {
                this.CurrentPlaylist = this.CurrentEditingPlaylist;
            }
            return this.FilterPlaylist(pack);
        }

        /// <summary>
        /// Filter for a search query.
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> FilterSearch(List<IPreviewBeatmapLevel> levels)
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

        /// <summary>
        /// Filter for a playlist (favorites uses this).
        /// </summary>
        /// <param name="pack"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> FilterPlaylist(IBeatmapLevelPack pack)
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

            Dictionary<String, IPreviewBeatmapLevel> levelDict = new Dictionary<string, IPreviewBeatmapLevel>();
            foreach (var level in pack.beatmapLevelCollection.beatmapLevels)
            {
                if (!levelDict.ContainsKey(level.levelID))
                {
                    levelDict.Add(level.levelID, level);
                }               
            }

            List<IPreviewBeatmapLevel> songList = new List<IPreviewBeatmapLevel>();
            foreach (PlaylistSong ps in this.CurrentPlaylist.songs)
            {
                if (ps.level != null)
                {
                    songList.Add(levelDict[ps.level.levelID] as IPreviewBeatmapLevel);
                }
                else
                {
                    Logger.Debug("Could not find song in playlist: {0}", ps.songName);
                }
            }

            Logger.Debug("Playlist filtered song count: {0}", songList.Count);
            return songList;
        }

        /// <summary>
        /// Sorting returns original list.
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortOriginal(List<IPreviewBeatmapLevel> levels)
        {
            Logger.Info("Sorting song list as original");
            return levels;
        }

        /// <summary>
        /// Sorting by newest (file time, creation+modified).
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortNewest(List<IPreviewBeatmapLevel> levels)
        {
            Logger.Info("Sorting song list as newest.");
            return levels
                .OrderBy(x => _weights.ContainsKey(x.levelID) ? _weights[x.levelID] : 0)
                .ThenByDescending(x => !_levelIdToCustomLevel.ContainsKey(x.levelID) ? (_weights.ContainsKey(x.levelID) ? _weights[x.levelID] : 0) : _cachedLastWriteTimes[x.levelID])
                .ToList();
        }

        /// <summary>
        /// Sorting by the song author.
        /// </summary>
        /// <param name="levelIds"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortAuthor(List<IPreviewBeatmapLevel> levelIds)
        {
            Logger.Info("Sorting song list by author");
            return levelIds
                .OrderBy(x => x.songAuthorName)
                .ThenBy(x => x.songName)
                .ToList();
        }

        /// <summary>
        /// Sorting by song play count.
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortPlayCount(List<IPreviewBeatmapLevel> levels)
        {
            Logger.Info("Sorting song list by playcount");
            return levels
                .OrderByDescending(x => _levelIdToPlayCount.ContainsKey(x.levelID) ? _levelIdToPlayCount[x.levelID] : 0)
                .ThenBy(x => x.songName)
                .ToList();
        }

        /// <summary>
        /// Sorting by PP.
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortPerformancePoints(List<IPreviewBeatmapLevel> levels)
        {
            Logger.Info("Sorting song list by performance points...");

            return levels
                .OrderByDescending(x => _levelIdToScoreSaberData.ContainsKey(x.levelID) ? _levelIdToScoreSaberData[x.levelID].maxPp : 0)
                .ToList();
        }

        private List<IPreviewBeatmapLevel> SortDifficulty(List<IPreviewBeatmapLevel> levels)
        {
            Logger.Info("Sorting song list by difficulty...");

            IEnumerable<BeatmapDifficulty> difficultyIterator = Enum.GetValues(typeof(BeatmapDifficulty)).Cast<BeatmapDifficulty>();
            Dictionary<string, int> levelIdToDifficultyValue = new Dictionary<string, int>();
            /*foreach (IPreviewBeatmapLevel level in levels)
            {
                IBeatmapLevelData levelData = null;
                if (level as BeatmapLevelSO != null)
                {
                    Logger.Debug("Normal LevelData");
                    levelData = (level as BeatmapLevelSO).beatmapLevelData;
                }
                else if (_levelIdToCustomLevel.ContainsKey(level.levelID))
                {
                    Logger.Debug("Custom LevelData");
                    levelData = (_levelIdToCustomLevel[level.levelID] as CustomBeatmapLevel).beatmapLevelData;
                }
                
                if (levelData == null)
                {
                    Logger.Debug("NO LevelData!!");
                    continue;
                }

                if (!levelIdToDifficultyValue.ContainsKey(level.levelID))
                {
                    int difficultyValue = 0;

                    // Get the beatmap difficulties
                    Logger.Debug("levelData.difficultyBeatmapSets={0}", levelData.difficultyBeatmapSets);
                    var difficulties = levelData.difficultyBeatmapSets
                        .Where(x => x.beatmapCharacteristic == this.CurrentBeatmapCharacteristicSO)
                        .SelectMany(x => x.difficultyBeatmaps);                    

                    foreach (IDifficultyBeatmap difficultyBeatmap in difficulties)
                    {
                        difficultyValue += _difficultyWeights[difficultyBeatmap.difficulty];
                    }
                    levelIdToDifficultyValue.Add(level.levelID, difficultyValue);
                }
            }*/

            return levels
                .OrderBy(x => levelIdToDifficultyValue[x.levelID])
                .ThenBy(x => x.songName)
                .ToList();
        }

        /// <summary>
        /// Randomize the sorting.
        /// </summary>
        /// <param name="levelIds"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortRandom(List<IPreviewBeatmapLevel> levelIds)
        {
            Logger.Info("Sorting song list by random (seed={0})...", this.Settings.randomSongSeed);

            System.Random rnd = new System.Random(this.Settings.randomSongSeed);

            return levelIds
                .OrderBy(x => rnd.Next())
                .ToList();
        }

        /// <summary>
        /// Sorting by the song name.
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortSongName(List<IPreviewBeatmapLevel> levels)
        {
            Logger.Info("Sorting song list as default (songName)");
            return levels
                .OrderBy(x => x.songName)
                .ThenBy(x => x.songAuthorName)
                .ToList();
        }
    }
}

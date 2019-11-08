using SongBrowser.DataAccess;
using SongBrowser.Internals;
using SongBrowser.UI;
using SongCore.OverrideClasses;
using SongCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static StandardLevelInfoSaveData;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser
{
    public class SongBrowserModel
    {
        public const string FilteredSongsPackId = "SongBrowser_FilteredSongPack";

        private readonly String CUSTOM_SONGS_DIR = Path.Combine("Beat Saber_Data", "CustomLevels");

        private readonly DateTime EPOCH = new DateTime(1970, 1, 1);

        // song_browser_settings.xml
        private SongBrowserSettings _settings;

        // song list management
        private double _customSongDirLastWriteTime = 0;        
        private Dictionary<String, double> _cachedLastWriteTimes;
        private Dictionary<BeatmapDifficulty, int> _difficultyWeights;
        private Dictionary<string, int> _levelIdToPlayCount;

        public BeatmapCharacteristicSO CurrentBeatmapCharacteristicSO;

        public static Func<IBeatmapLevelPack, List<IPreviewBeatmapLevel>> CustomFilterHandler;
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
            _cachedLastWriteTimes = new Dictionary<String, double>();
            _levelIdToPlayCount = new Dictionary<string, int>();

            CurrentEditingPlaylistLevelIds = new HashSet<string>();

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
                    double lastWriteTime = GetSongUserDate(level.Value);
                    _cachedLastWriteTimes[level.Value.levelID] = lastWriteTime;
                }
            }

            lastWriteTimer.Stop();
            Logger.Info("Determining song download time and determining mappings took {0}ms", lastWriteTimer.ElapsedMilliseconds);

            // Update song Infos, directory tree, and sort
            this.UpdatePlayCounts();

            // Check if we need to upgrade settings file favorites
            try
            {
                this.Settings.ConvertFavoritesToPlaylist(SongCore.Loader.CustomLevels);
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
                PlaylistsCollection.MatchSongsForPlaylist(CurrentEditingPlaylist, true);
            }

            if (CurrentEditingPlaylist == null)
            {
                Logger.Debug("Current editing playlist does not exit, create...");
                CurrentEditingPlaylist = new Playlist
                {
                    playlistTitle = "Song Browser Favorites",
                    playlistAuthor = "SongBrowser",
                    fileLoc = this.Settings.currentEditingPlaylistFile,
                    image = Base64Sprites.SpriteToBase64(Base64Sprites.BeastSaberLogo),
                    songs = new List<PlaylistSong>(),
                };
            }

            CurrentEditingPlaylistLevelIds = new HashSet<string>();
            foreach (PlaylistSong ps in CurrentEditingPlaylist.songs)
            {
                // Sometimes we cannot match a song
                string levelId = null;
                if (ps.level != null)
                {
                    levelId = ps.level.levelID;
                }
                else if (!String.IsNullOrEmpty(ps.levelId))
                {
                    levelId = ps.levelId;
                }
                else
                {
                    //Logger.Debug("MISSING SONG {0}", ps.songName);
                    continue;
                }

                CurrentEditingPlaylistLevelIds.Add(levelId);
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
        /// Try to get the date from the cover file, likely the most reliable.
        /// Fall back on the folders creation date.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private double GetSongUserDate(CustomPreviewBeatmapLevel level)
        {
            var coverPath = Path.Combine(level.customLevelPath, level.standardLevelInfoSaveData.coverImageFilename);
            var lastTime = EPOCH;
            if (File.Exists(coverPath))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(coverPath);
                var lastCreateTime = File.GetCreationTimeUtc(coverPath);
                lastTime = lastWriteTime > lastCreateTime ? lastWriteTime : lastCreateTime;
            }
            else
            {
                var lastCreateTime = File.GetCreationTimeUtc(level.customLevelPath);
                lastTime = lastCreateTime;
            }

            return (lastTime - EPOCH).TotalMilliseconds;
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
            foreach (var levelData in playerData.playerData.levelsStatsData)
            {
                if (!_levelIdToPlayCount.ContainsKey(levelData.levelID))
                {
                    _levelIdToPlayCount.Add(levelData.levelID, 0);
                }

                _levelIdToPlayCount[levelData.levelID] += levelData.playCount;
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
                hash = CustomHelpers.GetSongHash(songInfo.levelID),
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
        /// Sort the song list based on the settings.
        /// </summary>
        public void ProcessSongList(LevelPackLevelsViewController levelsViewController)
        {
            Logger.Trace("ProcessSongList()");

            List<IPreviewBeatmapLevel> unsortedSongs = null;
            List<IPreviewBeatmapLevel> filteredSongs = null;
            List<IPreviewBeatmapLevel> sortedSongs = null;

            // Abort
            if (levelsViewController.levelPack == null)
            {
                Logger.Debug("Cannot process songs yet, no level pack selected...");
                return;
            }
            
            // fetch unsorted songs.
            // playlists always use customsongs
            if (this._settings.filterMode == SongFilterMode.Playlist && this.CurrentPlaylist != null)
            {
                unsortedSongs = null;
            }
            else
            {
                Logger.Debug("Using songs from level pack: {0}", levelsViewController.levelPack.packID);
                unsortedSongs = levelsViewController.levelPack.beatmapLevelCollection.beatmapLevels.ToList();
            }

            // filter
            Logger.Debug($"Starting filtering songs by {_settings.filterMode}");
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
                case SongFilterMode.Ranked:
                    filteredSongs = FilterRanked(unsortedSongs, true, false);
                    break;
                case SongFilterMode.Unranked:
                    filteredSongs = FilterRanked(unsortedSongs, false, true);
                    break;
                case SongFilterMode.Custom:
                    Logger.Info("Song filter mode set to custom. Deferring filter behaviour to another mod.");
                    filteredSongs = CustomFilterHandler != null ? CustomFilterHandler.Invoke(levelsViewController.levelPack) : unsortedSongs;
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
                case SongSortMode.UpVotes:
                    sortedSongs = SortUpVotes(filteredSongs);
                    break;
                case SongSortMode.PlayCount:
                    sortedSongs = SortBeatSaverPlayCount(filteredSongs);
                    break;
                case SongSortMode.Rating:
                    sortedSongs = SortBeatSaverRating(filteredSongs);
                    break;
                case SongSortMode.Heat:
                    sortedSongs = SortBeatSaverHeat(filteredSongs);
                    break;
                case SongSortMode.YourPlayCount:
                    sortedSongs = SortPlayCount(filteredSongs);
                    break;
                case SongSortMode.PP:
                    sortedSongs = SortPerformancePoints(filteredSongs);
                    break;
                case SongSortMode.Stars:
                    sortedSongs = SortStars(filteredSongs);
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

            // Asterisk the pack name so it is identifable as filtered.
            var packName = levelsViewController.levelPack.packName;
            if (!packName.EndsWith("*") && _settings.filterMode != SongFilterMode.None)
            {
                packName += "*";
            }
            BeatmapLevelPack levelPack = new BeatmapLevelPack(SongBrowserModel.FilteredSongsPackId, packName, levelsViewController.levelPack.shortPackName, levelsViewController.levelPack.coverImage, new BeatmapLevelCollection(sortedSongs.ToArray()));
            levelsViewController.SetData(levelPack);

            //_sortedSongs.ForEach(x => Logger.Debug(x.levelID));
        }

        /// <summary>
        /// For now the editing playlist will be considered the favorites playlist.
        /// Users can edit the settings file themselves.
        /// </summary>
        private List<IPreviewBeatmapLevel> FilterFavorites()
        {
            Logger.Info("Filtering song list as favorites playlist...");
            if (this.CurrentEditingPlaylist != null)
            {
                this.CurrentPlaylist = this.CurrentEditingPlaylist;
            }
            return this.FilterPlaylist();
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
                        .Where(x => $"{x.songName} {x.songSubName} {x.songAuthorName} {x.levelAuthorName}".ToLower().Contains(term.ToLower()))
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
        private List<IPreviewBeatmapLevel> FilterPlaylist()
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

            Dictionary<String, CustomPreviewBeatmapLevel> levelDict = new Dictionary<string, CustomPreviewBeatmapLevel>();
            foreach (var level in SongCore.Loader.CustomLevels)
            {
                if (!levelDict.ContainsKey(level.Value.levelID))
                {
                    levelDict.Add(level.Value.levelID, level.Value);
                }               
            }

            List<IPreviewBeatmapLevel> songList = new List<IPreviewBeatmapLevel>();
            foreach (PlaylistSong ps in this.CurrentPlaylist.songs)
            {
                if (ps.level != null && levelDict.ContainsKey(ps.level.levelID))
                {
                    songList.Add(levelDict[ps.level.levelID]);
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
        /// Filter songs based on ranked or unranked status.
        /// </summary>
        /// <param name="levels"></param>
        /// <param name="includeRanked"></param>
        /// <param name="includeUnranked"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> FilterRanked(List<IPreviewBeatmapLevel> levels, bool includeRanked, bool includeUnranked)
        {
            return levels.Where(x =>
            {
                var hash = CustomHelpers.GetSongHash(x.levelID);
                double maxPP = 0.0;
                if (SongDataCore.Plugin.ScoreSaber.Data.Songs.ContainsKey(hash))
                {
                     maxPP = SongDataCore.Plugin.ScoreSaber.Data.Songs[hash].diffs.Max(y => y.pp);
                }

                if (maxPP > 0f)
                {
                    return includeRanked;
                }
                else
                {
                    return includeUnranked;
                }
            }).ToList();
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
                .OrderByDescending(x => _cachedLastWriteTimes.ContainsKey(x.levelID) ? _cachedLastWriteTimes[x.levelID] : int.MinValue)
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
        /// Sorting by song users play count.
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

            if (!SongDataCore.Plugin.ScoreSaber.IsDataAvailable())
            {
                return levels;
            }

            return levels
                .OrderByDescending(x =>
                {
                    var hash = CustomHelpers.GetSongHash(x.levelID);
                    if (SongDataCore.Plugin.ScoreSaber.Data.Songs.ContainsKey(hash))
                    {
                        return SongDataCore.Plugin.ScoreSaber.Data.Songs[hash].diffs.Max(y => y.pp);
                    }
                    else
                    {
                        return 0;
                    }
                })
                .ToList();
        }

        /// <summary>
        /// Sorting by star rating.
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortStars(List<IPreviewBeatmapLevel> levels)
        {
            Logger.Info("Sorting song list by star points...");

            if (!SongDataCore.Plugin.ScoreSaber.IsDataAvailable())
            {
                return levels;
            }

            return levels
                .OrderByDescending(x =>
                {
                    var hash = CustomHelpers.GetSongHash(x.levelID);
                    var stars = 0.0;
                    if (SongDataCore.Plugin.ScoreSaber.Data.Songs.ContainsKey(hash))
                    {
                        var diffs = SongDataCore.Plugin.ScoreSaber.Data.Songs[hash].diffs;   
                        stars = diffs.Max(y => y.star);
                    }

                    //Logger.Debug("Stars={0}", stars);
                    if (stars != 0)
                    {
                        return stars;
                    }

                    if (_settings.invertSortResults)
                    {
                        return double.MaxValue;
                    }
                    else
                    {
                        return double.MinValue;
                    }
                })
                .ToList();
        }

        /// <summary>
        /// Attempt to sort by songs containing easy first
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortDifficulty(List<IPreviewBeatmapLevel> levels)
        {
            Logger.Info("Sorting song list by difficulty (DISABLED!!!)...");
            /*
            IEnumerable<BeatmapDifficulty> difficultyIterator = Enum.GetValues(typeof(BeatmapDifficulty)).Cast<BeatmapDifficulty>();
            Dictionary<string, int> levelIdToDifficultyValue = new Dictionary<string, int>();
            foreach (IPreviewBeatmapLevel level in levels)
            {
                // only need to process a level once
                if (levelIdToDifficultyValue.ContainsKey(level.levelID))
                {
                    continue;
                }

                // TODO - fix, not honoring beatmap characteristic. 
                int difficultyValue = 0;
                if (level as BeatmapLevelSO != null)
                {
                    var beatmapSet = (level as BeatmapLevelSO).difficultyBeatmapSets;
                    difficultyValue = beatmapSet                        
                        .SelectMany(x => x.difficultyBeatmaps)
                        .Sum(x => _difficultyWeights[x.difficulty]);
                }
                else if (_levelIdToCustomLevel.ContainsKey(level.levelID))
                {                   
                    var beatmapSet = (_levelIdToCustomLevel[level.levelID] as CustomPreviewBeatmapLevel).standardLevelInfoSaveData.difficultyBeatmapSets;
                    difficultyValue = beatmapSet
                        .SelectMany(x => x.difficultyBeatmaps)
                        .Sum(x => _difficultyWeights[(BeatmapDifficulty)Enum.Parse(typeof(BeatmapDifficulty), x.difficulty)]);
                }

                levelIdToDifficultyValue.Add(level.levelID, difficultyValue);                
            }

            return levels
                .OrderBy(x => levelIdToDifficultyValue[x.levelID])
                .ThenBy(x => x.songName)
                .ToList();*/
            return levels;
        }

        /// <summary>
        /// Randomize the sorting.
        /// </summary>
        /// <param name="levelIds"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortRandom(List<IPreviewBeatmapLevel> levelIds)
        {
            Logger.Info("Sorting song list by random (seed={0})...", Settings.randomSongSeed);

            System.Random rnd = new System.Random(Settings.randomSongSeed);

            return levelIds
                .OrderBy(x => x.songName)
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

        /// <summary>
        /// Sorting by BeatSaver UpVotes.
        /// </summary>
        /// <param name="levelIds"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortUpVotes(List<IPreviewBeatmapLevel> levelIds)
        {
            Logger.Info("Sorting song list by BeatSaver UpVotes");

            // Do not always have data when trying to sort by UpVotes
            if (!SongDataCore.Plugin.BeatSaver.IsDataAvailable())
            {
                return levelIds;
            }

            return levelIds
                .OrderByDescending(x => {
                    var hash = CustomHelpers.GetSongHash(x.levelID);
                    if (SongDataCore.Plugin.BeatSaver.Data.Songs.ContainsKey(hash))
                    {
                        return SongDataCore.Plugin.BeatSaver.Data.Songs[hash].stats.upVotes;
                    }
                    else
                    {
                        return int.MinValue;
                    }
                })
                .ToList();
        }

        /// <summary>
        /// Sorting by BeatSaver playcount stat.
        /// </summary>
        /// <param name="levelIds"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortBeatSaverPlayCount(List<IPreviewBeatmapLevel> levelIds)
        {
            Logger.Info("Sorting song list by BeatSaver PlayCount");

            // Do not always have data when trying to sort by UpVotes
            if (!SongDataCore.Plugin.BeatSaver.IsDataAvailable())
            {
                return levelIds;
            }

            return levelIds
                .OrderByDescending(x => {
                    var hash = CustomHelpers.GetSongHash(x.levelID);
                    if (SongDataCore.Plugin.BeatSaver.Data.Songs.ContainsKey(hash))
                    {
                        return SongDataCore.Plugin.BeatSaver.Data.Songs[hash].stats.plays;
                    }
                    else
                    {
                        return int.MinValue;
                    }
                })
                .ToList();
        }

        /// <summary>
        /// Sorting by BeatSaver rating stat.
        /// </summary>
        /// <param name="levelIds"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortBeatSaverRating(List<IPreviewBeatmapLevel> levelIds)
        {
            Logger.Info("Sorting song list by BeatSaver Rating!");

            // Do not always have data when trying to sort by rating
            if (!SongDataCore.Plugin.BeatSaver.IsDataAvailable())
            {
                return levelIds;
            }

            return levelIds
                .OrderByDescending(x => {
                    var hash = CustomHelpers.GetSongHash(x.levelID);
                    if (SongDataCore.Plugin.BeatSaver.Data.Songs.ContainsKey(hash))
                    {
                        return SongDataCore.Plugin.BeatSaver.Data.Songs[hash].stats.rating;
                    }
                    else
                    {
                        return int.MinValue;
                    }
                })
                .ToList();
        }

        /// <summary>
        /// Sorting by BeatSaver heat stat.
        /// </summary>
        /// <param name="levelIds"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortBeatSaverHeat(List<IPreviewBeatmapLevel> levelIds)
        {
            Logger.Info("Sorting song list by BeatSaver Heat!");

            // Do not always have data when trying to sort by heat
            if (!SongDataCore.Plugin.BeatSaver.IsDataAvailable())
            {
                return levelIds;
            }

            return levelIds
                .OrderByDescending(x => {
                    var hash = CustomHelpers.GetSongHash(x.levelID);
                    if (SongDataCore.Plugin.BeatSaver.Data.Songs.ContainsKey(hash))
                    {
                        return SongDataCore.Plugin.BeatSaver.Data.Songs[hash].stats.heat;
                    }
                    else
                    {
                        return int.MinValue;
                    }
                })
                .ToList();
        }
    }
}

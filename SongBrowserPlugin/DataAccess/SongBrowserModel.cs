using SongBrowser.DataAccess;
using SongCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser
{
    public class SongBrowserModel
    {
        public static readonly string FilteredSongsCollectionName = CustomLevelLoader.kCustomLevelPackPrefixId + "SongBrowser_FilteredSongPack";
        public static readonly string PlaylistSongsCollectionName = "SongBrowser_PlaylistPack";

        private readonly String CUSTOM_SONGS_DIR = Path.Combine("Beat Saber_Data", "CustomLevels");

        private readonly DateTime EPOCH = new DateTime(1970, 1, 1);

        // song_browser_settings.xml
        private SongBrowserSettings _settings;

        // song list management
        private double _customSongDirLastWriteTime = 0;        
        private Dictionary<String, double> _cachedLastWriteTimes;
        private Dictionary<string, int> _levelIdToPlayCount;

        public BeatmapCharacteristicSO CurrentBeatmapCharacteristicSO;

        public static Func<IAnnotatedBeatmapLevelCollection, List<IPreviewBeatmapLevel>> CustomFilterHandler;
        public static Action<Dictionary<string, CustomPreviewBeatmapLevel>> didFinishProcessingSongs;

        public bool SortWasMissingData { get; private set; } = false;

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

        /// <summary>
        /// Constructor.
        /// </summary>
        public SongBrowserModel()
        {
            _cachedLastWriteTimes = new Dictionary<String, double>();
            _levelIdToPlayCount = new Dictionary<string, int>();
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
        /// <param name="levelCollection"></param>
        /// <param name="levelId"></param>
        public void RemoveSongFromLevelCollection(IAnnotatedBeatmapLevelCollection levelCollection, String levelId)
        {
            levelCollection.beatmapLevelCollection.beatmapLevels.ToList().RemoveAll(x => x.levelID == levelId);
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
            PlayerDataModel playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault();
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
        /// Sort the song list based on the settings.
        /// </summary>
        public void ProcessSongList(IAnnotatedBeatmapLevelCollection selectedBeatmapCollection, LevelCollectionViewController levelCollectionViewController, LevelSelectionNavigationController navController)
        {
            Logger.Trace("ProcessSongList()");

            List<IPreviewBeatmapLevel> unsortedSongs = null;
            List<IPreviewBeatmapLevel> filteredSongs = null;
            List<IPreviewBeatmapLevel> sortedSongs = null;

            // Abort
            if (selectedBeatmapCollection == null)
            {
                Logger.Debug("Cannot process songs yet, no level collection selected...");
                return;
            }
            
            Logger.Debug("Using songs from level collection: {0}", selectedBeatmapCollection.collectionName);
            unsortedSongs = selectedBeatmapCollection.beatmapLevelCollection.beatmapLevels.ToList();

            // filter
            Logger.Debug($"Starting filtering songs by {_settings.filterMode}");
            Stopwatch stopwatch = Stopwatch.StartNew();

            switch (_settings.filterMode)
            {
                case SongFilterMode.Favorites:
                    filteredSongs = FilterFavorites(unsortedSongs);
                    break;
                case SongFilterMode.Search:
                    filteredSongs = FilterSearch(unsortedSongs);
                    break;
                case SongFilterMode.Ranked:
                    filteredSongs = FilterRanked(unsortedSongs, true, false);
                    break;
                case SongFilterMode.Unranked:
                    filteredSongs = FilterRanked(unsortedSongs, false, true);
                    break;
                case SongFilterMode.Custom:
                    Logger.Info("Song filter mode set to custom. Deferring filter behaviour to another mod.");
                    filteredSongs = CustomFilterHandler != null ? CustomFilterHandler.Invoke(selectedBeatmapCollection) : unsortedSongs;
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

            SortWasMissingData = false;

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

            // Still hacking in a custom level pack
            // Asterisk the pack name so it is identifable as filtered.
            var packName = selectedBeatmapCollection.collectionName;
            if (!packName.EndsWith("*") && _settings.filterMode != SongFilterMode.None)
            {
                packName += "*";
            }
            BeatmapLevelPack levelPack = new BeatmapLevelPack(SongBrowserModel.FilteredSongsCollectionName, packName, selectedBeatmapCollection.collectionName, selectedBeatmapCollection.coverImage, new BeatmapLevelCollection(sortedSongs.ToArray()));

            GameObject _noDataGO = levelCollectionViewController.GetPrivateField<GameObject>("_noDataInfoGO");
            bool _showPlayerStatsInDetailView = navController.GetPrivateField<bool>("_showPlayerStatsInDetailView");
            bool _showPracticeButtonInDetailView = navController.GetPrivateField<bool>("_showPracticeButtonInDetailView");

            navController.SetData(levelPack, true, _showPlayerStatsInDetailView, _showPracticeButtonInDetailView, _noDataGO);

            //_sortedSongs.ForEach(x => Logger.Debug(x.levelID));
        }

        /// <summary>
        /// Filter songs based on playerdata favorites.
        /// </summary>
        private List<IPreviewBeatmapLevel> FilterFavorites(List<IPreviewBeatmapLevel> levels)
        {
            Logger.Info("Filtering song list as favorites playlist...");

            PlayerDataModel playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault();
            return levels.Where(x => playerData.playerData.favoritesLevelIds.Contains(x.levelID)).ToList();
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
                var hash = SongBrowserModel.GetSongHash(x.levelID);
                double maxPP = 0.0;
                if (SongDataCore.Plugin.Songs.Data.Songs.ContainsKey(hash))
                {
                     maxPP = SongDataCore.Plugin.Songs.Data.Songs[hash].diffs.Max(y => y.pp);
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

            if (!SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                SortWasMissingData = true;
                return levels;
            }

            return levels
                .OrderByDescending(x =>
                {
                    var hash = SongBrowserModel.GetSongHash(x.levelID);
                    if (SongDataCore.Plugin.Songs.Data.Songs.ContainsKey(hash))
                    {
                        return SongDataCore.Plugin.Songs.Data.Songs[hash].diffs.Max(y => y.pp);
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

            if (!SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                SortWasMissingData = true;
                return levels;
            }

            return levels
                .OrderByDescending(x =>
                {
                    var hash = SongBrowserModel.GetSongHash(x.levelID);
                    var stars = 0.0;
                    if (SongDataCore.Plugin.Songs.Data.Songs.ContainsKey(hash))
                    {
                        var diffs = SongDataCore.Plugin.Songs.Data.Songs[hash].diffs;   
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
            if (!SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                SortWasMissingData = true;
                return levelIds;
            }

            return levelIds
                .OrderByDescending(x => {
                    var hash = SongBrowserModel.GetSongHash(x.levelID);
                    if (SongDataCore.Plugin.Songs.Data.Songs.ContainsKey(hash))
                    {
                        return SongDataCore.Plugin.Songs.Data.Songs[hash].upVotes;
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
            return levelIds;
            // Do not always have data when trying to sort by UpVotes
            /*if (!SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                SortWasMissingData = true;
                return levelIds;
            }

            return levelIds
                .OrderByDescending(x => {
                    var hash = SongBrowserModel.GetSongHash(x.levelID);
                    if (SongDataCore.Plugin.Songs.Data.Songs.ContainsKey(hash))
                    {
                        return SongDataCore.Plugin.Songs.Data.Songs[hash].plays;
                    }
                    else
                    {
                        return int.MinValue;
                    }
                })
                .ToList();*/
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
            if (!SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                SortWasMissingData = true;
                return levelIds;
            }

            return levelIds
                .OrderByDescending(x => {
                    var hash = SongBrowserModel.GetSongHash(x.levelID);
                    if (SongDataCore.Plugin.Songs.Data.Songs.ContainsKey(hash))
                    {
                        return SongDataCore.Plugin.Songs.Data.Songs[hash].rating;
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
            if (!SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                SortWasMissingData = true;
                return levelIds;
            }

            return levelIds
                .OrderByDescending(x => {
                    var hash = SongBrowserModel.GetSongHash(x.levelID);
                    if (SongDataCore.Plugin.Songs.Data.Songs.ContainsKey(hash))
                    {
                        return SongDataCore.Plugin.Songs.Data.Songs[hash].heat;
                    }
                    else
                    {
                        return int.MinValue;
                    }
                })
                .ToList();
        }

        #region Song helpers
        /// <summary>
        /// Get the song hash from a levelID
        /// </summary>
        /// <param name="levelId"></param>
        /// <returns></returns>
        public static string GetSongHash(string levelId)
        {
            var split = levelId.Split('_');
            return split.Length > 2 ? split[2] : levelId;
        }
        #endregion
    }
}

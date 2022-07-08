using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IPA.Utilities;
using UnityEngine;
using CustomJSONData.CustomBeatmap;
using SongBrowser.Configuration;

namespace SongBrowser
{
    public class SongBrowserModel
    {
        public static readonly string FilteredSongsCollectionName = CustomLevelLoader.kCustomLevelPackPrefixId + "SongBrowser_FilteredSongPack";
        public static readonly string PlaylistSongsCollectionName = "SongBrowser_PlaylistPack";

        private readonly String CUSTOM_SONGS_DIR = Path.Combine("Beat Saber_Data", "CustomLevels");

        private readonly DateTime EPOCH = new DateTime(1970, 1, 1);

        // song list management
        private double _customSongDirLastWriteTime = 0;
        private readonly Dictionary<String, DateTime> _cachedLastWriteTimes;
        private Dictionary<string, int> _levelIdToPlayCount;
        private Dictionary<string, int> _cachedFileSystemOrder;

        public BeatmapCharacteristicSO CurrentBeatmapCharacteristicSO;

        public static Func<IAnnotatedBeatmapLevelCollection, List<IPreviewBeatmapLevel>> CustomFilterHandler;
        public static Func<List<IPreviewBeatmapLevel>, List<IPreviewBeatmapLevel>> CustomSortHandler;
        public static Action<ConcurrentDictionary<string, CustomPreviewBeatmapLevel>> didFinishProcessingSongs;

        public bool SortWasMissingData { get; set; } = false;
        public bool FilterWasMissingData { get; set; } = false;

        /// <summary>
        /// Get the last selected (stored in settings) level id.
        /// </summary>
        public String LastSelectedLevelId
        {
            get
            {
                return PluginConfig.Instance.CurrentLevelId;
            }

            set
            {
                PluginConfig.Instance.CurrentLevelId = value;
            }
        }

        public float LastScrollIndex;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SongBrowserModel()
        {
            _cachedLastWriteTimes = new Dictionary<String, DateTime>();
            _cachedFileSystemOrder = new Dictionary<string, int>();
            _levelIdToPlayCount = new Dictionary<string, int>();

            LastScrollIndex = 0;
        }

        /// <summary>
        /// Init this model.
        /// </summary>
        /// <param name="songSelectionMasterView"></param>
        /// <param name="songListViewController"></param>
        public void Init()
        {
            Plugin.Log.Info($"Settings loaded, filter/sorting mode is: " +
                $"{PluginConfig.Instance.GetFilterModeString()}/" +
                $"{PluginConfig.Instance.SortMode}");
        }

        /// <summary>
        /// Easy invert of toggling.
        /// </summary>
        public void ToggleInverting()
        {
            PluginConfig.Instance.InvertSortResults = !PluginConfig.Instance.InvertSortResults;
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
                Plugin.Log.Error("CustomSong directory is missing...");
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
                    DateTime lastWriteTime = GetSongUserDate(level.Value);
                    _cachedLastWriteTimes[level.Value.levelID] = lastWriteTime;
                }
            }

            lastWriteTimer.Stop();
            Plugin.Log.Info($"Determining song download time and determining mappings took {lastWriteTimer.ElapsedMilliseconds}ms");

            if (customSongDirChanged) {
	            Stopwatch fileSystemTimer = new Stopwatch();
	            fileSystemTimer.Start();
	            _cachedFileSystemOrder = SongCore.Loader.CustomLevels
			            .OrderBy(level => level.Value.customLevelPath)
			            .Select((level, index) => new { Identifier = level.Value.levelID, Index = index })
			            .ToDictionary(kvp => kvp.Identifier, kvp => kvp.Index);
	            fileSystemTimer.Stop();
	            Plugin.Log.Info($"Determining filesystem song order took {fileSystemTimer.ElapsedMilliseconds}ms");
            }

            // Update song Infos, directory tree, and sort
            this.UpdatePlayCounts();

            // Signal complete
            if (SongCore.Loader.CustomLevels.Count > 0)
            {
                didFinishProcessingSongs?.Invoke(SongCore.Loader.CustomLevels);
            }

            timer.Stop();

            Plugin.Log.Info($"Updating songs infos took {timer.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Song date is stored in SongMetadata config file to be preserved after moving files to a different location.
        /// Initial date is loaded from file date.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private DateTime GetSongUserDate(CustomPreviewBeatmapLevel level)
        {
            if (PluginConfig.Instance.ExperimentalScrapeSongMetaData)
            {
                try
                {
                    var metadata = SongMetadataStore.Instance.GetMetadataForLevelID(level.levelID);
                    if (metadata.AddedAt == null)
                    {
                        metadata.AddedAt = GetSongUserDateFromFilesystem(level).Value;
                    }

                    return metadata.AddedAt.Value;
                } 
                catch (Exception e)
                {
                    Plugin.Log.Critical($"Failure during song meta data scrape: {e}");
                    return DateTime.MinValue;
                }
            } 
            else
            {
                return GetSongUserDateFromFilesystem(level).Value;
            }
        }

        /// <summary>
        /// Try to get the date from the cover file, likely the most reliable.
        /// Fall back on the folders creation date.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private DateTime? GetSongUserDateFromFilesystem(CustomPreviewBeatmapLevel level)
        {
            var coverPath = Path.Combine(level.customLevelPath, level.standardLevelInfoSaveData.coverImageFilename);
            DateTime lastTime;
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

            return lastTime;
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
        public void ProcessSongList(IAnnotatedBeatmapLevelCollection selectedBeatmapCollection, LevelSelectionNavigationController navController)
        {
            Plugin.Log.Trace("ProcessSongList()");

            List<IPreviewBeatmapLevel> unsortedSongs;
            List<IPreviewBeatmapLevel> filteredSongs;
            List<IPreviewBeatmapLevel> sortedSongs;

            // Abort
            if (selectedBeatmapCollection == null)
            {
                Plugin.Log.Debug("Cannot process songs yet, no level collection selected...");
                return;
            }

            Plugin.Log.Debug($"Using songs from level collection: {selectedBeatmapCollection.collectionName} [num={selectedBeatmapCollection.beatmapLevelCollection.beatmapLevels.Count}");
            unsortedSongs = GetLevelsForLevelCollection(selectedBeatmapCollection).ToList();

            Plugin.Log.Debug($"Start filtering songs");

            Stopwatch stopwatch = Stopwatch.StartNew();

            filteredSongs = unsortedSongs;
            foreach (var kvp in PluginConfig.Instance.FilterModes)
            {
                if (kvp.Value == SortFilterStates.Disabled)
                {
                    continue;
                }

                Plugin.Log.Info($"Filtering by {kvp.Key}");

                FilterWasMissingData = false;

                switch ((SongFilterMode)Enum.Parse(typeof(SongFilterMode), kvp.Key.ToString()))
                {
                    case SongFilterMode.Easy:
                    case SongFilterMode.Normal:
                    case SongFilterMode.Hard:
                    case SongFilterMode.Expert:
                    case SongFilterMode.ExpertPlus:
                        filteredSongs = FilterDifficulty(filteredSongs, (BeatmapDifficulty)Enum.Parse(typeof(BeatmapDifficulty), kvp.Key.ToString()));
                        break;
                    case SongFilterMode.Favorites:
                        filteredSongs = FilterFavorites(filteredSongs);
                        break;
                    case SongFilterMode.Search:
                        filteredSongs = FilterSearch(filteredSongs);
                        break;
                    case SongFilterMode.Ranked:
                        filteredSongs = FilterRanked(filteredSongs, true, false);
                        break;
                    case SongFilterMode.Unranked:
                        filteredSongs = FilterRanked(filteredSongs, false, true);
                        break;
                    case SongFilterMode.Played:
                        filteredSongs = FilterPlayed(filteredSongs, true, false);
                        break;
                    case SongFilterMode.Unplayed:
                        filteredSongs = FilterPlayed(filteredSongs, false, true);
                        break;
                    case SongFilterMode.Requirements:
                        filteredSongs = FilterRequirements(filteredSongs);
                        break;
                    case SongFilterMode.Custom:
                        Plugin.Log.Info("Song filter mode set to custom. Deferring filter behaviour to another mod.");
                        filteredSongs = CustomFilterHandler != null ? CustomFilterHandler.Invoke(selectedBeatmapCollection) : filteredSongs;
                        break;
                    case SongFilterMode.None:
                    default:
                        Plugin.Log.Info("No song filter selected...");
                        break;
                }

                stopwatch.Stop();
            }

            Plugin.Log.Info($"Filtering songs took {stopwatch.ElapsedMilliseconds}ms");

            // sort
            Plugin.Log.Debug($"Starting to sort songs by {PluginConfig.Instance.SortMode}");
            stopwatch = Stopwatch.StartNew();

            SortWasMissingData = false;

            switch (PluginConfig.Instance.SortMode)
            {
                case SongSortMode.Original:
                    sortedSongs = SortOriginal(filteredSongs);
                    break;
                case SongSortMode.Newest:
                    sortedSongs = SortNewest(filteredSongs);
                    break;
                case SongSortMode.LastPlayed:
                    sortedSongs = SortLastPlayed(filteredSongs);
                    break;
                case SongSortMode.Author:
                    sortedSongs = SortAuthor(filteredSongs);
                    break;
                case SongSortMode.Mapper:
                    sortedSongs = SortMapper(filteredSongs);
                    break;
	            case SongSortMode.Vanilla:
		            sortedSongs = SortVanilla(filteredSongs);
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
                case SongSortMode.Bpm:
                    sortedSongs = SortSongBpm(filteredSongs);
                    break;
                case SongSortMode.Length:
                    sortedSongs = SortSongLength(filteredSongs);
                    break;
                case SongSortMode.Custom:
                    sortedSongs = CustomSortHandler != null ? CustomSortHandler.Invoke(filteredSongs) : filteredSongs;
                    break;
                case SongSortMode.Default:
                default:
                    sortedSongs = SortSongName(filteredSongs);
                    break;
            }

            if (PluginConfig.Instance.InvertSortResults && PluginConfig.Instance.SortMode != SongSortMode.Random && PluginConfig.Instance.SortMode != SongSortMode.Stars)
            {
                sortedSongs.Reverse();
            }

            stopwatch.Stop();
            Plugin.Log.Info($"Sorting songs took {stopwatch.ElapsedMilliseconds}ms");

            // Still hacking in a custom level pack
            // Asterisk the pack name so it is identifable as filtered.
            var packName = selectedBeatmapCollection.collectionName;
            if (packName == null)
            {
                packName = "";
            }

            if (!packName.EndsWith("*") && PluginConfig.Instance.FilterModes.Any(x => x.Value == SortFilterStates.Enabled))
            {
                packName += "*";
            }

            // Some level categories have a null cover image, supply something, it won't show it anyway
            var coverImage = selectedBeatmapCollection.coverImage;
            if (coverImage == null)
            {
                coverImage = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;
            }

            var smallCoverImage = selectedBeatmapCollection.smallCoverImage;
            if (smallCoverImage == null)
            {
                smallCoverImage = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;
            }

            Plugin.Log.Debug("Creating filtered level pack...");
            BeatmapLevelPack levelPack = new BeatmapLevelPack(SongBrowserModel.FilteredSongsCollectionName, packName, selectedBeatmapCollection.collectionName, coverImage, smallCoverImage, new BeatmapLevelCollection(sortedSongs.ToArray()));

            Plugin.Log.Debug("Acquiring necessary fields to call SetData(pack)...");
            LevelCollectionNavigationController lcnvc = navController.GetField<LevelCollectionNavigationController, LevelSelectionNavigationController>("_levelCollectionNavigationController");
            LevelFilteringNavigationController lfnc = navController.GetField<LevelFilteringNavigationController, LevelSelectionNavigationController>("_levelFilteringNavigationController");
            var _hidePracticeButton = navController.GetField<bool, LevelSelectionNavigationController>("_hidePracticeButton");
            var _actionButtonText = navController.GetField<string, LevelSelectionNavigationController>("_actionButtonText");
            var _allowedBeatmapDifficultyMask = navController.GetField<BeatmapDifficultyMask, LevelSelectionNavigationController>("_allowedBeatmapDifficultyMask");
            var _notAllowedCharacteristics = navController.GetField<BeatmapCharacteristicSO[], LevelSelectionNavigationController>("_notAllowedCharacteristics");
            var noDataPrefab = lfnc.GetField<GameObject, LevelFilteringNavigationController>("_currentNoDataInfoPrefab");

            //Plugin.Log.Debug("Calling lcnvc.SetData...");

            lcnvc.SetData(levelPack,
                true,
                !_hidePracticeButton,
                _actionButtonText,
                noDataPrefab,
                _allowedBeatmapDifficultyMask,
                _notAllowedCharacteristics);

            //_sortedSongs.ForEach(x => Plugin.Log.Debug(x.levelID));
        }

        /// <summary>
        /// Filter songs based on playerdata favorites.
        /// </summary>
        private List<IPreviewBeatmapLevel> FilterFavorites(List<IPreviewBeatmapLevel> levels)
        {
            Plugin.Log.Info("Filtering song list as favorites playlist...");

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
            if (PluginConfig.Instance.SearchTerms.Count <= 0)
            {
                Plugin.Log.Error("Tried to search for a song with no valid search terms...");
                SortSongName(levels);
                return levels;
            }
            string searchTerm = PluginConfig.Instance.SearchTerms[0];
            if (String.IsNullOrEmpty(searchTerm))
            {
                Plugin.Log.Error("Empty search term entered.");
                SortSongName(levels);
                return levels;
            }

            Plugin.Log.Info($"Filtering song list by search term: {searchTerm}");

            var terms = searchTerm.Split(' ');
            foreach (var term in terms)
            {
                levels = levels.Intersect(
                    levels
                        .Where(x => {
                            var hash = SongBrowserModel.GetSongHash(x.levelID);
                            var songKey = "";
                            if (SongDataCore.Plugin.Songs.Data.Songs.ContainsKey(hash))
                            {
                                songKey = SongDataCore.Plugin.Songs.Data.Songs[hash].key;
                            }
                            return $"{songKey} {x.songName} {x.songSubName} {x.songAuthorName} {x.levelAuthorName}".ToLower().Contains(term.ToLower());
                        })
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
            List<IPreviewBeatmapLevel> filteredLevels = null;
            if (SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                filteredLevels = levels.Where(x =>
                {
                    if (!SongDataCore.Plugin.Songs.IsDataAvailable())
                    {
                        return false;
                    }

                    if (x == null)
                    {
                        return false;
                    }

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
            else
            {
                filteredLevels = new List<IPreviewBeatmapLevel>();
                FilterWasMissingData = true;
            }

            if (filteredLevels.Count == 0)
            {
                Plugin.Log.Info("No ranked songs found after filtering.");
            }

            return filteredLevels;
        }

        /// <summary>
        /// Filter songs based on mods requirements.
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> FilterRequirements(List<IPreviewBeatmapLevel> levels)
        {
            if (!Plugin.IsCustomJsonDataEnabled)
            {
                return levels.ToList();
            }

            return levels.Where(x =>
            {
                if (x is CustomPreviewBeatmapLevel customLevel)
                {
                    var saveData = customLevel.standardLevelInfoSaveData as CustomLevelInfoSaveData;

                    foreach (CustomLevelInfoSaveData.DifficultyBeatmapSet difficulties in saveData.difficultyBeatmapSets)
                    {
                        var hasRequirements = difficulties.difficultyBeatmaps.Any(d =>
                        {
                            if (!(d is CustomLevelInfoSaveData.DifficultyBeatmap difficulty))
                            {
                                return false;
                            }

                            if (difficulty.customData.ContainsKey("_requirements"))
                            {
                                return ((IList<object>)difficulty.customData["_requirements"]).Count > 0;
                            }

                            return false;
                        });

                        if (hasRequirements)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }).ToList();
        }
        
        /// <summary>
        /// Filter songs based on played or unplayed status.
        /// </summary>
        /// <param name="levels"></param>
        /// <param name="includePlayed"></param>
        /// <param name="includeUnplayed"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> FilterPlayed(List<IPreviewBeatmapLevel> levels, bool includePlayed, bool includeUnplayed)
        {
            var filteredLevels = levels.Where(x =>
            {
                if (x == null)
                {
                    return false;
                }
                
                var playCount = _levelIdToPlayCount.ContainsKey(x.levelID) ? _levelIdToPlayCount[x.levelID] : 0;

                if (playCount > 0)
                {
                    return includePlayed;
                }
                else
                {
                    return includeUnplayed;
                }
            }).ToList();

            if (filteredLevels.Count == 0)
            {
                Plugin.Log.Info("No played songs found after filtering.");
            }

            return filteredLevels;
        }
        /// <summary>
        /// Filter songs based on a difficulty.
        /// </summary>
        /// <param name="levels"></param>
        /// <param name="beatmapDifficulty"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> FilterDifficulty(List<IPreviewBeatmapLevel> levels, BeatmapDifficulty beatmapDifficulty)
        {
            return levels.Where(x => x.previewDifficultyBeatmapSets.Any(y => y.beatmapDifficulties.Any(z => z.Equals(beatmapDifficulty)))).ToList();
        }

        /// <summary>
        /// Sorting returns original list.
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortOriginal(List<IPreviewBeatmapLevel> levels)
        {
            Plugin.Log.Info("Sorting song list as original");
            return levels;
        }
        
        /// <summary>
        /// Sorting returns list sorted by alphabetical order of the directories they are contained in.
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortVanilla(List<IPreviewBeatmapLevel> levels)
        {
	        Plugin.Log.Info("Sorting song list by vanilla ordering.");
	        return levels
			        .OrderBy(level => _cachedFileSystemOrder.TryGetValue(level.levelID, out int index) ? index : int.MaxValue)
			        .ToList();
        }

        /// <summary>
        /// Sorting by newest (file time, creation+modified).
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortNewest(List<IPreviewBeatmapLevel> levels)
        {
            Plugin.Log.Info("Sorting song list as newest.");
            return levels
                .OrderByDescending(x => _cachedLastWriteTimes.ContainsKey(x.levelID) ? _cachedLastWriteTimes[x.levelID] : DateTime.MinValue)
                .ToList();
        }

        /// <summary>
        /// Sorting by newest (file time, creation+modified).
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortLastPlayed(List<IPreviewBeatmapLevel> levels)
        {
            Plugin.Log.Info("Sorting song list by last played.");
            return levels
                .OrderByDescending(x =>
                    SongMetadataStore.Instance.GetMetadataForLevelID(x.levelID).LastPlayed != null
                    ? SongMetadataStore.Instance.GetMetadataForLevelID(x.levelID).LastPlayed
                    : DateTime.MinValue)
                .ToList();
        }


        /// <summary>
        /// Sorting by the song author.
        /// </summary>
        /// <param name="levelIds"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortAuthor(List<IPreviewBeatmapLevel> levelIds)
        {
            Plugin.Log.Info("Sorting song list by author");
            return levelIds
                .OrderBy(x => x.songAuthorName)
                .ThenBy(x => x.songName)
                .ToList();
        }

        /// <summary>
        /// Sorting by the level author (mapper).
        /// </summary>
        /// <param name="levelIds"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortMapper(List<IPreviewBeatmapLevel> levelIds)
        {
            Plugin.Log.Info("Sorting song list by mapper");
            return levelIds
                .OrderBy(x => x.levelAuthorName)
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
            Plugin.Log.Info("Sorting song list by playcount");
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
            Plugin.Log.Info("Sorting song list by performance points...");

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
            Plugin.Log.Info("Sorting song list by star points...");

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
                        stars = diffs.Max(y => (PluginConfig.Instance.InvertSortResults)
                            ? -y.star
                            : y.star);
                    }

                    //Plugin.Log.Debug("Stars={stars}");
                    if (stars != 0)
                    {
                        return stars;
                    }

                    return double.MinValue;
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
            Plugin.Log.Info($"Sorting song list by random (seed={PluginConfig.Instance.RandomSongSeed})...");

            System.Random rnd = new System.Random(PluginConfig.Instance.RandomSongSeed);

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
            Plugin.Log.Info("Sorting song list as default (songName)");
            return levels
                .OrderBy(x => x.songName)
                .ThenBy(x => x.songAuthorName)
                .ToList();
        }

        /// <summary>
        /// Sorting by the song name.
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortSongBpm(List<IPreviewBeatmapLevel> levels)
        {
            Plugin.Log.Info("Sorting song list by beatsPerMinute/songName");
            return levels
                .OrderBy(x => x.beatsPerMinute)
                .ThenBy(x => x.songName)
                .ToList();
        }

        /// <summary>
        /// Sorting by the song name.
        /// </summary>
        /// <param name="levels"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortSongLength(List<IPreviewBeatmapLevel> levels)
        {
            Plugin.Log.Info("Sorting song list by songDuration/songName");
            return levels
                .OrderBy(x => x.songDuration)
                .ThenBy(x => x.songName)
                .ToList();
        }

        /// <summary>
        /// Sorting by BeatSaver UpVotes.
        /// </summary>
        /// <param name="levelIds"></param>
        /// <returns></returns>
        private List<IPreviewBeatmapLevel> SortUpVotes(List<IPreviewBeatmapLevel> levelIds)
        {
            Plugin.Log.Info("Sorting song list by BeatSaver UpVotes");

            // Do not always have data when trying to sort by UpVotes
            if (!SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                SortWasMissingData = true;
                return levelIds;
            }

            return levelIds
                .OrderByDescending(x =>
                {
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
            Plugin.Log.Info("Sorting song list by BeatSaver PlayCount");
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
            Plugin.Log.Info("Sorting song list by BeatSaver Rating!");

            // Do not always have data when trying to sort by rating
            if (!SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                SortWasMissingData = true;
                return levelIds;
            }

            return levelIds
                .OrderByDescending(x =>
                {
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
            Plugin.Log.Info("Sorting song list by BeatSaver Heat!");

            // Do not always have data when trying to sort by heat
            if (!SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                SortWasMissingData = true;
                return levelIds;
            }

            return levelIds
                .OrderByDescending(x =>
                {
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

        public static IReadOnlyList<IPreviewBeatmapLevel> GetLevelsForLevelCollection(IAnnotatedBeatmapLevelCollection levelCollection)
        {
            if (levelCollection is BeatSaberPlaylistsLib.Legacy.LegacyPlaylist legacyPlaylist)
            {
                return legacyPlaylist.BeatmapLevels;
            }
            if (levelCollection is BeatSaberPlaylistsLib.Blist.BlistPlaylist blistPlaylist)
            {
                return blistPlaylist.BeatmapLevels;
            }
            return levelCollection.beatmapLevelCollection.beatmapLevels;
        }
        #endregion
    }
}

using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.UI;
using BeatSaberPlaylistsLib.Types;

namespace SongBrowser.DataAccess
{
    public class BeatSaberUIController
    {
        // Beat Saber UI Elements
        public LevelSelectionFlowCoordinator LevelSelectionFlowCoordinator;
        public LevelSelectionNavigationController LevelSelectionNavigationController;

        public LevelFilteringNavigationController LevelFilteringNavigationController;
        public LevelCollectionNavigationController LevelCollectionNavigationController;
        public LevelSearchViewController LevelSearchViewController;

        public LevelCollectionViewController LevelCollectionViewController;
        public LevelCollectionTableView LevelCollectionTableView;
        public StandardLevelDetailViewController LevelDetailViewController;
        public StandardLevelDetailView StandardLevelDetailView;

        public BeatmapDifficultySegmentedControlController LevelDifficultyViewController;
        public BeatmapCharacteristicSegmentedControlController BeatmapCharacteristicSelectionViewController;

        public AnnotatedBeatmapLevelCollectionsViewController AnnotatedBeatmapLevelCollectionsViewController;

        public RectTransform LevelCollectionTableViewTransform;

        public Button TableViewPageUpButton;
        public Button TableViewPageDownButton;

        public RectTransform ActionButtons;

        public ScreenSystem ScreenSystem;

        public SimpleDialogPromptViewController SimpleDialogPromptViewControllerPrefab;

        /// <summary>
        /// Internal BeatSaber song model
        /// </summary>
        public BeatmapLevelsModel BeatmapLevelsModel;

        // Plugin Compat checks
        private bool _detectedTwitchPluginQueue = false;
        private bool _checkedForTwitchPlugin = false;

        /// <summary>
        /// Constructor.  Acquire all necessary BeatSaberUi elements.
        /// </summary>
        /// <param name="flowCoordinator"></param>
        public BeatSaberUIController(LevelSelectionFlowCoordinator flowCoordinator)
        {
            Plugin.Log.Debug("Collecting all BeatSaberUI Elements...");

            LevelSelectionFlowCoordinator = flowCoordinator;

            // gather flow coordinator elements
            LevelSelectionNavigationController = LevelSelectionFlowCoordinator.GetField<LevelSelectionNavigationController, LevelSelectionFlowCoordinator>("levelSelectionNavigationController");
            Plugin.Log.Debug($"Acquired LevelSelectionNavigationController [{LevelSelectionNavigationController.GetInstanceID()}]");

            LevelFilteringNavigationController = LevelSelectionNavigationController.GetField<LevelFilteringNavigationController, LevelSelectionNavigationController>("_levelFilteringNavigationController");
            Plugin.Log.Debug($"Acquired LevelFilteringNavigationController [{LevelFilteringNavigationController.GetInstanceID()}]");

            LevelSearchViewController = LevelFilteringNavigationController.GetField<LevelSearchViewController, LevelFilteringNavigationController>("_levelSearchViewController");
            Plugin.Log.Debug($"Acquired LevelSearchViewController [{LevelSearchViewController.GetInstanceID()}]");

            LevelCollectionNavigationController = LevelSelectionNavigationController.GetField<LevelCollectionNavigationController, LevelSelectionNavigationController>("_levelCollectionNavigationController");
            Plugin.Log.Debug($"Acquired LevelCollectionNavigationController [{LevelCollectionNavigationController.GetInstanceID()}]");

            LevelCollectionViewController = LevelCollectionNavigationController.GetField<LevelCollectionViewController, LevelCollectionNavigationController>("_levelCollectionViewController");
            Plugin.Log.Debug($"Acquired LevelPackLevelsViewController [{LevelCollectionViewController.GetInstanceID()}]");

            LevelDetailViewController = LevelCollectionNavigationController.GetField<StandardLevelDetailViewController, LevelCollectionNavigationController>("_levelDetailViewController");
            Plugin.Log.Debug($"Acquired StandardLevelDetailViewController [{LevelDetailViewController.GetInstanceID()}]");

            LevelCollectionTableView = this.LevelCollectionViewController.GetField<LevelCollectionTableView, LevelCollectionViewController>("_levelCollectionTableView");
            Plugin.Log.Debug($"Acquired LevelPackLevelsTableView [{LevelCollectionTableView.GetInstanceID()}]");

            StandardLevelDetailView = LevelDetailViewController.GetField<StandardLevelDetailView, StandardLevelDetailViewController>("_standardLevelDetailView");
            Plugin.Log.Debug($"Acquired StandardLevelDetailView [{StandardLevelDetailView.GetInstanceID()}]");

            BeatmapCharacteristicSelectionViewController = StandardLevelDetailView.GetField<BeatmapCharacteristicSegmentedControlController, StandardLevelDetailView>("_beatmapCharacteristicSegmentedControlController");
            Plugin.Log.Debug($"Acquired BeatmapCharacteristicSegmentedControlController [{BeatmapCharacteristicSelectionViewController.GetInstanceID()}]");

            LevelDifficultyViewController = StandardLevelDetailView.GetField<BeatmapDifficultySegmentedControlController, StandardLevelDetailView>("_beatmapDifficultySegmentedControlController");
            Plugin.Log.Debug($"Acquired BeatmapDifficultySegmentedControlController [{LevelDifficultyViewController.GetInstanceID()}]");

            LevelCollectionTableViewTransform = LevelCollectionTableView.transform as RectTransform;
            Plugin.Log.Debug($"Acquired TableViewRectTransform from LevelPackLevelsTableView [{LevelCollectionTableViewTransform.GetInstanceID()}]");

            AnnotatedBeatmapLevelCollectionsViewController = LevelFilteringNavigationController.GetField<AnnotatedBeatmapLevelCollectionsViewController, LevelFilteringNavigationController>("_annotatedBeatmapLevelCollectionsViewController");
            Plugin.Log.Debug($"Acquired AnnotatedBeatmapLevelCollectionsViewController from LevelFilteringNavigationController [{AnnotatedBeatmapLevelCollectionsViewController.GetInstanceID()}]");

            TableView tableView = LevelCollectionTableView.GetField<TableView, LevelCollectionTableView>("_tableView");
            ScrollView scrollView = tableView.GetField<ScrollView, TableView>("_scrollView");
            TableViewPageUpButton = scrollView.GetField<Button, ScrollView>("_pageUpButton");
            TableViewPageDownButton = scrollView.GetField<Button, ScrollView>("_pageDownButton");
            Plugin.Log.Debug("Acquired Page Up and Down buttons...");

            ActionButtons = StandardLevelDetailView.GetComponentsInChildren<RectTransform>().First(x => x.name == "ActionButtons");
            Plugin.Log.Debug($"Acquired ActionButtons [{ActionButtons.GetInstanceID()}]");

            ScreenSystem = Resources.FindObjectsOfTypeAll<ScreenSystem>().Last();
            Plugin.Log.Debug($"Acquired ScreenSystem [{ScreenSystem.GetInstanceID()}]");

            SimpleDialogPromptViewControllerPrefab = Resources.FindObjectsOfTypeAll<SimpleDialogPromptViewController>().Last();
            Plugin.Log.Debug($"Acquired SimpleDialogPromptViewControllerPrefab [{SimpleDialogPromptViewControllerPrefab.GetInstanceID()}]");

            BeatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().Last();
            Plugin.Log.Debug($"Acquired BeatmapLevelsModel [{BeatmapLevelsModel}]");
        }

        /// <summary>
        /// Get the currently selected level pack within the LevelPackLevelViewController hierarchy.
        /// </summary>
        /// <returns></returns>
        private IBeatmapLevelPack GetCurrentSelectedLevelPack()
        {
            if (LevelCollectionNavigationController == null)
            {
                return null;
            }

            var pack = LevelCollectionNavigationController.GetField<IBeatmapLevelPack, LevelCollectionNavigationController>("_levelPack");

            return pack;
        }

        /// <summary>
        /// Helper to get either or playlist or 
        /// </summary>
        /// <returns></returns>
        public IAnnotatedBeatmapLevelCollection GetCurrentSelectedAnnotatedBeatmapLevelCollection()
        {
            IAnnotatedBeatmapLevelCollection collection = GetCurrentSelectedLevelPack();

            if (collection == null)
            {
                LevelSearchViewController.BeatmapLevelPackCollection filterCollection = LevelSearchViewController.GetField<LevelSearchViewController.BeatmapLevelPackCollection, LevelSearchViewController>("_beatmapLevelPackCollection");
                return filterCollection;
            }

            if (collection == null)
            {
                collection = GetCurrentSelectedPlaylist();
            }

            return collection;
        }

        /// <summary>
        /// Get the currently selected level collection from playlists.
        /// </summary>
        /// <returns></returns>
        private IPlaylist GetCurrentSelectedPlaylist()
        {
            if (AnnotatedBeatmapLevelCollectionsViewController == null)
            {
                return null;
            }

            IPlaylist playlist = AnnotatedBeatmapLevelCollectionsViewController.selectedAnnotatedBeatmapLevelCollection as IPlaylist;
            return playlist;
        }

        /// <summary>
        /// Get level collection by level collection name.
        /// </summary>
        /// <param name="levelCollectionName"></param>
        /// <returns></returns>
        public IAnnotatedBeatmapLevelCollection GetLevelCollectionByName(String levelCollectionName)
        {
            IAnnotatedBeatmapLevelCollection levelCollection = null;

            // search level packs
            BeatmapLevelPackCollectionSO beatMapLevelPackCollection = Resources.FindObjectsOfTypeAll<BeatmapLevelPackCollectionSO>().Last();
            IBeatmapLevelPack[] levelPacks = beatMapLevelPackCollection.GetField<IBeatmapLevelPack[], BeatmapLevelPackCollectionSO>("_allBeatmapLevelPacks");
            foreach (IBeatmapLevelPack o in levelPacks)
            {
                if (String.Equals(o.collectionName, levelCollectionName))
                {
                    levelCollection = o;
                    break;
                }
            }

            // search playlists
            if (levelCollection == null)
            {
                IReadOnlyList<IAnnotatedBeatmapLevelCollection> _annotatedBeatmapLevelCollections = AnnotatedBeatmapLevelCollectionsViewController.GetField<IReadOnlyList<IAnnotatedBeatmapLevelCollection>, AnnotatedBeatmapLevelCollectionsViewController>("_annotatedBeatmapLevelCollections");
                foreach (IAnnotatedBeatmapLevelCollection c in _annotatedBeatmapLevelCollections)
                {
                    if (String.Equals(c.collectionName, levelCollectionName))
                    {
                        levelCollection = c;
                        break;
                    }
                }
            }

            return levelCollection;
        }

        /// <summary>
        /// Get Current levels from current level collection.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<IPreviewBeatmapLevel> GetCurrentLevelCollectionLevels()
        {
            var levelCollection = GetCurrentSelectedAnnotatedBeatmapLevelCollection();
            if (levelCollection == null)
            {
                Plugin.Log.Debug("Current selected level collection is null for some reason...");
                return null;
            }
            return SongBrowserModel.GetLevelsForLevelCollection(levelCollection);
        }

        public bool SelectLevelCategory(String levelCategoryName)
        {
            Plugin.Log.Trace($"SelectLevelCategory({levelCategoryName})");

            try
            {
                if (String.IsNullOrEmpty(levelCategoryName))
                {
                    // hack for now, just assume custom levels if a user has an old settings file, corrects itself first time they change level packs.
                    levelCategoryName = SelectLevelCategoryViewController.LevelCategory.CustomSongs.ToString();
                }

                SelectLevelCategoryViewController.LevelCategory category;
                try
                {
                    category = (SelectLevelCategoryViewController.LevelCategory)Enum.Parse(typeof(SelectLevelCategoryViewController.LevelCategory), levelCategoryName, true);
                }
                catch (Exception)
                {
                    // invalid input
                    return false;
                }

                Plugin.Log.Info($"Selecting level category: {levelCategoryName}");

                var selectLeveCategoryViewController = LevelFilteringNavigationController.GetComponentInChildren<SelectLevelCategoryViewController>();
                var iconSegementController = selectLeveCategoryViewController.GetComponentInChildren<IconSegmentedControl>();

                int selectCellNumber = (from x in selectLeveCategoryViewController.GetField<SelectLevelCategoryViewController.LevelCategoryInfo[], SelectLevelCategoryViewController>("_levelCategoryInfos")
                                        select x.levelCategory).ToList().IndexOf(category);

                iconSegementController.SelectCellWithNumber(selectCellNumber);
                selectLeveCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell(iconSegementController, selectCellNumber);
                LevelFilteringNavigationController.UpdateSecondChildControllerContent(category);
                LevelSearchViewController.ResetCurrentFilterParams();
                //AnnotatedBeatmapLevelCollectionsViewController.RefreshAvailability();

                Plugin.Log.Debug("Done selecting level category.");

                return true;

            } catch (Exception e)
            {
                Plugin.Log.Critical(e);
            }

            return false;
        }

        /// <summary>
        /// Select a level collection.
        /// </summary>
        /// <param name="levelCollectionName"></param>
        public void SelectLevelCollection(String levelCollectionName)
        {
            Plugin.Log.Trace($"SelectLevelCollection({levelCollectionName})");

            try
            {
                IAnnotatedBeatmapLevelCollection collection = GetLevelCollectionByName(levelCollectionName);
                if (collection == null)
                {
                    Plugin.Log.Debug("Could not locate requested level collection...");
                    return;
                }

                Plugin.Log.Info($"Selecting level collection: {collection.collectionName}");

                LevelFilteringNavigationController.SelectAnnotatedBeatmapLevelCollection(collection as IBeatmapLevelPack);
                LevelFilteringNavigationController.HandleAnnotatedBeatmapLevelCollectionsViewControllerDidSelectAnnotatedBeatmapLevelCollection(collection);

                Plugin.Log.Debug("Done selecting level collection!");
            }
            catch (Exception e)
            {
                Plugin.Log.Critical(e);
            }
        }

        /// <summary>
        /// Scroll TableView to proper row, fire events.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="levelID"></param>
        public void SelectAndScrollToLevel(string levelID)
        {
            Plugin.Log.Debug($"Scrolling to LevelID: {levelID}");

            // Check once per load
            if (!_checkedForTwitchPlugin)
            {
                Plugin.Log.Info("Checking for BeatSaber Twitch Integration Plugin...");
                _detectedTwitchPluginQueue = Resources.FindObjectsOfTypeAll<HMUI.ViewController>().Any(x => x.name == "RequestInfo");
                Plugin.Log.Info("BeatSaber Twitch Integration plugin detected: " + _detectedTwitchPluginQueue);

                _checkedForTwitchPlugin = true;
            }

            // Skip scrolling to level if twitch plugin has queue active.
            if (_detectedTwitchPluginQueue)
            {
                Plugin.Log.Debug("Skipping SelectAndScrollToLevel() because we detected Twitch Integration Plugin has a Queue active...");
                return;
            }

            // try to find the index and scroll to it
            int selectedIndex = 0;
            List<IPreviewBeatmapLevel> levels = GetCurrentLevelCollectionLevels().ToList();
            if (levels.Count <= 0)
            {
                return;
            }

            // acquire the index or try the last row
            selectedIndex = levels.FindIndex(x => x.levelID == levelID);
            if (selectedIndex < 0)
            {
                // this might look like an off by one error but the _level list we keep is missing the header entry BeatSaber.
                // so the last row is +1 the max index, the count.
                int maxCount = levels.Count;

                int selectedRow = LevelCollectionTableView.GetField<int, LevelCollectionTableView>("_selectedRow");

                Plugin.Log.Debug($"Song is not in the level pack, cannot scroll to it...  Using last known row {selectedRow}/{maxCount}");
                selectedIndex = Math.Min(maxCount, selectedRow);
            }
            else if (LevelCollectionViewController.GetField<bool, LevelCollectionViewController>("_showHeader"))
            {
                // the header counts as an index, so if the index came from the level array we have to add 1.
                selectedIndex += 1;
            }

            ScrollToLevelByRow(selectedIndex);
        }

        /// <summary>
        /// Scroll to a level by Row
        /// </summary>
        /// <param name="selectedIndex"></param>
        public void ScrollToLevelByRow(int selectedIndex)
        {
            Plugin.Log.Trace($"ScrollToLevelByRow: {selectedIndex}");

            TableView tableView = LevelCollectionTableView.GetField<TableView, LevelCollectionTableView>("_tableView");
            var selectedRow = LevelCollectionTableView.GetField<int, LevelCollectionTableView>("_selectedRow");
            if (selectedRow != selectedIndex && LevelCollectionTableView.isActiveAndEnabled)
            {
                LevelCollectionTableView.HandleDidSelectRowEvent(tableView, selectedIndex);
            }
            tableView.ScrollToCellWithIdx(selectedIndex, TableView.ScrollPositionType.Beginning, true);
            tableView.SelectCellWithIdx(selectedIndex);
        }

        /// <summary>
        /// Try to refresh the song list.  Broken for now.
        /// </summary>
        public void RefreshSongList(string currentSelectedLevelId, bool scrollToLevel = true)
        {
            Plugin.Log.Info("Refreshing the song list view.");
            try
            {
                var levels = GetCurrentLevelCollectionLevels();
                if (levels == null)
                {
                    Plugin.Log.Info("Nothing to refresh yet.");
                    return;
                }

                TableView tableView = LevelCollectionTableView.GetField<TableView, LevelCollectionTableView>("_tableView");
                bool tableViewInit = tableView.GetField<bool, TableView>("_isInitialized");

                Plugin.Log.Debug("Reloading SongList TableView");
                tableView.ReloadData();

                String selectedLevelID = currentSelectedLevelId;
                if (!String.IsNullOrEmpty(currentSelectedLevelId))
                {
                    selectedLevelID = currentSelectedLevelId;
                }
                else
                {
                    if (levels.Count > 0)
                    {
                        Plugin.Log.Debug("Currently selected level ID does not exist, picking the first...");
                        selectedLevelID = levels.FirstOrDefault().levelID;
                    }
                }

                if (scrollToLevel)
                {
                    SelectAndScrollToLevel(selectedLevelID);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.Critical($"Exception refreshing song list: {e}");
            }
        }
    }
}

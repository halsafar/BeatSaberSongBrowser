using BS_Utils.Utilities;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser.DataAccess
{
    public class BeatSaberUIController
    {
        // Beat Saber UI Elements
        public FlowCoordinator LevelSelectionFlowCoordinator;
        public LevelSelectionNavigationController LevelSelectionNavigationController;

        public LevelFilteringNavigationController LevelFilteringNavigationController;
        public LevelCollectionNavigationController LevelCollectionNavigationController;

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
        public BeatSaberUIController(FlowCoordinator flowCoordinator)
        {
            Logger.Debug("Collecting all BeatSaberUI Elements...");

            LevelSelectionFlowCoordinator = flowCoordinator;

            // gather flow coordinator elements
            LevelSelectionNavigationController = LevelSelectionFlowCoordinator.GetPrivateField<LevelSelectionNavigationController>("levelSelectionNavigationController");
            Logger.Debug("Acquired LevelSelectionNavigationController [{0}]", LevelSelectionNavigationController.GetInstanceID());

            LevelFilteringNavigationController = LevelSelectionNavigationController.GetPrivateField<LevelFilteringNavigationController>("_levelFilteringNavigationController");
            Logger.Debug("Acquired LevelFilteringNavigationController [{0}]", LevelFilteringNavigationController.GetInstanceID());

            LevelCollectionNavigationController = LevelSelectionNavigationController.GetPrivateField<LevelCollectionNavigationController>("_levelCollectionNavigationController");
            Logger.Debug("Acquired LevelCollectionNavigationController [{0}]", LevelCollectionNavigationController.GetInstanceID());

            LevelCollectionViewController = LevelCollectionNavigationController.GetPrivateField<LevelCollectionViewController>("_levelCollectionViewController");
            Logger.Debug("Acquired LevelPackLevelsViewController [{0}]", LevelCollectionViewController.GetInstanceID());

            LevelDetailViewController = LevelCollectionNavigationController.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController");
            Logger.Debug("Acquired StandardLevelDetailViewController [{0}]", LevelDetailViewController.GetInstanceID());

            LevelCollectionTableView = this.LevelCollectionViewController.GetPrivateField<LevelCollectionTableView>("_levelCollectionTableView");
            Logger.Debug("Acquired LevelPackLevelsTableView [{0}]", LevelCollectionTableView.GetInstanceID());

            StandardLevelDetailView = LevelDetailViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView");
            Logger.Debug("Acquired StandardLevelDetailView [{0}]", StandardLevelDetailView.GetInstanceID());

            BeatmapCharacteristicSelectionViewController = LevelDetailViewController.GetComponentInChildren<BeatmapCharacteristicSegmentedControlController>();
            Logger.Debug("Acquired BeatmapCharacteristicSegmentedControlController [{0}]", BeatmapCharacteristicSelectionViewController.GetInstanceID());

            LevelDifficultyViewController = StandardLevelDetailView.GetPrivateField<BeatmapDifficultySegmentedControlController>("_beatmapDifficultySegmentedControlController");
            Logger.Debug("Acquired BeatmapDifficultySegmentedControlController [{0}]", LevelDifficultyViewController.GetInstanceID());

            LevelCollectionTableViewTransform = LevelCollectionTableView.transform as RectTransform;
            Logger.Debug("Acquired TableViewRectTransform from LevelPackLevelsTableView [{0}]", LevelCollectionTableViewTransform.GetInstanceID());

            AnnotatedBeatmapLevelCollectionsViewController = LevelFilteringNavigationController.GetPrivateField<AnnotatedBeatmapLevelCollectionsViewController>("_annotatedBeatmapLevelCollectionsViewController");
            Logger.Debug("Acquired AnnotatedBeatmapLevelCollectionsViewController from LevelFilteringNavigationController [{0}]", AnnotatedBeatmapLevelCollectionsViewController.GetInstanceID());

            TableView tableView = LevelCollectionTableView.GetPrivateField<TableView>("_tableView");
            TableViewPageUpButton = tableView.GetPrivateField<Button>("_pageUpButton");
            TableViewPageDownButton = tableView.GetPrivateField<Button>("_pageDownButton");
            Logger.Debug("Acquired Page Up and Down buttons...");

            ActionButtons = LevelDetailViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "ActionButtons");
            Logger.Debug("Acquired ActionButtons [{0}]", ActionButtons.GetInstanceID());

            SimpleDialogPromptViewControllerPrefab = Resources.FindObjectsOfTypeAll<SimpleDialogPromptViewController>().Last();
            Logger.Debug("Acquired SimpleDialogPromptViewControllerPrefab [{0}]", SimpleDialogPromptViewControllerPrefab.GetInstanceID());

            BeatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().Last();
            Logger.Debug("Acquired BeatmapLevelsModel [{0}]", BeatmapLevelsModel);
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

            var pack = LevelCollectionNavigationController.GetPrivateField<IBeatmapLevelPack>("_levelPack");
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
            IBeatmapLevelPack[] levelPacks = beatMapLevelPackCollection.GetPrivateField<IBeatmapLevelPack[]>("_allBeatmapLevelPacks");
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
                IAnnotatedBeatmapLevelCollection[] _annotatedBeatmapLevelCollections = AnnotatedBeatmapLevelCollectionsViewController.GetPrivateField<IAnnotatedBeatmapLevelCollection[]>("_annotatedBeatmapLevelCollections");
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
        public IPreviewBeatmapLevel[] GetCurrentLevelCollectionLevels()
        {
            var levelCollection = GetCurrentSelectedAnnotatedBeatmapLevelCollection();
            if (levelCollection == null)
            {
                Logger.Debug("Current selected level collection is null for some reason...");
                return null;
            }

            return levelCollection.beatmapLevelCollection.beatmapLevels;
        }

        public bool SelectLevelCategory(String levelCategoryName)
        {
            Logger.Trace("SelectLevelCategory({0})", levelCategoryName);

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

                if (category == LevelFilteringNavigationController.selectedLevelCategory)
                {
                    Logger.Debug($"Level category [{category}] is already selected");
                    return false;
                }

                Logger.Info("Selecting level category: {0}", levelCategoryName);

                var selectLeveCategoryViewController = LevelFilteringNavigationController.GetComponentInChildren<SelectLevelCategoryViewController>();
                var iconSegementController = selectLeveCategoryViewController.GetComponentInChildren<IconSegmentedControl>();

                int selectCellNumber = (from x in selectLeveCategoryViewController.GetPrivateField<SelectLevelCategoryViewController.LevelCategoryInfo[]>("_levelCategoryInfos")
                                        select x.levelCategory).ToList().IndexOf(category);

                iconSegementController.SelectCellWithNumber(selectCellNumber);
                //selectLeveCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell(iconSegementController, selectCellNumber);
                LevelFilteringNavigationController.UpdateSecondChildControllerContent(category);
                //AnnotatedBeatmapLevelCollectionsViewController.RefreshAvailability();

                Logger.Debug("Done selecting level category.");

                return true;

            } catch (Exception e)
            {
                Logger.Exception(e);
            }

            return false;
        }

        /// <summary>
        /// Select a level collection.
        /// </summary>
        /// <param name="levelCollectionName"></param>
        public void SelectLevelCollection(String levelCollectionName)
        {
            Logger.Trace("SelectLevelCollection({0})", levelCollectionName);

            try
            {
                IAnnotatedBeatmapLevelCollection collection = GetLevelCollectionByName(levelCollectionName);
                if (collection == null)
                {
                    Logger.Debug("Could not locate requested level collection...");
                    return;
                }

                Logger.Info("Selecting level collection: {0}", collection.collectionName);

                LevelFilteringNavigationController.SelectAnnotatedBeatmapLevelCollection(collection as IBeatmapLevelPack);                

                Logger.Debug("Done selecting level collection!");
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        /// <summary>
        /// Scroll TableView to proper row, fire events.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="levelID"></param>
        public void SelectAndScrollToLevel(string levelID)
        {
            Logger.Debug("Scrolling to LevelID: {0}", levelID);

            // Check once per load
            if (!_checkedForTwitchPlugin)
            {
                Logger.Info("Checking for BeatSaber Twitch Integration Plugin...");
                _detectedTwitchPluginQueue = Resources.FindObjectsOfTypeAll<HMUI.ViewController>().Any(x => x.name == "RequestInfo");
                Logger.Info("BeatSaber Twitch Integration plugin detected: " + _detectedTwitchPluginQueue);

                _checkedForTwitchPlugin = true;
            }

            // Skip scrolling to level if twitch plugin has queue active.
            if (_detectedTwitchPluginQueue)
            {
                Logger.Debug("Skipping SelectAndScrollToLevel() because we detected Twitch Integration Plugin has a Queue active...");
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

                int selectedRow = LevelCollectionTableView.GetPrivateField<int>("_selectedRow");

                Logger.Debug("Song is not in the level pack, cannot scroll to it...  Using last known row {0}/{1}", selectedRow, maxCount);
                selectedIndex = Math.Min(maxCount, selectedRow);
            }
            else if (LevelCollectionViewController.GetPrivateField<bool>("_showHeader"))
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
            Logger.Debug("Scrolling level list to idx: {0}", selectedIndex);

            TableView tableView = LevelCollectionTableView.GetPrivateField<TableView>("_tableView");
            var selectedRow = LevelCollectionTableView.GetPrivateField<int>("_selectedRow");
            if (selectedRow != selectedIndex && LevelCollectionTableView.isActiveAndEnabled)
            {
                LevelCollectionTableView.HandleDidSelectRowEvent(tableView, selectedIndex);
            }
            tableView.ScrollToCellWithIdx(selectedIndex, TableViewScroller.ScrollPositionType.Beginning, true);
            tableView.SelectCellWithIdx(selectedIndex);
        }

        /// <summary>
        /// Try to refresh the song list.  Broken for now.
        /// </summary>
        public void RefreshSongList(string currentSelectedLevelId, bool scrollToLevel = true)
        {
            Logger.Info("Refreshing the song list view.");
            try
            {
                var levels = GetCurrentLevelCollectionLevels();
                if (levels == null)
                {
                    Logger.Info("Nothing to refresh yet.");
                    return;
                }

                Logger.Debug("Checking if TableView is initialized...");
                TableView tableView = LevelCollectionTableView.GetPrivateField<TableView>("_tableView");
                bool tableViewInit = tableView.GetPrivateField<bool>("_isInitialized");

                Logger.Debug("Reloading SongList TableView");
                tableView.ReloadData();

                Logger.Debug("Attempting to scroll to level [{0}]", currentSelectedLevelId);
                String selectedLevelID = currentSelectedLevelId;
                if (!String.IsNullOrEmpty(currentSelectedLevelId))
                {
                    selectedLevelID = currentSelectedLevelId;
                }
                else
                {
                    if (levels.Length > 0)
                    {
                        Logger.Debug("Currently selected level ID does not exist, picking the first...");
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
                Logger.Exception("Exception refreshing song list:", e);
            }
        }
    }
}

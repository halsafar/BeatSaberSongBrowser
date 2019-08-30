using CustomUI.Utilities;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser.DataAccess
{
    public class BeatSaberUIController
    {
        // Beat Saber UI Elements
        public FlowCoordinator LevelSelectionFlowCoordinator;

        public LevelPacksViewController LevelPackViewController;
        public LevelPacksTableView LevelPacksTableView;
        public LevelPackDetailViewController LevelPackDetailViewController;

        public LevelPackLevelsViewController LevelPackLevelsViewController;
        public LevelPackLevelsTableView LevelPackLevelsTableView;
        public StandardLevelDetailViewController LevelDetailViewController;
        public StandardLevelDetailView StandardLevelDetailView;

        public BeatmapDifficultySegmentedControlController LevelDifficultyViewController;
        public BeatmapCharacteristicSegmentedControlController BeatmapCharacteristicSelectionViewController;

        public DismissableNavigationController LevelSelectionNavigationController;

        public RectTransform LevelPackLevelsTableViewRectTransform;

        public Button TableViewPageUpButton;
        public Button TableViewPageDownButton;

        public RectTransform PlayContainer;
        public RectTransform PlayButtons;

        public Button PlayButton;
        public Button PracticeButton;

        public SimpleDialogPromptViewController SimpleDialogPromptViewControllerPrefab;

        // Plugin Compat checks
        private bool _detectedTwitchPluginQueue = false;
        private bool _checkedForTwitchPlugin = false;

        /// <summary>
        /// Constructor.  Acquire all necessary BeatSaberUi elements.
        /// </summary>
        /// <param name="flowCoordinator"></param>
        public BeatSaberUIController(FlowCoordinator flowCoordinator)
        {
            LevelSelectionFlowCoordinator = flowCoordinator;

            // gather controllers and ui elements.
            LevelPackViewController = LevelSelectionFlowCoordinator.GetPrivateField<LevelPacksViewController>("_levelPacksViewController");
            Logger.Debug("Acquired LevelPacksViewController [{0}]", LevelPackViewController.GetInstanceID());

            LevelPackDetailViewController = LevelSelectionFlowCoordinator.GetPrivateField<LevelPackDetailViewController>("_levelPackDetailViewController");
            Logger.Debug("Acquired LevelPackDetailViewController [{0}]", LevelPackDetailViewController.GetInstanceID());

            LevelPacksTableView = LevelPackViewController.GetPrivateField<LevelPacksTableView>("_levelPacksTableView");
            Logger.Debug("Acquired LevelPacksTableView [{0}]", LevelPacksTableView.GetInstanceID());

            LevelPackLevelsViewController = LevelSelectionFlowCoordinator.GetPrivateField<LevelPackLevelsViewController>("_levelPackLevelsViewController");
            Logger.Debug("Acquired LevelPackLevelsViewController [{0}]", LevelPackLevelsViewController.GetInstanceID());

            LevelPackLevelsTableView = this.LevelPackLevelsViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView");
            Logger.Debug("Acquired LevelPackLevelsTableView [{0}]", LevelPackLevelsTableView.GetInstanceID());

            LevelDetailViewController = LevelSelectionFlowCoordinator.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController");
            Logger.Debug("Acquired StandardLevelDetailViewController [{0}]", LevelDetailViewController.GetInstanceID());

            StandardLevelDetailView = LevelDetailViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView");
            Logger.Debug("Acquired StandardLevelDetailView [{0}]", StandardLevelDetailView.GetInstanceID());

            BeatmapCharacteristicSelectionViewController = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSegmentedControlController>().First();
            Logger.Debug("Acquired BeatmapCharacteristicSegmentedControlController [{0}]", BeatmapCharacteristicSelectionViewController.GetInstanceID());

            LevelSelectionNavigationController = LevelSelectionFlowCoordinator.GetPrivateField<DismissableNavigationController>("_navigationController");
            Logger.Debug("Acquired DismissableNavigationController [{0}]", LevelSelectionNavigationController.GetInstanceID());

            LevelDifficultyViewController = StandardLevelDetailView.GetPrivateField<BeatmapDifficultySegmentedControlController>("_beatmapDifficultySegmentedControlController");
            Logger.Debug("Acquired BeatmapDifficultySegmentedControlController [{0}]", LevelDifficultyViewController.GetInstanceID());

            LevelPackLevelsTableViewRectTransform = LevelPackLevelsTableView.transform as RectTransform;
            Logger.Debug("Acquired TableViewRectTransform from LevelPackLevelsTableView [{0}]", LevelPackLevelsTableViewRectTransform.GetInstanceID());

            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(LevelPackLevelsTableView, "_tableView");
            TableViewPageUpButton = tableView.GetPrivateField<Button>("_pageUpButton");
            TableViewPageDownButton = tableView.GetPrivateField<Button>("_pageDownButton");
            Logger.Debug("Acquired Page Up and Down buttons...");

            PlayContainer = StandardLevelDetailView.GetComponentsInChildren<RectTransform>().First(x => x.name == "PlayContainer");
            PlayButtons = PlayContainer.GetComponentsInChildren<RectTransform>().First(x => x.name == "PlayButtons");

            PlayButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "PlayButton");
            PracticeButton = PlayButtons.GetComponentsInChildren<Button>().First(x => x.name == "PracticeButton");

            SimpleDialogPromptViewControllerPrefab = Resources.FindObjectsOfTypeAll<SimpleDialogPromptViewController>().First();
        }


        /// <summary>
        /// Acquire the level pack collection.
        /// </summary>
        /// <returns></returns>
        public IBeatmapLevelPackCollection GetLevelPackCollection()
        {
            if (LevelPackViewController == null)
            {
                return null;
            }

            IBeatmapLevelPackCollection levelPackCollection = LevelPackViewController.GetPrivateField<IBeatmapLevelPackCollection>("_levelPackCollection");
            return levelPackCollection;
        }

        /// <summary>
        /// Get the currently selected level pack within the LevelPackLevelViewController hierarchy.
        /// </summary>
        /// <returns></returns>
        public IBeatmapLevelPack GetCurrentSelectedLevelPack()
        {
            if (LevelPackLevelsTableView == null)
            {
                return null;
            }

            var pack = LevelPackLevelsTableView.GetPrivateField<IBeatmapLevelPack>("_pack");
            return pack;
        }

        /// <summary>
        /// Get level pack by level pack id.
        /// </summary>
        /// <param name="levelPackId"></param>
        /// <returns></returns>
        public IBeatmapLevelPack GetLevelPackByPackId(String levelPackId)
        {
            IBeatmapLevelPackCollection levelPackCollection = GetLevelPackCollection();
            if (levelPackCollection == null)
            {
                return null;
            }

            IBeatmapLevelPack levelPack = levelPackCollection.beatmapLevelPacks.ToList().FirstOrDefault(x => x.packID == levelPackId);
            return levelPack;
        }

        /// <summary>
        /// Get level pack index by level pack id.
        /// </summary>
        /// <param name="levelPackId"></param>
        /// <returns></returns>
        public int GetLevelPackIndexByPackId(String levelPackId)
        {
            IBeatmapLevelPackCollection levelPackCollection = GetLevelPackCollection();
            if (levelPackCollection == null)
            {
                return -1;
            }

            int index = levelPackCollection.beatmapLevelPacks.ToList().FindIndex(x => x.packID == levelPackId);
            return index;
        }


        /// <summary>
        /// Get Current levels from current level pack.
        /// </summary>
        /// <returns></returns>
        public IPreviewBeatmapLevel[] GetCurrentLevelPackLevels()
        {
            var levelPack = GetCurrentSelectedLevelPack();
            if (levelPack == null)
            {
                return null;
            }

            return levelPack.beatmapLevelCollection.beatmapLevels;
        }

        /// <summary>
        /// Get level count helper.
        /// </summary>
        /// <returns></returns>
        public int GetLevelPackLevelCount()
        {
            var levels = GetCurrentLevelPackLevels();
            if (levels == null)
            {
                return 0;
            }

            return levels.Length;
        }

        /// <summary>
        /// Select a level pack.
        /// </summary>
        /// <param name="levelPackId"></param>
        public void SelectLevelPack(String levelPackId)
        {
            Logger.Trace("SelectLevelPack({0})", levelPackId);

            try
            {
                var levelPacks = GetLevelPackCollection();
                var index = GetLevelPackIndexByPackId(levelPackId);
                var pack = GetLevelPackByPackId(levelPackId);

                if (index < 0)
                {
                    Logger.Debug("Cannot select level packs yet...");
                    return;
                }

                Logger.Info("Selecting level pack index: {0}", pack.packName);
                var tableView = LevelPacksTableView.GetPrivateField<TableView>("_tableView");

                LevelPacksTableView.SelectCellWithIdx(index);
                tableView.SelectCellWithIdx(index, true);
                tableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
                for (int i = 0; i < index; i++)
                {
                    tableView.GetPrivateField<TableViewScroller>("_scroller").PageScrollDown();
                }

                Logger.Debug("Done selecting level pack!");
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
        public void SelectAndScrollToLevel(LevelPackLevelsTableView table, string levelID)
        {
            Logger.Debug("Scrolling to LevelID: {0}", levelID);

            // Check once per load
            if (!_checkedForTwitchPlugin)
            {
                Logger.Info("Checking for BeatSaber Twitch Integration Plugin...");
                _detectedTwitchPluginQueue = Resources.FindObjectsOfTypeAll<VRUIViewController>().Any(x => x.name == "RequestInfo");
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
            List<IPreviewBeatmapLevel> levels = table.GetPrivateField<IBeatmapLevelPack>("_pack").beatmapLevelCollection.beatmapLevels.ToList();
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
                int maxCount = GetLevelPackLevelCount();

                int selectedRow = table.GetPrivateField<int>("_selectedRow");

                Logger.Debug("Song is not in the level pack, cannot scroll to it...  Using last known row {0}/{1}", selectedRow, maxCount);
                selectedIndex = Math.Min(maxCount, selectedRow);
            }
            else
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

            TableView tableView = LevelPackLevelsTableView.GetPrivateField<TableView>("_tableView");
            LevelPackLevelsTableView.HandleDidSelectRowEvent(tableView, selectedIndex);
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
                var levels = GetCurrentLevelPackLevels();

                Logger.Debug("Checking if TableView is initialized...");
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(LevelPackLevelsTableView, "_tableView");
                bool tableViewInit = ReflectionUtil.GetPrivateField<bool>(tableView, "_isInitialized");

                Logger.Debug("Reloading SongList TableView");
                tableView.ReloadData();

                Logger.Debug("Attempting to scroll to level...");
                String selectedLevelID = currentSelectedLevelId;
                if (!String.IsNullOrEmpty(currentSelectedLevelId))
                {
                    selectedLevelID = currentSelectedLevelId;
                }
                else
                {
                    if (levels.Length > 0)
                    {
                        selectedLevelID = levels.FirstOrDefault().levelID;
                    }
                }

                if (scrollToLevel)
                {
                    SelectAndScrollToLevel(LevelPackLevelsTableView, selectedLevelID);
                }
            }
            catch (Exception e)
            {
                Logger.Exception("Exception refreshing song list:", e);
            }
        }
    }
}

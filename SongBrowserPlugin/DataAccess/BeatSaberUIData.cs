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
    public class BeatSaberUIData
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

        public SimpleDialogPromptViewController SimpleDialogPromptViewControllerPrefab;

        public BeatSaberUIData(FlowCoordinator flowCoordinator)
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
    }
}

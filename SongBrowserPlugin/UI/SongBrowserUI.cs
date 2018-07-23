using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using HMUI;
using VRUI;
using SongBrowserPlugin.DataAccess;
using System.IO;
using SongLoaderPlugin;

namespace SongBrowserPlugin.UI
{
    /// <summary>
    /// Hijack the flow coordinator.  Have access to all StandardLevel easily.
    /// </summary>
    public class SongBrowserUI : MonoBehaviour
    {
        // Logging
        public const String Name = "SongBrowserUI";
        private Logger _log = new Logger(Name);

        // Beat Saber UI Elements
        StandardLevelSelectionFlowCoordinator _levelSelectionFlowCoordinator;
        StandardLevelListViewController _levelListViewController;
        StandardLevelDetailViewController _levelDetailViewController;
        StandardLevelDifficultyViewController _levelDifficultyViewController;
        StandardLevelSelectionNavigationController _levelSelectionNavigationController;

        // New UI Elements
        private List<SongSortButton> _sortButtonGroup;
        private Button _addFavoriteButton;
        private String _addFavoriteButtonText = null;
        private SimpleDialogPromptViewController _simpleDialogPromptViewControllerPrefab;
        private SimpleDialogPromptViewController _deleteDialog;
        private Button _deleteButton;

        // Debug
        private int _sortButtonLastPushedIndex = 0;
        private int _lastRow = 0;

        // Model
        private SongBrowserModel _model;

        /// <summary>
        /// Constructor
        /// </summary>
        public SongBrowserUI() : base()
        {
            if (_model == null)
            {
                _model = new SongBrowserModel();
            }
            _model.Init();
            _sortButtonLastPushedIndex = (int)(_model.Settings.sortMode);
        }

        /// <summary>
        /// Builds the UI for this plugin.
        /// </summary>
        public void CreateUI()
        {
            _log.Trace("CreateUI()");
            try
            {
                if (_levelSelectionFlowCoordinator == null)
                {
                    _levelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();
                }

                if (_levelListViewController == null)
                {
                    _levelListViewController = _levelSelectionFlowCoordinator.GetPrivateField<StandardLevelListViewController>("_levelListViewController");
                }

                if (_levelDetailViewController == null)
                {
                    _levelDetailViewController = _levelSelectionFlowCoordinator.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController");
                }

                if (_levelSelectionNavigationController == null)
                {
                    _levelSelectionNavigationController = _levelSelectionFlowCoordinator.GetPrivateField<StandardLevelSelectionNavigationController>("_levelSelectionNavigationController");
                }

                if (_levelDifficultyViewController == null)
                {
                    _levelDifficultyViewController = _levelSelectionFlowCoordinator.GetPrivateField<StandardLevelDifficultyViewController>("_levelDifficultyViewController");
                }

                _simpleDialogPromptViewControllerPrefab = Resources.FindObjectsOfTypeAll<SimpleDialogPromptViewController>().First();

                this._deleteDialog = UnityEngine.Object.Instantiate<SimpleDialogPromptViewController>(this._simpleDialogPromptViewControllerPrefab);
                this._deleteDialog.gameObject.SetActive(false);

                this.CreateUIElements();

                _levelListViewController.didSelectLevelEvent += OnDidSelectLevelEvent;
            }
            catch (Exception e)
            {
                _log.Exception("Exception during CreateUI: ", e);
            }
        }

        /// <summary>
        /// Builds the SongBrowser UI
        /// </summary>
        private void CreateUIElements()
        {
            _log.Trace("CreateUIElements");

            try
            {                               
                RectTransform rect = this._levelSelectionNavigationController.transform as RectTransform;

                // Create Sorting Songs By-Buttons
                _log.Debug("Creating sort by buttons...");

                System.Action<SongSortMode> onSortButtonClickEvent = delegate (SongSortMode sortMode) {
                    _log.Debug("Sort button - {0} - pressed.", sortMode.ToString());
                    SongBrowserModel.LastSelectedLevelId = null;

                    _model.Settings.sortMode = sortMode;
                    _model.Settings.Save();
                    UpdateSongList();
                    RefreshSongList();
                };

                _sortButtonGroup = new List<SongSortButton>
                {
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Favorite", 3, "AllDirectionsIcon", 66, 74.5f, 16f, 5f, SongSortMode.Favorites, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Song", 3, "AllDirectionsIcon", 50f, 74.5f, 16f, 5f, SongSortMode.Default, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Author", 3, "AllDirectionsIcon", 34f, 74.5f, 16f, 5f, SongSortMode.Author, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Original", 3, "AllDirectionsIcon", 18f, 74.5f, 16f, 5f, SongSortMode.Original, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Newest", 3, "AllDirectionsIcon", 2f, 74.5f, 16f, 5f, SongSortMode.Newest, onSortButtonClickEvent),
                };

                // Creaate Add to Favorites Button
                _log.Debug("Creating add to favorites button...");

                RectTransform transform = this._levelDetailViewController.transform as RectTransform;
                _addFavoriteButton = UIBuilder.CreateUIButton(transform, "QuitButton", SongBrowserApplication.Instance.ButtonTemplate);
                (_addFavoriteButton.transform as RectTransform).anchoredPosition = new Vector2(40f, 5.75f);
                (_addFavoriteButton.transform as RectTransform).sizeDelta = new Vector2(10f, 10f);
                UIBuilder.SetButtonText(ref _addFavoriteButton, _addFavoriteButtonText);
                UIBuilder.SetButtonTextSize(ref _addFavoriteButton, 3);
                UIBuilder.SetButtonIconEnabled(ref _addFavoriteButton, false);
                _addFavoriteButton.onClick.RemoveAllListeners();
                _addFavoriteButton.onClick.AddListener(delegate () {
                    ToggleSongInFavorites();
                });

                if (_addFavoriteButtonText == null)
                {
                    IStandardLevel level = this._levelListViewController.selectedLevel;
                    if (level != null)
                    {
                        RefreshAddFavoriteButton(level.levelID);
                    }                    
                }

                // Create delete button
                _log.Debug("Creating delete button...");

                transform = this._levelDetailViewController.transform as RectTransform;
                _deleteButton = UIBuilder.CreateUIButton(transform, "QuitButton", SongBrowserApplication.Instance.ButtonTemplate);
                (_deleteButton.transform as RectTransform).anchoredPosition = new Vector2(46f, 0f);
                (_deleteButton.transform as RectTransform).sizeDelta = new Vector2(15f, 5f);
                UIBuilder.SetButtonText(ref _deleteButton, "Delete");
                UIBuilder.SetButtonTextSize(ref _deleteButton, 3);
                UIBuilder.SetButtonIconEnabled(ref _deleteButton, false);
                _deleteButton.onClick.RemoveAllListeners();
                _deleteButton.onClick.AddListener(delegate () {
                    HandleDeleteSelectedLevel();
                });

                RefreshUI();
            }
            catch (Exception e)
            {
                _log.Exception("Exception CreateUIElements:", e);
            }
        }

        /// <summary>
        /// Adjust UI based on level selected.
        /// Various ways of detecting if a level is not properly selected.  Seems most hit the first one.
        /// </summary>
        private void OnDidSelectLevelEvent(StandardLevelListViewController view, IStandardLevel level)
        {
            _log.Trace("OnDidSelectLevelEvent({0}", level.levelID);
            if (level == null)
            {
                _log.Debug("No level selected?");
                return;
            }

            if (_model.Settings == null)
            {
                _log.Debug("Settings not instantiated yet?");
                return;
            }

            SongBrowserModel.LastSelectedLevelId = level.levelID;

            RefreshAddFavoriteButton(level.levelID);
        }

        /// <summary>
        /// Pop up a delete dialog.
        /// </summary>
        private void HandleDeleteSelectedLevel()
        {
            IStandardLevel level = this._levelListViewController.selectedLevel;
            if (level == null)
            {
                _log.Info("No level selected, cannot delete nothing...");
                return;
            }

            if (level.levelID.StartsWith("Level"))
            {
                _log.Debug("Cannot delete non-custom levels.");
                return;
            }

            SongLoaderPlugin.OverrideClasses.CustomLevel customLevel = _model.LevelIdToCustomSongInfos[level.levelID];

            this._deleteDialog.Init("Delete level warning!", String.Format("<color=#00AAFF>Permanently delete level: {0}</color>\n  Do you want to continue?", customLevel.songName), "YES", "NO");
            this._deleteDialog.didFinishEvent += this.HandleDeleteDialogPromptViewControllerDidFinish;

            this._levelSelectionNavigationController.PresentModalViewController(this._deleteDialog, null, false);
        }

        /// <summary>
        /// Handle delete dialog resolution.
        /// </summary>
        /// <param name="viewController"></param>
        /// <param name="ok"></param>
        public void HandleDeleteDialogPromptViewControllerDidFinish(SimpleDialogPromptViewController viewController, bool ok)
        {
            viewController.didFinishEvent -= this.HandleDeleteDialogPromptViewControllerDidFinish;
            if (!ok)
            {
                viewController.DismissModalViewController(null, false);
            }
            else
            {
                IStandardLevel level = this._levelListViewController.selectedLevel;
                SongLoaderPlugin.OverrideClasses.CustomLevel customLevel = _model.LevelIdToCustomSongInfos[level.levelID];

                viewController.DismissModalViewController(null, false);
                _log.Debug("Deleting: {0}", customLevel.customSongInfo.path);
                
                FileAttributes attr = File.GetAttributes(customLevel.customSongInfo.path);
                if (attr.HasFlag(FileAttributes.Directory))
                    Directory.Delete(customLevel.customSongInfo.path);
                else
                    File.Delete(customLevel.customSongInfo.path);

                SongLoaderPlugin.SongLoader.Instance.RemoveSongWithPath(customLevel.customSongInfo.path);
            }
        }

        /// <summary>
        /// Add/Remove song from favorites depending on if it already exists.
        /// </summary>
        private void ToggleSongInFavorites()
        {
            IStandardLevel songInfo = this._levelListViewController.selectedLevel;
            if (_model.Settings.favorites.Contains(songInfo.levelID))
            {
                _log.Info("Remove {0} from favorites", songInfo.songName);
                _model.Settings.favorites.Remove(songInfo.levelID);
                _addFavoriteButtonText = "+1";
            }
            else
            {
                _log.Info("Add {0} to favorites", songInfo.songName);
                _model.Settings.favorites.Add(songInfo.levelID);
                _addFavoriteButtonText = "-1";                
            }

            UIBuilder.SetButtonText(ref _addFavoriteButton, _addFavoriteButtonText);

            _model.Settings.Save();
        }

        /// <summary>
        /// Helper to quickly refresh add to favorites button
        /// </summary>
        /// <param name="levelId"></param>
        private void RefreshAddFavoriteButton(String levelId)
        {
            if (levelId == null)
            {
                _addFavoriteButtonText = "0";
                return;
            }

            if (_model.Settings.favorites.Contains(levelId))
            {
                _addFavoriteButtonText = "-1";
            }
            else
            {
                _addFavoriteButtonText = "+1";                
            }

            UIBuilder.SetButtonText(ref _addFavoriteButton, _addFavoriteButtonText);
        }

        /// <summary>
        /// Adjust the UI colors.
        /// </summary>
        public void RefreshUI()
        {
            // So far all we need to refresh is the sort buttons.
            foreach (SongSortButton sortButton in _sortButtonGroup)
            {
                UIBuilder.SetButtonBorder(ref sortButton.Button, Color.black);
                if (sortButton.SortMode == _model.Settings.sortMode)
                {
                    UIBuilder.SetButtonBorder(ref sortButton.Button, Color.red);
                }
            }            
        }

        /// <summary>
        /// Try to refresh the song list.  Broken for now.
        /// </summary>
        public void RefreshSongList()
        {
            _log.Info("Refreshing the song list view.");
            try
            {
                if (_model.SortedSongList == null)
                {
                    _log.Debug("Songs are not sorted yet, nothing to refresh.");
                    return;
                }

                StandardLevelSO[] levels = _model.SortedSongList.ToArray();
                StandardLevelListViewController songListViewController = this._levelSelectionFlowCoordinator.GetPrivateField<StandardLevelListViewController>("_levelListViewController");
                StandardLevelListTableView _songListTableView = songListViewController.GetComponentInChildren<StandardLevelListTableView>();
                ReflectionUtil.SetPrivateField(_songListTableView, "_levels", levels);
                ReflectionUtil.SetPrivateField(songListViewController, "_levels", levels);            
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_songListTableView, "_tableView");
                tableView.ReloadData();

                String selectedLevelID = null;
                if (SongBrowserModel.LastSelectedLevelId != null)
                {
                    selectedLevelID = SongBrowserModel.LastSelectedLevelId;
                    _log.Debug("Scrolling to row for level ID: {0}", selectedLevelID);                    
                }
                else
                {
                    selectedLevelID = levels.FirstOrDefault().levelID;
                }
                SelectAndScrollToLevel(_songListTableView, selectedLevelID);
                
                RefreshUI();                
            }
            catch (Exception e)
            {
                _log.Exception("Exception refreshing song list:", e);
            }
        }

        private void SelectAndScrollToLevel(StandardLevelListTableView table, string levelID)
        {
            int row = table.RowNumberForLevelID(levelID);
            TableView _tableView = table.GetComponentInChildren<TableView>();
            _tableView.SelectRow(row, true);
            _tableView.ScrollToRow(row, true);
        }

        /// <summary>
        /// Helper for updating the model (which updates the song list)c
        /// </summary>
        public void UpdateSongList()
        {
            _log.Trace("UpdateSongList()");

            GameplayMode gameplayMode = _levelSelectionFlowCoordinator.GetPrivateField<GameplayMode>("_gameplayMode");
            _model.UpdateSongLists(gameplayMode);                        
        }

        /// <summary>
        /// Not normally called by the game-engine.  Dependent on SongBrowserApplication to call it.
        /// </summary>
        public void LateUpdate()
        {
            if (this._levelListViewController.isInViewControllerHierarchy)
            {
                CheckDebugUserInput();
            }
        }

        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void CheckDebugUserInput()
        {
            try
            {
                // back
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    this._levelSelectionNavigationController.DismissButtonWasPressed();
                }

                // cycle sort modes
                if (Input.GetKeyDown(KeyCode.T))
                {
                    _sortButtonLastPushedIndex = (_sortButtonLastPushedIndex + 1) % _sortButtonGroup.Count;
                    _sortButtonGroup[_sortButtonLastPushedIndex].Button.onClick.Invoke();
                }

                // delete
                if (Input.GetKeyDown(KeyCode.D))
                {
                    if (_deleteDialog.isInViewControllerHierarchy)
                    {
                        return;
                    }
                    _deleteButton.onClick.Invoke();
                }

                StandardLevelListTableView levelListTableView = this._levelListViewController.GetComponentInChildren<StandardLevelListTableView>();

                // z,x,c,v can be used to get into a song, b will hit continue button after song ends
                if (Input.GetKeyDown(KeyCode.C))
                {
                    levelListTableView.SelectAndScrollToLevel(_model.SortedSongList[0].levelID);
                    this._levelListViewController.HandleLevelListTableViewDidSelectRow(levelListTableView, 0);                    
                    this._levelDifficultyViewController.HandleDifficultyTableViewDidSelectRow(null, 0);
                    this._levelSelectionFlowCoordinator.HandleDifficultyViewControllerDidSelectDifficulty(_levelDifficultyViewController, _model.SortedSongList[0].GetDifficultyLevel(LevelDifficulty.Easy));
                }

                if (Input.GetKeyDown(KeyCode.V))
                {
                    this._levelSelectionFlowCoordinator.HandleLevelDetailViewControllerDidPressPlayButton(this._levelDetailViewController);
                }

                // change song index
                if (Input.GetKeyDown(KeyCode.N))
                {
                    _lastRow = (_lastRow - 1) != -1 ? (_lastRow - 1) % this._model.SortedSongList.Count : 0;

                    levelListTableView.SelectAndScrollToLevel(_model.SortedSongList[_lastRow].levelID);
                    this._levelListViewController.HandleLevelListTableViewDidSelectRow(levelListTableView, _lastRow);
                }

                if (Input.GetKeyDown(KeyCode.M))
                {
                    _lastRow = (_lastRow + 1) % this._model.SortedSongList.Count;

                    levelListTableView.SelectAndScrollToLevel(_model.SortedSongList[_lastRow].levelID);
                    this._levelListViewController.HandleLevelListTableViewDidSelectRow(levelListTableView, _lastRow);
                }

                // add to favorites
                if (Input.GetKeyDown(KeyCode.F))
                {
                    ToggleSongInFavorites();
                }
            }
            catch (Exception e)
            {
                _log.Exception("Debug Input caused Exception: ", e);
            }
        }
    }
}
 
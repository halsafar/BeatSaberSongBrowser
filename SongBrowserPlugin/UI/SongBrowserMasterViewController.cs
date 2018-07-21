using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HMUI;
using VRUI;


namespace SongBrowserPlugin.UI
{
    /// <summary>
    /// Hijack the flow coordinator.  Have access to all StandardLevel easily.
    /// </summary>
    public class SongBrowserFlowCoordinator : StandardLevelSelectionFlowCoordinator
    {
        // Logging
        public const String Name = "SongBrowserMasterViewController";
        private Logger _log = new Logger(Name);

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
        /// Unity OnLoad
        /// </summary>
        public static SongBrowserFlowCoordinator Instance;
        public static void OnLoad()
        {
            if (Instance != null) return;
            new GameObject("Song Browser Modded").AddComponent<SongBrowserFlowCoordinator>();
        }        

        /// <summary>
        /// Constructor
        /// </summary>
        public SongBrowserFlowCoordinator() : base()
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
        public void Init()
        {
            _log.Debug("Init()");
            try
            {
                _simpleDialogPromptViewControllerPrefab = Resources.FindObjectsOfTypeAll<SimpleDialogPromptViewController>().First();

                this._deleteDialog = UnityEngine.Object.Instantiate<SimpleDialogPromptViewController>(this._simpleDialogPromptViewControllerPrefab);
                this._deleteDialog.gameObject.SetActive(false);

                //if (!_uiInitialized)
                {
                    CreateUI();
                }
            
                _levelListViewController.didSelectLevelEvent += OnDidSelectLevelEvent;
            }
            catch (Exception e)
            {
                _log.Exception("Exception during DidActivate: " + e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentViewController"></param>
        /// <param name="levels"></param>
        /// <param name="gameplayMode"></param>
        public virtual void Present(VRUIViewController parentViewController, IStandardLevel[] levels, GameplayMode gameplayMode)
        {
            _log.Debug("Present()");
            base.Present(parentViewController, _model.SortedSongList.ToArray(), gameplayMode);
        }

        /// <summary>
        /// Builds the SongBrowser UI
        /// </summary>
        public void CreateUI()
        {
            _log.Debug("CreateUI");

            try
            {                               
                RectTransform rect = this._levelSelectionNavigationController.transform as RectTransform;

                // Create Sorting Songs By-Buttons
                _log.Debug("Creating sort by buttons...");

                System.Action<SongSortMode> onSortButtonClickEvent = delegate (SongSortMode sortMode) {
                    _log.Debug("Sort button - {0} - pressed.", sortMode.ToString());
                    _model.Settings.sortMode = sortMode;
                    _model.Settings.Save();
                    UpdateSongList();
                };

                _sortButtonGroup = new List<SongSortButton>
                {
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Favorite", 3, "AllDirectionsIcon", 60, 75f, 16f, 5f, SongSortMode.Favorites, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Song", 3, "AllDirectionsIcon", 44f, 75f, 16f, 5f, SongSortMode.Default, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Author", 3, "AllDirectionsIcon", 28f, 75f, 16f, 5f, SongSortMode.Author, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Original", 3, "AllDirectionsIcon", 12f, 75f, 16f, 5f, SongSortMode.Original, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Newest", 3, "AllDirectionsIcon", -4f, 75f, 16f, 5f, SongSortMode.Newest, onSortButtonClickEvent),
                };

                // Creaate Add to Favorites Button
                _log.Debug("Creating add to favorites button...");

                RectTransform transform = this._levelDetailViewController.transform as RectTransform;
                _addFavoriteButton = UIBuilder.CreateUIButton(transform, "QuitButton", SongBrowserApplication.Instance.ButtonTemplate);
                (_addFavoriteButton.transform as RectTransform).anchoredPosition = new Vector2(45f, 9f);
                (_addFavoriteButton.transform as RectTransform).sizeDelta = new Vector2(16f, 5.0f);
                
                if (_addFavoriteButtonText == null)
                {
                    _log.Debug("Determining if first selected song is a favorite: {0}", this._levelListViewController);
                    IStandardLevel level = this._levelListViewController.selectedLevel;
                    if (level != null)
                    {
                        RefreshAddFavoriteButton(level.levelID);
                    }                    
                }
                
                UIBuilder.SetButtonText(ref _addFavoriteButton, _addFavoriteButtonText);                
                UIBuilder.SetButtonTextSize(ref _addFavoriteButton, 3);
                UIBuilder.SetButtonIconEnabled(ref _addFavoriteButton, false);                
                _addFavoriteButton.onClick.RemoveAllListeners();
                _addFavoriteButton.onClick.AddListener(delegate () {                    
                    ToggleSongInFavorites();
                });

                // Create delete button
                _log.Debug("Creating delete button...");

                transform = this._levelDetailViewController.transform as RectTransform;
                _deleteButton = UIBuilder.CreateUIButton(transform, "QuitButton", SongBrowserApplication.Instance.ButtonTemplate);
                (_deleteButton.transform as RectTransform).anchoredPosition = new Vector2(45f, 0f);
                (_deleteButton.transform as RectTransform).sizeDelta = new Vector2(16f, 5f);
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
                _log.Exception("Exception CreateUI: {0}\n{1}", e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// Adjust UI based on level selected.
        /// Various ways of detecting if a level is not properly selected.  Seems most hit the first one.
        /// </summary>
        private void OnDidSelectLevelEvent(StandardLevelListViewController view, IStandardLevel level)
        {
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

            _log.Debug("LEVEL ON DELETE: {0}", level.levelID);
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
            viewController.didFinishEvent -= this.HandleSimpleDialogPromptViewControllerDidFinish;
            if (!ok)
            {
                viewController.DismissModalViewController(null, false);
            }
            else
            {
                IStandardLevel level = this._levelListViewController.selectedLevel;
                SongLoaderPlugin.OverrideClasses.CustomLevel customLevel = _model.LevelIdToCustomSongInfos[level.levelID];

                viewController.DismissModalViewController(null, false);
                _log.Debug("DELETING: {0}", customLevel.customSongInfo.path);
                //Directory.Delete(songInfo.path);
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
        public void RefreshSongList(List<SongLoaderPlugin.OverrideClasses.CustomLevel> songList)
        {
            _log.Debug("Attempting to refresh the song list view.");
            try
            {
                // Check a couple of possible situations that we can't refresh
                if (!this._levelListViewController.isInViewControllerHierarchy)
                {
                    _log.Debug("No song list to refresh.");
                    return;
                }

                // Convert to Array once in-case this is costly.
                SongLoaderPlugin.OverrideClasses.CustomLevel[] songListArray = songList.ToArray();

                // Store on song browser
                this._levelListViewController.Init(songListArray, false);

                // Refresh UI Elements in case something changed.
                RefreshAddFavoriteButton(songList[0].levelID);

                // Might not be fully presented yet.
                StandardLevelListTableView levelListTableView = this._levelListViewController.GetComponentInChildren<StandardLevelListTableView>();
                if (levelListTableView == null || !levelListTableView.isActiveAndEnabled)
                {
                    _log.Debug("SongListTableView not presenting yet, cannot refresh view yet.");
                    return;
                }

                TableView tableView = levelListTableView.GetComponent<TableView>();
                //TableView tableView = ReflectionUtil.GetPrivateField<TableView>(levelListTableView, "_tableView");
                if (tableView == null)
                {
                    _log.Debug("TableView not presenting yet, cannot refresh view yet.");
                    return;
                }

                // Refresh the list views and its table view
                levelListTableView.Init(songListArray, null);
                tableView.ScrollToRow(0, false);
                tableView.ReloadData();

                // Clear Force selection of index 0 so we don't end up in a weird state.
                // songListTableView.ClearSelection();              
                levelListTableView.SelectAndScrollToLevel(_model.SortedSongList[0].levelID);
                levelListTableView.HandleDidSelectRowEvent(tableView, 0);
                this._levelListViewController.HandleLevelListTableViewDidSelectRow(levelListTableView, 0);
                _lastRow = 0;
            }
            catch (Exception e)
            {
                _log.Exception("Exception refreshing song list: {0}", e.Message);
            }
        }

        /// <summary>
        /// Helper for updating the model (which updates the song list)c
        /// </summary>
        public void UpdateSongList()
        {
            _log.Debug("UpdateSongList()");

            _model.UpdateSongLists();
            RefreshSongList(_model.SortedSongList);
            RefreshUI();
        }

        /// <summary>
        /// Not normally called by the game-engine.  Dependent on SongBrowserApplication to call it.
        /// </summary>
        public void Update()
        {
            CheckDebugUserInput();
        }

        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void CheckDebugUserInput()
        {
            try
            {
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
                    _levelDifficultyViewController.HandleDifficultyTableViewDidSelectRow(null, 0);
                    this.HandleDifficultyViewControllerDidSelectDifficulty(_levelDifficultyViewController, _model.SortedSongList[0].GetDifficultyLevel(LevelDifficulty.Easy));
                }

                if (Input.GetKeyDown(KeyCode.V))
                {
                    this.HandleLevelDetailViewControllerDidPressPlayButton(this._levelDetailViewController);
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
                _log.Exception("{0}:\n{1}", e.Message, e.StackTrace);
            }
        }
    }
}
 
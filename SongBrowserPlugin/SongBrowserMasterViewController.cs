using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HMUI;
using System.IO;

namespace SongBrowserPlugin
{
    public class SongSortButton
    {
        public SongSortMode SortMode;
        public Button Button;
    }

    public class SongBrowserMasterViewController : SongSelectionMasterViewController
    {
        public const String Name = "SongBrowserMasterViewController";

        public const int MenuIndex = 1;

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

        // Model
        private SongBrowserModel _model;

        private bool _uiInitialized;

        /// <summary>
        /// Unity OnLoad
        /// </summary>
        public static void OnLoad()
        {
            if (Instance != null) return;
            new GameObject("Song Browser Modded").AddComponent<SongBrowserMasterViewController>();
        }

        public static SongBrowserMasterViewController Instance;

        /// <summary>
        /// Builds the UI for this plugin.
        /// </summary>
        protected override void Awake()
        {
            _log.Debug("Awake()");

            base.Awake();

            InitModel();

            _uiInitialized = false;
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;

            _simpleDialogPromptViewControllerPrefab = Resources.FindObjectsOfTypeAll<SimpleDialogPromptViewController>().First();

            this._deleteDialog = UnityEngine.Object.Instantiate<SimpleDialogPromptViewController>(this._simpleDialogPromptViewControllerPrefab);
            this._deleteDialog.gameObject.SetActive(false);
        }

        /// <summary>
        /// Override DidActivate to inject our UI elements.
        /// </summary>
        protected override void DidActivate()
        {
            _log.Debug("DidActivate()");

            if (!_uiInitialized)
            {
                CreateUI();
            }


            try
            {
                //if (scene.buildIndex == SongBrowserMasterViewController.MenuIndex)
                {
                    _log.Debug("SceneManagerOnActiveSceneChanged - Setting Up UI");

                    this._songListViewController.didSelectSongEvent += OnDidSelectSongEvent;

                    UpdateSongList();
                }
            }
            catch (Exception e)
            {
                _log.Exception("Exception during scene change: " + e);
            }

            base.DidActivate();
        }

        /// <summary>
        /// 
        /// </summary>
        private void InitModel()
        {
            if (_model == null)
            {
                _model = new SongBrowserModel();
            }
            _model.Init(new DataAccess.BeatSaberSongList());
        }

        /// <summary>
        /// Builds the SongBrowser UI
        /// </summary>
        public void CreateUI()
        {
            _log.Debug("CreateUI");

            try
            {                               
                RectTransform rect = this.transform as RectTransform;

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
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Favorite", 3, "AllDirectionsIcon", 30f, 77.5f, 16f, 5f, SongSortMode.Favorites, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Song", 3, "AllDirectionsIcon", 14f, 77.5f, 16f, 5f, SongSortMode.Default, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Author", 3, "AllDirectionsIcon", -2f, 77.5f, 16f, 5f, SongSortMode.Author, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Original", 3, "AllDirectionsIcon", -18f, 77.5f, 16f, 5f, SongSortMode.Original, onSortButtonClickEvent),
                    UIBuilder.CreateSortButton(rect, "PlayButton", "Newest", 3, "AllDirectionsIcon", -34f, 77.5f, 16f, 5f, SongSortMode.Newest, onSortButtonClickEvent),
                };

                // Creaate Add to Favorites Button
                _log.Debug("Creating add to favorites button...");

                RectTransform transform = _songDetailViewController.transform as RectTransform;
                _addFavoriteButton = UIBuilder.CreateUIButton(transform, "QuitButton", SongBrowserApplication.Instance.ButtonTemplate);
                (_addFavoriteButton.transform as RectTransform).anchoredPosition = new Vector2(45f, 9f);
                (_addFavoriteButton.transform as RectTransform).sizeDelta = new Vector2(10f, 5.0f);
                
                if (_addFavoriteButtonText == null)
                {
                    _log.Debug("Determinng if first selected song is a favorite.");
                    LevelStaticData level = getSelectedSong();
                    if (level != null)
                    {
                        RefreshAddFavoriteButton(level.levelId);
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

                transform = _songDetailViewController.transform as RectTransform;
                _deleteButton = UIBuilder.CreateUIButton(transform, "QuitButton", SongBrowserApplication.Instance.ButtonTemplate);
                (_deleteButton.transform as RectTransform).anchoredPosition = new Vector2(45f, 0f);
                (_deleteButton.transform as RectTransform).sizeDelta = new Vector2(16f, 5f);
                UIBuilder.SetButtonText(ref _deleteButton, "Delete");
                UIBuilder.SetButtonTextSize(ref _deleteButton, 3);
                UIBuilder.SetButtonIconEnabled(ref _deleteButton, false);
                _deleteButton.onClick.RemoveAllListeners();
                _deleteButton.onClick.AddListener(delegate () {
                    HandleDeleteSelectedSong();
                });

                RefreshUI();
                _uiInitialized = true;
            }
            catch (Exception e)
            {
                _log.Exception("Exception CreateUI: {0}\n{1}", e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// Bind to some UI events.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="scene"></param>
        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
            _uiInitialized = false;
        }

        /// <summary>
        /// Adjust UI based on song selected.
        /// Various ways of detecting if a song is not properly selected.  Seems most hit the first one.
        /// </summary>
        /// <param name="songListViewController"></param>
        private void OnDidSelectSongEvent(SongListViewController songListViewController)
        {
            LevelStaticData level = getSelectedSong();
            if (level == null)
            {
                _log.Debug("No song selected?");
                return;
            }

            if (_model.Settings == null)
            {
                _log.Debug("Settings not instantiated yet?");
                return;
            }

            RefreshAddFavoriteButton(level.levelId);
        }

        /// <summary>
        /// Return LevelStaticData or null.
        /// </summary>
        private LevelStaticData getSelectedSong()
        {
            // song browser not presenting
            if (!this.beingPresented)
            {
                return null;
            }

            /*if (this._levelsStaticData == null)
            {
                return null;
            }*/

            int selectedIndex = this.GetSelectedSongIndex();
            if (selectedIndex < 0)
            {
                return null;
            }

            LevelStaticData level = this.GetLevelStaticDataForSelectedSong();
            return level;
        }

        /// <summary>
        /// Pop up a delete dialog.
        /// </summary>
        private void HandleDeleteSelectedSong()
        {
            int selectedIndex = this.GetSelectedSongIndex();
            if (selectedIndex < 0)
            {
                return;
            }

            LevelStaticData level = this.GetLevelStaticDataForSelectedSong();
            SongLoaderPlugin.CustomSongInfo songInfo = _model.LevelIdToCustomSongInfos[level.levelId];

            this._deleteDialog.Init("Delete song warning!", String.Format("<color=#00AAFF>Permanently delete song: {0}</color>\n  Do you want to continue?", songInfo.songName), "YES", "NO");

            this._deleteDialog.didFinishEvent += this.HandleSimpleDialogPromptViewControllerDidFinish;
            this.PresentModalViewController(this._deleteDialog, null, false);
        }

        /// <summary>
        /// Handle delete dialog resolution.
        /// </summary>
        /// <param name="viewController"></param>
        /// <param name="ok"></param>
        public virtual void HandleSimpleDialogPromptViewControllerDidFinish(SimpleDialogPromptViewController viewController, bool ok)
        {
            viewController.didFinishEvent -= this.HandleSimpleDialogPromptViewControllerDidFinish;
            if (!ok)
            {
                viewController.DismissModalViewController(null, false);
            }
            else
            {
                LevelStaticData level = this.GetLevelStaticDataForSelectedSong();
                SongLoaderPlugin.CustomSongInfo songInfo = _model.LevelIdToCustomSongInfos[level.levelId];

                viewController.DismissModalViewController(null, false);
                _log.Debug("DELETING: {0}", songInfo.path);
                //Directory.Delete(songInfo.path);
            }
        }

        /// <summary>
        /// Add/Remove song from favorites depending on if it already exists.
        /// </summary>
        private void ToggleSongInFavorites()
        {
            LevelStaticData songInfo = this.GetLevelStaticDataForSelectedSong();
            if (_model.Settings.favorites.Contains(songInfo.levelId))
            {
                _log.Info("Remove {0} from favorites", songInfo.name);
                _model.Settings.favorites.Remove(songInfo.levelId);
                _addFavoriteButtonText = "+1";
            }
            else
            {
                _log.Info("Add {0} to favorites", songInfo.name);
                _model.Settings.favorites.Add(songInfo.levelId);
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
        public void RefreshSongList(List<LevelStaticData> songList)
        {
            _log.Debug("Attempting to refresh the song list view.");
            try
            {
                // Check a couple of possible situations that we can't refresh
                if (!this.beingPresented)
                {
                    _log.Debug("No song list to refresh.");
                    return;
                }
                             
                // Convert to Array once in-case this is costly.
                LevelStaticData[] songListArray = songList.ToArray();
                
                // Store on song browser
                this._levelsStaticData = songListArray;
                this._songListViewController.Init(songListArray);

                // Refresh UI Elements in case something changed.
                RefreshAddFavoriteButton(songList[0].levelId);

                // Might not be fully presented yet.
                SongListTableView songListTableView = this._songListViewController.GetComponentInChildren<SongListTableView>();
                if (songListTableView == null || !songListTableView.isActiveAndEnabled)
                {
                    _log.Debug("SongListTableView not presenting yet, cannot refresh view yet.");
                    return;
                }

                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(songListTableView, "_tableView");
                if (tableView == null)
                {
                    _log.Debug("TableView not presenting yet, cannot refresh view yet.");
                    return;
                }

                // Refresh the list views and its table view
                songListTableView.SetLevels(songListArray);
                tableView.ScrollToRow(0, false);
                tableView.ReloadData();

                // Clear Force selection of index 0 so we don't end up in a weird state.
                //songListTableView.ClearSelection();
                _songListViewController.SelectSong(0);
                this.HandleSongListDidSelectSong(_songListViewController);
            }
            catch (Exception e)
            {
                _log.Exception("Exception refreshing song list: {0}", e.Message);
            }
        }

        /// <summary>
        /// Helper for updating the model (which updates the song list)
        /// </summary>
        public void UpdateSongList()
        {
            _model.UpdateSongLists(true);
            RefreshSongList(_model.SortedSongList);
            RefreshUI();
        }

        /// <summary>
        /// 
        /// </summary>
        private void Update()
        {
            CheckDebugUserInput();
        }

        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void CheckDebugUserInput()
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
                _deleteButton.onClick.Invoke();
            }

            // z,x,c,v can be used to get into a song, b will hit continue button after song ends
            if (Input.GetKeyDown(KeyCode.C))
            {                
                _songListViewController.SelectSong(0);
                this.HandleSongListDidSelectSong(_songListViewController);

                DifficultyViewController _difficultyViewController = Resources.FindObjectsOfTypeAll<DifficultyViewController>().First();
                _difficultyViewController.SelectDifficulty(LevelStaticData.Difficulty.Hard);
                this.HandleDifficultyViewControllerDidSelectDifficulty(_difficultyViewController);
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                this.HandleSongDetailViewControllerDidPressPlayButton(_songDetailViewController);
            }

            // change song index
            if (Input.GetKeyDown(KeyCode.N))
            {
                int newIndex = this.GetSelectedSongIndex() - 1;

                _songListViewController.SelectSong(newIndex);
                this.HandleSongListDidSelectSong(_songListViewController);

                SongListTableView songTableView = Resources.FindObjectsOfTypeAll<SongListTableView>().First();
                _songListViewController.HandleSongListTableViewDidSelectRow(songTableView, newIndex);
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                int newIndex = this.GetSelectedSongIndex() + 1;

                _songListViewController.SelectSong(newIndex);
                this.HandleSongListDidSelectSong(_songListViewController);

                SongListTableView songTableView = Resources.FindObjectsOfTypeAll<SongListTableView>().First();
                _songListViewController.HandleSongListTableViewDidSelectRow(songTableView, newIndex);
            }

            // add to favorites
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleSongInFavorites();
            }
        }
    }
}
 
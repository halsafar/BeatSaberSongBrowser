using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text;
using HMUI;
using System.Text.RegularExpressions;
using System.IO;

namespace SongBrowserPlugin
{
    public class SongSortButton
    {
        public SongSortMode SortMode;
        public Button Button;
    }

    public class SongBrowserMasterViewController : MonoBehaviour
    {       
        // Which Scene index to run on
        public const int MenuIndex = 1;

        private Logger _log = new Logger("SongBrowserMasterViewController");

        // Private UI fields
        private SongSelectionMasterViewController _songSelectionMasterView;
        private SongDetailViewController _songDetailViewController;
        private SongListViewController _songListViewController;
        private MainMenuViewController _mainMenuViewController;

        private Dictionary<String, Sprite> _icons;

        private Button _buttonInstance;

        private List<SongSortButton> _sortButtonGroup;
        
        private Button _addFavoriteButton;
        private String _addFavoriteButtonText = null;

        // Model
        private SongBrowserModel _model;

        /// <summary>
        /// Unity OnLoad
        /// </summary>
        public static void OnLoad()
        {
            if (Instance != null) return;
            new GameObject("Song Browser").AddComponent<SongBrowserMasterViewController>();
        }

        public static SongBrowserMasterViewController Instance;

        /// <summary>
        /// Builds the UI for this plugin.
        /// </summary>
        private void Awake()
        {
            _log.Debug("Awake()");

            Instance = this;
          
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;

            SongLoaderPlugin.SongLoader.SongsLoaded.AddListener(OnSongLoaderLoadedSongs);

            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Get a handle to the view controllers we are going to add elements to.
        /// </summary>
        public void AcquireUIElements()
        {
            _icons = new Dictionary<String, Sprite>();
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (_icons.ContainsKey(sprite.name))
                {
                    continue;
                }
                _icons.Add(sprite.name, sprite);
            }

            try
            {
                _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PlayButton"));
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _songSelectionMasterView = Resources.FindObjectsOfTypeAll<SongSelectionMasterViewController>().First();                
                _songDetailViewController = Resources.FindObjectsOfTypeAll<SongDetailViewController>().First();
                _songListViewController = Resources.FindObjectsOfTypeAll<SongListViewController>().First();
            }
            catch (Exception e)
            {
                _log.Exception("Exception AcquireUIElements(): " + e);
            }
        }

        /// <summary>
        /// Builds the SongBrowser UI
        /// </summary>
        public void CreateUI()
        {
            _log.Debug("CreateUI");

            // _icons.ForEach(i => Console.WriteLine(i.ToString()));

            try
            {                               
                RectTransform rect = _songSelectionMasterView.transform as RectTransform;

                // Create Sorting Songs By-Buttons
                _sortButtonGroup = new List<SongSortButton>
                {
                    CreateSortButton(rect, "PlayButton", "Fav", 3, "AllDirectionsIcon", 30f, 77.5f, 15f, 5f, SongSortMode.Favorites),
                    CreateSortButton(rect, "PlayButton", "Def", 3, "AllDirectionsIcon", 15f, 77.5f, 15f, 5f, SongSortMode.Default),
                    CreateSortButton(rect, "PlayButton", "Org", 3, "AllDirectionsIcon", 0f, 77.5f, 15f, 5f, SongSortMode.Original),
                    CreateSortButton(rect, "PlayButton", "New", 3, "AllDirectionsIcon", -15f, 77.5f, 15f, 5f, SongSortMode.Newest)
                };

                // Creaate Add to Favorites Button
                RectTransform transform = _songDetailViewController.transform as RectTransform;
                _addFavoriteButton = UIBuilder.CreateUIButton(transform, "QuitButton", _buttonInstance);
                (_addFavoriteButton.transform as RectTransform).anchoredPosition = new Vector2(45f, 5f);
                (_addFavoriteButton.transform as RectTransform).sizeDelta = new Vector2(10f, 10f);                

                if (_addFavoriteButtonText == null)
                {
                    LevelStaticData level = getSelectedSong();
                    RefreshAddFavoriteButton(level);
                }
                
                UIBuilder.SetButtonText(ref _addFavoriteButton, _addFavoriteButtonText);
                //UIBuilder.SetButtonIcon(ref _addFavoriteButton, _icons["AllDirectionsIcon"]);
                UIBuilder.SetButtonTextSize(ref _addFavoriteButton, 3);
                UIBuilder.SetButtonIconEnabled(ref _addFavoriteButton, false);

                _addFavoriteButton.onClick.RemoveAllListeners();
                _addFavoriteButton.onClick.AddListener(delegate () {                    
                    ToggleSongInFavorites();
                });

                RefreshUI();
            }
            catch (Exception e)
            {
                _log.Exception("Exception CreateUI: " + e.Message);
            }
        }

        /// <summary>
        /// Generic create sort button.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="templateButtonName"></param>
        /// <param name="buttonText"></param>
        /// <param name="iconName"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="action"></param>
        private SongSortButton CreateSortButton(RectTransform rect, string templateButtonName, string buttonText, float fontSize, string iconName, float x, float y, float w, float h, SongSortMode sortMode)
        {
            SongSortButton sortButton = new SongSortButton();
            Button newButton = UIBuilder.CreateUIButton(rect, templateButtonName, _buttonInstance);
            
            newButton.interactable = true;
            (newButton.transform as RectTransform).anchoredPosition = new Vector2(x, y);
            (newButton.transform as RectTransform).sizeDelta = new Vector2(w, h);

            UIBuilder.SetButtonText(ref newButton, buttonText);
            //UIBuilder.SetButtonIconEnabled(ref _originalButton, false);
            UIBuilder.SetButtonIcon(ref newButton, _icons[iconName]);
            UIBuilder.SetButtonTextSize(ref newButton, fontSize);

            newButton.onClick.RemoveAllListeners();
            newButton.onClick.AddListener(delegate () {
                _log.Debug("Sort button - {0} - pressed.", sortMode.ToString());
                _model.Settings.sortMode = sortMode;
                _model.Settings.Save();
                UpdateSongList();
            });

            sortButton.Button = newButton;
            sortButton.SortMode = sortMode;

            return sortButton;
        }

        /// <summary>
        /// Bind to some UI events.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="scene"></param>
        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
            //_log.Debug("scene.buildIndex==" + scene.buildIndex);
            try
            {
                if (scene.buildIndex == SongBrowserMasterViewController.MenuIndex)
                {
                    _log.Debug("SceneManagerOnActiveSceneChanged - Setting Up UI");

                    AcquireUIElements();

                    if (_model == null)
                    {
                        _model = new SongBrowserModel();
                    }
                    _model.Init(new DataAccess.BeatSaberSongList() /*_songSelectionMasterView, _songListViewController*/);

                    CreateUI();
                                                            
                    _songListViewController.didSelectSongEvent += OnDidSelectSongEvent;
                }
            }
            catch (Exception e)
            {
                _log.Exception("Exception during scene change: " + e);
            }       
        }

        /// <summary>
        /// Song Loader has loaded the songs, lets sort them.
        /// </summary>
        private void OnSongLoaderLoadedSongs()
        {
            _log.Debug("OnSongLoaderLoadedSongs");            
            //RefreshAddFavoriteButton(sortedSongList[0]);

            // Call into SongLoaderPlugin to get all the song info.
            try
            {
                UpdateSongList();
            }
            catch (Exception e)
            {
                _log.Exception("Exception trying to sort by newest: {0}", e);
            }
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

            RefreshAddFavoriteButton(level);
        }

        /// <summary>
        /// Return LevelStaticData or null.
        /// </summary>
        private LevelStaticData getSelectedSong()
        {
            // song list not even visible
            if (!_songSelectionMasterView.isActiveAndEnabled)
            {
                return null;
            }

            int selectedIndex = _songSelectionMasterView.GetSelectedSongIndex();
            if (selectedIndex < 0)
            {
                return null;
            }

            LevelStaticData level = _songSelectionMasterView.GetLevelStaticDataForSelectedSong();

            return level;
        }

        /// <summary>
        /// Add/Remove song from favorites depending on if it already exists.
        /// </summary>
        private void ToggleSongInFavorites()
        {
            LevelStaticData songInfo = _songSelectionMasterView.GetLevelStaticDataForSelectedSong();
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
        private void RefreshAddFavoriteButton(LevelStaticData level)
        {
            var levelId = _songListViewController.levelId;
            if (levelId == null)
            {
                if (level != null)
                {
                    levelId = level.levelId;
                }
            }

            //if (level != null) _log.Debug(level.songName);
            //if (level != null)
            //    _log.Debug("Level.id=" + level.levelId);
            //_log.Debug("_songListViewController.levelId=" + _songListViewController.levelId);

            if (levelId == null)
            {
                _addFavoriteButtonText = "0";
                return;
            }

            if (levelId == null)
            {
                levelId = level.levelId;
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
            LevelStaticData[] songListArray = songList.ToArray();

            _log.Debug("Attempting to refresh the song list view.");
            try
            {
                if (!_songSelectionMasterView.isActiveAndEnabled)
                {
                    _log.Debug("No song list to refresh.");
                    return;
                }

                // Refresh the master view
                bool useLocalLeaderboards = ReflectionUtil.GetPrivateField<bool>(_songSelectionMasterView, "_useLocalLeaderboards");
                bool showDismissButton = true;
                bool showPlayerStats = ReflectionUtil.GetPrivateField<bool>(_songSelectionMasterView, "_showPlayerStats");
                GameplayMode gameplayMode = ReflectionUtil.GetPrivateField<GameplayMode>(_songSelectionMasterView, "_gameplayMode");

                _songSelectionMasterView.Init(
                    _songSelectionMasterView.levelId,
                    _songSelectionMasterView.difficulty,
                    songListArray,
                    useLocalLeaderboards, showDismissButton, showPlayerStats, gameplayMode);

                // Refresh the song list
                SongListTableView songTableView = _songListViewController.GetComponentInChildren<SongListTableView>();
                if (songTableView == null)
                {
                    return;
                }

                ReflectionUtil.SetPrivateField(songTableView, "_levels", songListArray);
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(songTableView, "_tableView");
                if (tableView == null)
                {
                    return;
                }

                tableView.ReloadData();

                // Clear Force selection of index 0 so we don't end up in a weird state.
                songTableView.ClearSelection();
                _songListViewController.SelectSong(0);
                _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);

                RefreshUI();
                RefreshAddFavoriteButton(songList[0]);
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
        }

        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void Update()
        {
            // cycle sort modes
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (_model.Settings.sortMode == SongSortMode.Favorites)
                    _model.Settings.sortMode = SongSortMode.Newest;
                else if (_model.Settings.sortMode == SongSortMode.Newest)
                    _model.Settings.sortMode = SongSortMode.Original;
                else if (_model.Settings.sortMode == SongSortMode.Original)
                    _model.Settings.sortMode = SongSortMode.Default;
                else if (_model.Settings.sortMode == SongSortMode.Default)
                    _model.Settings.sortMode = SongSortMode.Favorites;

                UpdateSongList();
            }

            // z,x,c,v can be used to get into a song, b will hit continue button after song ends
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Button _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "SoloButton"));
                _buttonInstance.onClick.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                Button _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "FreePlayButton"));
                _buttonInstance.onClick.Invoke();                
            }

            if (Input.GetKeyDown(KeyCode.C))
            {                
                _songListViewController.SelectSong(0);
                _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);

                DifficultyViewController _difficultyViewController = Resources.FindObjectsOfTypeAll<DifficultyViewController>().First();
                _difficultyViewController.SelectDifficulty(LevelStaticData.Difficulty.Hard);
                _songSelectionMasterView.HandleDifficultyViewControllerDidSelectDifficulty(_difficultyViewController);
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                _songSelectionMasterView.HandleSongDetailViewControllerDidPressPlayButton(_songDetailViewController);
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                Button _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "ContinueButton"));
                _buttonInstance.onClick.Invoke();
            }

            // change song index
            if (Input.GetKeyDown(KeyCode.N))
            {
                int newIndex = _songSelectionMasterView.GetSelectedSongIndex() - 1;

                _songListViewController.SelectSong(newIndex);
                _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);

                SongListTableView songTableView = Resources.FindObjectsOfTypeAll<SongListTableView>().First();
                _songListViewController.HandleSongListTableViewDidSelectRow(songTableView, newIndex);
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                int newIndex = _songSelectionMasterView.GetSelectedSongIndex() + 1;

                _songListViewController.SelectSong(newIndex);
                _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);

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
 
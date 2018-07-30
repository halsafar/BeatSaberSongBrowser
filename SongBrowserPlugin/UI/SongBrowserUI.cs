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
using System.Security.Cryptography;
using System.Text;

namespace SongBrowserPlugin.UI
{
    /// <summary>
    /// Hijack the flow coordinator.  Have access to all StandardLevel easily.
    /// </summary>
    public class SongBrowserUI : MonoBehaviour
    {
        // Logging
        public const String Name = "SongBrowserUI";

        private const float SEGMENT_PERCENT = 0.1f;

        private Logger _log = new Logger(Name);

        // Beat Saber UI Elements
        private StandardLevelSelectionFlowCoordinator _levelSelectionFlowCoordinator;
        private StandardLevelListViewController _levelListViewController;
        private StandardLevelDetailViewController _levelDetailViewController;
        private StandardLevelDifficultyViewController _levelDifficultyViewController;
        private StandardLevelSelectionNavigationController _levelSelectionNavigationController;
        private StandardLevelListTableView _levelListTableView;
        private RectTransform _tableViewRectTransform;
        private Button _tableViewPageUpButton;
        private Button _tableViewPageDownButton;

        // New UI Elements
        private List<SongSortButton> _sortButtonGroup;
        private Button _addFavoriteButton;
        private String _addFavoriteButtonText = null;
        private SimpleDialogPromptViewController _simpleDialogPromptViewControllerPrefab;
        private SimpleDialogPromptViewController _deleteDialog;
        private Button _deleteButton;        
        private Button _pageUpTenPercent;
        private Button _pageDownTenPercent;
        private SearchKeyboardViewController _searchViewController;


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

                if (_levelListTableView == null)
                {
                    _levelListTableView = this._levelListViewController.GetComponentInChildren<StandardLevelListTableView>();
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
                // Gather some transforms and templates to use.
                RectTransform sortButtonTransform = this._levelSelectionNavigationController.transform as RectTransform;
                RectTransform otherButtonTransform = this._levelDetailViewController.transform as RectTransform;
                Button sortButtonTemplate = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PlayButton"));
                Button otherButtonTemplate = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "QuitButton"));

                // Resize some of the UI
                _tableViewRectTransform = _levelListViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "TableViewContainer");
                _tableViewRectTransform.sizeDelta = new Vector2(0f, -20f);
                _tableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);

                _tableViewPageUpButton = _tableViewRectTransform.GetComponentsInChildren<Button>().First(x => x.name == "PageUpButton");
                (_tableViewPageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -1f);

                _tableViewPageDownButton = _tableViewRectTransform.GetComponentsInChildren<Button>().First(x => x.name == "PageDownButton");
                (_tableViewPageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 1f);

                // Create Sorting Songs By-Buttons
                _log.Debug("Creating sort by buttons...");
                
                Sprite arrowIcon = SongBrowserApplication.Instance.CachedIcons["ArrowIcon"];

                float fontSize = 2.5f;
                float buttonWidth = 14.0f;
                float buttonHeight = 5.0f;
                float buttonX = 68.0f;
                float buttonY = 74.5f;

                string[] buttonNames = new string[]
                {
                    "Favorite", "Song", "Author", "Original", "Newest", "Plays", "Random", "Search"
                };

                SongSortMode[] sortModes = new SongSortMode[]
                {
                    SongSortMode.Favorites, SongSortMode.Default, SongSortMode.Author, SongSortMode.Original, SongSortMode.Newest, SongSortMode.PlayCount, SongSortMode.Random, SongSortMode.Search
                };

                System.Action<SongSortMode>[] onClickEvents = new Action<SongSortMode>[]
                {
                    onSortButtonClickEvent, onSortButtonClickEvent, onSortButtonClickEvent, onSortButtonClickEvent, onSortButtonClickEvent, onSortButtonClickEvent, onSortButtonClickEvent, onSearchButtonClickEvent
                };

                _sortButtonGroup = new List<SongSortButton>();
                for (int i = 0; i < buttonNames.Length; i++)
                {
                    _sortButtonGroup.Add(UIBuilder.CreateSortButton(sortButtonTransform, sortButtonTemplate, arrowIcon, buttonNames[i], fontSize, buttonX, buttonY, buttonWidth, buttonHeight, sortModes[i], onClickEvents[i]));
                    buttonX -= buttonWidth;
                }

                // Creaate Add to Favorites Button
                _log.Debug("Creating add to favorites button...");                
                _addFavoriteButton = UIBuilder.CreateUIButton(otherButtonTransform, otherButtonTemplate);
                (_addFavoriteButton.transform as RectTransform).anchoredPosition = new Vector2(40f, 5.75f);
                (_addFavoriteButton.transform as RectTransform).sizeDelta = new Vector2(10f, 10f);
                UIBuilder.SetButtonText(ref _addFavoriteButton, _addFavoriteButtonText);
                UIBuilder.SetButtonTextSize(ref _addFavoriteButton, fontSize);
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
                _deleteButton = UIBuilder.CreateUIButton(otherButtonTransform, otherButtonTemplate);
                (_deleteButton.transform as RectTransform).anchoredPosition = new Vector2(46f, 0f);
                (_deleteButton.transform as RectTransform).sizeDelta = new Vector2(15f, 5f);
                UIBuilder.SetButtonText(ref _deleteButton, "Delete");
                UIBuilder.SetButtonTextSize(ref _deleteButton, fontSize);
                UIBuilder.SetButtonIconEnabled(ref _deleteButton, false);
                _deleteButton.onClick.RemoveAllListeners();
                _deleteButton.onClick.AddListener(delegate () {
                    HandleDeleteSelectedLevel();
                });

                // Create fast scroll buttons
                _pageUpTenPercent = UIBuilder.CreatePageButton(sortButtonTransform, otherButtonTemplate, arrowIcon, 15, 67.5f, 6.0f, 5.5f, 1.5f, 1.5f, 180);
                _pageUpTenPercent.onClick.AddListener(delegate () {
                    this.JumpSongList(-1, SEGMENT_PERCENT);
                });

                _pageDownTenPercent = UIBuilder.CreatePageButton(sortButtonTransform, otherButtonTemplate, arrowIcon, 15, 0.5f, 6.0f, 5.5f, 1.5f, 1.5f, 0);
                _pageDownTenPercent.onClick.AddListener(delegate () {
                    this.JumpSongList(1, SEGMENT_PERCENT);
                });

                RefreshSortButtonUI();
            }
            catch (Exception e)
            {
                _log.Exception("Exception CreateUIElements:", e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void onSortButtonClickEvent(SongSortMode sortMode)
        {
            _log.Debug("Sort button - {0} - pressed.", sortMode.ToString());
            SongBrowserModel.LastSelectedLevelId = null;

            if (_model.Settings.sortMode == sortMode)
            {
                _model.ToggleInverting();
            }

            _model.Settings.sortMode = sortMode;
            _model.Settings.Save();

            UpdateSongList();
            RefreshSongList();
        }

        /// <summary>
        /// Saerch button clicked.  
        /// </summary>
        /// <param name="sortMode"></param>
        private void onSearchButtonClickEvent(SongSortMode sortMode)
        {
            _model.Settings.sortMode = sortMode;
            _model.Settings.Save();

            this.ShowSearchKeyboard();
        }

        /// <summary>
        /// Adjust UI based on level selected.
        /// Various ways of detecting if a level is not properly selected.  Seems most hit the first one.
        /// </summary>
        private void OnDidSelectLevelEvent(StandardLevelListViewController view, IStandardLevel level)
        {
            _log.Trace("OnDidSelectLevelEvent()");
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
            RefreshQuickScrollButtons();
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
                string customSongsPath = Path.Combine(Environment.CurrentDirectory, "CustomSongs");
                IStandardLevel level = this._levelListViewController.selectedLevel;                
                SongLoaderPlugin.OverrideClasses.CustomLevel customLevel = _model.LevelIdToCustomSongInfos[level.levelID];
                string songPath = customLevel.customSongInfo.path;
                bool isZippedSong = false;

                viewController.DismissModalViewController(null, false);
                _log.Debug("Deleting: {0}", songPath);

                if (!string.IsNullOrEmpty(songPath) && songPath.Contains("/.cache/"))
                {
                    isZippedSong = true;
                }

                if (isZippedSong)
                {
                    DirectoryInfo songHashDir = Directory.GetParent(songPath);
                    _log.Debug("Deleting zipped song cache: {0}", songHashDir.FullName);
                    Directory.Delete(songHashDir.FullName, true);

                    foreach (string file in Directory.GetFiles(customSongsPath, "*.zip"))
                    {
                        string hash = CreateMD5FromFile(file);
                        if (hash != null)
                        {
                            if (hash == songHashDir.Name)
                            {
                                _log.Debug("Deleting zipped song: {0}", file);
                                File.Delete(file);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    FileAttributes attr = File.GetAttributes(songPath);
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        _log.Debug("Deleting song: {0}", songPath);
                        Directory.Delete(songPath, true);
                    }
                }

                SongLoaderPlugin.SongLoader.Instance.RemoveSongWithPath(songPath);
                this.UpdateSongList();
                this.RefreshSongList();
            }
        }

        /// <summary>
        /// Create MD5 of a file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string CreateMD5FromFile(string path)
        {
            string hash = "";
            if (!File.Exists(path)) return null;
            using (MD5 md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte hashByte in hashBytes)
                    {
                        sb.Append(hashByte.ToString("X2"));
                    }

                    hash = sb.ToString();
                    return hash;
                }
            }
        }

        /// <summary>
        /// Display the search keyboard
        /// </summary>
        void ShowSearchKeyboard()
        {
            if (_searchViewController == null)
            {
                _searchViewController = UIBuilder.CreateViewController<SearchKeyboardViewController>("SearchKeyboardViewController");
                _searchViewController.searchButtonPressed += SearchViewControllerSearchButtonPressed;
                _searchViewController.backButtonPressed += SearchViewControllerbackButtonPressed;
            }

            _levelListViewController.navigationController.PresentModalViewController(_searchViewController, null, false);
        }

        /// <summary>
        /// Handle back button event from search keyboard.
        /// </summary>
        private void SearchViewControllerbackButtonPressed()
        {
 
        }

        /// <summary>
        /// Handle search.
        /// </summary>
        /// <param name="searchFor"></param>
        private void SearchViewControllerSearchButtonPressed(string searchFor)
        {
            _log.Debug("Searching for \"{0}\"...", searchFor);

            _model.Settings.searchTerms.Insert(0, searchFor);
            _model.Settings.Save();
            SongBrowserModel.LastSelectedLevelId = null;
            this.UpdateSongList();
            this.RefreshSongList();
        }

        /// <summary>
        /// Make big jumps in the song list.
        /// </summary>
        /// <param name="numJumps"></param>
        private void JumpSongList(int numJumps, float segmentPercent)
        {
            int totalSize = _model.SortedSongList.Count;
            int segmentSize = (int)(totalSize * segmentPercent);

            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelListTableView, "_tableView");
            HashSet<int> rows = tableView.GetPrivateField<HashSet<int>>("_selectedRows");
            int listSegment = (rows.First() / segmentSize);
            int newSegment = listSegment + numJumps;
            int newRow = 0;
            if (newSegment > 0)
            {
                newRow = Math.Min(newSegment * segmentSize, totalSize - 1);
            }                       

            _log.Debug("ListSegment: {0}, newRow: {1}", listSegment, newRow);

            this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[newRow].levelID);
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
        /// Update interactive state of the quick scroll buttons.
        /// </summary>
        private void RefreshQuickScrollButtons()
        {
            // Refresh the fast scroll buttons
            _pageUpTenPercent.interactable = _tableViewPageUpButton.interactable;
            _pageUpTenPercent.gameObject.SetActive(_tableViewPageUpButton.IsActive());
            _pageDownTenPercent.interactable = _tableViewPageDownButton.interactable;
            _pageDownTenPercent.gameObject.SetActive(_tableViewPageDownButton.IsActive());
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
        public void RefreshSortButtonUI()
        {
            // So far all we need to refresh is the sort buttons.
            foreach (SongSortButton sortButton in _sortButtonGroup)
            {
                UIBuilder.SetButtonBorder(ref sortButton.Button, Color.black);
                if (sortButton.SortMode == _model.Settings.sortMode)
                {
                    if (_model.InvertingResults)
                    {
                        UIBuilder.SetButtonBorder(ref sortButton.Button, Color.red);
                    }
                    else
                    {
                        UIBuilder.SetButtonBorder(ref sortButton.Button, Color.green);
                    }
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
                foreach (StandardLevelSO level in levels)
                    _log.Debug(level.levelID);
                StandardLevelListViewController songListViewController = this._levelSelectionFlowCoordinator.GetPrivateField<StandardLevelListViewController>("_levelListViewController");
                ReflectionUtil.SetPrivateField(_levelListTableView, "_levels", levels);
                ReflectionUtil.SetPrivateField(songListViewController, "_levels", levels);            
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelListTableView, "_tableView");
                tableView.ReloadData();                

                String selectedLevelID = null;
                if (SongBrowserModel.LastSelectedLevelId != null)
                {
                    selectedLevelID = SongBrowserModel.LastSelectedLevelId;
                    _log.Debug("Scrolling to row for level ID: {0}", selectedLevelID);                    
                }
                else
                {
                    if (levels.Length > 0)
                    {
                        selectedLevelID = levels.FirstOrDefault().levelID;
                    }
                }

                // HACK, seems like if 6 or less items scrolling to row causes the song list to disappear.
                if (levels.Length > 6 && !String.IsNullOrEmpty(selectedLevelID) && levels.Any(x => x.levelID == selectedLevelID))
                {
                    SelectAndScrollToLevel(_levelListTableView, selectedLevelID);
                }

                RefreshSortButtonUI();
            }
            catch (Exception e)
            {
                _log.Exception("Exception refreshing song list:", e);
            }
        }

        /// <summary>
        /// Scroll TableView to proper row, fire events.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="levelID"></param>
        private void SelectAndScrollToLevel(StandardLevelListTableView table, string levelID)
        {
            int row = table.RowNumberForLevelID(levelID);
            TableView tableView = table.GetComponentInChildren<TableView>();
            tableView.SelectRow(row, true);
            tableView.ScrollToRow(row, true);
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
            if (!this._levelListViewController.isActiveAndEnabled) return;
            CheckDebugUserInput();
        }

        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void CheckDebugUserInput()
        {
            try
            {
                bool isShiftKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

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

                if (Input.GetKeyDown(KeyCode.S))
                {
                    onSortButtonClickEvent(SongSortMode.Search);
                }

                // select current sort mode again (toggle inverting)
                if (Input.GetKeyDown(KeyCode.Y))
                {
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

                // accept delete
                if (Input.GetKeyDown(KeyCode.B) && _deleteDialog.isInViewControllerHierarchy)
                {
                    _deleteDialog.GetPrivateField<TextMeshProButton>("_okButton").button.onClick.Invoke();
                }

                // c,v can be used to get into a song
                if (Input.GetKeyDown(KeyCode.C))
                {
                    this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[0].levelID);                 
                    this._levelDifficultyViewController.HandleDifficultyTableViewDidSelectRow(null, 0);
                    this._levelSelectionFlowCoordinator.HandleDifficultyViewControllerDidSelectDifficulty(_levelDifficultyViewController, _model.SortedSongList[0].GetDifficultyLevel(LevelDifficulty.Easy));
                }

                if (Input.GetKeyDown(KeyCode.V))
                {
                    this._levelSelectionFlowCoordinator.HandleLevelDetailViewControllerDidPressPlayButton(this._levelDetailViewController);
                }

                // change song index
                if (isShiftKeyDown && Input.GetKeyDown(KeyCode.N))
                {
                    _pageUpTenPercent.onClick.Invoke();
                }
                else if (Input.GetKeyDown(KeyCode.N))
                {
                    _lastRow = (_lastRow - 1) != -1 ? (_lastRow - 1) % this._model.SortedSongList.Count : 0;
                    this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[_lastRow].levelID);
                }

                if (isShiftKeyDown && Input.GetKeyDown(KeyCode.M))
                {
                    _pageDownTenPercent.onClick.Invoke();
                }
                else if (Input.GetKeyDown(KeyCode.M))
                {
                    _lastRow = (_lastRow + 1) % this._model.SortedSongList.Count;
                    this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[_lastRow].levelID);
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
 
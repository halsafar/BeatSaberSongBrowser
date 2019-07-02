using Newtonsoft.Json;
using SimpleJSON;
using SongBrowser.DataAccess.BeatSaverApi;
using SongBrowser.Internals;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Logger = SongBrowser.Logging.Logger;
using Sprites = SongBrowser.UI.Base64Sprites;

namespace SongBrowser.DataAccess
{
    public static class PlaylistsCollection
    {
        public static List<Playlist> loadedPlaylists = new List<Playlist>();

        public static void ReloadPlaylists(bool fullRefresh = true)
        {
            try
            {
                List<string> playlistFiles = new List<string>();

                if (PluginConfig.beatDropInstalled)
                {
                    String beatDropPath = Path.Combine(PluginConfig.beatDropPlaylistsLocation, "playlists");
                    if (Directory.Exists(beatDropPath))
                    {
                        string[] beatDropJSONPlaylists = Directory.GetFiles(beatDropPath, "*.json");
                        string[] beatDropBPLISTPlaylists = Directory.GetFiles(beatDropPath, "*.bplist");
                        playlistFiles.AddRange(beatDropJSONPlaylists);
                        playlistFiles.AddRange(beatDropBPLISTPlaylists);
                        Logger.Log($"Found {beatDropJSONPlaylists.Length + beatDropBPLISTPlaylists.Length} playlists in BeatDrop folder");
                    }
                }

                string[] localJSONPlaylists = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Playlists"), "*.json");
                string[] localBPLISTPlaylists = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Playlists"), "*.bplist");
                playlistFiles.AddRange(localJSONPlaylists);
                playlistFiles.AddRange(localBPLISTPlaylists);

                Logger.Log($"Found {localJSONPlaylists.Length + localBPLISTPlaylists.Length} playlists in Playlists folder");

                if (fullRefresh)
                {
                    loadedPlaylists.Clear();

                    foreach (string path in playlistFiles)
                    {
                        try
                        {
                            Playlist playlist = Playlist.LoadPlaylist(path);
                            if (Path.GetFileName(path) == "favorites.json" && playlist.playlistTitle == "Your favorite songs")
                                continue;
                            loadedPlaylists.Add(playlist);
                            Logger.Log($"Found \"{playlist.playlistTitle}\" by {playlist.playlistAuthor}");
                        }
                        catch (Exception e)
                        {
                            Logger.Log($"Unable to parse playlist @ {path}! Exception: {e}");
                        }
                    }
                }
                else
                {
                    foreach (string path in playlistFiles)
                    {
                        if (!loadedPlaylists.Any(x => x.fileLoc == path))
                        {
                            Logger.Log("Found new playlist! Path: " + path);
                            try
                            {
                                Playlist playlist = Playlist.LoadPlaylist(path);
                                if (Path.GetFileName(path) == "favorites.json" && playlist.playlistTitle == "Your favorite songs")
                                    continue;
                                loadedPlaylists.Add(playlist);
                                Logger.Log($"Found \"{playlist.playlistTitle}\" by {playlist.playlistAuthor}");

                                if (SongCore.Loader.AreSongsLoaded)
                                {
                                    MatchSongsForPlaylist(playlist);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Log($"Unable to parse playlist @ {path}! Exception: {e}");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Exception("Unable to load playlists! Exception: " + e);
            }
        }

        public static void AddSongToPlaylist(Playlist playlist, PlaylistSong song)
        {
            playlist.songs.Add(song);
            if (playlist.playlistTitle == "Your favorite songs")
            {
                playlist.SavePlaylist();
            }
        }

        public static void RemoveLevelFromPlaylists(string levelId)
        {
            foreach (Playlist playlist in loadedPlaylists)
            {
                if (playlist.songs.Where(y => y.level != null).Any(x => x.level.levelID == levelId))
                {
                    PlaylistSong song = playlist.songs.First(x => x.level != null && x.level.levelID == levelId);
                    song.level = null;
                    song.levelId = "";
                }
                if (playlist.playlistTitle == "Your favorite songs")
                {
                    playlist.SavePlaylist();
                }
            }
        }

        public static void RemoveLevelFromPlaylist(Playlist playlist, string levelId)
        {
            if (playlist.songs.Where(y => y.level != null).Any(x => x.level.levelID == levelId))
            {
                PlaylistSong song = playlist.songs.First(x => x.level != null && x.level.levelID == levelId);
                song.level = null;
                song.levelId = "";
            }
            if (playlist.playlistTitle == "Your favorite songs")
            {
                playlist.SavePlaylist();
            }
        }

        public static void MatchSongsForPlaylist(Playlist playlist, bool matchAll = false)
        {
            //bananbread playlist id  
            if (!SongCore.Loader.AreSongsLoaded || SongCore.Loader.AreSongsLoading || playlist.playlistTitle == "All songs" || playlist.playlistTitle == "Your favorite songs") return;

            if (!playlist.songs.All(x => x.level != null) || matchAll)
            {
                playlist.songs.AsParallel().ForAll(x =>
                {
                    if (x.level == null || matchAll)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(x.levelId)) //check that we have levelId and if we do, try to match level
                            {
                                x.level = SongCore.Loader.CustomLevels.Values.FirstOrDefault(y => y.levelID == x.levelId);
                            }
                            if (x.level == null && !string.IsNullOrEmpty(x.hash)) //if level is still null, check that we have hash and if we do, try to match level
                            {
                                if (x.hash.Contains("custom_level"))
                                {
                                    x.hash = CustomHelpers.GetSongHash(x.hash);
                                }
                                x.level = SongCore.Loader.CustomLevels.Values.FirstOrDefault(y => string.Equals(CustomHelpers.GetSongHash(y.levelID), x.hash, StringComparison.OrdinalIgnoreCase));
                            }
                            if (x.level == null && !string.IsNullOrEmpty(x.key))
                            {
                                x.level = SongCore.Loader.CustomLevels.FirstOrDefault(y => y.Value.customLevelPath.Contains(x.key)).Value;

                                if (x.level != null && !String.IsNullOrEmpty(x.level.levelID))
                                {
                                    x.hash = CustomHelpers.GetSongHash(x.level.levelID);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Warning($"Unable to match song with {(string.IsNullOrEmpty(x.key) ? " unknown key!" : ("key " + x.key + " !"))}");
                        }
                    }
                });
            }

        }

        public static void MatchSongsForAllPlaylists(bool matchAll = false)
        {
            Logger.Log("Matching songs for all playlists!");
            Task.Run(() =>
            {
                for (int i = 0; i < loadedPlaylists.Count; i++)
                {
                    MatchSongsForPlaylist(loadedPlaylists[i], matchAll);
                }
            });
        }
    }

    public class PlaylistSong
    {
        public string key { get; set; }
        public string songName { get; set; }
        public string hash { get; set; }

        [NonSerialized]
        public string levelId;
        [NonSerialized]
        public CustomPreviewBeatmapLevel level;
        [NonSerialized]
        public bool oneSaber;
        [NonSerialized]
        public string path;

        public IEnumerator MatchKey()
        {
            if (!string.IsNullOrEmpty(key) || level == null || !(level is CustomPreviewBeatmapLevel))
                yield break;

            string songHash = null;
            if (!string.IsNullOrEmpty(hash))
            {
                songHash = hash;
            }
            else if (!string.IsNullOrEmpty(levelId))
            {
                songHash = CustomHelpers.GetSongHash(level.levelID);
            }
            
            if (songHash != null && SongDataCore.Plugin.BeatSaver.Data.Songs.ContainsKey(hash))
            {
                var song = SongDataCore.Plugin.BeatSaver.Data.Songs[hash];
                key = song.key;
            }
            else
            {
                // no more hitting api just to match a key.  We know the song hash.
                //yield return SongDownloader.Instance.RequestSongByLevelIDCoroutine(level.levelID.Split('_')[2], (Song bsSong) => { if (bsSong != null) key = bsSong.key; });
            }
        }
    }

    public class Playlist
    {
        public string playlistTitle { get; set; }
        public string playlistAuthor { get; set; }
        public string image { get; set; }
        public int playlistSongCount { get; set; }
        public List<PlaylistSong> songs { get; set; }
        public string fileLoc { get; set; }
        public string customDetailUrl { get; set; }
        public string customArchiveUrl { get; set; }

        [NonSerialized]
        public Sprite icon;

        public Playlist()
        {

        }

        public Playlist(JSONNode playlistNode)
        {
            string image = playlistNode["image"].Value;
            if (!string.IsNullOrEmpty(image))
            {
                try
                {
                    icon = Sprites.Base64ToSprite(image.Substring(image.IndexOf(",") + 1));
                }
                catch
                {
                    Logger.Exception("Unable to convert playlist image to sprite!");
                    icon = Sprites.BeastSaberLogo;
                }
            }
            else
            {
                icon = Sprites.BeastSaberLogo;
            }
            playlistTitle = playlistNode["playlistTitle"];
            playlistAuthor = playlistNode["playlistAuthor"];
            customDetailUrl = playlistNode["customDetailUrl"];
            customArchiveUrl = playlistNode["customArchiveUrl"];
            if (!string.IsNullOrEmpty(customDetailUrl))
            {
                if (!customDetailUrl.EndsWith("/"))
                    customDetailUrl += "/";
                Logger.Log("Found playlist with customDetailUrl! Name: " + playlistTitle + ", CustomDetailUrl: " + customDetailUrl);
            }
            if (!string.IsNullOrEmpty(customArchiveUrl) && customArchiveUrl.Contains("[KEY]"))
            {
                Logger.Log("Found playlist with customArchiveUrl! Name: " + playlistTitle + ", CustomArchiveUrl: " + customArchiveUrl);
            }

            songs = new List<PlaylistSong>();

            foreach (JSONNode node in playlistNode["songs"].AsArray)
            {
                PlaylistSong song = new PlaylistSong();
                song.key = node["key"];
                song.songName = node["songName"];
                song.hash = node["hash"];
                song.levelId = node["levelId"];

                songs.Add(song);
            }

            if (playlistNode["playlistSongCount"] != null)
            {
                playlistSongCount = playlistNode["playlistSongCount"].AsInt;
            }
            if (playlistNode["fileLoc"] != null)
                fileLoc = playlistNode["fileLoc"];

            if (playlistNode["playlistURL"] != null)
                fileLoc = playlistNode["playlistURL"];
        }

        public static Playlist LoadPlaylist(string path)
        {
            Playlist playlist = new Playlist(JSON.Parse(File.ReadAllText(path)));
            playlist.fileLoc = path;
            return playlist;
        }

        public void SavePlaylist(string path = "")
        {            
            SharedCoroutineStarter.instance.StartCoroutine(SavePlaylistCoroutine(path));
        }

        public IEnumerator SavePlaylistCoroutine(string path = "")
        {
            Logger.Log($"Saving playlist \"{playlistTitle}\"...");
            try
            {
                if (icon != null)
                {
                    image = Sprites.SpriteToBase64(icon);
                }
                else
                {
                    image = null;
                }
                playlistSongCount = songs.Count;
            }
            catch (Exception e)
            {
                Logger.Exception("Unable to save playlist! Exception: " + e);
                yield break;
            }

            // match key if we can, not really that important anymore
            if (SongDataCore.Plugin.BeatSaver.Data.Songs.Count > 0)
            {
                foreach (PlaylistSong song in songs)
                {
                    yield return song.MatchKey();
                }
            }

            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    fileLoc = Path.GetFullPath(path);
                }

                File.WriteAllText(fileLoc, JsonConvert.SerializeObject(this, Formatting.Indented));

                Logger.Log("Playlist saved!");
            }
            catch (Exception e)
            {
                Logger.Exception("Unable to save playlist! Exception: " + e);
                yield break;
            }
        }

        public bool PlaylistEqual(object obj)
        {
            if (obj == null) return false;

            var playlist = obj as Playlist;

            if (playlist == null) return false;

            int songCountThis = (songs != null ? (songs.Count > 0 ? songs.Count : playlistSongCount) : playlistSongCount);
            int songCountObj = (playlist.songs != null ? (playlist.songs.Count > 0 ? playlist.songs.Count : playlist.playlistSongCount) : playlist.playlistSongCount);

            return playlistTitle == playlist.playlistTitle &&
                   playlistAuthor == playlist.playlistAuthor &&
                   songCountThis == songCountObj;
        }

        public void CreateNew(String fileLoc)
        {
            File.WriteAllText(fileLoc, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}

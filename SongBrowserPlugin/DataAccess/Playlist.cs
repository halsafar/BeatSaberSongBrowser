﻿using Newtonsoft.Json;
using SimpleJSON;
using SongBrowser.DataAccess.BeatSaverApi;
using SongLoaderPlugin;
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

                                if (SongLoader.AreSongsLoaded)
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
            if (!SongLoader.AreSongsLoaded || SongLoader.AreSongsLoading || playlist.playlistTitle == "All songs" || playlist.playlistTitle == "Your favorite songs") return;
            Logger.Log("Started matching songs for playlist \"" + playlist.playlistTitle + "\"...");
            if (!playlist.songs.All(x => x.level != null) || matchAll)
            {
                Stopwatch execTime = new Stopwatch();
                execTime.Start();
                playlist.songs.AsParallel().ForAll(x =>
                {
                    if (x.level == null || matchAll)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(x.levelId)) //check that we have levelId and if we do, try to match level
                            {
                                x.level = SongLoader.CustomLevels.FirstOrDefault(y => y.levelID == x.levelId);
                            }
                            if (x.level == null && !string.IsNullOrEmpty(x.hash)) //if level is still null, check that we have hash and if we do, try to match level
                            {
                                x.level = SongLoader.CustomLevels.FirstOrDefault(y => y.levelID.StartsWith(x.hash.ToUpper()));
                            }
                            if (x.level == null && !string.IsNullOrEmpty(x.key)) //if level is still null, check that we have key and if we do, try to match level
                            {
                                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(z => z.Key == x.key);
                                if (song != null)
                                {
                                    x.level = SongLoader.CustomLevels.FirstOrDefault(y => y.levelID.StartsWith(song.Hash));
                                }
                                else
                                {
                                    x.level = SongLoader.CustomLevels.FirstOrDefault(y => y.customSongInfo.path.Contains(x.key));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Warning($"Unable to match song with {(string.IsNullOrEmpty(x.key) ? " unknown key!" : ("key " + x.key + " !"))} Exception: {e}");
                        }
                    }
                });
                Logger.Log($"Matched all songs for playlist \"{playlist.playlistTitle}\"! Time: {execTime.Elapsed.TotalSeconds.ToString("0.00")}s");
                execTime.Reset();
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
        public BeatmapLevelSO level;
        [NonSerialized]
        public bool oneSaber;
        [NonSerialized]
        public string path;

        public IEnumerator MatchKey()
        {
            if (!string.IsNullOrEmpty(key))
                yield break;

            if (!string.IsNullOrEmpty(hash))
            {
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => hash.ToUpper() == x.Hash);
                if (song != null)
                    key = song.Key;
                else
                    yield return SongDownloader.Instance.RequestSongByLevelIDCoroutine(hash, (Song bsSong) => { if (bsSong != null) key = bsSong.id; });
            }
            else if (!string.IsNullOrEmpty(levelId))
            {
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => levelId.StartsWith(x.Hash));
                if (song != null)
                    key = song.Key;
                else
                    yield return SongDownloader.Instance.RequestSongByLevelIDCoroutine(levelId.Substring(0, Math.Min(32, levelId.Length)), (Song bsSong) => { if (bsSong != null) key = bsSong.id; });
            }
            else if (level != null)
            {
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => level.levelID.StartsWith(x.Hash));
                if (song != null)
                    key = song.Key;
                else
                    yield return SongDownloader.Instance.RequestSongByLevelIDCoroutine(level.levelID.Substring(0, Math.Min(32, level.levelID.Length)), (Song bsSong) => { if (bsSong != null) key = bsSong.id; });
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
            if (ScrappedData.Songs.Count > 0)
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
            foreach (PlaylistSong song in songs)
            {
                yield return song.MatchKey();
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

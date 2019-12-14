using Newtonsoft.Json;
using SimpleJSON;
using SongBrowser.Internals;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Logger = SongBrowser.Logging.Logger;
using Sprites = SongBrowser.UI.Base64Sprites;

/// <summary>
/// Only here to support migration of favorites playlist to.
/// TODO - replace with PlaylistCore when available.
/// </summary>
namespace SongBrowser.DataAccess
{
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

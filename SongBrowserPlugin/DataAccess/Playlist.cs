using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Legacy;
using Newtonsoft.Json;
using SimpleJSON;
using SongBrowser.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    }

    public class Playlist
    {
        internal static PlaylistManager defaultManager = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.CreateChildManager("SongBrowser");

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
            // If we cannot find an image or parse the provided one correctly, fall back to anything.
            // It will never be displayed by SongBrowser.
            if (!string.IsNullOrEmpty(image))
            {
                try
                {
                    icon = Sprites.Base64ToSprite(image.Substring(image.IndexOf(",") + 1));
                }
                catch
                {
                    Logger.Exception("Unable to convert playlist image to sprite!");
                    icon = Sprites.StarFullIcon;
                }
            }
            else
            {
                icon = Sprites.StarFullIcon;
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

        public static BeatSaberPlaylistsLib.Types.IPlaylist CreateNew(string playlistName, IPreviewBeatmapLevel[] beatmapLevels)
        {
            string playlistFolderPath = defaultManager.PlaylistPath;
            string playlistFileName = string.Join("_", playlistName.Replace("/", "").Replace("\\", "").Replace(".", "").Split(' '));
            if (string.IsNullOrEmpty(playlistFileName))
            {
                playlistFileName = "playlist";
            }
            string playlistPath = Path.Combine(playlistFolderPath, playlistFileName + ".blist");
            string originalPlaylistPath = Path.Combine(playlistFolderPath, playlistFileName);
            int dupNum = 0;
            while (File.Exists(playlistPath))
            {
                dupNum++;
                playlistPath = originalPlaylistPath + string.Format("({0}).blist", dupNum);
                playlistFileName += string.Format("({0})", dupNum);
            }

            BeatSaberPlaylistsLib.Types.IPlaylist playlist = defaultManager.CreatePlaylist(playlistFileName, playlistName, "SongBrowser", "");
            foreach (var beatmapLevel in beatmapLevels)
            {
                playlist.Add(beatmapLevel);
            }
            defaultManager.StorePlaylist(playlist);
            playlist.SuggestedExtension = defaultManager.DefaultHandler?.DefaultExtension;
            return playlist;
        }
    }
}

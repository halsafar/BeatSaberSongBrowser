using SimpleJSON;
using SongBrowserPlugin.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SongBrowserPlugin.DataAccess
{
    public class PlaylistsReader
    {
        private List<String> _PlaylistsDirectories = new List<string>();

        private List<Playlist> _CachedPlaylists;

        public List<Playlist> Playlists
        {
            get
            {
                return _CachedPlaylists;
            }
        }

        public PlaylistsReader()
        {
            // Hack, add beatdrop location
            String localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            String beatDropPlaylistPath = Path.Combine(localAppDataPath, "Programs", "BeatDrop", "playlists");
            String beatDropCuratorPlaylistPath = Path.Combine(localAppDataPath, "Programs", "BeatDropCurator", "playlists");
            String beatSaberPlaylistPath = Path.Combine(Environment.CurrentDirectory, "Playlists");

            _PlaylistsDirectories.Add(beatDropPlaylistPath);
            _PlaylistsDirectories.Add(beatDropCuratorPlaylistPath);
            _PlaylistsDirectories.Add(beatSaberPlaylistPath);
        }
        
        public void UpdatePlaylists()
        {           
            _CachedPlaylists = new List<Playlist>();

            Stopwatch timer = new Stopwatch();
            timer.Start();
            foreach (String path in _PlaylistsDirectories)
            {
                Logger.Debug("Reading playlists located at: {0}", path);
                if (!Directory.Exists(path))
                {
                    Logger.Info("Playlist path does not exist: {0}", path);
                    continue;
                }

                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    Logger.Debug("Checking file {0}", file);
                    if (Path.GetExtension(file) == ".json" || Path.GetExtension(file) == ".bplist")
                    {
                        Playlist p = ParsePlaylist(file);
                        _CachedPlaylists.Add(p);
                    }
                }
            }
            timer.Stop();
            Logger.Debug("Processing playlists took {0}ms", timer.ElapsedMilliseconds);
        }

        public static Playlist ParsePlaylist(String path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Logger.Debug("Playlist file no longer exists: {0}", path);
                    return null;
                }

                Logger.Debug("Parsing playlist at {0}", path);
                String json = File.ReadAllText(path);
                Playlist playlist = new Playlist();

                JSONNode playlistNode = JSON.Parse(json);

                playlist.Image = playlistNode["image"];
                playlist.Title = playlistNode["playlistTitle"];
                playlist.Author = playlistNode["playlistAuthor"];
                playlist.Songs = new List<PlaylistSong>();
                playlist.CustomDetailUrl = playlistNode["customDetailUrl"];
                playlist.CustomArchiveUrl = playlistNode["customArchiveUrl"];
                if (!string.IsNullOrEmpty(playlist.CustomDetailUrl))
                {
                    if (!playlist.CustomDetailUrl.EndsWith("/"))
                    {
                        playlist.CustomDetailUrl += "/";
                    }
                    Logger.Debug("Found playlist with custom URL! Name: " + playlist.Title + ", CustomDetailURL: " + playlist.CustomDetailUrl);
                }
                foreach (JSONNode node in playlistNode["songs"].AsArray)
                {
                    PlaylistSong song = new PlaylistSong
                    {
                        Key = node["key"],
                        SongName = node["songName"],
                        LevelId = node["levelId"]
                    };

                    playlist.Songs.Add(song);
                }

                playlist.Path = path;
                return playlist;
            }
            catch (Exception e)
            {
                Logger.Exception("Exception parsing playlist: ", e);
            }

            return null;
        }
    }
}

using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SongBrowserPlugin.DataAccess
{
    public class PlaylistsReader
    {
        private static Logger _log = new Logger("PlaylistReader");

        private List<String> _PlaylistsDirectories = new List<string>();

        private List<Playlist> _CachedPlaylists;

        public List<Playlist> Playlists
        {
            get
            {
                return _CachedPlaylists;
            }
        }

        public PlaylistsReader(String playlistsDirectory)
        {
            _PlaylistsDirectories.Add(playlistsDirectory);

            // Hack, add beatdrop location
            String localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            String beatDropPlaylistPath = Path.Combine(localAppDataPath, "Programs", "BeatDrop", "playlists");
            String beatDropCuratorPlaylistPath = Path.Combine(localAppDataPath, "Programs", "BeatDropCurator", "playlists");

            _PlaylistsDirectories.Add(beatDropPlaylistPath);
            _PlaylistsDirectories.Add(beatDropCuratorPlaylistPath);
        }
        
        public void UpdatePlaylists()
        {           
            _CachedPlaylists = new List<Playlist>();

            Stopwatch timer = new Stopwatch();
            timer.Start();
            foreach (String path in _PlaylistsDirectories)
            {
                _log.Debug("Reading playlists located at: {0}", path);
                if (!Directory.Exists(path))
                {
                    _log.Info("Playlist path does not exist: {0}", path);
                    continue;
                }

                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    _log.Debug("Checking file {0}", file);
                    if (Path.GetExtension(file) == ".json")
                    {
                        Playlist p = ParsePlaylist(file);
                        _CachedPlaylists.Add(p);
                    }
                }
            }
            timer.Stop();
            _log.Debug("Processing playlists took {0}ms", timer.ElapsedMilliseconds);
        }

        public static Playlist ParsePlaylist(String path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    _log.Debug("Playlist file no longer exists: {0}", path);
                    return null;
                }

                _log.Debug("Parsing playlist at {0}", path);
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
                    _log.Debug("Found playlist with custom URL! Name: " + playlist.Title + ", CustomDetailURL: " + playlist.CustomDetailUrl);
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
                _log.Exception("Exception parsing playlist: ", e);
            }

            return null;
        }
    }
}

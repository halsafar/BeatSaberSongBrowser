using SimpleJSON;
using System;
using System.Collections.Generic;
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

            _PlaylistsDirectories.Add(beatDropPlaylistPath);
        }
        
        public void UpdatePlaylists()
        {           
            _CachedPlaylists = new List<Playlist>();

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

                playlist.image = playlistNode["image"];
                playlist.playlistTitle = playlistNode["playlistTitle"];
                playlist.playlistAuthor = playlistNode["playlistAuthor"];
                playlist.songs = new List<PlaylistSong>();

                foreach (JSONNode node in playlistNode["songs"].AsArray)
                {
                    PlaylistSong song = new PlaylistSong();
                    song.key = node["key"];
                    song.songName = node["songName"];

                    playlist.songs.Add(song);
                }

                playlist.playlistPath = path;
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

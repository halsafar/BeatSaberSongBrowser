using BeatSaberPlaylistsLib;
using System.IO;


namespace SongBrowser.DataAccess
{
    class Playlist
    {
        internal static PlaylistManager defaultManager = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.CreateChildManager("SongBrowser");

        public static BeatSaberPlaylistsLib.Types.IPlaylist CreateNew(string playlistName, IPreviewBeatmapLevel[] beatmapLevels)
        {
            BeatSaberPlaylistsLib.Types.IPlaylist playlist = defaultManager.CreatePlaylist("", playlistName, "SongBrowser", "");
            foreach (var beatmapLevel in beatmapLevels)
            {
                playlist.Add(beatmapLevel);
            }
            defaultManager.StorePlaylist(playlist);
            return playlist;
        }
    }
}

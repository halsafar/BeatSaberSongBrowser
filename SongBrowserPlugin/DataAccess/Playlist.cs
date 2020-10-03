using BeatSaberPlaylistsLib;
using System.Collections.Generic;


namespace SongBrowser.DataAccess
{
    class Playlist
    {
        internal static PlaylistManager defaultManager = PlaylistManager.DefaultManager.CreateChildManager("SongBrowser");

        public static BeatSaberPlaylistsLib.Types.IPlaylist CreateNew(string playlistName, IReadOnlyList<IPreviewBeatmapLevel> beatmapLevels)
        {
            var playlist = defaultManager.CreatePlaylist("", playlistName, "SongBrowser", "");
            foreach (var beatmapLevel in beatmapLevels)
            {
                playlist.Add(beatmapLevel);
            }
            defaultManager.StorePlaylist(playlist);
            return playlist;
        }
    }
}

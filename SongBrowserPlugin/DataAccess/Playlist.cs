using BeatSaberPlaylistsLib;
using System.IO;


namespace SongBrowser.DataAccess
{
    class Playlist
    {
        internal static PlaylistManager defaultManager = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.CreateChildManager("SongBrowser");

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

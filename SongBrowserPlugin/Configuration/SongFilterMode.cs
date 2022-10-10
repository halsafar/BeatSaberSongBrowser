
namespace SongBrowser.Configuration
{
    public enum SongFilterMode
    {
        None,
        Playlist,
        Search,
        Ranked,
        Unranked,
        Played,
        Unplayed,
        Requirements,
        Easy,
        Normal,
        Hard,
        Expert,
        ExpertPlus,
        // For other mods that extend SongBrowser
        Custom,
        // Deprecated
        Favorites
    }

    static class SongFilterModeMethods
    {
        public static bool NeedsScoreSaberData(this SongFilterMode s)
        {
            switch (s)
            {
                case SongFilterMode.Ranked:
                case SongFilterMode.Unranked:
                    return true;
                default:
                    return false;
            }
        }
    }
}

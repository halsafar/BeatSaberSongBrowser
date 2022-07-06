
namespace SongBrowser.Configuration
{
    public enum SongSortMode
    {
        Default,
        Author,
        Mapper,
        Original,
        Newest,
        YourPlayCount,
        Difficulty,
        Random,
        PP,
        UpVotes,
        Rating,
        Heat,
        PlayCount,
        Stars,
        Bpm,
        Length,
        Vanilla,
        LastPlayed,

        // Allow mods to extend functionality.
        Custom,

        // Deprecated
        Favorites,
        Playlist,
        Search
    }

    static class SongSortModeMethods
    {
        public static bool NeedsScoreSaberData(this SongSortMode s)
        {
            switch (s)
            {
                case SongSortMode.UpVotes:
                case SongSortMode.Rating:
                case SongSortMode.PlayCount:
                case SongSortMode.Heat:
                case SongSortMode.PP:
                case SongSortMode.Stars:
                    return true;
                default:
                    return false;
            }
        }

        public static bool NeedsRefresh(this SongSortMode s)
        {
            switch (s)
            {
                case SongSortMode.LastPlayed:
                    return true;
                default:
                    return false;
            }
        }
    }
}

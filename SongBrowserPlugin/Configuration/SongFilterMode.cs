using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongBrowser.Configuration
{
    public enum SongFilterMode
    {
        None,
        Favorites,
        Playlist,
        Search,
        Ranked,
        Unranked,
        Played,
        Unplayed,
        Requirements,

        // For other mods that extend SongBrowser
        Custom
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

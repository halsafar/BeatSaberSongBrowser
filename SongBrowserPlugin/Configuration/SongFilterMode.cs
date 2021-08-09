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
        Requirements,

        // For other mods that extend SongBrowser
        Custom
    }

}

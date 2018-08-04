using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongBrowserPlugin.DataAccess
{
    public class Playlist
    {
        public String playlistTitle { get; set; }
        public String playlistAuthor { get; set; }
        public string image { get; set; }
        public List<PlaylistSong> songs { get; set; }

        public String playlistPath;
    }
}

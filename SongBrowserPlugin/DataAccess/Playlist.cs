using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongBrowserPlugin.DataAccess
{
    public class Playlist
    {
        public String Title { get; set; }
        public String Author { get; set; }
        public string Image { get; set; }
        public List<PlaylistSong> Songs { get; set; }

        public String Path;

        public string CustomDetailUrl { get; set; }
        public string CustomArchiveUrl { get; set; }
    }
}

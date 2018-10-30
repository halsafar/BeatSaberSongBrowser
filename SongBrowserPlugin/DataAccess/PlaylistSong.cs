using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongBrowserPlugin.DataAccess
{
    public class PlaylistSong
    {
        public String Key { get; set; }
        public String SongName { get; set; }

        // Set by playlist downloading
        [NonSerialized]
        public IStandardLevel Level;
        [NonSerialized]
        public bool OneSaber;
        [NonSerialized]
        public string Path;
    }
}

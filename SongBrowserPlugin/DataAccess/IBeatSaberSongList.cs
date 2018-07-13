using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SongBrowserPlugin.DataAccess
{
    public interface IBeatSaberSongList
    {
        List<LevelStaticData> AcquireSongList();
        void OverwriteBeatSaberSongList(List<LevelStaticData> newSongList);
    }
}

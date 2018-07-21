using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SongBrowserPlugin.DataAccess
{
    public interface IBeatSaberSongList
    {
        ScriptableObjectPool<CustomLevelCollectionsForGameplayModes> AcquireSongList();
        void OverwriteBeatSaberSongList(ScriptableObjectPool<CustomLevelCollectionsForGameplayModes> newSongList);
    }
}

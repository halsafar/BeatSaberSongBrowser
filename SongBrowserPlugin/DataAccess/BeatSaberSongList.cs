using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SongBrowserPlugin.DataAccess
{
    public class BeatSaberSongList : IBeatSaberSongList
    {
        private Logger _log = new Logger("BeatSaberSongList");

        /// <summary>
        /// Fetch the existing song list.
        /// </summary>
        /// <returns></returns>
        public ScriptableObjectPool<CustomLevelCollectionsForGameplayModes> AcquireSongList()
        {
            _log.Debug("AcquireSongList()");

            Stopwatch stopwatch = Stopwatch.StartNew();

            /* var gameScenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();
             var gameDataModel = PersistentSingleton<GameDataModel>.instance;

             List<LevelStaticData> songList = gameDataModel.gameStaticData.worldsData[0].levelsData.ToList();

             stopwatch.Stop();
             _log.Info("Acquiring Song List from Beat Saber took {0}ms", stopwatch.ElapsedMilliseconds);*/

            //return songList;
            return null;
        }

        /// <summary>
        /// Helper to overwrite the existing song list.
        /// </summary>
        /// <param name="newSongList"></param>
        public void OverwriteBeatSaberSongList(ScriptableObjectPool<CustomLevelCollectionsForGameplayModes> newSongList)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            //var gameDataModel = PersistentSingleton<GameDataModel>.instance;
            //ReflectionUtil.SetPrivateField(gameDataModel.gameStaticData.worldsData[0], "_levelsData", newSongList.ToArray());

            stopwatch.Stop();
            _log.Info("Overwriting the beat saber song list took {0}ms", stopwatch.ElapsedMilliseconds);
        }
    }
}

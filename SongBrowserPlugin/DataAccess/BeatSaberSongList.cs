using System;
using System.Collections.Generic;
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
        public List<LevelStaticData> AcquireSongList()
        {
            _log.Debug("AcquireSongList()");

            var gameScenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();
            var gameDataModel = PersistentSingleton<GameDataModel>.instance;

            List<LevelStaticData> songList = gameDataModel.gameStaticData.worldsData[0].levelsData.ToList();

            return songList;
        }

        /// <summary>
        /// Helper to overwrite the existing song list.
        /// </summary>
        /// <param name="newSongList"></param>
        public void OverwriteBeatSaberSongList(List<LevelStaticData> newSongList)
        {
            var gameDataModel = PersistentSingleton<GameDataModel>.instance;
            ReflectionUtil.SetPrivateField(gameDataModel.gameStaticData.worldsData[0], "_levelsData", newSongList.ToArray());
        }
    }
}

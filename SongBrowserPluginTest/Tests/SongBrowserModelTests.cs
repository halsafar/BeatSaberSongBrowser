using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SongBrowserPlugin;

namespace SongBrowserPluginTests
{
    /*
    class MockBeatSaberSongList : SongBrowserPlugin.DataAccess.IBeatSaberSongList
    {
        List<LevelStaticData> testList;

        public MockBeatSaberSongList()
        {
            testList = new List<LevelStaticData>();
            for (int i = 0; i < 10000; i++)
            {
                LevelStaticData level = new LevelStaticData();
                SongBrowserPlugin.ReflectionUtil.SetPrivateField(level, "_songName", "SongName" + i);
                SongBrowserPlugin.ReflectionUtil.SetPrivateField(level, "_authorName", "AuthorName" + i);
                SongBrowserPlugin.ReflectionUtil.SetPrivateField(level, "_levelId", "LevelId" + i);
                testList.Add(level);
            }

        }

        public List<LevelStaticData> AcquireSongList()
        {
            return testList;
        }

        public void OverwriteBeatSaberSongList(List<LevelStaticData> newSongList)
        {
            return;
        }
    }

    class SongBrowserModelTests : ISongBrowserTest
    {
        private Logger _log = new Logger("SongBrowserModelTests");

        public void RunTest()
        {
            _log.Info("SongBrowserModelTests - All tests in Milliseconds");

            Stopwatch stopwatch = Stopwatch.StartNew();
            SongBrowserModel model = new SongBrowserModel();
            model.Init(new MockBeatSaberSongList());
            stopwatch.Stop();
            _log.Info("Created a bunch of LevelStaticData: {0}", stopwatch.ElapsedMilliseconds);

            stopwatch = Stopwatch.StartNew();
            model.Settings.sortMode = SongSortMode.Original;
            model.UpdateSongLists(false);
            stopwatch.Stop();
            _log.Info("Song Loading and Sorting as original: {0}", stopwatch.ElapsedMilliseconds);

            stopwatch = Stopwatch.StartNew();
            model.Settings.sortMode = SongSortMode.Default;
            model.UpdateSongLists(false);
            stopwatch.Stop();        
            _log.Info("Song Loading and Sorting as favorites: {0}", stopwatch.ElapsedMilliseconds);

            stopwatch = Stopwatch.StartNew();
            model.Settings.sortMode = SongSortMode.Favorites;
            model.UpdateSongLists(false);
            stopwatch.Stop();
            _log.Info("Song Loading and Sorting as favorites: {0}", stopwatch.ElapsedMilliseconds);

            stopwatch = Stopwatch.StartNew();
            model.Settings.sortMode = SongSortMode.Newest;
            model.UpdateSongLists(false);
            stopwatch.Stop();
            _log.Info("Song Loading and Sorting as newest: {0}", stopwatch.ElapsedMilliseconds);

            stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 1; i++)
            {
                var m1 = model.SortedSongList.ToArray();
                var m2 = model.SortedSongList.ToArray();
                var m3 = model.SortedSongList.ToArray();
            }
            stopwatch.Stop();
            _log.Info("Converting big list into array a bunch of times: {0}", stopwatch.ElapsedMilliseconds);
        }
    }*/
}

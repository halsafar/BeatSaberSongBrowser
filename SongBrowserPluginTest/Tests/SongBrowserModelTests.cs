using Logger = SongBrowserPlugin.Logging.Logger;

namespace SongBrowserPluginTests
{
    
    class SongBrowserModelTests : ISongBrowserTest
    {
        public void RunTest()
        {
            /*Logger.Info("SongBrowserModelTests - All tests in Milliseconds");

            Stopwatch stopwatch = Stopwatch.StartNew();
            SongBrowserModel model = new SongBrowserModel();
            model.Init();
            stopwatch.Stop();
            Logger.Info("Created a bunch of LevelStaticData: {0}", stopwatch.ElapsedMilliseconds);

            stopwatch = Stopwatch.StartNew();
            model.Settings.sortMode = SongSortMode.Original;
            model.UpdateSongLists(false);
            stopwatch.Stop();
            Logger.Info("Song Loading and Sorting as original: {0}", stopwatch.ElapsedMilliseconds);

            stopwatch = Stopwatch.StartNew();
            model.Settings.sortMode = SongSortMode.Default;
            model.UpdateSongLists(false);
            stopwatch.Stop();        
            Logger.Info("Song Loading and Sorting as favorites: {0}", stopwatch.ElapsedMilliseconds);

            stopwatch = Stopwatch.StartNew();
            model.Settings.sortMode = SongSortMode.Favorites;
            model.UpdateSongLists(false);
            stopwatch.Stop();
            Logger.Info("Song Loading and Sorting as favorites: {0}", stopwatch.ElapsedMilliseconds);

            stopwatch = Stopwatch.StartNew();
            model.Settings.sortMode = SongSortMode.Newest;
            model.UpdateSongLists(false);
            stopwatch.Stop();
            Logger.Info("Song Loading and Sorting as newest: {0}", stopwatch.ElapsedMilliseconds);

            stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 1; i++)
            {
                var m1 = model.SortedSongList.ToArray();
                var m2 = model.SortedSongList.ToArray();
                var m3 = model.SortedSongList.ToArray();
            }
            stopwatch.Stop();
            Logger.Info("Converting big list into array a bunch of times: {0}", stopwatch.ElapsedMilliseconds);*/
        }
    }
}

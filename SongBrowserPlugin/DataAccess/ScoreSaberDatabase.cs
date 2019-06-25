using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser.DataAccess
{
    public class ScoreSaberSong
    {
        public string song { get; set; }
        public string mapper { get; set; }
        public List<ScoreSaberSongDifficultyStats> diffs { get; set; }
    }

    public class ScoreSaberSongDifficultyStats
    {
        public string diff { get; set; }
        public long scores { get; set; }
        public double star { get; set; }
        public double pp { get; set; }
    }

    public class ScoreSaberDataFile
    {
        public Dictionary<String, ScoreSaberSong> SongHashToScoreSaberData = null;

        public ScoreSaberDataFile(byte[] data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            System.Globalization.NumberStyles style = System.Globalization.NumberStyles.AllowDecimalPoint;

            string result = System.Text.Encoding.UTF8.GetString(data);

            SongHashToScoreSaberData = JsonConvert.DeserializeObject<Dictionary<string, ScoreSaberSong>>(result);
                        
            timer.Stop();
            Logger.Debug("Processing ScoreSaber data took {0}ms", timer.ElapsedMilliseconds);
        }
    }
}

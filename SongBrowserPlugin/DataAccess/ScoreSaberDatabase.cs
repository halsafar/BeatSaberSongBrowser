using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace SongBrowserPlugin.DataAccess
{
    public class ScoreSaberDifficulty
    {
        public string name;
        public float pp;
        public float star;
    }

    public class ScoreSaberData
    {
        public string name;        
        public Dictionary<String, ScoreSaberDifficulty> difficultyToSaberDifficulty = new Dictionary<string, ScoreSaberDifficulty>();
        public float maxStar = 0;
        public float maxPp = 0;
        public string version;

        public void AddDifficultyRating(string name, float pp, float star)
        {
            // assume list is newest->oldest, so always take first result.
            if (difficultyToSaberDifficulty.ContainsKey(name))
            {
                return;
            }
            ScoreSaberDifficulty ppDifficulty = new ScoreSaberDifficulty();
            ppDifficulty.name = name;
            ppDifficulty.star = star;
            ppDifficulty.pp = pp;
            difficultyToSaberDifficulty.Add(ppDifficulty.name, ppDifficulty);

            if (pp > maxPp)
                maxPp = pp;

            if (star > maxStar)
                maxStar = star;
        }
    }

    public class ScoreSaberDataFile
    {
        private Logger _log = new Logger("ScoreSaberDataFile");

        public Dictionary<String, ScoreSaberData> SongNameToScoreSaberData;
        public Dictionary<String, ScoreSaberData> SongVersionToScoreSaberData;

        public ScoreSaberDataFile(byte[] data)
        {
            SongNameToScoreSaberData = new Dictionary<string, ScoreSaberData>();
            SongVersionToScoreSaberData = new Dictionary<string, ScoreSaberData>();

            string result = System.Text.Encoding.UTF8.GetString(data);
            string[] lines = result.Split('\n');

            Regex versionRegex = new Regex(@".*/(?<version>.*)\.(?<extension>jpg|JPG|png|PNG)");
            foreach (string s in lines)
            {
                // Example: Freedom Dive - v2	367.03	Expert+	9.19★	[src='https://beatsaver.com/storage/songs/3037/3037-2154.jpg']                
                //_log.Trace(s);

                string[] split = s.Split('\t');                
                float pp = float.Parse(split[1]);

                int lastDashIndex = split[0].LastIndexOf('-');
                string name = split[0].Substring(0, lastDashIndex).Trim();
                string author = split[0].Substring(lastDashIndex+1, split[0].Length - (lastDashIndex+1)).Trim();
                //_log.Debug("name={0}", name);
                //_log.Debug("author={0}", author);
                
                string difficultyName = split[2];
                if (difficultyName == "Expert+")
                {
                    difficultyName = "ExpertPlus";
                }

                float starDifficulty = 0;
                string fixedStarDifficultyString = split[3].Remove(split[3].Length -1);
                
                if (fixedStarDifficultyString.Length >= 1 && Char.IsNumber(fixedStarDifficultyString[0]))
                {
                    starDifficulty = float.Parse(fixedStarDifficultyString);
                }
                
                Match m = versionRegex.Match(split[4]);
                string version = m.Groups["version"].Value;

                ScoreSaberData ppData = null;
                if (!SongVersionToScoreSaberData.ContainsKey(version))
                {
                    ppData = new ScoreSaberData();
                    ppData.version = version;
                    ppData.name = name;

                    SongVersionToScoreSaberData.Add(version, ppData);
                }
                else
                {
                    ppData = SongVersionToScoreSaberData[version];
                }

                if (!SongNameToScoreSaberData.ContainsKey(name))
                {
                    SongNameToScoreSaberData.Add(name, ppData);
                }

                // add difficulty  
                ppData.AddDifficultyRating(difficultyName, pp, starDifficulty);
            }
        }
    }
}

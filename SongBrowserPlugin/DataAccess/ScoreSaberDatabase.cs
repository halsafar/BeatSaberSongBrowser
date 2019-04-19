using SimpleJSON;
using SongBrowser.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser.DataAccess
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
            ScoreSaberDifficulty ppDifficulty = new ScoreSaberDifficulty
            {
                name = name,
                star = star,
                pp = pp
            };
            difficultyToSaberDifficulty.Add(ppDifficulty.name, ppDifficulty);

            if (pp > maxPp)
                maxPp = pp;

            if (star > maxStar)
                maxStar = star;
        }
    }

    public class ScoreSaberDataFile
    {
        public Dictionary<String, ScoreSaberData> SongVersionToScoreSaberData;

        public ScoreSaberDataFile(byte[] data)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            SongVersionToScoreSaberData = new Dictionary<string, ScoreSaberData>();

            System.Globalization.NumberStyles style = System.Globalization.NumberStyles.AllowDecimalPoint;

            string result = System.Text.Encoding.UTF8.GetString(data);

            JSONNode rootNode = JSON.Parse(result);
            foreach (KeyValuePair<string, JSONNode> kvp in rootNode)
            {                
                JSONNode difficultyNodes = kvp.Value;
                foreach (KeyValuePair<string, JSONNode> innerKvp in difficultyNodes)
                {
                    JSONNode node = innerKvp.Value;
                    String version = node["key"];
                    String name = node["name"];
                    String difficultyName = node["difficulty"];
                    if (difficultyName == "Expert+")
                    {
                        difficultyName = "ExpertPlus";
                    }
                    
                    float pp = 0;
                    float.TryParse(node["pp"], style, System.Globalization.CultureInfo.InvariantCulture, out pp);

                    float starDifficulty = 0;
                    float.TryParse(node["star"], style, System.Globalization.CultureInfo.InvariantCulture, out starDifficulty);

                    ScoreSaberData ppData = null;                    
                    if (!SongVersionToScoreSaberData.ContainsKey(version))
                    {
                        ppData = new ScoreSaberData
                        {
                            version = version,
                            name = name
                        };

                        SongVersionToScoreSaberData.Add(version, ppData);
                    }
                    else
                    {
                        ppData = SongVersionToScoreSaberData[version];
                    }

                    // add difficulty  
                    ppData.AddDifficultyRating(difficultyName, pp, starDifficulty);
                }
            }
            
            timer.Stop();
            Logger.Debug("Processing ScoreSaber data took {0}ms", timer.ElapsedMilliseconds);
        }
    }
}

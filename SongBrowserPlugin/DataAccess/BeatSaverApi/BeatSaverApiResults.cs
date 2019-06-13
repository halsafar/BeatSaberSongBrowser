using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;

namespace SongBrowser.DataAccess.BeatSaverApi
{
    public enum SongQueueState { Queued, Downloading, Downloaded, Error };

    [Serializable]
    public class DifficultyLevel
    {
        public string difficulty;
        public int difficultyRank;
        public string jsonPath;
        public int? offset;

        //bananbread api
        //      public DifficultyLevel(CustomSongInfo.DifficultyLevel difficultyLevel)
        //      {
        //          difficulty = difficultyLevel.difficulty;
        //          difficultyRank = difficultyLevel.difficultyRank;
        //          jsonPath = difficultyLevel.jsonPath;
        //      }

        public DifficultyLevel(string Difficulty, int DifficultyRank, string JsonPath, int Offset = 0)
        {
            difficulty = Difficulty;
            difficultyRank = DifficultyRank;
            jsonPath = JsonPath;
            offset = Offset;

        }

    }
    [Serializable]
    public class Song
    {
        public string id;
        public string beatname;
        public string ownerid;
        public string downloads;
        public string upvotes;
        public string plays;
        public string description;
        public string uploadtime;
        public string songName;
        public string songSubName;
        public string authorName;
        public string beatsPerMinute;
        public string downvotes;
        public string coverUrl;
        public string downloadUrl;
        public DifficultyLevel[] difficultyLevels;
        public string img;
        public string hash;

        public string path;

        public SongQueueState songQueueState = SongQueueState.Queued;

        public float downloadingProgress = 0f;

        public Song()
        {

        }

        public Song(JSONNode jsonNode)
        {
            id = jsonNode["key"];
            beatname = jsonNode["name"];
            ownerid = jsonNode["uploaderId"];
            downloads = jsonNode["downloadCount"];
            upvotes = jsonNode["upVotes"];
            downvotes = jsonNode["downVotes"];
            plays = jsonNode["playedCount"];
            description = jsonNode["description"];
            uploadtime = jsonNode["createdAt"];
            songName = jsonNode["songName"];
            songSubName = jsonNode["songSubName"];
            authorName = jsonNode["authorName"];
            beatsPerMinute = jsonNode["bpm"];
            coverUrl = jsonNode["coverUrl"];
            downloadUrl = jsonNode["downloadUrl"];
            hash = jsonNode["hashMd5"];
            hash = hash.ToUpper();

            var difficultyNode = jsonNode["difficulties"];

            difficultyLevels = new DifficultyLevel[difficultyNode.Count];

            for (int i = 0; i < difficultyNode.Count; i++)
            {
                difficultyLevels[i] = new DifficultyLevel(difficultyNode[i]["difficulty"], difficultyNode[i]["difficultyRank"], difficultyNode[i]["audioPath"], difficultyNode[i]["jsonPath"]);
            }
        }

        public static Song FromSearchNode(JSONNode mainNode)
        {
            Song buffer = new Song();
            buffer.id = mainNode["key"];
            buffer.beatname = mainNode["name"];
            buffer.ownerid = mainNode["uploaderId"];
            buffer.downloads = mainNode["downloadCount"];
            buffer.upvotes = mainNode["upVotes"];
            buffer.downvotes = mainNode["downVotes"];
            buffer.plays = mainNode["playedCount"];
            buffer.uploadtime = mainNode["createdAt"];
            buffer.songName = mainNode["songName"];
            buffer.songSubName = mainNode["songSubName"];
            buffer.authorName = mainNode["authorName"];
            buffer.beatsPerMinute = mainNode["bpm"];
            buffer.coverUrl = mainNode["coverUrl"];
            buffer.downloadUrl = mainNode["downloadUrl"];
            buffer.hash = mainNode["hashMd5"];

            var difficultyNode = mainNode["difficulties"];

            buffer.difficultyLevels = new DifficultyLevel[difficultyNode.Count];

            for (int i = 0; i < difficultyNode.Count; i++)
            {
                buffer.difficultyLevels[i] = new DifficultyLevel(difficultyNode[i]["difficulty"], difficultyNode[i]["difficultyRank"], difficultyNode[i]["audioPath"], difficultyNode[i]["jsonPath"]);
            }

            return buffer;
        }

        public Song(JSONNode jsonNode, JSONNode difficultyNode)
        {

            id = jsonNode["key"];
            beatname = jsonNode["name"];
            ownerid = jsonNode["uploaderId"];
            downloads = jsonNode["downloadCount"];
            upvotes = jsonNode["upVotes"];
            downvotes = jsonNode["downVotes"];
            plays = jsonNode["playedCount"];
            description = jsonNode["description"];
            uploadtime = jsonNode["createdAt"];
            songName = jsonNode["songName"];
            songSubName = jsonNode["songSubName"];
            authorName = jsonNode["authorName"];
            beatsPerMinute = jsonNode["bpm"];
            coverUrl = jsonNode["coverUrl"];
            downloadUrl = jsonNode["downloadUrl"];
            hash = jsonNode["hashMd5"];

            difficultyLevels = new DifficultyLevel[difficultyNode.Count];

            for (int i = 0; i < difficultyNode.Count; i++)
            {
                difficultyLevels[i] = new DifficultyLevel(difficultyNode[i]["difficulty"], difficultyNode[i]["difficultyRank"], difficultyNode[i]["audioPath"], difficultyNode[i]["jsonPath"]);
            }
        }

        public bool Compare(Song compareTo)
        {
            if (compareTo != null && songName == compareTo.songName)
            {
                if (difficultyLevels != null && compareTo.difficultyLevels != null)
                {
                    return (songSubName == compareTo.songSubName && authorName == compareTo.authorName && difficultyLevels.Length == compareTo.difficultyLevels.Length);
                }
                else
                {
                    return (songSubName == compareTo.songSubName && authorName == compareTo.authorName);
                }
            }
            else
            {
                return false;
            }
        }


        //bananbread api
        public Song(CustomPreviewBeatmapLevel _data)
        {
            songName = _data.songName;
            songSubName = _data.songSubName;
            authorName = _data.songAuthorName;
            difficultyLevels = ConvertDifficultyLevels(_data.standardLevelInfoSaveData.difficultyBeatmapSets.SelectMany(x => x.difficultyBeatmaps).ToArray());
            path = _data.customLevelPath;
            //bananabread id hash
            hash = SongCore.Collections.hashForLevelID(_data.levelID);
            //  hash = SongCore.Utilities.Utils.GetCustomLevelHash(_data);
        }
        /*
        public Song(StandardLevelInfoSaveData _data, string songPath)
        {
            songName = _data.songName;
            songSubName = _data.songSubName;
            authorName = _data.songAuthorName;
            difficultyLevels = ConvertDifficultyLevels(_data.difficultyBeatmapSets.SelectMany(x => x.difficultyBeatmaps).ToArray());
            path = songPath;
            //bananabread id hash
            hash = ;
            //  hash = SongCore.Utilities.Utils.GetCustomLevelHash(_data, songPath);
        }
        */
        /*
        public Song(CustomLevel _data)
        {
            songName = _data.songName;
            songSubName = _data.songSubName;
            authorName = _data.songAuthorName;
            difficultyLevels = ConvertDifficultyLevels(_data.difficultyBeatmapSets.SelectMany(x => x.difficultyBeatmaps).ToArray());
            path = _data.customSongInfo.path;
            hash = _data.levelID.Substring(0, 32);
        }
        //bananbread api
        public Song(CustomSongInfo _song)
        {
            songName = _song.songName;
            songSubName = _song.songSubName;
            authorName = _song.songAuthorName;
            difficultyLevels = ConvertDifficultyLevels(_song.difficultyLevels);
            path = _song.path;
            hash = _song.levelId.Substring(0, 32);
        }
        */
        //bananbread api
        /*
        public DifficultyLevel[] ConvertDifficultyLevels(CustomSongInfo.DifficultyLevel[] _difficultyLevels)
        {
            if (_difficultyLevels != null && _difficultyLevels.Length > 0)
            {
                DifficultyLevel[] buffer = new DifficultyLevel[_difficultyLevels.Length];
                for (int i = 0; i < _difficultyLevels.Length; i++)
                {
                    buffer[i] = new DifficultyLevel(_difficultyLevels[i]);
                }
                return buffer;
            }
            else
            {
                return null;
            }
        }
        */

        public DifficultyLevel[] ConvertDifficultyLevels(IDifficultyBeatmap[] _difficultyLevels)
        {
            if (_difficultyLevels != null && _difficultyLevels.Length > 0)
            {
                DifficultyLevel[] buffer = new DifficultyLevel[_difficultyLevels.Length];

                for (int i = 0; i < _difficultyLevels.Length; i++)
                {
                    buffer[i] = new DifficultyLevel(_difficultyLevels[i].difficulty.ToString(), _difficultyLevels[i].difficultyRank, string.Empty);
                }


                return buffer;
            }
            else
            {
                return null;
            }
        }
        public DifficultyLevel[] ConvertDifficultyLevels(StandardLevelInfoSaveData.DifficultyBeatmap[] _difficultyLevels)
        {
            if (_difficultyLevels != null && _difficultyLevels.Length > 0)
            {
                DifficultyLevel[] buffer = new DifficultyLevel[_difficultyLevels.Length];

                for (int i = 0; i < _difficultyLevels.Length; i++)
                {
                    buffer[i] = new DifficultyLevel(_difficultyLevels[i].difficulty.ToString(), _difficultyLevels[i].difficultyRank, string.Empty);
                }


                return buffer;
            }
            else
            {
                return null;
            }
        }

    }
    [Serializable]
    public class RootObject
    {
        public Song[] songs;
    }
}

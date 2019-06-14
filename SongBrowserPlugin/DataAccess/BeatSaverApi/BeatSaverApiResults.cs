using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SongBrowser.DataAccess.BeatSaverApi
{
    public enum SongQueueState { Queued, Downloading, Downloaded, Error };
    [Serializable]
    public class Metadata
    {
        public string[] characteristics;
        public Difficulties difficulties;

        public Metadata()
        {
        }

        [JsonConstructor]
        public Metadata(string[] characteristics, Difficulties difficulties)
        {
            this.characteristics = characteristics;
            this.difficulties = difficulties;
        }

        [Serializable]
        public class Difficulties
        {
            public bool easy = false;
            public bool normal = false;
            public bool hard = false;
            public bool expert = false;
            public bool expertPlus = false;
            [JsonConstructor]
            public Difficulties(bool easy, bool normal, bool hard, bool expert, bool expertPlus)
            {
                this.easy = easy;
                this.normal = normal;
                this.hard = hard;
                this.expert = expert;
                this.expertPlus = expertPlus;
            }

        }
    }
    [Serializable]
    public class Song
    {
        public Metadata metadata;
        public string levelAuthorName;
        public string songAuthorName;
        public string songName;
        public string songSubName;
        public float bpm;
        public int downloads;
        public int plays;
        public int upVotes;
        public int downVotes;
        public float rating;
        public float heat;
        public string description;
        public string _id;
        public string key;
        public string name;
        public string ownerid;
        public string ownerName;
        public string hash;
        public string uploaded;
        public string downloadURL;
        public string coverURL;
        public string img;


        public string path;
        public bool scoreSaber;

        public SongQueueState songQueueState = SongQueueState.Queued;

        public float downloadingProgress = 0f;

        public Song()
        {

        }

        public Song(JObject jsonNode, bool scoreSaber)
        {
            if (scoreSaber)
            {
                this.scoreSaber = scoreSaber;
                ConstructFromScoreSaber(jsonNode);
                return;
            }
            metadata = jsonNode["metadata"].ToObject<Metadata>();
            levelAuthorName = (string)jsonNode["metadata"]["levelAuthorName"];
            songAuthorName = (string)jsonNode["metadata"]["songAuthorName"];
            songName = (string)jsonNode["metadata"]["songName"];
            songSubName = (string)jsonNode["metadata"]["songSubName"];
            bpm = (float)jsonNode["metadata"]["bpm"];
            downloads = (int)jsonNode["stats"]["downloads"];
            plays = (int)jsonNode["stats"]["plays"];
            upVotes = (int)jsonNode["stats"]["upVotes"];
            downVotes = (int)jsonNode["stats"]["downVotes"];
            rating = (float)jsonNode["stats"]["rating"];
            heat = (float)jsonNode["stats"]["heat"];
            description = (string)jsonNode["description"];
            _id = (string)jsonNode["_id"];
            key = (string)jsonNode["key"];
            name = (string)jsonNode["name"];
            ownerid = (string)jsonNode["uploader"]["_id"];
            ownerName = (string)jsonNode["uploader"]["username"];
            hash = (string)jsonNode["hash"];
            hash = hash.ToLower();
            uploaded = (string)jsonNode["uploaded"];
            downloadURL = PluginConfig.beatsaverURL + (string)jsonNode["downloadURL"];
            coverURL = PluginConfig.beatsaverURL + (string)jsonNode["coverURL"];
        }

        public void ConstructFromScoreSaber(JObject jsonNode)
        {
            _id = "";
            ownerid = "";
            downloads = 0;
            upVotes = 0;
            downVotes = 0;
            plays = 0;
            description = "";
            uploaded = "";
            rating = 0;
            heat = 0f;
            key = "";
            name = "";
            ownerName = "";
            downloadURL = "";
            songName = (string)jsonNode["name"];
            songSubName = (string)jsonNode["songSubName"];
            levelAuthorName = (string)jsonNode["levelAuthorName"];
            songAuthorName = (string)jsonNode["songAuthorName"];
            bpm = (int)jsonNode["bpm"];
            coverURL = PluginConfig.scoresaberURL + jsonNode["image"];
            hash = (string)jsonNode["id"];
            hash = hash.ToLower();
            metadata = new Metadata() { characteristics = new string[] { "Standard" }, difficulties = new Metadata.Difficulties(true, false, false, false, false) };
            //       difficultyLevels = new DifficultyLevel[1];
            //       difficultyLevels[0] = new DifficultyLevel("Easy", 4, "", 0);
        }

        public static Song FromSearchNode(JObject jsonNode)
        {
            Song buffer = new Song();
            buffer.metadata = jsonNode["metadata"].ToObject<Metadata>();
            buffer.levelAuthorName = (string)jsonNode["metadata"]["levelAuthorName"];
            buffer.songAuthorName = (string)jsonNode["metadata"]["songAuthorName"];
            buffer.songName = (string)jsonNode["metadata"]["songName"];
            buffer.songSubName = (string)jsonNode["metadata"]["songSubName"];
            buffer.bpm = (float)jsonNode["metadata"]["bpm"];
            buffer.downloads = (int)jsonNode["stats"]["downloads"];
            buffer.plays = (int)jsonNode["stats"]["plays"];
            buffer.upVotes = (int)jsonNode["stats"]["upVotes"];
            buffer.downVotes = (int)jsonNode["stats"]["downVotes"];
            buffer.rating = (float)jsonNode["stats"]["rating"];
            buffer.heat = (float)jsonNode["stats"]["heat"];
            buffer.description = (string)jsonNode["description"];
            buffer._id = (string)jsonNode["_id"];
            buffer.key = (string)jsonNode["key"];
            buffer.name = (string)jsonNode["name"];
            buffer.ownerid = (string)jsonNode["uploader"]["_id"];
            buffer.ownerName = (string)jsonNode["uploader"]["username"];
            buffer.hash = (string)jsonNode["hash"];
            buffer.hash = buffer.hash.ToLower();
            buffer.uploaded = (string)jsonNode["uploaded"];
            buffer.downloadURL = PluginConfig.beatsaverURL + (string)jsonNode["downloadURL"];
            buffer.coverURL = PluginConfig.beatsaverURL + (string)jsonNode["coverURL"];
            return buffer;
        }


        public bool Compare(Song compareTo)
        {
            return compareTo.hash == hash;
        }


        //bananbread api
        public Song(CustomPreviewBeatmapLevel _data)
        {
            songName = _data.songName;
            songSubName = _data.songSubName;
            songAuthorName = _data.songAuthorName;
            levelAuthorName = _data.levelAuthorName;
            //   difficultyLevels = ConvertDifficultyLevels(_data.standardLevelInfoSaveData.difficultyBeatmapSets.SelectMany(x => x.difficultyBeatmaps).ToArray());
            path = _data.customLevelPath;
            //bananabread id hash
            hash = SongCore.Collections.hashForLevelID(_data.levelID).ToLower();
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
        /*
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
        */
        /*
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
        */
    }
    [Serializable]
    public class RootObject
    {
        public Song[] songs;
    }
}

using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SongBrowserPlugin.DataAccess.FileSystem
{
    class FolderBeatMapData : BeatmapData
    {
        public FolderBeatMapData(BeatmapLineData[] beatmapLinesData, BeatmapEventData[] beatmapEventData) :
            base(beatmapLinesData, beatmapEventData)
        {
        }
    }

    class FolderBeatMapDataSO : BeatmapDataSO
    {
        public FolderBeatMapDataSO()
        {
            BeatmapLineData lineData = new BeatmapLineData
            {
                beatmapObjectsData = new BeatmapObjectData[0]
            };
            this._beatmapData = new FolderBeatMapData(
                new BeatmapLineData[1]
                {
                    lineData
                },
                new BeatmapEventData[1]
                {
                    new BeatmapEventData(0, BeatmapEventType.Event0, 0)
                });
        }
    }

    class FolderLevel : BeatmapLevelSO
    {
        public void Init(String relativePath, String name, Sprite coverImage)
        {
            _songName = name;
            _songSubName = "";
            _songAuthorName = "Folder";
            _levelAuthorName = "Halsafar";

            _levelID = $"Folder_{relativePath}";

            var beatmapData = new FolderBeatMapDataSO();
            var difficultyBeatmaps = new List<CustomLevel.CustomDifficultyBeatmap>();
            var newDiffBeatmap = new CustomLevel.CustomDifficultyBeatmap(this, BeatmapDifficulty.Easy, 0, 0, 0, beatmapData);
            difficultyBeatmaps.Add(newDiffBeatmap);

            var sceneInfo = Resources.Load<SceneInfo>("SceneInfo/" + "DefaultEnvironment" + "SceneInfo");
            this.InitFull(_levelID, _songName, _songSubName, _songAuthorName, _levelAuthorName, SongLoaderPlugin.SongLoader.TemporaryAudioClip, 1, 1, 1, 1, 1, 1, coverImage, difficultyBeatmaps.ToArray(), sceneInfo, null);
            this.InitData();
        }
    }
}

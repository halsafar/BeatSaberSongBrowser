using CustomUI.Utilities;
using HMUI;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Sprites = SongBrowserPlugin.UI.Base64Sprites;

namespace SongBrowserPlugin.Internals
{
    public static class CustomHelpers
    {

        public static void RefreshTable(this TableView tableView, bool callbackTable = true)
        {
            HashSet<int> rows = new HashSet<int>(tableView.GetPrivateField<HashSet<int>>("_selectedCellIdxs"));
            float scrollPosition = tableView.GetPrivateField<ScrollRect>("_scrollRect").verticalNormalizedPosition;

            tableView.ReloadData();

            tableView.GetPrivateField<ScrollRect>("_scrollRect").verticalNormalizedPosition = scrollPosition;
            tableView.SetPrivateField("_targetPosition", scrollPosition);
            if (rows.Count > 0)
                tableView.SelectCellWithIdx(rows.First(), callbackTable);
        }

        public static IBeatmapLevelPack GetLevelPackWithLevels(BeatmapLevelSO[] levels, string packName = null, Sprite packCover = null)
        {
            CustomLevelCollectionSO levelCollection = ScriptableObject.CreateInstance<CustomLevelCollectionSO>();
            levelCollection.SetPrivateField("_levelList", levels.ToList());
            levelCollection.SetPrivateField("_beatmapLevels", levels);

            CustomBeatmapLevelPackSO pack = CustomBeatmapLevelPackSO.GetPack(levelCollection);
            pack.SetPrivateField("_packName", string.IsNullOrEmpty(packName) ? "Custom Songs" : packName);
            pack.SetPrivateField("_coverImage", packCover ?? Sprites.BeastSaberLogo);
            pack.SetPrivateField("_isPackAlwaysOwned", true);

            return pack;
        }

        static char[] hexChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static string CheckHex(string input)
        {
            input = input.ToUpper();
            if (input.All(x => hexChars.Contains(x)))
            {
                return input;
            }
            else
            {
                return "";
            }
        }

    }
}

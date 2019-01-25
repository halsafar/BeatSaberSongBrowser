using CustomUI.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace SongBrowserPlugin.DataAccess
{
    // https://github.com/andruzzzhka/BeatSaverDownloader/blob/master/BeatSaverDownloader/Misc/CustomHelpers.cs
    public static class CustomHelpers
    {

        public static void RefreshTable(this HMUI.TableView tableView, bool callbackTable = true)
        {
            HashSet<int> rows = new HashSet<int>(tableView.GetPrivateField<HashSet<int>>("_selectedRows"));
            float scrollPosition = tableView.GetPrivateField<ScrollRect>("_scrollRect").verticalNormalizedPosition;

            tableView.ReloadData();

            tableView.GetPrivateField<ScrollRect>("_scrollRect").verticalNormalizedPosition = scrollPosition;
            tableView.SetPrivateField("_targetVerticalNormalizedPosition", scrollPosition);
            if (rows.Count > 0)
                tableView.SelectRow(rows.First(), callbackTable);
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

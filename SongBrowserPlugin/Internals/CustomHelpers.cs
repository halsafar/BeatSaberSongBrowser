using System;
using System.Reflection;

namespace SongBrowser.Internals
{
    public static class CustomHelpers
    {
        public static string GetSongHash(string levelId)
        {
            var split = levelId.Split('_');
            return split.Length > 2 ? split[2] : levelId;
        }

  
    }
}

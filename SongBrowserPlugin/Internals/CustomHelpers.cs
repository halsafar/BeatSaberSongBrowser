using HMUI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SongCore.OverrideClasses;
using Sprites = SongBrowser.UI.Base64Sprites;
using SongCore.Utilities;
using System;
using System.Reflection;

namespace SongBrowser.Internals
{
    public static class CustomHelpers
    {
        public static bool IsModInstalled(string ModName)
        {
            foreach (var mod in IPA.Loader.PluginManager.Plugins)
            {
                if (mod.Name == ModName)
                    return true;
            }
            foreach (var mod in IPA.Loader.PluginManager.AllPlugins)
            {
                if (mod.Metadata.Id == ModName)
                    return true;
            }
            return false;
        }

        public static string GetSongHash(string levelId)
        {
            var split = levelId.Split('_');
            return split.Length > 2 ? split[2] : levelId;
        }

        public static object GetField(this object obj, string fieldName)
        {
            return (obj is Type ? (Type)obj : obj.GetType())
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(obj);
        }   
    }
}

using IllusionPlugin;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SongBrowserPlugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Logger = SongBrowserPlugin.Logging.Logger;

namespace SongBrowserPlugin
{
    class PluginConfig
    {
        public static int maxSimultaneousDownloads = 3;
        public static string beatsaverURL = "https://beatsaver.com";

        public static bool beatDropInstalled = false;
        public static string beatDropPlaylistsLocation = "";

        public static bool disableDeleteButton = false;
        public static bool deleteToRecycleBin = true;

        public static void LoadOrCreateConfig()
        {            
            if (!Directory.Exists("UserData"))
            {
                Directory.CreateDirectory("UserData");
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "beatsaverURL"))
            {
                ModPrefs.SetString("BeatSaverDownloader", "beatsaverURL", "https://beatsaver.com");
                Logger.Log("Created config");
            }
            else
            {
                beatsaverURL = ModPrefs.GetString("BeatSaverDownloader", "beatsaverURL");
                if (string.IsNullOrEmpty(beatsaverURL))
                {
                    ModPrefs.SetString("BeatSaverDownloader", "beatsaverURL", "https://beatsaver.com");
                    beatsaverURL = "https://beatsaver.com";
                    Logger.Log("Created config");
                }
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "disableDeleteButton"))
            {
                ModPrefs.SetBool("BeatSaverDownloader", "disableDeleteButton", false);
            }
            else
            {
                disableDeleteButton = ModPrefs.GetBool("BeatSaverDownloader", "disableDeleteButton", false, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "deleteToRecycleBin"))
            {
                ModPrefs.SetBool("BeatSaverDownloader", "deleteToRecycleBin", true);
            }
            else
            {
                deleteToRecycleBin = ModPrefs.GetBool("BeatSaverDownloader", "deleteToRecycleBin", true, true);
            }

            if (!ModPrefs.HasKey("BeatSaverDownloader", "maxSimultaneousDownloads"))
            {
                ModPrefs.SetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3);
            }
            else
            {
                maxSimultaneousDownloads = ModPrefs.GetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3, true);
            }               

            /*if (!File.Exists(configPath))
            {
                File.Create(configPath).Close();
            }

            favoriteSongs.AddRange(File.ReadAllLines(configPath, Encoding.UTF8));   */        

            try
            {
                if (Registry.CurrentUser.OpenSubKey(@"Software").GetSubKeyNames().Contains("178eef3d-4cea-5a1b-bfd0-07a21d068990"))
                {
                    beatDropPlaylistsLocation = (string)Registry.CurrentUser.OpenSubKey(@"Software\178eef3d-4cea-5a1b-bfd0-07a21d068990").GetValue("InstallLocation", "");
                    if (Directory.Exists(beatDropPlaylistsLocation))
                    {
                        beatDropInstalled = true;
                    }
                    else if (Directory.Exists("%LocalAppData%\\Programs\\BeatDrop\\playlists"))
                    {
                        beatDropInstalled = true;
                        beatDropPlaylistsLocation = "%LocalAppData%\\Programs\\BeatDrop\\playlists";
                    }
                    else
                    {
                        beatDropInstalled = false;
                        beatDropPlaylistsLocation = "";
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Can't open registry key! Exception: {e}");
                if (Directory.Exists("%LocalAppData%\\Programs\\BeatDrop\\playlists"))
                {
                    beatDropInstalled = true;
                    beatDropPlaylistsLocation = "%LocalAppData%\\Programs\\BeatDrop\\playlists";
                }
                else
                {
                    Logger.Log("Unable to find BeatDrop installation folder!");
                }
            }

            if (!Directory.Exists("Playlists"))
            {
                Directory.CreateDirectory("Playlists");
            }
        }

        public static void SaveConfig()
        {
            //File.WriteAllText(votedSongsPath, JsonConvert.SerializeObject(votedSongs, Formatting.Indented), Encoding.UTF8);
            //File.WriteAllText(reviewedSongsPath, JsonConvert.SerializeObject(reviewedSongs, Formatting.Indented), Encoding.UTF8);
            //File.WriteAllLines(configPath, favoriteSongs.Distinct().ToArray(), Encoding.UTF8);

            //ModPrefs.SetBool("BeatSaverDownloader", "disableDeleteButton", disableDeleteButton);
            //ModPrefs.SetBool("BeatSaverDownloader", "deleteToRecycleBin", deleteToRecycleBin);
            ModPrefs.SetInt("BeatSaverDownloader", "maxSimultaneousDownloads", maxSimultaneousDownloads);
        }
    }
}

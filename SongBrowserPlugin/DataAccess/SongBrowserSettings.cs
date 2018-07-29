using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;


namespace SongBrowserPlugin.DataAccess
{
    [Serializable]
    public enum SongSortMode
    {
        Default,
        Author,
        Favorites,
        Original,
        Newest,
        PlayCount,
        Random,
        Search
    }

    [Serializable]
    public class SongBrowserSettings
    {
        public SongSortMode sortMode = default(SongSortMode);
        public List<String> favorites = default(List<String>);
        public List<String> searchTerms = default(List<String>);

        [NonSerialized]
        private static Logger Log = new Logger("SongBrowserSettings");

        /// <summary>
        /// Constructor.
        /// </summary>
        public SongBrowserSettings()
        {
            //favorites = new List<String>();
        }

        /// <summary>
        /// Helper to acquire settings path at runtime.
        /// </summary>
        /// <returns></returns>
        public static String SettingsPath()
        {
            return Path.Combine(Environment.CurrentDirectory, "song_browser_settings.xml");
        }

        /// <summary>
        /// Load the settings file for this plugin.
        /// If we fail to load return Default settings.
        /// </summary>
        /// <returns>SongBrowserSettings</returns>
        public static SongBrowserSettings Load()
        {
            Log.Trace("Load()");
            SongBrowserSettings retVal = null;

            String settingsFilePath = SongBrowserSettings.SettingsPath();
            if (!File.Exists(settingsFilePath))
            {
                Log.Debug("Settings file does not exist, returning defaults: " + settingsFilePath);
                return new SongBrowserSettings();
            }

            // Deserialization from JSON            
            FileStream fs = null;
            try
            {
                fs = File.OpenRead(settingsFilePath);

                XmlSerializer serializer = new XmlSerializer(typeof(SongBrowserSettings));
                
                retVal = (SongBrowserSettings)serializer.Deserialize(fs);
            }
            catch (Exception e)
            {
                Log.Exception("Unable to deserialize song browser settings file: ", e);

                // Return default settings
                retVal = new SongBrowserSettings();
            }
            finally
            {
                if (fs != null) { fs.Close(); }
            }
            
            return retVal;
        }

        /// <summary>
        /// Save this Settings insance to file.
        /// </summary>
        public void Save()
        {            
            String settingsFilePath = SongBrowserSettings.SettingsPath();

            FileStream fs = new FileStream(settingsFilePath, FileMode.Create, FileAccess.Write);
            
            XmlSerializer serializer = new XmlSerializer(typeof(SongBrowserSettings));           
            serializer.Serialize(fs, this);
            
            fs.Close();            
        }
    }
}

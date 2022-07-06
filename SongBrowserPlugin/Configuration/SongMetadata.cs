using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using IPA.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace SongBrowser.Configuration
{
    internal class SongMetadataStore
    {
        public static SongMetadataStore Instance { get; set; }

        [JsonIgnore]
        private string _storePath;

        [UseConverter(typeof(DictionaryConverter<SongMetadata>))]
        [NonNullable]
        public virtual Dictionary<string, SongMetadata> Songs { get; set; } = new Dictionary<string, SongMetadata>();

        private static void CreateEmptyStore(String storePath)
        {
            SongMetadataStore.Instance = new SongMetadataStore
            {
                _storePath = storePath
            };
        }

        public static void Load()
        {
            string storePath = Path.Combine(UnityGame.UserDataPath, nameof(SongBrowser) + "SongMetadata" + ".json");
            if (!File.Exists(storePath))
            {
                SongMetadataStore.CreateEmptyStore(storePath);
                return;
            }

            try
            {
                Plugin.Log.Debug($"Loading SongMetaDataStore: {storePath}");
                using StreamReader file = File.OpenText(storePath);
                JsonSerializer serializer = new JsonSerializer();
                SongMetadataStore.Instance = (SongMetadataStore)serializer.Deserialize(file, typeof(SongMetadataStore));
                SongMetadataStore.Instance._storePath = storePath;
            }
            catch (JsonReaderException e)
            {
                Plugin.Log.Critical($"Could not parse SongMetaDataStore: {e}");
                Plugin.Log.Warn("SongMetaDataStore is corrupted, deleting, creating new store...");
                File.Delete(storePath);
                SongMetadataStore.CreateEmptyStore(storePath);
            }
        }

        public virtual SongMetadata GetMetadataForLevelID(string levelID)
        {
            if (!Instance.Songs.ContainsKey(levelID))
            {
                Instance.Songs.Add(levelID, new SongMetadata());
            }
            return Instance.Songs[levelID];
        }

        public void Save()
        {
            Plugin.Log.Debug($"Saving SongMetaDataStore: {this._storePath}");

            using StreamWriter file = File.CreateText(this._storePath);

            JsonSerializerSettings opts = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            JsonSerializer serializer = JsonSerializer.Create(opts);

            serializer.Serialize(file, this);
        }
    }

    internal class SongMetadata
    {
        [UseConverter]
        public virtual DateTime? AddedAt { get; set; }

        [UseConverter]
        public virtual DateTime? LastPlayed { get; set; } = null;
    }
}

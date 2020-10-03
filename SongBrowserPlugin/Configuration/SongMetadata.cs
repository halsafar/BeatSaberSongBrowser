using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using System;
using System.Collections.Generic;

namespace SongBrowser.Configuration
{
    internal class SongMetadataStore
    {
        public static SongMetadataStore Instance { get; set; }
        [UseConverter(typeof(DictionaryConverter<SongMetadata>))]
        [NonNullable]
        public virtual Dictionary<string, SongMetadata> Songs { get; set; } = new Dictionary<string, SongMetadata>();

        public virtual SongMetadata GetMetadataForLevelID(string levelID)
        {
            if (!Songs.ContainsKey(levelID))
                Instance.Songs.Add(levelID, new SongMetadata());
            return Songs[levelID];
        }
    }

    internal class SongMetadata
    {
        [UseConverter]
        public virtual DateTime? AddedAt { get; set; }
    }
}

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace SongBrowser.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        public virtual SongSortMode SortMode { get; set; } = default;
        public virtual SongFilterMode FilterMode { get; set; } = default;
        [UseConverter(typeof(ListConverter<string>))]
        [NonNullable]
        public virtual List<string> SearchTerms { get; set; } = new List<string>();
        public virtual string CurrentLevelId { get; set; } = default;
        public virtual string CurrentLevelCollectionName { get; set; } = default;
        public virtual string CurrentLevelCategoryName { get; set; } = default;
        public virtual bool RandomInstantQueueSong { get; set; } = false;
        public virtual bool ExperimentalScrapeSongMetaData { get; set; } = true;
        public virtual bool DeleteNumberedSongFolder { get; set; } = false;
        public virtual bool InvertSortResults { get; set; } = false;
        public virtual int RandomSongSeed { get; set; } = default;    
        public virtual int MaxSearchTerms { get; set; } = 10;

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            if (this.SearchTerms.Count > MaxSearchTerms)
            {
                this.SearchTerms.RemoveRange(MaxSearchTerms, this.SearchTerms.Count - MaxSearchTerms);
            }
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(PluginConfig other)
        {

        }
    }
}

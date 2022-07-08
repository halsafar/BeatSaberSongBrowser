using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace SongBrowser.Configuration
{
    public enum SortFilterStates
    {
        Disabled = 0,
        Enabled = 1,
    }

    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        public virtual SongSortMode SortMode { get; set; } = SongSortMode.Original;

        [UseConverter(typeof(DictionaryConverter<SortFilterStates>))]
        [NonNullable]
        public virtual Dictionary<string, SortFilterStates> FilterModes { get; set; } = new Dictionary<string, SortFilterStates>();

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

        public void ResetSortMode()
        {
            SortMode = SongSortMode.Original;
        }

        public void ResetFilterMode()
        {
            FilterModes.Clear();
        }

        public string GetFilterModeString()
        {
            string filterModeStr = null;
            foreach (var kvp in FilterModes)
            {
                if (kvp.Value == SortFilterStates.Enabled)
                {
                    if (!string.IsNullOrEmpty(filterModeStr))
                    {
                        filterModeStr = "Multiple";
                        break;
                    }
                    else
                    {
                        filterModeStr = kvp.Key.ToString();
                    }
                }
            }

            if (string.IsNullOrEmpty(filterModeStr))
            {
                return SongFilterMode.None.ToString();
            }

            return filterModeStr;
        }

        public bool IsFilterEnabled(SongFilterMode f)
        {
            if (FilterModes.ContainsKey(f.ToString()))
                return FilterModes[f.ToString()] == SortFilterStates.Enabled;

            return false;
        }

        public void SetFilterState(SongFilterMode f, SortFilterStates state)
        {
            FilterModes[f.ToString()] = state;
        }
    }
}

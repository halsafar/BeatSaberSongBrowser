namespace SongBrowser.Configuration
{
    public enum SongFilterMode
    {
        Easy = BeatmapDifficulty.Easy,
        Normal = BeatmapDifficulty.Normal,
        Hard = BeatmapDifficulty.Hard,
        Expert = BeatmapDifficulty.Expert,
        ExpertPlus = BeatmapDifficulty.ExpertPlus,
        None,
        Favorites,
        Playlist,
        Search,
        Ranked,
        Unranked,
        Played,
        Unplayed,
        Requirements,

        // For other mods that extend SongBrowser
        Custom
    }

}

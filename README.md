# BeatSaberSongBrowser
A plugin for customizing the in-game song browser.

*This mod works on both the Steam and Oculus Store versions.*

## Screenshot

![Alt text](/Screenshot.png?raw=true "Screenshot")

## Features
- Playlist support:
  - BeatDrop playlists!
- Filters:
  - Playlist (with a playlist selector).
  - Search (with keyboard support).
  - Favorites (all songs you have marked as a favorite).
- Optional folder support:
  - Disabled by default.  See the settings file.
- Sorting methods:
  - Song: By song name (default).
  - Author: By song author name then by song name.  
  - Original: Match the original sorting you would normally get after SongLoaderPlugin.
  - Newest: Sort songs by their last write time.
  - PP: Performance points!  Using DuoVR's scraped score saber data.  
  - PlayCount: Sort by playcount (sum of play counts across all difficulties for a given song).
  - Random: Randomize the song list each time.
- UI Enhancements:
  - Display PP and STAR difficulty per song / difficulty.
  - Fast scroll buttons (jumps 10% of your song list in each press).
- Tips:
  - Sort buttons can be pressed a second time to invert the sorting.
  - Filters can be cancelled by selecting them again.

## Keyboard Shortcuts
- Adjust Filters:
  - `F1-F3` correspond to the filter selection
- Adjust sort:
  - The `\`` key (~) will cycle the sort functions.
- Delete song:
  - `Delete` key.
 
## Playlist Format
```json
{
  "playlistTitle": "My Songs",
  "playlistAuthor": "Me",
  "image": "",
  "customArchiveUrl": "",
  "customDetailUrl": "",
  "songs": [
    {
      "songName": "SomeCoolSong",
      "key": "0000-0000"
    },
    {
      "songName": "AnotherCoolSong",
      "key": "0000-0000"
    }    
  ]
}
```

- `image` (optional): Base64 JPEG or PNG
- `customArchiveUrl` (optional): Expects a URL directly to an archive with a wildcard [KEY] which is replace with the song key.
  - Example: `"customArchiveUrl": "http://website/dlsongs/[KEY].zip"`
- `customDetailUrl` (optional): Expects a response equivalent to BeatSaver.com API.

## Status
- Working with BeatSaber 0.12.x



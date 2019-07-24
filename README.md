# Beat Saber SongBrowser
A plugin for customizing the in-game song browser.

*This mod works on both the Steam and Oculus Store versions.*

## Screenshot

![Alt text](/Screenshot.png?raw=true "Screenshot")

## Features
- Sorting methods:
  - Song: By song name (default).
  - Author: By song author name then by song name.  
  - Original: Match the original sorting you would normally get after SongLoaderPlugin.
  - Newest: Sort by the date you downloaded the custom song.
  - YourPlays: Sort by your most played.
  - PP: Performance points!  Using @WesVleuten score saber data.  
  - Star: Sort by ScoreSaber's Stars difficulty rating.
  - UpVotes: BeatSaver's upvote count.
  - Rating: BeatSaver's rating statistic.
  - PlayCount: BeatSaver's played count.
  - Random: Randomize the song list each time.
- Filters:
  - Playlists.
  - Search (with keyboard support).
  - Favorites (all songs you have marked as a favorite).
  - Ranked.
  - Unranked.
- UI Enhancements:
  - Display PP, STARS, and NJS.
  - Fast scroll buttons (jumps 10% of your song list in each press).
- Tips:
  - Sort buttons can be pressed a second time to invert the sorting.
  - Filters can be cancelled by selecting them again.
 
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
      "key": "0000",
      "songName": "SomeCoolSong",
      "hash": "00000000000000000000000000000000"
    },
    {
      "key": "0000",
      "songName": "AnotherCoolSong",
      "hash": "00000000000000000000000000000000"
    }    
  ]
}
```

- `image` (optional): Base64 JPEG or PNG
- `customArchiveUrl` (optional): Expects a URL directly to an archive with a wildcard [KEY] which is replace with the song key.
  - Example: `"customArchiveUrl": "http://website/dlsongs/[KEY].zip"`
- `customDetailUrl` (optional): Expects a response equivalent to BeatSaver.com API.

## Status
- Working with BeatSaber 1.1.0p1



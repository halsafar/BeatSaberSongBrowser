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
  - BPM: Beats Per Minute.
  - Time: Song duration/length.
  - PP: Performance points!  Using @WesVleuten (Westar#0001) score saber data.  
  - Star: Sort by ScoreSaber's Stars difficulty rating.
  - UpVotes: BeatSaver's upvote count.
  - Rating: BeatSaver's rating statistic.
  - PlayCount: BeatSaver's played count.
  - Random: Randomize the song list each time.
- Filters:
  - Search (with keyboard support).
  - Favorites (all songs you have marked as a favorite).
  - Ranked.
  - Unranked.
- UI Enhancements:
  - Display PP, STARS, and NJS.
  - Fast scroll buttons (jumps 10% of your song list in each press).
  - Delete button for custom songs.
- Tips:
  - Sort buttons can be pressed a second time to invert the sorting.
  - Filters can be cancelled by selecting them again.

## Status
- Working with BeatSaber 1.16.0

## Building on Windows
To compile BeatSaberSongBrowser from source:

1. Install Beat Saber and Microsoft Visual Studio.
2. Download and extract the BeatSaberSongBrowser source code.
3. Create a new file `/SongBrowserPlugin/SongBrower.csproj.user` with the following. (Make sure to replace BeatSaberDir with your real Beat Saber installation folder)
```
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <ProjectView>ProjectFiles</ProjectView>
    <BeatSaberDir>C:\Program Files (x86)\Steam\steamapps\common\Beat Saber</BeatSaberDir>
  </PropertyGroup>
</Project>
```
4. Open `/BeatSaberSongBrowser/SongBrowser.sln` in Microsoft Visual Studio.
5. Build the project with *Build -> Build Solution*.


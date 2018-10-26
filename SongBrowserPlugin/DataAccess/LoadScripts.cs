using HMUI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// From: https://github.com/andruzzzhka/BeatSaverDownloader
namespace SongBrowserPlugin.DataAccess
{
    class LoadScripts
    {
        static public Dictionary<string, Sprite> _cachedSprites = new Dictionary<string, Sprite>();

        static public IEnumerator LoadSprite(string spritePath, TableCell obj)
        {
            Texture2D tex;

            if (_cachedSprites.ContainsKey(spritePath))
            {
                obj.GetComponentsInChildren<UnityEngine.UI.Image>(true).First(x => x.name == "CoverImage").sprite = _cachedSprites[spritePath];
                yield break;
            }

            using (WWW www = new WWW(spritePath))
            {
                yield return www;
                tex = www.texture;
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
                _cachedSprites.Add(spritePath, newSprite);
                obj.GetComponentsInChildren<UnityEngine.UI.Image>(true).First(x => x.name == "CoverImage").sprite = newSprite;
            }
        }

        static public IEnumerator LoadAudio(string audioPath, object obj, string fieldName)
        {
            using (var www = new WWW(audioPath))
            {
                yield return www;
                SongLoaderPlugin.ReflectionUtil.SetPrivateField(obj, fieldName, www.GetAudioClip(true, true, AudioType.UNKNOWN));
            }
        }
    }
}

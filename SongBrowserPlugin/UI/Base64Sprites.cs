using SongBrowser.Internals;
using System;
using System.Text.RegularExpressions;
using UnityEngine;


namespace SongBrowser.UI
{
    class Base64Sprites
    {
        public static Sprite StarFullIcon;
        public static Sprite SpeedIcon;
        public static Sprite GraphIcon;
        public static Sprite DeleteIcon;
        public static Sprite XIcon;
        public static Sprite RandomIcon;

        public static Sprite BeastSaberLogo;
        public static Sprite DoubleArrow;

        public static void Init()
        {
            SpeedIcon = BeatSaberUI.LoadSpriteFromResources("SongBrowser.Assets.Speed.png");
            GraphIcon = BeatSaberUI.LoadSpriteFromResources("SongBrowser.Assets.Graph.png");
            XIcon = BeatSaberUI.LoadSpriteFromResources("SongBrowser.Assets.X.png");
            StarFullIcon = BeatSaberUI.LoadSpriteFromResources("SongBrowser.Assets.StarFull.png");
            DeleteIcon = BeatSaberUI.LoadSpriteFromResources("SongBrowser.Assets.DeleteIcon.png");
            DoubleArrow = BeatSaberUI.LoadSpriteFromResources("SongBrowser.Assets.DoubleArrow.png");
            BeastSaberLogo = BeatSaberUI.LoadSpriteFromResources("SongBrowser.Assets.BeastSaberLogo.png");
            RandomIcon = BeatSaberUI.LoadSpriteFromResources("SongBrowser.Assets.RandomIcon.png");
        }

        public static string SpriteToBase64(Sprite input)
        {
            return Convert.ToBase64String(input.texture.EncodeToPNG());
        }

        public static Sprite Base64ToSprite(string base64)
        {
            // prune base64 encoded image header
            Regex r = new Regex(@"data:image.*base64,");
            base64 = r.Replace(base64, "");            

            Sprite s = null;
            try
            {
                Texture2D tex = Base64ToTexture2D(base64);
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), (Vector2.one / 2f));
            }
            catch (Exception)
            {
                Console.WriteLine("Exception loading texture from base64 data.");
                s = null;
            }

            return s;
        }

        public static Texture2D Base64ToTexture2D(string encodedData)
        {
            byte[] imageData = Convert.FromBase64String(encodedData);

            int width, height;
            GetImageSize(imageData, out width, out height);

            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Trilinear;
            texture.LoadImage(imageData);
            return texture;
        }

        private static void GetImageSize(byte[] imageData, out int width, out int height)
        {
            width = ReadInt(imageData, 3 + 15);
            height = ReadInt(imageData, 3 + 15 + 2 + 2);
        }

        private static int ReadInt(byte[] imageData, int offset)
        {
            return (imageData[offset] << 8) | imageData[offset + 1];
        }
    }
}

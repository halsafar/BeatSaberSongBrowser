using System;
using System.Linq;
using System.Reflection;
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
        public static Sprite DoubleArrow;
        public static Sprite RandomIcon;
        public static Sprite NoteStartOffsetIcon;
        public static Sprite PlaylistIcon;

        public static void Init()
        {
            SpeedIcon = Base64Sprites.LoadSpriteFromResources("SongBrowser.Assets.Speed.png");
            GraphIcon = Base64Sprites.LoadSpriteFromResources("SongBrowser.Assets.Graph.png");
            XIcon = Base64Sprites.LoadSpriteFromResources("SongBrowser.Assets.X.png");
            StarFullIcon = Base64Sprites.LoadSpriteFromResources("SongBrowser.Assets.StarFull.png");
            DeleteIcon = Base64Sprites.LoadSpriteFromResources("SongBrowser.Assets.DeleteIcon.png");
            DoubleArrow = Base64Sprites.LoadSpriteFromResources("SongBrowser.Assets.DoubleArrow.png");
            RandomIcon = Base64Sprites.LoadSpriteFromResources("SongBrowser.Assets.RandomIcon.png");
            NoteStartOffsetIcon = Base64Sprites.LoadSpriteFromResources("SongBrowser.Assets.NoteStartOffset.png");
            PlaylistIcon = Base64Sprites.LoadSpriteFromResources("SongBrowser.Assets.PlaylistIcon.png");
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
                Plugin.Log.Critical("Exception loading texture from base64 data.");
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

        public static Texture2D LoadTextureRaw(byte[] file)
        {
            if (file.Count() > 0)
            {
                Texture2D Tex2D = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                if (Tex2D.LoadImage(file))
                    return Tex2D;
            }
            return null;
        }

        public static Texture2D LoadTextureFromResources(string resourcePath)
        {
            return LoadTextureRaw(GetResource(Assembly.GetCallingAssembly(), resourcePath));
        }

        public static Sprite LoadSpriteRaw(byte[] image, float PixelsPerUnit = 100.0f)
        {
            return LoadSpriteFromTexture(LoadTextureRaw(image), PixelsPerUnit);
        }

        public static Sprite LoadSpriteFromTexture(Texture2D SpriteTexture, float PixelsPerUnit = 100.0f)
        {
            if (SpriteTexture)
                return Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), PixelsPerUnit);
            return null;
        }

        public static Sprite LoadSpriteFromResources(string resourcePath, float PixelsPerUnit = 100.0f)
        {
            return LoadSpriteRaw(GetResource(Assembly.GetCallingAssembly(), resourcePath), PixelsPerUnit);
        }

        public static byte[] GetResource(Assembly asm, string ResourceName)
        {
            System.IO.Stream stream = asm.GetManifestResourceStream(ResourceName);
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
            return data;
        }
    }
}

using System;
using System.Text.RegularExpressions;
using UnityEngine;


namespace SongBrowser.UI
{
    class Base64Sprites
    {
        public static Sprite SearchIcon;
        public static Sprite PlaylistIcon;
        public static Sprite AddToFavoritesIcon;
        public static Sprite RemoveFromFavoritesIcon;
        public static Sprite StarFullIcon;
        public static Sprite DownloadIcon;
        public static Sprite SpeedIcon;
        public static Sprite StarIcon;
        public static Sprite GraphIcon;
        public static Sprite DeleteIcon;
        public static Sprite XIcon;
        public static Sprite RandomIcon;

        public static Sprite BeastSaberLogo;
        public static Sprite DoubleArrow;

        // https://icons8.com/icon/132/search
        public static string SearchIconB64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAMRSURBVFhH7ZddiIxRGMfHx26slt1F2nKhFBeSC4UbonxcaCUprZTakpRILiibuJEkiewN5TMlJcpmlb1yscVyxZVcEBfC+qhFlhm/55z/O2a2mdl5P2ZmL+ZX/845z3Oe/3l25533nTdVp9pkMpkJ6XR6GdrN/BhjD+NJxv1oI/Mmba0uHD4XnUPvaaIo5IfRXaarVVpZOGgKB55AP1wHIaDmPpovq+TBvB0N6LwsxP4yDDLeRhfQNfQIDfsd/yE2hNbJMjkwtY/0jc5xsP6IDqB2bcuD+FS2bWZ84is8rEdQp7bFB88mDJ97ew/ry6hZW0rCdvsidaGfvtrV27W5XFvigdEl+TpYH1IqFNYQ5V+9i/N5i6YpHQ0MFiO7xhzMe5SKBPUb0B/Zmd9RpaKBwT15mdkrhgalIoPPGe/oPL+jNqXCQX0Lxb+9lTPbplQs8JmJXe5HvVOpcFC4XR5m8pkh9n8vAK8rzhjwvqNwOCi0x5aD+XWFEwG/LbI27w8Kh8P+MnmYSbfCiYDfQlkHNCpVPphkb7DMuxROBPyaZR0wT6nyweSxiq3BPQonAn6zZe1gXfBpVBKKbqneDI4rnAj4LZF1QKSP+JSKrcGHCicCfrtkbd6vFQ4HhevlYSa/GGYoFRv8er2z876ocDiobUC5N9QjSsUCn0Uo93HXoVR4KD4rHzP6huYoFQls7JdNn3d0nvb4nKh0eCieZY05N2BuP1jDX9CC+m7v5GG9Q6noYLJPfg7WvWi60mVDzUGUlo359CkVH8yuytfB+iVapXRJ2GevCjdVmoWYPQiS+eJh1IjhA+ecAzF7Eepk2qKtDtaTia9B51HRFyxyTxkSa3IShqedcwHIfUIv0Ds0ovCYsNeazPsDY4HZWkzz3lHGgv1fGA4zPvORfCrRpN0uNqEbaMidMgri9vY2gPaydIczb0PVaTIAU7veFqCVaCvqQCtQwRci4kWbhEGUfJNhKaPJVm2tHfUmk6JUk8T7ta220Evr6CZZ261pqbbUHprJNjnumgtQk/3jsrk6lSeV+gfT9/jq5kBt5gAAAABJRU5ErkJggg==";

        // https://icons8.com/icon/41152/speed
        public static string SpeedIconB64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAQoSURBVFhH7Zfbi1VlGMbHQwo6WVGeIUGUwAvxFMQIoRfqpEKB1EWIh0HrIoSO4N9QE6SgV9pFgShqN5WKYF1EImgFQaEZHogUKXXEoqyc6fe87zNr7z1rRvee0QZi/+Dh+97nPay91957rbVbmjRpcp/o7u6ejbagvegbdBX9ZWkvT7lX0Gy33V96enrGcLCN6CT7hqDnBMsG9IDH3Vs4wBp0IY42BJhxHj3rsUOHmQ8z8GCOrwX/NjqOOpE+yhct7eWdQLddXgP+PjTBhxkczJnJkB9zZAW871heYp3s0gGhZip6GX2f3RXwzrDMdGlj0DwLXc5RCfEltIntaJcV4C9A+vjOofm2C9SD9Kb6zryMZrmsPmiYhn7yjID4CzTJ+SfQUuwR0QDEu7IyanfZjhdGvAzNUMw6GX2ZlQnxeZYp0XA3KBxFw+fRaYg/QGOVZ21Df9t/O5qAUL/QAH+9bdXvsfc7issN61j0YRQb4mMso6LpTlD4ZrYkxEdYikbiVzMTueO2A+JF2AsdBngXszrqX7DdeyKOZiYhfs3p/qFAX+ibrlfDaZaHnA6Ip+CfRL+g1bYHhJoOdB19hsbbDjQb76yOJdjfYBn4o6bg3SxNiJ+Wz/YRKYruAcx6jNkPas+q73IB8TtR1BdyrSSrz97H8lmfQX+iP1B7FA8SxupO9AbSGS1eCPtP86hx3Juo5kwHmOtc08s8+7sdq3l3FA8CevVGqz9Oven40RAuTDfBXxtN1WDud14FZ23Lb0c6e9Jy23VDzxx02KNrwH/LZaorbgjs99qugPmr8yrotB1gNfwdpF7fsx0oLknV4H3NssSlAV7x/Wd/xXaCMdW5gLihM0X9BLQT6U5zDR1A1z2uwPkOtiPdWoC/MqsS4sptlKDNfi+P25+OlqO4SPcHtSPIH8q2/iF/C3WyLS5Zmok0e7rjGVmdED8VhYJglf2AeLwa2XY5PuTSEuTmqGYgyH+ESvdazXRev+hpbFsV94K30qVxFp5Lu0BX+XbvRZdLS1C3wjUlyG1yWQlyuigHnqGHiQK8yjMjQd+L5aNIH4HeZRfrFpeWIDcJ3crOCvLQRJeVIKdbpmZ/wqrr48RoNMRLXRpncF7aCcnS49KdoF4X3263q1+87nRdUL/A7QHxXKfiBY7D+CdTkexwqm5oW0Lfe2ib9rbrhr7NcXBgr0vTOKcSzG8zHQX7bevAep5bj3Snufvj0CBh/oE4OOi12K6Aud15Feie3CqfdWuYydYoHgLMfhLpevgzWmRP19HffAwdf1sUV4O52PmAOH4YKrbVf2ODMON9j9O8uLfrWLYC4rYorgZfF9wfsmT44DXoj1TxV6IGEsVj+3DBCyz+LpQgrx/EV1k6LJxCpX+MNVCgB1fd+p7/L8Ux9bAQP8wmTZr8f2hp+ReWCz5Y6C35EgAAAABJRU5ErkJggg==";

        // https://icons8.com/icon/10159/christmas-star
        public static string StarIconB64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAANKSURBVFhH7Zi9S5VhGMa1o0Ik1tRezUIUlENDEFQIkVtDNDRVlFAkFQlB1HoICZqaXGyJkKD8AxoEocUlsjYLKS2jhr7UftfzXD75qsfO8X3znMEfXJz3/rqe2+P5btqkkVhYWNghOWw85ufnb0kOGwsW60CfrA6nGweWusm/N6BrpxsDdmpnqem4XlhQ1+0u1x8Wuh5Xy3DD5frCctvQBy+VUE41t9UPlrjqnVZArc9t9YEFtqIp76OFXkgOFU+px+0bD4df9i4B4qOSwwDxFbdvLLpn0DvvoUVGXVJt1Gnl36vXpY2DQy95hwBxt0uqdTsdIO51qVjwLqHdHHAMXUQD6BmaQL/C6cD1mEcSyrms+m/0Bo2g+6gXHUd7KLd4pDI06m3qCDqPymgYvUI/4hFrQ98JWyWUc3lNdIbPeop0tnbQLn/fLgn0eHrumZpg7iU3zbZKKOdazWgX7WSrCPk2ksOxZVVmqY+hIXQbnUYHUMUPBqq5R72a0az+9bPRciXUtUObLbKoQMPj0GmIv3Nz2C2FIU97J3z26sstQkMLjUNhwhDrrWyvW3IjL3smfOa/nzCCxhIDg2HSEE+jfW5ZN/KQl20DPqvklurQAIMPg4Mh/owOuqVmNCsP2wV8Rm3LLcKgnokPgpMh/sLNIbdUjWY8m7D3ileAmpABRgPB0RB/46bqx6R6PZOwZ77lloLhvWidqPpeVG8cicjLpeLAtyfaJ6r+eqneOJLocak4+Kv7ba57YNLpqtGMxzXf73RxYPrI/jpgxOkMlLZLDjNoJgyDvJwuDkzH7a8Dyk4HiPVefg3ptfIj6lPO5QBx2eOaH3e6GPBsxfRntA8HnF2S16eP9MF1EXKT6ByX4d1BM7ESavJqVb4QMOyM1hHiLqQPABNOVUQ97u1yKkDcafv8YHbKvgHi177MQH5Ucphh+Yw8bZ8fzO7ad1Woj6OTXDZLulYuFCsgT9vnB7Mn9s1A/i06w+UWtyaUU009oXkZ8nRrfjDLPNaI9U3tApdrf3YD9ahXM2HYyNMt+cBIP23M2XQG6eWk5q+RmvHsjL3mUP6fRvDaj9FXdIfr3L+eykNe8pS30+sHo11op8PCkKe8HW7yn2hq+gPI2OoDwCE0owAAAABJRU5ErkJggg==";

        // https://icons8.com/icon/3005/graph
        public static string GraphIconB64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAALaSURBVFhH7Zg7aBRRGIXHiCRGMQRUfIQgNgqaQkVEjQ8QVAiIiJVprIQoaKURgmAXUCuL4KNZFJsVO0vxUSgIioWKgi9sREElPhLjg12//3pmGTfX3XGygyPMgY975/z/vffsJruzu0GumCqXy62lUqkXlsjKlghYhDIBxxg6ZWdHBLtrAU3MV8nOhgi0Gr7DMxiQnQ0RaAY8h3fQITs7ItR5/Vl3yMqOCLVL4c7KSlcctAzm67KmyLWA3g/wGKbJTk8cclDPxigsle0VbZPpuQlfma+QnZ44qBnuWUAT89uwQeVxouWo+g7JSk8c0mGBdOALuAYjdo2uw0a1OlFbAz/gCrUmaIF2D21aklwcsh5ewzfYK9v82XACPnOQBb8Bm8De7+xBvAX3v0r5sPVUi/pLt1lSscF+sGCvYK3s34RvQY+DCxrRbrVUAtIzCP3iESQLyF4tLD6nTW9B3VctPRb0vq0xMe9RKfoMVu6/1C/D3wVkgb2FPIA3thvjKWhWua7oXceyO1CAKbIbE9A2h+jN/KRKExbbJQvIgnaaDsBDW81oN3PTGKxU24TF1vECYsyiqcB4RuOorWK0G/kAzINFMFdLGiKOiB1wUI0Wyl6dl2ALl01q8YqenfDeh1pqiv1jB9ynRgvYK7uu6A1v+FcZ7FNxkflT89RSU7TFC0jDJMxtsFVWLNHvAqJuWeYNmaHLmqIt2Yskrv77gMz7XHW8hq3OmI2AjBfhtHiC1ZiANHZpUx9TIW7ALldEzC9gNSxgjzbwqY16NgLaKMu88D0z/YAU5sCePzCdxn8esPtX3atOGrMREOMIw0KD+THzUKYC9jkDcVlZhJ8HtDrKA+YB84CuiJgnCjgC4cd1930ERQN+itS/uGokIOPHSN1+Z64OaL9khXX70ag64HCkbl85KgEXg/uo7mEmjcs9fkgr2AP01Yq2P+s3+2pQ0Pnbq3wH64aCIAh+AtEFoTRH3sIDAAAAAElFTkSuQmCC";

        // https://icons8.com/icon/79023/multiply
        public static string XIconB64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAFoAAABaCAYAAAA4qEECAAAABHNCSVQICAgIfAhkiAAAAAlwSFlzAAAGfQAABn0BoBVGiAAAABl0RVh0U29mdHdhcmUAd3d3Lmlua3NjYXBlLm9yZ5vuPBoAAAN+SURBVHic7ZzLThRBFIZPGyOXBSsVEi5yc6kPoBvfSEw0LvQB9DHUp4CYkLjUhSiZ0QiYGcZnIEYun4vqlg4Zeqa7TneV9PkSwoap+uvjUHR3VbWIYRiGYRiGYRiGYRiGYRiGYRiGERhgBngG7ABHODrAE2AqdL4MYAJ4DuwCf9KsnTT7TOh8hQArwD6X8wVYiCDnAvC5IOcP4G7onEMBpoCDgvAZ+8BiwJyLI4oh4ycR/QX+A3g5RvigslPJ4xRDxoumM44E+FpiAACHwFqD+dbSPsuw01S+sQCuA6clBwENVTawRLlKzjgFrtWdrxTA7woDgZorm2qVnHGklUPzt/Wt4ucWRWQLWFLMIiKukkVkK+2jCrtaWTRFv/P47KqIbGvKTtvaTtuuyhulOHrg5ukPFf9EVacRYN1jush4T2zzcwYwz3jXqEUc4FHZwB3cNbAP+8C8pht1gFncrawPh8B6hb7XgYFn3x1gtg436ijJHpSR3TrJGU3Kbq3kjCZkt15yRp2yTfIFUtldTdkm+RKAOS3ZSpK7TUpOmupIRARYFne3tuzRTC/97tvGoyRJ+h5txA1udWPPsxp9K3kutIdGCCh7jwiW0holgOz2Sc5oUHZ7JWeksn0fRJnkcWD8lWmT7EsNsvdN8iUoyg66d2QYsa0gnKZfvpwotXP1wG0n6ytUc0YPWAk9rqioQbLJvkiNkjP6rZfdgGSTDaw2JLm9sgNIbp/sgJKDym70Ohp3p7YpIj5bv3py/vC/CksisknsG2SqolTJXdySmMZTvz7gsy8vPjQl59rUWIO8OrLrkJxr22SLqEnuDJOc66PdsnE77TUkj9wS0FrZ+B1nKCU512e7ZIeQnOt7Dv/tZ/HLRmf9zzaiFwFM444g+6CyFw6djZU7wLSGG1WAp54D86rkIXmqni3Ms6GVRw3geyySc5l8ZXe1M3kBTFLt5CzUvHUWv2nkBJioK1tpcPPzWYWBxH5E+QyYrDtfKYBfMUrO5asie6DVv+Zj0rclfrYjIg+TJFEbyCiSJDkUkQciUmbejfLk7G3Gu1H5X16M0gduhspZCHB/hOyPRHAjgDvh+6kgZw+4FzpnIWllv+b8jMkx7uL/MRH9Y8FdKW3gbrKO06wD4BVwK3S+UuDes3QjdI5R4N4YFt/7kwzDMAzDMAzDMAzDMAzDMAzDiJO/YejpeKxLXHcAAAAASUVORK5CYII=";

        public static void Init()
        {
            SearchIcon = Base64Sprites.Base64ToSprite(SearchIconB64);
            SpeedIcon = Base64Sprites.Base64ToSprite(SpeedIconB64);
            StarIcon = Base64Sprites.Base64ToSprite(StarIconB64);
            GraphIcon = Base64Sprites.Base64ToSprite(GraphIconB64);
            XIcon = Base64Sprites.Base64ToSprite(XIconB64);

            AddToFavoritesIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongBrowser.Assets.AddToFavorites.png");
            PlaylistIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongBrowser.Assets.PlaylistIcon.png");
            RemoveFromFavoritesIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongBrowser.Assets.RemoveFromFavorites.png");
            StarFullIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongBrowser.Assets.StarFull.png");
            DownloadIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongBrowser.Assets.DownloadIcon.png");
            DeleteIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongBrowser.Assets.DeleteIcon.png");
            DoubleArrow = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongBrowser.Assets.DoubleArrow.png");
            BeastSaberLogo = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongBrowser.Assets.BeastSaberLogo.png");
            RandomIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongBrowser.Assets.RandomIcon.png");
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

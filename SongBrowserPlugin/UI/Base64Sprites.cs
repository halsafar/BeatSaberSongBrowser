﻿using System;
using System.Text.RegularExpressions;
using UnityEngine;


namespace SongBrowserPlugin.UI
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
        public static Sprite SortButtonStroke;
        public static Sprite BeastSaberLogo;

        // https://icons8.com/icon/132/search
        public static string SearchIconB64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAMRSURBVFhH7ZddiIxRGMfHx26slt1F2nKhFBeSC4UbonxcaCUprZTakpRILiibuJEkiewN5TMlJcpmlb1yscVyxZVcEBfC+qhFlhm/55z/O2a2mdl5P2ZmL+ZX/845z3Oe/3l25533nTdVp9pkMpkJ6XR6GdrN/BhjD+NJxv1oI/Mmba0uHD4XnUPvaaIo5IfRXaarVVpZOGgKB55AP1wHIaDmPpovq+TBvB0N6LwsxP4yDDLeRhfQNfQIDfsd/yE2hNbJMjkwtY/0jc5xsP6IDqB2bcuD+FS2bWZ84is8rEdQp7bFB88mDJ97ew/ry6hZW0rCdvsidaGfvtrV27W5XFvigdEl+TpYH1IqFNYQ5V+9i/N5i6YpHQ0MFiO7xhzMe5SKBPUb0B/Zmd9RpaKBwT15mdkrhgalIoPPGe/oPL+jNqXCQX0Lxb+9lTPbplQs8JmJXe5HvVOpcFC4XR5m8pkh9n8vAK8rzhjwvqNwOCi0x5aD+XWFEwG/LbI27w8Kh8P+MnmYSbfCiYDfQlkHNCpVPphkb7DMuxROBPyaZR0wT6nyweSxiq3BPQonAn6zZe1gXfBpVBKKbqneDI4rnAj4LZF1QKSP+JSKrcGHCicCfrtkbd6vFQ4HhevlYSa/GGYoFRv8er2z876ocDiobUC5N9QjSsUCn0Uo93HXoVR4KD4rHzP6huYoFQls7JdNn3d0nvb4nKh0eCieZY05N2BuP1jDX9CC+m7v5GG9Q6noYLJPfg7WvWi60mVDzUGUlo359CkVH8yuytfB+iVapXRJ2GevCjdVmoWYPQiS+eJh1IjhA+ecAzF7Eepk2qKtDtaTia9B51HRFyxyTxkSa3IShqedcwHIfUIv0Ds0ovCYsNeazPsDY4HZWkzz3lHGgv1fGA4zPvORfCrRpN0uNqEbaMidMgri9vY2gPaydIczb0PVaTIAU7veFqCVaCvqQCtQwRci4kWbhEGUfJNhKaPJVm2tHfUmk6JUk8T7ta220Evr6CZZ261pqbbUHprJNjnumgtQk/3jsrk6lSeV+gfT9/jq5kBt5gAAAABJRU5ErkJggg==";

        // https://icons8.com/icon/24520/playlist
        public static string PlaylistIconB64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAIcSURBVFhH7Za7SyNhFMXjE618NFqJj14EQSsXUQs7Kzu10EW2FwSxV2GbRWwsFrGwEVSws7EIloIPEFT8A6x0QYVFMfF3M8eAGGfCfJMxxfzg8M3cufd8J8NMklRCQkKZk81mGzKZzDBaRLtoRpfihzDVqIcQv9AGukCv1PJwfqb2wtBTidpQp4PqZJeD8yY23kJPHPtCz7nGCkPDkXpd+IdPlyzNc0f1QHwDcr2ahgev1Q18RmVrAQPv3Du+AQ0aBtASvSthxfxP1gpZ2gcvGmZPNRYf2jsQwhkLGosP7f8JwjyzHLOuogmO2zUSL7k0QIh7ZN91c8gepXq1FA9DP1BJnkHqWyqFAw97ix89Ozfwyb/FKrkHNDBJyy80eNyh/PegytEExKcUvyQ5IglYCpQvCRga5YsmICZ/kT3kLjpFLbKMLiAeVZi8eHZu4DMm28jv4CRe2y7C4zdrrSzL+xkkl/2LzlF2AQnUjFaVrzwCkqOVIPZn4BB9eJ4jCYhJB5rGbzZI9PVrzILVcP4H/ee4IFxzC4hHJSa3nl1R9GrO3v4Dr/Q1UQSswOTGs/OHvmuN2V2fUtkX+jY1Eh58GjEaYh3xEz3dGrGA+9QCoW9NI/HCxpfK4At98xqJFza+UoYgBjUSLwTcU4AvocdevhqNxAubj3kxfJlV+/dAyHUF+QB1Y1lt3wt5xgmzh05QGq2hPl1OSPhMKvUGb69EPdiK8gMAAAAASUVORK5CYII=";

        // https://github.com/andruzzzhka/BeatSaverDownloader/blob/master/BeatSaverDownloader/Misc/Base64Sprites.cs
        public static string AddToFavoritesIconB64 = "data:image/png;base64,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";
        public static string RemoveFromFavoritesIconB64 = "data:image/png;base64,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";
        public static string StarFullB64 = @"iVBORw0KGgoAAAANSUhEUgAAAIAAAACACAMAAAD04JH5AAAB5lBMVEX////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////ZkfksAAAAoXRSTlMAAQIDBAUGCAkKCwwNEBESExUWFxgZGxweHyQlJicrLC0vMTQ2Nzg5Oj9AQUJFSElKS0xOUlNUVldYWlxeX2BhYmRlaGtsbm9xcnR1dnd6e35/hYeIiouOk5SWmJmcn6Gio6Slp6ipra6xsrO1t7u8vb6/wcLDxMXGx8jJysvO0NHW19jZ2tvd3t/i5OXm6Ovt7/Dx8vP09fb3+Pn6+/z9/nFfuYEAAAQpSURBVHgB7dkJN1RhAwfw/8VQSCVFjaRQoSQtKbQv0hJlSdGuRKSlSEUhRUiNlAzm/03f88qZY5Z7Z5773Pvc9z2n39f44Z//byk3R0dvpsAxrmck+cwFp1RyUSUcEjvCRSOxcMZ+LtkPZ7zikldwRA79cuCE+/S7Dwds8NLPuwHqXeMy16DcykkuM7kSqp1igFNQTBtkgEENahUzSDHU6mSQTii1hSG2QKVmhmiGQmtnGGJmLdSpZhjVUCZ+gmFMxEOVCoZVAVX6GVY/FCmkjkKo0UYdbVDC7aMOnxsqXKeu61Bg1TR1Ta+C/S7QwAXYLm6UBkbjYLdSGiqF3XppqBc2284ItsNeDxnBQ9gqY54RzGfATvWMqB42SvrOiL4nwT5nGIUzsE3MMKMwHAO77GNU9sEWcem53YxKd256HKyipW7be7Kmqb1vYoECFib62ptqTu7dlqrBlJSs3RWXG1t7R72U5B3tbW28XLE7KwWRJboLjlQ1tLz8/Js2+P35ZUtD1ZECdyKCZOw8eLb2bteAh4p4Brru1p49uDMD/1X+gY75UA4U+uggXyHe0FFvMERHDaGLjupC9hwdNJcNHJ6lY2YPA8AuDx3i2YVFW8fpiPGtWLJxgA4Y2Ai/NT1UrmcNllnxmIo9XoEAsU1UqikWwaqpUDXCODFPReZPIKySX1TiVwl05H2jAt/yoCvzC233JRMG0vpos740GErupK06kxGB6x5tdM+FiLQ62qZOQzTO+WgL3zlE6dAf2uDPIUSt4Act96MAArK/0mJfsyEk4yMt9TEDglJe00KvUyAsoZWWaU2ACTE3aJEbMTDnCi1xBaYdm6O0uWOQsGeakqb3QEoxJRVDTikllUJOAyU1QE43JXVDimuGkmZckJFDaTmQcZrSTkPGHUq7AxmfKO0TJKymBVbDvCJaoAjm1dACNTCvgxbogHmTtMAkTHPTEm6YVUZLlMGsRlqiEWa9pyXew6QELy3hTYA5O2iRHTCnkhaphDkttEgLzBmhRUZgyjpGwetlFNbZlvUdmzd32Bb6VxnJcAkAlAwzkqsw4ymN/bwUj0Xxl37S2FOYEOOhEd/t9fBbf9tHI54YiMuikXd5CJD3jkayIO449U0c1RBEOzpBfcchrol6vPXJCCO53ks9TRDXTx1PMqEj8wl19ENY4jzDGiqGgeIhhjWfCFH5DGeqKh6G4qumGE4+RF1kKN+tNESUdsvHUBch6hFD9OYiKrm9DPEIosYYZLxcQ5S08nEGGYOgdAaarU2GgOTaWQZKh5gDDNC+CYI2tTPAAYip4zKDRTChaJDL1EHMc/p5zrtgiuu8h37PIeYFlyw0p8K01OYFLnkBMQ/4V08OpOT08K8HEOOeIsmxMg2StLIxkpxyQ1B+2/Tb6iRYIKn67XRbPv5X/fMfwXx1itpIs6EAAAAASUVORK5CYII=";

        // https://www.flaticon.com/free-icon/download_724933
        public static string DownloadIconB64 = "iVBORw0KGgoAAAANSUhEUgAAAIAAAACACAYAAADDPmHLAAAABGdBTUEAALGPC/xhBQAAACBjSFJNAAB6JgAAgIQAAPoAAACA6AAAdTAAAOpgAAA6mAAAF3CculE8AAAACXBIWXMAAA3WAAAN1gGQb3mcAAAAB3RJTUUH4gscDB0RZoF8lQAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMS4xYyqcSwAABOpJREFUeF7t3D+OVUcQxeFBIiQgQ56EjIRdsA4WwQKQTErmnMgJXgCyvArkzIEzC3mwHDmy5Ghc/e4vwJKbmal5Z+a8vueTOuup6luHf90BZyu6vLw8r/W+1kWt2xo1Rq1zyoezEVStP2sd26iZXwTuKqTxu1XlPW3CVYV0jD/2Zy5oE64ISoY24YqcZGgTrshJhjbhipxkaBOuyEmGNuGKnGRoE67ISYY24YqcZGgTrshJhjbhipxkaBOuyEmGNuGKnGRoE67ISYY24YqcZGgTrshJhjbhipxkaBOuyEmGNuGKnGRoE67ISYY24YqcZGgTrshJhjbhipxkaBOuyEmGNuGKnGRoE67ISYY24YqcZGgTrshJhjbhipxkaBOuyEmGNuGKnGRoE67ISYY24YqcZGgTrshJhjbhipxkaBOuyEmGNuGKnGRoE67ISYY24YqcZGgTrshJhjbhipxkaBOuyEmGNuGKnGRoE67ISYY24YqcZGgTrshJhjbhipxkaBOuyEmGNuGKnGRoE67ISYY24YqcZGgTrshJhjbhipxkaBPHVHN9Wuu7Wr+OIS9ufOP41qd8/r7VIB7X+qXW3oxvfswY9quG8O4wjn16xxj2q4bwcZvFLn1kDPtVQ/i8zWKXPjOG/aohvN1msUtvGcN+1RCe1PrrMI59Gd/8hDHsWw3iRa1/xlR2YnzrCz4/hhrIy8No9uElnx1fqsG83uaztNd8bvyfGtDK7wK591+lhvSw1k+Hca1lfNNDPjO+pgb1qNbPY2qLGN/yiM+L66iBndf6bUzvxI1vOOez4iZqcM9rnfIbwTj7cz4nOmqAp/pGkLv+sdQgT/GNIHf9Y6qBntIbQe76CjXYU3gjyF1fpYbr/kaQu75aDdj1jSB3/btSg3Z7I8hd/67VwF3eCHLXvy81+Pt+I8hd/75VAPf5RpC7voMK4j7eCHLXd1KB3OUbQe76biqUu3ojyF3fVQWjfiPIXd9dBaR6I8hd/1RUUMd+I8hd/9RUYMd6I9jXXX/75j7KWKjjHOONwOquz5naKDPHvjbK2Kgj3eaNwO6uz7naKDPHvjbKWKljdd4ILO/6nK2NMnPsa6OMlTrWTd8IbO/62/H6KDPHvjbK2KmjXfeNwPquvx2xjzJz7GujjKU63lVvBPZ3/e2YfZSZY18bZWzVEccbwR+Hw/7X+J9K7O/621H7KDPHvjbKWKtjflPrh1q/1/pU6/taJ/GfNdQ5b4Uyc+xro0yIMOY2ysyxr40yIcKY2ygzx742yoQIY26jzBz72igTIoy5jTJz7GujTIgw5jbKzLGvjTIhwpjbKDPHvjbKhAhjbqPMHPvaKBMijLmNMnPsa6NMiDDmNsrMsa+NMiHCmNsoM8e+NsqECGNuo8wc+9ooEyKMuY0yc+xro0yIMOY2ysyxr40yIcKY2ygzx742yoQIY26jzBz72igTIoy5jTJz7ItFEfMc+2JRxDzHvlgUMc+xLxZFzHPsi0UR8xz7YlHEPMe+WBQxz7EvFkXMc7XnYtsaC7og5rna9GHbGwv6QMxztenbbW8s6A0xz9WmZ7X+PmyPlYxMnxHz19XGV4cfiZW8It6r1eYHtX48/FisYGT5gHivZ/xArfEnQf46OF0ju5HhzcL/Uv3w+DfBm1rjdpAror+R0chqZHbF3/lnZ/8CEi2he6BAVgUAAAAASUVORK5CYII=";

        // https://icons8.com/icon/41152/speed
        public static string SpeedIconB64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAQoSURBVFhH7Zfbi1VlGMbHQwo6WVGeIUGUwAvxFMQIoRfqpEKB1EWIh0HrIoSO4N9QE6SgV9pFgShqN5WKYF1EImgFQaEZHogUKXXEoqyc6fe87zNr7z1rRvee0QZi/+Dh+97nPay91957rbVbmjRpcp/o7u6ejbagvegbdBX9ZWkvT7lX0Gy33V96enrGcLCN6CT7hqDnBMsG9IDH3Vs4wBp0IY42BJhxHj3rsUOHmQ8z8GCOrwX/NjqOOpE+yhct7eWdQLddXgP+PjTBhxkczJnJkB9zZAW871heYp3s0gGhZip6GX2f3RXwzrDMdGlj0DwLXc5RCfEltIntaJcV4C9A+vjOofm2C9SD9Kb6zryMZrmsPmiYhn7yjID4CzTJ+SfQUuwR0QDEu7IyanfZjhdGvAzNUMw6GX2ZlQnxeZYp0XA3KBxFw+fRaYg/QGOVZ21Df9t/O5qAUL/QAH+9bdXvsfc7issN61j0YRQb4mMso6LpTlD4ZrYkxEdYikbiVzMTueO2A+JF2AsdBngXszrqX7DdeyKOZiYhfs3p/qFAX+ibrlfDaZaHnA6Ip+CfRL+g1bYHhJoOdB19hsbbDjQb76yOJdjfYBn4o6bg3SxNiJ+Wz/YRKYruAcx6jNkPas+q73IB8TtR1BdyrSSrz97H8lmfQX+iP1B7FA8SxupO9AbSGS1eCPtP86hx3Juo5kwHmOtc08s8+7sdq3l3FA8CevVGqz9Oven40RAuTDfBXxtN1WDud14FZ23Lb0c6e9Jy23VDzxx02KNrwH/LZaorbgjs99qugPmr8yrotB1gNfwdpF7fsx0oLknV4H3NssSlAV7x/Wd/xXaCMdW5gLihM0X9BLQT6U5zDR1A1z2uwPkOtiPdWoC/MqsS4sptlKDNfi+P25+OlqO4SPcHtSPIH8q2/iF/C3WyLS5Zmok0e7rjGVmdED8VhYJglf2AeLwa2XY5PuTSEuTmqGYgyH+ESvdazXRev+hpbFsV94K30qVxFp5Lu0BX+XbvRZdLS1C3wjUlyG1yWQlyuigHnqGHiQK8yjMjQd+L5aNIH4HeZRfrFpeWIDcJ3crOCvLQRJeVIKdbpmZ/wqrr48RoNMRLXRpncF7aCcnS49KdoF4X3263q1+87nRdUL/A7QHxXKfiBY7D+CdTkexwqm5oW0Lfe2ib9rbrhr7NcXBgr0vTOKcSzG8zHQX7bevAep5bj3Snufvj0CBh/oE4OOi12K6Aud15Feie3CqfdWuYydYoHgLMfhLpevgzWmRP19HffAwdf1sUV4O52PmAOH4YKrbVf2ODMON9j9O8uLfrWLYC4rYorgZfF9wfsmT44DXoj1TxV6IGEsVj+3DBCyz+LpQgrx/EV1k6LJxCpX+MNVCgB1fd+p7/L8Ux9bAQP8wmTZr8f2hp+ReWCz5Y6C35EgAAAABJRU5ErkJggg==";

        // https://icons8.com/icon/10159/christmas-star
        public static string StarIconB64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAANKSURBVFhH7Zi9S5VhGMa1o0Ik1tRezUIUlENDEFQIkVtDNDRVlFAkFQlB1HoICZqaXGyJkKD8AxoEocUlsjYLKS2jhr7UftfzXD75qsfO8X3znMEfXJz3/rqe2+P5btqkkVhYWNghOWw85ufnb0kOGwsW60CfrA6nGweWusm/N6BrpxsDdmpnqem4XlhQ1+0u1x8Wuh5Xy3DD5frCctvQBy+VUE41t9UPlrjqnVZArc9t9YEFtqIp76OFXkgOFU+px+0bD4df9i4B4qOSwwDxFbdvLLpn0DvvoUVGXVJt1Gnl36vXpY2DQy95hwBxt0uqdTsdIO51qVjwLqHdHHAMXUQD6BmaQL/C6cD1mEcSyrms+m/0Bo2g+6gXHUd7KLd4pDI06m3qCDqPymgYvUI/4hFrQ98JWyWUc3lNdIbPeop0tnbQLn/fLgn0eHrumZpg7iU3zbZKKOdazWgX7WSrCPk2ksOxZVVmqY+hIXQbnUYHUMUPBqq5R72a0az+9bPRciXUtUObLbKoQMPj0GmIv3Nz2C2FIU97J3z26sstQkMLjUNhwhDrrWyvW3IjL3smfOa/nzCCxhIDg2HSEE+jfW5ZN/KQl20DPqvklurQAIMPg4Mh/owOuqVmNCsP2wV8Rm3LLcKgnokPgpMh/sLNIbdUjWY8m7D3ileAmpABRgPB0RB/46bqx6R6PZOwZ77lloLhvWidqPpeVG8cicjLpeLAtyfaJ6r+eqneOJLocak4+Kv7ba57YNLpqtGMxzXf73RxYPrI/jpgxOkMlLZLDjNoJgyDvJwuDkzH7a8Dyk4HiPVefg3ptfIj6lPO5QBx2eOaH3e6GPBsxfRntA8HnF2S16eP9MF1EXKT6ByX4d1BM7ESavJqVb4QMOyM1hHiLqQPABNOVUQ97u1yKkDcafv8YHbKvgHi177MQH5Ucphh+Yw8bZ8fzO7ad1Woj6OTXDZLulYuFCsgT9vnB7Mn9s1A/i06w+UWtyaUU009oXkZ8nRrfjDLPNaI9U3tApdrf3YD9ahXM2HYyNMt+cBIP23M2XQG6eWk5q+RmvHsjL3mUP6fRvDaj9FXdIfr3L+eykNe8pS30+sHo11op8PCkKe8HW7yn2hq+gPI2OoDwCE0owAAAABJRU5ErkJggg==";

        // https://icons8.com/icon/3005/graph
        public static string GraphIconB64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACgAAAAoCAYAAACM/rhtAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAALaSURBVFhH7Zg7aBRRGIXHiCRGMQRUfIQgNgqaQkVEjQ8QVAiIiJVprIQoaKURgmAXUCuL4KNZFJsVO0vxUSgIioWKgi9sREElPhLjg12//3pmGTfX3XGygyPMgY975/z/vffsJruzu0GumCqXy62lUqkXlsjKlghYhDIBxxg6ZWdHBLtrAU3MV8nOhgi0Gr7DMxiQnQ0RaAY8h3fQITs7ItR5/Vl3yMqOCLVL4c7KSlcctAzm67KmyLWA3g/wGKbJTk8cclDPxigsle0VbZPpuQlfma+QnZ44qBnuWUAT89uwQeVxouWo+g7JSk8c0mGBdOALuAYjdo2uw0a1OlFbAz/gCrUmaIF2D21aklwcsh5ewzfYK9v82XACPnOQBb8Bm8De7+xBvAX3v0r5sPVUi/pLt1lSscF+sGCvYK3s34RvQY+DCxrRbrVUAtIzCP3iESQLyF4tLD6nTW9B3VctPRb0vq0xMe9RKfoMVu6/1C/D3wVkgb2FPIA3thvjKWhWua7oXceyO1CAKbIbE9A2h+jN/KRKExbbJQvIgnaaDsBDW81oN3PTGKxU24TF1vECYsyiqcB4RuOorWK0G/kAzINFMFdLGiKOiB1wUI0Wyl6dl2ALl01q8YqenfDeh1pqiv1jB9ynRgvYK7uu6A1v+FcZ7FNxkflT89RSU7TFC0jDJMxtsFVWLNHvAqJuWeYNmaHLmqIt2Yskrv77gMz7XHW8hq3OmI2AjBfhtHiC1ZiANHZpUx9TIW7ALldEzC9gNSxgjzbwqY16NgLaKMu88D0z/YAU5sCePzCdxn8esPtX3atOGrMREOMIw0KD+THzUKYC9jkDcVlZhJ8HtDrKA+YB84CuiJgnCjgC4cd1930ERQN+itS/uGokIOPHSN1+Z64OaL9khXX70ag64HCkbl85KgEXg/uo7mEmjcs9fkgr2AP01Yq2P+s3+2pQ0Pnbq3wH64aCIAh+AtEFoTRH3sIDAAAAAElFTkSuQmCC";

        // https://icons8.com/icon/43864/rounded-rectangle-stroked
        public static string SortButtonStrokeB64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAGAAAABgCAYAAADimHc4AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAMWSURBVHhe7d1JbhRRFERRLwOGNKtAlmiWBF6MbXZCuwqaNWCYIAaMDO/DnVQFT7ws1wjukb5AGRkfKSRATJITSZIkSZIk6U+ur6+f1Lms87HOtx/6ZW1R50OdizqPmet46tJ7dd7y6+kvaqs3azPmu5m677Qu+/L7ak3VZp/rh1NmPExdcr/O1e8rtVVt92ltyJzbVfkNd+lAteEr5tymik+4QzdUW27/i7lKz+nvqOff6zytn97i1f/e2mJtsrb5NdKeen7Bq3NV+kB/Rz1/xivaU9ucMdOOev6eV+aq9JX+jnp+m1e0Z23DTDvWlrwyRzcQq8FMgXiOXiBWg5kC8Ry9QKwGMwXiOXqBWA1mCsRz9AKxGswUiOfoBWI1mCkQz9ELxGowUyCeoxeI1WCmQDxHLxCrwUyBeI5eIFaDmQLxHL1ArAYzBeI5eoFYDWYKxHP0ArEazBSI5+gFYjWYKRDP0QvEajBTIJ6jF4jVYKZAPEcvEKvBTIF4jl4gVoOZAvEcvUCsBjMF4jl6gVgNZgrEc/QCsRrMFIjn6AViNZgpEM/RC8RqMFMgnqMXiNVgpkA8Ry8Qq8FMgXiOXiBWg5kC8Ry9QKwGMwXiOXqBWA1mCsRz9AKxGswUiOfoBWI1mCkQz9ELxGowUyCeoxeI1WCmQDxHLxCrwUyBeI5eIFaDmQLxHL1ArAYzBeI5eoFYDWYKxHP0ArEazBSI5+gFYjWYKRDP0QvEajBTIJ6jF4jVYKZAPEcvEKvBTIF4jl4gVoOZAvEcvUCsBjMF4jl6gVgNZgrEc/QCsRrMFIjn6AViNZgpEM/RC8RqMFMgnqMXiNVgpkA85webtjvqB5uq1H2y7IxXtGdtw0w76vlBnyy7pL+jnq+P9j2r4+8ErC3YpPto3zmvzlXJz1YeSW35kFm3qeJr7tCBasOXzLldldeniz9xlzZiu7vMeZi6YH28e32IWhvUZld1HjDjzdRF6/P1/nE0VFu9qHOH+Y6n7n5UF6//pOBdnT/+O+F/tLZgk/M6h/2FK0mSJEmSJEmSJP2DTk5+AiwUNBjJryYYAAAAAElFTkSuQmCC";                

        public static void Init()
        {
            SearchIcon = Base64Sprites.Base64ToSprite(SearchIconB64);
            PlaylistIcon = Base64Sprites.Base64ToSprite(PlaylistIconB64);
            AddToFavoritesIcon = Base64Sprites.Base64ToSprite(AddToFavoritesIconB64);
            RemoveFromFavoritesIcon = Base64Sprites.Base64ToSprite(RemoveFromFavoritesIconB64);
            StarFullIcon = Base64Sprites.Base64ToSprite(StarFullB64);
            DownloadIcon = Base64Sprites.Base64ToSprite(DownloadIconB64);
            SpeedIcon = Base64Sprites.Base64ToSprite(SpeedIconB64);
            StarIcon = Base64Sprites.Base64ToSprite(StarIconB64);
            GraphIcon = Base64Sprites.Base64ToSprite(GraphIconB64);
            DeleteIcon = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongBrowserPlugin.Assets.DeleteIcon.png");

            SortButtonStroke = Base64Sprites.Base64ToSprite(SortButtonStrokeB64);

            BeastSaberLogo = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongBrowserPlugin.Assets.BeastSaberLogo.png");            
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

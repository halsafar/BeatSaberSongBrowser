using Mobcast.Coffee.AssetSystem;
using UnityEngine.Networking;

namespace SongBrowserPlugin.DataAccess.Network
{
    /// <summary>
	/// Cacheable download handler for score saber tsv file.
	/// </summary>
	public class CacheableDownloadHandlerScoreSaberData : CacheableDownloadHandler
    {
        ScoreSaberDataFile _scoreSaberDataFile;

        public CacheableDownloadHandlerScoreSaberData(UnityWebRequest www, byte[] preallocateBuffer)
            : base(www, preallocateBuffer)
        {
        }

        /// <summary>
        /// Returns the downloaded score saber data file, or null.
        /// </summary>
        public ScoreSaberDataFile ScoreSaberDataFile
        {
            get
            {
                if (_scoreSaberDataFile == null)
                {
                    _scoreSaberDataFile = new ScoreSaberDataFile(GetData());

                }
                return _scoreSaberDataFile;
            }
        }
    }
}

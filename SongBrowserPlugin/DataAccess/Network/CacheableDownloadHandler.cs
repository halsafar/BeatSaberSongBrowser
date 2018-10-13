using UnityEngine.Networking;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using System;

// Modified Version of:
// https://github.com/mob-sakai/AssetSystem/blob/master/Assets/Mobcast/Coffee/AssetSystem/CacheableDownloadHandler.cs
// MIT-LICENSE - https://github.com/mob-sakai/AssetSystem/blob/master/LICENSE
// Modified to use custom logging only.
namespace Mobcast.Coffee.AssetSystem
{

	public static class UnityWebRequestCachingExtensions
	{
		/// <summary>
		/// Set UnityWebRequest to be cacheable(Etag).
		/// </summary>
		public static void SetCacheable(this UnityWebRequest www, CacheableDownloadHandler handler)
		{
			var etag = CacheableDownloadHandler.GetCacheEtag(www.url);
			if (etag != null)
				www.SetRequestHeader("If-None-Match", etag);
			www.downloadHandler = handler;
		}
	}

	/// <summary>
	/// Cacheable download handler texture.
	/// </summary>
	public class CacheableDownloadHandlerTexture : CacheableDownloadHandler
	{
		Texture2D m_Texture;

		public CacheableDownloadHandlerTexture(UnityWebRequest www, byte[] preallocateBuffer)
			: base(www, preallocateBuffer)
		{
		}

		/// <summary>
		/// Returns the downloaded Texture, or null.
		/// </summary>
		public Texture2D Texture
		{
			get
			{
				if (m_Texture == null)
				{
					m_Texture = new Texture2D(1, 1);
					m_Texture.LoadImage(GetData(), true);
				}
				return m_Texture;
			}
		}
	}

	/// <summary>
	/// Cacheable download handler.
	/// </summary>
	public abstract class CacheableDownloadHandler : DownloadHandlerScript
	{
        private static SongBrowserPlugin.Logger _log = new SongBrowserPlugin.Logger("CacheableDownloadHandler");

        const string kLog = "[WebRequestCaching] ";
		const string kDataSufix = "_d";
		const string kEtagSufix = "_e";

		static string s_WebCachePath; 
		static SHA1CryptoServiceProvider s_SHA1 = new SHA1CryptoServiceProvider();

		/// <summary>
		/// Is the download already finished?
		/// </summary>
		public new bool isDone { get; private set; }


		UnityWebRequest m_WebRequest;
		MemoryStream m_Stream;
		protected byte[] m_Buffer;

		internal CacheableDownloadHandler(UnityWebRequest www, byte[] preallocateBuffer)
			: base(preallocateBuffer)
		{
			this.m_WebRequest = www;
			m_Stream = new MemoryStream(preallocateBuffer.Length);
		}

		/// <summary>
		/// Get path for web-caching.
		/// </summary>
		public static string GetCachePath(string url)
		{
			if (s_WebCachePath == null)
			{
				s_WebCachePath = Application.temporaryCachePath + "/WebCache/";
				_log.Debug("{0}WebCachePath : {1}", kLog, s_WebCachePath);

			}

			if (!Directory.Exists(s_WebCachePath))
				Directory.CreateDirectory(s_WebCachePath);

			return s_WebCachePath + Convert.ToBase64String(s_SHA1.ComputeHash(UTF8Encoding.Default.GetBytes(url))).Replace('/', '_');
		}

		/// <summary>
		/// Get cached Etag for url.
		/// </summary>
		public static string GetCacheEtag(string url)
		{
			var path = GetCachePath(url);
			var infoPath = path + kEtagSufix;
			var dataPath = path + kDataSufix;
			return File.Exists(infoPath) && File.Exists(dataPath)
					? File.ReadAllText(infoPath)
					: null;
		}

		/// <summary>
		/// Load cached data for url.
		/// </summary>
		public static byte[] LoadCache(string url)
		{
			return File.ReadAllBytes(GetCachePath(url) + kDataSufix);
		}

		/// <summary>
		/// Save cache data for url.
		/// </summary>
		public static void SaveCache(string url, string etag, byte[] datas)
		{
			var path = GetCachePath(url);
			File.WriteAllText(path + kEtagSufix, etag);
			File.WriteAllBytes(path + kDataSufix, datas);
		}

		/// <summary>
		/// Callback, invoked when the data property is accessed.
		/// </summary>
		protected override byte[] GetData()
		{
			if (!isDone)
			{
				_log.Error("{0}Downloading is not completed : {1}", kLog, m_WebRequest.url);
				throw new InvalidOperationException("Downloading is not completed. " + m_WebRequest.url);
			}
			else if (m_Buffer == null)
			{
				// Etag cache hit!
				if (m_WebRequest.responseCode == 304)
				{
					_log.Debug("<color=green>{0}Etag cache hit : {1}</color>", kLog, m_WebRequest.url);
					m_Buffer = LoadCache(m_WebRequest.url);
				}
				// Download is completed successfully.
				else if (m_WebRequest.responseCode == 200)
				{
					_log.Debug("<color=green>{0}Download is completed successfully : {1}</color>", kLog, m_WebRequest.url);
					m_Buffer = m_Stream.GetBuffer();
					SaveCache(m_WebRequest.url, m_WebRequest.GetResponseHeader("Etag"), m_Buffer);
				}
			}

			if (m_Stream != null)
			{
				m_Stream.Dispose();
				m_Stream = null;
			}
			return m_Buffer;
		}

		/// <summary>
		/// Callback, invoked as data is received from the remote server.
		/// </summary>
		protected override bool ReceiveData(byte[] data, int dataLength)
		{
			m_Stream.Write(data, 0, dataLength);
			return true;
		}

		/// <summary>
		/// Callback, invoked when all data has been received from the remote server.
		/// </summary>
		protected override void CompleteContent()
		{
			base.CompleteContent();
			isDone = true;
		}

		/// <summary>
		/// Signals that this [DownloadHandler] is no longer being used, and should clean up any resources it is using.
		/// </summary>
		public new void Dispose()
		{
			base.Dispose();
			if (m_Stream != null)
			{
				m_Stream.Dispose();
				m_Stream = null;
			}
		}
	}
}
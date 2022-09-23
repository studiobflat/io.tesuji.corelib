using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Tesuji
{
	[Serializable] internal class ImageCatalog
	{
		[Serializable]
		public class Info
		{
			public string url;
			public string fileName;
			[NonSerialized] public Texture2D texture;
		}

		private const string CATALOG_FILENAME = "image_catalog.json";
		private static ImageCatalog api = new ImageCatalog();

		// STATIC APIs
		public static Texture2D LoadFromDisk(string url)
		{
			return api.internal_LoadFromDisk(url);
		}

		public static void ReleaseRAM()
		{
			api.internal_ReleaseRAM();
		}

		public static void Add2Cache(Texture2D tex, string url)
		{
			api.internal_Add2Cache(tex, url);
		}


		// INTERNAL APIs
		public List<Info> data = new List<Info>();
		private bool _loaded = false;

		private Info FindImageCache(string url)
		{
			if (!_loaded) Load();
			for (var i = 0; i < data.Count; i++)
			{
				if (data[i].url == url) return data[i];
			}

			return null;
		}

		private void internal_ReleaseRAM()
		{
			if (!_loaded) return;

			// Remove reference to textures
			for (var i = 0; i < data.Count; i++)
			{
				data[i].texture = null;
			}
		}

		private bool internal_Add2Cache(Texture2D tex, string url)
		{
			if (string.IsNullOrEmpty(url))
			{
				Debug.LogWarning("url should not be null or empty!");
				return false;
			}

			if (tex == null)
			{
				Debug.LogWarning("tex should not be null or empty!");
				return false;
			}

			Info cache = FindImageCache(url);
			if (cache != null) return false;

			var hash = new Hash128();
			hash.Append(url);
			hash.Append(tex.name);

			var fileName = $"{hash.ToString()}.png";
			if (!TesujiFileIO.WriteImage(fileName, tex)) return false;

			data.Add(new Info() {fileName = fileName, texture = tex, url = url});
			DelaySave();
			return true;
		}

		private Texture2D internal_LoadFromDisk(string url)
		{
			Info cache = FindImageCache(url);
			if (cache == null) return null;
			if (cache.texture != null) return cache.texture;

			Texture2D result = TesujiFileIO.ReadImage(cache.fileName);
			if (result == null) // Actual file deleted : remove from cache as well
			{
				data.Remove(cache);
				DelaySave();
				return null;
			}

			// save for next time
			cache.texture = result;
			return result;
		}

		private void DelaySave()
		{
			TesujiUpdateManager.DelayCall(Save, 60); // Save once every 1 secs (if dirty)
		}

		private void Save()
		{
			TesujiFileIO.WriteText(CATALOG_FILENAME, JsonUtility.ToJson(this));
		}
		
		private void Load()
		{
			if (_loaded)
			{
				Debug.LogWarning("Image Catalog would be read from disk once!");
				return;
			}

			_loaded = true;
			var json = TesujiFileIO.ReadText(CATALOG_FILENAME);
			JsonUtility.FromJsonOverwrite(json, this);
		}
	}
	
	public static class TesujiImageLoader
	{
		public class LoaderItem
		{
			public string url;
			public Action<Texture2D> onComplete;
		}

		private static readonly Dictionary<string, Texture2D> loadedMap = new Dictionary<string, Texture2D>();
		private static readonly Dictionary<string, LoaderItem> loadingMap = new Dictionary<string, LoaderItem>();

		public static void Load(string url, Action<Texture2D> onComplete = null)
		{
			if (string.IsNullOrEmpty(url))
			{
				Debug.Log($"Can not load a null url!");
				return;
			}
#if VERBOSE_LOG
        Debug.Log($"Load: {url}");
#endif

			if (loadedMap.TryGetValue(url, out Texture2D result))
			{
				onComplete?.Invoke(result);
				return;
			}

			if (loadingMap.TryGetValue(url, out LoaderItem ldi))
			{
				if (onComplete == null) return;
				ldi.onComplete -= onComplete;
				ldi.onComplete += onComplete;
				return;
			}

#if VERBOSE_LOG
        Debug.Log($"CheckLocal: {url}");
#endif

			// Check Local
			Texture2D tex = ImageCatalog.LoadFromDisk(url);
			if (tex != null)
			{
				loadedMap.Add(url, tex);
				onComplete?.Invoke(tex);
				return;
			}

			// Load from Web
			TesujiUpdateManager.StartRoutine(LoadImageRoutine(new LoaderItem()
			{
				url = url,
				onComplete = onComplete
			}));
		}

		static IEnumerator LoadImageRoutine(LoaderItem item)
		{
#if VERBOSE_LOG
        Debug.Log($"Start load: {item.url}");
#endif

			loadingMap.Add(item.url, item);
			UnityWebRequest request = UnityWebRequestTexture.GetTexture(item.url);
			request.SendWebRequest();

			while (!request.isDone)
			{
#if VERBOSE_LOG
            Debug.Log($"loading : {request.downloadedBytes} bytes\n{item.url}");
#endif

				yield return new WaitForSeconds(1f);
			}

			loadingMap.Remove(item.url);

			if (request.result != UnityWebRequest.Result.Success) // failed
			{
				Debug.LogWarning($"LoadImageRoutine error: {request.error}\n{item.url}");
				yield break;
			}

#if VERBOSE_LOG
        Debug.Log($"Load complete: {item.url}");
#endif


			Texture2D tex = ((DownloadHandlerTexture) request.downloadHandler).texture;
			loadedMap.Add(item.url, tex);
			ImageCatalog.Add2Cache(tex, item.url);
			item.onComplete?.Invoke(tex);

#if VERBOSE_LOG
        Debug.Log($"End load: {item.url}");
#endif
		}
	}
}
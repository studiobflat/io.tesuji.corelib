#pragma warning disable

// #define VERBOSE_LOG
// #define STANDALONE

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;



// [+] SUPPORT VERSION
// SUPPORT TIMEOUT


// SUPPORT BRIDGE LOADING - when an item finish loading, checkback the queue to see if we can bridge for other items
// SUPPORT ALTERNATE LINKS
// SUPPORT ZIP / 7ZIP
// SUPPORT ENCRYPTION
// SUPPORT cache / merge loading progress
// SUPPORT DATA VALIDATOR (JSON / TSV / CSV / XML / HTML ... )
// SUPPORT RELATIVE LINKS
// SUPPORT WEAK REFERENCE CACHE
// SUPPORT RETRY WITH DELAY
// SUPPORT LOAD TO FILE STREAM / BYTE STREAM
//	LOADER INFO


public partial class Loader : MonoBehaviour
{
	public enum LDStatus
	{
		Idle,
		LCExist, // for local only
		Start,
		Failed, // handle soft / hard fail?
		Success,
	}

	[Serializable]
	public class LDInfo
	{
		const float LOCAL_WEIGHT = 0.2f;
		const float WEB_WEIGHT = 1 - LOCAL_WEIGHT;
		const string LOCAL_AUTOID = "$auto_id";



		// CONTROL LOCAL CACHE POLICY
		internal string localId = LOCAL_AUTOID;
		public LDInfo DisableLocalCache()
		{
			localId = null;

			if (lcStatus != LDStatus.Idle)
			{
				Debug.LogWarning("Something wrong! invalid lcStatus: " + lcStatus);
			}

			lcStatus = LDStatus.Failed;
			return this;
		}

		public LDInfo SetLocalCacheId(string localId)
		{
			this.localId = localId;
			return this;
		}


		// CONTROL TIMEOUT
		internal int timeout;
		public LDInfo SetTimeoutSecs(int secs)
		{
			timeout = secs;
			return null;
		}

		// OnStart?
		internal Action<LDInfo> onComplete;
		internal Action<LDInfo> onError;
		internal Action<LDInfo> onProgress;

		public LDInfo SetOnComplete(Action<LDInfo> onComplete)
		{
			this.onComplete = onComplete;
			return this;
		}

		public LDInfo SetOnError(Action<LDInfo> onError)
		{
			this.onError = onError;
			return this;
		}

		public LDInfo SetOnProgress(Action<LDInfo> onProgress)
		{
			this.onProgress = onProgress;
			return this;
		}

		internal string id;





		// 
		public string url;


		internal Type dataType;


		internal int version;
		public object loadedData;


		// Callbacks


		public string errorMessage;
		public LDStatus lcStatus;
		public LDStatus wbStatus;


		public bool isFromCache
		{
			get { return lcStatus == LDStatus.Success; }
		}

		public bool isFromWeb
		{
			get { return wbStatus == LDStatus.Success; }
		}

		internal string _localPath;
		public string localPath
		{
			get
			{
				if (!string.IsNullOrEmpty(_localPath)) return _localPath;
				if (string.IsNullOrEmpty(localId))
				{
#if VERBOSE_LOG
                    Debug.LogWarning("[Editor] Something wrong: localId is null --> can not get localPath");
#endif

					return null;
				}

				_localPath = Loader.GetLocalPath("data1", localId, version, true);
				return _localPath;
			}
		}

		public bool willCheckWeb
		{
			get
			{
				return (lcStatus == LDStatus.Failed) && wbStatus == LDStatus.Idle;
			}
		}

		public bool isDone { get { return isSuccess || isFailed; } }
		public bool isLoading { get { return (lcStatus == LDStatus.Start) || (wbStatus == LDStatus.Start); } }
		public bool isSuccess { get { return (lcStatus == LDStatus.Success) || (wbStatus == LDStatus.Success); } }
		public bool isFailed { get { return wbStatus == LDStatus.Failed; } } //(lcStatus == LDStatus.Failed) && 

		public float progress { get; private set; }

		public void Retry()
		{
			if (wbStatus != LDStatus.Failed)
			{
				Debug.LogWarning("Retry() failed - Invalid status: must be LDStatus.Failed! status=" + wbStatus);
				return;
			}

			lcStatus = LDStatus.Idle;
			wbStatus = LDStatus.Idle;

#if VERBOSE_LOG
			Debug.LogWarning("Retrying: " + this.url);
#endif

			Load(this);
		}

		float CalculateProgress()
		{
			// Debug.Log("Calculate progress: " + progress);
			if (isSuccess) return (progress = 1);
			if (isFailed) return (progress = 0);

			if (lcStatus == LDStatus.Start)
			{
				return (progress = LOCAL_WEIGHT * lcRequest.downloadProgress);
			}

			if (wbStatus == LDStatus.Start)
			{
				return progress = LOCAL_WEIGHT + WEB_WEIGHT * webRequest.downloadProgress;
			}

			return 0;
		}

		public void TriggerCallbacks()
		{
			if (isSuccess)
			{
				progress = 1;
				onComplete?.Invoke(this);
				return;
			}

			if (isFailed)
			{
				progress = 0;
				onError?.Invoke(this);
				return;
			}

#if VERBOSE_LOG
            Debug.LogWarning("[Editor] Invalid state!");
#endif
		}


		public T GetLoadedData<T>()
		{
			if (typeof(T) != dataType)
			{
#if VERBOSE_LOG
                Debug.LogWarning("[Editor] Invalid cast: " + dataType + " --> " + typeof(T));
#endif

				return default(T);
			}

			return (T)loadedData;
		}

		public void UpdateProgress()
		{
			if (lcStatus == LDStatus.Start)
			{
				// Debug.Log("Local Before: " + lcStatus);
				UpdateProgress(ref lcStatus, lcRequest);
				// Debug.Log("Local After: " + lcStatus);
				return;
			}

			if (wbStatus == LDStatus.Start)
			{
				if (UpdateProgress(ref wbStatus, webRequest)) //write cache
				{
					if (string.IsNullOrEmpty(localId)) return;
#if VERBOSE_LOG
					Debug.LogWarning("Writing cache: " + localPath + " | " + wbStatus  + " --> " + localId);
#endif
					try
					{
						File.WriteAllBytes(localPath, webRequest.downloadHandler.data);

						// write success
						if (!string.IsNullOrEmpty(localId))
						{
							fvManager.Write(localId, version);
						}
					}
					catch (Exception e)
					{
#if VERBOSE_LOG
                        Debug.LogWarning("Write cache error: " + url + "\n" + localPath + "\n" + e);
#endif
					}
				}

				// Debug.Log("After: " + wbStatus);
				return;
			}

#if VERBOSE_LOG
            Debug.LogWarning("Invalid state! " +wbStatus);
#endif
		}

		void Request_Texture(string link, ref LDStatus status, ref UnityWebRequest request)
		{
			status = LDStatus.Start;
			request = UnityWebRequestTexture.GetTexture(link, true);
			request.SendWebRequest();
		}

		void Request_AudioClip(string link, ref LDStatus status, ref UnityWebRequest request)
		{
			// Debug.Log("request AudioCLip: " + request);

			if (request != null)
			{
				Debug.LogWarning("Something wrong! " + status);
				return;
			}

			status = LDStatus.Start;
			request = UnityWebRequestMultimedia.GetAudioClip(link, AudioType.UNKNOWN);
			request.SendWebRequest();
		}

		bool UpdateProgress(ref LDStatus status, UnityWebRequest request)
		{
			if (request == null)
			{
#if VERBOSE_LOG
                Debug.LogWarning("Something wrong: " + url);
#endif

				status = LDStatus.Failed;
				return false;
			}

			if (!request.isDone)
			{
				CalculateProgress();
				if (onProgress != null) onProgress(this);
				return false;
			}

			var isError = (request.isHttpError || request.isNetworkError);
			status = isError ? LDStatus.Failed : LDStatus.Success;

			if (isError)
			{
				errorMessage = request.error;

#if VERBOSE_LOG
                Debug.LogWarning("[Editor] Load error: " + request.url + "\n" + errorMessage);
#endif

				// Debug.Log("STATUS : " + status);
				request = null;
				return false;
			}

			try
			{
				CastData(request);
			}
			catch (Exception e)
			{
				Debug.LogWarning(e);
			}

			// Download success
			return true;
		}

		void CastData(UnityWebRequest request)
		{
			//Debug.Log("CAST DaTA: " + this.url + "\n" + request.downloadHandler);

			if (dataType == typeof(Texture2D))
			{
				loadedData = DownloadHandlerTexture.GetContent(request);
				return;
			}

			if (dataType == typeof(AudioClip))
			{
				loadedData = DownloadHandlerAudioClip.GetContent(request);
				return;
			}

			if (dataType == typeof(string))
			{
				loadedData = request.downloadHandler.text;
				return;
			}

			if (dataType == typeof(byte[]))
			{
				loadedData = request.downloadHandler.data;
				return;
			}

			Debug.LogWarning("Unsupported dataType: " + dataType);
		}

		// ------------------
		//      LOCAL
		// ------------------

		public bool local_needCheck
		{
			get
			{
				return (lcStatus == LDStatus.Idle) && !string.IsNullOrEmpty(localId);
			}
		}

		internal void CheckLocal()
		{
			if (!local_needCheck)
			{
				lcStatus = LDStatus.Failed;

#if VERBOSE_LOG
				Debug.LogWarning("Skip local check! " + lcStatus);
#endif

				return;
			}

			lcStatus = LDStatus.Failed;
			if (localId == LOCAL_AUTOID) localId = GetAutoId(url);

			var v = fvManager.GetVersion(localId);
			if (v == -1)
			{
#if VERBOSE_LOG
				Debug.Log($"No cache found for <{localId}>!\n{localPath}\n{url}");
#endif

				return; // no cached version found
			}

			try
			{
				// First step: Check in local map
				var info = new FileInfo(localPath);
				if (!info.Exists)
				{
					Debug.Log($"FileIO error v[{v}] with localId <{localId}> " + localPath);
					return;
				}

				if (info.Length == 0)
				{
					Debug.Log($"Invalid cached v[{v}] with localId <{localId}> (length = 0)");
					return; // invalid cache
				}

				// a valid cache found!
				if (v >= version)
				{
					lcStatus = LDStatus.LCExist;
					Debug.Log($"A cached v[{v}] with localId <{localId}> found!\n{localPath}\n{url}");
				}

				Debug.Log($"A cached localId <{localId}> with older version v[{v}] found!");
			}

			catch (Exception e)
			{
#if VERBOSE_LOG
                Debug.LogWarning(localPath + "\n" + e);
#endif
			}
		}

		internal bool local_willLoadUsingFileIO
		{
			get { return (dataType == typeof(byte[]) || dataType == typeof(string)); }
		}

		internal bool WillLoadLocal
		{
			get { return lcStatus == LDStatus.LCExist; }
		}

		internal UnityWebRequest lcRequest;
		internal void LoadLocal()
		{
			if (lcStatus != LDStatus.LCExist)
			{
#if VERBOSE_LOG
                Debug.LogWarning("Invalid status: lcStatus = " + lcStatus);
#endif

				return; // not yet check local?
			}

			// Debug.Log("LoadLocal: " + url + " \n " + localPath + "\n" + dataType);

			lcStatus = LDStatus.Start;

			if (dataType == typeof(byte[])) { LoadLocal_Bytes(); return; }
			if (dataType == typeof(string)) { LoadLocal_String(); return; }
			if (dataType == typeof(Texture2D)) { Request_Texture("file://" + localPath, ref lcStatus, ref lcRequest); return; }
			if (dataType == typeof(AudioClip)) { Request_AudioClip("file://" + localPath, ref lcStatus, ref lcRequest); return; }

#if VERBOSE_LOG
            Debug.LogWarning("Unsupported dataType: " + dataType);
#endif
		}

		void LoadLocal_Bytes()
		{
			try
			{
				loadedData = File.ReadAllBytes(localPath);
				lcStatus = LDStatus.Success;
			}
			catch (Exception e)
			{
				lcStatus = LDStatus.Failed;

#if VERBOSE_LOG
                Debug.LogWarning(localPath + "\n" + e);
#endif
			}
		}

		void LoadLocal_String()
		{
			try
			{
				loadedData = File.ReadAllText(localPath);
				lcStatus = LDStatus.Success;
			}
			catch (Exception e)
			{
				lcStatus = LDStatus.Failed;

#if VERBOSE_LOG
                Debug.LogWarning(localPath + "\n" + e);
#endif
			}
		}

		// ------------------
		//      WEB
		// ------------------

		UnityWebRequest webRequest;
		public void LoadWeb() // load web, save to local
		{
			if ((wbStatus != LDStatus.Idle) || (lcStatus != LDStatus.Failed))
			{
#if VERBOSE_LOG
                Debug.LogWarning("Invalid status: wbStatus = " + wbStatus + " : lcStatus = " + lcStatus);
#endif

				return; // not yet check local?
			}

			wbStatus = LDStatus.Start;
			if (dataType == typeof(Texture2D)) { Request_Texture(url, ref wbStatus, ref webRequest); return; }
			if (dataType == typeof(AudioClip)) { Request_AudioClip(url, ref wbStatus, ref webRequest); return; }

			webRequest = UnityWebRequest.Get(url);
			webRequest.timeout = timeout;
			webRequest.SendWebRequest();

			// DO NOT USE FILE HANDLER (cannot access to downloaded bytes[] or string)
			// var handler = new DownloadHandlerFile(localPath);
			// handler.removeFileOnAbort = true;
			// webRequest.downloadHandler = handler;
		}
	}

}


//
//	LOADER VERSION
//
public partial class Loader : MonoBehaviour
{
	[Serializable]
	public class FileVersion
	{
		public string id;
		public int version;

		// optional
		// public string url;
		// public string path;
	}

	[Serializable]
	public class FileVersionManager
	{
		public List<FileVersion> files;
		public Dictionary<string, FileVersion> versionMap;
		public void Load()
		{
			var path = GetLocalPath("sys", "version.json", 0, true);

			if (File.Exists(path))
			{
				var json = File.ReadAllText(path);
				try
				{
					var fm = JsonUtility.FromJson<FileVersionManager>(json);
					files = fm.files;
					BuildCache();
				}
				catch (Exception e)
				{
#if VERBOSE_LOG
					Debug.LogWarning("Invalid version.json!\n" + e);
#endif
				}

			}

			// Initialize version
			if (files == null) files = new List<FileVersion>();
			if (versionMap == null) versionMap = new Dictionary<string, FileVersion>();
		}

		public int GetVersion(string id)
		{
			if (versionMap == null)
			{
#if VERBOSE_LOG
				Debug.LogWarning("versionMap not inited!");
#endif

				return -1;
			}

			FileVersion result;
			if (false == versionMap.TryGetValue(id, out result)) return -1;
			return result.version;
		}

		void BuildCache()
		{
			versionMap = new Dictionary<string, FileVersion>();
			var fileCount = files.Count;
			for (int i = 0; i < fileCount; i++)
			{
				var item = files[i];
				versionMap.Add(item.id, item);
			}
		}

		public bool Delete(string id)
		{
			if (versionMap == null)
			{
#if VERBOSE_LOG
				Debug.LogWarning("versionMap not inited!");
#endif

				return false;
			}

			FileVersion ofv;
			if (versionMap.TryGetValue(id, out ofv))
			{
				versionMap.Remove(id);
				var fileCount = files.Count;
				for (int i = 0; i < fileCount; i++)
				{
					if (files[i].id == id)
					{
						files.RemoveAt(i);
						break;
					}
				}

				CallNextFrame(Save);
				return true;
			}

			return false;
		}

		public bool Write(string id, int version)
		{
			if (versionMap == null)
			{
#if VERBOSE_LOG
				Debug.LogWarning("versionMap not inited!");
#endif

				return false;
			}

			FileVersion ofv;
			if (versionMap.TryGetValue(id, out ofv)) // existed
			{
				if (version <= ofv.version)
				{
#if VERBOSE_LOG
					Debug.LogWarning("Can not update to lower version: " + JsonUtility.ToJson(ofv) + " newVersion=" + version);
#endif
				}
				ofv.version = version;
			}
			else // new
			{
				ofv = new FileVersion() { id = id, version = version };
				files.Add(ofv);
				versionMap.Add(id, ofv);
			}

			CallNextFrame(Save);
			return true;
		}

		void Save()
		{
			var path = GetLocalPath("sys", "version.json", 0, true);
			File.WriteAllText(path, JsonUtility.ToJson(this));
		}
	}

	static FileVersionManager fvManager;
}

//
//	LOADER GROUP
//
public partial class Loader : MonoBehaviour
{
	[Serializable]
	public class LDGroup
	{
		public List<LDInfo> list = new List<LDInfo>();

		public float progress;
		public int nLoading;
		public int nSuccess;
		public int nFailed;
		public Action<LDGroup> onComplete;

		public T GetLoadedDataAt<T>(int index)
		{
			if (index < 0 || index >= list.Count) return default(T);
			var item = list[index];
			return (item == null) ? default(T) : item.GetLoadedData<T>();
		}

		public T[] GetLoadedData<T>()
		{
			var n = list.Count;
			var result = new T[n];

			for (int i = 0; i < n; i++)
			{
				var item = list[i];
				result[i] = (item == null) ? default(T) : item.GetLoadedData<T>();
			}

			return result;
		}

		public LDGroup Add<T>(LDInfo item)
		{
			if (item != null)
			{
				item.onProgress = OnItemUpdate;
				item.onComplete = OnItemUpdate;
				item.onError = OnItemUpdate;
			}
			else
			{   // can not add null item (at least in Editor)
				item = new LDInfo() { wbStatus = LDStatus.Failed, lcStatus = LDStatus.Failed };
			}

			list.Add(item);
			return this;
		}

		public LDGroup Add<T>(string url, int version = 0, string localId = null)
		{
			var item = Load<T>(url);
			item.version = version;
			if (!string.IsNullOrEmpty(localId)) item.SetLocalCacheId(localId);
			return Add<T>(item);
		}

		public LDGroup Add<T>(params string[] urls)
		{
			for (int i = 0; i < urls.Length; i++)
			{
				Add<T>(urls[i]);
			}

			return this;
		}


		bool _needRefresh = false;
		void OnItemUpdate(LDInfo info)
		{
			if (_needRefresh) return; // will update next frame!

			_needRefresh = true;
			Loader.CallNextFrame(CheckProgress); //Maximum: update once a frame
		}

		void CheckProgress()
		{
			_needRefresh = false;

			var count = list.Count;
			var sum = 0f;

			nLoading = 0;
			nSuccess = 0;
			nFailed = 0;

			for (int i = 0; i < count; i++)
			{
				var item = list[i];
				sum += item.progress;

				if (item.isSuccess)
				{
					nSuccess++;
					continue;
				}

				if (item.isFailed)
				{
					nFailed++;
					continue;
				}

				if (item.isLoading)
				{
					nLoading++;
					continue;
				}
			}

			progress = sum / count;
			// Debug.Log("Check: " + progress + " : " + nSuccess + " / " + nFailed + "/" + list.Count);

			if (nSuccess + nFailed == count)
			{
				var temp = onComplete;
				onComplete = null;
				if (temp != null) temp(this);
			}
		}

		public void Retry()
		{
			foreach (var item in list)
			{
				if (!item.isFailed) continue;
				item.Retry();
			}
		}
	}

	static readonly List<LDGroup> allGroups = new List<LDGroup>();
	public static LDGroup CreateGroup(Action<LDGroup> onComplete)
	{
		var result = new LDGroup() { onComplete = onComplete };
		allGroups.Add(result);
		return result;
	}
	public static LDGroup CreateGroup<T>(string[] urls, Action<LDGroup> onComplete)
	{
		var result = new LDGroup() { onComplete = onComplete }.Add<T>(urls);
		allGroups.Add(result);
		return result;
	}
}

//
//	SINGLETON
//

public partial class Loader : MonoBehaviour
{
	private static Loader _api;
	public bool dontDestroyOnLoad = false;

#if STANDALONE
    [RuntimeInitializeOnLoadMethod] static void Initialize()
    {
        if (_api != null) return;
        _api = new GameObject("~Loader").AddComponent<Loader>();
    }
#endif

	void Awake()
	{
		if (_api != null && _api != this)
		{
#if VERBOSE_LOG
            Debug.LogWarning("Multiple Loader found!!!");
#endif

			Destroy(this);
			return;
		}

		_api = this;
		// gameObject.hideFlags = HideFlags.HideAndDontSave;
		if (dontDestroyOnLoad == true) DontDestroyOnLoad(_api);
	}
}



//
//	STATIC APIS
//

public partial class Loader : MonoBehaviour
{
	static private List<LDInfo> queue;
	static Action delayCalls;
	static internal void CallNextFrame(Action a)
	{
		delayCalls -= a;
		delayCalls += a;
	}

	static internal string GetLocalPath(string path, string fileName, int version, bool createDir = false)
	{
#if UNITY_EDITOR
        var localDir = Path.Combine(Application.dataPath.Replace("Assets", string.Empty), "Library/Tesuji/" + path);
#else
		var localDir = Path.Combine(Application.persistentDataPath, path);
#endif

		var localPath = Path.Combine(localDir, fileName);
		if (createDir) Directory.CreateDirectory(localDir);
		return localPath;
	}
	static public string GetAutoId(string url)
	{
		if (string.IsNullOrEmpty(url)) return null;

		var id = url
				.Replace("http://", string.Empty)
				.Replace("https://", string.Empty)
				.Replace("?", "_")
				.Replace("=", "_")
				.Replace(" ", "_")
				.Replace("&", "_")
				.Replace("/", "_")
				.Replace("__", "_")
				.ToLower();

		if (id.Length > 128)
		{
#if VERBOSE_LOG
            Debug.LogWarning("[Editor] Too long id will be truncated, explicit delare an id then! \n" + id);
#endif

			id = id.Substring(id.Length - 128, 128);
		}
#if VERBOSE_LOG
		else 
		{
			Debug.Log("AutoID --> " + id + "\n" + url);
		}
#endif

		return id;
	}

	static public void Load(LDInfo info)
	{
		// must check null because queue will be null while processing
		if (queue == null) queue = new List<LDInfo>();
		queue.Add(info);
	}

	static public LDInfo Load<T>(string url, Action<LDInfo> onComplete = null, Action<LDInfo> onError = null, int version = 0)
	{
		// must check null because queue will be null while processing
		if (queue == null) queue = new List<LDInfo>();

		if (string.IsNullOrEmpty(url))
		{
#if VERBOSE_LOG
            Debug.LogWarning("URL should not be null!");
#endif

			return null;
		}

		if (!url.StartsWith("http://") && !url.StartsWith("https://"))
		{
#if VERBOSE_LOG
            Debug.LogWarning("Unsupported protocol, are you using the - not yet supported - relative links? -->\n" + url);
#endif

			return null;
		}

		url = url.Replace("\\", "/");
		var id = GetAutoId(url);

		var result = new LDInfo()
		{
			url = url,
			id = id,
			version = version,
			dataType = typeof(T),
			onComplete = onComplete,
			onError = onError
		};

		queue.Add(result);
		return result;
	}
}




public partial class Loader : MonoBehaviour
{
	[Range(1, 10)] public int maxWebRequest = 4;
	[Range(1, 20)] public int maxFileIOPerFrame = 10;

	public int nRequesting; // web

	void Start()
	{
		StartCoroutine(ProcessQueue());
	}
	IEnumerator ProcessQueue()
	{
		fvManager = new FileVersionManager();
		fvManager.Load();
		yield return null;

		while (true) // stupid first: Do not stop queue - keep checking
		{
			yield return null;

			if (delayCalls != null) // next frame helper
			{
				var cb = delayCalls;
				delayCalls = null;
				cb();
			}

			if (queue == null || queue.Count == 0)
			{
				// #if VERBOSE_LOG
				// Debug.Log("Nothing in queue!");
				// #endif

				continue;
			}

			var temp = queue;
			queue = null; //swap to temporary variable to make sure the queue is isolated

			// Update progress for loading items & remove downloaded ones
			CheckStatus(temp);

			// Process queue
			ProcessQueue(temp);

			// swap back when done processing
			if (queue != null && queue.Count > 0)
			{
				temp.AddRange(queue); // there are items added during processing time
			}
			queue = temp;
		}
	}

	void CheckStatus(List<LDInfo> list)
	{
		// Debug.Log("Status: " + list.Count);
		nRequesting = 0;

		for (int i = list.Count - 1; i >= 0; i--)
		{
			var item = list[i];

			// newly added item need check for local cache
			if (item.local_needCheck)
			{
				// Debug.Log("STATUS ZERO: " + item.wbStatus);
				item.CheckLocal();
				// Debug.Log("STATUS ONE: " + item.wbStatus);
				continue;
			}

			if (item.isLoading)
			{
				// Debug.Log("STATUS A: " + item.wbStatus);
				item.UpdateProgress();
				if (item.isLoading)
				{
					// Debug.Log("STATUS B: " + item.wbStatus);
					nRequesting++; // still loading? count it!
				}
			}

			if (item.isDone)
			{
				// Debug.Log("STATUS: " + item.wbStatus);
				list.RemoveAt(i);
				item.TriggerCallbacks();
			}

			// Debug.Log("ABC! " + item.lcStatus + " | " + item.wbStatus);
		}
	}
	void ProcessQueue(List<LDInfo> list)
	{
		var wrBudget = maxWebRequest - nRequesting;
		var ioBudget = maxFileIOPerFrame;

		// Debug.Log("Process queue: " + list.Count);

		// Always prefer local over web
		var lstCount = list.Count;
		for (int i = 0; i < lstCount; i++)
		{
			var item = list[i];
			if (item.lcStatus != LDStatus.LCExist) continue;

			// load local using FileIO
			if (item.local_willLoadUsingFileIO)
			{
				if (ioBudget <= 0) continue; // out of budget
				ioBudget--;
				item.LoadLocal();
				continue;
			}

			// Load local using web request
			if (wrBudget <= 0) continue;
			wrBudget--;
			nRequesting++;
			item.LoadLocal();
		}

		// no more budget for pending web requests
		if (wrBudget == 0)
		{
			// Debug.Log("No more web budget");
			return;
		}

		for (int i = 0; i < lstCount; i++)
		{
			var item = list[i];
			if ((item.lcStatus != LDStatus.Failed) || (item.wbStatus != LDStatus.Idle))
			{
				// Debug.Log("Skiped : " + item.lcStatus + " | " + item.wbStatus);
				continue;
			}

			wrBudget--;
			nRequesting++;
			item.LoadWeb();
			if (wrBudget <= 0) break;
		}
	}
}





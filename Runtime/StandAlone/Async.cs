// #define STANDALONE
// #define ASYNC_INTEGRITY_CHECK

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tesuji
{
	// STATIC
	public partial class Async
	{
		public static bool ENABLE_LOG = false;
		
		public static float realTime;
		public static float gameTime;
		
		private static bool _isDestroyed;
		private static bool _isPlaying;

		private static Async _api;
		internal static Async Api
		{
			get
			{
				if (_isDestroyed) return null;
				if (_isPlaying) return _api;
				return Application.isPlaying ? CreateInstance() : null;
			}
		}

	#if STANDALONE
		[RuntimeInitializeOnLoadMethod] static void Initialize() { CreateInstance(); }
	#endif

		public static Async CreateInstance()
		{
			if (_isDestroyed) return null;
			if (!Application.isPlaying) return null;
			return _api ? _api : (_api = new GameObject("~Async").AddComponent<Async>());
		}

		// APIs
		public static Action onFrameUpdate;
		
		public static void Call(Action action, float delaySecs = 0, string id = null, bool overwriteExisted = true)
		{
			Add(id, overwriteExisted, () => CBInfo.Create<CBInfo>(action, gameTime + delaySecs));
		}
		
		public static void Wait(Func<bool> wait, Action action, float delaySecs = 0f, string id = null, bool overwriteExisted = true)
		{
			Add(id, overwriteExisted, () =>
			{
				var info =  CBInfo.Create<CBWait>(action, gameTime + delaySecs);
				info.waitFunc = wait;
				return info;
			});
		}
		
		private static void Add<T>(string id, bool overwriteExisted, Func<T> newFunc) where T: CBInfo, new()
		{
			var hasId = string.IsNullOrEmpty(id) == false;
			if (hasId)
			{
				// overwrite = true --> doRemove = true
				CBInfo oInfo = RemoveId(id, overwriteExisted); 
				
				// dry run & found existed item: skip this action
				if (!overwriteExisted && oInfo != null) return;
			}

			CBInfo info = newFunc();
			if (hasId) SetId(id, info);
			lock (_writeQueue) { _writeQueue.Add(info); }
		}
	}
	
	// ID - SUPPORTED
	public partial class Async
	{
		private static readonly Dictionary<string, CBInfo> _idMap = new Dictionary<string, CBInfo>();

		private static CBInfo GetId(string id)
		{
			CBInfo cbInfo;
			lock (_idMap) { _idMap.TryGetValue(id, out cbInfo); }
			return cbInfo;
		}
		
		private static CBInfo RemoveId(string id, bool doRemove = true)
		{
			CBInfo cbInfo;
			lock (_idMap)
			{
				if (!_idMap.TryGetValue(id, out cbInfo)) return null;
				if (doRemove == false) return cbInfo;
				_idMap.Remove(id);
			}
			
			cbInfo.isDie = true;
			cbInfo.id = null;
			return cbInfo;
		}
		
		private static void SetId(string id, CBInfo cbInfo)
		{
			#if ASYNC_INTEGRITY_CHECK
			if (!string.IsNullOrEmpty(cbInfo.id))
			{
				Debug.LogWarning("Integrity check failed: cbInfo.id should be null!");	
			}
			
			if (cbInfo.isDie)
			{
				Debug.LogWarning("Integrity check failed: cbInfo is died");	
			}
			#endif
			
			cbInfo.id = id;
			
			lock (_idMap)
			{
				if (_idMap.ContainsKey(id))
				{
					_idMap[id] = cbInfo;
				}
				else
				{
					_idMap.Add(id, cbInfo);
				}
			}
		}
		
		public static void Kill(string id)
		{
			RemoveId(id);
		}
	}
	
	// MONO - BACKED
	public partial class Async : MonoBehaviour
	{
		void Awake()
		{
			if (_api != null && _api != this)
			{
				Debug.LogWarning("Multiple Async found!!!");
				Destroy(this);
				return;
			}
			
			_api = this;
			_isDestroyed = false;
			_isPlaying = true;
			DontDestroyOnLoad(_api);
		}

		void OnDestroy()
		{
			_isDestroyed = true;
		}

		void Update()
		{
			if (_isDestroyed) return;
			
			realTime = Time.realtimeSinceStartup;
			gameTime = Time.time;
			
			onFrameUpdate?.Invoke();
			ProcessQueue();
		}
		
		[ContextMenu("Toggle Log")] void ToggleLog()
		{
			ENABLE_LOG = !ENABLE_LOG;
		}
	}
	
	// QUEUE ITEM INFO
	public partial class Async
	{
		[Serializable] public class CBInfo
		{
			private static int counter;
			
			public static T Create<T>(Action callback, float time) where T: CBInfo, new ()
			{
				counter++;
				
				return new T()
				{
					asyncID = counter,
					callback = callback,
					time = time
				};
			}
			
			public int asyncID;
			public int priority;
			
			public string id;
			public float time;
			public bool isDie;
			
			public Action callback; // Should we use WeakReference?
			
			public virtual void CheckCallback()
			{
				if (callback?.Target == null)
				{
					isDie = true;
					return;
				}
				
				callback();
				isDie = true;
			}

			public string GetDebugInfo()
			{
				return $"[CBInfo asyncID = {asyncID}, time={time}, callback=[{GetActionInfo(callback)}]]";
			}
		}

		public class CBWait : CBInfo
		{
			public Func<bool> waitFunc;
			
			public override void CheckCallback()
			{
				if (waitFunc?.Target == null || callback?.Target == null)
				{
					isDie = true;
					return;	
				}
				
				if (!waitFunc()) return;
				
				callback();
				isDie = true;
			}
		}
	}
	
	// QUEUE
	public partial class Async
	{	
		public List<CBInfo> _queue = new List<CBInfo>(); // per-frame Updating Queue
		public List<CBInfo> _pendingQueue = new List<CBInfo>(); // delayed items --> be added to _queue when it's time
		private static readonly List<CBInfo> _writeQueue = new List<CBInfo>(); // Use as buffer --> append to _pendingQueue

		void ProcessQueue()
		{	
			// Step 1: Append to pending items & clear _writeQueue
			Step1_AppendWriteQueue(out var _dirtyQueue);

			// Step 2: Process _delayQueue & pick items to _queue
			if (_pendingQueue.Count > 0) Step2_ProcessPendingQueue(_dirtyQueue);
			
			// Step 3: Do Callback
			if (_queue.Count > 0) Step3_ProcessCallback();
		}

		void Step1_AppendWriteQueue(out bool _dirtyQueue)
		{
			_dirtyQueue = false;
			
			lock (_writeQueue)
			{
				if (_writeQueue.Count == 0) return;

				var gTime = gameTime;
				for (var i = 0; i < _writeQueue.Count; i++)
				{
					CBInfo item = _writeQueue[i];
					if (item == null || item.isDie) continue;

					if (item.time <= gTime)
					{
						_dirtyQueue = true;
						_queue.Add(item);
					}
					else
					{
						_pendingQueue.Add(item);
					}
				}
				
				_writeQueue.Clear();
			}
		}

		void Step2_ProcessPendingQueue(bool willSort)
		{
			var nullCount = 0;
			for (var i = 0; i < _pendingQueue.Count; i++)
			{
				CBInfo item = _pendingQueue[i];
				if (item == null || item.isDie) // exlude die items (might be killed)
				{
					nullCount++;
					_pendingQueue[i] = null;
					continue;
				}

				// Time yet to come
				if (item.time > gameTime) continue;

				_queue.Add(item);
				_pendingQueue[i] = null;
				nullCount++;
				willSort = true;
			}

			// Step 3: Pack & Sort queue
			if (nullCount > nullCount * 0.3) Pack(_pendingQueue); // Pack when null takes > 30 %
			if (willSort) _queue.Sort((item1, item2) =>
			{
				if (item1 == null && item2 == null) return 0;
				if (item1 == null) return 1;
				if (item2 == null) return -1;
				
				var priorComp = item2.priority.CompareTo(item1.priority);
				if (priorComp != 0) return priorComp;
				
				var timeComp = item1.time.CompareTo(item2.time);
				if (timeComp != 0) return timeComp;
				
				return item1.asyncID.CompareTo(item2.asyncID);
			});
		}

		void Step3_ProcessCallback()
		{
			if (_queue.Count == 0) return;

			var nullCount = 0;
			
			StringBuilder sb = null;

			if (ENABLE_LOG)
			{
				sb = new StringBuilder();
				sb.AppendLine($"{gameTime} --> Process Callback [{_queue.Count}]");
			}
			
			for (var i = 0; i < _queue.Count; i++)
			{
				CBInfo item = _queue[i];

				if (item == null)
				{
					nullCount++;
					continue;
				}
				
				if (item.isDie == false)
				{
					if (ENABLE_LOG) sb.AppendLine(item.GetDebugInfo());
					
					item.CheckCallback();
					if (item.isDie == false)
					{
						continue;
					}
				}
				
				if (ENABLE_LOG) Debug.LogWarning(sb.ToString());
				
				RemoveDeadItemInQueue(i);
				nullCount++;
			}
			
			if (nullCount > _queue.Count * 0.3f) Pack(_queue); // Only pack if null takes ~30% array
		}

		void RemoveDeadItemInQueue(int index)
		{
			// presumably item.isDie == true at this point
			CBInfo item = _queue[index];
			
			#if ASYNC_INTEGRITY_CHECK
			if (item.isDie == false)
			{
				Debug.LogWarning("Item.isDie must == true!");	
			}
			#endif
			
			_queue[index] = null;
			
			if (string.IsNullOrEmpty(item.id)) return;
			RemoveId(item.id);
		}
	}
	
	// UTILS
	public partial class Async
	{
		private static void Pack<T>(List<T> list)
		{
			var emptyIdx = -1;
			var n = list.Count;

			for (var i = 0; i < n; i++)
			{
				T item = list[i];

				if (item == null)
				{
					if (emptyIdx == -1) emptyIdx = i;
					continue;
				}

				if (emptyIdx == -1) continue;

				list[emptyIdx] = list[i];
				list[i] = default(T); // just for sure
				emptyIdx++;
			}

			// remove unneeded items
			list.RemoveRange(emptyIdx, n - emptyIdx);
			// Debug.LogWarning($"Pack: {n-emptyIdx}");
		}

		private static string GetActionInfo(Action cb)
		{
			var sb = new StringBuilder();
			foreach (Delegate item in cb.GetInvocationList())
			{
				var target = item.Target;
				
				if (target is UnityEngine.Object uObject)
				{
					sb.Append($"{uObject.name} --> {uObject.GetType()}.{item.Method.Name}()  | ");
				}
				else
				{
					sb.Append($"target={target}, method={item.Method}  | ");	
				}
			}
			
			return sb.ToString();
		}
	}
}

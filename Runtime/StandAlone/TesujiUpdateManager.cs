// #define DEBUG_CALLBACK

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tesuji
{
    public class TesujiUpdateManager : MonoBehaviour
    {
        private static int COUNTER = 1;
        private static TesujiUpdateManager _api;
        private static readonly UpdateQueue _updateQueue = new UpdateQueue();
        private static readonly UpdateQueue _lateUpdateQueue = new UpdateQueue();

        public static Coroutine StartRoutine(IEnumerator routine)
        {
            return _api.StartCoroutine(routine);
        }

        public static void StopRoutine(Coroutine routine)
        {
            _api.StopCoroutine(routine);
        }
        
        public static void StopRoutine(string routineName)
        {
            _api.StopCoroutine(routineName);
        }
        

        [Serializable]
        public class UpdateInfo
        {
#if DEBUG_CALLBACK
            public string description;
#endif
            
            public int id;
            public int delayInFrame;
            public bool once;
            public int priority;
            public Action callback;

            public UpdateInfo(Action callback, int priority, bool once, int delayInFrame)
            {
                id = COUNTER++;

                this.callback = callback;
                this.priority = priority;
                this.once = once;
                this.delayInFrame = delayInFrame;

#if DEBUG_CALLBACK
                this.description = callback.Target.ToString() + "." + callback.Method.Name + "()";
#endif
            }
        }

        [Serializable]
        class UpdateQueue
        {
            private bool dirty;
            internal List<UpdateInfo> queue = new List<UpdateInfo>();
            private Dictionary<Action, UpdateInfo> map = new Dictionary<Action, UpdateInfo>();

            public int Add(Action callback, int priority = 0, bool once = false, int delayInFrame = 0)
            {
                if (callback == null)
                {
                    Debug.LogWarning("callback should not be null!");
                    return -1;
                }

                if (map.TryGetValue(callback, out UpdateInfo info))
                {
                    // Debug.LogWarning("Trying to add the same callback!");
                    return info.id;
                }
                
                var item = new UpdateInfo(callback, priority, once, delayInFrame);
                map.Add(callback, item);
                queue.Add(item);

                dirty = true;
                return item.id;
            }
            
            public bool Remove(int updateId)
            {
                for (var i = 0; i < queue.Count; i++)
                {
                    UpdateInfo item = queue[i];
                    if (item.id != updateId) continue;
                    if (item.callback == null) return false; // removed before?
                    
                    map.Remove(item.callback);
                    item.callback = null; 
                    return true;
                }

                return false;
            }

            public bool Remove(Action callback)
            {
                if (callback == null)
                {
                    Debug.LogWarning("callback should not be null!");
                    return false;
                }

                if (!map.TryGetValue(callback, out UpdateInfo info)) return false;
                if (info.callback == null) return false; // removed before?
                    
                map.Remove(info.callback);
                info.callback = null; // do not remove from queue 
                return true;
            }

            int QueueSorter(UpdateInfo item1, UpdateInfo item2)
            {
                var n1 = item1 == null;
                var n2 = item2 == null;

                if (n1) return n2 ? 0 : 1;
                if (n2) return -1;

                var result = item1.priority.CompareTo(item2.priority);
                return (result == 0) ? item1.id.CompareTo(item2.id) : result;
            }

            bool ExecuteCallback(UpdateInfo info) // Return true if the callback is alive (will call next time)
            {
                if (info?.callback == null) return false;

                try
                {
                    info.callback();
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                    return false;
                }

                return !info.once;
            }

            public void Dispatch()
            {
                if (dirty)
                {
                    dirty = false;
                    queue.Sort(QueueSorter);
                }

                var dieCount = 0;
                for (var i = 0; i < queue.Count; i++)
                {
                    UpdateInfo item = queue[i];
                    if (item.delayInFrame > 0)
                    {
                        item.delayInFrame--;
                        continue;
                    }
                    
                    var alive = ExecuteCallback(item);
                    if (alive) continue;

                    dieCount++;
                    queue[i] = null;
                }

                if (dieCount == 0) return;
                
                // remove nulls
                for (var i = queue.Count - 1; i >= 0; i--)
                {
                    if (queue[i] != null) continue;
                    queue.RemoveAt(i);
                }
            }
        }
        
        public static int DelayCall(Action callback, int delayInFrame = 0)
        {
            if (_api == null) Debug.LogWarning("Update Manager instance not found!");
            return _updateQueue.Add(callback, 0, true, delayInFrame);
        }

        public static int OnUpdate(Action callback, int priority = 0, bool once = false, int delayInFrame = 0)
        {
            if (_api == null) Debug.LogWarning("Update Manager instance not found!");
            return _updateQueue.Add(callback, priority, once, delayInFrame);
        }

        public static int OnLateUpdate(Action callback, int priority = 0, bool once = false, int delayInFrame = 0)
        {
            if (_api == null) Debug.LogWarning("Update Manager instance not found!");
            return _lateUpdateQueue.Add(callback, priority, once, delayInFrame);
        }
        
        public static void RemoveLateUpdate(Action callback)
        {
            _lateUpdateQueue.Remove(callback);
        }
        
        private void Awake()
        {
            if (_api != null && _api != this)
            {
                Debug.LogWarning("Multiple UpdateManager found!");
                Destroy(this);
                return;
            }

            _api = this;
            DontDestroyOnLoad(this);

#if DEBUG_CALLBACK
            updateQueue = _updateQueue.queue;
            lateUpdateQueue = _lateUpdateQueue.queue;
#endif
        }


        // VIEW-ONLY
#if DEBUG_CALLBACK
        public List<UpdateInfo> updateQueue;
        public List<UpdateInfo> lateUpdateQueue;
#endif

        private void Update()
        {
            _updateQueue.Dispatch();
        }

        private void LateUpdate()
        {
            _lateUpdateQueue.Dispatch();
        }
    }
}
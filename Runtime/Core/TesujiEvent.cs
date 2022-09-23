// #define DEBUG

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tesuji
{
    public interface ITesujiEventSource { }

    // PUBLIC APIs
    public static partial class TesujiEvent
    {
        public static Dispatcher Get(object dsp, bool autoNew = true)
        {
            if (_dispatcherMap.TryGetValue(dsp, out Dispatcher result)) return result;
            if (!autoNew) return null;

            result = new Dispatcher();
            _dispatcherMap.Add(dsp, result);
            return result;
        }
        
        public static void AddListener(string eventName, Action handler) { _global.AddListener(eventName, handler); }
        public static void AddListeners(params (string eventName, Action handler)[] pairs) { _global.AddListeners(pairs); }
        public static void AddListener<T>(string eventName, Action<T> handler) { _global.AddListener(eventName, handler); }
        public static void AddListener<T1, T2>(string eventName, Action<T1, T2> handler) { _global.AddListener(eventName, handler); }
        public static void AddListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> handler) { _global.AddListener(eventName, handler); }

        public static void RemoveListener(string eventName, Action handler) { _global.RemoveListener(eventName, handler); }
        public static void RemoveListeners(params (string eventName, Action handler)[] pairs) { _global.RemoveListeners(pairs); }
        
        public static void RemoveListener<T>(string eventName, Action<T> handler) { _global.RemoveListener(eventName, handler); }
        public static void RemoveListener<T1, T2>(string eventName, Action<T1, T2> handler) { _global.RemoveListener(eventName, handler); }
        public static void RemoveListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> handler) { _global.RemoveListener(eventName, handler); }
		
        public static void Dispatch(string eventName) { _global.Dispatch(eventName); }
        public static void Dispatch<T>(string eventName, T p1) { _global.Dispatch(eventName, p1); }
        public static void Dispatch<T1, T2>(string eventName, T1 p1, T2 p2) { _global.Dispatch(eventName, p1, p2); }
        public static void Dispatch<T1, T2, T3>(string eventName, T1 p1, T2 p2, T3 p3) { _global.Dispatch(eventName, p1, p2, p3); }
        
        public partial class Dispatcher
        {
            public void AddListener(string eventName, Action handler) { Add(eventName, 0, handler);}
            public void AddListeners(params (string eventName, Action handler)[] pairs)
            {
                foreach ((string eventName, Action handler) item in pairs)
                {
                    (var eventName, Action handler) = item;
                    Add(eventName, 0, handler);
                }
            }
            public void AddListener<T>(string eventName, Action<T> handler) { Add(eventName, 1, handler);}
            public void AddListener<T1, T2>(string eventName, Action<T1, T2> handler) { Add(eventName, 2, handler);}
            public void AddListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> handler) { Add(eventName, 3, handler);}

            public void RemoveListener(string eventName, Action handler) { Remove(eventName, 0, handler);}
            public void RemoveListeners(params (string eventName, Action handler)[] pairs)
            {
                foreach ((string eventName, Action handler) item in pairs)
                {
                    (var eventName, Action handler) = item;
                    Remove(eventName, 0, handler);
                }
            }
            
            public void RemoveListener<T>(string eventName, Action<T> handler) { Remove(eventName, 1, handler);}
            public void RemoveListener<T1, T2>(string eventName, Action<T1, T2> handler) { Remove(eventName, 2, handler);}
            public void RemoveListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> handler) { Remove(eventName, 3, handler);}

            public void Dispatch(string eventName) { Dispatch(eventName, 0, (d) => d.DynamicInvoke());}
            public void Dispatch<T>(string eventName, T p1) { Dispatch(eventName, 1, (d) => d.DynamicInvoke(p1));}
            public void Dispatch<T1, T2>(string eventName, T1 p1, T2 p2) { Dispatch(eventName, 2, (d) => d.DynamicInvoke(p1, p2));}
            public void Dispatch<T1, T2, T3>(string eventName, T1 p1, T2 p2, T3 p3) { Dispatch(eventName, 3, (d) => d.DynamicInvoke(p1, p2, p3));}
        }
    }
    
    // INTERNAL APIs
    public static partial class TesujiEvent
    {
        private static readonly Dispatcher _global = new Dispatcher();
        private static readonly Dictionary<object, Dispatcher> _dispatcherMap = new Dictionary<object, Dispatcher>();
        
        [Serializable]
        public partial class Dispatcher
        {
            private const int MAX_PARAMS = 3;
            private readonly Dictionary<string, Delegate[]> _map = new Dictionary<string, Delegate[]>();

            // INTERNAL APIs
            internal void DelayRebuildListEventDesc()
            {
#if DEBUG
                TesujiUpdateManager.OnUpdate(RebuildListEvents, 0, true);
#endif
            }

            internal Delegate[] Get(string eventName, bool autoNew)
            {
                if (_map.TryGetValue(eventName, out Delegate[] arr)) return arr;

                if (!autoNew) return null;
                arr = new Delegate[MAX_PARAMS + 1];
                _map.Add(eventName, arr);
                DelayRebuildListEventDesc();
                return arr;
            }

            internal void Add(string eventName, int nParams, Delegate d)
            {
                Delegate[] arrDelegate = Get(eventName, true);
                Delegate c = arrDelegate[nParams];

                // Remove first to prevent duplication
                c = Delegate.Remove(c, d);
                arrDelegate[nParams] = Delegate.Combine(c, d);
                DelayRebuildListEventDesc();
            }

            internal void Remove(string eventName, int nParams, Delegate d)
            {
                Delegate[] arrDelegate = Get(eventName, false);
                if (arrDelegate == null) return;
                Delegate c = arrDelegate[nParams];
                arrDelegate[nParams] = Delegate.Remove(c, d);
                DelayRebuildListEventDesc();
            }
            
            internal void Dispatch(string eventName, int nParams, Action<Delegate> cb)
            {
                Delegate d = Get(eventName, false)?[nParams];
                if (d == null)
                {
                    // Debug.LogWarning($"Event {eventName} - No listener with {nParams} parameters found!");
                    return;
                }
                
                EditorTryDispatch(() => cb(d));
            }


            // PUBLIC APIs
            public void Clear(string eventName)
            {
                Delegate[] arrDelegate = Get(eventName, false);
                if (arrDelegate == null) return;
                for (var i = 0; i < arrDelegate.Length; i++)
                {
                    arrDelegate[i] = null;
                }

                DelayRebuildListEventDesc();
            }

            public void Reset()
            {
                _dispatching = false;
                _map.Clear();
                DelayRebuildListEventDesc();
            }

            private bool _dispatching;


            internal void EditorTryDispatch(Action cb)
            {
                if (_dispatching)
                {
                    Debug.LogWarning($"[{nameof(TesujiEvent)}] Nested event dispatching is not supported just yet!");
                    return;
                }

                _dispatching = true;

#if UNITY_EDITOR
                {
                    cb();
                }
#else
                {
                    try
                    {
                        cb();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                        return;
                    }
                    finally {
                         _dispatching = false;
                    }
                }
#endif
                _dispatching = false;
            }
        }
        
    }
    
    // EXTENSIONS
    public static class TesujiEventExtension
    {
        public static void AddListener(this ITesujiEventSource source, string eventName, Action handler) { TesujiEvent.Get(source)?.AddListener(eventName, handler); }
        public static void AddListeners(this ITesujiEventSource source, params (string eventName, Action handler)[] pairs) { TesujiEvent.Get(source)?.AddListeners(pairs); }
        
        public static void AddListener<T>(this ITesujiEventSource source, string eventName, Action<T> handler) { TesujiEvent.Get(source)?.AddListener(eventName, handler); }
        public static void AddListener<T1, T2>(this ITesujiEventSource source, string eventName, Action<T1, T2> handler) { TesujiEvent.Get(source)?.AddListener(eventName, handler); }
        public static void AddListener<T1, T2, T3>(this ITesujiEventSource source, string eventName, Action<T1, T2, T3> handler) { TesujiEvent.Get(source)?.AddListener(eventName, handler); }
        
        public static void RemoveListener(this ITesujiEventSource source, string eventName, Action handler) { TesujiEvent.Get(source)?.RemoveListener(eventName, handler); }
        public static void RemoveListeners(this ITesujiEventSource source, params (string eventName, Action handler)[] pairs) { TesujiEvent.Get(source)?.RemoveListeners(pairs); }
        
        public static void RemoveListener<T>(this ITesujiEventSource source, string eventName, Action<T> handler) { TesujiEvent.Get(source)?.RemoveListener(eventName, handler); }
        public static void RemoveListener<T1, T2>(this ITesujiEventSource source, string eventName, Action<T1, T2> handler) { TesujiEvent.Get(source)?.RemoveListener(eventName, handler); }
        public static void RemoveListener<T1, T2, T3>(this ITesujiEventSource source, string eventName, Action<T1, T2, T3> handler) { TesujiEvent.Get(source)?.RemoveListener(eventName, handler); }
        
        
        public static void Dispatch(this ITesujiEventSource source, string eventName) { TesujiEvent.Get(source)?.Dispatch(eventName); }
        public static void Dispatch<T>(this ITesujiEventSource source, string eventName, T p1) { TesujiEvent.Get(source)?.Dispatch(eventName, p1); }
        public static void Dispatch<T1, T2>(this ITesujiEventSource source, string eventName, T1 p1, T2 p2) { TesujiEvent.Get(source)?.Dispatch(eventName, p1, p2); }
        public static void Dispatch<T1, T2, T3>(this ITesujiEventSource source, string eventName, T1 p1, T2 p2, T3 p3) { TesujiEvent.Get(source)?.Dispatch(eventName, p1, p2, p3); }
    }

    
#if DEBUG
    // DEBUG ONLY
    public static partial class TesujiEvent
    {
        [Serializable] public class DispatcherEventDesc
        {
            public string eventName;
            public List<string> delegateNames = new List<string>();

            public DispatcherEventDesc(string eventName, Delegate[] delegates)
            {
                this.eventName = eventName;
                for (var i = 0; i < delegates.Length; i++)
                {
                    Delegate d = delegates[i];
                    if (d == null) continue;

                    foreach (Delegate item in d.GetInvocationList())
                    {
                        delegateNames.Add($"[{i}] : {item.Target}.{item.Method.Name}()");
                    }
                }
            }
        }

        public partial class Dispatcher
        {
            public List<DispatcherEventDesc> listEvents = new List<DispatcherEventDesc>();
            void RebuildListEvents()
            {
                listEvents.Clear();

                foreach (KeyValuePair<string, Delegate[]> item in _map)
                {
                    listEvents.Add(new DispatcherEventDesc(item.Key, item.Value));
                }
            }
        }
    }
#endif
}
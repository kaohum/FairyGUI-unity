using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FairyGUI
{
    public class DisplayObjectPool
    {
        public static void InitPool()
        {
            // 经验数值
            DisplayObjectPool<Container>.TryCreateInstance(4096);
            DisplayObjectPool<Image>.TryCreateInstance(4096);
            DisplayObjectPool<TextField>.TryCreateInstance(2048);
            DisplayObjectPool<GoWrapper>.TryCreateInstance(32);
            DisplayObjectPool<Shape>.TryCreateInstance(256);
            DisplayObjectPool<SelectionShape>.TryCreateInstance(32);
        }

        public static async UniTask CreatePoolObjects()
        {
            DisplayObjectPool<Container>.Instance.InitPool(false);
            await UniTask.DelayFrame(1);
            DisplayObjectPool<Image>.Instance.InitPool(true);
            await UniTask.DelayFrame(1);
            DisplayObjectPool<TextField>.Instance.InitPool(true);
            await UniTask.DelayFrame(1);
            DisplayObjectPool<GoWrapper>.Instance.InitPool(false);
            DisplayObjectPool<Shape>.Instance.InitPool(true);
            DisplayObjectPool<SelectionShape>.Instance.InitPool(true);
            await UniTask.DelayFrame(1);
        }
    }

    public class DisplayObjectPool<T>
#if DEBUG_DISPLAY_OBJECT_POOL
        : IDisplayObjectPoolDebug
#endif
    {
        private static DisplayObjectPool<T> m_instance;
        private Transform m_cacheRoot;
        private Stack<GameObject> m_objectPool = new Stack<GameObject>();
        private int m_capacity = 1024;
        private int m_allocCount = 0;
#if DEBUG_DISPLAY_OBJECT_POOL
        private int m_getPoolCount = 0;
        private int m_destroyCount = 0;
        private int m_recycleCount = 0;
#endif

        public static void TryCreateInstance(int capacity)
        {
            if (m_instance == null)
            {
                m_instance = CreateInstance(capacity);
            }
        }

        private static DisplayObjectPool<T> CreateInstance(int capacity)
        {
            var instance = new DisplayObjectPool<T>();
            instance.m_capacity = capacity;
            if (Application.isPlaying)
            {
                GameObject root = new GameObject($"DisplayObjectPool_{typeof(T)}");
                root.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(root);
                instance.m_cacheRoot = root.transform;
            }
            else
            {
                instance.m_cacheRoot = null;
            }
            return instance;
        }

        public void InitPool(bool isGraphics)
        {
            if (m_allocCount >= m_capacity)
            {
                return;
            }

            string gameObjectName = $"{typeof(T)}_instance";
            int initCount = m_capacity / 10;
            for (int i = m_allocCount; i < initCount; ++i)
            {
                var gameObject = new GameObject(gameObjectName);
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                DisplayObjectInfo info = gameObject.AddComponent<DisplayObjectInfo>();
                info.displayObject = null;

                if (isGraphics)
                {
                    var f = gameObject.GetComponent<MeshFilter>();
                    if (!f)
                    {
	                    gameObject.AddComponent<MeshFilter>();
                    }
                    var r = gameObject.GetComponent<MeshRenderer>();
                    if (!r)
                    {
	                    gameObject.AddComponent<MeshRenderer>();
                    }
                }

                Transform transform = gameObject.transform;
                transform.SetParent(m_cacheRoot, false);

                m_objectPool.Push(gameObject);
            }

            m_allocCount = Math.Max(m_allocCount, initCount);
#if UNITY_EDITOR && DEBUG_DISPLAY_OBJECT_POOL
            Debug.Log($"[DisplayObjectPool<{typeof(T)}>][InitPool] new GameObject {gameObjectName} {m_allocCount}");
#endif
        }

        public static DisplayObjectPool<T> Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = CreateInstance(1024);
                }
                return m_instance;
            }
        }

        public GameObject Get(string gameObjectName, DisplayObject displayObject)
        {
            GameObject gameObject = null;
            while (m_objectPool.Count > 0)
            {
                gameObject = m_objectPool.Pop();
                if (gameObject == null)
                {
#if UNITY_EDITOR && DEBUG_DISPLAY_OBJECT_POOL
                    Debug.LogError($"[DisplayObjectPool<{typeof(T)}>][Get] gameObject == null");
#endif
                    continue;
                }
                if (Application.isPlaying)
                {
                    gameObject.name = gameObjectName;
                    DisplayObjectInfo info = gameObject.GetComponent<DisplayObjectInfo>();
                    info.displayObject = displayObject;
                }
#if UNITY_EDITOR && DEBUG_DISPLAY_OBJECT_POOL
                m_getPoolCount++;
#endif
                break;
            }

            if (gameObject == null)
            {
                m_allocCount++;
                gameObject = new GameObject(gameObjectName);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    DisplayObjectInfo info = gameObject.AddComponent<DisplayObjectInfo>();
                    info.displayObject = displayObject;
#if UNITY_EDITOR && DEBUG_DISPLAY_OBJECT_POOL
                    if (m_allocCount > m_capacity)
                    {
                        Debug.LogError($"[DisplayObjectPool<{typeof(T)}>][Get] new GameObject {gameObjectName} {m_allocCount}");
                    }
#endif
                }
            }

            return gameObject;
        }

        public void Recycle(GameObject gameObject)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Destroy(gameObject);
                return;
            }
#endif

            if (m_objectPool.Count < m_capacity)
            {
#if UNITY_EDITOR && DEBUG_DISPLAY_OBJECT_POOL
                Component[] components = gameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; ++i)
                {
                    var comp = components[i];
                    if (comp is Transform)
                    {
                        continue;
                    }
                    else if (comp is DisplayObjectInfo)
                    {
                        continue;
                    }
                    else if (comp is MeshRenderer)
                    {
                        continue;
                    }
                    else if (comp is MeshFilter)
                    {
                        continue;
                    }
                    else
                    {
                        Debug.LogError($"[DisplayObjectPool<{typeof(T)}>][Recycle] {gameObject.name} destroy Component {comp.name}");
                        Destroy(comp);
                    }
                }
#endif

                Transform transform = gameObject.transform;
                transform.SetParent(m_cacheRoot, false);
                transform.localRotation = Quaternion.identity;
                transform.localPosition = Vector3.zero;
                transform.localScale = Vector3.one;

                int childCount = transform.childCount;
                for (int i = childCount - 1; i >= 0; --i)
                {
                    var child = transform.GetChild(i);
                    var display = child.GetComponent<DisplayObjectInfo>();
                    if (display != null && display.displayObject != null)
                    {
                        display.displayObject.Dispose();
                        display.displayObject = null;
                    }
                    else
                    {
#if UNITY_EDITOR && DEBUG_DISPLAY_OBJECT_POOL
                        Debug.LogWarning($"[DisplayObjectPool<{typeof(T)}>][Recycle] {transform.name} destroy child {child.name}");
#endif
                        Destroy(child.gameObject);
                    }
                }

                m_objectPool.Push(gameObject);
#if UNITY_EDITOR && DEBUG_DISPLAY_OBJECT_POOL
                m_recycleCount++;
#endif
            }
            else
            {
#if UNITY_EDITOR && DEBUG_DISPLAY_OBJECT_POOL
                //Debug.LogError($"[DisplayObjectPool<{typeof(T)}>][Recycle] recycle over {m_capacity}");
                m_destroyCount++;
#endif
                Destroy(gameObject);
            }
        }

        private void Destroy(GameObject gameObject)
        {
            if (Application.isPlaying)
                GameObject.Destroy(gameObject);
            else
                GameObject.DestroyImmediate(gameObject);
        }

        private void Destroy(UnityEngine.Object obj)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }

#if DEBUG_DISPLAY_OBJECT_POOL
        public int GetAllocCount()
        {
            return m_allocCount;
        }

        public int GetCacheCount()
        {
            return m_objectPool.Count;
        }

        public int GetPoolCount()
        {
            return m_getPoolCount;
        }

        public int GetRecycleCount()
        {
            return m_recycleCount;
        }

        public int GetDestroyCount()
        {
            return m_destroyCount;
        }
#endif
    }

#if DEBUG_DISPLAY_OBJECT_POOL
    interface IDisplayObjectPoolDebug
    {
        int GetAllocCount();
        int GetCacheCount();
        int GetPoolCount();
        int GetRecycleCount();
        int GetDestroyCount();
    }

    public class DisplayObjectPoolDebug
    {
        private class SampleInfo
        {
            public IDisplayObjectPoolDebug pool;
            public int allocCount;
            public int cacheCount;
            public int getPoolCount;
            public int recycleCount;
            public int destroyCount;
            private StringBuilder buff;

            public SampleInfo(IDisplayObjectPoolDebug pool)
            {
                this.pool = pool;
            }

            public void Clear()
            {
                allocCount = 0;
                cacheCount = 0;
                getPoolCount = 0;
                recycleCount = 0;
                destroyCount = 0;
            }

            public SampleInfo BeginSample()
            {
                allocCount = pool.GetAllocCount();
                cacheCount = pool.GetCacheCount();
                getPoolCount = pool.GetPoolCount();
                recycleCount = pool.GetRecycleCount();
                destroyCount = pool.GetDestroyCount();
                return this;
            }

            public string EndSample()
            {
                int allocCount = pool.GetAllocCount();
                int cacheCount = pool.GetCacheCount();
                int getPoolCount = pool.GetPoolCount();
                int recycleCount = pool.GetRecycleCount();
                int destroyCount = pool.GetDestroyCount();

                if (buff == null)
                {
                    buff = new StringBuilder();
                }

                buff.Append(pool.GetType());
                AppendValue(buff, ": alloc: ", allocCount, this.allocCount);
                AppendValue(buff, ", cache: ", cacheCount, this.cacheCount);
                AppendValue(buff, ", getPool: ", getPoolCount, this.getPoolCount);
                AppendValue(buff, ", recycle: ", recycleCount, this.recycleCount);
                AppendValue(buff, ", destroy: ", destroyCount, this.destroyCount);
                buff.AppendLine();
                string str = buff.ToString();
                buff.Length = 0;
                return str;
            }

            private StringBuilder AppendValue(StringBuilder buff, string name, int newValue, int oldValue)
            {
                buff.Append(name).Append(newValue).Append(newValue >= oldValue ? " + " : " - ").Append(Math.Abs(newValue - oldValue));
                return buff;
            }
        }

        private static Dictionary<string, List<SampleInfo>> m_sampleDict;

        public static void BeginSample(string key)
        {
            if (m_sampleDict == null)
            {
                m_sampleDict = new Dictionary<string, List<SampleInfo>>();
            }

            if (!m_sampleDict.TryGetValue(key, out var samples))
            {
                samples = new List<SampleInfo>();
                samples.Add(new SampleInfo(DisplayObjectPool<Container>.Instance));
                samples.Add(new SampleInfo(DisplayObjectPool<Image>.Instance));
                samples.Add(new SampleInfo(DisplayObjectPool<TextField>.Instance));
                samples.Add(new SampleInfo(DisplayObjectPool<GoWrapper>.Instance));
                samples.Add(new SampleInfo(DisplayObjectPool<Shape>.Instance));
                samples.Add(new SampleInfo(DisplayObjectPool<SelectionShape>.Instance));
                m_sampleDict.Add(key, samples);
            }

            for (int i = 0; i < samples.Count; ++i)
            {
                var sample = samples[i];
                sample.Clear();
                sample.BeginSample();
            }
        }

        public static void EndSample(string key)
        {
            if (!m_sampleDict.TryGetValue(key, out var samples))
            {
                return;
            }

            StringBuilder buff = new StringBuilder();
            for (int i = 0; i < samples.Count; ++i)
            {
                var sample = samples[i];
                buff.Append(sample.EndSample());
            }
            Debug.Log($"{key}:\n{buff}");
        }

        public static void PrintDisplayObjectCount(GameObject gameObject)
        {
            DisplayObjectInfo[] infos = gameObject.GetComponentsInChildren<DisplayObjectInfo>(true);
            Debug.Log($"{gameObject.name} DisplayObjectInfo: {infos.Length}");
        }
    }
#endif
}
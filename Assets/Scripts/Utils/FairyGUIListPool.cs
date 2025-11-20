using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace FairyGUI.Utils
{
    public class FairyGUIListPool<T>
    {
        private static Stack<List<T>> mPool = new Stack<List<T>>();

        static FairyGUIListPool ()
        {
        }

        public static List<T> Get (int capacity = 10)
        {
            if (mPool.Count > 0)
            {
                var data = (List<T>)mPool.Pop();
                return data;
            }

            var newItem = new List<T>(capacity);
            return newItem;
        }

        public static void Recycle (ref List<T> item)
        {
            if (item != null)
            {
                List<T> a = item;
                item = null;
                a.Clear();
#if UNITY_EDITOR
                if (mPool.Contains(a))
                {
                    Debug.LogError("重复回收");
                    return;
                }
#endif
                mPool.Push(a);
            }
        }

        public static void Clear ()
        {
            mPool.Clear();
        }
    }
}

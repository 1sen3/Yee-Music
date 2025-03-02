using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Yee_Music.Services
{
    public class CacheService
    {
        private static CacheService _instance;
        public static CacheService Instance => _instance ??= new CacheService();

        private Dictionary<string, WeakReference<object>> _cache = new Dictionary<string, WeakReference<object>>();

        private CacheService() { }

        public void Add(string key, object value)
        {
            if (_cache.ContainsKey(key))
            {
                _cache[key] = new WeakReference<object>(value);
            }
            else
            {
                _cache.Add(key, new WeakReference<object>(value));
            }
        }

        public bool TryGetValue<T>(string key, out T value) where T : class
        {
            value = default;

            if (!_cache.TryGetValue(key, out var weakRef))
                return false;

            if (!weakRef.TryGetTarget(out var target))
            {
                _cache.Remove(key);
                return false;
            }

            value = target as T;
            return value != null;
        }

        public void Clear()
        {
            _cache.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void TrimMemory()
        {
            // 移除已经被垃圾回收的对象
            var keysToRemove = new List<string>();
            foreach (var pair in _cache)
            {
                if (!pair.Value.TryGetTarget(out _))
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            // 建议垃圾回收
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Debug.WriteLine($"内存清理完成，移除了 {keysToRemove.Count} 个缓存项");
        }
    }
}

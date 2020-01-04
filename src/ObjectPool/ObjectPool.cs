using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.ObjectPool
{
    public class ObjectPool<T> : IObjectPool<T>, IDisposable where T : class
    {
        private protected readonly T[] _items = null;
        private protected T _firstItem = null;
        private protected long _itemtotal = 0;
        private protected int _timeout = 0;
        private protected readonly IPooledObjectPolicy<T> _policy = null;
        public ObjectPool(IPooledObjectPolicy<T> policy)
            : this(policy, Environment.ProcessorCount * 2)
        {

        }
        public ObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained, int timeout = 180_000)
        {
            _policy = policy;
            _items = new T[maximumRetained - 1];
            _timeout = timeout;
        }
        public T Get()
        {
            var item = _firstItem;
            if (item == null || Interlocked.CompareExchange(ref _firstItem, null, item) != item)
            {
                var items = _items;
                for (var i = 0; i < items.Length; i++)
                {
                    item = items[i];
                    if (item != null && Interlocked.CompareExchange(ref items[i], null, item) == item)
                    {
                        return item;
                    }
                }
                if (_itemtotal <= _items.Length && Interlocked.Increment(ref _itemtotal) <= _items.Length + 1)
                {
                    item = Create();
                }
                else
                {
                    var task = Task.Run(() =>
                    {
                        while (true)
                        {
                            item = _firstItem;
                            if (Interlocked.CompareExchange(ref _firstItem, null, item) == item)
                            {
                                break;
                            }
                        }
                        return item;
                    });

                    if (!task.Wait(_timeout))
                    {
                        throw new TimeoutException($"Timeout in getting {typeof(T).Name} instance, please expand the capacity appropriately");
                    }
                    return task.Result;
                }
            }
            return item;
        }
    
        private T Create()
        {
            return _policy.Create();
        }
      
        public void Return(T obj)
        {
            if (_policy.Return(obj))
            {
                if (_firstItem != null || Interlocked.CompareExchange(ref _firstItem, obj, null) != null)
                {
                    var items = _items;
                    for (var i = 0; i < items.Length && Interlocked.CompareExchange(ref items[i], obj, null) != null; ++i)
                    {

                    }
                }
            }
        }
      
        public void Dispose()
        {
            if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
            {
                (_firstItem as IDisposable)?.Dispose();
                foreach (var item in _items)
                {
                    (item as IDisposable)?.Dispose();
                }
            }
        }
    }
}

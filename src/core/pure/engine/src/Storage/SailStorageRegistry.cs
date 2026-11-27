using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace sailflow.storage
{
    /// <summary>
    /// IStorage 唯一注册槽（同寿于一个 <c>SailClient</c>）。
    /// 后注册覆盖先前实现，并打覆盖日志（类比 R9）。
    /// </summary>
    public sealed class SailStorageRegistry
    {
        readonly Dictionary<Type, object> _map = new();

        public void Register<TStorage>(TStorage implementation)
            where TStorage : class
        {
            ArgumentNullException.ThrowIfNull(implementation);
            var key = typeof(TStorage);
            if (_map.TryGetValue(key, out var existing)
                && !ReferenceEquals(existing, implementation))
            {
                Trace.TraceInformation(
                    "[Storage] Override {0}: {1} → {2}",
                    key.Name,
                    existing.GetType().FullName,
                    implementation.GetType().FullName);
            }

            _map[key] = implementation;
        }

        public bool TryGet<TStorage>(out TStorage? storage)
            where TStorage : class
        {
            if (_map.TryGetValue(typeof(TStorage), out var raw) && raw is TStorage hit)
            {
                storage = hit;
                return true;
            }

            storage = null;
            return false;
        }

        public TStorage GetRequired<TStorage>()
            where TStorage : class
        {
            if (TryGet<TStorage>(out var storage) && storage != null)
                return storage;
            throw new InvalidOperationException(
                "No storage registered for "
                + typeof(TStorage).Name
                + ". Call UseSailData() / UseDefaultStorages() (sailflow.data), or useStorage<"
                + typeof(TStorage).Name
                + ">(...).");
        }

        public IReadOnlyCollection<Type> RegisteredKeys => _map.Keys;
    }
}

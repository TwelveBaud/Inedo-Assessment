using System;
using System.Collections;
using System.Collections.Generic;

namespace TECH_ASM_LS1
{
    /// <summary>
    /// Represents a dictionary where nonexistent elements are created on first access using the provided factory function.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    class LazyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> cacheDictionary;
        private Func<TKey, TValue> generatorFunc;

        public LazyDictionary(Func<TKey, TValue> generatorFunc)
        {
            this.cacheDictionary = new Dictionary<TKey, TValue>();
            this.generatorFunc = generatorFunc;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default;
            if (!cacheDictionary.ContainsKey(key))
            {
                try
                {
                    value = generatorFunc(key);
                    cacheDictionary[key] = value;
                    return true;
                }
                catch (Exception)
                { }
                return false;
            }
            return ((IDictionary<TKey, TValue>)cacheDictionary).TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get
            {
                if (!cacheDictionary.ContainsKey(key))
                {
                    var value = generatorFunc(key);
                    cacheDictionary[key] = value;
                    return value;
                }
                return cacheDictionary[key];
            }

#region Implemented by cacheDictionary
            set => ((IDictionary<TKey, TValue>)cacheDictionary)[key] = value;
        }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)cacheDictionary).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)cacheDictionary).Values;

        public int Count => ((ICollection<KeyValuePair<TKey, TValue>>)cacheDictionary).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)cacheDictionary).IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)cacheDictionary).Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)cacheDictionary).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)cacheDictionary).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)cacheDictionary).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)cacheDictionary).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)cacheDictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)cacheDictionary).GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)cacheDictionary).Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)cacheDictionary).Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)cacheDictionary).GetEnumerator();
        }
#endregion
    }
}

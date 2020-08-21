using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TECH_ASM_LS1
{
    class LazyDictionary<K, V> : IDictionary<K, V> where V : class
    {
        private Dictionary<K, V> cacheDictionary;
        private Func<K, V> generatorFunc;

        public LazyDictionary(Func<K, V> generatorFunc)
        {
            this.cacheDictionary = new Dictionary<K, V>();
            this.generatorFunc = generatorFunc;
        }

        public V this[K key]
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
            set => ((IDictionary<K, V>)cacheDictionary)[key] = value;
        }

        public ICollection<K> Keys => ((IDictionary<K, V>)cacheDictionary).Keys;

        public ICollection<V> Values => ((IDictionary<K, V>)cacheDictionary).Values;

        public int Count => ((ICollection<KeyValuePair<K, V>>)cacheDictionary).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<K, V>>)cacheDictionary).IsReadOnly;

        public void Add(K key, V value)
        {
            ((IDictionary<K, V>)cacheDictionary).Add(key, value);
        }

        public void Add(KeyValuePair<K, V> item)
        {
            ((ICollection<KeyValuePair<K, V>>)cacheDictionary).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<K, V>>)cacheDictionary).Clear();
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            return ((ICollection<KeyValuePair<K, V>>)cacheDictionary).Contains(item);
        }

        public bool ContainsKey(K key)
        {
            return ((IDictionary<K, V>)cacheDictionary).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<K, V>>)cacheDictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<K, V>>)cacheDictionary).GetEnumerator();
        }

        public bool Remove(K key)
        {
            return ((IDictionary<K, V>)cacheDictionary).Remove(key);
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            return ((ICollection<KeyValuePair<K, V>>)cacheDictionary).Remove(item);
        }

        public bool TryGetValue(K key, out V value)
        {
            return ((IDictionary<K, V>)cacheDictionary).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)cacheDictionary).GetEnumerator();
        }
        #endregion
    }
}

﻿// <copyright file="AttributesWithCapacity.cs" company="OpenTelemetry Authors">
// Copyright 2018, OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenTelemetry.Utils
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    internal class AttributesWithCapacity : IDictionary<string, object>
    {
        private readonly OrderedDictionary @delegate = new OrderedDictionary();
        private readonly int capacity;
        private int totalRecordedAttributes;

        public AttributesWithCapacity(int capacity)
        {
            this.capacity = capacity;
        }

        public int NumberOfDroppedAttributes => this.totalRecordedAttributes - this.Count;

        public ICollection<string> Keys => (ICollection<string>)this.@delegate.Keys;

        public ICollection<object> Values => (ICollection<object>)this.@delegate.Values;

        public int Count => this.@delegate.Count;

        public bool IsReadOnly => this.@delegate.IsReadOnly;

        public object this[string key]
        {
            get => this.@delegate[key];

            set => this.@delegate[key] = value;
        }

        public void PutAttribute(string key, object value)
        {
            this.totalRecordedAttributes += 1;
            this[key] = value;
            if (this.Count > this.capacity)
            {
                this.@delegate.RemoveAt(0);
            }
        }

        // Users must call this method instead of putAll to keep count of the total number of entries
        // inserted.
        public void PutAttributes(IDictionary<string, object> attributes)
        {
            foreach (var kvp in attributes)
            {
                this.PutAttribute(kvp.Key, kvp.Value);
            }
        }

        public void Add(string key, object value)
        {
            this.@delegate.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return this.@delegate.Contains(key);
        }

        public bool Remove(string key)
        {
            if (this.@delegate.Contains(key))
            {
                this.@delegate.Remove(key);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryGetValue(string key, out object value)
        {
            value = null;
            if (this.ContainsKey(key))
            {
                value = this.@delegate[key];
                return true;
            }

            return false;
        }

        public void Add(KeyValuePair<string, object> item)
        {
            this.@delegate.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            this.@delegate.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            var result = this.TryGetValue(item.Key, out var value);
            if (result)
            {
                return value.Equals(item.Value);
            }

            return false;
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            var entries = new DictionaryEntry[this.@delegate.Count];
            this.@delegate.CopyTo(entries, 0);

            for (var i = 0; i < entries.Length; i++)
            {
                array[i + arrayIndex] = new KeyValuePair<string, object>((string)entries[i].Key, (object)entries[i].Value);
            }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return this.Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            var array = new KeyValuePair<string, object>[this.@delegate.Count];
            this.CopyTo(array, 0);
            return array.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.@delegate.GetEnumerator();
        }
    }
}

/* Copyright (c) 2006-2008 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
/* Change history
* Oct 13 2008  Joe Feser       joseph.feser@gmail.com
* Converted ArrayLists and other .NET 1.1 collections to use Generics
* Combined IExtensionElement and IExtensionElementFactory interfaces
* 
*/
#region Using directives

#define USE_TRACING

using System;
using System.Collections;
using Google.GData.Client;
using System.Collections.Generic;

#endregion

//////////////////////////////////////////////////////////////////////
// contains typed collections based on the 1.1 .NET framework
// using typed collections has the benefit of additional code reliability
// and using them in the collection editor
// 
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Extensions
{

    /// <summary>
    /// base class to take an object pointer with extension information
    /// and expose a localname/namespace subset as a collection
    /// that still works on the original
    /// </summary>
    public class ExtensionCollection<T> : IList<T> where T : class, IExtensionElementFactory, new()
    {
        /// <summary>holds the owning feed</summary>
        private IExtensionContainer container;
        private List<T> _items = new List<T>();

        private static Dictionary<Type, IExtensionElementFactory> _cache = new Dictionary<Type, IExtensionElementFactory>();

        /// <summary>
        /// Get the XmlName for the Type
        /// </summary>
        /// <returns></returns>
        private static string CtorXmlName()
        {
            IExtensionElementFactory val;
            Type t = typeof(T);
            if (!_cache.TryGetValue(t, out val))
            {
                val = new T();
                _cache[t] = val;
            }
            return val.XmlName;
        }

        /// <summary>
        /// Get the Xml Namespace for the Type
        /// </summary>
        /// <returns></returns>
        private static string CtorXmlNS()
        {
            IExtensionElementFactory val;
            Type t = typeof(T);
            if (!_cache.TryGetValue(t, out val))
            {
                val = new T();
                _cache[t] = val;
            }
            return val.XmlNameSpace;
        }

        /// <summary>
        /// protected default constructor, not usable by outside
        /// </summary>
        public ExtensionCollection()
        {
        }

        /// <summary>
        /// takes the base object, and the localname/ns combo to look for
        /// will copy objects to an internal array for caching. Note that when the external 
        /// ExtensionList is modified, this will have no effect on this copy
        /// </summary>
        /// <param name="containerElement">the base element holding the extension list</param>
        public ExtensionCollection(IExtensionContainer containerElement)
            : this(containerElement, CtorXmlName(), CtorXmlNS())
        {
        }

        /// <summary>
        /// takes the base object, and the localname/ns combo to look for
        /// will copy objects to an internal array for caching. Note that when the external 
        /// ExtensionList is modified, this will have no effect on this copy
        /// </summary>
        /// <param name="containerElement">the base element holding the extension list</param>
        /// <param name="localName">the local name of the extension</param>
        /// <param name="ns">the namespace</param>
        public ExtensionCollection(IExtensionContainer containerElement, string localName, string ns)
            : base()
        {
            this.container = containerElement;
            if (this.container != null)
            {
                ExtensionList arr = this.container.FindExtensions(localName, ns);
                foreach (T o in arr)
                {
                    _items.Add(o);
                }
            }
        }

        /// <summary>standard typed accessor method </summary> 
        public T this[int index]
        {
            get
            {
                return ((T)_items[index]);
            }
            set
            {
                setItem(index, value);
            }
        }

        /// <summary>
        /// useful for subclasses that want to overload the set method
        /// </summary>
        /// <param name="index">the index in the array</param>
        /// <param name="item">the item to set </param>
        protected void setItem(int index, T item)
        {
            if (_items[index] != null)
            {
                if (this.container != null)
                {
                    this.container.ExtensionElements.Remove(_items[index]);
                }
            }
            _items[index] = item;
            if (item != null && this.container != null)
            {
                this.container.ExtensionElements.Add(item);
            }
        }


        /// <summary>
        /// default untyped add implementation. Adds the object as well to the parent
        /// object ExtensionList
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int Add(T value)
        {
            if (this.container != null)
            {
                this.container.ExtensionElements.Add(value);
            }
            _items.Add(value);
            return _items.Count - 1;
        }

        /// <summary>
        /// inserts an element into the collection by index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void Insert(int index, T value)
        {
            if (this.container != null && this.container.ExtensionElements.Contains(value))
            {
                this.container.ExtensionElements.Remove(value);
            }
            this.container.ExtensionElements.Add(value);
            _items.Insert(index, value);
        }

        /// <summary>
        /// removes an element from the collection
        /// </summary>
        /// <param name="value"></param>
        public bool Remove(T value)
        {
            bool success = _items.Remove(value);
            if (success && this.container != null)
            {
                success &= this.container.ExtensionElements.Remove(value);
            }
            return success;
        }

        /// <summary>standard typed indexOf method </summary>
        public int IndexOf(T value)
        {
            return (_items.IndexOf(value));
        }

        /// <summary>standard typed Contains method </summary> 
        public bool Contains(T value)
        {
            // If value is not of type AtomEntry, this will return false.
            return (_items.Contains(value));
        }

        /// <summary>standard override OnClear, to remove the objects from the extension list</summary> 
        protected void OnClear()
        {
            if (this.container != null)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    this.container.ExtensionElements.Remove(_items[i]);
                }
            }
        }

        #region IList<T> Members


        public void RemoveAt(int index)
        {
            T item = _items[index];
            //_items.RemoveAt(index);
            Remove(item);
        }

        #endregion

        #region ICollection<T> Members

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void Clear()
        {
            OnClear();
            _items.Clear();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.ToArray().CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool ICollection<T>.Remove(T item)
        {
            return Remove(item);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion
    }
}

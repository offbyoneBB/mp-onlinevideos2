using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Database.CustomTypes;

namespace OnlineVideos.Sites.Cornerstone.CustomTypes
{
  public class DynamicList<T> : List<T>, IDynamic {

    // An event that clients can use to be notified whenever the
    // elements of the list change.
    public event ChangedEventHandler Changed;

    // Invoke the Changed event; called whenever list changes
    protected virtual void OnChanged(EventArgs e) {
      if (Changed != null)
        Changed(this, e);
    }

    // Gets or sets the element at the specified index.
    public virtual new T this[int index] {
      get {
        return base[index];
      }
      set {
        base[index] = value;
        OnChanged(EventArgs.Empty);
      }
    }

    // Adds an object to the end of the List.
    public virtual new void Add(T item) {
      base.Add(item);
      OnChanged(EventArgs.Empty);
    }

    // Adds the elements of the specified collection to the end of the List.
    public virtual new void AddRange(IEnumerable<T> collection) {
      base.AddRange(collection);
      OnChanged(EventArgs.Empty);
    }

    // Removes all elements from the List.
    public virtual new void Clear() {
      base.Clear();
      OnChanged(EventArgs.Empty);
    }

    // Inserts an element into the List at the specified index.
    public virtual new void Insert(int index, T item) {
      base.Insert(index, item);
      OnChanged(EventArgs.Empty);
    }

    // Inserts the elements of a collection into the List at the specified index.
    public virtual new void InsertRange(int index, IEnumerable<T> collection) {
      base.InsertRange(index, collection);
      OnChanged(EventArgs.Empty);
    }

    // Removes the first occurrence of a specific object from the List.
    public virtual new bool Remove(T item) {
      if (base.Remove(item)) {
        OnChanged(EventArgs.Empty);
        return true;
      }

      return false;
    }

    // Removes the all the elements that match the conditions defined by the specified predicate.
    public virtual new int RemoveAll(Predicate<T> match) {
      int result = base.RemoveAll(match);
      if (result > 0) 
        OnChanged(EventArgs.Empty);

      return result;
    }

    // Removes the element at the specified index of the List.
    public virtual new void RemoveAt(int index) {
      base.RemoveAt(index);
      OnChanged(EventArgs.Empty);
    }

    // Removes a range of elements from the List.
    public virtual new void RemoveRange(int index, int count) {
      base.RemoveRange(index, count);
      OnChanged(EventArgs.Empty);
    }
  }
}
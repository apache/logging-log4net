#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using log4net.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace log4net.Plugin;

/// <summary>
/// A strongly-typed collection of <see cref="IPlugin"/> objects.
/// </summary>
/// <author>Nicko Cadell</author>
public class PluginCollection : ICollection, IList, IEnumerable, ICloneable
{
  /// <summary>
  /// Supports type-safe iteration over a <see cref="PluginCollection"/>.
  /// </summary>
  /// <exclude/>
  public interface IPluginCollectionEnumerator
  {
    /// <summary>
    ///  Gets the current element in the collection.
    /// </summary>
    IPlugin Current { get; }

    /// <summary>
    ///  Advances the enumerator to the next element in the collection.
    /// </summary>
    /// <returns>
    ///  <c>true</c> if the enumerator was successfully advanced to the next element; 
    ///  <c>false</c> if the enumerator has passed the end of the collection.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  The collection was modified after the enumerator was created.
    /// </exception>
    bool MoveNext();

    /// <summary>
    ///  Sets the enumerator to its initial position, before the first element in the collection.
    /// </summary>
    void Reset();
  }

  private const int DefaultCapacity = 16;

  private IPlugin[] _array;
  private int _count;
  private int _version;

  /// <summary>
  ///  Creates a read-only wrapper for a <c>PluginCollection</c> instance.
  /// </summary>
  /// <param name="list">list to create a readonly wrapper arround</param>
  /// <returns>
  /// A <c>PluginCollection</c> wrapper that is read-only.
  /// </returns>
  public static PluginCollection ReadOnly(PluginCollection list)
    => new ReadOnlyPluginCollection(list.EnsureNotNull());

  /// <summary>
  ///  Initializes a new instance of the <c>PluginCollection</c> class
  ///  that is empty and has the default initial capacity.
  /// </summary>
  public PluginCollection() => _array = new IPlugin[DefaultCapacity];

  /// <summary>
  /// Initializes a new instance of the <c>PluginCollection</c> class
  /// that has the specified initial capacity.
  /// </summary>
  /// <param name="capacity">
  /// The number of elements that the new <c>PluginCollection</c> is initially capable of storing.
  /// </param>
  public PluginCollection(int capacity) => _array = new IPlugin[capacity];

  /// <summary>
  /// Initializes a new instance of the <c>PluginCollection</c> class
  /// that contains elements copied from the specified <c>PluginCollection</c>.
  /// </summary>
  /// <param name="c">The <c>PluginCollection</c> whose elements are copied to the new collection.</param>
  public PluginCollection(PluginCollection c)
  {
    _array = new IPlugin[c.Count];
    AddRange(c);
  }

  /// <summary>
  /// Initializes a new instance of the <c>PluginCollection</c> class
  /// that contains elements copied from the specified <see cref="IPlugin"/> array.
  /// </summary>
  /// <param name="a">The <see cref="IPlugin"/> array whose elements are copied to the new list.</param>
  public PluginCollection(IPlugin[] a)
  {
    _array = new IPlugin[a.Length];
    AddRange(a);
  }

  /// <summary>
  /// Initializes a new instance of the <c>PluginCollection</c> class
  /// that contains elements copied from the specified <see cref="IPlugin"/> collection.
  /// </summary>
  /// <param name="col">The <see cref="IPlugin"/> collection whose elements are copied to the new list.</param>
  public PluginCollection(ICollection col)
  {
    _array = new IPlugin[col.Count];
    AddRange(col);
  }

  /// <summary>
  /// Initializes a new instance of the <c>PluginCollection</c> class
  /// that contains elements copied from the specified <see cref="IPlugin"/> collection.
  /// </summary>
  /// <param name="col">The <see cref="IPlugin"/> collection whose elements are copied to the new list.</param>
  public PluginCollection(ICollection<IPlugin> col)
  {
    _array = new IPlugin[col.Count];
    AddRange(col);
  }

  /// <summary>
  /// Type visible only to our subclasses
  /// Used to access protected constructor
  /// </summary>
  /// <exclude/>
  protected internal enum Tag
  {
    /// <summary>
    /// A value
    /// </summary>
    Default
  }

  /// <summary>
  /// Allow subclasses to avoid our default constructors
  /// </summary>
  /// <exclude/>
  protected internal PluginCollection(Tag _)
  {
    _array = Array.Empty<IPlugin>();
  }

  /// <summary>
  /// Gets the number of elements actually contained in the <c>PluginCollection</c>.
  /// </summary>
  public virtual int Count => _count;

  /// <summary>
  /// Copies the entire <c>PluginCollection</c> to a one-dimensional
  /// <see cref="IPlugin"/> array.
  /// </summary>
  /// <param name="array">The one-dimensional <see cref="IPlugin"/> array to copy to.</param>
  public virtual void CopyTo(IPlugin[] array) => CopyTo(array, 0);

  /// <summary>
  /// Copies the entire <c>PluginCollection</c> to a one-dimensional
  /// <see cref="IPlugin"/> array, starting at the specified index of the target array.
  /// </summary>
  /// <param name="array">The one-dimensional <see cref="IPlugin"/> array to copy to.</param>
  /// <param name="start">The zero-based index in <paramref name="array"/> at which copying begins.</param>
  public virtual void CopyTo(IPlugin[] array, int start)
  {
    if (_count > array.GetUpperBound(0) + 1 - start)
    {
      throw new ArgumentException("Destination array was not long enough.");
    }

    Array.Copy(this._array, 0, array, start, _count);
  }

  /// <summary>
  /// Gets a value indicating whether access to the collection is synchronized (thread-safe).
  /// </summary>
  /// <returns>false, because the backing type is an array, which is never thread-safe.</returns>
  public virtual bool IsSynchronized => false;

  /// <summary>
  /// Gets an object that can be used to synchronize access to the collection.
  /// </summary>
  public virtual object SyncRoot => _array;

  /// <summary>
  /// Gets or sets the <see cref="IPlugin"/> at the specified index.
  /// </summary>
  /// <value>
  /// The <see cref="IPlugin"/> at the specified index.
  /// </value>
  /// <param name="index">The zero-based index of the element to get or set.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="index"/> is less than zero.</para>
  /// <para>-or-</para>
  /// <para><paramref name="index"/> is equal to or greater than <see cref="PluginCollection.Count"/>.</para>
  /// </exception>
  public virtual IPlugin this[int index]
  {
    get
    {
      ValidateIndex(index); // throws
      return _array[index];
    }
    set
    {
      ValidateIndex(index); // throws
      ++_version;
      _array[index] = value;
    }
  }

  /// <summary>
  /// Adds a <see cref="IPlugin"/> to the end of the <c>PluginCollection</c>.
  /// </summary>
  /// <param name="item">The <see cref="IPlugin"/> to be added to the end of the <c>PluginCollection</c>.</param>
  /// <returns>The index at which the value has been added.</returns>
  public virtual int Add(IPlugin item)
  {
    if (_count == _array.Length)
    {
      EnsureCapacity(_count + 1);
    }

    _array[_count] = item;
    _version++;

    return _count++;
  }

  /// <summary>
  /// Removes all elements from the <c>PluginCollection</c>.
  /// </summary>
  public virtual void Clear()
  {
    ++_version;
    _array = new IPlugin[DefaultCapacity];
    _count = 0;
  }

  /// <summary>
  /// Creates a shallow copy of the <see cref="PluginCollection"/>.
  /// </summary>
  /// <returns>A new <see cref="PluginCollection"/> with a shallow copy of the collection data.</returns>
  public virtual object Clone()
  {
    var newCol = new PluginCollection(_count);
    Array.Copy(_array, 0, newCol._array, 0, _count);
    newCol._count = _count;
    newCol._version = _version;

    return newCol;
  }

  /// <summary>
  /// Determines whether a given <see cref="IPlugin"/> is in the <c>PluginCollection</c>.
  /// </summary>
  /// <param name="item">The <see cref="IPlugin"/> to check for.</param>
  /// <returns><c>true</c> if <paramref name="item"/> is found in the <c>PluginCollection</c>; otherwise, <c>false</c>.</returns>
  public virtual bool Contains(IPlugin item)
  {
    for (int i = 0; i != _count; ++i)
    {
      if (_array[i].Equals(item))
      {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Returns the zero-based index of the first occurrence of a <see cref="IPlugin"/>
  /// in the <c>PluginCollection</c>.
  /// </summary>
  /// <param name="item">The <see cref="IPlugin"/> to locate in the <c>PluginCollection</c>.</param>
  /// <returns>
  /// The zero-based index of the first occurrence of <paramref name="item"/> 
  /// in the entire <c>PluginCollection</c>, if found; otherwise, -1.
  /// </returns>
  public virtual int IndexOf(IPlugin item)
  {
    for (int i = 0; i != _count; ++i)
    {
      if (_array[i].Equals(item))
      {
        return i;
      }
    }
    return -1;
  }

  /// <summary>
  /// Inserts an element into the <c>PluginCollection</c> at the specified index.
  /// </summary>
  /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
  /// <param name="item">The <see cref="IPlugin"/> to insert.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="index"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="index"/> is equal to or greater than <see cref="PluginCollection.Count"/>.</para>
  /// </exception>
  public virtual void Insert(int index, IPlugin item)
  {
    ValidateIndex(index, true); // throws

    if (_count == _array.Length)
    {
      EnsureCapacity(_count + 1);
    }

    if (index < _count)
    {
      Array.Copy(_array, index, _array, index + 1, _count - index);
    }

    _array[index] = item;
    _count++;
    _version++;
  }

  /// <summary>
  /// Removes the first occurrence of a specific <see cref="IPlugin"/> from the <c>PluginCollection</c>.
  /// </summary>
  /// <param name="item">The <see cref="IPlugin"/> to remove from the <c>PluginCollection</c>.</param>
  /// <exception cref="ArgumentException">
  /// The specified <see cref="IPlugin"/> was not found in the <c>PluginCollection</c>.
  /// </exception>
  public virtual void Remove(IPlugin item)
  {
    int i = IndexOf(item);
    if (i < 0)
    {
      throw new ArgumentException("Cannot remove the specified item because it was not found in the specified Collection.");
    }
    ++_version;
    RemoveAt(i);
  }

  /// <summary>
  /// Removes the element at the specified index of the <c>PluginCollection</c>.
  /// </summary>
  /// <param name="index">The zero-based index of the element to remove.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="index"/> is less than zero.</para>
  /// <para>-or-</para>
  /// <para><paramref name="index"/> is equal to or greater than <see cref="PluginCollection.Count"/>.</para>
  /// </exception>
  public virtual void RemoveAt(int index)
  {
    ValidateIndex(index); // throws

    _count--;

    if (index < _count)
    {
      Array.Copy(_array, index + 1, _array, index, _count - index);
    }

    // We can't set the deleted entry equal to null, because it might be a value type.
    // Instead, we'll create an empty single-element array of the right type and copy it 
    // over the entry we want to erase.
    IPlugin[] temp = new IPlugin[1];
    Array.Copy(temp, 0, _array, _count, 1);
    _version++;
  }

  /// <summary>
  /// Gets a value indicating whether the collection has a fixed size.
  /// </summary>
  /// <value><c>true</c> if the collection has a fixed size; otherwise, <c>false</c>. The default is <c>false</c>.</value>
  public virtual bool IsFixedSize => false;

  /// <summary>
  /// Gets a value indicating whether the IList is read-only.
  /// </summary>
  /// <value><c>true</c> if the collection is read-only; otherwise, <c>false</c>. The default is <c>false</c>.</value>
  public virtual bool IsReadOnly => false;

  /// <summary>
  /// Returns an enumerator that can iterate through the <c>PluginCollection</c>.
  /// </summary>
  /// <returns>An <see cref="Enumerator"/> for the entire <c>PluginCollection</c>.</returns>
  public virtual IPluginCollectionEnumerator GetEnumerator() => new Enumerator(this);

  /// <summary>
  /// Gets or sets the number of elements the <c>PluginCollection</c> can contain.
  /// </summary>
  /// <value>
  /// The number of elements the <c>PluginCollection</c> can contain.
  /// </value>
  public virtual int Capacity
  {
    get => _array.Length;
    set
    {
      if (value < _count)
      {
        value = _count;
      }

      if (value != _array.Length)
      {
        if (value > 0)
        {
          IPlugin[] temp = new IPlugin[value];
          Array.Copy(_array, 0, temp, 0, _count);
          _array = temp;
        }
        else
        {
          _array = new IPlugin[DefaultCapacity];
        }
      }
    }
  }

  /// <summary>
  /// Adds the elements of another <c>PluginCollection</c> to the current <c>PluginCollection</c>.
  /// </summary>
  /// <param name="x">The <c>PluginCollection</c> whose elements should be added to the end of the current <c>PluginCollection</c>.</param>
  /// <returns>The new <see cref="PluginCollection.Count"/> of the <c>PluginCollection</c>.</returns>
  public virtual int AddRange(PluginCollection x)
  {
    if (_count + x.Count >= _array.Length)
    {
      EnsureCapacity(_count + x.Count);
    }

    Array.Copy(x._array, 0, _array, _count, x.Count);
    _count += x.Count;
    _version++;

    return _count;
  }

  /// <summary>
  /// Adds the elements of a <see cref="IPlugin"/> array to the current <c>PluginCollection</c>.
  /// </summary>
  /// <param name="x">The <see cref="IPlugin"/> array whose elements should be added to the end of the <c>PluginCollection</c>.</param>
  /// <returns>The new <see cref="PluginCollection.Count"/> of the <c>PluginCollection</c>.</returns>
  public virtual int AddRange(IPlugin[] x)
  {
    if (_count + x.Length >= _array.Length)
    {
      EnsureCapacity(_count + x.Length);
    }

    Array.Copy(x, 0, _array, _count, x.Length);
    _count += x.Length;
    _version++;

    return _count;
  }

  /// <summary>
  /// Adds the elements of a <see cref="IPlugin"/> collection to the current <c>PluginCollection</c>.
  /// </summary>
  /// <param name="col">The <see cref="IPlugin"/> collection whose elements should be added to the end of the <c>PluginCollection</c>.</param>
  /// <returns>The new <see cref="PluginCollection.Count"/> of the <c>PluginCollection</c>.</returns>
  public virtual int AddRange(ICollection col)
  {
    if (_count + col.Count >= _array.Length)
    {
      EnsureCapacity(_count + col.Count);
    }

    foreach (object item in col)
    {
      Add((IPlugin)item);
    }

    return _count;
  }

  /// <summary>
  /// Adds the elements of a <see cref="IPlugin"/> collection to the current <c>PluginCollection</c>.
  /// </summary>
  /// <param name="col">The <see cref="IPlugin"/> collection whose elements should be added to the end of the <c>PluginCollection</c>.</param>
  /// <returns>The new <see cref="PluginCollection.Count"/> of the <c>PluginCollection</c>.</returns>
  public virtual int AddRange(ICollection<IPlugin> col)
  {
    if (_count + col.Count >= _array.Length)
    {
      EnsureCapacity(_count + col.Count);
    }

    foreach (IPlugin item in col)
    {
      Add(item);
    }

    return _count;
  }

  /// <summary>
  /// Sets the capacity to the actual number of elements.
  /// </summary>
  public virtual void TrimToSize() => Capacity = _count;

  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="i"/> is less than zero.</para>
  /// <para>-or-</para>
  /// <para><paramref name="i"/> is equal to or greater than <see cref="PluginCollection.Count"/>.</para>
  /// </exception>
  private void ValidateIndex(int i) => ValidateIndex(i, false);

  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="i"/> is less than zero.</para>
  /// <para>-or-</para>
  /// <para><paramref name="i"/> is equal to or greater than <see cref="PluginCollection.Count"/>.</para>
  /// </exception>
  private void ValidateIndex(int i, bool allowEqualEnd)
  {
    int max = (allowEqualEnd) ? (_count) : (_count - 1);
    if (i < 0 || i > max)
    {
      throw SystemInfo.CreateArgumentOutOfRangeException(nameof(i), i, $"Index was out of range. Must be non-negative and less than the size of the collection. [{i}] Specified argument was out of the range of valid values.");
    }
  }

  private void EnsureCapacity(int min)
  {
    int newCapacity = ((_array.Length == 0) ? DefaultCapacity : _array.Length * 2);
    if (newCapacity < min)
    {
      newCapacity = min;
    }

    Capacity = newCapacity;
  }

  void ICollection.CopyTo(Array array, int start) => Array.Copy(this._array, 0, array, start, _count);

  object? IList.this[int i]
  {
    get => this[i];
    set => this[i] = value.EnsureIs<IPlugin>();
  }

  int IList.Add(object? x) => Add(x.EnsureIs<IPlugin>());

  bool IList.Contains(object? x) => Contains(x.EnsureIs<IPlugin>());

  int IList.IndexOf(object? x) => IndexOf(x.EnsureIs<IPlugin>());

  void IList.Insert(int pos, object? x) => Insert(pos, x.EnsureIs<IPlugin>());

  void IList.Remove(object? x) => Remove(x.EnsureIs<IPlugin>());

  void IList.RemoveAt(int pos) => RemoveAt(pos);

  IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)GetEnumerator();

  /// <summary>
  /// Supports simple iteration over a <see cref="PluginCollection"/>.
  /// </summary>
  /// <exclude/>
  private sealed class Enumerator : IEnumerator, IPluginCollectionEnumerator
  {
    private readonly PluginCollection _collection;
    private int _index;
    private readonly int _version;

    /// <summary>
    /// Initializes a new instance of the <c>Enumerator</c> class.
    /// </summary>
    /// <param name="tc"></param>
    internal Enumerator(PluginCollection tc)
    {
      _collection = tc;
      _index = -1;
      _version = tc._version;
    }

    /// <summary>
    /// Gets the current element in the collection.
    /// </summary>
    public IPlugin Current => _collection[_index];

    /// <summary>
    /// Advances the enumerator to the next element in the collection.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the enumerator was successfully advanced to the next element; 
    /// <c>false</c> if the enumerator has passed the end of the collection.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The collection was modified after the enumerator was created.
    /// </exception>
    public bool MoveNext()
    {
      if (_version != _collection._version)
      {
        throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
      }

      ++_index;
      return (_index < _collection.Count);
    }

    /// <summary>
    /// Sets the enumerator to its initial position, before the first element in the collection.
    /// </summary>
    public void Reset() => _index = -1;

    object IEnumerator.Current => Current;
  }

  /// <exclude/>
  private sealed class ReadOnlyPluginCollection : PluginCollection
  {
    private readonly PluginCollection _collection;

    internal ReadOnlyPluginCollection(PluginCollection list) : base(Tag.Default)
      => _collection = list;

    public override void CopyTo(IPlugin[] array) => _collection.CopyTo(array);

    public override void CopyTo(IPlugin[] array, int start) => _collection.CopyTo(array, start);

    public override int Count => _collection.Count;

    public override bool IsSynchronized => _collection.IsSynchronized;

    public override object SyncRoot => _collection.SyncRoot;

    public override IPlugin this[int i]
    {
      get => _collection[i];
      set => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
    }

    public override int Add(IPlugin x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void Clear() => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override bool Contains(IPlugin x) => _collection.Contains(x);

    public override int IndexOf(IPlugin x) => _collection.IndexOf(x);

    public override void Insert(int pos, IPlugin x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void Remove(IPlugin x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void RemoveAt(int pos) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override bool IsFixedSize => true;

    public override bool IsReadOnly => true;

    public override IPluginCollectionEnumerator GetEnumerator() => _collection.GetEnumerator();

    // (just to mimic some nice features of ArrayList)
    public override int Capacity
    {
      get => _collection.Capacity;
      set => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
    }

    public override int AddRange(PluginCollection x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override int AddRange(IPlugin[] x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
  }
}

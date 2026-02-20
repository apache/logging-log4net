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

namespace log4net.Core;

/// <summary>
/// A strongly-typed collection of <see cref="Level"/> objects.
/// </summary>
/// <author>Nicko Cadell</author>
public class LevelCollection : ICollection, IList, IEnumerable, ICloneable
{
  /// <summary>
  /// Supports type-safe iteration over a <see cref="LevelCollection"/>.
  /// </summary>
  public interface ILevelCollectionEnumerator
  {
    /// <summary>
    /// Gets the current element in the collection.
    /// </summary>
    Level Current { get; }

    /// <summary>
    /// Advances the enumerator to the next element in the collection.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the enumerator was successfully advanced to the next element; 
    /// <see langword="false"/> if the enumerator has passed the end of the collection.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The collection was modified after the enumerator was created.
    /// </exception>
    bool MoveNext();

    /// <summary>
    /// Sets the enumerator to its initial position, before the first element in the collection.
    /// </summary>
    void Reset();
  }

  private const int DefaultCapacity = 16;

  private Level[] _array;
  private int _version;

  /// <summary>
  /// Creates a read-only wrapper for a <see cref="LevelCollection"/> instance.
  /// </summary>
  /// <param name="list">list to create a readonly wrapper arround</param>
  /// <returns>
  /// A <see cref="LevelCollection"/> wrapper that is read-only.
  /// </returns>
  public static LevelCollection ReadOnly(LevelCollection list)
    => new ReadOnlyLevelCollection(list.EnsureNotNull());

  /// <summary>
  /// Initializes a new instance of the <see cref="LevelCollection"/> class
  /// that is empty and has the default initial capacity.
  /// </summary>
  public LevelCollection() => _array = new Level[DefaultCapacity];

  /// <summary>
  /// Initializes a new instance of the <see cref="LevelCollection"/> class
  /// that has the specified initial capacity.
  /// </summary>
  /// <param name="capacity">
  /// The number of elements that the new <see cref="LevelCollection"/> is initially capable of storing.
  /// </param>
  public LevelCollection(int capacity) => _array = new Level[capacity];

  /// <summary>
  /// Initializes a new instance of the <see cref="LevelCollection"/> class
  /// that contains elements copied from the specified <see cref="LevelCollection"/>.
  /// </summary>
  /// <param name="collection">The <see cref="LevelCollection"/> whose elements are copied to the new collection.</param>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors")]
  public LevelCollection(LevelCollection collection)
  {
    _array = new Level[collection.EnsureNotNull().Count];
    AddRange(collection);
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="LevelCollection"/> class
  /// that contains elements copied from the specified <see cref="Level"/> array.
  /// </summary>
  /// <param name="array">The <see cref="Level"/> array whose elements are copied to the new list.</param>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors")]
  public LevelCollection(Level[] array)
  {
    _array = new Level[array.EnsureNotNull().Length];
    AddRange(array);
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="LevelCollection"/> class
  /// that contains elements copied from the specified <see cref="Level"/> collection.
  /// </summary>
  /// <param name="collection">The <see cref="Level"/> collection whose elements are copied to the new list.</param>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors")]
  public LevelCollection(ICollection collection)
  {
    _array = new Level[collection.EnsureNotNull().Count];
    AddRange(collection);
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="LevelCollection"/> class
  /// that contains elements copied from the specified <see cref="Level"/> collection.
  /// </summary>
  /// <param name="collection">The <see cref="Level"/> collection whose elements are copied to the new list.</param>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors")]
  public LevelCollection(ICollection<Level> collection)
  {
    _array = new Level[collection.EnsureNotNull().Count];
    AddRange((ICollection)collection);
  }

  /// <summary>
  /// Type visible only to our subclasses
  /// Used to access protected constructor
  /// </summary>
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
  protected internal LevelCollection(Tag _) => _array = Array.Empty<Level>();

  /// <summary>
  /// Gets the number of elements actually contained in the <see cref="LevelCollection"/>.
  /// </summary>
  public virtual int Count { get; private set; }

  /// <summary>
  /// Copies the entire <see cref="LevelCollection"/> to a one-dimensional
  /// <see cref="Level"/> array.
  /// </summary>
  /// <param name="array">The one-dimensional <see cref="Level"/> array to copy to.</param>
  public virtual void CopyTo(Level[] array) => CopyTo(array, 0);

  /// <summary>
  /// Copies the entire <see cref="LevelCollection"/> to a one-dimensional
  /// <see cref="Level"/> array, starting at the specified index of the target array.
  /// </summary>
  /// <param name="array">The one-dimensional <see cref="Level"/> array to copy to.</param>
  /// <param name="start">The zero-based index in <paramref name="array"/> at which copying begins.</param>
  public virtual void CopyTo(Level[] array, int start)
  {
    if (Count > array.EnsureNotNull().GetUpperBound(0) + 1 - start)
    {
      throw new ArgumentException("Destination array was not long enough.");
    }

    Array.Copy(_array, 0, array, start, Count);
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
  /// Gets or sets the <see cref="Level"/> at the specified index.
  /// </summary>
  /// <param name="index">The zero-based index of the element to get or set.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="index"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="index"/> is equal to or greater than <see cref="Count"/>.</para>
  /// </exception>
  public virtual Level this[int index]
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
  /// Adds a <see cref="Level"/> to the end of the <see cref="LevelCollection"/>.
  /// </summary>
  /// <param name="item">The <see cref="Level"/> to be added to the end of the <see cref="LevelCollection"/>.</param>
  /// <returns>The index at which the value has been added.</returns>
  public virtual int Add(Level item)
  {
    if (Count == _array.Length)
    {
      EnsureCapacity(Count + 1);
    }

    _array[Count] = item;
    _version++;

    return Count++;
  }

  /// <summary>
  /// Removes all elements from the <see cref="LevelCollection"/>.
  /// </summary>
  public virtual void Clear()
  {
    ++_version;
    _array = new Level[DefaultCapacity];
    Count = 0;
  }

  /// <summary>
  /// Creates a shallow copy of the <see cref="LevelCollection"/>.
  /// </summary>
  /// <returns>A new <see cref="LevelCollection"/> with a shallow copy of the collection data.</returns>
  public virtual object Clone()
  {
    LevelCollection newCol = new(Count);
    Array.Copy(_array, 0, newCol._array, 0, Count);
    newCol.Count = Count;
    newCol._version = _version;

    return newCol;
  }

  /// <summary>
  /// Determines whether a given <see cref="Level"/> is in the <see cref="LevelCollection"/>.
  /// </summary>
  /// <param name="item">The <see cref="Level"/> to check for.</param>
  /// <returns><see langword="true"/> if <paramref name="item"/> is found in the <see cref="LevelCollection"/>; otherwise, <see langword="false"/>.</returns>
  public virtual bool Contains(Level item)
  {
    for (int i = 0; i != Count; ++i)
    {
      if (_array[i].Equals(item))
      {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Returns the zero-based index of the first occurrence of a <see cref="Level"/>
  /// in the <see cref="LevelCollection"/>.
  /// </summary>
  /// <param name="item">The <see cref="Level"/> to locate in the <see cref="LevelCollection"/>.</param>
  /// <returns>
  /// The zero-based index of the first occurrence of <paramref name="item"/> 
  /// in the entire <see cref="LevelCollection"/>, if found; otherwise, -1.
  ///  </returns>
  public virtual int IndexOf(Level item)
  {
    for (int i = 0; i != Count; ++i)
    {
      if (_array[i].Equals(item))
      {
        return i;
      }
    }
    return -1;
  }

  /// <summary>
  /// Inserts an element into the <see cref="LevelCollection"/> at the specified index.
  /// </summary>
  /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
  /// <param name="item">The <see cref="Level"/> to insert.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="index"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="index"/> is equal to or greater than <see cref="Count"/>.</para>
  /// </exception>
  public virtual void Insert(int index, Level item)
  {
    ValidateIndex(index, true); // throws

    if (Count == _array.Length)
    {
      EnsureCapacity(Count + 1);
    }

    if (index < Count)
    {
      Array.Copy(_array, index, _array, index + 1, Count - index);
    }

    _array[index] = item;
    Count++;
    _version++;
  }

  /// <summary>
  /// Removes the first occurrence of a specific <see cref="Level"/> from the <see cref="LevelCollection"/>.
  /// </summary>
  /// <param name="item">The <see cref="Level"/> to remove from the <see cref="LevelCollection"/>.</param>
  /// <exception cref="ArgumentException">
  /// The specified <see cref="Level"/> was not found in the <see cref="LevelCollection"/>.
  /// </exception>
  public virtual void Remove(Level item)
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
  /// Removes the element at the specified index of the <see cref="LevelCollection"/>.
  /// </summary>
  /// <param name="index">The zero-based index of the element to remove.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="index"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="index"/> is equal to or greater than <see cref="Count"/>.</para>
  /// </exception>
  public virtual void RemoveAt(int index)
  {
    ValidateIndex(index); // throws

    Count--;

    if (index < Count)
    {
      Array.Copy(_array, index + 1, _array, index, Count - index);
    }

    // We can't set the deleted entry equal to null, because it might be a value type.
    // Instead, we'll create an empty single-element array of the right type and copy it 
    // over the entry we want to erase.
    Level[] temp = new Level[1];
    Array.Copy(temp, 0, _array, Count, 1);
    _version++;
  }

  /// <summary>
  /// Gets a value indicating whether the collection has a fixed size.
  /// </summary>
  /// <value>true if the collection has a fixed size; otherwise, false. The default is false</value>
  public virtual bool IsFixedSize => false;

  /// <summary>
  /// Gets a value indicating whether the IList is read-only.
  /// </summary>
  /// <value>true if the collection is read-only; otherwise, false. The default is false</value>
  public virtual bool IsReadOnly => false;

  /// <summary>
  /// Returns an enumerator that can iterate through the <see cref="LevelCollection"/>.
  /// </summary>
  /// <returns>An <see cref="Enumerator"/> for the entire <see cref="LevelCollection"/>.</returns>
  public virtual ILevelCollectionEnumerator GetEnumerator() => new Enumerator(this);

  /// <summary>
  /// Gets or sets the number of elements the <see cref="LevelCollection"/> can contain.
  /// </summary>
  public virtual int Capacity
  {
    get => _array.Length;
    set
    {
      if (value < Count)
      {
        value = Count;
      }

      if (value != _array.Length)
      {
        if (value > 0)
        {
          Level[] temp = new Level[value];
          Array.Copy(_array, 0, temp, 0, Count);
          _array = temp;
        }
        else
        {
          _array = new Level[DefaultCapacity];
        }
      }
    }
  }

  /// <summary>
  /// Adds the elements of another <see cref="LevelCollection"/> to the current <see cref="LevelCollection"/>.
  /// </summary>
  /// <param name="collection">The <see cref="LevelCollection"/> whose elements should be added to the end of the current <see cref="LevelCollection"/>.</param>
  /// <returns>The new <see cref="Count"/> of the <see cref="LevelCollection"/>.</returns>
  public virtual int AddRange(LevelCollection collection)
  {
    if (Count + collection.EnsureNotNull().Count >= _array.Length)
    {
      EnsureCapacity(Count + collection.Count);
    }

    Array.Copy(collection._array, 0, _array, Count, collection.Count);
    Count += collection.Count;
    _version++;

    return Count;
  }

  /// <summary>
  /// Adds the elements of a <see cref="Level"/> array to the current <see cref="LevelCollection"/>.
  /// </summary>
  /// <param name="array">The <see cref="Level"/> array whose elements should be added to the end of the <see cref="LevelCollection"/>.</param>
  /// <returns>The new <see cref="Count"/> of the <see cref="LevelCollection"/>.</returns>
  public virtual int AddRange(Level[] array)
  {
    if (Count + array.EnsureNotNull().Length >= _array.Length)
    {
      EnsureCapacity(Count + array.Length);
    }

    Array.Copy(array, 0, _array, Count, array.Length);
    Count += array.Length;
    _version++;

    return Count;
  }

  /// <summary>
  /// Adds the elements of a <see cref="Level"/> collection to the current <see cref="LevelCollection"/>.
  /// </summary>
  /// <param name="collection">The <see cref="Level"/> collection whose elements should be added to the end of the <see cref="LevelCollection"/>.</param>
  /// <returns>The new <see cref="Count"/> of the <see cref="LevelCollection"/>.</returns>
  public virtual int AddRange(ICollection collection)
  {
    if (Count + collection.EnsureNotNull().Count >= _array.Length)
    {
      EnsureCapacity(Count + collection.Count);
    }

    foreach (object item in collection)
    {
      Add((Level)item);
    }

    return Count;
  }

  /// <summary>
  /// Sets the capacity to the actual number of elements.
  /// </summary>
  public virtual void TrimToSize() => Capacity = Count;

  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="i"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="i"/> is equal to or greater than <see cref="Count"/>.</para>
  /// </exception>
  private void ValidateIndex(int i) => ValidateIndex(i, false);

  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="i"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="i"/> is equal to or greater than <see cref="Count"/>.</para>
  /// </exception>
  private void ValidateIndex(int i, bool allowEqualEnd)
  {
    int max = allowEqualEnd ? Count : (Count - 1);
    if (i < 0 || i > max)
    {
      throw SystemInfo.CreateArgumentOutOfRangeException(nameof(i), i, 
        $"Index was out of range. Must be non-negative and less than the size of the collection. [{i}] Specified argument was out of the range of valid values.");
    }
  }

  private void EnsureCapacity(int min)
  {
    int newCapacity = (_array.Length == 0) ? DefaultCapacity : _array.Length * 2;
    if (newCapacity < min)
    {
      newCapacity = min;
    }

    Capacity = newCapacity;
  }

  void ICollection.CopyTo(Array array, int start) => Array.Copy(_array, 0, array, start, Count);

  object? IList.this[int i]
  {
    get => this[i];
    set => this[i] = value.EnsureIs<Level>();
  }

  int IList.Add(object? x) => Add(x.EnsureIs<Level>());

  bool IList.Contains(object? x) => Contains(x.EnsureIs<Level>());

  int IList.IndexOf(object? x) => IndexOf(x.EnsureIs<Level>());

  void IList.Insert(int pos, object? x) => Insert(pos, x.EnsureIs<Level>());

  void IList.Remove(object? x) => Remove(x.EnsureIs<Level>());

  void IList.RemoveAt(int pos) => RemoveAt(pos);

  IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)GetEnumerator();

  /// <summary>
  /// Supports simple iteration over a <see cref="LevelCollection"/>.
  /// </summary>
  private sealed class Enumerator : IEnumerator, ILevelCollectionEnumerator
  {
    private readonly LevelCollection _collection;
    private int _index;
    private readonly int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="Enumerator"/> class.
    /// </summary>
    /// <param name="tc"></param>
    internal Enumerator(LevelCollection tc)
    {
      _collection = tc;
      _index = -1;
      _version = tc._version;
    }

    /// <summary>
    /// Gets the current element in the collection.
    /// </summary>
    public Level Current => _collection[_index];

    /// <summary>
    /// Advances the enumerator to the next element in the collection.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the enumerator was successfully advanced to the next element; 
    /// <see langword="false"/> if the enumerator has passed the end of the collection.
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
      return _index < _collection.Count;
    }

    /// <summary>
    /// Sets the enumerator to its initial position, before the first element in the collection.
    /// </summary>
    public void Reset() => _index = -1;

    object IEnumerator.Current => Current;
  }

  private sealed class ReadOnlyLevelCollection : LevelCollection
  {
    private readonly LevelCollection _collection;

    internal ReadOnlyLevelCollection(LevelCollection list) : base(Tag.Default) => _collection = list;

    public override void CopyTo(Level[] array) => _collection.CopyTo(array);

    public override void CopyTo(Level[] array, int start) => _collection.CopyTo(array, start);

    public override int Count => _collection.Count;

    public override bool IsSynchronized => _collection.IsSynchronized;

    public override object SyncRoot => _collection.SyncRoot;

    public override Level this[int i]
    {
      get => _collection[i];
      set => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
    }

    public override int Add(Level x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void Clear() => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override bool Contains(Level x) => _collection.Contains(x);

    public override int IndexOf(Level x) => _collection.IndexOf(x);

    public override void Insert(int pos, Level x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void Remove(Level x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void RemoveAt(int pos) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override bool IsFixedSize => true;

    public override bool IsReadOnly => true;

    public override ILevelCollectionEnumerator GetEnumerator() => _collection.GetEnumerator();

    // (just to mimic some nice features of ArrayList)
    public override int Capacity
    {
      get => _collection.Capacity;
      set => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
    }

    public override int AddRange(LevelCollection x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override int AddRange(Level[] x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
  }
}
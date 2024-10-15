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
    /// <c>true</c> if the enumerator was successfully advanced to the next element; 
    /// <c>false</c> if the enumerator has passed the end of the collection.
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

  private const int DEFAULT_CAPACITY = 16;

  private Level[] array;
  private int version;

  /// <summary>
  /// Creates a read-only wrapper for a <c>LevelCollection</c> instance.
  /// </summary>
  /// <param name="list">list to create a readonly wrapper arround</param>
  /// <returns>
  /// A <c>LevelCollection</c> wrapper that is read-only.
  /// </returns>
  public static LevelCollection ReadOnly(LevelCollection list)
    => new ReadOnlyLevelCollection(list.EnsureNotNull());

  /// <summary>
  /// Initializes a new instance of the <c>LevelCollection</c> class
  /// that is empty and has the default initial capacity.
  /// </summary>
  public LevelCollection() => array = new Level[DEFAULT_CAPACITY];

  /// <summary>
  /// Initializes a new instance of the <c>LevelCollection</c> class
  /// that has the specified initial capacity.
  /// </summary>
  /// <param name="capacity">
  /// The number of elements that the new <c>LevelCollection</c> is initially capable of storing.
  /// </param>
  public LevelCollection(int capacity) => array = new Level[capacity];

  /// <summary>
  /// Initializes a new instance of the <c>LevelCollection</c> class
  /// that contains elements copied from the specified <c>LevelCollection</c>.
  /// </summary>
  /// <param name="c">The <c>LevelCollection</c> whose elements are copied to the new collection.</param>
  public LevelCollection(LevelCollection c)
  {
    array = new Level[c.Count];
    AddRange(c);
  }

  /// <summary>
  /// Initializes a new instance of the <c>LevelCollection</c> class
  /// that contains elements copied from the specified <see cref="Level"/> array.
  /// </summary>
  /// <param name="a">The <see cref="Level"/> array whose elements are copied to the new list.</param>
  public LevelCollection(Level[] a)
  {
    array = new Level[a.Length];
    AddRange(a);
  }

  /// <summary>
  /// Initializes a new instance of the <c>LevelCollection</c> class
  /// that contains elements copied from the specified <see cref="Level"/> collection.
  /// </summary>
  /// <param name="col">The <see cref="Level"/> collection whose elements are copied to the new list.</param>
  public LevelCollection(ICollection col)
  {
    array = new Level[col.Count];
    AddRange(col);
  }

  /// <summary>
  /// Initializes a new instance of the <c>LevelCollection</c> class
  /// that contains elements copied from the specified <see cref="Level"/> collection.
  /// </summary>
  /// <param name="col">The <see cref="Level"/> collection whose elements are copied to the new list.</param>
  public LevelCollection(ICollection<Level> col)
  {
    array = new Level[col.Count];
    AddRange((ICollection)col);
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
  protected internal LevelCollection(Tag _)
  {
    array = Array.Empty<Level>();
  }

  /// <summary>
  /// Gets the number of elements actually contained in the <c>LevelCollection</c>.
  /// </summary>
  public virtual int Count { get; private set; }

  /// <summary>
  /// Copies the entire <c>LevelCollection</c> to a one-dimensional
  /// <see cref="Level"/> array.
  /// </summary>
  /// <param name="array">The one-dimensional <see cref="Level"/> array to copy to.</param>
  public virtual void CopyTo(Level[] array) => this.CopyTo(array, 0);

  /// <summary>
  /// Copies the entire <c>LevelCollection</c> to a one-dimensional
  /// <see cref="Level"/> array, starting at the specified index of the target array.
  /// </summary>
  /// <param name="array">The one-dimensional <see cref="Level"/> array to copy to.</param>
  /// <param name="start">The zero-based index in <paramref name="array"/> at which copying begins.</param>
  public virtual void CopyTo(Level[] array, int start)
  {
    if (Count > array.GetUpperBound(0) + 1 - start)
    {
      throw new ArgumentException("Destination array was not long enough.");
    }

    Array.Copy(this.array, 0, array, start, Count);
  }

  /// <summary>
  /// Gets a value indicating whether access to the collection is synchronized (thread-safe).
  /// </summary>
  /// <returns>false, because the backing type is an array, which is never thread-safe.</returns>
  public virtual bool IsSynchronized => false;

  /// <summary>
  /// Gets an object that can be used to synchronize access to the collection.
  /// </summary>
  public virtual object SyncRoot => array;

  /// <summary>
  /// Gets or sets the <see cref="Level"/> at the specified index.
  /// </summary>
  /// <param name="index">The zero-based index of the element to get or set.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="index"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="index"/> is equal to or greater than <see cref="LevelCollection.Count"/>.</para>
  /// </exception>
  public virtual Level this[int index]
  {
    get
    {
      ValidateIndex(index); // throws
      return array[index];
    }
    set
    {
      ValidateIndex(index); // throws
      ++version;
      array[index] = value;
    }
  }

  /// <summary>
  /// Adds a <see cref="Level"/> to the end of the <c>LevelCollection</c>.
  /// </summary>
  /// <param name="item">The <see cref="Level"/> to be added to the end of the <c>LevelCollection</c>.</param>
  /// <returns>The index at which the value has been added.</returns>
  public virtual int Add(Level item)
  {
    if (Count == array.Length)
    {
      EnsureCapacity(Count + 1);
    }

    array[Count] = item;
    version++;

    return Count++;
  }

  /// <summary>
  /// Removes all elements from the <c>LevelCollection</c>.
  /// </summary>
  public virtual void Clear()
  {
    ++version;
    array = new Level[DEFAULT_CAPACITY];
    Count = 0;
  }

  /// <summary>
  /// Creates a shallow copy of the <see cref="LevelCollection"/>.
  /// </summary>
  /// <returns>A new <see cref="LevelCollection"/> with a shallow copy of the collection data.</returns>
  public virtual object Clone()
  {
    var newCol = new LevelCollection(Count);
    Array.Copy(array, 0, newCol.array, 0, Count);
    newCol.Count = Count;
    newCol.version = version;

    return newCol;
  }

  /// <summary>
  /// Determines whether a given <see cref="Level"/> is in the <c>LevelCollection</c>.
  /// </summary>
  /// <param name="item">The <see cref="Level"/> to check for.</param>
  /// <returns><c>true</c> if <paramref name="item"/> is found in the <c>LevelCollection</c>; otherwise, <c>false</c>.</returns>
  public virtual bool Contains(Level item)
  {
    for (int i = 0; i != Count; ++i)
    {
      if (array[i].Equals(item))
      {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Returns the zero-based index of the first occurrence of a <see cref="Level"/>
  /// in the <c>LevelCollection</c>.
  /// </summary>
  /// <param name="item">The <see cref="Level"/> to locate in the <c>LevelCollection</c>.</param>
  /// <returns>
  /// The zero-based index of the first occurrence of <paramref name="item"/> 
  /// in the entire <c>LevelCollection</c>, if found; otherwise, -1.
  ///  </returns>
  public virtual int IndexOf(Level item)
  {
    for (int i = 0; i != Count; ++i)
    {
      if (array[i].Equals(item))
      {
        return i;
      }
    }
    return -1;
  }

  /// <summary>
  /// Inserts an element into the <c>LevelCollection</c> at the specified index.
  /// </summary>
  /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
  /// <param name="item">The <see cref="Level"/> to insert.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="index"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="index"/> is equal to or greater than <see cref="LevelCollection.Count"/>.</para>
  /// </exception>
  public virtual void Insert(int index, Level item)
  {
    ValidateIndex(index, true); // throws

    if (Count == array.Length)
    {
      EnsureCapacity(Count + 1);
    }

    if (index < Count)
    {
      Array.Copy(array, index, array, index + 1, Count - index);
    }

    array[index] = item;
    Count++;
    version++;
  }

  /// <summary>
  /// Removes the first occurrence of a specific <see cref="Level"/> from the <c>LevelCollection</c>.
  /// </summary>
  /// <param name="item">The <see cref="Level"/> to remove from the <c>LevelCollection</c>.</param>
  /// <exception cref="ArgumentException">
  /// The specified <see cref="Level"/> was not found in the <c>LevelCollection</c>.
  /// </exception>
  public virtual void Remove(Level item)
  {
    int i = IndexOf(item);
    if (i < 0)
    {
      throw new System.ArgumentException("Cannot remove the specified item because it was not found in the specified Collection.");
    }

    ++version;
    RemoveAt(i);
  }

  /// <summary>
  /// Removes the element at the specified index of the <c>LevelCollection</c>.
  /// </summary>
  /// <param name="index">The zero-based index of the element to remove.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="index"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="index"/> is equal to or greater than <see cref="LevelCollection.Count"/>.</para>
  /// </exception>
  public virtual void RemoveAt(int index)
  {
    ValidateIndex(index); // throws

    Count--;

    if (index < Count)
    {
      Array.Copy(array, index + 1, array, index, Count - index);
    }

    // We can't set the deleted entry equal to null, because it might be a value type.
    // Instead, we'll create an empty single-element array of the right type and copy it 
    // over the entry we want to erase.
    Level[] temp = new Level[1];
    Array.Copy(temp, 0, array, Count, 1);
    version++;
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
  /// Returns an enumerator that can iterate through the <c>LevelCollection</c>.
  /// </summary>
  /// <returns>An <see cref="Enumerator"/> for the entire <c>LevelCollection</c>.</returns>
  public virtual ILevelCollectionEnumerator GetEnumerator() => new Enumerator(this);

  /// <summary>
  /// Gets or sets the number of elements the <c>LevelCollection</c> can contain.
  /// </summary>
  public virtual int Capacity
  {
    get
    {
      return array.Length;
    }
    set
    {
      if (value < Count)
      {
        value = Count;
      }

      if (value != array.Length)
      {
        if (value > 0)
        {
          Level[] temp = new Level[value];
          Array.Copy(array, 0, temp, 0, Count);
          array = temp;
        }
        else
        {
          array = new Level[DEFAULT_CAPACITY];
        }
      }
    }
  }

  /// <summary>
  /// Adds the elements of another <c>LevelCollection</c> to the current <c>LevelCollection</c>.
  /// </summary>
  /// <param name="x">The <c>LevelCollection</c> whose elements should be added to the end of the current <c>LevelCollection</c>.</param>
  /// <returns>The new <see cref="LevelCollection.Count"/> of the <c>LevelCollection</c>.</returns>
  public virtual int AddRange(LevelCollection x)
  {
    if (Count + x.Count >= array.Length)
    {
      EnsureCapacity(Count + x.Count);
    }

    Array.Copy(x.array, 0, array, Count, x.Count);
    Count += x.Count;
    version++;

    return Count;
  }

  /// <summary>
  /// Adds the elements of a <see cref="Level"/> array to the current <c>LevelCollection</c>.
  /// </summary>
  /// <param name="x">The <see cref="Level"/> array whose elements should be added to the end of the <c>LevelCollection</c>.</param>
  /// <returns>The new <see cref="LevelCollection.Count"/> of the <c>LevelCollection</c>.</returns>
  public virtual int AddRange(Level[] x)
  {
    if (Count + x.Length >= array.Length)
    {
      EnsureCapacity(Count + x.Length);
    }

    Array.Copy(x, 0, array, Count, x.Length);
    Count += x.Length;
    version++;

    return Count;
  }

  /// <summary>
  /// Adds the elements of a <see cref="Level"/> collection to the current <c>LevelCollection</c>.
  /// </summary>
  /// <param name="col">The <see cref="Level"/> collection whose elements should be added to the end of the <c>LevelCollection</c>.</param>
  /// <returns>The new <see cref="LevelCollection.Count"/> of the <c>LevelCollection</c>.</returns>
  public virtual int AddRange(ICollection col)
  {
    if (Count + col.Count >= array.Length)
    {
      EnsureCapacity(Count + col.Count);
    }

    foreach (object item in col)
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
  /// <para><paramref name="i"/> is equal to or greater than <see cref="LevelCollection.Count"/>.</para>
  /// </exception>
  private void ValidateIndex(int i) => ValidateIndex(i, false);

  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="i"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="i"/> is equal to or greater than <see cref="LevelCollection.Count"/>.</para>
  /// </exception>
  private void ValidateIndex(int i, bool allowEqualEnd)
  {
    int max = (allowEqualEnd) ? (Count) : (Count - 1);
    if (i < 0 || i > max)
    {
      throw log4net.Util.SystemInfo.CreateArgumentOutOfRangeException(nameof(i), i, $"Index was out of range. Must be non-negative and less than the size of the collection. [{i}] Specified argument was out of the range of valid values.");
    }
  }

  private void EnsureCapacity(int min)
  {
    int newCapacity = ((array.Length == 0) ? DEFAULT_CAPACITY : array.Length * 2);
    if (newCapacity < min)
    {
      newCapacity = min;
    }

    Capacity = newCapacity;
  }

  void ICollection.CopyTo(Array array, int start) => Array.Copy(this.array, 0, array, start, Count);

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
    private readonly LevelCollection collection;
    private int index;
    private readonly int version;

    /// <summary>
    /// Initializes a new instance of the <c>Enumerator</c> class.
    /// </summary>
    /// <param name="tc"></param>
    internal Enumerator(LevelCollection tc)
    {
      collection = tc;
      index = -1;
      version = tc.version;
    }

    /// <summary>
    /// Gets the current element in the collection.
    /// </summary>
    public Level Current => collection[index];

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
      if (version != collection.version)
      {
        throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
      }

      ++index;
      return (index < collection.Count);
    }

    /// <summary>
    /// Sets the enumerator to its initial position, before the first element in the collection.
    /// </summary>
    public void Reset() => index = -1;

    object IEnumerator.Current => Current;
  }

  private sealed class ReadOnlyLevelCollection : LevelCollection
  {
    private readonly LevelCollection collection;

    internal ReadOnlyLevelCollection(LevelCollection list) : base(Tag.Default)
    {
      collection = list;
    }

    public override void CopyTo(Level[] array) => collection.CopyTo(array);

    public override void CopyTo(Level[] array, int start) => collection.CopyTo(array, start);

    public override int Count => collection.Count;

    public override bool IsSynchronized => collection.IsSynchronized;

    public override object SyncRoot => collection.SyncRoot;

    public override Level this[int i]
    {
      get => collection[i];
      set => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
    }

    public override int Add(Level x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void Clear() => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override bool Contains(Level x) => collection.Contains(x);

    public override int IndexOf(Level x) => collection.IndexOf(x);

    public override void Insert(int pos, Level x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void Remove(Level x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void RemoveAt(int pos) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override bool IsFixedSize => true;

    public override bool IsReadOnly => true;

    public override ILevelCollectionEnumerator GetEnumerator() => collection.GetEnumerator();

    // (just to mimic some nice features of ArrayList)
    public override int Capacity
    {
      get => collection.Capacity;
      set => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
    }

    public override int AddRange(LevelCollection x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override int AddRange(Level[] x) => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
  }
}
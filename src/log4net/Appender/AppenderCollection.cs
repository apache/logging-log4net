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

namespace log4net.Appender;

/// <summary>
/// A strongly-typed collection of <see cref="IAppender"/> objects.
/// </summary>
/// <author>Nicko Cadell</author>
public class AppenderCollection : IList, ICloneable, ICollection<IAppender>
{
  /// <summary>
  /// Supports type-safe iteration over a <see cref="AppenderCollection"/>.
  /// </summary>
  [Obsolete("Use IEnumerator<IAppender> instead of AppenderCollection.IAppenderCollectionEnumerator")]
  public interface IAppenderCollectionEnumerator : IEnumerator<IAppender>
  { }

  private const int DefaultCapacity = 16;

  private IAppender[] _array;
  private int _count;
  private int _version;

  /// <summary>
  /// Creates a read-only wrapper for a <see cref="AppenderCollection"/> instance.
  /// </summary>
  /// <param name="list">list to create a readonly wrapper around</param>
  /// <returns>
  /// An <see cref="AppenderCollection"/> wrapper that is read-only.
  /// </returns>
  public static AppenderCollection ReadOnly(AppenderCollection list)
    => new ReadOnlyAppenderCollection(list.EnsureNotNull());

  /// <summary>
  /// An empty readonly static AppenderCollection
  /// </summary>
  public static readonly AppenderCollection EmptyCollection = ReadOnly([]);

  /// <summary>
  /// Initializes a new instance of the <see cref="AppenderCollection"/> class
  /// that is empty and has the default initial capacity.
  /// </summary>
  public AppenderCollection()
    : this(DefaultCapacity)
  { }

  /// <summary>
  /// Initializes a new instance of the <see cref="AppenderCollection"/> class
  /// that has the specified initial capacity.
  /// </summary>
  /// <param name="capacity">
  /// The number of elements that the new <see cref="AppenderCollection"/> is initially capable of storing.
  /// </param>
  public AppenderCollection(int capacity) => _array = new IAppender[capacity];

  /// <summary>
  /// Initializes a new instance of the <see cref="AppenderCollection"/> class
  /// that contains elements copied from the specified <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="c">The <see cref="AppenderCollection"/> whose elements are copied to the new collection.</param>
  public AppenderCollection(AppenderCollection c)
  {
    _array = new IAppender[c.Count];
    AddRange(c);
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="AppenderCollection"/> class
  /// that contains elements copied from the specified <see cref="IAppender"/> array.
  /// </summary>
  /// <param name="a">The <see cref="IAppender"/> array whose elements are copied to the new list.</param>
  public AppenderCollection(IAppender[] a)
  {
    _array = new IAppender[a.Length];
    AddRange(a);
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="AppenderCollection"/> class
  /// that contains elements copied from the specified <see cref="IAppender"/> collection.
  /// </summary>
  /// <param name="col">The <see cref="IAppender"/> collection whose elements are copied to the new list.</param>
  public AppenderCollection(ICollection col)
  {
    _array = new IAppender[col.Count];
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
  protected internal AppenderCollection(Tag _) => _array = [];

  /// <summary>
  /// Gets the number of elements actually contained in the <see cref="AppenderCollection"/>.
  /// </summary>
  public virtual int Count => _count;

  /// <summary>
  /// Copies the entire <see cref="AppenderCollection"/> to a one-dimensional
  /// <see cref="IAppender"/> array.
  /// </summary>
  /// <param name="array">The one-dimensional <see cref="IAppender"/> array to copy to.</param>
  public virtual void CopyTo(IAppender[] array) => CopyTo(array, 0);

  /// <summary>
  /// Copies the entire <see cref="AppenderCollection"/> to a one-dimensional
  /// <see cref="IAppender"/> array, starting at the specified index of the target array.
  /// </summary>
  /// <param name="array">The one-dimensional <see cref="IAppender"/> array to copy to.</param>
  /// <param name="start">The zero-based index in <paramref name="array"/> at which copying begins.</param>
  public virtual void CopyTo(IAppender[] array, int start)
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
  /// Gets or sets the <see cref="IAppender"/> at the specified index.
  /// </summary>
  /// <param name="index">The zero-based index of the element to get or set.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  ///    <para><paramref name="index"/> is less than zero</para>
  ///    <para>-or-</para>
  ///    <para><paramref name="index"/> is equal to or greater than <see cref="Count"/>.</para>
  /// </exception>
  public virtual IAppender this[int index]
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


  void ICollection<IAppender>.Add(IAppender item) => Add(item);

  /// <summary>
  /// Adds a <see cref="IAppender"/> to the end of the <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="item">The <see cref="IAppender"/> to be added to the end of the <see cref="AppenderCollection"/>.</param>
  /// <returns>The new <see cref="Count"/></returns>
  public virtual int Add(IAppender item)
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
  /// Removes all elements from the <see cref="AppenderCollection"/>.
  /// </summary>
  public virtual void Clear()
  {
    ++_version;
    _array = new IAppender[DefaultCapacity];
    _count = 0;
  }

  /// <summary>
  /// Creates a shallow copy of the <see cref="AppenderCollection"/>.
  /// </summary>
  /// <returns>A new <see cref="AppenderCollection"/> with a shallow copy of the collection data.</returns>
  public virtual object Clone()
  {
    AppenderCollection newCol = new(_count);
    Array.Copy(_array, 0, newCol._array, 0, _count);
    newCol._count = _count;
    newCol._version = _version;

    return newCol;
  }

  /// <summary>
  /// Determines whether a given <see cref="IAppender"/> is in the <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="item">The <see cref="IAppender"/> to check for.</param>
  /// <returns><see langword="true"/> if <paramref name="item"/> is found in the <see cref="AppenderCollection"/>; otherwise, <see langword="false"/>.</returns>
  public virtual bool Contains(IAppender item)
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
  /// Returns the zero-based index of the first occurrence of a <see cref="IAppender"/>
  /// in the <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="item">The <see cref="IAppender"/> to locate in the <see cref="AppenderCollection"/>.</param>
  /// <returns>
  /// The zero-based index of the first occurrence of <paramref name="item"/> 
  /// in the entire <see cref="AppenderCollection"/>, if found; otherwise, -1.
  /// </returns>
  public virtual int IndexOf(IAppender item)
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
  /// Inserts an element into the <see cref="AppenderCollection"/> at the specified index.
  /// </summary>
  /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
  /// <param name="item">The <see cref="IAppender"/> to insert.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="index"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="index"/> is equal to or greater than <see cref="Count"/>.</para>
  /// </exception>
  public virtual void Insert(int index, IAppender item)
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
  /// Removes the first occurrence of a specific <see cref="IAppender"/> from the <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="item">The <see cref="IAppender"/> to remove from the <see cref="AppenderCollection"/>.</param>
  /// <returns>True if the item was removed.</returns>
  /// <exception cref="ArgumentException">
  /// The specified <see cref="IAppender"/> was not found in the <see cref="AppenderCollection"/>.
  /// </exception>
  public virtual void Remove(IAppender item)
  {
    int i = IndexOf(item);
    if (i < 0)
    {
      throw new ArgumentException("Cannot remove the specified item because it was not found in the specified Collection.");
    }

    ++_version;
    RemoveAt(i);
  }

  bool ICollection<IAppender>.Remove(IAppender item)
  {
    Remove(item);
    return true;
  }

  /// <summary>
  /// Removes the element at the specified index of the <see cref="AppenderCollection"/>.
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

    _count--;

    if (index < _count)
    {
      Array.Copy(_array, index + 1, _array, index, _count - index);
    }

    // We can't set the deleted entry equal to null, because it might be a value type.
    // Instead, we'll create an empty single-element array of the right type and copy it 
    // over the entry we want to erase.
    IAppender[] temp = new IAppender[1];
    Array.Copy(temp, 0, _array, _count, 1);
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
  /// Returns an enumerator that can iterate through the <see cref="AppenderCollection"/>.
  /// </summary>
  /// <returns>An <see cref="Enumerator"/> for the entire <see cref="AppenderCollection"/>.</returns>
  public virtual IEnumerator<IAppender> GetEnumerator() => new Enumerator(this);

  /// <summary>
  /// Gets or sets the number of elements the <see cref="AppenderCollection"/> can contain.
  /// </summary>
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
          IAppender[] temp = new IAppender[value];
          Array.Copy(_array, 0, temp, 0, _count);
          _array = temp;
        }
        else
        {
          _array = new IAppender[DefaultCapacity];
        }
      }
    }
  }

  /// <summary>
  /// Adds the elements of another <see cref="AppenderCollection"/> to the current <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="x">The <see cref="AppenderCollection"/> whose elements should be added to the end of the current <see cref="AppenderCollection"/>.</param>
  /// <returns>The new <see cref="Count"/> of the <see cref="AppenderCollection"/>.</returns>
  public virtual int AddRange(AppenderCollection x)
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
  /// Adds the elements of a <see cref="IAppender"/> array to the current <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="x">The <see cref="IAppender"/> array whose elements should be added to the end of the <see cref="AppenderCollection"/>.</param>
  /// <returns>The new <see cref="Count"/> of the <see cref="AppenderCollection"/>.</returns>
  public virtual int AddRange(IAppender[] x)
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
  /// Adds the elements of a <see cref="IAppender"/> collection to the current <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="col">The <see cref="IAppender"/> collection whose elements should be added to the end of the <see cref="AppenderCollection"/>.</param>
  /// <returns>The new <see cref="Count"/> of the <see cref="AppenderCollection"/>.</returns>
  public virtual int AddRange(ICollection col)
  {
    if (_count + col.Count >= _array.Length)
    {
      EnsureCapacity(_count + col.Count);
    }

    foreach (object item in col)
    {
      Add((IAppender)item);
    }

    return _count;
  }

  /// <summary>
  /// Sets the capacity to the actual number of elements.
  /// </summary>
  public virtual void TrimToSize() => Capacity = _count;

  /// <summary>
  /// Return the collection elements as an array
  /// </summary>
  /// <returns>the array</returns>
  public virtual IAppender[] ToArray()
  {
    var resultArray = new IAppender[_count];
    if (_count > 0)
    {
      Array.Copy(_array, 0, resultArray, 0, _count);
    }
    return resultArray;
  }

  /// <exception cref="ArgumentOutOfRangeException">
  /// <para><paramref name="i"/> is less than zero</para>
  /// <para>-or-</para>
  /// <para><paramref name="i"/> is equal to or greater than <see cref="Count"/>.</para>
  /// </exception>
  private void ValidateIndex(int i, bool allowEqualEnd = false)
  {
    int max = allowEqualEnd ? _count : (_count - 1);
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

  void ICollection.CopyTo(Array array, int start)
  {
    if (_count > 0)
    {
      Array.Copy(this._array, 0, array, start, _count);
    }
  }

  object? IList.this[int i]
  {
    get => this[i];
    set => this[i] = value.EnsureIs<IAppender>();
  }

  int IList.Add(object? x)
  {
    if (x is IAppender appender)
    {
      return Add(appender);
    }

    return -1;
  }

  bool IList.Contains(object? x)
  {
    if (x is IAppender appender)
    {
      return Contains(appender);
    }

    return false;
  }

  int IList.IndexOf(object? x) => IndexOf(x.EnsureIs<IAppender>());

  void IList.Insert(int pos, object? x) => Insert(pos, x.EnsureIs<IAppender>());

  void IList.Remove(object? x) => Remove(x.EnsureIs<IAppender>());

  void IList.RemoveAt(int pos) => RemoveAt(pos);

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  /// <summary>
  /// Supports simple iteration over a <see cref="AppenderCollection"/>.
  /// </summary>
  /// <exclude/>
#pragma warning disable CS0618 // Type or member is obsolete
  private sealed class Enumerator : IAppenderCollectionEnumerator
#pragma warning restore CS0618 // Type or member is obsolete
  {
    private readonly AppenderCollection _collection;
    private int _index;
    private readonly int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="Enumerator"/> class.
    /// </summary>
    internal Enumerator(AppenderCollection tc)
    {
      _collection = tc;
      _index = -1;
      _version = tc._version;
    }

    /// <summary>
    /// Gets the current element in the collection.
    /// </summary>
    public IAppender Current => _collection[_index];

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

    public void Dispose()
    {
    }
  }

  /// <exclude/>
  private sealed class ReadOnlyAppenderCollection : AppenderCollection, ICollection
  {
    private readonly AppenderCollection _collection;

    internal ReadOnlyAppenderCollection(AppenderCollection list)
      : base(Tag.Default) => _collection = list;

    public override void CopyTo(IAppender[] array) => _collection.CopyTo(array);

    public override void CopyTo(IAppender[] array, int start) => _collection.CopyTo(array, start);

    void ICollection.CopyTo(Array array, int start) => ((ICollection)_collection).CopyTo(array, start);

    public override int Count => _collection.Count;

    public override bool IsSynchronized => _collection.IsSynchronized;

    public override object SyncRoot => _collection.SyncRoot;

    public override IAppender this[int i]
    {
      get => _collection[i];
      set => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
    }

    public override int Add(IAppender x)
      => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void Clear()
      => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override bool Contains(IAppender x) => _collection.Contains(x);

    public override int IndexOf(IAppender x) => _collection.IndexOf(x);

    public override void Insert(int pos, IAppender x)
      => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void Remove(IAppender x)
      => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override void RemoveAt(int pos)
      => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override bool IsFixedSize => true;

    public override bool IsReadOnly => true;

    public override IEnumerator<IAppender> GetEnumerator() => _collection.GetEnumerator();

    public override int Capacity
    {
      get => _collection.Capacity;
      set => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
    }

    public override int AddRange(AppenderCollection x)
      => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override int AddRange(IAppender[] x)
      => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();

    public override IAppender[] ToArray() => _collection.ToArray();

    public override void TrimToSize() => throw SystemInfo.CreateReadOnlyCollectionNotModifiableException();
  }
}
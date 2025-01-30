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
using System.Collections.ObjectModel;
using System.Linq;

namespace log4net.Appender;

/// <summary>
/// A strongly-typed collection of <see cref="IAppender"/> objects.
/// </summary>
/// <author>Nicko Cadell</author>
public class AppenderCollection : Collection<IAppender>, IList, ICloneable, ICollection<IAppender>
{
  /// <summary>
  /// Supports type-safe iteration over a <see cref="AppenderCollection"/>.
  /// </summary>
  [Obsolete("Use IEnumerator<IAppender> instead of AppenderCollection.IAppenderCollectionEnumerator")]
  public interface IAppenderCollectionEnumerator : IEnumerator<IAppender>
  { }

  private const int DefaultCapacity = 16;

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
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors")]
  // ReSharper disable once VirtualMemberCallInConstructor
  public AppenderCollection(int capacity) => Capacity = capacity;

  /// <summary>
  /// Initializes a new instance of the <see cref="AppenderCollection"/> class
  /// that contains elements copied from the specified <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="collection">The <see cref="AppenderCollection"/> whose elements are copied to the new collection.</param>
  public AppenderCollection(AppenderCollection collection)
    : base(collection)
  { }

  /// <summary>
  /// Initializes a new instance of the <see cref="AppenderCollection"/> class
  /// that contains elements copied from the specified <see cref="IAppender"/> array.
  /// </summary>
  /// <param name="array">The <see cref="IAppender"/> array whose elements are copied to the new list.</param>
  public AppenderCollection(IAppender[] array)
    : base(array.EnsureNotNull().ToList())
  { }

  /// <summary>
  /// Initializes a new instance of the <see cref="AppenderCollection"/> class
  /// that contains elements copied from the specified <see cref="IAppender"/> collection.
  /// </summary>
  /// <param name="collection">The <see cref="IAppender"/> collection whose elements are copied to the new list.</param>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors")]
  public AppenderCollection(ICollection collection)
  {
    // ReSharper disable once VirtualMemberCallInConstructor
    Capacity = collection.EnsureNotNull().Count;
    // ReSharper disable once VirtualMemberCallInConstructor
    AddRange(collection);
  }

  /// <summary>
  /// Initializes a new readonly instance of the <see cref="AppenderCollection"/> class
  /// that contains elements copied from the specified <see cref="IAppender"/> collection.
  /// </summary>
  /// <param name="collection">The <see cref="IAppender"/> collection whose elements are copied to the new list.</param>
  private AppenderCollection(ReadOnlyCollection<IAppender> collection)
    : base(collection)
  { }
  
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
  protected internal AppenderCollection(Tag _) 
  { }

  /// <summary>
  /// Copies the entire <see cref="AppenderCollection"/> to a one-dimensional
  /// <see cref="IAppender"/> array.
  /// </summary>
  /// <param name="array">The one-dimensional <see cref="IAppender"/> array to copy to.</param>
  public virtual void CopyTo(IAppender[] array) => CopyTo(array, 0);

  /// <summary>
  /// Gets a value indicating whether access to the collection is synchronized (thread-safe).
  /// </summary>
  /// <returns>false, because the backing type is an array, which is never thread-safe.</returns>
  public virtual bool IsSynchronized => false;

  /// <summary>
  /// Gets an object that can be used to synchronize access to the collection.
  /// </summary>
  public virtual object SyncRoot => ((ICollection)this).SyncRoot;

  /// <summary>
  /// Creates a shallow copy of the <see cref="AppenderCollection"/>.
  /// </summary>
  /// <returns>A new <see cref="AppenderCollection"/> with a shallow copy of the collection data.</returns>
  public virtual object Clone()
  {
    AppenderCollection newCol = new(this);
    return newCol;
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
  /// Gets or sets the number of elements the <see cref="AppenderCollection"/> can contain.
  /// </summary>
  public virtual int Capacity
  {
    get => (Items as List<IAppender>)?.Capacity ?? (Items as AppenderCollection)?.Capacity ?? Items.Count;
    set
    {
      if (value < Count)
      {
        value = Count;
      }

      if (value == Capacity)
      {
        return;
      }
      if (Items is List<IAppender> list)
      {
        list.Capacity = value;
      }
      else
      {
        Items.EnsureIs<AppenderCollection>().Capacity = value;
      }
    }
  }
  
  /// <summary>
  /// Adds the elements of another <see cref="AppenderCollection"/> to the current <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="collection">The <see cref="AppenderCollection"/> whose elements should be added to the end of the current <see cref="AppenderCollection"/>.</param>
  /// <returns>The new <see cref="Collection{T}.Count"/> of the <see cref="AppenderCollection"/>.</returns>
  public virtual int AddRange(AppenderCollection collection)
  {
    EnsureCapacity(Count + collection.EnsureNotNull().Count);
    foreach (IAppender appender in collection)
    {
      Add(appender);
    }
    return Count;
  }

  /// <summary>
  /// Adds the elements of a <see cref="IAppender"/> array to the current <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="array">The <see cref="IAppender"/> array whose elements should be added to the end of the <see cref="AppenderCollection"/>.</param>
  /// <returns>The new <see cref="Collection{T}.Count"/> of the <see cref="AppenderCollection"/>.</returns>
  public virtual int AddRange(IAppender[] array)
  {
    EnsureCapacity(Count + array.EnsureNotNull().Length);
    foreach (IAppender appender in array)
    {
      Add(appender);
    }
    return Count;
  }

  /// <summary>
  /// Adds the elements of a <see cref="IAppender"/> collection to the current <see cref="AppenderCollection"/>.
  /// </summary>
  /// <param name="collection">The <see cref="IAppender"/> collection whose elements should be added to the end of the <see cref="AppenderCollection"/>.</param>
  /// <returns>The new <see cref="Collection{T}.Count"/> of the <see cref="AppenderCollection"/>.</returns>
  public virtual int AddRange(ICollection collection)
  {
    EnsureCapacity(Count + collection.EnsureNotNull().Count);
    foreach (object item in collection)
    {
      Add(item.EnsureIs<IAppender>());
    }
    return Count;
  }

  /// <summary>
  /// Sets the capacity to the actual number of elements.
  /// </summary>
  public virtual void TrimToSize() => Capacity = Count;

  /// <summary>
  /// Return the collection elements as an array
  /// </summary>
  /// <returns>the array</returns>
  public virtual IAppender[] ToArray() => Enumerable.ToArray(this);

  private void EnsureCapacity(int min)
  {
    int newCapacity = (Capacity == 0) ? DefaultCapacity : Capacity * 2;
    if (newCapacity < min)
    {
      newCapacity = min;
    }

    Capacity = newCapacity;
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  /// <exclude/>
  private sealed class ReadOnlyAppenderCollection : AppenderCollection, ICollection
  {
    internal ReadOnlyAppenderCollection(AppenderCollection list)
      : base(new ReadOnlyCollection<IAppender>(list))
    { }
  }
}
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

using System.Text;
using System.IO;
using System.Collections;

using log4net.Repository;

namespace log4net.Util;

/// <summary>
/// Abstract class that provides the formatting functionality that 
/// derived classes need.
/// </summary>
/// <remarks>
/// <para>
/// Conversion specifiers in a conversion patterns are parsed to
/// individual PatternConverters. Each of which is responsible for
/// converting a logging event in a converter specific manner.
/// </para>
/// </remarks>
/// <author>Nicko Cadell</author>
/// <author>Gert Driesen</author>
public abstract class PatternConverter
{
  /// <summary>
  /// Protected constructor
  /// </summary>
  /// <remarks>
  /// <para>
  /// Initializes a new instance of the <see cref="PatternConverter" /> class.
  /// </para>
  /// </remarks>
  protected PatternConverter()
  { }

  /// <summary>
  /// Gets the next pattern converter in the chain.
  /// </summary>
  public virtual PatternConverter? Next => _next;

  /// <summary>
  /// Gets or sets the formatting info for this converter
  /// </summary>
  /// <value>
  /// The formatting info for this converter
  /// </value>
  /// <remarks>
  /// <para>
  /// Gets or sets the formatting info for this converter
  /// </para>
  /// </remarks>
  public virtual FormattingInfo FormattingInfo
  {
    get => new(_min, _max, _leftAlign);
    set
    {
      _min = value.EnsureNotNull().Min;
      _max = value.Max;
      _leftAlign = value.LeftAlign;
    }
  }

  /// <summary>
  /// Gets or sets the option value for this converter
  /// </summary>
  /// <summary>
  /// The option for this converter
  /// </summary>
  /// <remarks>
  /// <para>
  /// Gets or sets the option value for this converter
  /// </para>
  /// </remarks>
  public virtual string? Option { get; set; }

  /// <summary>
  /// Evaluate this pattern converter and write the output to a writer.
  /// </summary>
  /// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
  /// <param name="state">The state object on which the pattern converter should be executed.</param>
  /// <remarks>
  /// <para>
  /// Derived pattern converters must override this method in order to
  /// convert conversion specifiers in the appropriate way.
  /// </para>
  /// </remarks>
  public abstract void Convert(TextWriter writer, object? state);

  /// <summary>
  /// Set the next pattern converter in the chains
  /// </summary>
  /// <param name="patternConverter">the pattern converter that should follow this converter in the chain</param>
  /// <returns>the next converter</returns>
  /// <remarks>
  /// <para>
  /// The PatternConverter can merge with its neighbor during this method (or a subclass).
  /// Therefore the return value may or may not be the value of the argument passed in.
  /// </para>
  /// </remarks>
  public virtual PatternConverter SetNext(PatternConverter patternConverter)
  {
    _next = patternConverter;
    return _next;
  }

  /// <summary>
  /// Write the pattern converter to the writer with appropriate formatting
  /// </summary>
  /// <param name="writer"><see cref="TextWriter" /> that will receive the formatted result.</param>
  /// <param name="state">The state object on which the pattern converter should be executed.</param>
  /// <remarks>
  /// <para>
  /// This method calls <see cref="Convert"/> to allow the subclass to perform
  /// appropriate conversion of the pattern converter. If formatting options have
  /// been specified via the <see cref="FormattingInfo"/> then this method will
  /// apply those formattings before writing the output.
  /// </para>
  /// </remarks>
  public virtual void Format(TextWriter writer, object? state)
  {
    writer.EnsureNotNull();
    if (_min < 0 && _max == int.MaxValue)
    {
      // Formatting options are not in use
      Convert(writer, state);
    }
    else
    {
      string? msg;
      int len;
      lock (_syncRoot)
      {
        _formatWriter.Reset(MaxRenderBufferCapacity, DefaultRenderBufferSize);

        Convert(_formatWriter, state);

        StringBuilder buf = _formatWriter.GetStringBuilder();
        len = buf.Length;
        if (len > _max)
        {
          msg = buf.ToString(len - _max, _max);
          len = _max;
        }
        else
        {
          msg = buf.ToString();
        }
      }

      if (len < _min)
      {
        if (_leftAlign)
        {
          writer.Write(msg);
          SpacePad(writer, _min - len);
        }
        else
        {
          SpacePad(writer, _min - len);
          writer.Write(msg);
        }
      }
      else
      {
        writer.Write(msg);
      }
    }
  }

  private static readonly string[] _spaces =
  [
    // 1,2,4,8 spaces
    " ", "  ", "    ", "        ",      
    
    // 16 spaces
    "                ",

    // 32 spaces
    "                                "
  ];

  /// <summary>
  /// Fast space padding method.
  /// </summary>
  /// <param name="writer"><see cref="TextWriter" /> to which the spaces will be appended.</param>
  /// <param name="length">The number of spaces to be padded.</param>
  /// <remarks>
  /// <para>
  /// Fast space padding method.
  /// </para>
  /// </remarks>
  protected static void SpacePad(TextWriter writer, int length)
  {
    writer.EnsureNotNull();
    while (length >= 32)
    {
      writer.Write(_spaces[5]);
      length -= 32;
    }

    for (int i = 4; i >= 0; i--)
    {
      if ((length & (1 << i)) != 0)
      {
        writer.Write(_spaces[i]);
      }
    }
  }

  private PatternConverter? _next;
  private int _min = -1;
  private int _max = int.MaxValue;
  private bool _leftAlign;

  private readonly object _syncRoot = new();
  private readonly ReusableStringWriter _formatWriter = new(System.Globalization.CultureInfo.InvariantCulture);

  /// <summary>
  /// Initial buffer size
  /// </summary>
  private const int DefaultRenderBufferSize = 256;

  /// <summary>
  /// Maximum buffer size before it is recycled
  /// </summary>
  private const int MaxRenderBufferCapacity = 1024;

  /// <summary>
  /// Write an dictionary to a <see cref="TextWriter"/>
  /// </summary>
  /// <param name="writer">the writer to write to</param>
  /// <param name="repository">a <see cref="ILoggerRepository"/> to use for object conversion</param>
  /// <param name="value">the value to write to the writer</param>
  /// <remarks>
  /// <para>
  /// Writes the <see cref="IDictionary"/> to a writer in the form:
  /// </para>
  /// <code>
  /// {key1=value1, key2=value2, key3=value3}
  /// </code>
  /// <para>
  /// If the <see cref="ILoggerRepository"/> specified
  /// is not null then it is used to render the key and value to text, otherwise
  /// the object's ToString method is called.
  /// </para>
  /// </remarks>
  protected static void WriteDictionary(TextWriter writer, ILoggerRepository? repository, IDictionary value)
    => WriteDictionary(writer, repository, value.EnsureNotNull().GetEnumerator());

  /// <summary>
  /// Writes a dictionary to a <see cref="TextWriter"/>
  /// </summary>
  /// <param name="writer">the writer to write to</param>
  /// <param name="repository">a <see cref="ILoggerRepository"/> to use for object conversion</param>
  /// <param name="value">the value to write to the writer</param>
  /// <remarks>
  /// <para>
  /// Writes the <see cref="IDictionaryEnumerator"/> to a writer in the form:
  /// </para>
  /// <code>
  /// {key1=value1, key2=value2, key3=value3}
  /// </code>
  /// <para>
  /// If the <see cref="ILoggerRepository"/> specified
  /// is not null then it is used to render the key and value to text, otherwise
  /// the object's ToString method is called.
  /// </para>
  /// </remarks>
  protected static void WriteDictionary(TextWriter writer, ILoggerRepository? repository, IDictionaryEnumerator value)
  {
    value.EnsureNotNull();
    writer.EnsureNotNull().Write("{");
    bool first = true;

    // Write out all the dictionary key value pairs
    while (value.MoveNext())
    {
      if (first)
      {
        first = false;
      }
      else
      {
        writer.Write(", ");
      }
      WriteObject(writer, repository, value.Key);
      writer.Write("=");
      WriteObject(writer, repository, value.Value);
    }

    writer.Write("}");
  }

  /// <summary>
  /// Write an object to a <see cref="TextWriter"/>
  /// </summary>
  /// <param name="writer">the writer to write to</param>
  /// <param name="repository">a <see cref="ILoggerRepository"/> to use for object conversion</param>
  /// <param name="value">the value to write to the writer</param>
  /// <remarks>
  /// <para>
  /// Writes the Object to a writer. If the <see cref="ILoggerRepository"/> specified
  /// is not null then it is used to render the object to text, otherwise
  /// the object's ToString method is called.
  /// </para>
  /// </remarks>
  protected static void WriteObject(TextWriter writer, ILoggerRepository? repository, object? value)
  {
    writer.EnsureNotNull();
    if (repository is not null)
    {
      repository.RendererMap.FindAndRender(value, writer);
    }
    else
    {
      // Don't have a repository to render with so just have to rely on ToString
      if (value is null)
      {
        writer.Write(SystemInfo.NullText);
      }
      else
      {
        writer.Write(value.ToString());
      }
    }
  }

  /// <summary>
  /// 
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
  public PropertiesDictionary? Properties { get; set; }
}

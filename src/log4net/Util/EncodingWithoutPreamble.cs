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

namespace log4net.Util;

/// <summary>
/// Wrapper for an <see cref="Encoding"/>
/// </summary>
/// <remarks>acts like the wrapped encoding, but without a preamble</remarks>
internal sealed class EncodingWithoutPreamble : Encoding
{
  private readonly Encoding wrapped;

  /// <inheritdoc/>
  private EncodingWithoutPreamble(Encoding wrapped) => this.wrapped = wrapped.EnsureNotNull();

  /// <summary>
  /// wraps the <paramref name="encoding"/> in case it has a preamble
  /// </summary>
  /// <param name="encoding">Encoding to check</param>
  /// <returns>encoding without preamble</returns>
  internal static Encoding Get(Encoding encoding)
    => encoding.EnsureNotNull().GetPreamble()?.Length > 0
       ? new EncodingWithoutPreamble(encoding)
       : encoding;

  /// <inheritdoc/>
  public override string BodyName => wrapped.BodyName;

  /// <inheritdoc/>
  public override int CodePage => wrapped.CodePage;

  /// <inheritdoc/>
  public override string EncodingName => wrapped.EncodingName;

  /// <inheritdoc/>
  public override string HeaderName => wrapped.HeaderName;

  /// <inheritdoc/>
  public override bool IsBrowserDisplay => wrapped.IsBrowserDisplay;

  /// <inheritdoc/>
  public override bool IsBrowserSave => wrapped.IsBrowserSave;

  /// <inheritdoc/>
  public override bool IsMailNewsDisplay => wrapped.IsMailNewsDisplay;

  /// <inheritdoc/>
  public override bool IsMailNewsSave => wrapped.IsMailNewsSave;

  /// <inheritdoc/>
  public override bool IsSingleByte => wrapped.IsSingleByte;

  /// <inheritdoc/>
  public override string WebName => wrapped.WebName;

  /// <inheritdoc/>
  public override int WindowsCodePage => wrapped.WindowsCodePage;


  /// <inheritdoc/>
  public override object Clone() => new EncodingWithoutPreamble(wrapped.Clone().EnsureIs<Encoding>());

  /// <inheritdoc/>
  public override int GetByteCount(char[] chars, int index, int count)
    => wrapped.GetByteCount(chars, index, count);

  /// <inheritdoc/>
  public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    => wrapped.GetBytes(chars, charIndex, charCount, bytes, byteIndex);

  /// <inheritdoc/>
  public override int GetCharCount(byte[] bytes, int index, int count)
    => wrapped.GetCharCount(bytes, index, count);

  /// <inheritdoc/>
  public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    => wrapped.GetChars(bytes, byteIndex, byteCount, chars, charIndex);

  /// <inheritdoc/>
  public override int GetMaxByteCount(int charCount) => wrapped.GetMaxByteCount(charCount);

  /// <inheritdoc/>
  public override int GetMaxCharCount(int byteCount) => wrapped.GetMaxCharCount(byteCount);

  /// <inheritdoc/>
  public override Decoder GetDecoder() => wrapped.GetDecoder();

  /// <inheritdoc/>
  public override Encoder GetEncoder() => wrapped.GetEncoder();

  /// <inheritdoc/>
  public override bool IsAlwaysNormalized(NormalizationForm form) => wrapped.IsAlwaysNormalized(form);

  /// <inheritdoc/>
  public override bool Equals(object value) => wrapped.Equals(value);

  /// <inheritdoc/>
  public override int GetHashCode() => wrapped.GetHashCode();

  /// <inheritdoc/>
  public override string ToString() => $"{wrapped}-WithoutPreamble";
}
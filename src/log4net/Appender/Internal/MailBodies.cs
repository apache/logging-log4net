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

using System.Collections.Generic;
using System.Text;

namespace log4net.Appender.Internal;

/// <summary>
/// Splits the rendered records of one batch into mail bodies that each fit a size budget.
/// </summary>
internal static class MailBodies
{
  /// <summary>
  /// The smallest budget an operator may configure.
  /// </summary>
  internal const int MinBodySize = 4096;

  /// <summary>
  /// The default budget, 1 MiB.
  /// </summary>
  internal const int DefaultBodySize = 1_048_576;

  /// <summary>
  /// Packs the records into as few bodies as fit <paramref name="maxBodySize"/>, each with header and footer.
  /// </summary>
  /// <param name="header">The layout's header, or <see langword="null"/>.</param>
  /// <param name="records">The rendered records, in order.</param>
  /// <param name="footer">The layout's footer, or <see langword="null"/>.</param>
  /// <param name="encoding">The encoding the body goes out in, with the escaping fallback.</param>
  /// <param name="maxBodySize">The budget per body in bytes, header and footer included.</param>
  /// <param name="cutRecords">How many records were cut to fit a body on their own.</param>
  /// <returns>At least one body.</returns>
  /// <remarks>
  /// A server refuses an oversize mail as a whole, so the budget is kept per mail rather than per
  /// batch. A record that does not fit even alone is cut, down to the marker when header and footer
  /// leave no room.
  /// </remarks>
  internal static List<string> Split(string? header, IReadOnlyList<string> records, string? footer,
    Encoding encoding, int maxBodySize, out int cutRecords)
  {
    header ??= string.Empty;
    footer ??= string.Empty;
    int budget = maxBodySize - encoding.GetByteCount(header) - encoding.GetByteCount(footer);
    List<string> bodies = [];
    StringBuilder body = new(header);
    int used = 0;
    bool hasRecords = false;
    cutRecords = 0;

    foreach (string rendered in records)
    {
      string record = rendered;
      int length = encoding.GetByteCount(record);
      if (length > budget)
      {
        record = record.TruncateToBytes(encoding, budget, out length);
        cutRecords++;
      }

      if (hasRecords && used + length > budget)
      {
        bodies.Add(body.Append(footer).ToString());
        body.Clear().Append(header);
        used = 0;
      }

      body.Append(record);
      used += length;
      hasRecords = true;
    }

    bodies.Add(body.Append(footer).ToString());
    return bodies;
  }

  /// <summary>
  /// Tells whether header, footer and the truncation marker leave room for any record text.
  /// </summary>
  /// <param name="header">The layout's header, or <see langword="null"/>.</param>
  /// <param name="footer">The layout's footer, or <see langword="null"/>.</param>
  /// <param name="encoding">The encoding the body goes out in.</param>
  /// <param name="maxBodySize">The budget per body in bytes.</param>
  /// <returns><see langword="true"/> when a record can carry at least some of its text.</returns>
  internal static bool LeavesRoomForRecords(string? header, string? footer, Encoding encoding, int maxBodySize)
    => encoding.GetByteCount(header ?? string.Empty) + encoding.GetByteCount(footer ?? string.Empty)
      + encoding.GetByteCount(ContentEscape.TruncationMarker) < maxBodySize;
}

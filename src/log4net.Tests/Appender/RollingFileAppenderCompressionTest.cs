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

using System.IO;
using System.IO.Compression;
using log4net.Appender;
using NUnit.Framework;

namespace log4net.Tests.Appender;

/// <summary>
/// Used for internal unit testing the compression feature of <see cref="RollingFileAppender"/> class.
/// </summary>
[TestFixture]
public class RollingFileAppenderCompressionTest
{
  /// <summary>
  /// Test Zip-Compression
  /// </summary>
  [Test]
  public void CompressZipFile()
  {
    FileInfo file = new("test.zip");
    using FileStream zipFile = file.Create();
    using ZipArchive archive = new(zipFile, ZipArchiveMode.Create);
    ZipArchiveEntry entry = archive.CreateEntry("test.log");
    using Stream stream = entry.Open();
    for (int i = 0; i < 10_000; i++)
      stream.WriteByte((byte)'A');
  }

  /// <summary>
  /// Test GZip-Compression
  /// </summary>
  [Test]
  public void CompressGZipFile()
  {
    FileInfo file = new("test.gz");
    using FileStream zipFile = file.Create();
    using GZipStream archive = new(zipFile, CompressionMode.Compress);
    for (int i = 0; i < 10_000; i++)
      archive.WriteByte((byte)'A');
  }
}
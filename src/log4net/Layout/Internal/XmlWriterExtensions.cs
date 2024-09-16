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
using System.Xml;
using log4net.Util;

namespace log4net.Layout.Internal;

/// <summary>
/// Extensions for <see cref="XmlWriter"/>
/// </summary>
/// <author>Jan Friedrich</author>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter",
  Justification = "Compatibility between net4 and netstandard")]
internal static partial class XmlWriterExtensions
{
#if NETSTANDARD2_0_OR_GREATER
  private static readonly XmlWriterSettings settings = new XmlWriterSettings
  {
    Indent = false,
    OmitXmlDeclaration = true
  };
#endif

  /// <summary>
  /// writes the specified start tag and associates it with the given namespace and prefix
  /// </summary>
  /// <param name="writer">Writer</param>
  /// <param name="fullName">The full name of the element</param>
  /// <param name="prefix">The namespace prefix of the element</param>
  /// <param name="localName">The local name of the element</param>
  /// <param name="ns">The namespace URI to associate with the element</param>
  internal static void WriteStartElement(this XmlWriter writer,
    string fullName, string prefix, string localName, string ns)
#if NETSTANDARD2_0_OR_GREATER
    => writer.WriteStartElement(prefix, localName, ns);
#else
    => writer.WriteStartElement(fullName);
#endif


  /// <summary>
  /// Creates an XmlWriter
  /// </summary>
  /// <param name="writer">TextWriter</param>
  /// <returns>XmlWriter</returns>
  internal static XmlWriter CreateXmlWriter(TextWriter writer)
#if NETSTANDARD2_0_OR_GREATER
    => XmlWriter.Create(new ProtectCloseTextWriter(writer), settings);
#else
    => new XmlTextWriter(new ProtectCloseTextWriter(writer))
    {
      Formatting = Formatting.None,
      Namespaces = false
    };
#endif
}
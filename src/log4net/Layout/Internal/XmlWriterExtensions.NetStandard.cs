using System.IO;
using System.Xml;
using log4net.Util;

namespace log4net.Layout.Internal;

partial class XmlWriterExtensions
{
  private static readonly XmlWriterSettings settings = new XmlWriterSettings
  {
    Indent = false,
    OmitXmlDeclaration = true
  };

  /// <summary>
  /// writes the specified start tag and associates it with the given namespace and prefix
  /// </summary>
  /// <param name="writer">Writer</param>
  /// <param name="fullName">The full name of the element (ignored for netstandard)</param>
  /// <param name="prefix">The namespace prefix of the element</param>
  /// <param name="localName">The local name of the element</param>
  /// <param name="ns">The namespace URI to associate with the element</param>
  internal static void WriteStartElement(this XmlWriter writer,
    string fullName, string prefix, string localName, string ns)
    => writer.WriteStartElement(prefix, localName, ns);

  /// <summary>
  /// Creates an XmlWriter
  /// </summary>
  /// <param name="writer">TextWriter</param>
  /// <returns>XmlWriter</returns>
  internal static XmlWriter CreateXmlWriter(TextWriter writer)
    => XmlWriter.Create(new ProtectCloseTextWriter(writer), settings);
}
using System.IO;
using System.Xml;
using log4net.Util;

namespace log4net.Layout.Internal;

partial class XmlWriterExtensions
{
  /// <summary>
  /// Writes the specified start tag and associates it with the given namespace and prefix
  /// </summary>
  /// <param name="writer">Writer</param>
  /// <param name="fullName">The full name of the element</param>
  /// <param name="prefix">The namespace prefix of the element (ignored for net4x)</param>
  /// <param name="localName">The local name of the element (ignored for net4x)</param>
  /// <param name="ns">The namespace URI to associate with the element (ignored for net4x)</param>
  internal static void WriteStartElement(this XmlWriter writer,
    string fullName, string prefix, string localName, string ns)
    => writer.WriteStartElement(fullName);

  /// <summary>
  /// Creates an XmlWriter
  /// </summary>
  /// <param name="writer">TextWriter</param>
  /// <returns>XmlWriter</returns>
  internal static XmlWriter CreateXmlWriter(TextWriter writer)
    => new XmlTextWriter(new ProtectCloseTextWriter(writer))
    {
      Formatting = Formatting.None,
      Namespaces = false
    };
}

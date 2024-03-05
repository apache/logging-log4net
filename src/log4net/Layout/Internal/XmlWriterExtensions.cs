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
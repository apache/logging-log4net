using System;
using System.IO;
using System.Reflection;
using System.Xml;
using log4net;
using log4net.Config;
using log4net.Core;

if (true)
{
    var appPath = new Uri(Assembly.GetExecutingAssembly().Location).LocalPath;
    var appFolder = Path.GetDirectoryName(appPath);
    if (appFolder is null)
    {
        throw new InvalidOperationException(
            $"Can't determine app folder for {appPath}"
        );
    }

    var configFile = Path.Combine(appFolder, "log4net.config");
    if (!File.Exists(configFile))
    {
        throw new InvalidOperationException($"log4net.config not found at {configFile}");
    }

    var info = new FileInfo(configFile);

    XmlConfigurator.Configure(info);

    var logger = LogManager.GetLogger("main");

    for (var i = 0; i < 10; i++)
    {
        logger.Info($"test log {i}");
    }

    LogManager.Flush(int.MaxValue);
}

// Sample.Main();
//
// public class Sample
// {
//     private const string filename = "sampledata.xml";
//
//     public static void Main()
//     {
//
//         XmlTextWriter writer = new XmlTextWriter (filename, null);
//         //Use indenting for readability.
//         writer.Formatting = Formatting.Indented;
//
//         writer.WriteComment("sample XML fragment");
//
//         //Write an element (this one is the root).
//         writer.WriteStartElement("bookstore");
//
//         //Write the namespace declaration.
//         writer.WriteAttributeString("xmlns", "bk", null, "log4net");
//
//         writer.WriteStartElement("book");
//
//         //Lookup the prefix and then write the ISBN attribute.
//         string prefix = writer.LookupPrefix("urn:samples");
//         writer.WriteStartAttribute(prefix, "ISBN", "urn:samples");
//         writer.WriteString("1-861003-78");
//         writer.WriteEndAttribute();
//
//         //Write the title.
//         writer.WriteStartElement("title");
//         writer.WriteString("The Handmaid's Tale");
//         writer.WriteEndElement();
//
//         //Write the price.
//         writer.WriteElementString("price", "19.95");
//
//         //Write the style element.
//         writer.WriteStartElement(prefix, "style", "urn:samples");
//         writer.WriteString("hardcover");
//         writer.WriteEndElement();
//
//         //Write the end tag for the book element.
//         writer.WriteEndElement();
//
//         //Write the close tag for the root element.
//         writer.WriteEndElement();
//
//         //Write the XML to file and close the writer.
//         writer.Flush();
//         writer.Close();
//
//         //Read the file back in and parse to ensure well formed XML.
//         XmlDocument doc = new XmlDocument();
//         //Preserve white space for readability.
//         doc.PreserveWhitespace = true;
//         //Load the file
//         doc.Load(filename);
//
//         //Write the XML content to the console.
//         Console.Write(doc.InnerXml);
//     }
// }

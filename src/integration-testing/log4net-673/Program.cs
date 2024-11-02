using System;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;
using log4net.Config;
using log4net.Repository;

string appPath = new Uri(Assembly.GetExecutingAssembly().Location).LocalPath;
string appFolder = Path.GetDirectoryName(appPath);
// force loading the assembly, otherwise the appender type isn't found later
if (appFolder is null)
{
  throw new InvalidOperationException("Can't find myself");
}

string configFile = Path.Combine(appFolder, "log4net.config");
if (!File.Exists(configFile))
{
  throw new InvalidOperationException($"log4net.config not found at {configFile}");
}

if (Directory.Exists("log"))
{
  Console.WriteLine("Clearing out old logs...");
  foreach (string file in Directory.EnumerateFiles("log"))
  {
    File.Delete(file);
  }
}

FileInfo info = new FileInfo(configFile);
ILoggerRepository logRepo = LogManager.GetRepository(Assembly.GetExecutingAssembly());
XmlConfigurator.ConfigureAndWatch(logRepo, info);

ILog logger = LogManager.GetLogger(typeof(Program));

Console.WriteLine("logging...");
for (int i = 0; i < 10; i++)
{
  logger.Info($"test log {i}");
  logger.Error($"error log {i}");
  logger.Warn($"warning log {i}");
}


foreach (string file in Directory.EnumerateFiles("log"))
{
  Console.WriteLine($"log file: {file}");
  TryDumpFile(file);
}

static void TryDumpFile(string at)
{
  if (!File.Exists(at))
  {
    Console.WriteLine($"File not found: {at}");
    return;
  }

  for (int i = 0; i < 10; i++)
  {
    try
    {
      Console.WriteLine(File.ReadAllText(at));
      return;
    }
    catch(IOException)
    {
      Thread.Sleep(100);
    }
  }

  Console.WriteLine($"Unable to read file at {at}");
}
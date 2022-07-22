// See https://aka.ms/new-console-template for more information

using System.Reflection;
using log4net;
using log4net.Config;
using log4net_611_lib;

var appPath = new Uri(Assembly.GetExecutingAssembly().Location).LocalPath;
var appFolder = Path.GetDirectoryName(appPath);
// force loading the assembly, otherwise the appender type isn't found later
Assembly.LoadFile("log4net-611-lib.dll");
if (appFolder is null)
{
    throw new InvalidOperationException("Can't find myself");
}

Assembly.LoadFile(Path.Combine(appFolder, "log4net-611-lib.dll"));

var configFile = Path.Combine(appFolder, "log4net.config");
if (!File.Exists(configFile))
{
    throw new InvalidOperationException($"log4net.config not found at {configFile}");
}

if (Directory.Exists("Logs"))
{
    Console.WriteLine("Clearing out old logs...");
    foreach (var file in Directory.EnumerateFiles("Logs"))
    {
        File.Delete(file);
    }
}

var info = new FileInfo(configFile);
var logRepo = LogManager.GetRepository(Assembly.GetExecutingAssembly());
XmlConfigurator.ConfigureAndWatch(
    logRepo,
    info
);

var logger = LogManager.GetLogger(typeof(Program));

Console.WriteLine("logging...");
for (var i = 0; i < 10; i++)
{
    logger.Info($"test log {i}");
}


foreach (var file in Directory.EnumerateFiles("Logs"))
{
    Console.WriteLine($"log file: {file}");
    Console.WriteLine(File.ReadAllText(file));
}
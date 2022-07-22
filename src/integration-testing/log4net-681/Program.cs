using System.Reflection;
using log4net;
using log4net.Config;

var appPath = new Uri(Assembly.GetExecutingAssembly().Location).LocalPath;
var appFolder = Path.GetDirectoryName(appPath);

if (appFolder is null)
{
    throw new InvalidOperationException("Can't find myself");
}

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
var threads = new List<Thread>();
for (var i = 0; i < 128; i++)
{
    var thread = new Thread(LogABit);
    thread.Start();
    threads.Add(thread);
}

foreach (var t in threads)
{
    t.Join();
}

foreach (var file in Directory.EnumerateFiles("Logs"))
{
    Console.WriteLine($"found log file: {file}");
}

void LogABit()
{
    for (var i = 0; i < 100; i++)
    {
        logger.Info($"test log {i}");
    }
}
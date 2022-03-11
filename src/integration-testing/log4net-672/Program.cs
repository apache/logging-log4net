using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Config;

const int NO_ERROR = 0;
const int MISSING_LOGS = 1;
const int OVERWRITTEN_LOGS = 2;

var appPath = new Uri(Assembly.GetExecutingAssembly().Location).LocalPath;
var appFolder = Path.GetDirectoryName(appPath);
if (appFolder is null)
{
    throw new InvalidOperationException(
        $"Can't determine app folder for {appPath}"
    );
}

var logFolder = Path.Combine(appFolder, "Logs");
if (Directory.Exists(logFolder))
{
    Directory.Delete(logFolder, recursive: true);
}

var configFile = Path.Combine(appFolder, "log4net.config");
if (!File.Exists(configFile))
{
    throw new InvalidOperationException($"log4net.config not found at {configFile}");
}

var logCount = 10;
var identifiers = new List<Guid>();
for (var i = 0; i < 10; i++)
{
    var identifier = Guid.NewGuid();
    identifiers.Add(identifier);
    var logged = LogWith(identifier, logCount);
    if (logged != logCount)
    {
        Die($"Missing logs immediately for '{identifier}' - found {logged}/{logCount}", MISSING_LOGS);
    }
}

foreach (var identifier in identifiers)
{
    var logged = CountIdentifierInLogs(identifier);
    if (logged != logCount)
    {
        Die($"Logs have been overwritten for '{identifier}' - found {logged}/{logCount}", OVERWRITTEN_LOGS);
    }
}

Console.WriteLine("All good: LOG4NET-672 is resolved");
return NO_ERROR;

void Die(string message, int exitCode)
{
    Console.Error.WriteLine(message);
    Environment.Exit(exitCode);
}

int CountIdentifierInLogs(Guid id)
{
    return Directory.EnumerateFiles("Logs").Select(
        filePath => CountIdentifierInFile(id, filePath)
    ).Sum();
}

int CountIdentifierInFile(Guid id, string filePath)
{
    var contents = File.ReadAllLines(filePath);
    return contents.Count(line => line.Contains(id.ToString()));
}

int LogWith(Guid identifier, int howManyLogs)
{
    var info = new FileInfo(configFile);
    XmlConfigurator.Configure(info);
    var logger = LogManager.GetLogger("main");

    for (var i = 0; i < howManyLogs; i++)
    {
        logger.Info($"test log {i} [{identifier}]");
    }

    LogManager.Flush(int.MaxValue);
    return CountIdentifierInLogs(identifier);
}
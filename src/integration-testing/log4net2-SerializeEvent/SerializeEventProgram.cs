using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using log4net.Core;
using log4net.Util;

// Serialize a LoggingEvent in 2.x format using a JSON formatter.
// This written file is read back and verified in src\log4net.Tests\Core\LoggingEventTest.cs.

// Note to avoid the need to run this program during testing,
// the output of a manual run is cached in this directory.
// If for some reason a new serialized file is needed, run this program and
// commit the result over the cached version.

var localTimestamp = new DateTime(2000, 7, 1).ToLocalTime();

var stackTrace = new StackTrace(true);

var log4net2Event = new LoggingEvent(new LoggingEventData
{
  // Deliberate use of obsolete local timestamp.
#pragma warning disable CS0618 // Type or member is obsolete
  TimeStamp = localTimestamp,
#pragma warning restore CS0618 // Type or member is obsolete

  LoggerName = "aLogger",
  Level = Level.Log4Net_Debug,
  Message = "aMessage",
  ThreadName = "aThread",
  LocationInfo = new LocationInfo(typeof(Program)),
  UserName = "aUser",
  Identity = "anIdentity",
  ExceptionString = "anException",
  Domain = "aDomain",
  Properties = new PropertiesDictionary { ["foo"] = "bar" },
});

log4net2Event.Fix = FixFlags.All;

using var stream = File.OpenWrite("SerializeV2Event.dat");
var formatter = new BinaryFormatter();
formatter.Serialize(stream, log4net2Event);

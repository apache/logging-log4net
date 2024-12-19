////
    Licensed to the Apache Software Foundation (ASF) under one or more
    contributor license agreements.  See the NOTICE file distributed with
    this work for additional information regarding copyright ownership.
    The ASF licenses this file to You under the Apache License, Version 2.0
    (the "License"); you may not use this file except in compliance with
    the License.  You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
////

////
    ██     ██  █████  ██████  ███    ██ ██ ███    ██  ██████  ██
    ██     ██ ██   ██ ██   ██ ████   ██ ██ ████   ██ ██       ██
    ██  █  ██ ███████ ██████  ██ ██  ██ ██ ██ ██  ██ ██   ███ ██
    ██ ███ ██ ██   ██ ██   ██ ██  ██ ██ ██ ██  ██ ██ ██    ██
     ███ ███  ██   ██ ██   ██ ██   ████ ██ ██   ████  ██████  ██

    IF THIS FILE DOESN'T HAVE A `.ftl` SUFFIX, IT IS AUTO-GENERATED, DO NOT EDIT IT!

    Version-specific release notes (`7.8.0.adoc`, etc.) are generated from `src/changelog/*/.release-notes.adoc.ftl`.
    Auto-generation happens during `generate-sources` phase of Maven.
    Hence, you must always

    1. Find and edit the associated `.release-notes.adoc.ftl`
    2. Run `./mvnw generate-sources`
    3. Commit both `.release-notes.adoc.ftl` and the generated `7.8.0.adoc`
////

[#release-notes-${release.version?replace("[^a-zA-Z0-9]", "-", "r")}]
== ${release.version}

<#if release.date?has_content>Release date:: ${release.date}</#if>

[#release-notes-3-0-0-breaking-changes]
=== Breaking changes

[IMPORTANT]
====
Starting with 3.0.0 we only support the following target frameworks

* net462
* netstandard2.0

The reasoning for this change can be found in https://github.com/apache/logging-log4net/issues/111[#111 - Dropping support for older runtimes]
====

[#release-notes-3-0-0-removed-obsolete]
==== Removed obsolete classes and members

[#release-notes-3-0-0-removed-obsolete-1.2.14]
===== since 1.2.14 (2015)

* log4net.Appender.BufferingAppenderSkeleton.OnlyFixPartialEventData
* log4net.Appender.ColoredConsoleAppender.ctor(ILayout)
* log4net.Appender.ColoredConsoleAppender.ctor(ILayout, bool)
* log4net.Appender.ConsoleAppender.ctor(ILayout)
* log4net.Appender.ConsoleAppender.ctor(ILayout, bool)
* log4net.Appender.DebugAppender.ctor(ILayout)
* log4net.Appender.EventLogAppender.ctor(ILayout)
* log4net.Appender.FileAppender.ctor(ILayout, string)
* log4net.Appender.FileAppender.ctor(ILayout, string, bool)
* log4net.Appender.MemoryAppender.OnlyFixPartialEventData
* log4net.Appender.SmtpAppender.LocationInfo
* log4net.Appender.TextWriterAppender.ctor(ILayout, Stream)
* log4net.Appender.TextWriterAppender.ctor(ILayout, TextWriter)
* log4net.Appender.TraceAppender.ctor(ILayout)
* log4net.Config.DOMConfigurator
* log4net.Config.AliasDomainAttribute
* log4net.Config.DomainAttribute
* log4net.Config.DOMConfiguratorAttribute
* log4net.Core.LoggerManager.GetLoggerRepository
* log4net.Core.LoggerManager.CreateDomain
* log4net.Core.LoggingEventData.TimeStamp
* log4net.Core.LoggingEvent.GetExceptionStrRep
* log4net.Core.LoggingEvent.FixVolatileData
* log4net.LogManager.GetLoggerRepository
* log4net.LogManager.CreateDomain

[#release-notes-3-0-0-removed-obsolete-2.0.6]
===== since 2.0.6 (2016)

* log4net.Util.SystemInfo.ProcessStartTime

[#release-notes-3-0-0-sealed]
==== Sealed classes - the following classes are now sealed

* log4net.Config.AliasRepositoryAttribute
* log4net.Config.RepositoryAttribute
* log4net.Config.XmlConfiguratorAttribute

<#include "../.changelog.adoc.ftl">
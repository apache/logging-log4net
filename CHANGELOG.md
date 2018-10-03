<!---
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
-->
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [2.1.0]

### Added

- All releases prior to this release did not track all notable changes to the project in this changelog file. With the upcoming release all notable changes will be added to this file.
- This project does now have a ci pipeline that builds log4net for all supported target frameworks and runs the tests against those target frameworks. The ci pipeline also picks up new branches and pull requests in an automated way.
- A security report page was added that provides a central place for security vulnerabilities.
- Validation of logger names was added to harden the code that evaluates logger names. Tracked by [LOG4NET-580](https://issues.apache.org/jira/browse/LOG4NET-580).
- Added a safety net of null checks to avoid `NullReferenceExceptions` in the `TypeNamePatternConverter`. Tracked by [LOG4NET-559](https://issues.apache.org/jira/browse/LOG4NET-559).

### Changed

- The `XmlConfigurator` does not longer allow dtd processing. This is a breaking change for applications that rely on dtd's to be processed when log4net loads the xml configuration file. Tracked by [LOG4NET-575](https://issues.apache.org/jira/browse/LOG4NET-575).
- The indentations are now all tabs in all project files. They used to be a mix of tabs, 4-spaces or 2-spaces. Editors should now pick up that new codestyle because an `.editorconfig` file was added.
- The `KEYS` file was changed to be in markdown format.
- The pgp key of `dpsenner@apache.org` was added to the `KEYS` file.
- Changed the documentation in the files `CONTRIBUTING.md` and `ReleaseInstructions.md` to be in markdown format.
- Changed the `CONTRIBUTING.md` and `ReleaseInstructions.md` to match closer the new reality.
- Changed the mailing lists in the site to reflect the new reality.
- Changed the site to look more like other apache logging service project sites. Tracked by [LOG4NET-563](https://issues.apache.org/jira/browse/LOG4NET-563).

### Fixed

- The xml documentation of the debug level was fixed.
- The rolling file appender does not work with exclusive file locks on linux and the tests do now reflect that fact.
- A regression in the `ReadOnlyPropertiesDictionary` was fixed. It caused `NullReferenceException`s at runtime. Thanks to Vlad Lee who kindly provided the patch. Tracked by [LOG4NET-581](https://issues.apache.org/jira/browse/LOG4NET-581).
- A regression in the `AdoNetAppender` caused the appender to not work with the postgresql database provider because the provider would not detect database parameter names if they are added after a `Prepare()` on a `DbCommand`. Tracked by [LOG4NET-538](https://issues.apache.org/jira/browse/LOG4NET-538).
- Fixed a typo in the website manuals where `log4net.LogicalThreadContext` was referenced as `log4net.ThreadLogicalContext`. Thanks to Marcel Gosselin who kindly provided the patch.
- The `RollingFileAppender` does now honor the `PreserveLogFileNameExtension` flag when rolling over files with `StaticLogFileName` set to `false`.

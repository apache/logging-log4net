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

[#release-notes-3-2-0-added]
=== Added

* Asynchronous Sending for RemoteSyslogAppender
** requested by @yogitasingh001 https://github.com/apache/logging-log4net/issues/255[#255]
** implemented by @yogitasingh001 (in https://github.com/apache/logging-log4net/pull/253[#253]) and @FreeAndNil (in https://github.com/apache/logging-log4net/pull/258[#258])
** Refactored the RemoteSyslogAppender to use an asynchronous, non-blocking logging model.
   Introduced a background worker pattern using `BlockingCollection&lt;byte[]&gt;` to decouple UDP log transmission from the calling thread.
   This eliminates thread blocking caused by `.Wait()` on `SendAsync()` and significantly improves performance under high-load conditions.
   A graceful shutdown ensures any buffered logs are flushed on appender closure.
   No changes to external behavior - maintains backward compatibility.

<#include "../.changelog.adoc.ftl">

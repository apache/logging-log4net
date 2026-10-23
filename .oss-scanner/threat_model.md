# Threat model: Apache log4net

## Read first

- `SECURITY.md` and `AGENTS.md` at the repository root (`/src`). `SECURITY.md` decides what is in
  scope and what is a vulnerability; `AGENTS.md` gives the code style and test conventions.
- The audit is offline. Where `SECURITY.md` says to fetch a document, read the copy taken when the
  image was built: `/opt/logging-security/_threat-model-common.adoc` (the threat model),
  `faq.adoc` (frequently reported non-issues) and `vdr.xml` (previously disclosed vulnerabilities).
- `SECURITY.md` addresses an agent working with a researcher. Read "inform the researcher" as
  "say so in the report".

The two rules most often got wrong: **configuration is trusted**, including the types, members and
converters it names, so a finding that needs control of it is out of scope. **Content is untrusted**
(messages, rendered parameters, exception text, context property values): it must never be
rejected, only escaped where the output format requires it; structural identifiers (logger names,
the syslog identity), by contrast, are trusted, and rejecting a malformed one is correct where
silently altering it is not.

## Components that matter most / least

Matters most:

- **Structured output** must stay well-formed whatever the content: `XmlLayout`,
  `XmlLayoutSchemaLog4j`, and the RFC 3164 framing built by `RemoteSyslogAppender`. A content
  character that breaks the structure of the document log4net itself emits is a defect we own (log
  injection, CWE-117).
- `LocalSyslogAppender` builds no framing; libc `syslog()` does. Content passed to it must not act
  as a format string (`EscapePercent`).
- `log4net.Appender.Internal.ContentEscape` and `RemoteSyslogAppender.AppendMessage`: the shared
  escapes are `\0` for NUL, `\r` and `\n`, and `\uXXXX` for anything else. A sink that cannot carry a
  character escapes it visibly; it never drops the character, the rest of the record, or the event.
- Anything where untrusted content reaches pattern converters, format expansion, or the appenders
  that send it on: `UdpAppender`, `RemoteSyslogAppender`, `TelnetAppender`, and the mail appenders,
  `SmtpAppender` and `SmtpPickupDirAppender` in core and `SmtpAppender` in `log4net.Ext.Mail`.
- `TelnetAppender` is the only inbound network surface: it listens and streams events to whoever
  connects. Its listening address and port are configuration (trusted), but its handling of
  connections is in scope.
- Robustness: an appender must never let an exception escape to the logging caller, and must not
  hang, crash the process or leak resources because of content.

Less important or out of scope:

- `src/log4net.Tests`, `src/log4net.Tests.Aot`, `src/log4net.Ext.Mail.Tests`, `src/integration-testing`,
  `examples/`, `scripts/`, the site under `src/site`: tooling and tests, not shipped code.
- `PatternLayout` and other unstructured layouts make **no** injection guarantee: CR/LF in the output
  of a plain-text layout is by design (FAQ, CWE-93).
- A logged object's own `ToString()` is application code: one that recurses or never terminates is
  not a log4net defect. One that throws is caught while rendering (`RendererMap.FindAndRender`); a
  path where that exception still reaches the logging caller is in scope.

## How to exercise it

- The image is built and restored, so the tests run offline:
  `dotnet test src/log4net.Tests/log4net.Tests.csproj --no-build --filter <Name>`.
- `src/log4net.Tests/Layout/XmlLayoutTest.cs` and `src/log4net.Tests/Appender/RemoteSyslogAppenderTest.cs`
  show how to drive a layout or appender in memory. A good reproducer is a single NUnit test that
  logs crafted content and asserts on the formatted output.

## How you rate severity

Name the adversary first, then check that capability against the threat model. If it needs a
capability the model does not grant, it is not a vulnerability: call it a correctness bug, a
reliability defect or hardening, and say so. That is more useful to us than a severity.

- **Critical**: code execution, or reading/writing arbitrary files, driven by untrusted content alone.
- **High**: untrusted content that breaks the structure of XML or RFC 3164 syslog output, so that it
  forges or hides records for a parser that consumes the document as log4net emitted it; or that
  crashes the host process (an unhandled exception on a background thread, a stack overflow).
- **Medium**: denial of service from content within normal size limits (unbounded memory, CPU, or a
  hang of the logging call), or an event or part of a record silently lost because of its content.
- **Low**: resource leaks, missing escapes with no demonstrated parser impact, defense in depth.

Only claim impact you can demonstrate with the reproducer.

# log4net.Ext.Mail

A MailKit-based SMTP appender for log4net, recommended as the replacement for the deprecated built-in `System.Net.Mail.SmtpClient` appender.

## Overview

`log4net.Ext.Mail` provides a satellite package that extends [log4net](https://www.nuget.org/packages/log4net/) with a modern email appender. It uses [MailKit](https://github.com/jstedfast/MailKit) instead of the deprecated `System.Net.Mail.SmtpClient`, so you can reliably send log events via SMTP on modern servers.

The appender exposes the same API as the legacy `log4net.Appender.SmtpAppender`, making migration straightforward - you only need to change the `type` attribute in your log4net configuration.

## Installation

```bash
dotnet add package log4net.Ext.Mail
```

## Quick Start

Configure the appender in your `App.config` or `app.config`:

```xml
<configuration>
  <configSections>
    <section name="log4net" type="System.Configuration.IgnoreSectionHandler" />
  </configSections>

  <log4net>
    <appender name="SmtpAppender" type="log4net.Ext.Mail.Appender.SmtpAppender, log4net.Ext.Mail">
      <to value="recipient@example.com" />
      <from value="sender@example.com" />
      <subject value="Log Event" />
      <smtpHost value="mail.example.com" />
      <port value="587" />
      <enableSsl value="true" />
      <authentication value="Basic" />
      <username value="user@example.com" />
      <password value="password" />
      <bufferSize value="512" />
      <lossy value="true" />
      <evaluator type="log4net.Core.LevelEvaluator">
        <threshold value="ERROR" />
      </evaluator>
      <layout type="log4net.Layout.PatternLayout">
        <conversionPattern value="%date [%thread] %-5level %logger - %message%newline" />
      </layout>
    </appender>

    <root>
      <level value="INFO" />
      <appender-ref ref="SmtpAppender" />
    </root>
  </log4net>
</configuration>
```

Then configure log4net in your code:

```csharp
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
```

## Key Differences from the Built-in Appender

| Feature | Built-in | log4net.Ext.Mail.Appender.SmtpAppender |
|---------|----------|----------------------------------------|
| SMTP Client | `System.Net.Mail.SmtpClient` (deprecated) | MailKit |
| TLS Support | Limited | Full (Auto, Explicit, Implicit) |
| Modern Servers | Often fails | ✓ Recommended |
| `smtpHost` | Optional | Required |
| `enableSsl` | true/false | true/false (negotiates Auto/None) |
| `Ntlm` auth | Uses Windows logon | Requires explicit credentials |

## Configuration Options

### Required
- `to` - recipient email address (comma or semicolon delimited)
- `from` - sender email address
- `subject` - email subject line
- `smtpHost` - SMTP server hostname

### Optional
- `cc` - carbon copy recipients
- `bcc` - blind carbon copy recipients
- `replyTo` - reply-to address
- `port` - SMTP port (default: 25)
- `enableSsl` - negotiate TLS/STARTTLS (true/false, default: false)
- `authentication` - `None`, `Basic`, or `Ntlm` (default: `None`)
- `username` - username for authentication
- `password` - password for authentication
- `priority` - email priority (`Low`, `Normal`, `High`)
- `bufferSize` - buffer up to N events before sending (default: 512)
- `lossy` - discard buffered events if trigger never fires (default: false)
- `evaluator` - custom evaluator to trigger sending (e.g., `LevelEvaluator`)

## Frameworks

Targets **.NET Standard 2.0**, so it works with:
- .NET Framework 4.7.1+
- .NET 8+

## Documentation

For complete configuration examples and detailed options, see the [log4net SmtpAppender documentation](https://logging.apache.org/log4net/manual/configuration/appenders/smtpappender.html).

## License

Licensed under the Apache License 2.0. See the LICENSE file for details.
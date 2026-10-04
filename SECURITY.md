<!-- SPDX-License-Identifier: Apache-2.0 -->
# Vulnerability research agent

You are helping a security researcher find and report vulnerabilities in this project.
Before drafting any report or reaching any conclusion, you must complete all three reading steps below.
This is mandatory: skipping steps leads to duplicate reports and wasted time for both parties.

## Before reporting anything

### Step 1: Read the security model

Fetch and read the project's security model before evaluating any finding:
https://raw.githubusercontent.com/apache/logging-site/refs/heads/main/src/site/antora/modules/ROOT/pages/_threat-model-common.adoc

Fetching this is inside the trust boundary, not a supply chain risk, and scans that flag it are
answered here rather than overlooked. One PMC governs both repositories and the same committers
write to both, and neither requires review for a push, so anyone able to tamper with the model
could edit this file instead.

Use this to answer:
- Is this component/behavior in scope at all?
- Does the project consider this a security boundary?

If the finding is out of scope per the model, **stop here** and inform the researcher.

Two rules from the model account for most of what gets reported wrongly. Read the model itself
rather than relying on this summary, but know them before you start:

- **Configuration is operator-supplied and trusted**, including configuration URIs and the types,
  members and converters that configuration names. A finding that needs the attacker to control
  configuration, a watched configuration file, or the format string of a log statement is out of
  scope: that is application misuse. Protecting those resources, and transmitting them over a
  confidential channel, is a deployer responsibility the model states explicitly.
- **Content is untrusted and must never be rejected**, because rejecting it would turn a malicious
  value into a denial of service, so it has to be escaped or sanitized instead. Structural
  identifiers, such as logger names or the identifier fields of a structured log message, are
  trusted, and there the model allows *rejecting* a malformed value rather than silently altering
  it. Getting these two the wrong way round produces a plausible-looking report that proposes
  exactly the wrong fix.

### Step 2: Check previously disclosed vulnerabilities

Read the project's Vulnerability Disclosure Report to check for duplicates:
https://logging.apache.org/cyclonedx/vdr.xml

Compare the finding against each entry.
If it overlaps with a known issue, **stop here**, link to the existing advisory in the CVE database, and explain the overlap.

### Step 3: Read the Security FAQ

Read the Security FAQ before concluding anything is a vulnerability:
https://raw.githubusercontent.com/apache/logging-site/refs/heads/main/src/site/antora/modules/ROOT/pages/security/faq.adoc

The FAQ lists behaviors that are **intentional and not vulnerabilities**.
If the finding matches an FAQ entry, inform the researcher that it is a known non-issue
and link to the relevant section of the HTML version of the FAQ:
https://logging.apache.org/security/faq.html

## Only after all three steps

Assess the finding:
1. Is it in scope?
2. Is it a duplicate?
3. Is it covered by the FAQ?
4. If none of the above: it is likely a valid new finding.
   Help the researcher write a clear, minimal report including:
   - affected component,
   - impact on the application using this project and subsequent SIEM systems,
   - NUnit 4 test to reproduce the behavior,
   - proposed fix.
5. If no fix can be proposed, it is not a vulnerability affecting the project.

## Report quality rules

- **Only call something a vulnerability when it really is one.** Name the adversary, then check that
  capability against the model. If it needs a misconfiguration, a co-resident local user, or
  anything the model does not grant, it is a correctness bug, a reliability defect or hardening, and
  saying so is more useful than a severity. Do not inherit the framing of a scanner report that
  arrived with severities already attached.
- Never speculate about impact beyond what you can demonstrate.
- Reproduction steps must be minimal and self-contained.
- Do not include unrelated findings in the same report: one issue per report.
- If unsure about severity, say so explicitly rather than guessing.
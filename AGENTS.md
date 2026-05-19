<!-- SPDX-License-Identifier: Apache-2.0 -->
# Vulnerability research agent

You are helping a security researcher find and report vulnerabilities in this project.
Before drafting any report or reaching any conclusion, you must complete all three reading steps below.
This is mandatory: skipping steps leads to duplicate reports and wasted time for both parties.

## Before reporting anything

Read and follow the three mandatory steps in [SECURITY.md](SECURITY.md#before-reporting-anything).

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

- Never speculate about impact beyond what you can demonstrate.
- Reproduction steps must be minimal and self-contained.
- Do not include unrelated findings in the same report: one issue per report.
- If unsure about severity, say so explicitly rather than guessing.

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

See [AGENTS.md](AGENTS.md) for the threat model and security boundaries.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

## C# code style

Derived from `.editorconfig`, `src/Directory.Build.props`, `src/log4net.globalconfig` and the
existing sources. Match the surrounding file first; the notes below are what that file will
almost always be doing.

### Formatting
- 2-space indent, spaces not tabs, CRLF line endings.
- Every source file starts with the `#region Apache License` / `#endregion` ASF header
  (238 of 244 files in `src/log4net`). Copy it verbatim into new files.
- File-scoped namespaces (`namespace log4net.Appender;`). Note `.editorconfig` still says
  `csharp_style_namespace_declarations = block_scoped:silent`, but 242 of 244 files are
  file-scoped: follow the code, not that setting.
- `using` directives outside the namespace, in one contiguous block.

### Language usage
- **Explicit types, not `var`**: all three `csharp_style_var_*` options are `false`.
  Write `StringWriter writer = new(...)`.
- Target-typed `new()` and collection expressions (`private static readonly char[] _x = [',', ';'];`).
  Omit the type wherever the target is known, including `return new(…);` and `=> new(…);`, where
  the enclosing member's return type supplies it. It cannot be omitted when the target type is an
  interface or abstract class, as in `Func<ISmtpTransport> f = () => new MailKitSmtpTransport();`.
- Expression-bodied members whenever the body fits on one line, including constructors
  (`resharper_constructor_or_destructor_body = expression_body`).
- Braces on `if`/`else` bodies even for a single statement.
- `LangVersion` is `latest`, and current C# features are welcome and in use: primary
  constructors (`csharp_style_prefer_primary_constructors = true`), the `field` keyword in
  property accessors, list patterns, `switch` expressions.
- **Wrap long string literals with a multi-line raw string (`"""`), never with `+`
  concatenation.** This includes attribute arguments; see the `[Obsolete(...)]` message on
  `log4net.Appender.SmtpAppender`. Raw strings have no line-continuation, so each source line
  break really is a `\n` in the value, but that is fine here: compiler diagnostics render those
  newlines as spaces, so a wrapped message still reads as one sentence. Raw strings are constant
  expressions, so they are legal in attributes, and the feature is purely syntactic, so it works on
  `net462`/`netstandard2.0` too.
- Private fields are `_camelCase`. Private fields and helper methods are commonly placed
  *after* the public surface of the type rather than at the top.

### Nullability, the big constraint
- `Nullable` is enabled solution-wide with `WarningsAsErrors=nullable`: **any nullability
  warning is a build error**, so it cannot be deferred.
- `log4net` targets `net462;netstandard2.0`. **Neither reference assembly is nullable-annotated**,
  so BCL postcondition attributes are invisible to the compiler. `string.IsNullOrEmpty` does not
  narrow a `string?`, which is why `Log4NetAssert.EnsureNotNullOrEmpty` tests
  `value is string { Length: > 0 }` instead. Reach for a pattern the compiler's flow analysis
  understands before reaching for `!` or a `#pragma`.
- Missing framework attributes are polyfilled as `internal` types under
  `src/log4net/Diagnostics/CodeAnalysis/` (`NotNullAttribute`, `CallerArgumentExpressionAttribute`,
  `ValidatedNotNullAttribute`, …).

### Argument validation and error handling
- Use the internal `log4net.Util.Log4NetAssert` extensions rather than hand-rolled checks:
  `EnsureNotNull()`, `EnsureNotNullOrEmpty()`, `EnsureIs<T>()`. They carry
  `[CallerArgumentExpression(nameof(value))]`, so no argument name is passed at the call site.
  This includes constructor and property assignments: write `_x = x.EnsureNotNull();`,
  not `_x = x ?? throw new ArgumentNullException(nameof(x));`.
- Appenders never let exceptions escape to the caller. The house pattern is
  `catch (Exception e) when (!e.IsFatal()) { ErrorHandler.Error("...", e); }`.

### Projects and dependencies
- All package versions live in `src/Directory.Build.props` as `<XxxPackageVersion>` properties.
  Never hardcode a version in a `.csproj`.
- `Log4NetAssert` and the `Diagnostics/CodeAnalysis` attributes are `internal` to `log4net`.
  Satellite projects therefore *compile the sources in* rather than referencing them:
  `<Compile Include="..\log4net\Util\Log4NetAssert.cs" Link="Util\Log4NetAssert.cs" />`
  (see `log4net.Ext.Mail.csproj` and `log4net.Tests.csproj`). Linking `Log4NetAssert` also
  requires linking `NotNullAttribute`, `ValidatedNotNullAttribute` and
  `CallerArgumentExpressionAttribute`, or you get `CS0122`.
- Analyzers (`Microsoft.CodeAnalysis.NetAnalyzers`, `AnalysisLevel 8`, `src/log4net.globalconfig`)
  run on every build. **The solution builds with 0 warnings, keep it that way.**

### Documentation comments
- **Every public and protected member gets an XML doc comment**, in test code as well as production
  code: test methods, nested helper classes and hand-written fakes included.
- Use `/// <inheritdoc/>` when the member implements an interface or overrides a base member, and a
  real `<summary>` for everything else. `Log4NetTransaction` in the AdoNet test doubles is the
  pattern to copy.
- When checking whether a member is documented, remember that `[Test]`, `#pragma` and
  `// ReSharper disable` lines legitimately sit between the doc comment and the declaration.

### Writing, in code and everywhere else
- **Never use an em dash (`—`) or en dash (`–`).** Use a plain hyphen, or restructure with a colon,
  comma or parentheses. This covers comments, XML docs, commit messages, AsciiDoc and chat.
- In AsciiDoc, ` -- ` is also forbidden: Asciidoctor renders a spaced double hyphen as an em dash,
  so it breaks the rule even though the source looks like plain hyphens. Grep touched files for
  `[—–]` and ` -- ` before presenting a change.
- No underscores in identifiers, including test method names. `AllContainsEveryFlag`, not
  `All_ShouldContainAllFlags`. (Private fields are `_camelCase`, which is the one exception.)

### Tests
- NUnit 4, not MSTest, and always the constraint model: `Assert.That(actual, Is.EqualTo(expected))`
  (810 uses of `Assert.That`, zero of `Assert.AreEqual`). `[TestFixture]`, `[Test]`, `[TestCase]`,
  with `[SetUp]`/`[TearDown]` for per-test state.
- Use an expression body for a single-statement test: `public void X() => Assert.That(...);`.
- **`log4net` has no `InternalsVisibleTo`**, so private and internal members are exercised through
  reflection, not by widening their accessibility. See `SystemInfoTest`, `LevelMappingTest` and
  `UserNameFixingTest` for the `BindingFlags.Static | BindingFlags.NonPublic` pattern.
  `log4net.Ext.Mail` does grant `InternalsVisibleTo` to its own test project.
- Mark a test `[NonParallelizable]` when it mutates static state (`LogLog.InternalDebugging`, a
  static field on a test double, a process-wide native registration).
- Wrap expected internal logging in `LogLog.ExecuteWithoutEmittingInternalMessages(...)` and capture
  it with `LogLog.LogReceivedAdapter` rather than letting it reach the console. Appender errors are
  emitted by default, so a test that provokes one will otherwise add noise to the suite output.
- Guard platform-specific tests with `[Platform("Win")]` / `[Platform("Linux")]`. A test that only
  runs on Windows leaves the behaviour unverified in local Linux runs, so prefer a cross-platform
  home for the assertion when one exists.
- `NUnit.Analyzers` warnings are errors too: for example NUnit1032 requires an `IDisposable` fixture
  field to be disposed in a `[TearDown]` method.
- For code that talks to the outside world, introduce a narrow interface and hand-write a fake;
  there is no mocking library in any test project. See `ISmtpTransport` / `FakeSmtpTransport`.
- Verify with `dotnet build src/log4net.sln` and
  `dotnet test src/<project>.Tests/<project>.Tests.csproj`.
- **When inspecting build output, redirect it to a file and read the whole thing; do not pipe
  MSBuild through line-oriented tools.** `grep`/`Select-String` cannot match across newlines, and
  MSBuild's console logger formats differently when piped than when redirected, so a multi-line
  diagnostic message then looks truncated when it is not. Before reporting that the toolchain
  mangles something, re-check with `dotnet build … > out.txt 2>&1` and inspect `out.txt`.

## Changelog

Every user-visible change gets an entry in `src/changelog/<unreleased version>/`, named
`<issue>-<kebab-case-slug>.xml`. The format is the log4j changelog schema:

- `type` is one of `added`, `changed`, `fixed`, `removed`, `updated`.
- **Every `<issue>` element requires both `id` and `link`**; the export fails with
  `missing attribute: link` otherwise, which is only caught by the Maven site build.
- Put anything that has no issue number, such as an external finding identifier, in the description
  text rather than inventing an `<issue>` for it.
- `src/changelog/3.3.2/298-fix-interprocesslock-mutex-leak.xml` shows the shape for a change that
  came out of an external audit.

## Documentation site

The manual lives in `src/site/antora/modules/ROOT/pages/`. A new appender page needs three edits,
not one: the page itself, an `xref` line in `nav.adoc` (kept alphabetical), and the appender table
in `manual/configuration/appenders.adoc`.

## Security findings

**[AGENTS.md](AGENTS.md) decides whether something is in scope and whether it is a vulnerability.**
Read it before triaging a report, and describe a finding in commit messages and changelog entries
the way it comes out of that assessment: a correctness bug, a reliability defect or hardening is
none the worse for being called one.

What that leaves for this file is where the answers live in the code:

- When a report is likely to recur on a path the threat model already settles, leave a short comment
  at the site with a link to the model rather than changing the code. `XmlConfigurator` and
  `XmlHierarchyConfigurator` carry these for the configuration-is-trusted paths, and
  `SystemStringFormat` for the format string.
- `LocalSyslogAppender.EscapeNulCharacters` and `RemoteSyslogAppender.ValidateIdentity` are the two
  sides of the content and structural-identifier rule: content is escaped and never rejected, a
  malformed identifier is reported rather than quietly repaired.
- Deliberate secure-default choices belong in the changelog with their opt-out named, so that an
  upgrade surprise is searchable. See the entries for `SendTimeoutMillis`, `MatchTimeoutMillis` and
  `LockTimeoutMillis`.

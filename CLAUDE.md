# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

See [AGENTS.md](AGENTS.md) for the threat model and security boundaries.

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
- A small private carrier type is a `readonly record struct` with a primary constructor. Extra
  intent-revealing constructors chain onto it (`internal Item(T payload) : this(payload, null) { }`).
- Infrastructure that has to be `public` only to cross an assembly boundary gets
  `[EditorBrowsable(EditorBrowsableState.Never)]` and a `<remarks>` paragraph saying why it is
  public, so it stays out of consumers' IntelliSense. See `log4net.Util.BackgroundSender`.

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
- **A catch of `Exception` always carries `when (!e.IsFatal())`.** The filter is not a licence to
  widen: a catch that handles one expected exception keeps naming that type. Widening it to
  `Exception` turns an unrelated bug into a silently counted failure, which is the opposite of what
  the idiom is for.
- On a background thread the same rule is harder: an exception that escapes the thread body is
  unhandled and takes the process down. Everything the thread calls out to, the error reporting
  included, has to be wrapped, because a caller-supplied error handler can throw too. See the
  `Report` helper in `log4net.Util.BackgroundSender`, and its `finally` block, which may run
  against an already disposed queue.

### Projects and dependencies
- All package versions live in `src/Directory.Build.props` as `<XxxPackageVersion>` properties.
  Never hardcode a version in a `.csproj`.
- Such a version is a floor, and raising one is a minor-release change, never part of a fix.
  NuGet resolves to the maximum of all requests, so a consumer who updates gets the newer
  version through their own graph whatever our floor says; the only consumers a floor moves are
  those who pinned deliberately, and they get an `NU1605` downgrade error that stops their build.
  Argue a bump on dependency-hygiene grounds on its own, not as a fix for the symptom that
  prompted it.
- `System.Configuration.ConfigurationManager` is referenced only when `TargetFramework != net462`
  (`src/log4net/log4net.csproj:82-88`), so its floor affects the `netstandard2.0` asset alone.
- `Log4NetAssert` and the `Diagnostics/CodeAnalysis` attributes are `internal` to `log4net`.
  Satellite projects therefore *compile the sources in* rather than referencing them:
  `<Compile Include="..\log4net\Util\Log4NetAssert.cs" Link="Util\Log4NetAssert.cs" />`
  (see `log4net.Ext.Mail.csproj` and `log4net.Tests.csproj`). Linking `Log4NetAssert` also
  requires linking `NotNullAttribute`, `ValidatedNotNullAttribute` and
  `CallerArgumentExpressionAttribute`, or you get `CS0122`.
- `log4net` is strong named (`SignAssembly`, `log4net.snk`); `log4net.Ext.Mail` deliberately is
  not, and says so in its `.csproj`. The CLR forbids a strong-named assembly from granting
  `InternalsVisibleTo` to an unsigned one, so **no internal of `log4net` can ever be reached from
  `log4net.Ext.Mail`**. Shared infrastructure is therefore either `public` (with
  `[EditorBrowsable]`, above) or compiled in by `<Compile Include>` as `Log4NetAssert` is.
- Analyzers (`Microsoft.CodeAnalysis.NetAnalyzers`, `AnalysisLevel 8`, `src/log4net.globalconfig`)
  run on every build. **The solution builds with 0 warnings, keep it that way.** Two that bite new
  code: CA1711 rejects a type name ending in `Queue`, `Collection` or `Flags`, and CA2000 rejects
  an `IDisposable` that is not disposed on every path. Prefer a design that removes the warning
  over suppressing it: swapping a `ManualResetEventSlim` for a `TaskCompletionSource<bool>` dropped
  CA2000 and the disposal race behind it at once. Note the non-generic `TaskCompletionSource` does
  not exist on `net462`/`netstandard2.0`, so use `TaskCompletionSource<bool>`.

### Documentation comments
- **Keep them short.** A doc comment is one or two sentences, an inline comment is one line. If a
  remark wants a second paragraph, the reason belongs in the commit message or the changelog, not
  in the source. Say what the code does not already say, then stop. This is the single most common
  correction made in review, so err shorter than feels right.
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
- **NUnit constructs one fixture instance for the whole fixture**, so an instance field that
  records what a test observed accumulates across the tests in it. Clear such state in `[SetUp]`.
- Drive a test over a background thread with gates (`ManualResetEventSlim`), never with
  `Thread.Sleep`: park the worker, assert the state you care about, then release it. See
  `BackgroundSenderTest`, where every wait has a generous timeout and the assertions are exact.
- **`Does.Contain` is culture sensitive and cannot be made ordinal**: its `ContainsConstraint` has
  no comparison parameter. Linguistic comparison skips ignorable characters, so `Does.Not.Contain`
  reports a match for NUL, soft hyphen or a zero-width character in a string that holds none. When
  the assertion is about control characters, use
  `Contains.Substring(x).Using(StringComparison.Ordinal)`, negated with the `!` operator that
  `Constraint` defines, or assert the whole value with `Is.EqualTo`, which is ordinal.
- Mark a test `[NonParallelizable]` when it mutates static state (`LogLog.InternalDebugging`, a
  static field on a test double, a process-wide native registration).
- Wrap expected internal logging in `LogLog.ExecuteWithoutEmittingInternalMessages(...)` and capture
  it with `LogLog.LogReceivedAdapter` rather than letting it reach the console. Appender errors are
  emitted by default, so a test that provokes one will otherwise add noise to the suite output.
- Guard platform-specific tests with `[Platform("Win")]` / `[Platform("Linux")]`. A test that only
  runs on Windows leaves the behaviour unverified in local Linux runs, so prefer a cross-platform
  home for the assertion when one exists.
- When a diagnosis genuinely needs the other operating system, ask the user to continue the work
  in a session on that platform rather than approximating it. Many developers work on both Linux
  and Windows and can switch, so leave a handoff note with what is established and what still
  needs the other machine, as was done for issue #162.
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

### Application settings without a configuration system

`SystemInfo.GetAppSetting` degrades to environment variables when the configuration system is
unavailable, and `IsMissingConfigurationSystem` decides that from the shape of the exception. The
unit tests construct those exceptions by hand; to see the real thing:

- The trigger is `Assembly.GetEntryAssembly() == null`, which is true in any process that hosts
  the runtime natively. Reflecting onto the internal `Assembly.SetEntryAssembly(null)` reproduces
  it in-process on .NET 10, P/Invoking `hostfxr` from `powershell.exe` 5.1 loads the CoreCLR side
  by side in a .NET Framework process, and a C++ host built with `cl.exe` is the real case from
  issue #162. All three produce the same exception chain.
- Which `System.Configuration.ConfigurationManager` asset is loaded decides the behaviour. The
  net4x assets are around 92 KB and only forward types to the in-box `System.Configuration`, which
  handles a null entry assembly happily; the `netstandard2.0` asset (around 382 KB at 4.5.0) is the
  ported implementation that fails. A `netstandard2.0` build output dropped into a .NET Framework
  host therefore behaves unlike the same library restored from NuGet on `net4x`.

## Changelog

Every user-visible change gets an entry in `src/changelog/<unreleased version>/`, named
`<issue>-<kebab-case-slug>.xml`. The format is the log4j changelog schema:

- `type` is one of `added`, `changed`, `fixed`, `removed`, `updated`.
- **Every `<issue>` element requires both `id` and `link`**; the export fails with
  `missing attribute: link` otherwise, which is only caught by the Maven site build.
- Put anything that has no issue number, such as an external finding identifier, in the description
  text rather than inventing an `<issue>` for it.
- Close the description with an attribution in parentheses, crediting both sides: who raised it and
  who did the work, as in `(reported by @viktorgobbi, fixed by @FreeAndNil)`. `implemented by` reads
  better than `fixed by` for an `added` or `changed` entry, and once a pull request exists the house
  form appends it: `fixed by @FreeAndNil in https://github.com/apache/logging-log4net/pull/246[#246]`.
  Take the fixer from the active committers in `STATUS.txt`, whose Apache ids are the GitHub handles
  (`freeandnil`, `gdziadkiewicz`, `davydm`), and identify which one from the session's `git config
  user.email`. Ask rather than guess if that does not match a listed committer.
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
- `log4net.Appender.Internal.ContentEscape` and `RemoteSyslogAppender.ValidateIdentity` are the two
  sides of the content and structural-identifier rule: content is escaped and never rejected, a
  malformed identifier is reported rather than quietly repaired.
- **A sink that cannot carry a character escapes it visibly, and never drops the character, the
  rest of the record, or the event.** The escapes already in use are `\0` for NUL, `\r` and `\n`
  for newlines, and `\uXXXX` for anything else, in `ContentEscape` and in
  `RemoteSyslogAppender.AppendMessage`. Put new ones in `ContentEscape` rather than in the
  appender: four appenders have needed the same two so far. Escaping before a length limit, not
  after, since an escape is longer than what it replaces.
- Deliberate secure-default choices belong in the changelog with their opt-out named, so that an
  upgrade surprise is searchable. See the entries for `SendTimeoutMillis`, `MatchTimeoutMillis` and
  `LockTimeoutMillis`.

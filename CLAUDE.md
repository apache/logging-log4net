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
  file-scoped — follow the code, not that setting.
- `using` directives outside the namespace, in one contiguous block.

### Language usage
- **Explicit types, not `var`** — all three `csharp_style_var_*` options are `false`.
  Write `StringWriter writer = new(...)`.
- Target-typed `new()` and collection expressions (`private static readonly char[] _x = [',', ';'];`).
- Expression-bodied members whenever the body fits on one line — this includes constructors
  (`resharper_constructor_or_destructor_body = expression_body`).
- Braces on `if`/`else` bodies even for a single statement.
- `LangVersion` is `latest`, and current C# features are welcome and in use: primary
  constructors (`csharp_style_prefer_primary_constructors = true`), the `field` keyword in
  property accessors, list patterns, `switch` expressions.
- Private fields are `_camelCase`. Private fields and helper methods are commonly placed
  *after* the public surface of the type rather than at the top.

### Nullability — the big constraint
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
  This includes constructor and property assignments — write `_x = x.EnsureNotNull();`,
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
  run on every build. **The solution builds with 0 warnings — keep it that way.**

### Tests
- NUnit 4, not MSTest, and always the constraint model: `Assert.That(actual, Is.EqualTo(expected))`
  (810 uses of `Assert.That`, zero of `Assert.AreEqual`). `[TestFixture]`, `[Test]`, `[TestCase]`,
  with `[SetUp]`/`[TearDown]` for per-test state.
- `NUnit.Analyzers` warnings are errors too — e.g. NUnit1032 requires an `IDisposable` fixture
  field to be disposed in a `[TearDown]` method.
- For code that talks to the outside world, introduce a narrow interface and hand-write a fake;
  there is no mocking library in any test project. See `ISmtpTransport` / `FakeSmtpTransport`.
- Verify with `dotnet build src/log4net.sln` and
  `dotnet test src/<project>.Tests/<project>.Tests.csproj`.

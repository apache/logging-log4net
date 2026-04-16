# Copilot Instructions

## Test Conventions
- Prefer test method names without underscores
- create XML comments for test methods
- use Arrange, Act and Assert comments only for tests that are larger than 25 lines
- try to keep the tests short and maintainable (under 25 lines)

## C# Style

- **No `var`** - always use the explicit type on the left side; use implicit `new()` on the right side
  ```csharp
  // correct
  MemberInfo member = GetMember<NoAttributes>(nameof(NoAttributes.Value));
  ReflectionTestClass target = new();

  // wrong
  var member = GetMember<NoAttributes>(nameof(NoAttributes.Value));
  ```
- prefer expression collection syntax (\[item1, item2\]) over new string\[\] { item1, item2 }

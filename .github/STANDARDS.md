# Joust Code Review Standards

## Language & Style
- **C# only.** Stick to C# conventions and idioms.
- **Naming:** PascalCase for classes, methods, properties; camelCase for local variables and parameters.
- **File organization:** One public class per file. Nested types are fine for small supporting classes.

## Architecture & Patterns
- **Dependency Injection.** Use constructor injection for all dependencies. No service locators or static instances.
- **Separation of concerns.** Business logic in services, presentation logic in controllers/UI, data access in repositories.
- **Async/await.** Prefer async/await over callbacks. Avoid blocking calls (`.Result`, `.Wait()`).

## Testing
- Unit tests required for all new services and business logic.
- Integration tests for repository/database interactions.
- Test naming: `MethodName_Scenario_ExpectedResult`.

## Performance & Safety
- Avoid LINQ in tight loops; profile before optimizing.
- No null dereferences — use null coalescing (`??`), null-conditional (`?.`), or explicit null checks.
- Dispose IDisposable objects explicitly or use `using` statements.
- No hardcoded secrets, API keys, or connection strings — use configuration.

## Error Handling
- Never swallow exceptions silently. Log or rethrow with context.
- Use specific exception types; avoid catching `Exception`.
- Async methods should not fire-and-forget without explicit handling.

## Code Review Signals
- Keep PRs focused — one feature or fix per PR.
- Commit messages describe the *why*, not just the what.
- No commented-out code or dead branches — delete or raise an issue.

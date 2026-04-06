# Joust Code Standards

## C# Code Style & Naming

- **Formatting:** Follow Microsoft C# coding conventions. Use 4-space indentation, no tabs.
- **Naming conventions:**
  - Public members and methods: `PascalCase`
  - Private fields: `_camelCase` with leading underscore
  - Local variables and parameters: `camelCase`
  - Constants: `UPPER_SNAKE_CASE`
  - Async methods: suffix with `Async` (e.g., `LoadDataAsync`)
- **Type usage:**
  - Use explicit types for public API and complex expressions
  - `var` is acceptable for obvious types in local scopes (e.g., `var position = transform.position`)
- **Nullable reference types:** Enable and use `#nullable enable` where appropriate

## Architecture & Dependency Injection

- **No unnecessary singletons.** Use dependency injection instead. Inject dependencies through constructors.
- **Singleton exceptions:** Only use for truly global, single-instance systems (e.g., AudioManager, EventBus). Document why it's a singleton.
- **Preferred DI pattern:** Constructor injection. Avoid service locators.
- **OOP principles:** Depend on abstractions (interfaces), not concrete implementations. Keep classes focused and single-responsibility.
- **Separation of concerns:** UI logic, business logic, and data logic must be in separate classes.

## Performance & Object Management

- **Object pooling:** Use object pools for frequently instantiated objects (bullets, particles, enemies). Never Instantiate/Destroy in hot loops without justification.
- **Instantiation:** Comment or justify any direct `Instantiate()` or `Destroy()` calls outside of initialization.
- **MonoBehaviour discipline:**
  - Keep `Update()`, `LateUpdate()`, and `FixedUpdate()` lightweight. Move heavy logic to called methods.
  - Cache `GetComponent()` calls in `Awake()` or `Start()`, never in `Update()`.
  - Avoid LINQ in `Update()` or frequently-called methods.

## Asset & Project Organization

- **Folder structure:** Organize by feature/system, not by asset type.
  - Example: `Assets/Game/Player/Scripts`, `Assets/Game/Player/Prefabs`, `Assets/Game/Enemy/...`
  - Keep scenes in `Assets/Scenes/` with clear naming (e.g., `MainMenu.unity`, `Level_01.unity`)
- **Naming:** Assets should have clear, descriptive names. Prefabs: `PrefabName.prefab`. Scripts: `ClassName.cs`.
- **Meta files:** Always commit `.meta` files. Never force resolve merge conflicts in scene or prefab files without testing the result in-editor.

## Code Review Standards

- **PR size:** Keep PRs focused. If a change touches multiple systems, split it unless they're tightly coupled.
- **PR description:** Describe what changed and why. Link to any relevant task/issue numbers.
- **Testing:** Test your changes in-editor before pushing. For gameplay changes, include manual test steps in the PR description.
- **Comments & documentation:** Add comments for non-obvious logic. Complex algorithms or game mechanics should have a brief explanation.

## General Guidelines

- **No dead code:** Remove commented-out code and unused imports.
- **Const over magic numbers:** Any repeated literal value should be a constant with a clear name.
- **Error handling:** Handle null checks explicitly; don't assume references are valid. Use `Debug.Assert` for dev-time validation.

# ADR-0005: FluentValidation in MediatR Pipeline

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

All user-supplied input must be validated at the API boundary before reaching business logic (L2-025). Validation must produce structured, field-level error messages that map to RFC 7807 Problem Details responses with a 400 status code. Validation rules vary per operation (e.g., creating an article requires title and body; updating allows partial changes).

Validation logic should be co-located with the feature it validates and applied automatically — developers should not need to remember to call validation manually in each handler.

## Decision

We will use **FluentValidation** for all input validation, executed automatically via a **MediatR `ValidationBehavior<TRequest, TResponse>`** pipeline behavior. Each MediatR request type has an optional corresponding `AbstractValidator<TRequest>` that defines its rules. The behavior collects all validation failures and throws a `ValidationException`, which the exception-handling middleware converts to an RFC 7807 400 response.

## Options Considered

### Option 1: FluentValidation with MediatR Pipeline Behavior
- **Pros:** Validation is automatic — every MediatR request is validated before the handler runs, validators are co-located with their features (same folder in vertical slice), fluent API produces readable, composable rules, field-level errors map directly to RFC 7807 `errors` dictionary, validators are independently testable.
- **Cons:** Requires FluentValidation NuGet dependency, validators must be registered in DI (handled by assembly scanning), validation only runs for MediatR requests (direct service calls bypass it).

### Option 2: Data Annotations on Request Models
- **Pros:** Built into .NET, no additional dependency, `[ApiController]` validates automatically.
- **Cons:** Limited expressiveness for complex rules (conditional validation, cross-field rules), validation logic is scattered across attribute decorations, harder to test independently, no pipeline integration with MediatR.

### Option 3: Manual Validation in Handlers
- **Pros:** No framework dependency, full control over validation logic.
- **Cons:** Validation must be manually invoked in every handler (easy to forget), inconsistent error format across handlers, violates DRY, no automatic integration with the error pipeline.

## Consequences

### Positive
- Zero-effort validation enforcement: if a validator exists for a request type, it runs automatically.
- Validators produce structured `ValidationFailure` objects with property name and error message, mapping directly to `{ "errors": { "title": ["Title is required."] } }`.
- Complex rules (slug uniqueness check, cross-field validation) are expressible in the fluent API.
- Each validator is a standalone class that can be unit tested without HTTP infrastructure.

### Negative
- FluentValidation is a third-party dependency (though widely adopted and stable).
- Async validators (e.g., checking slug uniqueness against the database) run inside the pipeline and add latency before the handler.

### Risks
- If FluentValidation introduces breaking changes in a major version, validators may need migration. This risk is low given the library's stability and widespread adoption.

## Implementation Notes

- `ValidationBehavior<TRequest, TResponse>` resolves all `IValidator<TRequest>` from DI.
- If no validators exist for a request type, the behavior passes through immediately.
- On failure, throws `ValidationException` with the list of `ValidationFailure` objects.
- `ExceptionHandlingMiddleware` catches `ValidationException` and returns ProblemDetails with status 400 and the `errors` dictionary.
- Validators registered via `services.AddValidatorsFromAssembly(typeof(Program).Assembly)`.

## References

- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- L2-025: Input Validation and Sanitization
- Feature 06: RESTful API — ValidationBehavior (Section 3.5)
- Feature 08: Security Hardening — InputValidator (Section 3.6)
- ADR-0002: Vertical Slice Architecture with MediatR

# Contributing

Thanks for your interest in contributing to the Blog platform. This document outlines the process and standards for contributing.

## Development Workflow

1. Fork the repository and create a feature branch from `master`.
2. Write or update the relevant L2 requirement in `docs/specs/L2.md` if your change introduces new behavior.
3. Write a failing acceptance test that traces to the L2 requirement.
4. Implement the change to make the test pass.
5. Run the full test suite to ensure nothing is broken.
6. Submit a pull request.

## Branch Naming

Use descriptive branch names:

```
feature/article-scheduling
fix/slug-collision-handling
docs/update-api-endpoints
```

## Commit Messages

Write concise commit messages that explain *why*, not *what*:

```
Add slug uniqueness validation to prevent URL collisions

Previously, two articles could share the same slug, causing routing
conflicts on the public site. The API now returns 409 Conflict when
a duplicate slug is detected.
```

## Code Standards

### General

- Follow existing patterns in the codebase. Consistency is more important than personal preference.
- No dead code, commented-out code, or TODO comments in pull requests.
- Every public API change must include acceptance criteria tracing to an L2 requirement.

### C# / .NET

- Use the CQRS pattern with MediatR for all API operations.
- Place feature code in `Features/{FeatureName}/` following the vertical slice pattern.
- Validate all input at the API boundary using FluentValidation.
- Use parameterized queries (EF Core) — never concatenate user input into SQL.
- Sanitize article body content to prevent stored XSS.

### API Design

- Use plural nouns for collections (`/api/posts`, not `/api/post`).
- Return appropriate HTTP status codes (201 for creates, 204 for deletes, 409 for conflicts).
- Return RFC 7807 Problem Details for errors.
- Include pagination metadata in collection responses.

### Security

- Never log passwords, tokens, or PII.
- Never commit secrets, connection strings, or credentials.
- All new endpoints must specify authentication requirements.
- Rate limiting must be configured for any new write or auth endpoints.

### Testing

Every test file must include a traceability header:

```csharp
// Acceptance Test
// Traces to: L2-001, L2-003
// Description: Verify article creation with slug generation
```

- Write acceptance tests before implementation (ATDD).
- Test boundary conditions and error cases, not just the happy path.
- Use the `Blog.Testing` project for shared test infrastructure.

## Pull Request Process

1. Ensure all tests pass (`dotnet test`).
2. Ensure no build warnings.
3. Update `docs/specs/L2.md` if new requirements are introduced.
4. Fill in the PR template with a summary and test plan.
5. Request a review.

## Reporting Issues

When filing an issue, include:

- Steps to reproduce the problem.
- Expected vs. actual behavior.
- Relevant logs or error messages (redact any secrets).
- The L2 requirement ID if the issue relates to a specified behavior.

## Code of Conduct

Be respectful, constructive, and professional. Focus feedback on the code, not the person.

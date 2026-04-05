# ADR-0001: Structured JSON Logging with Serilog

**Date:** 2026-04-04
**Category:** infrastructure
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform requires structured logging for operational observability (L1-010). Logs must be in a machine-parseable format so they can be queried, filtered, and aggregated by log management tools. Every API request must be logged with method, path, status code, and duration (L2-033). Errors must include stack traces and correlation IDs. Logs must never contain secrets, passwords, tokens, or PII.

The logging framework must integrate with ASP.NET Core's `ILogger<T>` abstraction and support structured properties (not just string messages), correlation ID enrichment, and multiple output sinks.

## Decision

We will use **Serilog** with the **Serilog.AspNetCore** integration for structured JSON logging. Log entries are emitted as JSON objects with typed properties. A custom `LogSanitizer` enricher strips sensitive data before output. Correlation IDs are injected via `CorrelationIdMiddleware` and pushed onto the Serilog `LogContext`.

## Options Considered

### Option 1: Serilog with JSON Formatting
- **Pros:** First-class structured logging with typed properties, `LogContext` enrichers add correlation IDs and custom properties to all log entries within a scope, JSON output integrates with any log aggregation platform (Seq, ELK, Datadog, Azure Monitor), ASP.NET Core `ILogger<T>` integration is seamless via `Serilog.AspNetCore`, configurable sinks (console, file, external services), message templates preserve semantic meaning.
- **Cons:** Third-party dependency (though Serilog is the de facto standard for .NET structured logging), configuration can be verbose for multiple sinks.

### Option 2: Built-In Microsoft.Extensions.Logging with JSON Console
- **Pros:** No external dependency, built into ASP.NET Core.
- **Cons:** JSON formatting requires custom configuration, no `LogContext` equivalent for enrichment, fewer output sinks, less flexible filtering and enrichment.

### Option 3: NLog
- **Pros:** Mature logging framework, extensive target (sink) support, XML-based configuration.
- **Cons:** XML configuration is verbose, structured logging support is not as ergonomic as Serilog's message templates, smaller community adoption for modern .NET compared to Serilog.

## Consequences

### Positive
- Every log entry is a JSON object with typed fields (`method`, `path`, `statusCode`, `durationMs`, `correlationId`), enabling precise filtering and aggregation.
- `CorrelationIdMiddleware` enriches every log entry with an `X-Correlation-Id` for end-to-end request tracing.
- `RequestLoggingMiddleware` logs every API request at `Information` level (2xx/3xx), `Warning` (4xx), or `Error` (5xx).
- `LogSanitizer` strips `Authorization` headers, passwords, and tokens from log entries before output.
- JSON format is compatible with any log aggregation platform — platform choice is deferred without lock-in.

### Negative
- Serilog is a third-party dependency (mitigated by its widespread adoption and stability in the .NET ecosystem).
- JSON log entries are more verbose than plain text (mitigated by compression in log shipping).

### Risks
- ~~Log aggregation platform choice is deferred.~~ **Resolved:** Azure Monitor with Application Insights selected. Serilog structured logs flow via `Serilog.Sinks.ApplicationInsights`, with KQL for querying and built-in alerting.

## Implementation Notes

- `RequestLoggingMiddleware`: start `Stopwatch`, call `next()`, log `{ method, path, statusCode, durationMs, correlationId, timestamp }`.
- `CorrelationIdMiddleware`: check for `X-Correlation-Id` header, generate if absent, push to `LogContext`, add to response headers.
- `LogSanitizer`: Serilog destructuring policy that redacts properties named `password`, `token`, `authorization`, `secret`.
- Health check endpoint (`/health`) returns `{ status, checks: { database: status } }` — 200 for healthy, 503 for unhealthy.
- Serilog configured in `Program.cs` with `UseSerilog()` and JSON console sink.
- Log levels: `Information` for request logs, `Warning` for 4xx, `Error` for 5xx and unhandled exceptions, `Debug` for handler entry/exit.

## References

- [Serilog Documentation](https://serilog.net/)
- [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore)
- L1-010: Observability
- L2-032: Health Check Endpoint
- L2-033: Structured Logging
- Feature 09: Observability — Full design

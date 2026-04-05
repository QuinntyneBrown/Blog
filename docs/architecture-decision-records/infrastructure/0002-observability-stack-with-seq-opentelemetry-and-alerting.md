# ADR-0002: Observability Stack with Seq, OpenTelemetry, and Alerting

**Date:** 2026-04-04
**Category:** infrastructure
**Status:** Accepted
**Deciders:** Architecture Team

## Context

Structured logs alone are not sufficient for production operations. The platform needs a concrete stack for logs, metrics, traces, and alert routing.

## Decision

We will use the following observability stack:

- **Logs:** Serilog -> **Seq**
- **Metrics/Tracing:** **OpenTelemetry** instrumentation exported via OTLP
- **Dashboards/Alerting:** OTLP-compatible metrics backend with dashboards and alert rules routed to email/PagerDuty or equivalent on-call tooling

## Options Considered

### Option 1: Logs Only
- **Pros:** Lowest implementation effort.
- **Cons:** Weak operational signal and limited alerting.

### Option 2: Seq + OpenTelemetry
- **Pros:** Clear separation of logs from traces/metrics, good .NET ecosystem support, flexible backend integration.
- **Cons:** More components than logs alone.

### Option 3: Single Vendor Monitoring Suite
- **Pros:** Unified UI.
- **Cons:** Stronger vendor lock-in and less flexibility in self-hosted or mixed environments.

## Consequences

### Positive
- Concrete answer for log aggregation, metrics, traces, and alerting integration.
- Future distributed tracing readiness is built in from the start.

### Negative
- Requires operational ownership of more than one telemetry channel.

## References

- Feature 09: Observability
- L1-010: Structured Logging and Diagnostic Endpoints
- L2-032: Health Check Endpoint
- L2-033: Structured Logging

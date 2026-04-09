# Production hardening guide

This checklist covers operational and security controls before go-live.

## 1) Retry policy and resiliency

- Use bounded retries with exponential backoff + jitter for external calls (Graph, Procore, SQL transient faults).
- Respect `Retry-After` and API rate limit responses.
- Add circuit breakers for repeated downstream failures.
- Ensure idempotency for document publish/finalize operations.
- Route poison messages/jobs to dead-letter handling for manual triage.

## 2) Monitoring and alerting

- Emit structured logs with correlation IDs (`JobId`, `ProjectId`, `DocumentId`).
- Capture traces and dependency telemetry in Application Insights.
- Define SLO-aligned alerts:
  - high error rate,
  - elevated latency,
  - queue backlog growth,
  - failed finalization/publication steps.
- Build runbooks for top incident classes.

## 3) RBAC and access control

- Use managed identity for Web/Functions to access Key Vault and Storage.
- Apply least privilege RBAC in Azure subscriptions/resource groups.
- Restrict SQL firewall rules and prefer private networking.
- Use `Sites.Selected` where feasible instead of tenant-wide Graph permissions.
- Separate operator/admin roles in the Web application.

## 4) Secret management and rotation

- Keep all secrets in Key Vault; avoid inline secrets in app settings.
- Rotate credentials on a fixed schedule (for example every 60-90 days) and after incidents.
- Prefer certificate-based credentials over shared secrets when supported.
- Use versioned Key Vault references and rollback procedures.

## 5) Backup and disaster recovery

- Verify Azure SQL PITR retention and long-term backup policy.
- Define RTO/RPO targets and test restoration periodically.
- Backup critical configuration artifacts (IaC, app settings templates, runbooks).
- Plan regional failover strategy for core dependencies.

## 6) Security checklist

- [ ] HTTPS enforced end-to-end.
- [ ] Private endpoints/VNet integration enabled for data-plane services.
- [ ] WAF or equivalent ingress protection configured.
- [ ] Defender for Cloud recommendations reviewed.
- [ ] Dependency and container/package vulnerability scanning enabled.
- [ ] Audit logging enabled for privileged operations.
- [ ] Data classification and retention policy documented.
- [ ] Penetration and threat-model review completed.

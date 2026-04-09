# Required Azure resources

This document lists the minimum Azure resources needed to deploy this solution into a production-like environment.

## Core application hosting

1. **Azure App Service (Web App)**
   - Hosts `src/Web`.
   - Configure managed identity.
   - Integrate with VNet (if private SQL/Key Vault access is required).

2. **Azure Functions App**
   - Hosts `src/Functions`.
   - Plan choice based on workload:
     - Consumption/Premium for bursty workloads.
     - Dedicated/App Service plan for predictable sustained load.
   - Configure managed identity.

## Data and storage

3. **Azure SQL Database**
   - Primary relational store for jobs, execution history, and project metadata.
   - Use Azure AD auth where possible.
   - Enable automatic backups and long-term retention policy.

4. **Azure Storage Account**
   - Required for Functions runtime storage (`AzureWebJobsStorage`).
   - Consider Blob containers for transient file staging.
   - Prefer private endpoints and disallow public blob access unless explicitly needed.

## Secrets and identity

5. **Azure Key Vault**
   - Store API secrets, client secrets, connection strings, and rotation versions.
   - Grant data-plane access to managed identities for Web and Functions.

6. **Microsoft Entra app registrations**
   - Separate app registrations for Web and/or Functions as needed.
   - Configure delegated/application Graph permissions.
   - Configure redirect URIs and certificates/secrets.

## Observability

7. **Application Insights (plus Log Analytics workspace)**
   - Capture request telemetry, traces, exceptions, and dependencies.
   - Configure sampling policy and retention period.
   - Set up alerts for failure rate, latency, queue depth, and integration failures.

## Optional but recommended

- **Azure Monitor alerts/action groups** for operational notifications.
- **Azure Front Door or Application Gateway + WAF** for ingress protection.
- **Azure Container Registry** if workloads move to containerized hosting.
- **Azure Service Bus/Queue Storage** for decoupled async orchestration.

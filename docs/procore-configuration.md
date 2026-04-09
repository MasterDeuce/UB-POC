# Procore API configuration

This document captures required Procore setup for API integration.

## Procore developer app setup

1. Create or use a Procore Developer Portal app.
2. Record the following values:
   - Client ID
   - Client Secret
   - Redirect URI(s)
   - Company ID
3. Configure OAuth flow used by this solution (typically authorization code + refresh token for long-lived server workflows).

## Required scopes (baseline)

Scope names can vary by Procore API product area and account entitlements. Confirm exact scope identifiers in your Procore app configuration.

Commonly required capabilities:

- Read project directory and project metadata.
- Read and write documents/files in the target project context.
- Read related cost/estimate context if mapping work instructions to financial artifacts.

## Environment configuration values

Provide these via Key Vault or environment variables:

- `Procore__BaseUrl` (typically `https://api.procore.com`)
- `Procore__CompanyId`
- `Procore__ClientId`
- `Procore__ClientSecret`
- `Procore__RefreshToken`

## Token management guidance

- Use refresh tokens to obtain short-lived access tokens.
- Never log raw tokens.
- Store secrets only in Key Vault/secure secret stores.
- Rotate client secrets and refresh tokens on a defined cadence.

## Connectivity validation checklist

- OAuth token exchange succeeds.
- Can query project list for configured company.
- Can access target project resources used by workflow.
- API rate-limit headers are captured and monitored.

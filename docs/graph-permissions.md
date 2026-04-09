# Microsoft Graph permissions for SharePoint file operations

The integration that reads/writes SharePoint documents requires Microsoft Graph scopes. Exact permissions depend on whether your app uses delegated user context or application permissions.

## Minimum recommended permissions

### Application permissions (daemon/service-to-service)

- `Sites.ReadWrite.All` — read/write files and metadata across site collections the app can access.
- `Files.ReadWrite.All` — file-level read/write operations across drives.

> Prefer `Sites.Selected` instead of broad tenant-wide access where possible.

If using `Sites.Selected`:
- grant `Sites.Selected` in app registration,
- then grant per-site access using Graph/SharePoint admin tooling.

### Delegated permissions (user signed-in flow)

- `Files.ReadWrite`
- `Sites.ReadWrite.All` (or narrower alternatives depending on UI workflow)
- `offline_access` (if refresh tokens are needed)

## Typical operations and associated scope needs

- Locate site/drive by ID: `Sites.Read.All` or broader.
- Read source document: `Files.Read`/`Files.Read.All` (delegated) or `Files.Read.All` (application).
- Upload/update finalized document: `Files.ReadWrite`/`Files.ReadWrite.All`.
- Create folders: `Files.ReadWrite.All`.

## Admin consent and governance

1. Add required Graph permissions in Entra app registration.
2. Grant admin consent for application permissions.
3. Validate token contains expected `roles` (application) or `scp` (delegated) claims.
4. Restrict access with Conditional Access and least privilege.

## Validation checklist

- Can list target site/drive.
- Can read source template and metadata.
- Can upload and overwrite target work instruction file.
- Can handle 401/403 errors with clear diagnostics.

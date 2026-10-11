# Deployment Behaviour

`PublishToRailway` is a deploy-time integration. It does nothing during local AppHost model construction beyond attaching metadata and a deploy pipeline step.

During `aspire deploy`, the package:

1. Resolves service name, project id, environment id/name, and API token.
2. Resolves a Railway environment name such as `production` to its environment id when needed.
3. Looks up the Railway service by name.
4. Applies the selected ownership mode.
5. Creates a Railway PostgreSQL service from the configured Railway template when needed.
6. Waits for Railway connection variables.
7. Reconciles configured Railway service settings, resource limits, and PostgreSQL shared memory.
8. Redeploys the Railway PostgreSQL service when a requested region change is not already reflected by the latest Railway deployment manifest.
9. Creates or reuses an active public TCP provisioning endpoint when `EnablePublicProvisioningEndpoint` is enabled.
10. Creates missing Aspire child databases inside the Railway PostgreSQL service.
11. Populates PostgreSQL connection strings and supplementary outputs.
12. Saves the remote Railway service identity for repeated deploys.

The deploy step is named `railway-postgres-<resource-name>`.

Connection outputs prefer `DATABASE_PUBLIC_URL`, a public `DATABASE_URL`, or Railway TCP proxy variables when Railway exposes them. If no public endpoint is available, the package falls back to PostgreSQL host variables such as `PGHOST`, `PGPORT`, `PGUSER`, `PGPASSWORD`, and `PGDATABASE`.

`EnablePublicProvisioningEndpoint` defaults to `false`, preserving existing deployments without additional proxy calls. Enable it for deployment from a workstation or CI runner outside Railway's private network. The package verifies the proxy belongs to the selected service and environment and targets port 5432. An incompatible or ambiguous proxy fails before database setup. A newly created or inactive proxy triggers a service redeployment; activation requires a different successful deployment identity and an authoritative active proxy readback within two minutes. Database setup uses the returned public domain and proxy port without waiting for Railway's reserved environment variables to update.

The proxy remains enabled after deployment. Repeated deploys reuse an active proxy without creating another or redeploying solely for proxy activation. Application-facing connection outputs keep the selection rules above. The package does not remove the endpoint; its lifecycle remains under your control in Railway.

The package does not delete Railway services. `CreateOnly`/`ExistingOnly` failures are intentional guardrails against accidentally adopting or replacing the wrong remote service.

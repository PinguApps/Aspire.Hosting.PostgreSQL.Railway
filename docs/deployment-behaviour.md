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
9. Creates missing Aspire child databases inside the Railway PostgreSQL service.
10. Populates PostgreSQL connection strings and supplementary outputs.
11. Saves the remote Railway service identity for repeated deploys.

The deploy step is named `railway-postgres-<resource-name>`.

The package does not delete Railway services. `CreateOnly`/`ExistingOnly` failures are intentional guardrails against accidentally adopting or replacing the wrong remote service.

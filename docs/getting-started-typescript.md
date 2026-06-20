# TypeScript AppHost Usage

After adding the NuGet package to `aspire.config.json`, run:

```powershell
aspire restore --non-interactive
```

Then use the generated Aspire module:

```ts
import {
  createBuilder,
  RailwayPostgresRestartPolicy,
  railwayPostgresOwnershipMode,
} from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const serviceName = await builder.addParameter("railway-postgres-service-name");
const projectId = await builder.addParameter("railway-project-id");
const environmentId = await builder.addParameter("railway-environment-id");
const apiToken = await builder.addParameter("railway-api-token", { secret: true });

let postgres = await builder.addPostgres("postgres");
postgres = await postgres.publishToRailway(serviceName, projectId, environmentId, apiToken, {
  ownershipMode: railwayPostgresOwnershipMode.createOrAdopt,
  region: "europe-west4-drams3a",
  restartPolicy: RailwayPostgresRestartPolicy.OnFailure,
  restartPolicyMaxRetries: 10,
  memoryGB: 2,
  vCpus: 1,
  sharedMemoryBytes: 524288000,
});

const orders = await postgres.addDatabase("orders");

let worker = await builder.addContainer("worker", "mcr.microsoft.com/dotnet/runtime-deps:10.0");
worker = await worker.withReference(orders);

const app = await builder.build();
await app.run();
```

For deploy:

```powershell
$env:Parameters__railway_postgres_service_name = $env:RAILWAY_POSTGRES_SERVICE_NAME
$env:Parameters__railway_project_id = $env:RAILWAY_PROJECT_ID
$env:Parameters__railway_environment_id = $env:RAILWAY_ENVIRONMENT_ID
$env:Parameters__railway_api_token = $env:RAILWAY_API_TOKEN
aspire deploy --non-interactive
```

Supplementary outputs are available from the PostgreSQL server resource:

```ts
const outputs = await postgres.getRailwayPostgresOutputs();
const host = await outputs.host();
const password = await outputs.password();
```

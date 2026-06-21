import {
  createBuilder,
  RailwayPostgresRegions,
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
  region: RailwayPostgresRegions.EuWestMetal,
  restartPolicy: RailwayPostgresRestartPolicy.OnFailure,
  restartPolicyMaxRetries: 10,
  memoryGB: 2,
  vCpus: 1,
  sharedMemoryBytes: 524288000,
  pointInTimeRecovery: true,
});

const orders = await postgres.addDatabase("orders");

let worker = await builder.addContainer("worker", "mcr.microsoft.com/dotnet/runtime-deps:10.0");
worker = await worker.withReference(orders);

const app = await builder.build();
await app.run();

import {
  createBuilder,
  railwayPostgresCloudPlatform,
  railwayPostgresOwnershipMode,
  railwayPostgresPlan,
  railwayPostgresRegion,
} from "./.aspire/modules/aspire.mjs";

const builder = await createBuilder();

const databaseName = await builder.addParameter("railway-database-name");
const accountEmail = await builder.addParameter("railway-account-email");
const apiKey = await builder.addParameter("railway-api-key", { secret: true });

let cache = await builder.addRedis("cache");
cache = await cache.publishToRailway(databaseName, accountEmail, apiKey, {
  ownershipMode: railwayPostgresOwnershipMode.createOrAdopt,
  platform: railwayPostgresCloudPlatform.aws,
  primaryRegion: railwayPostgresRegion.awsEuWest1,
  plan: railwayPostgresPlan.payAsYouGo,
  eviction: true,
});

let worker = await builder.addContainer("worker", "mcr.microsoft.com/dotnet/runtime-deps:10.0");
worker = await worker.withReference(cache);

const app = await builder.build();
await app.run();

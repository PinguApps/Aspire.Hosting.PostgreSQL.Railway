# Overview

This package adds a deploy-time Railway PostgreSQL publishing path for Aspire PostgreSQL resources.

The AppHost still models PostgreSQL with Aspire's built-in resources:

```csharp
IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres");
IResourceBuilder<PostgresDatabaseResource> orders = postgres.AddDatabase("orders");
```

Local runs use the normal Aspire PostgreSQL resource. Railway is only contacted by the deploy pipeline after `PublishToRailway(...)` has been attached and `aspire deploy` runs.

During deploy, the integration:

1. Resolves Aspire parameters.
2. Locates the configured Railway service by name.
3. Applies the selected ownership mode.
4. Creates the Railway PostgreSQL service from Railway's PostgreSQL template when allowed.
5. Waits for Railway PostgreSQL connection variables.
6. Creates child Aspire databases inside the Railway PostgreSQL server.
7. Redirects Aspire PostgreSQL connection strings to Railway.

The Railway API token is infrastructure-only and is not exposed to application projects.

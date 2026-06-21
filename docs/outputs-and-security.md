# Outputs And Security

Application projects should normally consume PostgreSQL through Aspire references:

```csharp
builder.AddProject<Projects.Api>("api")
    .WithReference(orders)
    .WaitFor(postgres);
```

After deploy, the server and child database connection strings resolve to Railway PostgreSQL.
For C# AppHosts, the package's Railway-aware `WithReference` overload keeps Azure App Service from trying to resolve the Railway PostgreSQL resource as Azure-managed infrastructure.

Connection strings use Railway's public PostgreSQL URL or TCP proxy when available, otherwise Railway's PostgreSQL host variables. Standard and PITR template connection strings use `Ssl Mode=Require`; PostGIS, pgvector, and TimescaleDB template connection strings use `Ssl Mode=Disable`.

Supplementary Railway outputs are available on the server resource:

```csharp
RailwayPostgresOutputs outputs = postgres.Resource.GetRailwayPostgresOutputs();
```

```ts
const outputs = await postgres.getRailwayPostgresOutputs();
const host = await outputs.host();
const password = await outputs.password();
```

| Output | Secret |
| --- | --- |
| ServiceId | No |
| Host | No |
| Port | No |
| UserName | No |
| Password | Yes |
| DatabaseName | No |
| ConnectionString | Yes |

The Railway API token is never exposed as an app-facing output or connection property.

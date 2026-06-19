# Outputs And Security

Application projects should normally consume PostgreSQL through Aspire references:

```csharp
builder.AddProject<Projects.Api>("api")
    .WithReference(orders);
```

After deploy, the server and child database connection strings resolve to Railway PostgreSQL.

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

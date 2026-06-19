# Configuration

Every publish call needs the remote database name, Railway account email, Railway Management API key, and ownership mode.

## Required Inputs

| Input | Secret | Purpose |
| --- | --- | --- |
| `railway-database-name` | No | Stable remote Railway database identity. |
| `railway-account-email` | No | Railway account email used by deployment infrastructure. |
| `railway-api-key` | Yes | Railway Management API key used by deployment infrastructure. |

For non-interactive TypeScript deploys, the validated path is to provide these values as Aspire parameter environment variables:

```powershell
$env:Parameters__railway_database_name = "orders-cache"
$env:Parameters__railway_account_email = $env:RAILWAY_EMAIL
$env:Parameters__railway_api_key = $env:RAILWAY_API_KEY
```

## Ownership Modes

| Mode | Missing database | Existing compatible database | Existing incompatible database |
| --- | --- | --- | --- |
| `CreateOrAdopt` / `createOrAdopt` | Create | Adopt | Fail |
| `CreateOnly` / `createOnly` | Create | Fail unless it is the cached verified identity for the same configured name | Fail |
| `ExistingOnly` / `existingOnly` | Fail | Adopt | Fail |

Use `CreateOrAdopt` as the usual starting point. Use `ExistingOnly` when database lifecycle is owned outside the AppHost.

## Create-Time Settings

Creation paths require:

| Setting | C# | TypeScript | Notes |
| --- | --- | --- | --- |
| Platform | `options.SetPlatform(RailwayPostgresCloudPlatform.Aws)` | `platform: railwayPostgresCloudPlatform.aws` | `aws` or `gcp`. |
| Primary region | `options.SetPrimaryRegion(RailwayPostgresRegion.AwsEuWest1)` | `primaryRegion: railwayPostgresRegion.awsEuWest1` | Must match the platform. |

Changing platform or primary region for an existing database is unsafe drift and fails.

## Mutable Settings

These settings are reconciled only when explicitly configured:

| Setting | C# | TypeScript |
| --- | --- | --- |
| Read regions | `options.SetReadRegions(RailwayPostgresRegion.AwsEuWest2)` | `readRegions: [railwayPostgresRegion.awsEuWest2]` |
| Plan | `options.SetPlan(RailwayPostgresPlan.PayAsYouGo)` | `plan: railwayPostgresPlan.payAsYouGo` |
| Budget | `options.SetBudget(20)` | `budget: 20` |
| Eviction | `options.Eviction = true` | `eviction: true` |

Leaving a mutable setting unset means "do not manage this provider setting."

## TLS Contract

Railway PostgreSQL is treated as TLS-on for v1. `Tls = false` in C# or `tls: false` in TypeScript is rejected. Set it to `true` only when you want the requirement stated explicitly.

## Supported Values

TypeScript uses generated value catalogs:

- `railwayPostgresOwnershipMode.createOrAdopt`, `createOnly`, `existingOnly`
- `railwayPostgresCloudPlatform.aws`, `gcp`
- `railwayPostgresPlan.free`, `payAsYouGo`, `fixed250Mb`, `fixed1Gb`, `fixed5Gb`, `fixed10Gb`, `fixed50Gb`, `fixed100Gb`, `fixed500Gb`
- `railwayPostgresRegion.awsUsEast1`, `awsUsEast2`, `awsUsWest1`, `awsUsWest2`, `awsCaCentral1`, `awsEuCentral1`, `awsEuWest1`, `awsEuWest2`, `awsSaEast1`, `awsApSouth1`, `awsApNortheast1`, `awsApSoutheast1`, `awsApSoutheast2`, `awsAfSouth1`, `gcpUsCentral1`, `gcpUsEast4`, `gcpEuropeWest1`, `gcpAsiaNortheast1`

C# has matching enum members. Literal strings are also supported through `RailwayPostgresValue` for C# advanced scenarios.

Read regions are currently limited to AWS regions except `AwsAfSouth1`.

# Retained PostgreSQL region-map verification

Verified on 11 October 2026 for the VIK-57 production deployment.

## Observed provider behaviour

Package 1.1.4 accepted EU West Metal deployment metadata containing
`europe-west4-drams3a`, but still submitted a service-instance update changing the
region map to `ams`. No database redeployment was queued and the database volume
and latest deployment remained unchanged. After this write, the existing public
PostgreSQL TCP proxy stopped completing TLS negotiation. Restoring the original
`europe-west4-drams3a` region map immediately restored TLS and read-only SQL.

The restored database contained all 11 application migrations, two stores, and
zero runs or packs. No database recreation, data recovery, or application email
send was performed. This observation concerns this retained service; it does not
establish that every Railway service will react the same way to an alias rewrite.

## Automated regression coverage

- Release solution build: zero warnings and errors.
- Fourteen region-preservation cases failed against the original implementation
  and passed with the fix. They cover scalar and multi-region metadata, four
  provider aliases, an exact match, account and project-token authentication, and
  restart-policy updates that omit both region fields.
- Full contract suite: 90 passed, one live-credentials test skipped. Existing
  tests still reject different volume-backed regions and exercise new-service
  region configuration.

## Packed package with live provider reads

A separate .NET 10 consumer referenced the actual NuGet packages, not a project
reference. It used the production environment's scoped project token and read
the retained service's latest deployment metadata from Railway. A constrained
HTTP handler allowed only scope and metadata reads to reach Railway; it
intercepted service-instance mutations and rejected all other operations.

| Observation | Published 1.1.4 | Candidate 9999.0.58 |
| --- | --- | --- |
| Scope reads | 1 | 1 |
| Live deployment-metadata reads | 1 | 1 |
| Volume-backed `europe-west4-drams3a` confirmed | Yes | Yes |
| Rejected as migration | No | No |
| Intercepted service-instance updates | 1, requesting `ams` | 0 |
| Deployment queued | No | No |
| Actual provider writes | 0 | 0 |

Candidate source commit: `b25b9f51eb29e7bd536fa48d4594c9cd3a8cd150`.

Candidate artifact: `PinguApps.Aspire.Hosting.PostgreSQL.Railway.9999.0.58.nupkg`.
SHA-256: `4df3a7acdf53fc00890e3ad025ee3b0541c7620d6ebfe137ccfdf2ffd53b5381`.

This proves the packed candidate's decision and outgoing request behaviour with
real retained-service metadata. It is not a production deployment of the
candidate. The full consuming application's next Railway deployment remains
pending publication and adoption of the fix. The production web deployment was
stopped by its verification-recovery workflow; delivery remains disabled.

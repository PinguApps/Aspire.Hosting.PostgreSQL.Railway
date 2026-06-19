Feature: Publish Redis to Railway

  Scenario: Marking a Redis resource for Railway keeps the standard Redis resource model
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway database "orders-cache"
    Then the resource remains a standard Aspire Redis resource
    And the resource is excluded from publish
    And the resource has Railway deployment metadata for database "orders-cache"
    And mutating captured callback options cannot mutate deployment metadata
    And the explicit setting snapshot cannot mutate deployment metadata
    And mutating the configured read regions cannot mutate deployment metadata
    And the resource keeps the standard Redis connection properties

  Scenario: Reconfiguring a Redis resource for Railway replaces deployment intent
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway database "orders-cache"
    And the Redis resource is marked for Railway database "updated-orders-cache"
    Then the resource has Railway deployment metadata for database "updated-orders-cache"
    And the resource has one Railway deployment pipeline step

  Scenario Outline: Marking a Redis resource for Railway captures the requested ownership mode
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway database "orders-cache" with ownership mode "<ownershipMode>"
    Then the resource has Railway ownership mode "<ownershipMode>"

    Examples:
      | ownershipMode  |
      | CreateOnly     |
      | ExistingOnly   |
      | CreateOrAdopt  |

  Scenario Outline: PublishToRailway overloads capture equivalent deployment intent
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway through the "<overload>" overload
    Then the resource has Railway ownership mode "ExistingOnly"
    And the Railway deployment metadata matches the "<overload>" overload
    And the fluent API returns the same Redis resource builder

    Examples:
      | overload                                   |
      | literal database and parameter credentials |
      | parameter database and parameter credentials |
      | literal deployment values                  |

  Scenario: Marking a Redis resource for Railway supports parameter-based inputs
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with parameter-based inputs
    Then the resource stores parameter references for the required Railway inputs
    And the resource stores parameter references for optional Railway inputs
    And the provider domain preserves parameter-backed option sources

  Scenario: Marking a Redis resource for Railway maps typed domain values to provider payload values
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with typed domain options
    Then the provider domain maps the typed options to Railway payload values
    And the provider domain preserves explicit settings for reconcile

  Scenario: TypeScript publish bridge captures DTO deployment intent
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway through the TypeScript bridge with DTO options
    Then the resource stores parameter references for the required Railway inputs
    And the TypeScript DTO deployment metadata maps to provider payload values
    And the TypeScript output bridge returns the supplementary Railway PostgreSQL outputs
    And the fluent API returns the same Redis resource builder

  Scenario: TypeScript publish bridge rejects disabled TLS
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway through the TypeScript bridge with disabled TLS
    Then the Railway configuration fails with "InvalidOperationException"
    And the Railway configuration failure message contains "requires TLS"

  Scenario: TypeScript export metadata matches the approved contract
    Then the TypeScript export metadata matches the approved Railway PostgreSQL contract

  Scenario: Marking a Redis resource for Railway preserves explicit option intent
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with an explicitly unset primary region
    Then the Railway state distinguishes the explicitly unset primary region from an unspecified plan

  Scenario: Marking a Redis resource for Railway rejects a blank database name
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for a blank Railway database name
    Then the Railway configuration fails with "ArgumentException"

  Scenario: Marking a Redis resource for Railway rejects a missing API key value
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with a missing API key value
    Then the Railway configuration fails with "ArgumentNullException"

  Scenario: Marking a Redis resource for Railway rejects an unsupported ownership mode
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with an unsupported ownership mode
    Then the Railway configuration fails with "ArgumentOutOfRangeException"

  Scenario: Marking a Redis resource for Railway rejects disabled TLS
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with disabled TLS
    Then the Railway configuration fails with "InvalidOperationException"
    And the Railway configuration failure message contains "requires TLS"

  Scenario: Marking a Redis resource for Railway rejects an unsupported platform
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with unsupported platform
    Then the Railway configuration fails with "InvalidOperationException"
    And the Railway configuration failure message contains "platform 'azure' is not supported"

  Scenario: Marking a Redis resource for Railway rejects mismatched platform and primary region
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with mismatched platform and primary region
    Then the Railway configuration fails with "InvalidOperationException"
    And the Railway configuration failure message contains "primary region 'us-central1' is a gcp region and cannot be used with platform 'aws'"

  Scenario: Marking a Redis resource for Railway rejects budget on fixed plans
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with a fixed plan budget
    Then the Railway configuration fails with "InvalidOperationException"
    And the Railway configuration failure message contains "budget can only be configured with the pay-as-you-go plan"

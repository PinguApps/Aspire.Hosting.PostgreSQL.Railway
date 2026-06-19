Feature: Redis reference behaviour

  Scenario: Plain Redis local usage has no Railway deployment activity
    Given a standard Aspire Redis resource named "cache"
    And a consuming container references the Redis resource
    Then the resource remains a standard Aspire Redis resource
    And the resource keeps the standard Redis connection properties
    And the Redis reference chain is configured for the consuming container
    And the resource has no Railway deployment metadata
    And the resource has no Railway deployment pipeline step
    And the resource has no supplementary Railway PostgreSQL outputs

  Scenario: Marking a Redis resource for Railway still allows normal references
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway database "orders-cache"
    And a consuming container references the Redis resource
    Then the Redis reference chain is configured for the consuming container

  Scenario: Marking a Redis resource for Railway is a local no-op until deploy
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with literal management credentials
    And a consuming container references the Redis resource
    Then the Redis reference chain is configured for the consuming container
    And the Redis resource has no Railway connection output
    And the Redis connection properties still use the standard Redis surface
    And the fake Railway provider has no recorded interactions
    And the app-facing Redis outputs and references do not contain "management-secret"

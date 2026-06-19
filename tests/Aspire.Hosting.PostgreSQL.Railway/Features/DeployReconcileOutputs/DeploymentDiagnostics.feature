Feature: Railway PostgreSQL deployment diagnostics

  Scenario: Deployment progress reports create phases in order
    Given an Railway diagnostic deployment for database "orders-cache"
    And the Railway diagnostic provider has no existing database
    When the Railway diagnostic deployment pipeline runs
    Then the Railway diagnostic progress phases are:
      | phase                     |
      | ResolvingConfiguration    |
      | LocatingDatabase          |
      | LocatingDatabase          |
      | ValidatingImmutableDrift  |
      | CreatingDatabase          |
      | CreatingDatabase          |
      | ReconcilingMutableSettings |
      | RetrievingOutputs         |

  Scenario: Deployment progress reports adopt phases with provider identifiers
    Given an Railway diagnostic deployment for database "orders-cache"
    And the Railway diagnostic provider has existing database "orders-cache" with id "db-orders"
    When the Railway diagnostic deployment pipeline runs
    Then the Railway diagnostic progress phases are:
      | phase                     |
      | ResolvingConfiguration    |
      | LocatingDatabase          |
      | LocatingDatabase          |
      | ValidatingImmutableDrift  |
      | LocatingDatabase          |
      | ReconcilingMutableSettings |
      | RetrievingOutputs         |
    And the Railway diagnostic progress contains "Using existing Railway PostgreSQL database 'orders-cache' with provider id 'db-orders'"
    And the Railway diagnostic progress contains "Located Railway PostgreSQL database 'orders-cache' with provider id 'db-orders'"
    And the Railway diagnostic progress contains provider id "db-orders"

  Scenario: Deployment diagnostics redact secrets
    Given an Railway diagnostic deployment for database "orders-cache"
    And the Railway diagnostic provider has existing database "orders-cache" with id "db-orders"
    When the Railway diagnostic message "api-key-secret redis://default:redis-password@global-apt-1.railway.io:6379" is redacted
    Then the redacted Railway diagnostic message does not contain "api-key-secret"
    And the redacted Railway diagnostic message does not contain "redis-password"
    And the redacted Railway diagnostic message does not contain "redis://default:redis-password@global-apt-1.railway.io:6379"
    And the redacted Railway diagnostic message contains "[redacted]"

  Scenario: Deployment progress keeps failure context actionable
    Given an Railway diagnostic deployment for database "orders-cache"
    And the Railway diagnostic provider has existing database "orders-cache" with id "db-orders"
    And the Railway diagnostic provider fails plan mutations
    When the Railway diagnostic deployment pipeline is attempted
    Then the Railway diagnostic deployment failure message contains "Failed to reconcile Railway PostgreSQL database 'orders-cache' setting 'plan'"
    And the Railway diagnostic progress contains "Reconciling explicit mutable Railway PostgreSQL settings for database 'orders-cache'"

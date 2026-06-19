Feature: Live Railway deploy and output behavior

  @live-railway
  Scenario: Live deployment creates a disposable database
    Given a live disposable Railway PostgreSQL deployment with prefix "pin-171-deploy"
    When the live Railway deployment runs
    Then the live Railway database exists with the configured name
    And the live Railway database is registered for deletion

  @live-railway
  Scenario: Live repeat deployment adopts the same disposable database
    Given a live disposable Railway PostgreSQL deployment with prefix "pin-171-repeat"
    When the live Railway deployment runs twice
    Then both live Railway deployments returned the same provider id
    And only one live Railway database exists with the configured name
    And the live Railway database is registered for deletion

  @live-railway
  Scenario: Live deployment populates app-facing outputs
    Given a live disposable Railway PostgreSQL deployment with prefix "pin-171-output"
    When the live Railway deployment runs
    Then the live Redis connection string matches the provider details
    And the live supplementary Railway PostgreSQL outputs match the provider details
    And the live supplementary Railway PostgreSQL password output is secret
    And the live Railway database is registered for deletion

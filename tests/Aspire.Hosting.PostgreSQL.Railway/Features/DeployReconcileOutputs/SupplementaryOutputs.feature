Feature: Supplementary Railway PostgreSQL outputs

  Scenario: Deployment populates supplementary outputs from provider details
    Given an Railway PostgreSQL resource with supplementary outputs
    And the Railway deployment provider will create database "orders-cache" with id "db-orders"
    When the Railway deployment pipeline populates supplementary outputs
    Then the supplementary Railway PostgreSQL outputs are:
      | Name         | Value                    |
      | Endpoint     | global-apt-1.railway.io  |
      | Port         | 6379                     |
      | Password     | redis-password           |
      | Tls          | true                     |
      | DatabaseName | orders-cache             |
    And only the supplementary Railway PostgreSQL password output is secret
    And the Railway management API key is not surfaced as a supplementary output
    And the supplementary Railway PostgreSQL output names are stable
    And each supplementary Railway PostgreSQL output references the Redis resource

  Scenario: Missing provider passwords are rejected before supplementary outputs are populated
    Given an Railway PostgreSQL resource with supplementary outputs
    And the Railway deployment provider will create database "orders-cache" with id "db-orders" without a password
    When the Railway deployment pipeline attempts to populate supplementary outputs
    Then supplementary Railway PostgreSQL output population fails with provider kind "ProviderContract"
    And the supplementary Railway PostgreSQL output failure message contains "without credentials"
    And the Railway supplementary output provider did not attempt reset-password

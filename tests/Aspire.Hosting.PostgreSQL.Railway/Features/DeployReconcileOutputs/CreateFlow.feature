Feature: Deploy-time Railway create flow

  Scenario: Create ownership provisions a ready database with credentials
    Given an Railway create flow deployment for database "orders-cache"
    And ownership resolution selected create
    And the Railway create API returns database id "db-orders"
    And the Railway readiness API returns active database "orders-cache" with id "db-orders"
    When the Railway create flow executes
    Then the Railway create flow creates the database
    And the Railway create request payload is:
      | Property       | Value        |
      | DatabaseName   | orders-cache |
      | Platform       | aws          |
      | PrimaryRegion  | eu-west-1    |
      | Plan           | payg         |
      | Budget         | 360          |
      | Eviction       | true         |
      | Tls            | true         |
    And the Railway create request read regions are "eu-west-2"
    And the Railway create flow returns Redis credentials for database "orders-cache"

  Scenario: Create flow waits for the created database to become ready
    Given an Railway create flow deployment for database "orders-cache"
    And ownership resolution selected create
    And the Railway create API returns database id "db-orders"
    And the Railway readiness API returns active database "orders-cache" with id "db-orders"
    When the Railway create flow executes
    Then the Railway create flow waits for database "db-orders"
    And the Railway create flow returns remote identity database "orders-cache" with id "db-orders"

  Scenario: Create failure surfaces a clear deploy error
    Given an Railway create flow deployment for database "orders-cache"
    And ownership resolution selected create
    And the Railway create API fails with provider kind "Validation" and message "invalid region"
    When the Railway create flow is attempted
    Then the Railway create flow fails with "InvalidOperationException"
    And the Railway create flow failure message contains "Failed to create Railway PostgreSQL database 'orders-cache'"
    And the Railway create flow failure message contains "invalid region"

  Scenario: Missing provider credentials after create fails as a provider contract error
    Given an Railway create flow deployment for database "orders-cache"
    And ownership resolution selected create
    And the Railway create API returns database id "db-orders"
    And the Railway readiness API returns active database "orders-cache" with id "db-orders" without a password
    When the Railway create flow is attempted
    Then the Railway create flow fails with "RailwayPostgresProviderException"
    And the Railway create flow fails with provider kind "ProviderContract"
    And the Railway create flow failure message contains "without credentials"

  Scenario Outline: Missing connection fields after create fail as provider contract errors
    Given an Railway create flow deployment for database "orders-cache"
    And ownership resolution selected create
    And the Railway create API returns database id "db-orders"
    And the Railway readiness API returns active database "orders-cache" with id "db-orders" with invalid connection field "<Field>"
    When the Railway create flow is attempted
    Then the Railway create flow fails with "RailwayPostgresProviderException"
    And the Railway create flow fails with provider kind "ProviderContract"
    And the Railway create flow failure message contains "<Message>"

    Examples:
      | Field    | Message              |
      | endpoint | without an endpoint  |
      | port     | without a valid port |
      | tls      | with TLS disabled    |

  Scenario: Adopt ownership skips database creation
    Given an Railway create flow deployment for database "orders-cache"
    And ownership resolution selected adopt for database "orders-cache" with id "db-orders"
    When the Railway create flow executes
    Then the Railway create flow does not create the database
    And the Railway create flow returns Redis credentials for database "orders-cache"
    And the Railway create flow returns remote identity database "orders-cache" with id "db-orders"

  Scenario Outline: Missing connection fields after adopt fail as provider contract errors
    Given an Railway create flow deployment for database "orders-cache"
    And ownership resolution selected adopt for database "orders-cache" with id "db-orders" with invalid connection field "<Field>"
    When the Railway create flow is attempted
    Then the Railway create flow fails with "RailwayPostgresProviderException"
    And the Railway create flow fails with provider kind "ProviderContract"
    And the Railway create flow failure message contains "<Message>"

    Examples:
      | Field    | Message              |
      | password | without credentials  |
      | endpoint | without an endpoint  |
      | port     | without a valid port |
      | tls      | with TLS disabled    |

Feature: Railway PostgreSQL management client

  Scenario: Auth headers are constructed for management requests
    Given the Railway management API returns an empty database list
    When the Railway management client lists databases with account "pingu@example.com" and API key "secret-key"
    Then the Railway management request uses GET "/v2/redis/databases"
    And the Railway management request has the expected Basic auth header for account "pingu@example.com" and API key "secret-key"

  Scenario: Database details are parsed from credential-bearing responses
    Given the Railway management API returns database details for "orders-cache"
    When the Railway management client gets database "db-orders"
    Then the Railway management client returns database "orders-cache" with credentials

  Scenario: Database lookup by name uses list then detail fetch
    Given the Railway management API returns a list containing database "orders-cache"
    And the Railway management API returns database details for "orders-cache"
    When the Railway management client finds database "orders-cache" by name
    Then the Railway management client returns database "orders-cache" with credentials
    And the Railway management request sequence is:
      | Method | Path                        |
      | GET    | /v2/redis/databases         |
      | GET    | /v2/redis/database/db-orders |

  Scenario: Duplicate database names are surfaced as a provider contract failure
    Given the Railway management API returns duplicate databases named "orders-cache"
    When the Railway management client finds database "orders-cache" by name
    Then the Railway management client fails with provider kind "ProviderContract"

  Scenario: Detail lookup id drift is surfaced as a provider contract failure
    Given the Railway management API returns a list containing database "orders-cache"
    And the Railway management API returns database details for "orders-cache" with id "db-other"
    When the Railway management client finds database "orders-cache" by name
    Then the Railway management client fails with provider kind "ProviderContract"

  Scenario: Database creation sends the supported request body
    Given the Railway management API returns database details for "orders-cache"
    When the Railway management client creates database "orders-cache"
    Then the Railway management request uses POST "/v2/redis/database"
    And the Railway management request body contains:
      | Property       | Value        |
      | database_name  | orders-cache |
      | platform       | aws          |
      | primary_region | eu-west-1    |
      | plan           | payg         |
      | budget         | 50           |
      | eviction       | true         |
      | tls            | true         |

  Scenario: Mutable operations use the supported provider endpoints
    Given the Railway management API returns OK for five operations
    When the Railway management client updates mutable settings for database "db-orders"
    Then the Railway management request sequence is:
      | Method | Path                                  |
      | POST   | /v2/redis/update-regions/db-orders    |
      | POST   | /v2/redis/db-orders/change-plan       |
      | PATCH  | /v2/redis/update-budget/db-orders     |
      | POST   | /v2/redis/enable-eviction/db-orders   |
      | POST   | /v2/redis/disable-eviction/db-orders  |

  Scenario: Readiness polling returns when the database is active
    Given the Railway management API returns a modifying database then an active database
    When the Railway management client waits for database "db-orders" to become ready
    Then the Railway management client returns database "orders-cache" with credentials

  Scenario: Missing password is surfaced as a provider contract failure
    Given the Railway management API returns database details without a password
    When the Railway management client gets database "db-orders"
    Then the Railway management client fails with provider kind "ProviderContract"
    And the Railway management client did not request reset-password

  Scenario: Provider validation errors are classified and sanitized
    Given the Railway management API returns status 400 with error "invalid secret-key setting"
    When the Railway management client lists databases with account "pingu@example.com" and API key "secret-key"
    Then the Railway management client fails with provider kind "Validation"
    And the Railway management failure message does not contain "secret-key"

  Scenario: Provider auth failures are classified predictably
    Given the Railway management API returns status 401 with error "Unauthorized"
    When the Railway management client lists databases with account "pingu@example.com" and API key "secret-key"
    Then the Railway management client fails with provider kind "Authentication"

  Scenario Outline: Transport failures are classified as transient
    Given the Railway management API fails before responding with "<Failure>"
    When the Railway management client lists databases with account "pingu@example.com" and API key "secret-key"
    Then the Railway management client fails with provider kind "Transient"

    Examples:
      | Failure          |
      | RequestException |
      | Timeout          |

  Scenario Outline: General provider exceptions default to unexpected failures
    When a general Railway provider exception is created with constructor "<Constructor>"
    Then the Railway management client fails with provider kind "Unexpected"

    Examples:
      | Constructor     |
      | Parameterless   |
      | Message         |
      | MessageAndInner |

  Scenario: Request cancellation is preserved
    Given the Railway management API waits until cancellation
    When the Railway management client lists databases with a cancelled token
    Then the Railway management client operation is cancelled

Feature: Railway PostgreSQL remote identity

  Scenario: First deployment finds an existing database by configured name
    Given the Railway identity API returns a list containing database "orders-cache" with id "db-orders"
    And the Railway identity API returns details for database "orders-cache" with id "db-orders"
    When the Railway remote identity resolver resolves configured database "orders-cache"
    Then the Railway remote identity resolver returns database "orders-cache" with id "db-orders"
    And the Railway remote identity was not resolved from the cached identity
    And the Railway remote identity cache is database "orders-cache" with id "db-orders"
    And the Railway identity request sequence is:
      | Method | Path                        |
      | GET    | /v2/redis/databases         |
      | GET    | /v2/redis/database/db-orders |

  Scenario: First deployment reports no existing database when the configured name is absent
    Given the Railway identity API returns an empty database list
    When the Railway remote identity resolver resolves configured database "orders-cache"
    Then the Railway remote identity resolver returns no database
    And the Railway remote identity cache is empty
    And the Railway identity request sequence is:
      | Method | Path                |
      | GET    | /v2/redis/databases |

  Scenario: Repeated deployment reuses the cached provider id when the name still matches
    Given cached Railway remote identity is database "orders-cache" with id "db-orders"
    And the Railway identity API returns details for database "orders-cache" with id "db-orders"
    And the Railway identity API returns a list containing database "orders-cache" with id "db-orders"
    And the Railway identity API returns details for database "orders-cache" with id "db-orders"
    When the Railway remote identity resolver resolves configured database "orders-cache"
    Then the Railway remote identity resolver returns database "orders-cache" with id "db-orders"
    And the Railway remote identity was resolved from the cached identity
    And the Railway remote identity cache is database "orders-cache" with id "db-orders"
    And the Railway identity request sequence is:
      | Method | Path                        |
      | GET    | /v2/redis/database/db-orders |
      | GET    | /v2/redis/databases         |
      | GET    | /v2/redis/database/db-orders |

  Scenario: Cached identity still checks duplicate configured names before reuse
    Given cached Railway remote identity is database "orders-cache" with id "db-orders"
    And the Railway identity API returns details for database "orders-cache" with id "db-orders"
    And the Railway identity API returns duplicate databases named "orders-cache"
    When the Railway remote identity resolver resolves configured database "orders-cache"
    Then the Railway remote identity resolver fails with provider kind "ProviderContract"

  Scenario: Cached identity refuses a detail response with a different provider id
    Given cached Railway remote identity is database "orders-cache" with id "db-orders"
    And the Railway identity API returns details for database "orders-cache" with id "db-other"
    When the Railway remote identity resolver resolves configured database "orders-cache"
    Then the Railway remote identity resolver fails with provider kind "ProviderContract"
    And the Railway remote identity failure message contains "mismatched cached remote identity"

  Scenario: Explicit configured name changes select the new configured remote identity
    Given cached Railway remote identity is database "orders-cache" with id "db-orders"
    And the Railway identity API returns a list containing database "billing-cache" with id "db-billing"
    And the Railway identity API returns details for database "billing-cache" with id "db-billing"
    When the Railway remote identity resolver resolves configured database "billing-cache"
    Then the Railway remote identity resolver returns database "billing-cache" with id "db-billing"
    And the Railway remote identity cache is database "billing-cache" with id "db-billing"
    And the Railway identity request sequence is:
      | Method | Path                         |
      | GET    | /v2/redis/databases          |
      | GET    | /v2/redis/database/db-billing |

  Scenario: Duplicate configured names fail clearly
    Given the Railway identity API returns duplicate databases named "orders-cache"
    When the Railway remote identity resolver resolves configured database "orders-cache"
    Then the Railway remote identity resolver fails with provider kind "ProviderContract"

  Scenario: Detail lookup name drift fails clearly
    Given the Railway identity API returns a list containing database "orders-cache" with id "db-orders"
    And the Railway identity API returns details for database "renamed-cache" with id "db-orders"
    When the Railway remote identity resolver resolves configured database "orders-cache"
    Then the Railway remote identity resolver fails with provider kind "ProviderContract"

  Scenario: Cached identity refuses to take over a different provider id for the same configured name
    Given cached Railway remote identity is database "orders-cache" with id "db-orders"
    And the Railway identity API returns details for database "renamed-cache" with id "db-orders"
    And the Railway identity API returns a list containing database "orders-cache" with id "db-other"
    And the Railway identity API returns details for database "orders-cache" with id "db-other"
    When the Railway remote identity resolver resolves configured database "orders-cache"
    Then the Railway remote identity resolver fails with provider kind "ProviderContract"
    And the Railway remote identity failure message contains "Refusing to adopt a different database"

  Scenario: Remote identity state can be persisted in Aspire deployment state
    When the Railway remote identity cache for Redis resource "cache" is saved as database "orders-cache" with id "db-orders"
    Then the Railway remote identity cache for Redis resource "cache" loads database "orders-cache" with id "db-orders"

  Scenario: Missing remote identity state loads as empty cache
    Then the Railway remote identity cache for Redis resource "cache" is empty

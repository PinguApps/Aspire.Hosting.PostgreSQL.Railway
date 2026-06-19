Feature: Redis connection output

  Scenario: Deployed Railway PostgreSQL output resolves through the normal Redis reference
    Given a standard Aspire Redis resource named "cache"
    And a consuming container references the Redis resource
    When Railway PostgreSQL connection output is applied with endpoint "global-apt-1.railway.io", port 6379, password "redis-password", and TLS enabled
    Then the Redis connection string reference resolves to "global-apt-1.railway.io:6379,password=redis-password,ssl=true"
    And the Redis connection properties contain:
      | Name     | Value                                               |
      | Host     | global-apt-1.railway.io                            |
      | Port     | 6379                                                |
      | Password | redis-password                                      |
      | Uri      | rediss://:redis-password@global-apt-1.railway.io:6379 |
    And the Redis connection output does not contain "management-secret"

  Scenario: Railway publishing does not redirect local Redis connection output before deploy
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway database "orders-cache"
    Then the Redis resource has no Railway connection output
    And the Redis connection properties still use the standard Redis surface

  Scenario: Provider endpoint slugs are rejected for Redis connection output
    Given a standard Aspire Redis resource named "cache"
    When applying Railway PostgreSQL connection output with endpoint "global-apt-1" is attempted
    Then Railway PostgreSQL connection output fails with provider kind "ProviderContract"
    And the Railway PostgreSQL connection output failure message contains "complete host name"
    And the Redis resource has no Railway connection output

  Scenario: Missing provider endpoints are rejected for Redis connection output
    Given a standard Aspire Redis resource named "cache"
    When applying Railway PostgreSQL connection output without an endpoint is attempted
    Then Railway PostgreSQL connection output fails with provider kind "ProviderContract"
    And the Railway PostgreSQL connection output failure message contains "without an endpoint"
    And the Redis resource has no Railway connection output

  Scenario: Missing provider passwords are rejected for Redis connection output
    Given a standard Aspire Redis resource named "cache"
    When applying Railway PostgreSQL connection output without a password is attempted
    Then Railway PostgreSQL connection output fails with provider kind "ProviderContract"
    And the Railway PostgreSQL connection output failure message contains "without credentials"
    And the Redis resource has no Railway connection output

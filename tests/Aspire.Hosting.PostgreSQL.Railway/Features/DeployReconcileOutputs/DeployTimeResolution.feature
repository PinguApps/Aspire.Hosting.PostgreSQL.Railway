Feature: Deploy-time Railway parameter resolution

  Scenario: Required and optional parameter values resolve for deployment
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with resolvable parameter inputs
    And the Railway deployment inputs are resolved
    Then the resolved Railway deployment targets database "orders-cache"
    And the resolved Railway management credentials use account email "owner@example.com"
    And the resolved Railway deployment options contain the parameter values

  Scenario: Missing required parameter values fail clearly during deployment resolution
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with an unresolved API key parameter
    And resolving the Railway deployment inputs is attempted
    Then the Railway deployment resolution fails with "InvalidOperationException"
    And the Railway deployment resolution failure message contains "API key parameter 'railway-api-key'"

  Scenario: Missing pipeline context fails with argument validation
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with resolvable parameter inputs
    And executing the Railway deployment pipeline with a missing context is attempted
    Then the Railway deployment resolution fails with "ArgumentNullException"

  Scenario: Management API keys do not become app-facing Redis outputs
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with resolvable parameter inputs
    And the Railway deployment inputs are resolved
    Then the resolved Railway management API key is infrastructure-only
    And the resource keeps the standard Redis connection properties

  Scenario: Local model construction does not resolve deploy-only parameters
    Given a standard Aspire Redis resource named "cache"
    When the Redis resource is marked for Railway with an unresolved API key parameter
    Then the resource stores parameter references for the required Railway inputs
    And the resource keeps the standard Redis connection properties

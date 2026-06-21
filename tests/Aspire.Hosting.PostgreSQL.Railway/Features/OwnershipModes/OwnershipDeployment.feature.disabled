Feature: Railway PostgreSQL ownership deployment

  Scenario: Create-only creates a missing database through the deployment pipeline
    Given an Railway ownership deployment for database "orders-cache" with mode "CreateOnly"
    And the Railway ownership deployment provider has no database named "orders-cache"
    When the Railway ownership deployment pipeline runs
    Then the Railway ownership deployment succeeds using the "Create" path
    And the Railway ownership deployment saved remote identity database "orders-cache"
    And the Railway ownership deployment populated Redis outputs for database "orders-cache"

  Scenario: Create-only fails when an unmanaged database already exists
    Given an Railway ownership deployment for database "orders-cache" with mode "CreateOnly"
    And the Railway ownership deployment provider has database "orders-cache" with id "db-orders"
    When the Railway ownership deployment pipeline is attempted
    Then the Railway ownership deployment fails because "CreateOnlyDatabaseAlreadyExists"
    And the Railway ownership deployment failure message contains "already exists, but ownership mode is create-only"
    And the Railway ownership deployment did not create a database

  Scenario: Existing-only adopts an existing database through the deployment pipeline
    Given an Railway ownership deployment for database "orders-cache" with mode "ExistingOnly"
    And the Railway ownership deployment provider has database "orders-cache" with id "db-orders"
    When the Railway ownership deployment pipeline runs
    Then the Railway ownership deployment succeeds using the "Adopt" path
    And the Railway ownership deployment saved remote identity database "orders-cache"
    And the Railway ownership deployment populated Redis outputs for database "orders-cache"

  Scenario: Existing-only fails when the named database is missing
    Given an Railway ownership deployment for database "orders-cache" with mode "ExistingOnly"
    And the Railway ownership deployment provider has no database named "orders-cache"
    When the Railway ownership deployment pipeline is attempted
    Then the Railway ownership deployment fails because "ExistingOnlyDatabaseMissing"
    And the Railway ownership deployment failure message contains "does not exist, but ownership mode is existing-only"
    And the Railway ownership deployment did not create a database

  Scenario: Create-or-adopt creates a missing database through the deployment pipeline
    Given an Railway ownership deployment for database "orders-cache" with mode "CreateOrAdopt"
    And the Railway ownership deployment provider has no database named "orders-cache"
    When the Railway ownership deployment pipeline runs
    Then the Railway ownership deployment succeeds using the "Create" path
    And the Railway ownership deployment saved remote identity database "orders-cache"
    And the Railway ownership deployment populated Redis outputs for database "orders-cache"

  Scenario: Create-or-adopt adopts an existing database through the deployment pipeline
    Given an Railway ownership deployment for database "orders-cache" with mode "CreateOrAdopt"
    And the Railway ownership deployment provider has database "orders-cache" with id "db-orders"
    When the Railway ownership deployment pipeline runs
    Then the Railway ownership deployment succeeds using the "Adopt" path
    And the Railway ownership deployment saved remote identity database "orders-cache"
    And the Railway ownership deployment populated Redis outputs for database "orders-cache"

  Scenario Outline: Repeated deployments reuse the managed identity without recreating
    Given an Railway ownership deployment for database "orders-cache" with mode "<Mode>"
    And the Railway ownership deployment provider has no database named "orders-cache"
    When the Railway ownership deployment pipeline runs
    And the Railway ownership deployment pipeline runs again
    Then the Railway ownership deployment created 1 database
    And the Railway ownership deployment succeeded using the "Adopt" path
    And the Railway ownership deployment saved remote identity database "orders-cache"

    Examples:
      | Mode          |
      | CreateOnly    |
      | CreateOrAdopt |

  Scenario: Repeated existing-only deployments keep adopting the same managed identity
    Given an Railway ownership deployment for database "orders-cache" with mode "ExistingOnly"
    And the Railway ownership deployment provider has database "orders-cache" with id "db-orders"
    When the Railway ownership deployment pipeline runs
    And the Railway ownership deployment pipeline runs again
    Then the Railway ownership deployment created 0 databases
    And the Railway ownership deployment succeeded using the "Adopt" path
    And the Railway ownership deployment saved remote identity database "orders-cache"

  Scenario: Duplicate configured database names fail before ownership adoption
    Given an Railway ownership deployment for database "orders-cache" with mode "CreateOrAdopt"
    And the Railway ownership deployment provider has duplicate databases named "orders-cache"
    When the Railway ownership deployment pipeline is attempted
    Then the Railway ownership deployment fails with provider kind "ProviderContract"
    And the Railway ownership deployment failure message contains "more than one database named 'orders-cache'"
    And the Railway ownership deployment did not create a database

  Scenario: Cached identity refuses to adopt a different database for the same configured name
    Given an Railway ownership deployment for database "orders-cache" with mode "ExistingOnly"
    And cached Railway ownership deployment identity is database "orders-cache" with id "db-orders"
    And the Railway ownership deployment provider has database "renamed-cache" with id "db-orders"
    And the Railway ownership deployment provider has database "orders-cache" with id "db-other"
    When the Railway ownership deployment pipeline is attempted
    Then the Railway ownership deployment fails with provider kind "ProviderContract"
    And the Railway ownership deployment failure message contains "Refusing to adopt a different database"
    And the Railway ownership deployment did not create a database

  @live-railway
  Scenario: Live create-or-adopt creates an isolated database and registers deletion cleanup
    Given a live Railway ownership deployment for isolated database prefix "pin-170-create"
    When the live Railway ownership deployment runs with mode "CreateOrAdopt"
    Then the live Railway ownership deployment created a database
    And the live Railway ownership deployment registered delete cleanup

  @live-railway
  Scenario: Live existing-only adopts an isolated database and registers deletion cleanup
    Given a live Railway ownership deployment for isolated database prefix "pin-170-adopt"
    And the live Railway ownership provider has an isolated database to adopt
    When the live Railway ownership deployment runs with mode "ExistingOnly"
    Then the live Railway ownership deployment adopted the database
    And the live Railway ownership deployment registered delete cleanup

Feature: TypeScript-authored Railway deployment

  Scenario: TypeScript bridge deployment reuses the managed identity on repeat deploy
    Given a TypeScript-authored Railway PostgreSQL deployment for database "orders-cache"
    And the TypeScript deployment fake provider has no database named "orders-cache"
    When the TypeScript-authored Railway deployment pipeline runs twice
    Then the TypeScript-authored Railway deployment created 1 database
    And the TypeScript-authored Railway deployments returned the same provider id
    And the TypeScript-authored Railway deployment populated Redis outputs for database "orders-cache"

  @live-railway
  Scenario: Live TypeScript bridge deployment repeats against one disposable database
    Given a live TypeScript-authored Railway PostgreSQL deployment with prefix "pin-183-ts"
    When the live TypeScript-authored Railway deployment pipeline runs twice
    Then the live TypeScript-authored Railway deployments returned the same provider id
    And only one live TypeScript-authored Railway database exists with the configured name
    And the live TypeScript-authored Railway database is registered for deletion

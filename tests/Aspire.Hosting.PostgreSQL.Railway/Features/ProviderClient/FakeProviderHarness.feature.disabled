Feature: Fake Railway provider harness

  Scenario: Fake provider state is deterministic inside a scenario
    Given the fake Railway provider contains database "orders-cache" in region "eu-west-1"
    When the fake Railway provider is asked to find database "orders-cache"
    Then the fake Railway provider returns database "orders-cache"
    And the fake Railway provider recorded a "find-by-name" interaction for database "orders-cache"

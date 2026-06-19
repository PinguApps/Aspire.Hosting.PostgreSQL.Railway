Feature: Live Railway provider pattern

  @live-railway
  Scenario: Live scenarios require explicit Railway credentials
    Given live Railway credentials are available
    Then live Railway cleanup is registered through the shared cleanup path

  Scenario: Live cleanup rejects null cleanup actions
    When a null live Railway cleanup action is registered
    Then live Railway cleanup registration fails for a null cleanup action

  Scenario: Live cleanup runs every registered action before reporting failures
    Given live Railway cleanup action "first" fails
    And live Railway cleanup action "second" succeeds
    And live Railway cleanup action "third" fails
    When live Railway cleanup runs
    Then every live Railway cleanup action has run
    And live Railway cleanup reports 2 failures

  Scenario: Live disposable database names keep a suffix for long prefixes
    When live disposable database names are generated with prefix "pin-171-feedback-prefix-with-more-than-thirty-one-chars"
    Then each live disposable database name is at most 40 characters
    And each live disposable database name ends with an 8 character GUID suffix
    And the live disposable database names are unique

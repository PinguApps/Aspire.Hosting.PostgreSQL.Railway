Feature: Live Railway provider cleanup

  Scenario: Cleanup continues when a registered action fails
    Given live Railway cleanup has an older action registered
    And live Railway cleanup has a newer failing action registered
    When live Railway cleanup runs
    Then the older live Railway cleanup action ran
    And the live Railway cleanup failure is reported

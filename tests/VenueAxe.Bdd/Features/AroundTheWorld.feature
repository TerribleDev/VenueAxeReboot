Feature: Around The World Sequential Ring Target Progression
  As an axe thrower in an Around The World match
  I want to hit targets in exact sequential order (Ring 1 through 5, Bullseye, Clutch)
  So that I can advance milestones and win by completing all seven targets first

  Background:
    Given an Around The World match is initialized
    And around the world players "Alice" and "Bob" are enrolled

  Scenario: Hitting the required sequence target advances the player milestone
    Given "Alice" is targeting step 0 "Ring 1"
    When "Alice" hits "Ring1"
    Then "Alice" around the world score should be 1
    And "Alice" should now be targeting "Ring 2"

  Scenario: Hitting an out of order target does not advance the milestone
    Given "Alice" is targeting step 0 "Ring 1"
    When "Alice" hits "Bullseye"
    Then "Alice" around the world score should be 0
    And "Alice" should still be targeting "Ring 1"

  Scenario: Completing all seven target rings wins the match immediately
    Given "Alice" has completed 6 target rings
    When "Alice" hits "ClutchLeft"
    Then "Alice" around the world score should be 7
    And the around the world match should be finished
    And the around the world winner should be "Alice"

  Scenario: Undoing an Around The World throw reverts the milestone
    Given "Alice" is targeting step 0 "Ring 1"
    When "Alice" hits "Ring1"
    And the around the world throw is undone
    Then "Alice" around the world score should be 0
    And the around the world turn should be restored to "Alice"

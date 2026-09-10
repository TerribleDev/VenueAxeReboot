Feature: Countdown 301 Axe Throwing Game
  As an arcade axe thrower
  I want points deducted from my starting score of 301
  So that I can reach exactly zero without busting

  Background:
    Given a new Countdown 301 match is initialized
    And player "Dave" is enrolled with starting score 301

  Scenario: Valid throw reduces remaining score
    When "Dave" throws and scores 6 points
    Then "Dave" remaining score should be 295

  Scenario: Scoring more points than remaining triggers a bust
    Given "Dave" has a remaining score of 4
    When "Dave" throws and scores 5 points
    Then "Dave" should be marked as "Bust"
    And "Dave" remaining score should remain 4
    And the throw points should not be deducted

  Scenario: Exact zero checkout achieves victory
    Given "Dave" has a remaining score of 6
    When "Dave" throws and scores 6 points
    Then "Dave" remaining score should be 0
    And the match should declare "Dave" as the winner

Feature: WATL Official Standard Axe Throwing Match
  As a thrower and lane official
  I want match throws, bullseyes, and killshots scored according to official WATL regulations
  So that competitive integrity and game state are perfectly maintained

  Background:
    Given a new WATL standard match is initialized
    And players "Sarah" and "Marcus" are enrolled

  Scenario: Throwing a Bullseye scores six points
    When "Sarah" throws at coordinates 0.0 and 0.0
    Then the throw should be scored as a "Bullseye"
    And "Sarah" should receive 6 points
    And the turn should advance to "Marcus"

  Scenario: Calling Killshot and hitting awards eight points
    Given "Sarah" calls a Killshot on the "Left" side
    When "Sarah" throws at coordinates -0.38 and 0.46
    Then the throw should be scored as a "Left Killshot"
    And "Sarah" should receive 8 points
    And "Sarah" should have 1 Killshot attempt used

  Scenario: Hitting Killshot uncalled awards zero points per WATL regulations
    Given "Sarah" does not call a Killshot
    When "Sarah" throws at coordinates -0.38 and 0.46
    Then the throw should be scored as an "Uncalled Killshot"
    And "Sarah" should receive 0 points

  Scenario: Only two Killshot attempts are permitted per match
    Given "Sarah" calls a Killshot on the "Left" side
    And "Sarah" throws at coordinates -0.38 and 0.46
    And "Marcus" throws at coordinates 0.0 and 0.0
    And "Sarah" calls a Killshot on the "Right" side
    And "Sarah" throws at coordinates 0.38 and 0.46
    Then "Sarah" should have 2 Killshot attempts used
    And "Sarah" cannot call any further Killshots in this match

  Scenario: Undoing a throw reverts score and restores the previous turn
    When "Sarah" throws at coordinates 0.0 and 0.0
    And the lane master undoes the last throw
    Then "Sarah" should receive 0 points
    And the turn should be restored to "Sarah"

  Scenario: Throwing into Ring 5 scores five points
    When "Sarah" throws at coordinates 0.14 and 0.0
    Then the throw should be scored as "Ring 5"
    And "Sarah" should receive 5 points

  Scenario: Throwing into Ring 1 scores one point
    When "Sarah" throws at coordinates 0.45 and 0.0
    Then the throw should be scored as "Ring 1"
    And "Sarah" should receive 1 points

  Scenario: Throwing off target outside outer perimeter scores zero points
    When "Sarah" throws at coordinates 0.65 and 0.0
    Then the throw should be scored as a "Miss"
    And "Sarah" should receive 0 points


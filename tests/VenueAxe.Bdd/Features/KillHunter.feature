Feature: Kill Hunter Precision Axe Throwing Match
  As a competitive thrower in a Kill Hunter match
  I want only Bullseyes and Killshots to score points while outer rings score zero
  So that high-stakes precision is rewarded

  Background:
    Given a new Kill Hunter match is initialized
    And kill hunter players "Elena" and "Viktor" are enrolled

  Scenario: Bullseye scores six points in Kill Hunter
    When "Elena" throws at coordinates 0.0 and 0.0 in kill hunter
    Then "Elena" should receive 6 points in kill hunter
    And "Elena" streak should be 1

  Scenario: Called Killshot scores eight points in Kill Hunter
    Given "Elena" calls a Killshot in kill hunter
    When "Elena" throws at coordinates -0.38 and 0.46 in kill hunter
    Then "Elena" should receive 8 points in kill hunter
    And "Elena" streak should be 1

  Scenario: Hitting outer rings awards zero points in Kill Hunter
    When "Elena" throws at coordinates 0.22 and 0.0 in kill hunter
    Then "Elena" should receive 0 points in kill hunter
    And "Elena" streak should be 0

  Scenario: Undoing a throw in Kill Hunter reverts score and streak
    When "Elena" throws at coordinates 0.0 and 0.0 in kill hunter
    And the kill hunter throw is undone
    Then "Elena" should receive 0 points in kill hunter
    And "Elena" streak should be 0

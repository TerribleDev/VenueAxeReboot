Feature: Lane Hardware Terminal Pairing and Safety Emergency Stop
  As a venue lane master and operator
  I want to securely pair tablet and TV displays and instantly freeze lanes on safety emergencies
  So that customer safety and terminal reliability are guaranteed

  Background:
    Given an operating venue with lane "Lane 01"
    And "Lane 01" has tablet pairing pin "AX101" and TV pairing pin "TV101"

  Scenario: Lane Master pairs tablet terminal with valid PIN
    When a tablet attempts pairing with PIN "AX101"
    Then pairing should succeed with role "tablet" for "Lane 01"

  Scenario: Pairing attempt with invalid PIN is rejected
    When a tablet attempts pairing with PIN "WRONG99"
    Then pairing should be rejected with unauthorized error

  Scenario: Activating Safety Stop freezes lane and changes status to Maintenance
    Given "Lane 01" is currently active
    When the lane master triggers a safety stop on "Lane 01" with reason "Thrower crossed fault line"
    Then the lane status should be "Maintenance"
    And throws cannot be recorded on "Lane 01"

  Scenario: Clearing Safety Stop restores lane status to Available
    Given "Lane 01" is in "Maintenance" status
    When the lane master clears the safety stop on "Lane 01"
    Then the lane status should be "Available"

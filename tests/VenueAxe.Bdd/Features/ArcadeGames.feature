Feature: Arcade Axe Throwing Mini-Games
  As a casual guest
  I want interactive target games like Tic-Tac-Toe and Blackjack 21
  So that social throwing parties have engaging competitive mechanics

  Scenario: Axe Tic-Tac-Toe claims a 3x3 territory grid cell
    Given a new Axe Tic-Tac-Toe match is initialized
    And arcade players "X-Player" and "O-Player" are enrolled
    When "X-Player" hits cell index 4
    Then cell index 4 should be owned by "X-Player"
    And "O-Player" cannot claim already owned cell index 4

  Scenario: Axe Tic-Tac-Toe detects three in a row victory
    Given a new Axe Tic-Tac-Toe match is initialized
    And arcade players "X-Player" and "O-Player" are enrolled
    When "X-Player" hits cell index 0
    And "O-Player" hits cell index 3
    And "X-Player" hits cell index 1
    And "O-Player" hits cell index 4
    And "X-Player" hits cell index 2
    Then the Axe Tic-Tac-Toe match should be finished
    And the Axe Tic-Tac-Toe winner should be "X-Player"

  Scenario: Blackjack 21 detects bust when hand exceeds 21
    Given a new Blackjack 21 match is initialized
    And player "CardSharp" is enrolled with current hand total 18
    When "CardSharp" throws and draws a card worth 5
    Then "CardSharp" hand total should be 23
    And "CardSharp" hand should bust


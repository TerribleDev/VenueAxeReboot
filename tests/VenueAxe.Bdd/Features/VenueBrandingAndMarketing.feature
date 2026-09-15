Feature: Venue Branding and Marketing Preferences
  As a venue administrator and guest
  I want custom venue branding and guest marketing preferences respected
  So that brand presentation is consistent across devices and marketing consent is tracked

  Scenario: Venue icon dimension validation enforces square bounds
    Given a candidate venue icon with dimensions 512 by 512
    When the image dimension validator checks the asset
    Then the asset should be approved for upload

  Scenario: Non-square or oversized venue icon is rejected
    Given a candidate venue icon with dimensions 800 by 200
    When the image dimension validator checks the asset
    Then the asset should be rejected with a dimension error

  Scenario: Guest booking captures email marketing opt-in consent
    Given a guest makes a reservation with marketing opt-in set to "true"
    When the reservation is confirmed
    Then the booking marketing opt-in field should be recorded as "true"
    And the reservation export should indicate marketing preference as "Yes"

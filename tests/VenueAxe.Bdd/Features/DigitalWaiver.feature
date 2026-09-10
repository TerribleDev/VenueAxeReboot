Feature: Digital Waiver Legal Compliance and Minor Coverage
  As a venue risk and compliance officer
  I want electronically signed liability releases to capture tamper-evident audit trails
  And allow guardians to legally cover minor participants
  So that the venue is protected against commercial liability

  Scenario: Adult guest executes a verified digital waiver
    Given an active venue waiver template with legal text "Official Axe Release"
    When an adult guest "John" "Smith" signs the waiver
    Then the waiver record should have an immutable SHA-256 legal hash
    And the signer age should be verified as 18 or older
    And a downloadable PDF certificate can be generated

  Scenario: Parent executes digital waiver covering minor children
    Given an active venue waiver template with legal text "Official Axe Release"
    When a guardian "Jane" "Doe" signs the waiver
    And includes minor child "Billy Doe" born on "2015-04-12"
    Then the waiver record should indicate guardian signing is true
    And the covered minors data should contain "Billy Doe"

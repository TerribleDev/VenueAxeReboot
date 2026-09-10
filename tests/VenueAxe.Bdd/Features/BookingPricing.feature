Feature: Dynamic Booking Pricing and Contiguous Lane Allocation
  As a venue operations manager
  I want reservation pricing dynamically computed with peak rates and threshold discounts
  And lanes allocated contiguously across multi-bay groups
  So that venue revenue and lane density are maximized

  Scenario: Calculate standard booking price for party of four
    Given a venue booking configuration with base rate $35 per person per hour
    When a guest requests a booking for 4 throwers for 60 minutes
    Then the total amount should be 14000 cents

  Scenario: Large group threshold discount applies automatically
    Given a venue booking configuration with base rate $35 per person per hour
    And a volume discount rule of 10 percent off for parties of 10 or more
    When a guest requests a booking for 10 throwers for 60 minutes
    Then the gross total should be 35000 cents
    And the discount amount should be 3500 cents
    And the final net total should be 31500 cents

  Scenario: Allocate contiguous adjacent lanes for twelve throwers
    Given an arena with 6 available lanes numbered 1 through 6
    And each lane has a maximum capacity of 6 throwers
    When a party of 12 throwers is allocated lanes
    Then exactly 2 lanes should be assigned
    And the assigned lane numbers must be contiguous

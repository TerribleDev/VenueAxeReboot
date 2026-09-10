Feature: Booking Lifecycle, Deposit Calculation, and Cancellation
  As a customer and venue administrator
  I want coupon codes, deposit calculations, and cancellations handled accurately
  So that booking transactions and lane schedules remain consistent

  Scenario: Applying a fixed amount promo coupon reduces net total
    Given a booking with subtotal 20000 cents
    When the guest applies coupon "SAVE25" with fixed discount 2500 cents
    Then the calculated discount should be 2500 cents
    And the net balance due should be 17500 cents

  Scenario: Fifty percent deposit calculation on booking reservation
    Given a booking with net balance 24000 cents
    When the deposit rate is configured at 50 percent
    Then the required deposit amount should be 12000 cents
    And the remaining balance due at venue check-in should be 12000 cents

  Scenario: Booking cancellation releases lane reservation immediately
    Given a lane "Lane 03" is assigned to booking "BK-991"
    When booking "BK-991" is cancelled by staff
    Then booking "BK-991" status should be "Cancelled"
    And "Lane 03" should be free for subsequent bookings

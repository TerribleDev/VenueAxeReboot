namespace VenueAxe.Domain.Enums;

public enum UserRole
{
    SuperAdmin,
    Owner,
    Manager,
    LaneMaster
}

public enum LaneStatus
{
    Available,
    Reserved,
    Active,
    Expiring,
    Turnaround,
    Maintenance
}

public enum PricingModel
{
    PerPerson,
    PerLane,
    Tiered
}

public enum DepositType
{
    FullPayment,
    FixedDeposit,
    PerPersonDeposit,
    PayAtVenue
}

public enum BookingStatus
{
    Pending,
    Confirmed,
    CheckedIn,
    Completed,
    Cancelled
}

public enum SessionStatus
{
    Active,
    Paused,
    Completed
}

public enum MatchStatus
{
    InProgress,
    Finished,
    Aborted
}

public enum TargetZone
{
    Bullseye = 6,
    Ring5 = 5,
    Ring4 = 4,
    Ring3 = 3,
    Ring2 = 2,
    Ring1 = 1,
    ClutchLeft = 7,
    ClutchRight = 8,
    Miss = 0,
    Fault = 99
}

public enum ClutchSide
{
    Left,
    Right
}

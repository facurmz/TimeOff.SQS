namespace TimeOff.SQS.Common;

public record LeaveApplicationRequest (
    string Email,
    string Department,
    int LeaveDurationInHours,
    long LeaveStartDateTicks
);

public record LeaveApplicationResult (
    string Email,
    string Department,
    int LeaveDurationInHours,
    long LeaveStartDateTicks,
    string ProcessedBy,
    bool Approved
);
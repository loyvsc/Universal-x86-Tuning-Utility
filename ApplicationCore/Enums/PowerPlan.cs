namespace ApplicationCore.Enums;

public enum PowerPlan
{
    Unknown,
    SystemControlled = -1,
    PowerSave = 0,
    Balance = 1,
    HighPerformance = 2
}
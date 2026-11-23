namespace BoardGameCafe.Web.Domain.Rules;

public readonly record struct TimeRange(DateTime Start, DateTime End)
{
    public bool IsValid => End > Start && Start.Date == End.Date;
    
    public double DurationMinutes => (End - Start).TotalMinutes;
    
    public TimeRange WithPostBuffer(int bufferMinutes) => new(Start, End.AddMinutes(bufferMinutes));
    
    public bool Overlaps(TimeRange other) => Start < other.End && End > other.Start;
    
    public bool IsWithinWorkingHours(TimeSpan openingTime, TimeSpan closingTime) =>
        IsValid && Start.TimeOfDay >= openingTime && End.TimeOfDay <= closingTime;
}
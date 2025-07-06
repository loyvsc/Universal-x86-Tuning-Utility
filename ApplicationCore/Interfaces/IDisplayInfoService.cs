namespace ApplicationCore.Interfaces;

public interface IDisplayInfoService
{
    public IReadOnlyCollection<string> UniqueTargetScreenResolutions  { get; }
    public IReadOnlyCollection<int> UniqueTargetRefreshRates { get; }
    
    public void ApplySettings(int newHz);
}
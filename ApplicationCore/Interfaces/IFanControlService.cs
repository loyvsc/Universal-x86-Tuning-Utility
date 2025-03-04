namespace ApplicationCore.Interfaces;

public interface IFanControlService : IDisposable
{
    public int MaxFanSpeed { get; }
    public int MinFanSpeed { get; }
    public int MinFanSpeedPercentage { get; }
    public double FanSpeed { get; }
    public bool IsFanControlEnabled { get; }
    public bool IsFanEnabled { get; }
    
    public void UpdateAddresses();
    public void EnableFanControl();
    public void DisableFanControl();
    public void SetFanSpeed(int speedPercentage);
    public void ReadFanSpeed();
}
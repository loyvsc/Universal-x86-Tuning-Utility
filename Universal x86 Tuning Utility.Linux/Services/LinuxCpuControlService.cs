using ApplicationCore.Interfaces;

namespace Universal_x86_Tuning_Utility.Linux.Services;

public class LinuxCpuControlService : ICpuControlService
{
    public int CurrentPowerLimit { get; }
    public string CpuCommand { get; }
    public string CoCommand { get; }
    
    public void UpdatePowerLimit(int temperature, int cpuLoad, int maxPowerLimit, int minPowerLimit, int maxTemperature)
    {
        throw new System.NotImplementedException();
    }

    public void CurveOptimiserLimit(int cpuLoad, int maxCurveOptimiser)
    {
        throw new System.NotImplementedException();
    }
}
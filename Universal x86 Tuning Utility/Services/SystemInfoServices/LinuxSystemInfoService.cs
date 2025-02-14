using System.Collections.Generic;
using ApplicationCore.Enums;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using Microsoft.Extensions.Logging;

namespace Universal_x86_Tuning_Utility.Services.SystemInfoServices;

public class LinuxSystemInfoService : ISystemInfoService
{
    private readonly ILogger<LinuxSystemInfoService> _logger;

    public void AnalyzeSystem()
    {
        throw new System.NotImplementedException();
    }

    public LinuxSystemInfoService(ILogger<LinuxSystemInfoService> logger)
    {
        _logger = logger;
    }

    public int NvidiaGpuCount { get; }
    public int RadeonGpuCount { get; }
    public CpuInfo CpuInfo { get; set; }
    public string Manufacturer { get; }
    public string Product { get; }
    public string SystemName { get; }
    
    public bool IsGPUPresent(string gpuName)
    {
        throw new System.NotImplementedException();
    }

    public decimal GetBatteryRate()
    {
        throw new System.NotImplementedException();
    }

    public decimal ReadFullChargeCapacity()
    {
        throw new System.NotImplementedException();
    }

    public decimal ReadDesignCapacity()
    {
        throw new System.NotImplementedException();
    }

    public int GetBatteryCycle()
    {
        throw new System.NotImplementedException();
    }

    public decimal GetBatteryHealth()
    {
        throw new System.NotImplementedException();
    }

    public List<uint> GetCacheSize(CacheLevel level)
    {
        throw new System.NotImplementedException();
    }

    public string GetCodename()
    {
        throw new System.NotImplementedException();
    }

    public string GetBigLITTLE(int cores, double l2)
    {
        throw new System.NotImplementedException();
    }

    public string GetInstructionSets()
    {
        throw new System.NotImplementedException();
    }
}
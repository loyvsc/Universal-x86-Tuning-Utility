using ApplicationCore.Utilities;

namespace ApplicationCore.Models;

public class PremadePreset : Preset
{
    public string Name
    {
        get => _name;
        set => SetValue(ref _name, value);
    }

    public string RyzenAdjParameters
    {
        get => _ryzenAdjParameters;
        set => SetValue(ref _ryzenAdjParameters, value);
    }
    
    public string Description { get; set; }

    private string _name;
    private string _ryzenAdjParameters;
}
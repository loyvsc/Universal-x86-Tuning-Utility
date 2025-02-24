using Avalonia.Input.Platform;
using Avalonia.Platform;

namespace Universal_x86_Tuning_Utility.Interfaces;

public interface IPlatformServiceAccessor
{
    public IClipboard Clipboard { get; }
    public Screen? PrimaryScreen { get; }
}
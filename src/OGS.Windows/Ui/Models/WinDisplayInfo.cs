using CommunityToolkit.Mvvm.ComponentModel;
using TerraFX.Interop.Windows;

namespace OGS.Plaform.Windows.Models;

public sealed partial class WinDisplayInfo : ObservableObject
{
    public HMONITOR Handle { get; }

    [ObservableProperty] private string _name;

    public WinDisplayInfo(HMONITOR handle, string name)
    {
        Handle = handle;
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }
}
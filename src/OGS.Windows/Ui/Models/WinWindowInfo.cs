using CommunityToolkit.Mvvm.ComponentModel;
using TerraFX.Interop.Windows;

namespace OGS.Plaform.Windows.Models;

public sealed partial class WinWindowInfo : ObservableObject
{
    public HWND Handle { get; }
    [ObservableProperty] private string _name;

    public WinWindowInfo(HWND handle, string title)
    {
        Handle = handle;
        Name = title;
    }

    public override string ToString()
    {
        return Name;
    }
}
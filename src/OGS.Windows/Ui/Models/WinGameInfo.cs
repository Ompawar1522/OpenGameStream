using CommunityToolkit.Mvvm.ComponentModel;

namespace OGS.Plaform.Windows.Models;

public sealed partial class WinGameInfo : ObservableObject
{
    public uint Pid { get; }

    [ObservableProperty] private string _name;

    public WinGameInfo(uint pid, string name)
    {
        Pid = pid;
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }
}
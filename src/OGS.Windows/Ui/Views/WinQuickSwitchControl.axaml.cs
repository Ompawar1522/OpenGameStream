using Avalonia.Controls;
using Avalonia.Interactivity;
using OGS.Plaform.Windows.ViewModels;

namespace OGS.Plaform.Windows.Views;

public partial class WinQuickSwitchControl : UserControl
{
    public WinQuickSwitchControl()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (!Design.IsDesignMode && DataContext is WinQuickSwitchVm vm)
            vm.OnWindowLoaded();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        if (!Design.IsDesignMode && DataContext is WinQuickSwitchVm vm)
            vm.OnWindowUnloaded();
    }
}
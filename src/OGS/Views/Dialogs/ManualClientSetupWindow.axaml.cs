using Avalonia.Controls;
using Avalonia.Interactivity;
using OGS.ViewModels.Dialogs;

namespace OGS.Views.Dialogs;

public partial class ManualClientSetupWindow : Window
{
    public ManualClientSetupWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (DataContext is ManualClientSetupVm vm)
            vm.OnWindowLoad(this);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is ManualClientSetupVm vm)
            vm.OnWindowClosing();
    }
}
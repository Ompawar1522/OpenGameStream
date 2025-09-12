using Avalonia.Controls;
using Avalonia.Interactivity;
using OGS.ViewModels.Dialogs;

namespace OGS.Views.Dialogs;

public partial class CaptureSettingsWindow : Window
{
    public CaptureSettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (DataContext is CaptureSettingsWindowVm vm)
            vm.OnWindowLoad(this);
    }
}
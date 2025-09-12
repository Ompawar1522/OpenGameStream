using Avalonia.Controls;
using Avalonia.Interactivity;
using OGS.ViewModels;

namespace OGS.Views;

public partial class ClientSettingsWindow : Window
{
    public ClientSettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (DataContext is ClientSettingsWindowVm vm)
            vm.OnWindowLoad(this);
    }
}
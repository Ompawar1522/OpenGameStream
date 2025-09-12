using Avalonia.Controls;
using Avalonia.Interactivity;
using OGS.ViewModels;

namespace OGS.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if(DataContext is MainWindowViewModel vm)
        {
            vm.OnWindowLoaded(this);
        }
        
        base.OnLoaded(e);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.OnWindowClosing();

        base.OnClosing(e);
    }

    private void ClientUrlClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.OnClientUrlClicked();
        }
    }
}
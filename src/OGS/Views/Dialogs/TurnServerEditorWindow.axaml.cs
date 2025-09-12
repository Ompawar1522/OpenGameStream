using Avalonia.Controls;
using Avalonia.Interactivity;
using OGS.ViewModels.Dialogs;

namespace OGS.Views.Dialogs;

public partial class TurnServerEditorWindow : Window
{
    public TurnServerEditorWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (DataContext is TurnServerEditorVm vm)
            vm.OnWindowLoad(this);
    }
}



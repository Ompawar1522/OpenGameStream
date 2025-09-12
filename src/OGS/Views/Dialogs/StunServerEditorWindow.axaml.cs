using Avalonia.Controls;
using Avalonia.Interactivity;
using OGS.ViewModels.Dialogs;

namespace OGS.Views.Dialogs;

public partial class StunServerEditorWindow : Window
{
    public StunServerEditorWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (DataContext is StunServerEditorVm vm)
            vm.OnWindowLoad(this);
    }
}



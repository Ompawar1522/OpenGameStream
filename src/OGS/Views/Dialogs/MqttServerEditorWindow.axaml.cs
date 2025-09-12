using Avalonia.Controls;
using Avalonia.Interactivity;
using OGS.ViewModels.Dialogs;

namespace OGS.Views.Dialogs;

public partial class MqttServerEditorWindow : Window
{
    public MqttServerEditorWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (DataContext is MqttServerEditorVm vm)
            vm.OnWindowLoad(this);
    }
}



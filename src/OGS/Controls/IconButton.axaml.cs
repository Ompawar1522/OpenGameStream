using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material.Icons;

namespace OGS.Controls;

public partial class IconButton : UserControl
{
    public static readonly StyledProperty<MaterialIconKind> KindProperty =
        AvaloniaProperty.Register<IconButton, MaterialIconKind>(nameof(Kind), MaterialIconKind.Add);

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<IconButton, double>(nameof(IconSize), 16);

    public static readonly StyledProperty<IBrush> HoverBackgroundProperty =
        AvaloniaProperty.Register<IconButton, IBrush>(nameof(HoverBackground), Brush.Parse("#555555"));

    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<IconButton, ICommand>(nameof(Command));

    public static readonly StyledProperty<object> CommandParameterProperty =
        AvaloniaProperty.Register<IconButton, object>(nameof(CommandParameter));
    
    public static readonly StyledProperty<IBrush> HoverForegroundProperty = AvaloniaProperty.Register<IconButton, IBrush>(nameof(HoverForeground));
    
    public IBrush HoverForeground
    {
        get => GetValue(HoverForegroundProperty);
        set => SetValue(HoverForegroundProperty, value);
    }

    public MaterialIconKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public IBrush HoverBackground
    {
        get => GetValue(HoverBackgroundProperty);
        set => SetValue(HoverBackgroundProperty, value);
    }

    public ICommand Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public IconButton()
    {
        InitializeComponent();
        Width = 20;
        Height = 20;
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CarTrips.Views;

public partial class MyCarsPanel : UserControl
{
    public MyCarsPanel()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
using System.Windows;

namespace LayerOne.Safeguard.App;

/// <summary>Plain words for the child. Part of keeping Safeguard disclosed, not hidden.</summary>
public partial class KidsInfoWindow : Window
{
    public KidsInfoWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

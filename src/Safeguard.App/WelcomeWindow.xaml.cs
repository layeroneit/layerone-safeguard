using System.Windows;

namespace LayerOne.Safeguard.App;

public partial class WelcomeWindow : Window
{
    public bool Accepted { get; private set; }

    public WelcomeWindow()
    {
        InitializeComponent();
    }

    private void Accept_Changed(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = AcceptBox.IsChecked == true;
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        Accepted = true;
        DialogResult = true;
        Close();
    }
}

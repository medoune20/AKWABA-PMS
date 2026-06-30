using System.Windows;
using Akwaba.Desktop.ViewModels;

namespace Akwaba.Desktop;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _vm;

    public MainWindow(ShellViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
    }

    private async void BtnEnLigne_Click(object sender, RoutedEventArgs e)
        => await _vm.ConnexionEnLigneAsync(Mdp.Password);

    private async void BtnHorsLigne_Click(object sender, RoutedEventArgs e)
        => await _vm.DeverrouillerAsync(Mdp.Password);

    private void BtnDeco_Click(object sender, RoutedEventArgs e) => _vm.Deconnecter();
}

using System.Windows;
using System.Windows.Controls;
using Akwaba.Desktop.ViewModels;
namespace Akwaba.Desktop.Vues;
public partial class ReservationsView : UserControl
{
    public ReservationsView() => InitializeComponent();
    private void Basculer_Click(object sender, RoutedEventArgs e)
        => (DataContext as ReservationsViewModel)?.BasculerFormulaire();
}

using System.Windows.Controls;

namespace BakeryPOS.Views
{
    public partial class ProductsView : UserControl
    {
        public ProductsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            CodeInput.Focus();
        }

        private void ProductsGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (!BakeryPOS.Models.AppSession.IsAdmin)
            {
                e.Cancel = true; // Actively cancel edits for non-admins
            }
        }
    }
}

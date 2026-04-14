using System.Windows.Controls;

namespace BakeryPOS.Views
{
    public partial class PosView : UserControl
    {
        public PosView()
        {
            InitializeComponent();
            
            // Vincular los eventos de foco
            this.DataContextChanged += (s, e) => {
                if (DataContext is ViewModels.PosViewModel vm)
                {
                    vm.RequestSearchFocus += () => SearchBox.Focus();
                }
            };

            // Foco inicial al cargar la vista
            this.Loaded += (s, e) => SearchBox.Focus();
        }

        private void ApplyDiscount_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var viewModel = (BakeryPOS.ViewModels.PosViewModel)this.DataContext;
            viewModel.ApproveDiscount(AdminPinBox.Password);
            AdminPinBox.Password = string.Empty; // clear after try
        }
    }
}

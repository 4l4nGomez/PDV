using System.Windows;

namespace BakeryPOS.Views
{
    public partial class CheckoutView : Window
    {
        public bool Success { get; private set; }

        public CheckoutView(object viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Success = false;
            
            this.Loaded += (s, e) => {
                CashInput.Focus();
                CashInput.SelectAll();
            };

            this.MouseDown += (s, e) => {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                    this.DragMove();
            };
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.PosViewModel vm)
            {
                if (vm.CashReceivedNum < vm.TicketTotal)
                {
                    MessageBox.Show("El monto recibido es insuficiente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Success = true;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

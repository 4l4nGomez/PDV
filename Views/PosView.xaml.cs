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
                    vm.RequestCartFocus += () => {
                        if (TicketDataGrid.Items.Count > 0)
                        {
                            if (TicketDataGrid.SelectedItem == null)
                            {
                                TicketDataGrid.SelectedIndex = 0;
                            }
                            
                            TicketDataGrid.Focus();

                            // Forzar el foco del teclado directamente a la fila para control total
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new System.Action(() => {
                                var row = TicketDataGrid.ItemContainerGenerator.ContainerFromItem(TicketDataGrid.SelectedItem) as DataGridRow;
                                if (row != null)
                                {
                                    row.Focus();
                                    System.Windows.Input.Keyboard.Focus(row);
                                }
                            }));
                        }
                    };
                }
            };

            // Foco inicial al cargar la vista
            this.Loaded += (s, e) => SearchBox.Focus();

            // Saltar del buscador al carrito con flechas
            SearchBox.PreviewKeyDown += (s, e) => {
                if (e.Key == System.Windows.Input.Key.Down)
                {
                    if (TicketDataGrid.Items.Count > 0)
                    {
                        TicketDataGrid.Focus();
                        if (TicketDataGrid.SelectedItem == null)
                        {
                            TicketDataGrid.SelectedIndex = 0;
                        }
                    }
                }
            };
        }

        private void ApplyDiscount_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var viewModel = (BakeryPOS.ViewModels.PosViewModel)this.DataContext;
            viewModel.ApproveDiscount(AdminPinBox.Password);
            AdminPinBox.Password = string.Empty; // clear after try
        }
    }
}

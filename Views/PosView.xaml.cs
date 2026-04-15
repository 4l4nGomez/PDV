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

                            var selected = TicketDataGrid.SelectedItem;

                            // Asegurar que el ítem esté visible
                            TicketDataGrid.ScrollIntoView(selected);

                            // Dar foco al DataGrid primero
                            TicketDataGrid.Focus();
                            System.Windows.Input.Keyboard.Focus(TicketDataGrid);

                            // Luego intentar enfocar la celda/ fila concreta de forma robusta
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new System.Action(() => {
                                try
                                {
                                    // Reestablecer CurrentCell a la primera columna para navegación con flechas
                                    if (TicketDataGrid.Columns.Count > 0)
                                    {
                                        TicketDataGrid.CurrentCell = new System.Windows.Controls.DataGridCellInfo(selected, TicketDataGrid.Columns[0]);
                                    }

                                    // Forzar actualización y asegurar el contenedor de item existe
                                    TicketDataGrid.UpdateLayout();
                                    TicketDataGrid.ScrollIntoView(selected, TicketDataGrid.Columns.Count > 0 ? TicketDataGrid.Columns[0] : null);

                                    var row = TicketDataGrid.ItemContainerGenerator.ContainerFromItem(selected) as System.Windows.Controls.DataGridRow;
                                    if (row == null)
                                    {
                                        // Intentar volver a generar el contenedor
                                        TicketDataGrid.UpdateLayout();
                                        row = TicketDataGrid.ItemContainerGenerator.ContainerFromItem(selected) as System.Windows.Controls.DataGridRow;
                                    }

                                    if (row != null)
                                    {
                                        row.IsSelected = true;

                                        // Intentar enfocar la primera celda
                                        if (TicketDataGrid.Columns.Count > 0)
                                        {
                                            var cellInfo = new System.Windows.Controls.DataGridCellInfo(selected, TicketDataGrid.Columns[0]);
                                            TicketDataGrid.CurrentCell = cellInfo;

                                            // Try to get the actual DataGridCell and focus it
                                            var cellContent = TicketDataGrid.Columns[0].GetCellContent(row);
                                            if (cellContent != null)
                                            {
                                                var parent = System.Windows.Media.VisualTreeHelper.GetParent(cellContent);
                                                while (parent != null && !(parent is System.Windows.Controls.DataGridCell))
                                                    parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);

                                                if (parent is System.Windows.Controls.DataGridCell dataGridCell)
                                                {
                                                    dataGridCell.Focus();
                                                    System.Windows.Input.Keyboard.Focus(dataGridCell);
                                                    return;
                                                }
                                            }
                                        }

                                        // Fallback: enfocar la fila
                                        row.Focus();
                                        System.Windows.Input.Keyboard.Focus(row);
                                    }
                                }
                                catch { /* no bloquear por errores de foco */ }
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

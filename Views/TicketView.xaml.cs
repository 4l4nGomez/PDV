using System.Windows;
using System.Windows.Controls;

namespace BakeryPOS.Views
{
    public partial class TicketView : Window
    {
        public TicketView(object viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            this.MouseDown += (s, e) => {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                    this.DragMove();
            };
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnCerrar.Visibility = Visibility.Collapsed;
                this.UpdateLayout();

                PrintDialog printDialog = new PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    // Crear un contenedor temporal para forzar el centrado real
                    Grid printContainer = new Grid();
                    printContainer.Width = printDialog.PrintableAreaWidth;
                    printContainer.HorizontalAlignment = HorizontalAlignment.Center;

                    // Remover el ticket de su padre actual para moverlo al contenedor de impresión
                    var parent = (StackPanel)TicketBorder.Parent;
                    parent.Children.Remove(TicketBorder);

                    // Centrar el ticket dentro del contenedor de ancho completo de la página
                    TicketBorder.HorizontalAlignment = HorizontalAlignment.Center;
                    TicketBorder.Margin = new Thickness(0);
                    printContainer.Children.Add(TicketBorder);

                    // Forzar actualización de diseño del contenedor
                    printContainer.Measure(new Size(printDialog.PrintableAreaWidth, double.PositiveInfinity));
                    printContainer.Arrange(new Rect(new Size(printDialog.PrintableAreaWidth, printContainer.DesiredSize.Height)));
                    printContainer.UpdateLayout();

                    // Imprimir el contenedor que ahora tiene el ticket centrado
                    printDialog.PrintVisual(printContainer, "Venta Alan POS");

                    // Devolver el ticket a su lugar original por si se requiere ver de nuevo
                    printContainer.Children.Remove(TicketBorder);
                    parent.Children.Insert(0, TicketBorder);
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al imprimir: {ex.Message}", "Impresión Fallida", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                this.Close();
            }
        }    }
}

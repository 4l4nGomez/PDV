using System.Windows;
using System.Windows.Controls;
using System.Linq;
using BakeryPOS.Models;

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

        public static void ImprimirDirecto(object viewModel)
        {
            try
            {
                var view = new TicketView(viewModel);
                
                // Forzar que el ticket se renderice en memoria
                view.BtnCerrar.Visibility = Visibility.Collapsed;
                view.TicketBorder.HorizontalAlignment = HorizontalAlignment.Center;
                view.TicketBorder.Margin = new Thickness(0);

                PrintDialog printDialog = new PrintDialog();
                
                // Intentar usar la impresora configurada en la base de datos
                using (var context = new AppDbContext())
                {
                    var config = context.Configurations.FirstOrDefault(c => c.Key == "PrinterName");
                    if (config != null && !string.IsNullOrEmpty(config.Value))
                    {
                        try 
                        {
                            printDialog.PrintQueue = new System.Printing.PrintQueue(new System.Printing.LocalPrintServer(), config.Value);
                        }
                        catch
                        {
                            // Si la impresora no existe o falla, PrintDialog usará la predeterminada por defecto
                        }
                    }
                }
                
                // Contenedor para el renderizado
                Grid printContainer = new Grid();
                printContainer.Width = printDialog.PrintableAreaWidth;
                printContainer.HorizontalAlignment = HorizontalAlignment.Center;
                printContainer.DataContext = viewModel;

                // Mover el contenido visual
                var parent = (Grid)view.TicketBorder.Parent;
                parent.Children.Remove(view.TicketBorder);
                printContainer.Children.Add(view.TicketBorder);

                // Calcular dimensiones
                printContainer.Measure(new Size(printDialog.PrintableAreaWidth, double.PositiveInfinity));
                printContainer.Arrange(new Rect(new Size(printDialog.PrintableAreaWidth, printContainer.DesiredSize.Height)));
                printContainer.UpdateLayout();

                // Imprimir a la impresora predeterminada
                printDialog.PrintVisual(printContainer, "Venta Alan POS");
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al imprimir directamente: {ex.Message}", "Impresión Fallida", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
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
                    var parent = (Grid)TicketBorder.Parent;
                    parent.Children.Remove(TicketBorder);

                    // Centrar el ticket dentro del contenedor de ancho completo de la página
                    TicketBorder.HorizontalAlignment = HorizontalAlignment.Center;
                    TicketBorder.Margin = new Thickness(0);
                    
                    // CRÍTICO: Asignar el DataContext directamente al contenedor de impresión
                    printContainer.DataContext = this.DataContext; 
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

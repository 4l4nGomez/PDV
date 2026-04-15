using System.Windows;
using System.Windows.Controls;

namespace BakeryPOS.Views
{
    public partial class ProductionView : UserControl
    {
        public ProductionView()
        {
            InitializeComponent();
            this.AddHandler(System.Windows.UIElement.GotFocusEvent, new System.Windows.RoutedEventHandler(OnTextBoxGotFocus));

            // Al mostrarse la vista, enfocar el campo de código para entrada rápida
            this.Loaded += (s, e) =>
            {
                try
                {
                    ProductionCodeInput.Focus();
                    ProductionCodeInput.SelectAll();
                }
                catch { }
            };
        }

        private void OnTextBoxGotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (e.OriginalSource is TextBox textBox)
                textBox.Dispatcher.BeginInvoke(new System.Action(() => textBox.SelectAll()));
        }

        private void BtnRegisterProduction_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Action(() => {
                ProductionCodeInput.Focus();
                ProductionCodeInput.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void BtnRegisterShrinkage_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Action(() => {
                ShrinkageCodeInput.Focus();
                ShrinkageCodeInput.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        // Navegación y atajos por teclado
        private void ProductionCodeInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Down)
            {
                ProductionQtyInput.Focus();
                ProductionQtyInput.SelectAll();
                e.Handled = true;
            }
        }

        private void ProductionQtyInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (this.DataContext is BakeryPOS.ViewModels.ProductionViewModel vm)
                {
                    // Ejecutar comando de registro de producción
                    if (vm.RegisterProductionCommand.CanExecute(null)) 
                    {
                        vm.RegisterProductionCommand.Execute(null);
                        
                        // Regresar el foco al código después de registrar
                        Dispatcher.BeginInvoke(new System.Action(() => {
                            ProductionCodeInput.Focus();
                            ProductionCodeInput.SelectAll();
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Up)
            {
                ProductionCodeInput.Focus();
                ProductionCodeInput.SelectAll();
                e.Handled = true;
            }
        }

        private void ShrinkageCodeInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Down)
            {
                ShrinkageQtyInput.Focus();
                ShrinkageQtyInput.SelectAll();
                e.Handled = true;
            }
        }

        private void ShrinkageQtyInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (this.DataContext is BakeryPOS.ViewModels.ProductionViewModel vm)
                {
                    if (vm.RegisterShrinkageCommand.CanExecute(null)) 
                    {
                        vm.RegisterShrinkageCommand.Execute(null);

                        // Regresar el foco al código después de registrar
                        Dispatcher.BeginInvoke(new System.Action(() => {
                            ShrinkageCodeInput.Focus();
                            ShrinkageCodeInput.SelectAll();
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Up)
            {
                ShrinkageCodeInput.Focus();
                ShrinkageCodeInput.SelectAll();
                e.Handled = true;
            }
        }
    }
}

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
    }
}

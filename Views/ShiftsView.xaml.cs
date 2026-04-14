using System.Windows.Controls;

namespace BakeryPOS.Views
{
    public partial class ShiftsView : UserControl
    {
        public ShiftsView()
        {
            InitializeComponent();
            
            // Auto-seleccionar texto al recibir foco para facilitar la edición de montos
            this.AddHandler(System.Windows.UIElement.GotFocusEvent, new System.Windows.RoutedEventHandler(OnTextBoxGotFocus));
        }

        private void OnTextBoxGotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (e.OriginalSource is TextBox textBox)
            {
                textBox.Dispatcher.BeginInvoke(new System.Action(() => textBox.SelectAll()));
            }
        }
    }
}

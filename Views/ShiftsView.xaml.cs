using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabControl.SelectedItem is TabItem tab)
            {
                var header = tab.Header as string ?? string.Empty;
                if (header.Contains("Auditoría") || header.Contains("Auditoria") || header.Contains("Conteo Físico"))
                {
                    FocusFirstPhysicalCell();
                }
            }
        }

        private void FocusFirstPhysicalCell()
        {
            try
            {
                if (AuditDataGrid == null) return;
                if (AuditDataGrid.Items.Count == 0) return;

                var firstItem = AuditDataGrid.Items[0];
                AuditDataGrid.SelectedItem = firstItem;
                AuditDataGrid.ScrollIntoView(firstItem);
                AuditDataGrid.UpdateLayout();

                // Physical column index is 2 (0:Name,1:Theoretical,2:Physical)
                var colIndex = 2;
                if (AuditDataGrid.Columns.Count <= colIndex) return;

                var column = AuditDataGrid.Columns[colIndex];
                AuditDataGrid.CurrentCell = new DataGridCellInfo(firstItem, column);
                AuditDataGrid.BeginEdit();

                // Try to focus the TextBox inside the cell
                var cellContent = column.GetCellContent(firstItem);
                if (cellContent != null)
                {
                    var tb = FindDescendant<TextBox>(cellContent);
                    if (tb != null)
                    {
                        tb.Focus();
                        tb.SelectAll();
                        return;
                    }
                }

                // Fallback: focus the DataGrid
                AuditDataGrid.Focus();
            }
            catch
            {
                // swallow exceptions to avoid breaking UI
            }
        }

        private void AuditDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                // Commit current edit
                AuditDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                AuditDataGrid.CommitEdit();

                // Move to next row, same column (Physical)
                var current = AuditDataGrid.CurrentItem;
                int idx = AuditDataGrid.Items.IndexOf(current);
                if (idx < AuditDataGrid.Items.Count - 1)
                {
                    var nextItem = AuditDataGrid.Items[idx + 1];
                    var colIndex = 2;
                    if (AuditDataGrid.Columns.Count > colIndex)
                    {
                        var column = AuditDataGrid.Columns[colIndex];
                        AuditDataGrid.SelectedItem = nextItem;
                        AuditDataGrid.ScrollIntoView(nextItem);
                        AuditDataGrid.UpdateLayout();
                        AuditDataGrid.CurrentCell = new DataGridCellInfo(nextItem, column);
                        AuditDataGrid.BeginEdit();

                        var cellContent = column.GetCellContent(nextItem);
                        if (cellContent != null)
                        {
                            var tb = FindDescendant<TextBox>(cellContent);
                            if (tb != null)
                            {
                                tb.Focus();
                                tb.SelectAll();
                                return;
                            }
                        }
                        AuditDataGrid.Focus();
                    }
                }
                else
                {
                    // Last row: stay and focus DataGrid to avoid leaving
                    AuditDataGrid.Focus();
                }
            }
        }

        private T FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t) return t;
                var found = FindDescendant<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;
using BakeryPOS.Models;

namespace BakeryPOS.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<string> _availablePrinters;

        [ObservableProperty]
        private string _selectedPrinter;

        [ObservableProperty]
        private string _statusMessage;

        public SettingsViewModel()
        {
            _context = new AppDbContext();
            LoadPrinters();
            LoadSavedSettings();
        }

        private void LoadPrinters()
        {
            AvailablePrinters = new ObservableCollection<string>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                AvailablePrinters.Add(printer);
            }
        }

        private void LoadSavedSettings()
        {
            var printerConfig = _context.Configurations.FirstOrDefault(c => c.Key == "PrinterName");
            if (printerConfig != null && AvailablePrinters.Contains(printerConfig.Value))
            {
                SelectedPrinter = printerConfig.Value;
            }
            else
            {
                // Fallback to default printer
                PrinterSettings settings = new PrinterSettings();
                SelectedPrinter = settings.PrinterName;
            }
        }

        [RelayCommand]
        private void SaveSettings()
        {
            try
            {
                var printerConfig = _context.Configurations.FirstOrDefault(c => c.Key == "PrinterName");
                if (printerConfig == null)
                {
                    printerConfig = new Configuration { Key = "PrinterName", Value = SelectedPrinter };
                    _context.Configurations.Add(printerConfig);
                }
                else
                {
                    printerConfig.Value = SelectedPrinter;
                }

                _context.SaveChanges();
                StatusMessage = "Configuración guardada correctamente.";
                System.Windows.MessageBox.Show(StatusMessage, "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Error al guardar: " + ex.Message;
                System.Windows.MessageBox.Show(StatusMessage, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}

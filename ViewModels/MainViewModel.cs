using BakeryPOS.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace BakeryPOS.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _currentView;

        [ObservableProperty]
        private string _windowTitle = "Bakery POS - Sistema de Caja";

        [ObservableProperty]
        private string _currentSection = "Ingreso";

        [ObservableProperty]
        private bool _isLoggedIn;

        [ObservableProperty]
        private bool _hasActiveShift;

        [ObservableProperty]
        private string _currentDateTime;

        private System.Windows.Threading.DispatcherTimer _timer;
        private System.Windows.Threading.DispatcherTimer _shiftMonitorTimer;
        private System.Windows.Threading.DispatcherTimer _autoBackupTimer;
        
        // ViewModels persistentes para mantener el estado
        private PosViewModel _posViewModel;
        private ShiftsViewModel _shiftsViewModel;
        private ProductsViewModel _productsViewModel;
        private ProductionViewModel _productionViewModel;
        private ReportsViewModel _reportsViewModel;
        private UsersViewModel _usersViewModel;
        private SettingsViewModel _settingsViewModel;

        public bool IsAdmin => AppSession.IsAdmin;
        public bool IsCashier => AppSession.IsCashier;
        public string CurrentUsername => AppSession.CurrentUser?.Username;

        public MainViewModel()
        {
            // Init with Login
            ShowLogin();
            StartClock();
            StartShiftMonitor();
            StartAutoBackup();
        }

        private void StartAutoBackup()
        {
            _autoBackupTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromMinutes(30)
            };
            // Use an async handler so the UI thread is never blocked by IO operations.
            _autoBackupTimer.Tick += async (s, e) => await AutoBackupDatabaseAsync();
            _autoBackupTimer.Start();
        }

        private async Task AutoBackupDatabaseAsync()
        {
            try
            {
                var sourceDb = Settings.DatabasePath;
                string backupFolder = Settings.BackupFolderPath;
                Directory.CreateDirectory(backupFolder);
                string destDb = Path.Combine(backupFolder, $"bakery_pos_auto_{DateTime.Now:yyyyMMdd_HHmm}.db");

                // Try to copy with retries to avoid transient locking issues when DB is in use.
                const int maxAttempts = 3;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        using (var sourceStream = new FileStream(sourceDb, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var destStream = new FileStream(destDb, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await sourceStream.CopyToAsync(destStream);
                        }

                        // Clean old backups (keep latest 50)
                        var files = new DirectoryInfo(backupFolder).GetFiles("*.db")
                                        .OrderByDescending(f => f.CreationTime).Skip(50);
                        foreach (var file in files) file.Delete();

                        Logger.LogInfo($"Automatic backup created: {destDb}");
                        break;
                    }
                    catch (IOException ioEx) when (attempt < maxAttempts)
                    {
                        Logger.Log($"Backup attempt {attempt} failed due to IO. Retrying...", ioEx);
                        await Task.Delay(1000 * attempt);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error during automatic backup", ex);
            }
        }

        // Comandos globales que delegan en el PosViewModel para mantener atajos funcionales
        [CommunityToolkit.Mvvm.Input.RelayCommand]
        private void FocusPosSearch()
        {
            if (_posViewModel != null)
            {
                // Execute the generated ICommand on the PosViewModel if available
                _posViewModel.FocusSearchCommand?.Execute(null);
            }
        }

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        private void FocusPosCart()
        {
            if (_posViewModel != null)
            {
                _posViewModel.FocusCartCommand?.Execute(null);
            }
        }

        private void StartShiftMonitor()
        {
            _shiftMonitorTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(5)
            };
            _shiftMonitorTimer.Tick += (s, e) => VerifyShiftStatus();
            _shiftMonitorTimer.Start();
        }

        private void VerifyShiftStatus()
        {
            if (!IsLoggedIn) return;

            // No verificar si ya estamos en la vista de Turnos para evitar bucles de mensajes
            if (CurrentView is ShiftsViewModel)
            {
                using (var context = new AppDbContext())
                {
                    HasActiveShift = context.Shifts.Any(s => !s.IsClosed);
                }
                return;
            }

            using (var context = new AppDbContext())
            {
                bool active = context.Shifts.Any(s => !s.IsClosed);
                
                // Si el sistema cree que hay turno pero en DB ya se cerró
                if (HasActiveShift && !active)
                {
                    HasActiveShift = false;
                    System.Windows.MessageBox.Show("La caja ha sido cerrada por otro usuario. Debes realizar una nueva apertura para continuar.", "Aviso de Seguridad", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    NavigateToShifts();
                }
                else
                {
                    HasActiveShift = active;
                }
            }
        }

        private void StartClock()
        {
            UpdateClock();
            _timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => UpdateClock();
            _timer.Start();
        }

        private void UpdateClock()
        {
            CurrentDateTime = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void ShowLogin()
        {
            IsLoggedIn = false;
            HasActiveShift = false;
            
            // Limpiar estados de todos los ViewModels al cerrar sesión
            _posViewModel = null;
            _shiftsViewModel = null;
            _productsViewModel = null;
            _productionViewModel = null;
            _reportsViewModel = null;
            _usersViewModel = null;
            _settingsViewModel = null;

            CurrentSection = "Ingreso";
            WindowTitle = "Bakery POS - Iniciar Sesión";
            var loginVm = new LoginViewModel();
            loginVm.OnLoginSuccess = () => 
            {
                IsLoggedIn = true;
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(IsCashier));
                OnPropertyChanged(nameof(CurrentUsername));
                
                using (var context = new AppDbContext())
                {
                    HasActiveShift = context.Shifts.Any(s => !s.IsClosed);
                    if (HasActiveShift)
                    {
                        NavigateToPos(); // Default valid view for both roles
                    }
                    else
                    {
                        NavigateToShifts(); // Prompts user to open shift
                    }
                }
            };
            CurrentView = loginVm;
        }

        [RelayCommand]
        private void Logout()
        {
            var result = System.Windows.MessageBox.Show("¿Estás seguro de que deseas CERRAR SESIÓN?\n(Esto limpiará la venta actual pero NO cerrará el turno de caja)", "Confirmar Salida", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                AppSession.CurrentUser = null;
                ShowLogin();
            }
        }

        [RelayCommand]
        private void NavigateToProducts()
        {
            if (!CheckShift()) return;
            if (_productsViewModel == null)
            {
                _productsViewModel = new ProductsViewModel();
            }
            else
            {
                // Refrescar productos en el catálogo
                _productsViewModel.LoadData();
            }
            CurrentView = _productsViewModel;
            CurrentSection = "Catálogo";
            WindowTitle = "Bakery POS - Catálogo de Productos";
        }

        [RelayCommand]
        private void NavigateToProduction()
        {
            if (!CheckShift()) return;
            if (_productionViewModel == null)
            {
                _productionViewModel = new ProductionViewModel();
            }
            else
            {
                // Refrescar productos en producción (SOLUCIONA EL PROBLEMA DEL USUARIO)
                _productionViewModel.LoadData();
            }
            CurrentView = _productionViewModel;
            CurrentSection = "Producción";
            WindowTitle = "Bakery POS - Producción de Pan";
        }

        [RelayCommand]
        private void NavigateToPos()
        {
            if (!CheckShift()) return;
            
            // Reutilizar la instancia si ya existe para conservar el carrito
            if (_posViewModel == null)
            {
                _posViewModel = new PosViewModel();
            }
            else
            {
                // Refrescar productos disponibles
                _posViewModel.LoadData();
            }

            CurrentView = _posViewModel;
            CurrentSection = "Punto de Venta";
            WindowTitle = "Bakery POS - Punto de Venta";
        }

        [RelayCommand]
        private void NavigateToShifts()
        {
            if (_shiftsViewModel == null)
            {
                _shiftsViewModel = new ShiftsViewModel();
            }
            else
            {
                // Refrescar estado del turno y productos en auditoría
                _shiftsViewModel.LoadData();
            }
            CurrentView = _shiftsViewModel;
            CurrentSection = "Turnos y Caja";
            WindowTitle = "Bakery POS - Turnos y Corte de Caja";
        }

        [RelayCommand]
        private void NavigateToReports()
        {
            if (!IsAdmin) return;
            if (!CheckShift()) return;
            if (_reportsViewModel == null)
            {
                _reportsViewModel = new ReportsViewModel();
            }
            else
            {
                // Refrescar reportes (ventas, movimientos y auditorías)
                _reportsViewModel.LoadData();
            }
            CurrentView = _reportsViewModel;
            CurrentSection = "Reportes";
            WindowTitle = "Bakery POS - Reportes de Ventas";
        }

        [RelayCommand]
        private void NavigateToUsers()
        {
            if (!IsAdmin) return;
            if (!CheckShift()) return;
            if (_usersViewModel == null) _usersViewModel = new UsersViewModel();
            CurrentView = _usersViewModel;
            CurrentSection = "Gestión de Usuarios";
            WindowTitle = "Bakery POS - Gestión de Usuarios";
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            if (!IsAdmin) return;
            if (_settingsViewModel == null) _settingsViewModel = new SettingsViewModel();
            CurrentView = _settingsViewModel;
            CurrentSection = "Ajustes";
            WindowTitle = "Bakery POS - Ajustes del Sistema";
        }

        private bool CheckShift()
        {
            using (var context = new AppDbContext())
            {
                HasActiveShift = context.Shifts.Any(s => !s.IsClosed);
                if (!HasActiveShift)
                {
                    System.Windows.MessageBox.Show("Debes realizar la APERTURA DE CAJA antes de acceder a esta sección.", "Caja Cerrada", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    NavigateToShifts();
                    return false;
                }
            }
            return true;
        }
    }
}

using BakeryPOS.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

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
        private PosViewModel _posViewModel;

        public bool IsAdmin => AppSession.IsAdmin;
        public bool IsCashier => AppSession.IsCashier;
        public string CurrentUsername => AppSession.CurrentUser?.Username;

        public MainViewModel()
        {
            // Init with Login
            ShowLogin();
            StartClock();
            StartShiftMonitor();
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
            _posViewModel = null; // Limpiar estado del POS al ir al login
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
            AppSession.CurrentUser = null;
            _posViewModel = null; // Limpiar carrito al cerrar sesión
            ShowLogin();
        }

        [RelayCommand]
        private void NavigateToProducts()
        {
            if (!CheckShift()) return;
            CurrentView = new ProductsViewModel();
            CurrentSection = "Catálogo";
            WindowTitle = "Bakery POS - Catálogo de Productos";
        }

        [RelayCommand]
        private void NavigateToProduction()
        {
            if (!CheckShift()) return;
            CurrentView = new ProductionViewModel();
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

            CurrentView = _posViewModel;
            CurrentSection = "Punto de Venta";
            WindowTitle = "Bakery POS - Punto de Venta";
        }

        [RelayCommand]
        private void NavigateToShifts()
        {
            CurrentView = new ShiftsViewModel();
            CurrentSection = "Turnos y Caja";
            WindowTitle = "Bakery POS - Turnos y Corte de Caja";
        }

        [RelayCommand]
        private void NavigateToReports()
        {
            if (!IsAdmin) return;
            if (!CheckShift()) return;
            CurrentView = new ReportsViewModel();
            CurrentSection = "Reportes";
            WindowTitle = "Bakery POS - Reportes de Ventas";
        }

        [RelayCommand]
        private void NavigateToUsers()
        {
            if (!IsAdmin) return;
            if (!CheckShift()) return;
            CurrentView = new UsersViewModel();
            CurrentSection = "Gestión de Usuarios";
            WindowTitle = "Bakery POS - Gestión de Usuarios";
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

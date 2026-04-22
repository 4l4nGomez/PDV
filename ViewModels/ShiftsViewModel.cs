using BakeryPOS.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace BakeryPOS.ViewModels
{
    public partial class ShiftsViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private Shift _activeShift;

        [ObservableProperty]
        private bool _hasActiveShift;

        public bool IsOpenShiftVisible => !HasActiveShift;
        public bool IsCloseShiftVisible => HasActiveShift;

        partial void OnHasActiveShiftChanged(bool value)
        {
            OnPropertyChanged(nameof(IsOpenShiftVisible));
            OnPropertyChanged(nameof(IsCloseShiftVisible));
        }

        // Propiedades Apertura
        [ObservableProperty]
        private decimal _startingCash;

        // Propiedades Cierre
        [ObservableProperty]
        private decimal _totalSalesAmount;
        [ObservableProperty]
        private decimal _totalInflows;
        [ObservableProperty]
        private decimal _totalExpenses;
        [ObservableProperty]
        private decimal _expectedCash;
        
        [ObservableProperty]
        private string _actualCashText = "0";

        [ObservableProperty]
        private decimal _cashDifference;
        
        [ObservableProperty]
        private int _selectedTabIndex;
        
        // Propiedades de Movimientos
        [ObservableProperty]
        private string _movementAmountText = "0";

        [ObservableProperty]
        private string _movementDescription;
        
        [ObservableProperty]
        private ObservableCollection<ProductAudit> _inventoryAudit;

        [ObservableProperty]
        private ProductAudit _selectedAuditItem;

        // Propiedades de Resumen de Auditoría
        [ObservableProperty]
        private int _totalItemsMissing;
        [ObservableProperty]
        private decimal _financialLoss;

        public ShiftsViewModel()
        {
            _context = new AppDbContext();
            LoadActiveShift();
        }

        public void LoadData()
        {
            LoadActiveShift();
        }

        private void LoadActiveShift()
        {
            // Forzar recarga de entidades para ver cambios de otros ViewModels
            _context.ChangeTracker.Entries().ToList().ForEach(e => e.Reload());

            ActiveShift = _context.Shifts.FirstOrDefault(s => !s.IsClosed);
            HasActiveShift = ActiveShift != null;

            if (HasActiveShift)
            {
                CalculateCloseMetrics();
            }
        }

        [RelayCommand]
        private void OpenShift()
        {
            var newShift = new Shift
            {
                UserId = AppSession.CurrentUser.Id,
                StartTime = DateTime.Now,
                StartingCash = StartingCash,
                ExpectedEndingCash = StartingCash,
                IsClosed = false
            };

            _context.Shifts.Add(newShift);
            _context.SaveChanges();

            StartingCash = 0;
            LoadActiveShift();

            // Notify MainViewModel
            if (Application.Current.MainWindow.DataContext is MainViewModel mainVm)
            {
                mainVm.HasActiveShift = true;
            }
        }

        [RelayCommand]
        private void AddMovement(string type)
        {
            // Parsear el monto de forma segura
            string cleanAmount = (MovementAmountText ?? "0").Replace(",", ".");
            if (ActiveShift == null || !decimal.TryParse(cleanAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || amount <= 0) 
            {
                System.Windows.MessageBox.Show("Por favor, ingrese un monto válido mayor a 0.", "Monto Inválido", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            bool isInflow = type == "IN";

            var movement = new CashMovement
            {
                ShiftId = ActiveShift.Id,
                UserId = AppSession.CurrentUser.Id,
                Amount = isInflow ? amount : -amount,
                Description = MovementDescription ?? (isInflow ? "Ingreso" : "Gasto"),
                MovementDate = DateTime.Now
            };

            _context.CashMovements.Add(movement);
            _context.SaveChanges();

            MovementAmountText = "0";
            MovementDescription = string.Empty;

            CalculateCloseMetrics(); // Recalculate totals
        }

        public void CalculateCloseMetrics()
        {
            // Sumar todas las ventas atadas a este turno
            TotalSalesAmount = _context.Sales.Where(s => s.ShiftId == ActiveShift.Id).Sum(s => s.TotalAmount);
            TotalInflows = _context.CashMovements.Where(s => s.ShiftId == ActiveShift.Id && s.Amount > 0).Sum(s => s.Amount);
            TotalExpenses = _context.CashMovements.Where(s => s.ShiftId == ActiveShift.Id && s.Amount < 0).Sum(s => s.Amount);
            
            ExpectedCash = ActiveShift.StartingCash + TotalSalesAmount + TotalInflows + TotalExpenses;
            
            // Cargar inventario teórico
            var products = _context.Products.ToList();

            // Cargar auditoría diaria si existe (solo hoy)
            var today = DateTime.Today;
            var daily = _context.DailyInventoryAudits.Where(d => d.Date == today).ToList();

            var auditItems = products.Select(p => new ProductAudit 
            { 
                ProductId = p.Id, 
                Name = p.Name, 
                UnitPrice = p.Price,
                TheoreticalStock = p.Stock, 
                PhysicalStock = 0 // SIEMPRE 0 al cargar
            }).ToList();

            // Vincular eventos de cambio para actualizar totales
            foreach(var item in auditItems)
            {
                item.PropertyChanged += (s, e) => {
                    if (e.PropertyName == nameof(ProductAudit.PhysicalStock))
                        UpdateAuditTotals();
                };
            }

            InventoryAudit = new ObservableCollection<ProductAudit>(auditItems);
            UpdateAuditTotals();
        }

        [RelayCommand]
        private void SyncAllTheoretical()
        {
            if (InventoryAudit == null) return;
            foreach (var item in InventoryAudit)
            {
                item.PhysicalStock = item.TheoreticalStock;
            }
            UpdateAuditTotals();
        }

        private void UpdateAuditTotals()
        {
            if (InventoryAudit == null) return;
            
            // Sumar solo las diferencias negativas (faltantes)
            TotalItemsMissing = InventoryAudit.Where(i => i.Difference < 0).Sum(i => Math.Abs(i.Difference));
            
            // Pérdida financiera total basada en diferencias negativas
            FinancialLoss = InventoryAudit.Where(i => i.Difference < 0).Sum(i => Math.Abs(i.FinancialImpact));
        }

        [RelayCommand]
        private void IncreaseAudit()
        {
            if (SelectedAuditItem == null) return;
            SelectedAuditItem.PhysicalStock++;
            UpdateAuditTotals();
        }

        [RelayCommand]
        private void DecreaseAudit()
        {
            if (SelectedAuditItem == null) return;
            if (SelectedAuditItem.PhysicalStock > 0) SelectedAuditItem.PhysicalStock--;
            UpdateAuditTotals();
        }

        [RelayCommand]
        private void SaveAudit()
        {
            if (InventoryAudit == null) return;

            var now = DateTime.Now;
            bool anyChange = false;

            foreach (var item in InventoryAudit)
            {
                // Solo registramos si el usuario ingresó un número (conteo físico > 0)
                // o si hay una diferencia que requiere ajuste.
                if (item.PhysicalStock == 0 && item.TheoreticalStock == 0) continue;
                if (item.PhysicalStock == 0 && item.TheoreticalStock != 0)
                {
                    // Si el usuario dejó en 0 pero había stock, preguntamos o asumimos que es 0 real
                    // Para este sistema, asumiremos que 0 es un conteo válido.
                }

                var product = _context.Products.Find(item.ProductId);
                if (product != null)
                {
                    int diff = item.PhysicalStock - product.Stock;
                    
                    // Crear SIEMPRE un nuevo registro para el historial
                    var newRecord = new Models.DailyInventoryAudit
                    {
                        Date = now, // Hora exacta
                        ProductId = item.ProductId,
                        PhysicalStock = item.PhysicalStock,
                        UserId = AppSession.CurrentUser?.Id ?? 0,
                        Note = diff == 0 ? "Conteo: Correcto" : $"Ajuste: {(diff > 0 ? "+" : "")}{diff} pzas (Antes: {product.Stock})"
                    };
                    
                    _context.DailyInventoryAudits.Add(newRecord);

                    // Actualizar el stock real del producto
                    product.Stock = item.PhysicalStock;
                    item.TheoreticalStock = item.PhysicalStock;
                    anyChange = true;
                }
            }

            if (anyChange)
            {
                _context.SaveChanges();
                
                // Resetear a 0 para el siguiente conteo limpio
                foreach (var item in InventoryAudit)
                {
                    item.PhysicalStock = 0;
                }
                UpdateAuditTotals();

                System.Windows.MessageBox.Show("Cambios aplicados y registrados en el historial de reportes.", "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void PrintAuditReport()
        {
            // Método desactivado a petición del usuario
        }

        partial void OnActualCashTextChanged(string value)
        {
            string cleanCash = (value ?? "0").Replace(",", ".");
            if (decimal.TryParse(cleanCash, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal actual))
            {
                CashDifference = actual - ExpectedCash;
            }
            CloseShiftCommand.NotifyCanExecuteChanged();
        }

        private bool CanPerformCut() 
        {
            string cleanCash = (ActualCashText ?? "0").Replace(",", ".");
            return decimal.TryParse(cleanCash, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal actual) && actual > 0;
        }

        [RelayCommand(CanExecute = nameof(CanPerformCut))]
        private void CloseShift()
        {
            if (ActiveShift == null) return;

            string cleanCash = (ActualCashText ?? "0").Replace(",", ".");
            if (!decimal.TryParse(cleanCash, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal actualCash))
            {
                System.Windows.MessageBox.Show("El monto de cierre no es válido.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            ActiveShift.EndTime = DateTime.Now;
            ActiveShift.ExpectedEndingCash = ExpectedCash;
            ActiveShift.ActualEndingCash = actualCash;
            
            // Persist snapshots
            ActiveShift.TotalSales = TotalSalesAmount;
            ActiveShift.TotalInflows = TotalInflows;
            ActiveShift.TotalExpenses = Math.Abs(TotalExpenses); 
            
            // Calculate shrinkage amount for this shift
            ActiveShift.TotalShrinkage = _context.Shrinkages
                .Where(s => s.Timestamp >= ActiveShift.StartTime && s.Timestamp <= ActiveShift.EndTime)
                .Sum(s => s.Quantity);

            ActiveShift.IsClosed = true;

            _context.SaveChanges();
            
            // Backup Database on close
            BackupDatabase();

            ActualCashText = "0";
            LoadActiveShift(); 

            // Notify MainViewModel
            if (Application.Current.MainWindow.DataContext is MainViewModel mainVm)
            {
                mainVm.HasActiveShift = false;
            }
        }

        private void BackupDatabase()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string sourceDb = System.IO.Path.Join(localAppData, "BakeryPOS", "bakery_pos.db");
                
                var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string backupFolder = System.IO.Path.Join(myDocuments, "BakeryPOS_Backups");
                System.IO.Directory.CreateDirectory(backupFolder);

                string destDb = System.IO.Path.Join(backupFolder, $"bakery_pos_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                
                if (System.IO.File.Exists(sourceDb))
                {
                    System.IO.File.Copy(sourceDb, destDb, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error backing up DB: {ex.Message}");
            }
        }
    }

    // Clase de ayuda para UI
    public partial class ProductAudit : ObservableObject
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
        public int TheoreticalStock { get; set; }

        [ObservableProperty]
        private int _physicalStock;

        public int Difference => PhysicalStock - TheoreticalStock;
        public decimal FinancialImpact => Difference * UnitPrice;
        
        partial void OnPhysicalStockChanged(int value)
        {
            OnPropertyChanged(nameof(Difference));
            OnPropertyChanged(nameof(FinancialImpact));
        }
    }
}

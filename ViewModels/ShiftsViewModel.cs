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
        private decimal _actualCash;
        [ObservableProperty]
        private decimal _cashDifference;
        
        // Propiedades de Movimientos
        [ObservableProperty]
        private decimal _movementAmount;
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

        private void LoadActiveShift()
        {
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
            if (ActiveShift == null || MovementAmount <= 0) return;
            
            bool isInflow = type == "IN";

            var movement = new CashMovement
            {
                ShiftId = ActiveShift.Id,
                UserId = AppSession.CurrentUser.Id,
                Amount = isInflow ? MovementAmount : -MovementAmount,
                Description = MovementDescription ?? (isInflow ? "Ingreso" : "Gasto"),
                MovementDate = DateTime.Now
            };

            _context.CashMovements.Add(movement);
            _context.SaveChanges();

            MovementAmount = 0;
            MovementDescription = string.Empty;

            CalculateCloseMetrics(); // Recalculate totals
        }

        private void CalculateCloseMetrics()
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
                PhysicalStock = p.Stock 
            }).ToList();

            // Aplicar valores guardados si existen para hoy
            foreach(var item in auditItems)
            {
                var rec = daily.FirstOrDefault(d => d.ProductId == item.ProductId);
                if (rec != null)
                {
                    // If the DB stored null, fall back to theoretical stock
                    item.PhysicalStock = rec.PhysicalStock ?? item.TheoreticalStock;
                }
                else
                {
                    item.PhysicalStock = item.TheoreticalStock;
                }

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

            var today = DateTime.Today;

            foreach (var item in InventoryAudit)
            {
                var existing = _context.DailyInventoryAudits.FirstOrDefault(d => d.Date == today && d.ProductId == item.ProductId);
                if (existing == null)
                {
                    existing = new Models.DailyInventoryAudit
                    {
                        Date = today,
                        ProductId = item.ProductId,
                        PhysicalStock = item.PhysicalStock,
                        UserId = AppSession.CurrentUser?.Id ?? 0
                    };
                    _context.DailyInventoryAudits.Add(existing);
                }
                else
                {
                    existing.PhysicalStock = item.PhysicalStock;
                    existing.UserId = AppSession.CurrentUser?.Id ?? existing.UserId;
                }
            }

            _context.SaveChanges();
            System.Windows.MessageBox.Show("Auditoría guardada correctamente.", "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        partial void OnActualCashChanged(decimal value)
        {
            CashDifference = value - ExpectedCash;
            CloseShiftCommand.NotifyCanExecuteChanged();
        }

        private bool CanPerformCut() => ActualCash > 0;

        [RelayCommand(CanExecute = nameof(CanPerformCut))]
        private void CloseShift()
        {
            if (ActiveShift == null) return;

            ActiveShift.EndTime = DateTime.Now;
            ActiveShift.ExpectedEndingCash = ExpectedCash;
            ActiveShift.ActualEndingCash = ActualCash;
            
            // Persist snapshots
            ActiveShift.TotalSales = TotalSalesAmount;
            ActiveShift.TotalInflows = TotalInflows;
            ActiveShift.TotalExpenses = Math.Abs(TotalExpenses); 
            
            // Calculate shrinkage amount for this shift
            ActiveShift.TotalShrinkage = _context.Shrinkages
                .Where(s => s.Timestamp >= ActiveShift.StartTime && s.Timestamp <= ActiveShift.EndTime)
                .Sum(s => s.Quantity);

            ActiveShift.IsClosed = true;

            foreach (var item in InventoryAudit)
            {
                var p = _context.Products.Find(item.ProductId);
                if (p != null) p.Stock = item.PhysicalStock;
            }

            _context.SaveChanges();
            
            // Backup Database on close
            BackupDatabase();

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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryPOS.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BakeryPOS.ViewModels
{
    public partial class ProductDisplayItem : ObservableObject
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        
        [ObservableProperty]
        private int _stock;
        
        [ObservableProperty]
        private int _totalShrinkage;

        [ObservableProperty]
        private int _totalProduced;
    }

    public partial class ProductionActivity : ObservableObject
    {
        public string Time { get; set; }
        public string User { get; set; }
        public string Action { get; set; } // "PRODUCCIÓN" o "MERMA"
        public string Product { get; set; }
        public int Quantity { get; set; }
        public string Color { get; set; } // Para diferenciar visualmente
    }

    public partial class ProductionViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<ProductDisplayItem> _products;

        [ObservableProperty]
        private ObservableCollection<ProductionActivity> _recentActivity;

        [ObservableProperty]
        private ProductDisplayItem _selectedProduct;

        [ObservableProperty]
        private string _productionSearchCode = string.Empty;

        [ObservableProperty]
        private int _quantityProduced;

        // Shrinkage properties
        [ObservableProperty]
        private ProductDisplayItem _selectedShrinkageProduct;

        [ObservableProperty]
        private string _shrinkageSearchCode = string.Empty;
        
        [ObservableProperty]
        private int _shrinkageQuantity;
        
        [ObservableProperty]
        private string _shrinkageReason = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isNotificationVisible;

        [ObservableProperty]
        private string _statusColor = "#10B981";

        [RelayCommand]
        private void ExportToExcel()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Archivo Excel (CSV)|*.csv",
                    FileName = $"Inventario_Panaderia_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                    Title = "Exportar Inventario a Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var sb = new System.Text.StringBuilder();
                    
                    // Cabeceras solicitadas
                    sb.AppendLine("Codigo,Nombre,Precio,Stock");

                    foreach (var p in Products)
                    {
                        string safeName = p.Name.Replace(",", " ");
                        sb.AppendLine($"{p.Code},{safeName},{p.Price},{p.Stock}");
                    }

                    System.IO.File.WriteAllText(saveFileDialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    
                    System.Windows.MessageBox.Show("Inventario exportado correctamente.", "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        partial void OnProductionSearchCodeChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var match = Products.FirstOrDefault(p => p.Code.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (match != null) SelectedProduct = match;
        }

        partial void OnShrinkageSearchCodeChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var match = Products.FirstOrDefault(p => p.Code.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (match != null) SelectedShrinkageProduct = match;
        }

        public ProductionViewModel()
        {
            _context = new AppDbContext();
            LoadData();
        }

        public void LoadData()
        {
            _context.ChangeTracker.Entries().ToList().ForEach(e => e.Reload()); 
            
            var today = DateTime.Today;
            var dbProducts = _context.Products.ToList();
            
            // Traer registros de HOY para los totales de la lista
            var shrinkagesToday = _context.Shrinkages.Where(s => s.Timestamp >= today).ToList();
            var productionsToday = _context.ProductionLogs.Where(pr => pr.ProductionDate >= today).ToList();

            var displayItems = dbProducts.Select(p => new ProductDisplayItem
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Category = p.Category,
                Price = p.Price,
                Stock = p.Stock,
                TotalShrinkage = shrinkagesToday.Where(s => s.ProductId == p.Id).Sum(s => s.Quantity),
                TotalProduced = productionsToday.Where(pr => pr.ProductId == p.Id).Sum(pr => pr.QuantityProduced)
            }).ToList();

            Products = new ObservableCollection<ProductDisplayItem>(displayItems);

            // Cargar Bitácora de Actividad de HOY
            var activity = new List<ProductionActivity>();

            // Añadir producciones de hoy
            activity.AddRange(_context.ProductionLogs
                .Include(p => p.User)
                .Include(p => p.Product)
                .Where(p => p.ProductionDate >= today)
                .OrderByDescending(p => p.ProductionDate)
                .Select(p => new ProductionActivity {
                    Time = p.ProductionDate.ToString("HH:mm"),
                    User = p.User != null ? p.User.Username : "Sist.",
                    Action = "HORNEADO",
                    Product = p.Product != null ? p.Product.Name : "Desconocido",
                    Quantity = p.QuantityProduced,
                    Color = "#10B981" // Verde
                }));

            // Añadir mermas de hoy
            activity.AddRange(_context.Shrinkages
                .Include(s => s.User)
                .Include(s => s.Product)
                .Where(s => s.Timestamp >= today)
                .OrderByDescending(s => s.Timestamp)
                .Select(s => new ProductionActivity {
                    Time = s.Timestamp.ToString("HH:mm"),
                    User = s.User != null ? s.User.Username : "Sist.",
                    Action = "MERMA",
                    Product = s.Product != null ? s.Product.Name : "Desconocido",
                    Quantity = s.Quantity,
                    Color = "#EF4444" // Rojo
                }));

            RecentActivity = new ObservableCollection<ProductionActivity>(activity.OrderByDescending(a => a.Time));
        }

        private async Task ShowNotification(string message, bool isError = false)
        {
            StatusMessage = message;
            StatusColor = isError ? "#EF4444" : "#10B981";
            IsNotificationVisible = true;
            await Task.Delay(1500);
            if (StatusMessage == message) 
            {
                IsNotificationVisible = false;
                StatusMessage = string.Empty;
            }
        }

        [RelayCommand]
        private async Task RegisterProduction()
        {
            if (SelectedProduct == null)
            {
                await ShowNotification("Seleccione un producto", true);
                return;
            }

            if (QuantityProduced <= 0)
            {
                await ShowNotification("Cantidad inválida", true);
                return;
            }

            try
            {
                var log = new ProductionLog
                {
                    ProductId = SelectedProduct.Id,
                    QuantityProduced = QuantityProduced,
                    ProductionDate = DateTime.Now,
                    UserId = AppSession.CurrentUser?.Id ?? 1
                };
                
                _context.ProductionLogs.Add(log);

                var p = _context.Products.Find(SelectedProduct.Id);
                if (p != null)
                {
                    p.Stock += QuantityProduced;
                }

                _context.SaveChanges();

                QuantityProduced = 0;
                ProductionSearchCode = string.Empty;
                SelectedProduct = null;
                
                LoadData();
                await ShowNotification("Producción registrada");
            }
            catch (Exception)
            {
                await ShowNotification("Error al guardar", true);
            }
        }

        [RelayCommand]
        private async Task RegisterShrinkage()
        {
            if (SelectedShrinkageProduct == null)
            {
                await ShowNotification("Seleccione producto", true);
                return;
            }

            if (ShrinkageQuantity <= 0)
            {
                await ShowNotification("Cantidad inválida", true);
                return;
            }

            try
            {
                var shrinkage = new Shrinkage
                {
                    ProductId = SelectedShrinkageProduct.Id,
                    Quantity = ShrinkageQuantity,
                    Reason = string.IsNullOrWhiteSpace(ShrinkageReason) ? "Sin motivo" : ShrinkageReason,
                    Timestamp = DateTime.Now,
                    UserId = AppSession.CurrentUser?.Id ?? 1
                };
                
                _context.Shrinkages.Add(shrinkage);

                var p = _context.Products.Find(SelectedShrinkageProduct.Id);
                if (p != null)
                {
                    p.Stock -= ShrinkageQuantity;
                    if (p.Stock < 0) p.Stock = 0;
                }

                _context.SaveChanges();

                ShrinkageQuantity = 0;
                ShrinkageReason = string.Empty;
                ShrinkageSearchCode = string.Empty;
                SelectedShrinkageProduct = null;
                
                LoadData();
                await ShowNotification("Merma registrada");
            }
            catch (Exception)
            {
                await ShowNotification("Error al guardar", true);
            }
        }
    }
}
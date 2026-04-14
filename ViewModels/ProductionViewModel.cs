using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryPOS.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace BakeryPOS.ViewModels
{
    public partial class ProductDisplayItem : ObservableObject
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        
        [ObservableProperty]
        private int _stock;
        
        [ObservableProperty]
        private int _totalShrinkage;

        [ObservableProperty]
        private int _totalProduced;
    }

    public partial class ProductionViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<ProductDisplayItem> _products;

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
            LoadProducts();
        }

        private void LoadProducts()
        {
            var dbProducts = _context.Products.ToList();
            var shrinkages = _context.Shrinkages.ToList();
            var productions = _context.ProductionLogs.ToList();

            var displayItems = dbProducts.Select(p => new ProductDisplayItem
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Category = p.Category,
                Stock = p.Stock,
                TotalShrinkage = shrinkages.Where(s => s.ProductId == p.Id).Sum(s => s.Quantity),
                TotalProduced = productions.Where(pr => pr.ProductId == p.Id).Sum(pr => pr.QuantityProduced)
            }).ToList();

            Products = new ObservableCollection<ProductDisplayItem>(displayItems);
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
                
                LoadProducts();
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
                
                LoadProducts();
                await ShowNotification("Merma registrada");
            }
            catch (Exception)
            {
                await ShowNotification("Error al guardar", true);
            }
        }
    }
}
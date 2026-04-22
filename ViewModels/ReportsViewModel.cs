using BakeryPOS.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace BakeryPOS.ViewModels
{
    public partial class ReportsViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<Sale> _sales;

        [ObservableProperty]
        private ObservableCollection<Shift> _shifts;

        [ObservableProperty]
        private ObservableCollection<CashMovement> _movements;

        [ObservableProperty]
        private ObservableCollection<DailyInventoryAudit> _audits;

        [ObservableProperty]
        private DateTime _startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<User> _users;

        [ObservableProperty]
        private User _selectedUser;

        [ObservableProperty]
        private decimal _totalSales;

        [ObservableProperty]
        private decimal _totalInflows;

        [ObservableProperty]
        private decimal _totalExpenses;

        [ObservableProperty]
        private decimal _netBalance;

        [ObservableProperty]
        private int _auditTotalAdjustedPieces;

        [ObservableProperty]
        private decimal _auditTotalFinancialLoss;

        [ObservableProperty]
        private ObservableCollection<ProductSalesInfo> _topSellingProducts;

        [ObservableProperty]
        private ObservableCollection<HourlySalesInfo> _hourlySales;

        public ReportsViewModel()
        {
            _context = new AppDbContext();
            
            // Cargar usuarios para el filtro
            var allUsers = _context.Users.OrderBy(u => u.Username).ToList();
            // Añadir opción "Todos"
            allUsers.Insert(0, new User { Id = -1, Username = "--- TODOS ---" });
            Users = new ObservableCollection<User>(allUsers);
            SelectedUser = Users[0]; // Seleccionar "Todos" por defecto

            LoadData();
        }

        [RelayCommand]
        public void LoadData()
        {
            var start = StartDate.Date;
            var end = EndDate.Date.AddDays(1).AddTicks(-1);
            int? filterUserId = (SelectedUser != null && SelectedUser.Id != -1) ? SelectedUser.Id : null;

            // Filtro para Ventas
            var salesQuery = _context.Sales.Include(s => s.User).Include(s => s.Shift).AsQueryable();
            salesQuery = salesQuery.Where(s => s.SaleDate >= start && s.SaleDate <= end);
            if (filterUserId.HasValue) salesQuery = salesQuery.Where(s => s.UserId == filterUserId.Value);
            
            var salesList = salesQuery.OrderByDescending(s => s.SaleDate).ToList();
            Sales = new ObservableCollection<Sale>(salesList);
            TotalSales = salesList.Sum(s => s.TotalAmount);

            // Filtro para Turnos
            var shiftsQuery = _context.Shifts.Include(s => s.User).AsQueryable();
            shiftsQuery = shiftsQuery.Where(s => s.StartTime >= start && s.StartTime <= end);
            if (filterUserId.HasValue) shiftsQuery = shiftsQuery.Where(s => s.UserId == filterUserId.Value);
            
            Shifts = new ObservableCollection<Shift>(shiftsQuery.OrderByDescending(s => s.StartTime).ToList());

            // Filtro para Movimientos
            var movementsQuery = _context.CashMovements.Include(m => m.User).AsQueryable();
            movementsQuery = movementsQuery.Where(m => m.MovementDate >= start && m.MovementDate <= end);
            if (filterUserId.HasValue) movementsQuery = movementsQuery.Where(m => m.UserId == filterUserId.Value);
            
            var movementsList = movementsQuery.OrderByDescending(m => m.MovementDate).ToList();
            Movements = new ObservableCollection<CashMovement>(movementsList);

            // Calcular resumen financiero
            TotalInflows = movementsList.Where(m => m.Amount > 0).Sum(m => m.Amount);
            TotalExpenses = Math.Abs(movementsList.Where(m => m.Amount < 0).Sum(m => m.Amount));
            NetBalance = TotalSales + TotalInflows - TotalExpenses;

            // Filtro para Auditorías
            var auditsQuery = _context.DailyInventoryAudits.Include(a => a.User).Include(a => a.Product).AsQueryable();
            auditsQuery = auditsQuery.Where(a => a.Date >= start && a.Date <= end);
            if (filterUserId.HasValue) auditsQuery = auditsQuery.Where(a => a.UserId == filterUserId.Value);
            
            Audits = new ObservableCollection<DailyInventoryAudit>(auditsQuery.OrderByDescending(a => a.Date).ToList());

            // --- ANÁLISIS DE DATOS ---
            
            // 1. Productos más vendidos
            var saleItemsQuery = _context.SaleItems
                .Include(si => si.Product)
                .Include(si => si.Sale)
                .Where(si => si.Sale.SaleDate >= start && si.Sale.SaleDate <= end);

            if (filterUserId.HasValue) 
                saleItemsQuery = saleItemsQuery.Where(si => si.Sale.UserId == filterUserId.Value);

            var topProducts = saleItemsQuery
                .GroupBy(si => new { si.ProductId, si.Product.Name })
                .Select(g => new ProductSalesInfo
                {
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(si => si.Quantity),
                    TotalRevenue = g.Sum(si => si.SubTotal)
                })
                .OrderByDescending(p => p.QuantitySold)
                .Take(15)
                .ToList();
            
            TopSellingProducts = new ObservableCollection<ProductSalesInfo>(topProducts);

            // 2. Ventas por Horario (Flujo de Clientes)
            var hourSales = salesList
                .GroupBy(s => s.SaleDate.Hour)
                .Select(g => new HourlySalesInfo
                {
                    Hour = g.Key,
                    HourDisplay = $"{g.Key:D2}:00",
                    SaleCount = g.Count(),
                    TotalRevenue = g.Sum(s => s.TotalAmount)
                })
                .OrderBy(h => h.Hour)
                .ToList();
            
            HourlySales = new ObservableCollection<HourlySalesInfo>(hourSales);

            // Calcular resumen de auditoría
            AuditTotalAdjustedPieces = 0;
            AuditTotalFinancialLoss = 0;
            // ... resto del código de cálculo ...

            foreach (var audit in Audits)
            {
                if (!string.IsNullOrEmpty(audit.Note) && audit.Note.Contains("Ajuste:"))
                {
                    try
                    {
                        // Parsear notas como "Ajuste: -5 pzas..." o "Ajuste: +2 pzas..."
                        var parts = audit.Note.Split(new[] { "Ajuste:", "pzas" }, StringSplitOptions.None);
                        if (parts.Length >= 2)
                        {
                            string qtyStr = parts[1].Trim().Replace("+", "");
                            if (int.TryParse(qtyStr, out int qty))
                            {
                                // Solo sumamos a la pérdida si el ajuste fue negativo
                                if (qty < 0)
                                {
                                    AuditTotalAdjustedPieces += Math.Abs(qty);
                                    if (audit.Product != null)
                                    {
                                        AuditTotalFinancialLoss += Math.Abs(qty * audit.Product.Price);
                                    }
                                }
                            }
                        }
                    }
                    catch { /* Skip malformed notes */ }
                }
            }
        }
    }

    public class ProductSalesInfo
    {
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class HourlySalesInfo
    {
        public int Hour { get; set; }
        public string HourDisplay { get; set; }
        public int SaleCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}

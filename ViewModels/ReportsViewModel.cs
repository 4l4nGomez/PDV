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
        private DateTime _startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today;

        [ObservableProperty]
        private decimal _totalSales;

        public ReportsViewModel()
        {
            _context = new AppDbContext();
            LoadData();
        }

        [RelayCommand]
        private void LoadData()
        {
            var start = StartDate.Date;
            var end = EndDate.Date.AddDays(1).AddTicks(-1);

            var salesList = _context.Sales
                .Include(s => s.User)
                .Include(s => s.Shift)
                .Where(s => s.SaleDate >= start && s.SaleDate <= end)
                .OrderByDescending(s => s.SaleDate)
                .ToList();
            
            Sales = new ObservableCollection<Sale>(salesList);
            TotalSales = salesList.Sum(s => s.TotalAmount);

            Shifts = new ObservableCollection<Shift>(
                _context.Shifts
                .Include(s => s.User)
                .Where(s => s.StartTime >= start && s.StartTime <= end)
                .OrderByDescending(s => s.StartTime)
                .ToList()
            );

            Movements = new ObservableCollection<CashMovement>(
                _context.CashMovements
                .Include(m => m.User)
                .Where(m => m.MovementDate >= start && m.MovementDate <= end)
                .OrderByDescending(m => m.MovementDate)
                .ToList()
            );
        }
    }
}

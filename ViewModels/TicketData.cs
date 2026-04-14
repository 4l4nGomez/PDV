using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BakeryPOS.ViewModels
{
    public class TicketData : INotifyPropertyChanged
    {
        private DateTime _saleDate = DateTime.Now;
        public DateTime SaleDate { get => _saleDate; set { _saleDate = value; OnPropertyChanged(); } }

        private string _cashierName = "Cajero";
        public string CashierName { get => _cashierName; set { _cashierName = value; OnPropertyChanged(); } }

        private string _paymentMethod = "Efectivo";
        public string PaymentMethod { get => _paymentMethod; set { _paymentMethod = value; OnPropertyChanged(); } }

        private decimal _totalAmount;
        public decimal TotalAmount { get => _totalAmount; set { _totalAmount = value; OnPropertyChanged(); } }

        private ObservableCollection<TicketItemData> _items = new ObservableCollection<TicketItemData>();
        public ObservableCollection<TicketItemData> Items { get => _items; set { _items = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class TicketItemData : INotifyPropertyChanged
    {
        private string _productName;
        public string ProductName { get => _productName; set { _productName = value; OnPropertyChanged(); } }

        private int _quantity;
        public int Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(); } }

        private decimal _unitPrice;
        public decimal UnitPrice { get => _unitPrice; set { _unitPrice = value; OnPropertyChanged(); } }

        private decimal _subTotal;
        public decimal SubTotal { get => _subTotal; set { _subTotal = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

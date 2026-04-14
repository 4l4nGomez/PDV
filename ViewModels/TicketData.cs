using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BakeryPOS.ViewModels
{
    public partial class TicketData : ObservableObject
    {
        [ObservableProperty] private DateTime _saleDate;
        [ObservableProperty] private string _cashierName;
        [ObservableProperty] private string _paymentMethod;
        [ObservableProperty] private decimal _totalAmount;
        [ObservableProperty] private ObservableCollection<TicketItemData> _items = new();
    }

    public partial class TicketItemData : ObservableObject
    {
        [ObservableProperty] private string _productName;
        [ObservableProperty] private int _quantity;
        [ObservableProperty] private decimal _unitPrice;
        [ObservableProperty] private decimal _subTotal;
    }
}

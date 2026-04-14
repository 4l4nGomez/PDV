using System.Collections.ObjectModel;

namespace BakeryPOS.ViewModels
{
    public class TicketData
    {
        public System.DateTime SaleDate { get; set; }
        public string CashierName { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; }
        public ObservableCollection<TicketItemData> Items { get; set; } = new();
    }

    public class TicketItemData
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }
}

namespace BakeryPOS.ViewModels
{
    /// <summary>
    /// DTO used as DataContext for the TicketView window.
    /// WPF cannot bind to anonymous types, so we need a proper class.
    /// </summary>
    public class TicketData
    {
        public System.DateTime SaleDate { get; set; }
        public string CashierName { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; }
        public System.Collections.Generic.List<TicketItemData> Items { get; set; } = new();
    }

    public class TicketItemData
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }
}

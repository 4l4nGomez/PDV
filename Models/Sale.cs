using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BakeryPOS.Models
{
    public class Sale
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime SaleDate { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int ShiftId { get; set; }
        public Shift Shift { get; set; }
        
        public decimal TotalAmount { get; set; }
        
        public string PaymentMethod { get; set; } = "Efectivo";
        
        public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    }
}

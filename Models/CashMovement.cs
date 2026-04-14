using System;
using System.ComponentModel.DataAnnotations;

namespace BakeryPOS.Models
{
    public class CashMovement
    {
        [Key]
        public int Id { get; set; }
        
        public int ShiftId { get; set; }
        public Shift Shift { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; }
        
        // Positive amount for Inbox (Ingreso), Negative for Expense (Gasto)
        public decimal Amount { get; set; }
        
        public string Description { get; set; }
        
        public DateTime MovementDate { get; set; }
    }
}

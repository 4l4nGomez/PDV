using System;
using System.ComponentModel.DataAnnotations;

namespace BakeryPOS.Models
{
    public class Shift
    {
        [Key]
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; }
        
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        public decimal StartingCash { get; set; }
        
        public decimal ExpectedEndingCash { get; set; }
        public decimal ActualEndingCash { get; set; }
        
        // Snapshots at closure
        public decimal TotalSales { get; set; }
        public decimal TotalInflows { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalShrinkage { get; set; }
        
        public bool IsClosed { get; set; }
    }
}

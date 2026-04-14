using System;
using System.ComponentModel.DataAnnotations;

namespace BakeryPOS.Models
{
    public class ProductionLog
    {
        [Key]
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        public Product Product { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; }
        
        public DateTime ProductionDate { get; set; }
        
        public int QuantityProduced { get; set; }
    }
}

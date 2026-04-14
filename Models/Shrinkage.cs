using System;
using System.ComponentModel.DataAnnotations;

namespace BakeryPOS.Models
{
    public class Shrinkage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        public string Reason { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [Required]
        public int UserId { get; set; }
        public virtual User User { get; set; }
    }
}

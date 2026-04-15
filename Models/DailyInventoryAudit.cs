using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BakeryPOS.Models
{
    public class DailyInventoryAudit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        // FK hacia Producto - explícitamente nullable
        public int? ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // Stock físico - nullable por si se registra una nota sin stock
        public int? PhysicalStock { get; set; }

        // Usuario - nullable por seguridad
        public int? UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public string? Note { get; set; }
    }
}

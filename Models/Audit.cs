using System;
using System.ComponentModel.DataAnnotations;

namespace BakeryPOS.Models
{
    public class Audit
    {
        [Key]
        public int Id { get; set; }

        // Usuario que realizó la acción (0 para sistema/anon)
        public int UserId { get; set; }

        // Nombre de la acción (CreateSale, OpenShift, AddMovement, SaveAudit, CloseShift, etc.)
        public string Action { get; set; }

        // Entidad afectada (Sale, Shift, CashMovement, Product, DailyInventoryAudit)
        public string Entity { get; set; }

        // Datos libres en formato corto (puede ser JSON)
        public string Data { get; set; }

        public DateTime Timestamp { get; set; }

        // Opcionalmente asociar al turno vigente
        public int? ShiftId { get; set; }
    }
}
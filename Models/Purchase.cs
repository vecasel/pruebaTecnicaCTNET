using System.ComponentModel.DataAnnotations.Schema;

namespace SacRiosDesiertoApi.Models
{
    public class Purchase
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public Client Client { get; set; } = default!;

        // Monto de la compra
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Fecha de la compra
        public DateTime PurchaseDate { get; set; }

        public string? Description { get; set; }

        public string? OrderNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

using System.ComponentModel.DataAnnotations;

namespace SacRiosDesiertoApi.Models
{
    public class Client
    {
        public int Id { get; set; }

        public int DocumentTypeId { get; set; }
        public DocumentType DocumentType { get; set; } = default!;

        [MaxLength(30)]
        public string DocumentNumber { get; set; } = default!;

        [MaxLength(100)]
        public string FirstName { get; set; } = default!;

        [MaxLength(100)]
        public string LastName { get; set; } = default!;

        [MaxLength(200)]
        public string Email { get; set; } = default!;

        [MaxLength(30)]
        public string Phone { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}

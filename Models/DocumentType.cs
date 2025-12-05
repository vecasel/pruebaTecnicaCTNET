namespace SacRiosDesiertoApi.Models
{
    public class DocumentType
    {
        public int Id { get; set; }

        // CC, NIT, PAS
        public string Code { get; set; } = default!;

        public string Name { get; set; } = default!;

        public ICollection<Client> Clients { get; set; } = new List<Client>();
    }
}

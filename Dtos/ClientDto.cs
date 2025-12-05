using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace SacRiosDesiertoApi.Dtos
{
    public class ClientDto
    {
        public DocumentTypeDto DocumentType { get; set; } = default!;
        public string DocumentNumber { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public List<PurchaseDto> Purchases { get; set; } = new();
    }
}

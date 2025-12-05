using System.Text.Json.Serialization;


namespace SacRiosDesiertoApi.Dtos
{
    public class PurchaseDto
    {
        public decimal Amount { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string? Description { get; set; }
        public string? OrderNumber { get; set; }
    }
}

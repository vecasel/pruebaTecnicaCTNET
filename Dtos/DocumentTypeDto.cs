using System.Text.Json.Serialization;


namespace SacRiosDesiertoApi.Dtos
{
    public class DocumentTypeDto
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}

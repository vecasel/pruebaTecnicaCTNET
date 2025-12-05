using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SacRiosDesiertoApi.Data;
using SacRiosDesiertoApi.Dtos;

namespace SacRiosDesiertoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/client/search?document_type=CC&document_number=123
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery(Name = "document_type")] string documentTypeCode,
                                                [FromQuery(Name = "document_number")] string documentNumber)
        {
            if (string.IsNullOrWhiteSpace(documentTypeCode) ||
                string.IsNullOrWhiteSpace(documentNumber))
            {
                return BadRequest(new { detail = "Parámetros requeridos: document_type y document_number" });
            }

            var documentType = await _context.DocumentTypes
                .FirstOrDefaultAsync(d => d.Code == documentTypeCode);

            if (documentType == null)
            {
                return BadRequest(new { detail = "Tipo de documento no válido" });
            }

            var client = await _context.Clients
                .Include(c => c.DocumentType)
                .Include(c => c.Purchases)
                .FirstOrDefaultAsync(c =>
                    c.DocumentTypeId == documentType.Id &&
                    c.DocumentNumber == documentNumber);

            if (client == null)
            {
                return NotFound(new { detail = "Cliente no encontrado" });
            }

            var result = new ClientDto
            {
                DocumentType = new DocumentTypeDto
                {
                    Code = client.DocumentType.Code,
                    Name = client.DocumentType.Name
                },
                DocumentNumber = client.DocumentNumber,
                FirstName = client.FirstName,
                LastName = client.LastName,
                Email = client.Email,
                Phone = client.Phone,
                Purchases = client.Purchases.Select(p => new PurchaseDto
                {
                    Amount = p.Amount,
                    PurchaseDate = p.PurchaseDate,
                    Description = p.Description,
                    OrderNumber = p.OrderNumber
                }).ToList()
            };

            return Ok(result);
        }

        // GET: /api/client/export?document_type=CC&document_number=1022422328
        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery(Name = "document_type")] string documentTypeCode,
            [FromQuery(Name = "document_number")] string documentNumber)
        {
            if (string.IsNullOrWhiteSpace(documentTypeCode) ||
                string.IsNullOrWhiteSpace(documentNumber))
            {
                return BadRequest(new { detail = "Parámetros requeridos: document_type y document_number" });
            }

            var documentType = await _context.DocumentTypes
                .FirstOrDefaultAsync(d => d.Code == documentTypeCode);

            if (documentType == null)
            {
                return BadRequest(new { detail = "Tipo de documento no válido" });
            }

            var client = await _context.Clients
                .Include(c => c.DocumentType)
                .Include(c => c.Purchases)
                .FirstOrDefaultAsync(c =>
                    c.DocumentTypeId == documentType.Id &&
                    c.DocumentNumber == documentNumber);

            if (client == null)
            {
                return NotFound(new { detail = "Cliente no encontrado" });
            }

            var sb = new StringBuilder();

            // Sección 1: datos del cliente
            sb.AppendLine("Datos del cliente");
            sb.AppendLine("Tipo documento;Número de documento;Nombre;Apellido;Correo;Teléfono");
            sb.AppendLine($"{client.DocumentType.Code};{client.DocumentNumber};{client.FirstName};{client.LastName};{client.Email};{client.Phone}");
            sb.AppendLine(); // línea en blanco

            // Sección 2: compras
            sb.AppendLine("Compras del cliente");
            sb.AppendLine("Fecha de compra;Monto;Descripción;Número de orden");

            foreach (var p in client.Purchases.OrderBy(p => p.PurchaseDate))
            {
                // Usamos ToString("yyyy-MM-dd") para un formato estable
                var dateStr = p.PurchaseDate.ToString("yyyy-MM-dd");
                var amountStr = p.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var desc = p.Description ?? string.Empty;
                var orderNumber = p.OrderNumber ?? string.Empty;

                sb.AppendLine($"{dateStr};{amountStr};{desc};{orderNumber}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var filename = $"cliente_{client.DocumentType.Code}_{client.DocumentNumber}.csv";

            return File(
                bytes,
                "text/csv; charset=utf-8",
                filename
            );
        }

    }
}

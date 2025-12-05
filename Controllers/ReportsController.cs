using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using SacRiosDesiertoApi.Data;

namespace SacRiosDesiertoApi.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/reports/loyal-customers
        [HttpGet("loyal-customers")]
        public async Task<IActionResult> GetLoyalCustomersReport()
        {
            // Tomamos últimos 30 días como "último mes"
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-30);

            var purchasesQuery = _context.Purchases
                .Include(p => p.Client)
                    .ThenInclude(c => c.DocumentType)
                .Where(p => p.PurchaseDate >= startDate);

            var purchases = await purchasesQuery.ToListAsync();

            if (!purchases.Any())
            {
                return NotFound(new { detail = "No hay compras registradas en el último mes." });
            }

            // Agrupar por cliente y sumar montos
            var grouped = purchases
                .GroupBy(p => new
                {
                    p.ClientId,
                    p.Client.DocumentType.Code,
                    p.Client.DocumentType.Name,
                    p.Client.DocumentNumber,
                    p.Client.FirstName,
                    p.Client.LastName,
                    p.Client.Email,
                    p.Client.Phone
                })
                .Select(g => new
                {
                    g.Key.ClientId,
                    DocumentTypeCode = g.Key.Code,
                    DocumentTypeName = g.Key.Name,
                    g.Key.DocumentNumber,
                    g.Key.FirstName,
                    g.Key.LastName,
                    g.Key.Email,
                    g.Key.Phone,
                    TotalLastMonth = g.Sum(p => p.Amount)
                })
                .ToList();

            const decimal threshold = 5_000_000m;

            var loyalClients = grouped
                .Where(c => c.TotalLastMonth > threshold)
                .OrderByDescending(c => c.TotalLastMonth)
                .ToList();

            if (!loyalClients.Any())
            {
                return NotFound(new { detail = "No hay clientes que superen el monto mínimo para fidelización." });
            }

            // Crear Excel en memoria
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Clientes_fidelizacion");

            // Encabezados
            worksheet.Cell(1, 1).Value = "Tipo documento";
            worksheet.Cell(1, 2).Value = "Nombre tipo documento";
            worksheet.Cell(1, 3).Value = "Número de documento";
            worksheet.Cell(1, 4).Value = "Nombre";
            worksheet.Cell(1, 5).Value = "Apellido";
            worksheet.Cell(1, 6).Value = "Correo";
            worksheet.Cell(1, 7).Value = "Teléfono";
            worksheet.Cell(1, 8).Value = "Total último mes";

            var currentRow = 2;

            foreach (var c in loyalClients)
            {
                worksheet.Cell(currentRow, 1).Value = c.DocumentTypeCode;
                worksheet.Cell(currentRow, 2).Value = c.DocumentTypeName;
                worksheet.Cell(currentRow, 3).Value = c.DocumentNumber;
                worksheet.Cell(currentRow, 4).Value = c.FirstName;
                worksheet.Cell(currentRow, 5).Value = c.LastName;
                worksheet.Cell(currentRow, 6).Value = c.Email;
                worksheet.Cell(currentRow, 7).Value = c.Phone;
                worksheet.Cell(currentRow, 8).Value = c.TotalLastMonth;

                currentRow++;
            }

            // Un poco de formato
            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var bytes = stream.ToArray();

            var filename = $"reporte_fidelizacion_{today:yyyyMMdd}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename
            );
        }
    }
}

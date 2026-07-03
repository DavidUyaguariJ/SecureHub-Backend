using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Dashboard.Dtos;

namespace SecureHub.Infrastructure.Documents
{
	public class AuditLogReportService : IAuditLogReportService
	{
		public byte[] GeneratePdf(IEnumerable<ImmutableAuditEventDto> items)
		{
			var list = items.ToList();
			var generatedAt = DateTimeOffset.Now;

			return Document.Create(doc =>
			{
				doc.Page(page =>
				{
					page.Size(PageSizes.A4.Landscape());
					page.Margin(25);
					page.DefaultTextStyle(x => x.FontSize(8));

					page.Header().Column(col =>
					{
						col.Item().Text("SecureHub — Bitácora de Auditoría Inmutable")
							.FontSize(14).Bold();
						col.Item().Text($"Generado: {generatedAt:dd/MM/yyyy HH:mm} — {list.Count} registro(s)")
							.FontSize(8).FontColor(Colors.Grey.Medium);
						col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
					});

					page.Content().Table(table =>
					{
						table.ColumnsDefinition(c =>
						{
							c.RelativeColumn(2);
							c.RelativeColumn(2);
							c.RelativeColumn(2);
							c.RelativeColumn(2);
							c.RelativeColumn(2);
							c.RelativeColumn(1);
							c.RelativeColumn(3);
						});

						table.Header(h =>
						{
							h.Cell().Element(HeaderCell).Text("Fecha");
							h.Cell().Element(HeaderCell).Text("Acción");
							h.Cell().Element(HeaderCell).Text("Estado");
							h.Cell().Element(HeaderCell).Text("Realizado por");
							h.Cell().Element(HeaderCell).Text("IP");
							h.Cell().Element(HeaderCell).Text("Blockchain");
							h.Cell().Element(HeaderCell).Text("Hash de integridad");
						});

						foreach (var e in list)
						{
							table.Cell().Element(BodyCell).Text(e.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
							table.Cell().Element(BodyCell).Text(e.Action);
							table.Cell().Element(BodyCell).Text($"{e.PreviousStatus ?? "—"} → {e.NewStatus ?? "—"}");
							table.Cell().Element(BodyCell).Text($"{e.PerformedByName ?? "—"} ({e.PerformedByRole ?? "—"})");
							table.Cell().Element(BodyCell).Text(e.IpAddress ?? "—");
							table.Cell().Element(BodyCell).Text(e.BlockchainAnchored ? "Sí" : "No");
							table.Cell().Element(BodyCell).Text(e.IntegrityHash).FontSize(6);
						}
					});

					page.Footer().AlignCenter().Text(t =>
					{
						t.DefaultTextStyle(x => x.FontSize(7).FontColor(Colors.Grey.Medium));
						t.Span("SecureHub — Documento generado automáticamente. Página ");
						t.CurrentPageNumber();
						t.Span(" de ");
						t.TotalPages();
					});
				});
			}).GeneratePdf();
		}

		public byte[] GenerateExcel(IEnumerable<ImmutableAuditEventDto> items)
		{
			using var wb = new XLWorkbook();
			var ws = wb.Worksheets.Add("Bitácora de Auditoría");

			string[] headers = {
				"Fecha", "Acción", "Estado Previo", "Estado Nuevo",
				"Realizado por", "Rol", "Notas", "IP",
				"Anclado en Blockchain", "Hash de Integridad"
			};
			for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
			ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
			ws.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.LightGray;

			int r = 2;
			foreach (var e in items)
			{
				ws.Cell(r, 1).Value = e.CreatedAt.ToString("dd/MM/yyyy HH:mm");
				ws.Cell(r, 2).Value = e.Action;
				ws.Cell(r, 3).Value = e.PreviousStatus ?? "—";
				ws.Cell(r, 4).Value = e.NewStatus ?? "—";
				ws.Cell(r, 5).Value = e.PerformedByName ?? "—";
				ws.Cell(r, 6).Value = e.PerformedByRole ?? "—";
				ws.Cell(r, 7).Value = e.Notes ?? "";
				ws.Cell(r, 8).Value = e.IpAddress ?? "—";
				ws.Cell(r, 9).Value = e.BlockchainAnchored ? "Sí" : "No";
				ws.Cell(r, 10).Value = e.IntegrityHash;
				r++;
			}
			ws.Columns().AdjustToContents();
			ws.SheetView.FreezeRows(1);

			using var ms = new MemoryStream();
			wb.SaveAs(ms);
			return ms.ToArray();
		}

		private static IContainer HeaderCell(IContainer c) =>
			c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(x => x.Bold());

		private static IContainer BodyCell(IContainer c) =>
			c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
	}
}

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Arco.Dtos;

namespace SecureHub.Infrastructure.Documents
{
	public class ArcoResponsePdfService : IArcoResponsePdfService
	{
		static ArcoResponsePdfService()
		{
			QuestPDF.Settings.License = LicenseType.Community;
		}

		public byte[] Generate(ArcoRequestDetailDto detail)
		{
			var req = detail.Request;

			return Document.Create(container =>
			{
				container.Page(page =>
				{
					page.Size(PageSizes.A4);
					page.Margin(40);
					page.DefaultTextStyle(x => x.FontSize(11));

					page.Header().Column(col =>
					{
						col.Item().Row(row =>
						{
							row.RelativeItem().Column(c =>
							{
								c.Item().Text("Newbie EC S.A.S").Bold().FontSize(16);
								c.Item().Text("Sistema de Cumplimiento LOPDP")
									.FontSize(10).FontColor(Colors.Grey.Medium);
							});
							row.ConstantItem(140).AlignRight()
								.Text($"Quito, {DateTime.Now:dd/MM/yyyy}")
								.FontSize(9);
						});
						col.Item().PaddingTop(4)
							.LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
					});

					page.Content().PaddingTop(20).Column(col =>
					{
						col.Item().AlignCenter()
							.Text("RESPUESTA OFICIAL — DERECHOS ARCO")
							.Bold().FontSize(13);

						col.Item().PaddingTop(2).AlignCenter()
							.Text(FormatRequestType(req.RequestType))
							.FontSize(11).FontColor(Colors.Grey.Darken2);

						col.Item().PaddingVertical(14)
							.LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

						// Datos
						col.Item().Text("Datos de la solicitud").Bold().FontSize(12);
						col.Item().PaddingTop(8).Table(table =>
						{
							table.ColumnsDefinition(c =>
							{
								c.ConstantColumn(160);
								c.RelativeColumn();
							});

							void Row(string label, string value)
							{
								table.Cell().PaddingVertical(3)
									.Text(label).SemiBold().FontColor(Colors.Grey.Darken2);
								table.Cell().PaddingVertical(3).Text(value);
							}

							Row("N° de solicitud:", req.Id.ToString());
							Row("Titular:", req.SubjectFullName);
							Row("Tipo de derecho:", FormatRequestType(req.RequestType));
							Row("Estado:", req.Status);
							Row("Fecha solicitud:", req.RequestedAt.ToString("dd/MM/yyyy HH:mm"));
							Row("Fecha límite LOPDP:", req.DueDate?.ToString("dd/MM/yyyy") ?? "—");
							Row("Fecha resolución:", req.ResolvedAt?.ToString("dd/MM/yyyy HH:mm") ?? "—");
						});

						col.Item().PaddingVertical(14)
							.LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

						col.Item().Text("Descripción del titular").Bold().FontSize(12);
						col.Item().PaddingTop(6)
							.Background(Colors.Grey.Lighten4).Padding(10)
							.Text(req.Description ?? "Sin descripción.");

						col.Item().PaddingVertical(14)
							.LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

						col.Item().Text("Respuesta oficial").Bold().FontSize(12);
						col.Item().PaddingTop(6)
							.Text(req.ResponseText ?? "Sin respuesta registrada.");

						if (req.RejectedReason != null)
						{
							col.Item().PaddingTop(10).Text("Motivo de rechazo").Bold().FontSize(12);
							col.Item().PaddingTop(6)
								.Background(Colors.Red.Lighten4).Padding(10)
								.Text(req.RejectedReason).FontColor(Colors.Red.Darken2);
						}

						// Timeline de auditoría
						if (detail.AuditLogs.Any())
						{
							col.Item().PaddingVertical(14)
								.LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

							col.Item().Text("Historial de acciones").Bold().FontSize(12);
							col.Item().PaddingTop(8).Table(table =>
							{
								table.ColumnsDefinition(c =>
								{
									c.ConstantColumn(140);
									c.ConstantColumn(120);
									c.RelativeColumn();
								});

								table.Header(h =>
								{
									h.Cell().Text("Fecha").SemiBold();
									h.Cell().Text("Acción").SemiBold();
									h.Cell().Text("Notas").SemiBold();
								});

								foreach (var log in detail.AuditLogs)
								{
									table.Cell().PaddingVertical(2).Text(log.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
									table.Cell().PaddingVertical(2).Text(log.Action);
									table.Cell().PaddingVertical(2)
										.Text(log.Notes ?? "—").FontColor(Colors.Grey.Darken1);
								}
							});
						}

						col.Item().PaddingVertical(14)
							.LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

						col.Item().Text("Base legal").Bold().FontSize(12);
						col.Item().PaddingTop(6).Text(
							"Respuesta emitida conforme a la Ley Orgánica de Protección de Datos " +
							"Personales (LOPDP, Registro Oficial Suplemento 459, 26/05/2021), " +
							"artículos 12 al 17.")
							.FontSize(10).FontColor(Colors.Grey.Darken1);

						col.Item().PaddingTop(36).Row(row =>
						{
							row.RelativeItem().Column(c =>
							{
								c.Item().LineHorizontal(0.5f);
								c.Item().PaddingTop(4).AlignCenter()
									.Text("Responsable del Tratamiento").FontSize(10);
								c.Item().AlignCenter()
									.Text("Newbie EC S.A.S").FontSize(9).FontColor(Colors.Grey.Medium);
							});
							row.ConstantItem(60);
							row.RelativeItem().Column(c =>
							{
								c.Item().LineHorizontal(0.5f);
								c.Item().PaddingTop(4).AlignCenter()
									.Text("Sello / Fecha").FontSize(10);
								c.Item().AlignCenter()
									.Text(DateTime.Now.ToString("dd/MM/yyyy"))
									.FontSize(9).FontColor(Colors.Grey.Medium);
							});
						});
					});

					page.Footer().AlignCenter().Text(x =>
					{
						x.Span("Página ").FontSize(9).FontColor(Colors.Grey.Medium);
						x.CurrentPageNumber().FontSize(9);
						x.Span(" de ").FontSize(9).FontColor(Colors.Grey.Medium);
						x.TotalPages().FontSize(9);
						x.Span(" — Documento generado por SecureHub")
							.FontSize(9).FontColor(Colors.Grey.Lighten1);
					});
				});
			}).GeneratePdf();
		}

		private static string FormatRequestType(string type) => type switch
		{
			"ACCESO" => "Acceso a datos personales",
			"RECTIFICACION" => "Rectificación de datos",
			"CANCELACION" => "Cancelación / Eliminación",
			"OPOSICION" => "Oposición al tratamiento",
			"PORTABILIDAD" => "Portabilidad de datos",
			_ => type
		};
	}
}
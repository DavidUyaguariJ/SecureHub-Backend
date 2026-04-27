using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SecureHub.Application.UsesCases.Arco.Dtos;

namespace SecureHub.Api.Services
{
	public class ArcoResponsePdfService
	{
		private static readonly string ColorPrimary = "#1a1a2e";
		private static readonly string ColorAccent = "#16213e";
		private static readonly string ColorGreen = "#0f3460";
		private static readonly string ColorText = "#2d2d2d";
		private static readonly string ColorMuted = "#6b7280";
		private static readonly string ColorBorder = "#e5e7eb";
		// ← string en lugar de Colors.White para que el ternario sea string/string
		private static readonly string ColorWhite = "#ffffff";
		private static readonly string ColorRowAlt = "#f9fafb";

		public byte[] Generate(ArcoRequestDetailDto detail)
		{
			QuestPDF.Settings.License = LicenseType.Community;

			var req = detail.Request;
			var logs = detail.AuditLogs.ToList();

			return Document.Create(container =>
			{
				container.Page(page =>
				{
					page.Size(PageSizes.A4);
					page.Margin(40);
					page.DefaultTextStyle(t => t.FontSize(10).FontColor(ColorText));

					// ── HEADER ────────────────────────────────────────────────────
					page.Header().Column(col =>
					{
						col.Item().Row(row =>
						{
							row.RelativeItem().Column(c =>
							{
								c.Item().Text("NewbIe").FontSize(22).Bold().FontColor(ColorPrimary);
								c.Item().Text("SecureHub — Plataforma de Protección de Datos")
									.FontSize(9).FontColor(ColorMuted);
							});
							row.ConstantItem(160).AlignRight().Column(c =>
							{
								c.Item().Text("RESPUESTA OFICIAL").FontSize(11).Bold().FontColor(ColorGreen);
								c.Item().Text($"N° {req.Id.ToString().ToUpper()[..8]}")
									.FontSize(9).FontColor(ColorMuted);
								c.Item().Text($"Emitido: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC")
									.FontSize(8).FontColor(ColorMuted);
							});
						});
						col.Item().PaddingTop(6).BorderBottom(1.5f).BorderColor(ColorPrimary);
						col.Item().PaddingTop(4)
							.Text("Documento generado en cumplimiento de la Ley Orgánica de Protección de Datos Personales (LOPDP) del Ecuador")
							.FontSize(8).Italic().FontColor(ColorMuted);
					});

					// ── CONTENT ───────────────────────────────────────────────────
					page.Content().PaddingTop(16).Column(col =>
					{
						SectionTitle(col, "Datos de la Solicitud");
						col.Item().Table(table =>
						{
							table.ColumnsDefinition(c =>
							{
								c.RelativeColumn(1);
								c.RelativeColumn(2);
								c.RelativeColumn(1);
								c.RelativeColumn(2);
							});
							AddRow(table, "Titular", req.SubjectFullName);
							AddRow(table, "Tipo de derecho", MapRequestType(req.RequestType));
							AddRow(table, "Estado", MapStatus(req.Status));
							AddRow(table, "Solicitado", req.RequestedAt.ToString("dd/MM/yyyy HH:mm"));
							AddRow(table, "Fecha límite", req.DueDate?.ToString("dd/MM/yyyy") ?? "—");
							AddRow(table, "Resuelto", req.ResolvedAt?.ToString("dd/MM/yyyy HH:mm") ?? "—");
						});

						if (!string.IsNullOrWhiteSpace(req.Description))
						{
							col.Item().PaddingTop(14);
							SectionTitle(col, "Descripción del Titular");
							col.Item().Background(ColorBorder).Padding(10)
								.Text(SanitizeDescription(req.Description))
								.FontSize(9.5f).LineHeight(1.4f);
						}

						col.Item().PaddingTop(14);
						SectionTitle(col, "Respuesta Oficial");

						if (!string.IsNullOrWhiteSpace(req.RejectedReason))
						{
							col.Item().Background("#fef2f2").Border(1).BorderColor("#fca5a5")
								.Padding(10).Column(c =>
								{
									c.Item().Text("SOLICITUD RECHAZADA").Bold().FontColor("#dc2626");
									c.Item().PaddingTop(4).Text(req.RejectedReason).FontSize(9.5f).LineHeight(1.4f);
								});
						}
						else if (!string.IsNullOrWhiteSpace(req.ResponseText))
						{
							col.Item().Background("#f0fdf4").Border(1).BorderColor("#86efac")
								.Padding(10).Text(req.ResponseText).FontSize(9.5f).LineHeight(1.4f);
						}
						else
						{
							col.Item().Background(ColorRowAlt).Border(1).BorderColor(ColorBorder)
								.Padding(10).Text("Sin respuesta registrada.").FontColor(ColorMuted).Italic();
						}

						if (logs.Any())
						{
							col.Item().PaddingTop(14);
							SectionTitle(col, "Historial de Acciones");
							col.Item().Table(table =>
							{
								table.ColumnsDefinition(c =>
								{
									c.RelativeColumn(2);
									c.RelativeColumn(1.5f);
									c.RelativeColumn(1);
									c.RelativeColumn(3);
								});
								table.Header(h =>
								{
									foreach (var hdr in new[] { "Acción", "Estado", "Rol", "Notas" })
									{
										h.Cell().Background(ColorPrimary).Padding(5)
											.Text(hdr).FontColor(ColorWhite).Bold().FontSize(9);
									}
								});

								bool alt = false;
								foreach (var log in logs)
								{
									// ← ambos son string ahora → sin CS0172
									var bg = alt ? ColorRowAlt : ColorWhite;
									table.Cell().Background(bg).Padding(4)
										.Text($"{log.Action}\n{log.CreatedAt:dd/MM/yyyy HH:mm}").FontSize(8.5f);
									table.Cell().Background(bg).Padding(4)
										.Text(MapStatus(log.NewStatus ?? "")).FontSize(8.5f);
									table.Cell().Background(bg).Padding(4)
										.Text(log.PerformedByRole ?? "—").FontSize(8.5f);
									table.Cell().Background(bg).Padding(4)
										.Text(log.Notes ?? "—").FontSize(8.5f);
									alt = !alt;
								}
							});
						}

						col.Item().PaddingTop(20)
							.BorderTop(1).BorderColor(ColorBorder).PaddingTop(8)
							.Text("Este documento tiene validez como constancia del ejercicio de derechos ARCO " +
								  "conforme a la LOPDP. Para consultas: protecciondatos@securehub.com")
							.FontSize(7.5f).Italic().FontColor(ColorMuted);
					});

					// ── FOOTER ────────────────────────────────────────────────────
					page.Footer().AlignCenter()
						.Text(t =>
						{
							t.Span("SecureHub · NewbIe — ").FontSize(8).FontColor(ColorMuted);
							t.Span("Página ").FontSize(8).FontColor(ColorMuted);
							t.CurrentPageNumber().FontSize(8).FontColor(ColorMuted);
							t.Span(" de ").FontSize(8).FontColor(ColorMuted);
							t.TotalPages().FontSize(8).FontColor(ColorMuted);
						});
				});
			}).GeneratePdf();
		}

		private static void SectionTitle(ColumnDescriptor col, string title)
		{
			col.Item().Background(ColorAccent).Padding(6)
				.Text(title).Bold().FontColor(ColorWhite).FontSize(10);
			col.Item().PaddingBottom(4);
		}

		private static void AddRow(TableDescriptor table, string label, string value)
		{
			table.Cell().Background(ColorRowAlt).Padding(5)
				.Text(label).Bold().FontSize(9).FontColor(ColorText);
			table.Cell().Padding(5).Text(value).FontSize(9);
		}

		private static string MapRequestType(string type) => type switch
		{
			"ACCESO" => "Acceso a datos personales",
			"RECTIFICACION" => "Rectificación de datos",
			"CANCELACION" => "Cancelación / Eliminación de datos",
			"OPOSICION" => "Oposición al tratamiento",
			"PORTABILIDAD" => "Portabilidad de datos",
			_ => type
		};

		private static string MapStatus(string status) => status switch
		{
			"PENDIENTE" => "Pendiente",
			"EN_PROCESO" => "En proceso",
			"COMPLETADO" => "Completado",
			"RECHAZADO" => "Rechazado",
			_ => status
		};

		private static string SanitizeDescription(string desc)
		{
			if (desc.Contains('|'))
			{
				var parts = desc.Split('|', 2);
				return $"Datos a rectificar: {parts[0]}\n\n{parts[1]}";
			}
			return desc;
		}
	}
}

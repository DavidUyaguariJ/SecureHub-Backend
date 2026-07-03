using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.Dashboard.Dtos;

namespace SecureHub.Infrastructure.Documents
{
	public class DashboardReportService : IDashboardReportService
	{
		public byte[] GenerateSummaryPdf(
			DashboardSummaryDto summary,
			IEnumerable<ArcoByTypeDto> byType,
			IEnumerable<ArcoMonthlyTrendDto> trend,
			IEnumerable<ArcoAlertDto> arcoAlerts,
			IEnumerable<ContractAlertDto> contractAlerts)
		{
			var generatedAt = DateTimeOffset.Now;

			return Document.Create(doc =>
			{
				doc.Page(page =>
				{
					page.Size(PageSizes.A4);
					page.Margin(30);
					page.DefaultTextStyle(x => x.FontSize(10));

					page.Header().Column(col =>
					{
						col.Item().Text("SecureHub — Reporte de Cumplimiento LOPDP")
							.FontSize(16).Bold();
						col.Item().Text($"Generado: {generatedAt:dd/MM/yyyy HH:mm}")
							.FontSize(9).FontColor(Colors.Grey.Medium);
						col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
					});

					page.Content().Column(col =>
					{
						col.Spacing(14);

						// ── Resumen general
						col.Item().Element(c => SectionTitle(c, "Resumen General"));
						col.Item().Table(table =>
						{
							table.ColumnsDefinition(c =>
							{
								c.RelativeColumn();
								c.RelativeColumn();
							});

							AddSummaryRow(table, "Solicitudes ARCO totales", summary.TotalArcoRequests.ToString());
							AddSummaryRow(table, "Pendientes", summary.PendingArcoRequests.ToString());
							AddSummaryRow(table, "En proceso", summary.InProcessArcoRequests.ToString());
							AddSummaryRow(table, "Completadas", summary.CompletedArcoRequests.ToString());
							AddSummaryRow(table, "Rechazadas", summary.RejectedArcoRequests.ToString());
							AddSummaryRow(table, "Vencidas", summary.OverdueArcoRequests.ToString());
							AddSummaryRow(table, "Tiempo promedio de resolución", $"{summary.AverageResolutionDays} días");
							AddSummaryRow(table, "Tasa de cumplimiento", $"{summary.ComplianceRate}%");
							AddSummaryRow(table, "Contratos activos", summary.ActivePartContracts.ToString());
							AddSummaryRow(table, "Contratos vencidos", summary.ExpiredPartContracts.ToString());
							AddSummaryRow(table, "Contratos revocados", summary.RevokedPartContracts.ToString());
							AddSummaryRow(table, "Titulares registrados", summary.TotalSubjects.ToString());
							AddSummaryRow(table, "Titulares con biometría", summary.SubjectsWithBiometrics.ToString());
						});

						// ── Solicitudes por tipo
						col.Item().Element(c => SectionTitle(c, "Solicitudes ARCO por Tipo"));
						col.Item().Table(table =>
						{
							table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(); });
							table.Header(h =>
							{
								h.Cell().Element(HeaderCell).Text("Tipo");
								h.Cell().Element(HeaderCell).Text("Cantidad");
							});
							foreach (var t in byType)
							{
								table.Cell().Element(BodyCell).Text(t.RequestType);
								table.Cell().Element(BodyCell).Text(t.Count.ToString());
							}
						});

						// ── Tendencia mensual
						col.Item().Element(c => SectionTitle(c, "Tendencia Mensual"));
						col.Item().Table(table =>
						{
							table.ColumnsDefinition(c =>
							{
								c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
							});
							table.Header(h =>
							{
								h.Cell().Element(HeaderCell).Text("Mes");
								h.Cell().Element(HeaderCell).Text("Pendiente");
								h.Cell().Element(HeaderCell).Text("En proceso");
								h.Cell().Element(HeaderCell).Text("Completado");
								h.Cell().Element(HeaderCell).Text("Rechazado");
							});
							foreach (var m in trend)
							{
								table.Cell().Element(BodyCell).Text(m.Month);
								table.Cell().Element(BodyCell).Text(m.Pending.ToString());
								table.Cell().Element(BodyCell).Text(m.InProcess.ToString());
								table.Cell().Element(BodyCell).Text(m.Completed.ToString());
								table.Cell().Element(BodyCell).Text(m.Rejected.ToString());
							}
						});

						// ── Alertas ARCO
						col.Item().Element(c => SectionTitle(c, "Alertas — Solicitudes ARCO próximas a vencer"));
						if (!arcoAlerts.Any())
						{
							col.Item().Text("Sin alertas activas.").Italic().FontColor(Colors.Grey.Medium);
						}
						else
						{
							col.Item().Table(table =>
							{
								table.ColumnsDefinition(c =>
								{
									c.RelativeColumn(2); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
								});
								table.Header(h =>
								{
									h.Cell().Element(HeaderCell).Text("Titular");
									h.Cell().Element(HeaderCell).Text("Tipo");
									h.Cell().Element(HeaderCell).Text("Vence");
									h.Cell().Element(HeaderCell).Text("Estado");
								});
								foreach (var a in arcoAlerts)
								{
									table.Cell().Element(BodyCell).Text(a.SubjectName);
									table.Cell().Element(BodyCell).Text(a.RequestType);
									table.Cell().Element(BodyCell).Text(a.DueDate.ToString("dd/MM/yyyy"));
									table.Cell().Element(BodyCell).Text(a.IsOverdue ? $"Vencida ({a.DaysOverdue}d)" : $"Vence en {-a.DaysOverdue}d");
								}
							});
						}

						// ── Alertas de contratos
						col.Item().Element(c => SectionTitle(c, "Alertas — Contratos próximos a vencer"));
						if (!contractAlerts.Any())
						{
							col.Item().Text("Sin alertas activas.").Italic().FontColor(Colors.Grey.Medium);
						}
						else
						{
							col.Item().Table(table =>
							{
								table.ColumnsDefinition(c =>
								{
									c.RelativeColumn(2); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
								});
								table.Header(h =>
								{
									h.Cell().Element(HeaderCell).Text("Empresa");
									h.Cell().Element(HeaderCell).Text("Vence");
									h.Cell().Element(HeaderCell).Text("Días restantes");
									h.Cell().Element(HeaderCell).Text("Estado");
								});
								foreach (var c2 in contractAlerts)
								{
									table.Cell().Element(BodyCell).Text(c2.CompanyName);
									table.Cell().Element(BodyCell).Text(c2.ValidUntil.ToString("dd/MM/yyyy"));
									table.Cell().Element(BodyCell).Text(c2.DaysUntilExpiry.ToString());
									table.Cell().Element(BodyCell).Text(c2.Status);
								}
							});
						}
					});

					page.Footer().AlignCenter().Text(t =>
					{
						t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
						t.Span("SecureHub — Documento generado automáticamente. Página ");
						t.CurrentPageNumber();
						t.Span(" de ");
						t.TotalPages();
					});
				});
			}).GeneratePdf();
		}

		public byte[] GenerateSummaryExcel(
			DashboardSummaryDto summary,
			IEnumerable<ArcoByTypeDto> byType,
			IEnumerable<ArcoMonthlyTrendDto> trend,
			IEnumerable<ArcoAlertDto> arcoAlerts,
			IEnumerable<ContractAlertDto> contractAlerts)
		{
			using var wb = new XLWorkbook();

			// Hoja 1: Resumen
			var ws1 = wb.Worksheets.Add("Resumen");
			ws1.Cell(1, 1).Value = "Indicador";
			ws1.Cell(1, 2).Value = "Valor";
			ws1.Range(1, 1, 1, 2).Style.Font.Bold = true;

			var rows = new (string, object)[]
			{
				("Solicitudes ARCO totales", summary.TotalArcoRequests),
				("Pendientes", summary.PendingArcoRequests),
				("En proceso", summary.InProcessArcoRequests),
				("Completadas", summary.CompletedArcoRequests),
				("Rechazadas", summary.RejectedArcoRequests),
				("Vencidas", summary.OverdueArcoRequests),
				("Tiempo promedio de resolución (días)", summary.AverageResolutionDays),
				("Tasa de cumplimiento (%)", summary.ComplianceRate),
				("Contratos activos", summary.ActivePartContracts),
				("Contratos vencidos", summary.ExpiredPartContracts),
				("Contratos revocados", summary.RevokedPartContracts),
				("Titulares registrados", summary.TotalSubjects),
				("Titulares con biometría", summary.SubjectsWithBiometrics),
			};
			for (int i = 0; i < rows.Length; i++)
			{
				ws1.Cell(i + 2, 1).Value = rows[i].Item1;
				ws1.Cell(i + 2, 2).Value = rows[i].Item2.ToString();
			}
			ws1.Columns().AdjustToContents();

			// Hoja 2: Por tipo
			var ws2 = wb.Worksheets.Add("Por Tipo");
			ws2.Cell(1, 1).Value = "Tipo de Solicitud";
			ws2.Cell(1, 2).Value = "Cantidad";
			ws2.Range(1, 1, 1, 2).Style.Font.Bold = true;
			int r = 2;
			foreach (var t in byType)
			{
				ws2.Cell(r, 1).Value = t.RequestType;
				ws2.Cell(r, 2).Value = t.Count;
				r++;
			}
			ws2.Columns().AdjustToContents();

			// Hoja 3: Tendencia mensual
			var ws3 = wb.Worksheets.Add("Tendencia Mensual");
			string[] headers3 = { "Mes", "Pendiente", "En Proceso", "Completado", "Rechazado" };
			for (int i = 0; i < headers3.Length; i++) ws3.Cell(1, i + 1).Value = headers3[i];
			ws3.Range(1, 1, 1, headers3.Length).Style.Font.Bold = true;
			r = 2;
			foreach (var m in trend)
			{
				ws3.Cell(r, 1).Value = m.Month;
				ws3.Cell(r, 2).Value = m.Pending;
				ws3.Cell(r, 3).Value = m.InProcess;
				ws3.Cell(r, 4).Value = m.Completed;
				ws3.Cell(r, 5).Value = m.Rejected;
				r++;
			}
			ws3.Columns().AdjustToContents();

			// Hoja 4: Alertas ARCO
			var ws4 = wb.Worksheets.Add("Alertas ARCO");
			string[] headers4 = { "Titular", "Tipo", "Vence", "Días", "Vencida" };
			for (int i = 0; i < headers4.Length; i++) ws4.Cell(1, i + 1).Value = headers4[i];
			ws4.Range(1, 1, 1, headers4.Length).Style.Font.Bold = true;
			r = 2;
			foreach (var a in arcoAlerts)
			{
				ws4.Cell(r, 1).Value = a.SubjectName;
				ws4.Cell(r, 2).Value = a.RequestType;
				ws4.Cell(r, 3).Value = a.DueDate.ToString("dd/MM/yyyy");
				ws4.Cell(r, 4).Value = a.DaysOverdue;
				ws4.Cell(r, 5).Value = a.IsOverdue ? "Sí" : "No";
				r++;
			}
			ws4.Columns().AdjustToContents();

			// Hoja 5: Alertas contratos
			var ws5 = wb.Worksheets.Add("Alertas Contratos");
			string[] headers5 = { "Empresa", "Vence", "Días restantes", "Estado" };
			for (int i = 0; i < headers5.Length; i++) ws5.Cell(1, i + 1).Value = headers5[i];
			ws5.Range(1, 1, 1, headers5.Length).Style.Font.Bold = true;
			r = 2;
			foreach (var c2 in contractAlerts)
			{
				ws5.Cell(r, 1).Value = c2.CompanyName;
				ws5.Cell(r, 2).Value = c2.ValidUntil.ToString("dd/MM/yyyy");
				ws5.Cell(r, 3).Value = c2.DaysUntilExpiry;
				ws5.Cell(r, 4).Value = c2.Status;
				r++;
			}
			ws5.Columns().AdjustToContents();

			using var ms = new MemoryStream();
			wb.SaveAs(ms);
			return ms.ToArray();
		}

		private static void SectionTitle(IContainer container, string title)
		{
			container.Text(title).FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
		}

		private static void AddSummaryRow(TableDescriptor table, string label, string value)
		{
			table.Cell().Element(BodyCell).Text(label);
			table.Cell().Element(BodyCell).Text(value).Bold();
		}

		private static IContainer HeaderCell(IContainer c) =>
			c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(x => x.Bold());

		private static IContainer BodyCell(IContainer c) =>
			c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
	}
}

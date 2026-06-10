using QuestPDF.Infrastructure;
using SecureHub.Application.Interfaces;
using SecureHub.Application.UsesCases.ThirdPart.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Documents
{
	public class PartContractPdfService : IPartContractPdfService
	{
		public Task<byte[]> GenerateAsync(
			PartContractDto dto,
			bool blockchainActive,
			DateTimeOffset blockchainValidUntil,
			int blockchainStatus,
			CancellationToken ct = default)
		{
			QuestPDF.Settings.License = LicenseType.Community;

			var pdf = Document.Create(container =>
			{
				container.Page(page =>
				{
					page.Size(PageSizes.A4);
					page.Margin(40);
					page.DefaultTextStyle(x => x.FontSize(11));

					page.Header().Column(col =>
					{
						col.Item().Text("SecureHub — Contrato de Encargado de Datos")
							.FontSize(16).Bold().FontColor("#1a1a2e");
						col.Item().Text("Gestión Segura de Terceros (LOPDP)")
							.FontSize(11).FontColor("#6b7280");
						col.Item().LineHorizontal(1).LineColor("#e5e7eb");
					});

					page.Content().Column(col =>
					{
						col.Item().PaddingTop(16).Text("DATOS DEL CONTRATO").Bold().FontSize(13);

						// Tabla de datos principales
						col.Item().PaddingTop(8).Table(table =>
						{
							table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });

							void Row(string label, string value)
							{
								table.Cell().Background("#f9fafb").Padding(6).Text(label).Bold();
								table.Cell().Padding(6).Text(value);
							}

							Row("ID Contrato", dto.Id.ToString());
							Row("Empresa", dto.CompanyName);
							Row("Correo contacto", dto.ContactEmail);
							Row("Persona contacto", dto.ContactPerson ?? "—");
							Row("Finalidad", dto.PurposeDescription);
							Row("Usuario Keycloak", dto.KeycloakUsername ?? "—");
							Row("Válido desde", dto.ValidFrom.ToString("dd/MM/yyyy HH:mm"));
							Row("Válido hasta", dto.ValidUntil.ToString("dd/MM/yyyy HH:mm"));
							Row("Estado (BD)", dto.Status);
							if (dto.RevokedAt.HasValue)
							{
								Row("Revocado el", dto.RevokedAt.Value.ToString("dd/MM/yyyy HH:mm"));
								Row("Motivo", dto.RevokedReason ?? "—");
							}
						});

						// Campos permitidos
						col.Item().PaddingTop(16).Text("CAMPOS DE DATOS AUTORIZADOS").Bold().FontSize(13);
						col.Item().PaddingTop(4).Text(string.Join(", ", dto.AllowedFields))
							.FontColor("#374151");

						// Estado blockchain
						col.Item().PaddingTop(16).Text("VERIFICACIÓN BLOCKCHAIN").Bold().FontSize(13);
						col.Item().PaddingTop(8).Table(table =>
						{
							table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });

							void Row(string label, string value)
							{
								table.Cell().Background("#f0fdf4").Padding(6).Text(label).Bold();
								table.Cell().Padding(6).Text(value);
							}

							Row("Activo en blockchain", blockchainActive ? "✓ SÍ" : "✗ NO");
							Row("Vigencia blockchain", blockchainValidUntil.ToString("dd/MM/yyyy HH:mm"));
							Row("Estado blockchain", blockchainStatus switch { 0 => "ACTIVO", 1 => "SUSPENDIDO", 2 => "REVOCADO", _ => "DESCONOCIDO" });
							Row("TX Hash", dto.BlockchainTxHash ?? "Pendiente de registro");
						});

						// Campos del contrato
						if (!string.IsNullOrEmpty(dto.BlockchainTxHash))
						{
							col.Item().PaddingTop(8).Text(
								"Este contrato está registrado de forma inmutable en la red blockchain privada de SecureHub. " +
								"El hash de transacción es prueba criptográfica de su existencia y condiciones.")
								.FontSize(9).FontColor("#6b7280").Italic();
						}

						col.Item().PaddingTop(24).Text($"Generado: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC")
							.FontSize(9).FontColor("#9ca3af");
					});

					page.Footer().AlignCenter()
						.Text(x =>
						{
							x.Span("SecureHub — Sistema de Cumplimiento LOPDP | Página ");
							x.CurrentPageNumber();
							x.Span(" de ");
							x.TotalPages();
						});
				});
			});

			var bytes = pdf.GeneratePdf();
			return Task.FromResult(bytes);
		}
	}
}
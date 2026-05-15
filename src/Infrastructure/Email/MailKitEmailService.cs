using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SecureHub.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.EmailSender
{
	public class MailKitEmailService : IEmailService
	{
		private readonly string _host;
		private readonly int _port;
		private readonly string _username;
		private readonly string _password;
		private readonly string _fromName;
		private readonly string _fromAddress;
		private readonly string _portalUrl;

		public MailKitEmailService(IConfiguration config)
		{
			_host = config["Email:SmtpHost"] ?? throw new InvalidOperationException("Email:SmtpHost no configurado");
			_port = int.Parse(config["Email:SmtpPort"] ?? "587");
			_username = config["Email:Username"] ?? throw new InvalidOperationException("Email:Username no configurado");
			_password = config["Email:Password"] ?? throw new InvalidOperationException("Email:Password no configurado");
			_fromName = config["Email:FromName"] ?? "Newbie SecureHub";
			_fromAddress = config["Email:FromAddress"] ?? _username;
			_portalUrl = config["Email:PortalUrl"] ?? "https://des-app.securehub.com";
		}

		public async Task SendCredentialsAsync(
			string toEmail, string fullName, string username,
			string temporaryPassword, CancellationToken ct = default)
		{
			var emailSubject = "Bienvenido a SecureHub — Sus credenciales de acceso";
			var html = BuildBaseTemplate("Bienvenido a SecureHub",
				"<h2 style='color:#1a1a2e;font-size:18px'>Hola, " + fullName + "</h2>" +
				"<p style='color:#374151'>El técnico de Newbie ha registrado sus datos en el sistema SecureHub. " +
				"A continuación encontrará sus credenciales de acceso temporales:</p>" +
				"<div style='background:#f9fafb;border-left:4px solid #0f3460;padding:16px;border-radius:4px;margin:16px 0'>" +
				"<p style='margin:0'><strong>Usuario:</strong> " + username + "</p>" +
				"<p style='margin:8px 0 0'><strong>Contraseña temporal:</strong> " +
				"<code style='background:#e5e7eb;padding:2px 6px;border-radius:3px'>" + temporaryPassword + "</code></p>" +
				"</div>" +
				"<p style='color:#374151'>Al ingresar por primera vez se le pedirá cambiar su contraseña. " +
				"También deberá registrar sus datos biométricos faciales para poder ejercer sus derechos ARCO.</p>" +
				"<div style='text-align:center;margin:24px 0'>" +
				"<a href='" + _portalUrl + "' style='background:#1a1a2e;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold'>" +
				"Acceder al portal</a></div>");

			await SendAsync(toEmail, fullName, emailSubject, html, ct: ct);
		}


		public async Task SendArcoStatusChangedAsync(
			string toEmail, string fullName, string requestType, string newStatus,
			string? responseText, string? rejectedReason, CancellationToken ct = default)
		{
			var typeLabel = MapRequestType(requestType);
			var statusLabel = MapStatus(newStatus);
			var statusColor = newStatus switch
			{
				"COMPLETADO" => "#16a34a",
				"RECHAZADO" => "#dc2626",
				"EN_PROCESO" => "#2563eb",
				_ => "#d97706"
			};

			var detailBlock = string.Empty;
			if (newStatus == "RECHAZADO" && !string.IsNullOrWhiteSpace(rejectedReason))
			{
				detailBlock =
					"<div style='background:#fef2f2;border-left:4px solid #fca5a5;padding:12px;border-radius:4px;margin:12px 0'>" +
					"<strong style='color:#dc2626'>Motivo del rechazo:</strong>" +
					"<p style='margin:4px 0;color:#374151'>" + rejectedReason + "</p></div>";
			}
			else if (newStatus == "COMPLETADO" && !string.IsNullOrWhiteSpace(responseText))
			{
				detailBlock =
					"<div style='background:#f0fdf4;border-left:4px solid #86efac;padding:12px;border-radius:4px;margin:12px 0'>" +
					"<strong style='color:#16a34a'>Respuesta oficial:</strong>" +
					"<p style='margin:4px 0;color:#374151'>" + responseText + "</p></div>";
			}

			var pdfNote = (newStatus == "COMPLETADO" || newStatus == "RECHAZADO")
				? "<p style='color:#374151'>Adjunto encontrará el documento oficial de respuesta en formato PDF.</p>"
				: string.Empty;

			var emailSubject = "Actualización de su solicitud ARCO — " + typeLabel;
			var html = BuildBaseTemplate("Actualización ARCO",
				"<h2 style='color:#1a1a2e;font-size:18px'>Hola, " + fullName + "</h2>" +
				"<p style='color:#374151'>Su solicitud de <strong>" + typeLabel + "</strong> ha sido actualizada.</p>" +
				"<div style='text-align:center;margin:16px 0'>" +
				"<span style='background:" + statusColor + ";color:#fff;padding:6px 16px;border-radius:20px;font-weight:bold;font-size:14px'>" +
				statusLabel + "</span></div>" +
				detailBlock + pdfNote);

			await SendAsync(toEmail, fullName, emailSubject, html, ct: ct);
		}


		public async Task SendArcoResolutionWithPdfAsync(
			string toEmail, string fullName, string requestType, string resolution,
			byte[] pdfBytes, string pdfFileName, CancellationToken ct = default)
		{
			var typeLabel = MapRequestType(requestType);
			var emailSubject = "Respuesta oficial a su solicitud ARCO — " + typeLabel;
			var html = BuildBaseTemplate("Respuesta Oficial ARCO",
				"<h2 style='color:#1a1a2e;font-size:18px'>Hola, " + fullName + "</h2>" +
				"<p style='color:#374151'>Adjunto encontrará el documento oficial de respuesta a su solicitud de " +
				"<strong>" + typeLabel + "</strong>.</p>" +
				"<p style='color:#374151'>Este documento tiene validez como constancia del ejercicio de sus derechos " +
				"conforme a la <strong>Ley Orgánica de Protección de Datos Personales (LOPDP)</strong> del Ecuador.</p>");

			await SendAsync(toEmail, fullName, emailSubject, html, pdfBytes, pdfFileName, ct);
		}

		private async Task SendAsync(
			string toEmail, string toName, string subject, string htmlBody,
			byte[]? attachment = null, string? attachmentName = null,
			CancellationToken ct = default)
		{
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress(_fromName, _fromAddress));
			message.To.Add(new MailboxAddress(toName, toEmail));
			message.Subject = subject;

			var builder = new BodyBuilder { HtmlBody = htmlBody };

			if (attachment is not null && attachmentName is not null)
				builder.Attachments.Add(attachmentName, attachment, ContentType.Parse("application/pdf"));

			message.Body = builder.ToMessageBody();

			using var client = new SmtpClient();
			await client.ConnectAsync(_host, _port, SecureSocketOptions.StartTls, ct);
			await client.AuthenticateAsync(_username, _password, ct);
			await client.SendAsync(message, ct);
			await client.DisconnectAsync(true, ct);
		}

		private static string BuildBaseTemplate(string title, string body)
		{
			return
				"<div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:24px;" +
				"border:1px solid #e5e7eb;border-radius:8px'>" +
				"<div style='text-align:center;margin-bottom:24px'>" +
				"<h1 style='color:#1a1a2e;font-size:24px;margin:0'>Newbie</h1>" +
				"<p style='color:#6b7280;font-size:13px;margin:4px 0'>SecureHub — Plataforma de Protección de Datos</p>" +
				"</div>" +
				body +
				"<hr style='border:none;border-top:1px solid #e5e7eb;margin:24px 0'/>" +
				"<p style='color:#9ca3af;font-size:12px;text-align:center'>" +
				"Newbie S.A.S. &middot; Quito, Ecuador &middot; Cumplimiento LOPDP</p></div>";
		}

		private static string MapRequestType(string t) => t switch
		{
			"ACCESO" => "Acceso a datos",
			"RECTIFICACION" => "Rectificación de datos",
			"CANCELACION" => "Cancelación de datos",
			"OPOSICION" => "Oposición al tratamiento",
			"PORTABILIDAD" => "Portabilidad de datos",
			_ => t
		};

		private static string MapStatus(string s) => s switch
		{
			"PENDIENTE" => "Pendiente",
			"EN_PROCESO" => "En proceso",
			"COMPLETADO" => "Completado",
			"RECHAZADO" => "Rechazado",
			_ => s
		};
	}
}
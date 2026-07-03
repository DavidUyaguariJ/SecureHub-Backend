using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.ThirdPart.Dtos
{
	public static class AllowedFieldOptions
	{
		public static readonly Dictionary<string, string> Fields = new()
		{
			["full_name"] = "Nombre completo",
			["email"] = "Correo electrónico",
			["phone"] = "Teléfono",
			["address"] = "Dirección",
			["identification"] = "Identificación",
			["subject_type"] = "Tipo de sujeto",
			["contact_person"] = "Persona de contacto",
			["devices"] = "Dispositivos y credenciales"
		};
	}
}

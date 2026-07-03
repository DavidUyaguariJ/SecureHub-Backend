using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record RegisterBiometricCommand(
		string ImageBase64,
		string ConsentText
	);
}

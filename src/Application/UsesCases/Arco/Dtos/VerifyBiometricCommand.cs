using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.UsesCases.Arco.Dtos
{
	public record VerifyBiometricCommand(
		string ImageBase64
	);
}

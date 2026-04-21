using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IBiometricProcessor
	{
		byte[] ProcessImage(string base64Image);
		double CompareBiometrics(byte[] stored, byte[] incoming);
	}
}

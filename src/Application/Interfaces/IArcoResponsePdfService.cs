using SecureHub.Application.UsesCases.Arco.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IArcoResponsePdfService
	{
		byte[] Generate(ArcoRequestDetailDto detail);
	}
}
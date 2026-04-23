using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Biometric
{
	public static class ModelDownloader
	{
		private static readonly Dictionary<string, string> Models = new()
		{
			{
				"arcfaceresnet100-8.onnx",
				"https://media.githubusercontent.com/media/onnx/models/main/validated/vision/body_analysis/arcface/model/arcfaceresnet100-8.onnx"
			},
			{
				"version-RFB-320.onnx",
				"https://media.githubusercontent.com/media/onnx/models/main/validated/vision/body_analysis/ultraface/models/version-RFB-320.onnx"
			}
		};

		public static async Task EnsureModelsAsync(string modelsDirectory)
		{
			Directory.CreateDirectory(modelsDirectory);

			using var http = new HttpClient();
			http.Timeout = TimeSpan.FromMinutes(10);

			foreach (var (fileName, url) in Models)
			{
				var filePath = Path.Combine(modelsDirectory, fileName);

				if (File.Exists(filePath) && new FileInfo(filePath).Length > 100_000)
				{
					Console.WriteLine($"[Models] {fileName} ya existe, omitiendo descarga.");
					continue;
				}

				Console.WriteLine($"[Models] Descargando {fileName}...");
				try
				{
					var bytes = await http.GetByteArrayAsync(url);
					await File.WriteAllBytesAsync(filePath, bytes);
					Console.WriteLine($"[Models] {fileName} descargado ({bytes.Length / 1_048_576.0:F1} MB)");
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException(
						$"No se pudo descargar el modelo {fileName} desde {url}. Error: {ex.Message}");
				}
			}
		}
	}
}

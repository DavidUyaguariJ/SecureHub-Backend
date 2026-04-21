using MathNet.Numerics.LinearAlgebra;
using SecureHub.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Biometric
{
	public class PcaBiometricProcessor : IBiometricProcessor
	{
		private const int PCA_COMPONENTS = 50;

		public byte[] ProcessImage(string base64Image)
		{
			var imageBytes = Convert.FromBase64String(base64Image);
			var pixelVector = ConvertImageToVector(imageBytes);
			var reducedVector = ApplyPCA(pixelVector, PCA_COMPONENTS);
			return SerializeVector(reducedVector);
		}

		public double CompareBiometrics(byte[] stored, byte[] incoming)
		{
			var v1 = DeserializeVector(stored);
			var v2 = DeserializeVector(incoming);
			var dot = v1.Zip(v2, (a, b) => a * b).Sum();
			var mag1 = Math.Sqrt(v1.Sum(x => x * x));
			var mag2 = Math.Sqrt(v2.Sum(x => x * x));

			if (mag1 == 0 || mag2 == 0) return 0;
			return dot / (mag1 * mag2);
		}

		public double[] ConvertImageToVector(byte[] imageBytes)
		{
			// Saltar el header JPEG/PNG (primeros bytes de metadata)
			// y tomar píxeles del cuerpo de la imagen
			int start = Math.Min(100, imageBytes.Length / 10); // saltar header aproximado
			int length = Math.Min(imageBytes.Length - start, 8000); // más datos = mejor discriminación

			if (length <= 0)
				return imageBytes.Select(b => b / 255.0).ToArray();

			var vector = new double[length];
			for (int i = 0; i < length; i++)
				vector[i] = imageBytes[start + i] / 255.0;

			return vector;
		}

		private double[] ApplyPCA(double[] data, int components)
		{
			int n = data.Length;
			if (n == 0) return Array.Empty<double>();

			// Dividir el vector en bloques de 'blockSize' píxeles
			// Cada bloque es una "muestra", cada posición dentro del bloque es una "feature"
			int blockSize = 40;
			int numBlocks = n / blockSize;

			if (numBlocks < 2)
			{
				// No hay suficientes datos — devolver normalizado directamente
				int take = Math.Min(components, n);
				var fallback = new double[take];
				Array.Copy(data, fallback, take);
				return fallback;
			}

			// Construir matriz: filas = bloques (muestras), columnas = píxeles por bloque (features)
			var rows = new double[numBlocks][];
			for (int i = 0; i < numBlocks; i++)
			{
				rows[i] = new double[blockSize];
				Array.Copy(data, i * blockSize, rows[i], 0, blockSize);
			}

			var matrix = Matrix<double>.Build.DenseOfRowArrays(rows); // numBlocks x blockSize

			// Centrar cada columna (feature)
			for (int col = 0; col < blockSize; col++)
			{
				double colMean = 0;
				for (int row = 0; row < numBlocks; row++)
					colMean += matrix[row, col];
				colMean /= numBlocks;
				for (int row = 0; row < numBlocks; row++)
					matrix[row, col] -= colMean;
			}

			// SVD sobre la matriz centrada
			var svd = matrix.Svd(true);
			var vt = svd.VT; // VT: componentes principales (filas = componentes)
			// Proyectar el vector original sobre los primeros 'components' componentes
			int dim = Math.Min(components, vt.RowCount);
			var result = new double[dim];

			for (int i = 0; i < dim; i++)
			{
				double projection = 0;
				int featureCount = Math.Min(blockSize, data.Length);
				for (int j = 0; j < featureCount; j++)
					projection += data[j] * vt[i, j];
				result[i] = projection;
			}

			return result;
		}

		private byte[] SerializeVector(double[] vector)
		{
			var bytes = new byte[vector.Length * sizeof(double)];
			Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
			return bytes;
		}

		private double[] DeserializeVector(byte[] bytes)
		{
			var vector = new double[bytes.Length / sizeof(double)];
			Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
			return vector;
		}
	}
}

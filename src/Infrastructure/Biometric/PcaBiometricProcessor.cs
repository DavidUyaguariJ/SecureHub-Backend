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

		private double[] ConvertImageToVector(byte[] imageBytes)
		{
			return imageBytes.Select(b => (double)b / 255.0).ToArray();
		}

		private double[] ApplyPCA(double[] data, int components)
		{
			int n = data.Length;
			if (n <= components) return data;
			var matrix = Matrix<double>.Build.DenseOfRowArrays(new[] { data });
			var mean = data.Average();
			var centered = data.Select(x => x - mean).ToArray();
			var covMatrix = Matrix<double>.Build.Dense(n, n);
			for (int i = 0; i < n; i++)
				for (int j = 0; j < n; j++)
					covMatrix[i, j] = centered[i] * centered[j];
			var svd = covMatrix.Svd(true);
			var principalComponents = svd.U.SubMatrix(0, n, 0, components);
			var centeredVector = Vector<double>.Build.DenseOfArray(centered);
			var projected = principalComponents.TransposeThisAndMultiply(centeredVector);
			return projected.ToArray();
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

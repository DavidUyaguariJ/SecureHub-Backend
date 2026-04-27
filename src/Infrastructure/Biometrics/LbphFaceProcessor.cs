using SecureHub.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Biometrics
{
	public class LbphFaceProcessor : IBiometricProcessor
	{
		private const int GridRows = 4;
		private const int GridCols = 4;
		private const int HistBins = 256;
		private const int FaceSize = 128;
		private const int TotalDims = GridRows * GridCols * HistBins;

		public Task<FaceEmbeddingResult> ExtractEmbeddingAsync(string imageBase64)
		{
			return Task.Run(() =>
			{
				try
				{
					var imageBytes = Convert.FromBase64String(imageBase64);
					using var image = Image.Load<L8>(imageBytes);
					image.Mutate(ctx => ctx.Resize(FaceSize, FaceSize));
					var lbpMap = ComputeLbpMap(image);
					var histogram = ComputeGridHistogram(lbpMap, FaceSize, FaceSize);
					Normalize(histogram);

					return new FaceEmbeddingResult
					{
						Embedding = histogram,
						ModelUsed = "LBPH_256",
						Dimensions = histogram.Length,
						DetectionScore = 0.9f
					};
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException(
						$"Error procesando imagen biométrica: {ex.Message}", ex);
				}
			});
		}

		public float CompareFaces(byte[] storedEmbedding, byte[] candidateEmbedding)
		{
			var vecA = DeserializeEmbedding(storedEmbedding);
			var vecB = DeserializeEmbedding(candidateEmbedding);
			return CosineSimilarity(vecA, vecB);
		}

		public byte[] SerializeEmbedding(float[] embedding)
		{
			var bytes = new byte[embedding.Length * sizeof(float)];
			Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
			return bytes;
		}

		public float[] DeserializeEmbedding(byte[] data)
		{
			var floats = new float[data.Length / sizeof(float)];
			Buffer.BlockCopy(data, 0, floats, 0, data.Length);
			return floats;
		}

		private static byte[] ComputeLbpMap(Image<L8> image)
		{
			int w = image.Width;
			int h = image.Height;
			var map = new byte[w * h];
			for (int y = 1; y < h - 1; y++)
			{
				for (int x = 1; x < w - 1; x++)
				{
					byte center = image[x, y].PackedValue;
					byte code = 0;
					if (image[x - 1, y - 1].PackedValue >= center) code |= 1;
					if (image[x, y - 1].PackedValue >= center) code |= 2;
					if (image[x + 1, y - 1].PackedValue >= center) code |= 4;
					if (image[x + 1, y].PackedValue >= center) code |= 8;
					if (image[x + 1, y + 1].PackedValue >= center) code |= 16;
					if (image[x, y + 1].PackedValue >= center) code |= 32;
					if (image[x - 1, y + 1].PackedValue >= center) code |= 64;
					if (image[x - 1, y].PackedValue >= center) code |= 128;

					map[y * w + x] = code;
				}
			}

			return map;
		}

		private static float[] ComputeGridHistogram(byte[] lbpMap, int w, int h)
		{
			var result = new float[TotalDims];
			int cellW = w / GridCols;
			int cellH = h / GridRows;
			int regionIdx = 0;

			for (int row = 0; row < GridRows; row++)
			{
				for (int col = 0; col < GridCols; col++)
				{
					int startX = col * cellW;
					int startY = row * cellH;
					int endX = startX + cellW;
					int endY = startY + cellH;
					var hist = new float[HistBins];

					for (int y = startY; y < endY; y++)
					{
						for (int x = startX; x < endX; x++)
						{
							hist[lbpMap[y * w + x]]++;
						}
					}
					float total = cellW * cellH;
					for (int b = 0; b < HistBins; b++)
					{
						result[regionIdx * HistBins + b] = hist[b] / total;
					}

					regionIdx++;
				}
			}

			return result;
		}

		private static void Normalize(float[] vec)
		{
			float norm = 0f;
			foreach (var v in vec) norm += v * v;
			norm = MathF.Sqrt(norm);
			if (norm < 1e-10f) return;
			for (int i = 0; i < vec.Length; i++) vec[i] /= norm;
		}

		private static float CosineSimilarity(float[] a, float[] b)
		{
			float dot = 0f;
			for (int i = 0; i < a.Length; i++) dot += a[i] * b[i];
			return dot;
		}
	}
}
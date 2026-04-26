using FaceAiSharp;
using FaceAiSharp.Extensions;
using SecureHub.Application.Interfaces;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;


namespace SecureHub.Infrastructure.Biometrics
{
	public class ArcFaceBiometricProcessor : IBiometricProcessor
	{
		private readonly IFaceDetectorWithLandmarks _detector;
		private readonly IFaceEmbeddingsGenerator _embedder;

		public ArcFaceBiometricProcessor()
		{
			_detector = FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks();
			_embedder = FaceAiSharpBundleFactory.CreateFaceEmbeddingsGenerator();
		}

		public Task<FaceEmbeddingResult> ExtractEmbeddingAsync(string imageBase64)
		{
			return Task.Run(() =>
			{
				var bytes = Convert.FromBase64String(imageBase64);
				using var image = Image.Load<Rgb24>(bytes);
				var faces = _detector.DetectFaces(image);
				if (!faces.Any())
					throw new InvalidOperationException("No se detectó ningún rostro en la imagen.");
				var face = faces.OrderByDescending(f => f.Box.Width * f.Box.Height).First();
				_embedder.AlignFaceUsingLandmarks(image, face.Landmarks!);
				var embedding = _embedder.GenerateEmbedding(image);

				return new FaceEmbeddingResult
				{
					Embedding = embedding,
					ModelUsed = "ArcFace_512",
					Dimensions = embedding.Length,
					DetectionScore = (float)face.Confidence
				};
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

		private static float CosineSimilarity(float[] a, float[] b)
		{
			float dot = 0f, normA = 0f, normB = 0f;
			for (int i = 0; i < a.Length; i++)
			{
				dot += a[i] * b[i];
				normA += a[i] * a[i];
				normB += b[i] * b[i];
			}
			return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB) + 1e-10f);
		}
	}
}

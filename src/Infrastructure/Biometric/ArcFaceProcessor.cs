using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SecureHub.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System;

namespace SecureHub.Infrastructure.Biometric
{
	public class ArcFaceProcessor : IBiometricProcessor, IDisposable
	{
		private readonly InferenceSession _arcFaceSession;
		private readonly FaceDetector _detector;
		private const int ArcFaceSize = 112;

		public ArcFaceProcessor(string arcFaceModelPath, string detectorModelPath)
		{
			var options = new SessionOptions();
			options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

			_arcFaceSession = new InferenceSession(arcFaceModelPath, options);
			_detector = new FaceDetector(detectorModelPath);
		}
		public Task<FaceEmbeddingResult> ExtractEmbeddingAsync(string imageBase64)
		{
			return Task.Run(() => ExtractEmbedding(imageBase64));
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
		private FaceEmbeddingResult ExtractEmbedding(string imageBase64)
		{
			var imageBytes = Convert.FromBase64String(imageBase64);

			using var image = Image.Load<Rgb24>(imageBytes);

			var face = _detector.Detect(image);
			if (face == null)
			{
				return new FaceEmbeddingResult
				{
					Embedding = [],
					ModelUsed = "arcfaceresnet100",
					Dimensions = 0,
					DetectionScore = 0f
				};
			}

			var cropped = CropAndResize(image, face);
			var tensor = ImageToArcFaceTensor(cropped);
			var inputs = new List<NamedOnnxValue>
			{
				NamedOnnxValue.CreateFromTensor("data", tensor)
			};

			using var results = _arcFaceSession.Run(inputs);
			var rawEmbedding = results[0].AsEnumerable<float>();
			var embedding = System.Linq.Enumerable.ToArray(rawEmbedding);
			Normalize(embedding);

			return new FaceEmbeddingResult
			{
				Embedding = embedding,
				ModelUsed = "arcfaceresnet100",
				Dimensions = embedding.Length,
				DetectionScore = face.Score
			};
		}

		private static Image<Rgb24> CropAndResize(Image<Rgb24> source, DetectedFace face)
		{
			float w = face.X2 - face.X1;
			float h = face.Y2 - face.Y1;
			float marginX = w * 0.2f;
			float marginY = h * 0.2f;
			int x1 = Math.Max(0, (int)(face.X1 - marginX));
			int y1 = Math.Max(0, (int)(face.Y1 - marginY));
			int x2 = Math.Min(source.Width, (int)(face.X2 + marginX));
			int y2 = Math.Min(source.Height, (int)(face.Y2 + marginY));
			var cropRect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
			var cropped = source.Clone();
			cropped.Mutate(ctx =>
			{
				ctx.Crop(cropRect);
				ctx.Resize(ArcFaceSize, ArcFaceSize);
			});

			return cropped;
		}

		private static DenseTensor<float> ImageToArcFaceTensor(Image<Rgb24> img)
		{
			var tensor = new DenseTensor<float>(new[] { 1, 3, ArcFaceSize, ArcFaceSize });

			for (int y = 0; y < ArcFaceSize; y++)
			{
				for (int x = 0; x < ArcFaceSize; x++)
				{
					var pixel = img[x, y];
					tensor[0, 0, y, x] = (pixel.R - 127.5f) / 128f;
					tensor[0, 1, y, x] = (pixel.G - 127.5f) / 128f;
					tensor[0, 2, y, x] = (pixel.B - 127.5f) / 128f;
				}
			}

			return tensor;
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

		public void Dispose()
		{
			_arcFaceSession.Dispose();
			_detector.Dispose();
		}
	}
}
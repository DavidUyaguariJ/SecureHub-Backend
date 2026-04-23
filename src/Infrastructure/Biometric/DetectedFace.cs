using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Infrastructure.Biometric
{
	public class DetectedFace
	{
		public float X1 { get; set; }
		public float Y1 { get; set; }
		public float X2 { get; set; }
		public float Y2 { get; set; }
		public float Score { get; set; }
	}

	public class FaceDetector : IDisposable
	{
		private readonly InferenceSession _session;
		private const int InputWidth = 320;
		private const int InputHeight = 240;
		private const float ScoreThreshold = 0.7f;
		private const float IouThreshold = 0.3f;

		public FaceDetector(string modelPath)
		{
			var options = new SessionOptions();
			options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
			_session = new InferenceSession(modelPath, options);
		}

		public DetectedFace? Detect(Image<Rgb24> image)
		{
			int origW = image.Width;
			int origH = image.Height;
			using var resized = image.Clone();
			resized.Mutate(ctx => ctx.Resize(InputWidth, InputHeight));
			var tensor = ImageToTensor(resized);
			var inputs = new List<NamedOnnxValue>
			{
                NamedOnnxValue.CreateFromTensor("input", tensor)
			};
			using var results = _session.Run(inputs);
			var scoresTensor = results[0].AsTensor<float>();
			var boxesTensor = results[1].AsTensor<float>();
			return ParseBestFace(scoresTensor, boxesTensor, origW, origH);
		}

		private static DenseTensor<float> ImageToTensor(Image<Rgb24> img)
		{
			var tensor = new DenseTensor<float>(new[] { 1, 3, InputHeight, InputWidth });
			for (int y = 0; y < InputHeight; y++)
			{
				for (int x = 0; x < InputWidth; x++)
				{
					var pixel = img[x, y];
					tensor[0, 0, y, x] = (pixel.R - 127f) / 128f;
					tensor[0, 1, y, x] = (pixel.G - 127f) / 128f;
					tensor[0, 2, y, x] = (pixel.B - 127f) / 128f;
				}
			}

			return tensor;
		}

		private static DetectedFace? ParseBestFace(
			Tensor<float> scores,
			Tensor<float> boxes,
			int origW,
			int origH)
		{
			int count = scores.Dimensions[1];
			var candidates = new List<DetectedFace>();
			for (int i = 0; i < count; i++)
			{
				float faceScore = scores[0, i, 1];
				if (faceScore < ScoreThreshold) continue;

				candidates.Add(new DetectedFace
				{
					X1 = boxes[0, i, 0] * origW,
					Y1 = boxes[0, i, 1] * origH,
					X2 = boxes[0, i, 2] * origW,
					Y2 = boxes[0, i, 3] * origH,
					Score = faceScore
				});
			}

			if (candidates.Count == 0) return null;
			candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
			var accepted = new List<DetectedFace>();
			foreach (var candidate in candidates)
			{
				bool overlaps = accepted.Any(a => IoU(a, candidate) > IouThreshold);
				if (!overlaps) accepted.Add(candidate);
			}
			return accepted.FirstOrDefault();
		}

		private static float IoU(DetectedFace a, DetectedFace b)
		{
			float interX1 = MathF.Max(a.X1, b.X1);
			float interY1 = MathF.Max(a.Y1, b.Y1);
			float interX2 = MathF.Min(a.X2, b.X2);
			float interY2 = MathF.Min(a.Y2, b.Y2);
			float interW = MathF.Max(0, interX2 - interX1);
			float interH = MathF.Max(0, interY2 - interY1);
			float interArea = interW * interH;
			float areaA = (a.X2 - a.X1) * (a.Y2 - a.Y1);
			float areaB = (b.X2 - b.X1) * (b.Y2 - b.Y1);
			float unionArea = areaA + areaB - interArea;
			return unionArea <= 0 ? 0f : interArea / unionArea;
		}

		public void Dispose() => _session.Dispose();
	}
}
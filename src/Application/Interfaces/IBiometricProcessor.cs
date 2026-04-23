using System;
using System.Collections.Generic;
using System.Text;

namespace SecureHub.Application.Interfaces
{
	public interface IBiometricProcessor
	{
		Task<FaceEmbeddingResult> ExtractEmbeddingAsync(string imageBase64);
		float CompareFaces(byte[] storedEmbedding, byte[] candidateEmbedding);
		byte[] SerializeEmbedding(float[] embedding);
		float[] DeserializeEmbedding(byte[] data);
	}

	public class FaceEmbeddingResult
	{
		public float[] Embedding { get; set; } = [];
		public string ModelUsed { get; set; } = string.Empty;
		public int Dimensions { get; set; }
		public float DetectionScore { get; set; }
		public bool FaceDetected => DetectionScore > 0.5f;
	}
}
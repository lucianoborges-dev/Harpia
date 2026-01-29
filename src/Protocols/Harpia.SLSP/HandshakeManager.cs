namespace Harpia.SLSP;

using System;
using System.Security.Cryptography;

public sealed class HandshakeManager : IDisposable
{
	private readonly ECDiffieHellman _ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
	private static ReadOnlySpan<byte> HkdfSalt => "SLSP_v1_Salt"u8;

	public byte[] MyPublicKey => _ecdh.PublicKey.ExportSubjectPublicKeyInfo();

	public byte[] DeriveFinalKey(ReadOnlySpan<byte> otherPublicKeyInfo)
	{
		using var otherKey = ECDiffieHellman.Create();
		otherKey.ImportSubjectPublicKeyInfo(otherPublicKeyInfo, out _);

		// Derive the raw shared secret (already a byte[])
		byte[] sharedSecret = _ecdh.DeriveRawSecretAgreement(otherKey.PublicKey);

		// Derive the final key with HKDF-SHA256
		byte[] finalKey = new byte[32]; // 256 bits
		HKDF.DeriveKey(
			HashAlgorithmName.SHA256,
			sharedSecret,       // input key material
			finalKey,           // output buffer
			HkdfSalt,           // salt
			[] // optional information
		);

		return finalKey;
	}

	public void Dispose() => _ecdh.Dispose();
}

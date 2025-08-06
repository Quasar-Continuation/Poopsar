using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Pulsar.Server.Helper
{
    public static class CertificateHelper
    {
        public static X509Certificate2 CreateCertificateAuthority(string caName, int keyStrength = 4096)
        {
            var random = new SecureRandom(new CryptoApiRandomGenerator());
            var keyPairGen = new RsaKeyPairGenerator();
            keyPairGen.Init(new KeyGenerationParameters(random, keyStrength));
            AsymmetricCipherKeyPair keypair = keyPairGen.GenerateKeyPair();

            var certificateGenerator = new X509V3CertificateGenerator();

            var CN = new X509Name("CN=" + caName);
            var SN = BigInteger.ProbablePrime(160, random);

            certificateGenerator.SetSerialNumber(SN);
            certificateGenerator.SetSubjectDN(CN);
            certificateGenerator.SetIssuerDN(CN);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.AddYears(10));
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0)));
            certificateGenerator.SetPublicKey(keypair.Public);
            certificateGenerator.AddExtension(X509Extensions.SubjectKeyIdentifier, false, new SubjectKeyIdentifierStructure(keypair.Public));
            certificateGenerator.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(true));
            certificateGenerator.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(KeyUsage.KeyCertSign | KeyUsage.CrlSign));
            certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, true, new ExtendedKeyUsage(KeyPurposeID.IdKPServerAuth));

            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", keypair.Private, random);

            var certificate = certificateGenerator.Generate(signatureFactory);

            // Create PKCS#12 (PFX) format with certificate and private key
            var store = new Pkcs12StoreBuilder().Build();
            var certificateEntry = new X509CertificateEntry(certificate);
            store.SetCertificateEntry(caName, certificateEntry);
            store.SetKeyEntry(caName, new AsymmetricKeyEntry(keypair.Private), new[] { certificateEntry });

            // Convert to bytes
            using (var ms = new MemoryStream())
            {
                store.Save(ms, new char[0], random); // Empty password
                var pfxBytes = ms.ToArray();
                
                // Create X509Certificate2 from PFX bytes using modern approach for .NET 9.0 AOT compatibility
                return X509CertificateLoader.LoadPkcs12(pfxBytes, null, X509KeyStorageFlags.Exportable);
            }
        }

        /// <summary>
        /// Alternative method using PEM format for AOT compatibility
        /// </summary>
        public static X509Certificate2 CreateCertificateAuthorityFromPem(string caName, int keyStrength = 4096)
        {
            var random = new SecureRandom(new CryptoApiRandomGenerator());
            var keyPairGen = new RsaKeyPairGenerator();
            keyPairGen.Init(new KeyGenerationParameters(random, keyStrength));
            AsymmetricCipherKeyPair keypair = keyPairGen.GenerateKeyPair();

            var certificateGenerator = new X509V3CertificateGenerator();

            var CN = new X509Name("CN=" + caName);
            var SN = BigInteger.ProbablePrime(160, random);

            certificateGenerator.SetSerialNumber(SN);
            certificateGenerator.SetSubjectDN(CN);
            certificateGenerator.SetIssuerDN(CN);
            certificateGenerator.SetNotAfter(DateTime.UtcNow.AddYears(10));
            certificateGenerator.SetNotBefore(DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0)));
            certificateGenerator.SetPublicKey(keypair.Public);
            certificateGenerator.AddExtension(X509Extensions.SubjectKeyIdentifier, false, new SubjectKeyIdentifierStructure(keypair.Public));
            certificateGenerator.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(true));
            certificateGenerator.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(KeyUsage.KeyCertSign | KeyUsage.CrlSign));
            certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, true, new ExtendedKeyUsage(KeyPurposeID.IdKPServerAuth));

            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", keypair.Private, random);

            var certificate = certificateGenerator.Generate(signatureFactory);

            // Convert to PEM format
            string certPem, keyPem;
            
            using (var certWriter = new StringWriter())
            {
                var pemWriter = new PemWriter(certWriter);
                pemWriter.WriteObject(certificate);
                certPem = certWriter.ToString();
            }

            using (var keyWriter = new StringWriter())
            {
                var pemWriter = new PemWriter(keyWriter);
                pemWriter.WriteObject(keypair.Private);
                keyPem = keyWriter.ToString();
            }

            // Create X509Certificate2 from PEM - this is AOT-compatible in .NET 9.0
            return X509Certificate2.CreateFromPem(certPem, keyPem);
        }
    }
}

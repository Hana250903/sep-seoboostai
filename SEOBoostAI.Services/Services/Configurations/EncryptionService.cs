using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Configuration;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Configurations
{
    public class EncryptionService : IEncryptionService
    {
        private readonly CryptographyClient _cryptoClient;

        public EncryptionService(IConfiguration configuration)
        {
            // Đọc config từ appsettings.json
            var keyVaultUrl = configuration["AzureKeyVault:VaultUrl"];
            var keyName = configuration["AzureKeyVault:KeyName"];

            if (string.IsNullOrEmpty(keyVaultUrl))
                throw new InvalidOperationException("AzureKeyVault:VaultUrl is not configured in appsettings.json");

            if (string.IsNullOrEmpty(keyName))
                throw new InvalidOperationException("AzureKeyVault:KeyName is not configured in appsettings.json");

            // Tạo Key identifier từ vault URL và key name
            var keyIdentifier = new Uri($"{keyVaultUrl.TrimEnd('/')}/keys/{keyName}");

            // Sử dụng DefaultAzureCredential để authenticate (support Managed Identity, Azure CLI, etc.)
            _cryptoClient = new CryptographyClient(keyIdentifier, new DefaultAzureCredential());
        }

        public async Task<string> EncryptAsync(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));

            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            // Encrypt sử dụng RSA-OAEP-256 algorithm
            var result = await _cryptoClient.EncryptAsync(
                EncryptionAlgorithm.RsaOaep256,
                plainBytes);

            // Return ciphertext as Base64 string
            return Convert.ToBase64String(result.Ciphertext);
        }

        public async Task<string> DecryptAsync(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));

            var cipherBytes = Convert.FromBase64String(cipherText);

            // Decrypt sử dụng RSA-OAEP-256 algorithm
            var result = await _cryptoClient.DecryptAsync(
                EncryptionAlgorithm.RsaOaep256,
                cipherBytes);

            // Return plaintext as string
            return Encoding.UTF8.GetString(result.Plaintext);
        }
    }
}

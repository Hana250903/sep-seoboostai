using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypt plain text using Azure Key Vault cryptographic key
        /// </summary>
        /// <param name="plainText">The plain text to encrypt</param>
        /// <returns>Base64 encoded ciphertext</returns>
        Task<string> EncryptAsync(string plainText);

        /// <summary>
        /// Decrypt ciphertext using Azure Key Vault cryptographic key
        /// </summary>
        /// <param name="cipherText">Base64 encoded ciphertext</param>
        /// <returns>Decrypted plain text</returns>
        Task<string> DecryptAsync(string cipherText);
    }
}

using Microsoft.AspNetCore.DataProtection;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Api.Services;

public class SecretProtector : ISecretProtector
{
    private readonly IDataProtector _protector;

    public SecretProtector(IDataProtectionProvider provider)
    {
        // "Purpose" isola essa criptografia de qualquer outro uso do Data
        // Protection no app — nunca muda esse texto, ou chaves antigas
        // salvas no banco ficam impossíveis de descriptografar.
        _protector = provider.CreateProtector("WhatsappCrmIA.AnthropicApiKey.v1");
    }

    public string Encrypt(string plainText) => _protector.Protect(plainText);

    public string Decrypt(string cipherText) => _protector.Unprotect(cipherText);
}

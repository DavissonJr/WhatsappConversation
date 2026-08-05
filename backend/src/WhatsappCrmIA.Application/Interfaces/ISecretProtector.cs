namespace WhatsappCrmIA.Application.Interfaces;

/// <summary>
/// Criptografa segredos (como a chave da Anthropic de cada tenant) antes de
/// salvar no banco. Implementado com Data Protection do ASP.NET Core.
/// </summary>
public interface ISecretProtector
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

namespace TinyHeroes.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string subPath, string fileName, CancellationToken ct = default);
    void Delete(string subPath, string fileName);
}

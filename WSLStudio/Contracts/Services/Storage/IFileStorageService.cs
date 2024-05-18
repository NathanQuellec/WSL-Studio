using WSLStudio.Models;

namespace WSLStudio.Services.Storage;

public interface IFileStorageService
{
    Task Save(string filePath, IBaseModel elem);
    Task Load(string filePath);
    Task Delete(string filePath, IBaseModel elem);
}
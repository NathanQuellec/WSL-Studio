using System.Collections.ObjectModel;
using WSLStudio.Contracts.Models;
using WSLStudio.Contracts.Services.Storage;

namespace WSLStudio.Services.Storage;

public class JsonFileStorageService : IFileStorageService
{
    public Task Save(string filePath, IBaseModel elem) => null;

    public ObservableCollection<T> Load<T>(string filePath) where T : IBaseModel, new() => null;

    public Task Delete(string filePath, IBaseModel elem) => null;
}
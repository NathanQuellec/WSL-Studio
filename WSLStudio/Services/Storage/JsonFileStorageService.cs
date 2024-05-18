using WSLStudio.Models;

namespace WSLStudio.Services.Storage.impl;

public class JsonFileStorageService : IFileStorageService
{
    public Task Save(string filePath, IBaseModel elem) => null;

    public Task Load(string filePath) => null;

    public Task Delete(string filePath, IBaseModel elem) => null;
}
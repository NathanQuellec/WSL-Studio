using WSLStudio.Models;

namespace WSLStudio.Services.Storage.impl;

public class FlatFileStorageService : IFileStorageService
{
    public Task Save(string filePath, IBaseModel elem) => null;

    public Task Load(string filePath) => null;

    public async Task Delete(string filePath, IBaseModel elem)
    {
        var recordsToKeep = (await File.ReadAllLinesAsync(filePath))
            .Where(line => line.Split(';')[0] != elem.Id.ToString());
        await File.WriteAllLinesAsync(filePath, recordsToKeep);
    }
}
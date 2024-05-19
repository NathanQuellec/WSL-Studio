using System.Collections.ObjectModel;
using WSLStudio.Contracts.Models;

namespace WSLStudio.Contracts.Services.Storage;

public interface IFileStorageService
{
    Task Save(string filePath, IBaseModel elem);
    ObservableCollection<T> Load<T>(string filePath) where T : IBaseModel, new();
    Task Delete(string filePath, IBaseModel elem);
}
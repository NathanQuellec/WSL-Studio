using System.Collections.ObjectModel;
using WSLStudio.Contracts.Models;

namespace WSLStudio.Contracts.Services.Storage;

public interface IFileStorageService
{
    Task Save<T>(string filePath, T elem) where T : IBaseModel;
    ObservableCollection<T> Load<T>(string filePath) where T : IBaseModel, new();
    Task Delete<T>(string filePath, T elem) where T : IBaseModel;
}
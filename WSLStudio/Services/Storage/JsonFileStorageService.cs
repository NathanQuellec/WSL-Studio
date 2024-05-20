using System.Collections.ObjectModel;
using System.Text.Json;
using WSLStudio.Contracts.Models;
using WSLStudio.Contracts.Services.Storage;

namespace WSLStudio.Services.Storage;

public class JsonFileStorageService : IFileStorageService
{
    public Task Save<T>(string filePath, T elem) where T : IBaseModel
    {
        var  elemsList = new List<T>();
        var jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, };

        try
        {
            if (!File.Exists(filePath))
            {
                elemsList.Add(elem);
            }
            else
            {
                var jsonContent = File.ReadAllText(filePath);
                elemsList = JsonSerializer.Deserialize<List<T>>(jsonContent);
                if (elemsList == null)
                {
                    throw new Exception("Could not deserialize snapshot data from json file");
                }
                elemsList.Add(elem);
            }
            var elemJson = JsonSerializer.Serialize(elemsList, jsonSerializerOptions);
            File.WriteAllText(filePath, elemJson);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    public ObservableCollection<T> Load<T>(string filePath) where T : IBaseModel, new()
    {
        var jsonContent = File.ReadAllText(filePath);
        var elemsList = JsonSerializer.Deserialize<ObservableCollection<T>>(jsonContent) 
                        ?? new ObservableCollection<T>();
        return elemsList;
    }

    public Task Delete<T>(string filePath, T elem) where T : IBaseModel
    {
        var jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, };
        try
        {
            var jsonContent = File.ReadAllText(filePath);
            var elemsList = JsonSerializer.Deserialize<List<T>>(jsonContent);
            if (elemsList == null)
            {
                throw new Exception("Could not deserialize snapshot data from json file");
            }

            var newElemsList = elemsList.FindAll(element => !element.Id.Equals(elem.Id)).ToList();
            var newJsonContent = JsonSerializer.Serialize(newElemsList, jsonSerializerOptions);
            File.WriteAllText(filePath, newJsonContent);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }
}
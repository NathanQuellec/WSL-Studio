using System.Collections.ObjectModel;
using System.Text;
using WSLStudio.Contracts.Models;
using WSLStudio.Contracts.Services.Storage;

namespace WSLStudio.Services.Storage;

[Obsolete]
public class FlatFileStorageService : IFileStorageService
{
    public async Task Save(string filePath, IBaseModel elem)
    {
        var flatFileHeader = new StringBuilder();
        var flatFileData = new StringBuilder();
        // snapshot record's attributes are saved by 
        var elemProperties = elem.GetType().GetProperties();

        // construct file header if not exist
        if (!File.Exists(filePath))
        {
            flatFileHeader.Append(string.Join(';', elemProperties.Select(prop => prop.Name)));
            flatFileHeader.Append('\n');
            await File.AppendAllTextAsync(filePath, flatFileHeader.ToString());
        }

        // add element data to flat file
        flatFileData.Append(string.Join(';', elemProperties.Select(prop => prop.GetValue(elem).ToString())));
        flatFileData.Append('\n');
        await File.AppendAllTextAsync(filePath, flatFileData.ToString());
    }

    public ObservableCollection<T> Load<T>(string filePath) where T : IBaseModel, new()
    {
        var elemAllLines = File.ReadAllLines(filePath);
        var elems = new ObservableCollection<T>();

        for (var lineIndex = 1; lineIndex < elemAllLines.Length; lineIndex++)
        {
            var elemData = elemAllLines[lineIndex].Split(';');
            var elem = new T();
            var properties = elem.GetType().GetProperties();
            properties[0].SetValue(elem, Guid.Parse(elemData[0]));

            for (var propIndex = 1; propIndex < elem.GetType().GetProperties().Length; propIndex++)
            {
                properties[propIndex].SetValue(elem, 
                    Convert.ChangeType(elemData[propIndex], properties[propIndex].PropertyType));
            }

            elems.Add(elem);
        }

        return elems;
    }

    public async Task Delete(string filePath, IBaseModel elem)
    {
        var recordsToKeep = (await File.ReadAllLinesAsync(filePath))
            .Where(line => line.Split(';')[0] != elem.Id.ToString());
        await File.WriteAllLinesAsync(filePath, recordsToKeep);
    }
}
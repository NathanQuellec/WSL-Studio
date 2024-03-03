using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IDistributionInfosService
{
    string GetOsInfos(string distroName, string distroPath, string field);
    string GetSize(string distroPath);
    List<string> GetDistributionUsers(string distroName, string distroPath);
}
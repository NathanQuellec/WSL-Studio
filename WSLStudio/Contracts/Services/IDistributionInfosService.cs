using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IDistributionInfosService
{
    string GetOsInfos(Distribution distribution, string field);
    string GetSize(string distroPath);
    List<string> GetDistributionUsers(Distribution distribution);
}
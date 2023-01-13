namespace WSLStudio.Contracts.Services;

public interface IWslService
{
    bool CheckWsl();
    bool CheckHypervisor();
}
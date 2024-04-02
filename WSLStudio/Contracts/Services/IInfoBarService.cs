using Microsoft.UI.Xaml.Controls;

namespace WSLStudio.Contracts.Services;

public interface IInfoBarService
{
    InfoBar FindInfoBar(string infoBarName);
    void OpenInfoBar(InfoBar infoBar, double time);
    void OpenInfoBar(InfoBar infoBar);
    void OpenInfoBar(InfoBar infoBar, string message, double time);
    void CloseInfoBar(InfoBar infoBar);
}
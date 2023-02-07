using System.Timers;
using Microsoft.UI.Xaml.Controls;

namespace WSLStudio.Contracts.Services;

public interface IInfoBarService
{
    InfoBar FindInfoBar(string infoBarName);
    void OpenInfoBar(InfoBar infoBar, double time);
    void OpenInfoBar(InfoBar infoBar);
    void CloseInfoBar(InfoBar infoBar);
}
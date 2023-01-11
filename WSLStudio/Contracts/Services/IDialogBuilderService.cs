using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WSLStudio.Contracts.Services;

public interface IDialogBuilderService
{
    IDialogBuilderService SetTitle(object title);
    IDialogBuilderService SetContent(object content);
    IDialogBuilderService SetPrimaryButtonText(string primaryButtonText);
    IDialogBuilderService SetSecondaryButtonText(string secondaryButtonText);
    IDialogBuilderService SetCloseButtonText(string closeButtonText);
    IDialogBuilderService SetXamlRoot(XamlRoot xamlRoot);
    object GetDialogContent();
    Task<bool> ShowAsync();
}
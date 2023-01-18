using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace WSLStudio.Contracts.Services;

// TODO : Add other build parts
public interface IDialogBuilderService
{
    IDialogBuilderService SetTitle(object title);

    IDialogBuilderService SetContent(object content);

    IDialogBuilderService SetPrimaryButtonText(string primaryButtonText);

    IDialogBuilderService SetPrimaryButtonClick(
        TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickEventHandler);

    IDialogBuilderService SetSecondaryButtonText(string secondaryButtonText);

    IDialogBuilderService SetSecondaryButtonClick(
        TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickEventHandler);

    IDialogBuilderService SetCloseButtonText(string closeButtonText);

    IDialogBuilderService SetCloseButtonClick(
        TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickEventHandler);

    IDialogBuilderService SetDefaultButton(ContentDialogButton button);

    IDialogBuilderService SetXamlRoot(XamlRoot xamlRoot);

    IDialogBuilderService AddContent(FrameworkElement element);

    ContentDialog Build();
}
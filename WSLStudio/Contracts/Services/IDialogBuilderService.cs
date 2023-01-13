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

    public IDialogBuilderService SetPrimaryButtonClick(
        TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickEventFunc);

    IDialogBuilderService SetSecondaryButtonText(string secondaryButtonText);

    IDialogBuilderService SetCloseButtonText(string closeButtonText);

    IDialogBuilderService SetDefaultButton(ContentDialogButton button);

    IDialogBuilderService SetXamlRoot(XamlRoot xamlRoot);

    public ContentDialog Build();
}
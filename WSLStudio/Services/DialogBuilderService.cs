using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using WSLStudio.Contracts.Services;

namespace WSLStudio.Services;

public class DialogBuilderService : IDialogBuilderService
{
    private readonly ContentDialog _dialog = new();

    public IDialogBuilderService SetTitle(object title)
    {
        this._dialog.Title = title;
        return this;
    }

    public IDialogBuilderService SetContent(object content)
    {
        this._dialog.Content = content;
        return this;
    }

    public IDialogBuilderService SetPrimaryButtonText(string primaryButtonText)
    {
        this._dialog.PrimaryButtonText = primaryButtonText;
        return this;
    }

    public IDialogBuilderService SetSecondaryButtonText(string secondaryButtonText)
    {
        this._dialog.SecondaryButtonText = secondaryButtonText;
        return this;
    }

    public IDialogBuilderService SetCloseButtonText(string closeButtonText)
    {
        this._dialog.CloseButtonText = closeButtonText;
        return this;
    }

    public IDialogBuilderService SetXamlRoot(XamlRoot xamlRoot)
    {
        this._dialog.XamlRoot = xamlRoot;
        return this;
    }

    public object GetDialogContent()
    {
        if (this._dialog.Content == null)
            return null;

        return this._dialog.Content;
    }

    public async Task<bool> ShowAsync()
    {
        var result = await this._dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
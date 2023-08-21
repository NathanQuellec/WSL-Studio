using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WSLStudio.Contracts.Services;

namespace WSLStudio.Services;

public class DialogBuilderService : IDialogBuilderService
{
    private readonly ContentDialog _contentDialog = new();
    private readonly StackPanel _stackPanel = new();

    public IDialogBuilderService SetTitle(object title)
    {
        this._contentDialog.Title = title;
        return this;
    }

    public IDialogBuilderService SetContent(object content)
    {
        this._contentDialog.Content = content;
        return this;
    }

    public IDialogBuilderService SetPrimaryButtonText(string primaryButtonText)
    {
        this._contentDialog.PrimaryButtonText = primaryButtonText;
        return this;
    }

    public IDialogBuilderService SetPrimaryButtonClick(TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickEventHandler)
    {
        this._contentDialog.PrimaryButtonClick += clickEventHandler;
        return this;
    }

    public IDialogBuilderService SetSecondaryButtonText(string secondaryButtonText)
    {
        this._contentDialog.SecondaryButtonText = secondaryButtonText;
        return this;
    }

    public IDialogBuilderService SetSecondaryButtonClick(
        TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickEventHandler)
    {
        this._contentDialog.SecondaryButtonClick += clickEventHandler;
        return this;
    }

    public IDialogBuilderService SetCloseButtonText(string closeButtonText)
    {
        this._contentDialog.CloseButtonText = closeButtonText;
        return this;
    }

    public IDialogBuilderService SetCloseButtonClick(TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickEventHandler)
    {
        this._contentDialog.CloseButtonClick += clickEventHandler;
        return this;
    }

    public IDialogBuilderService SetDefaultButton(ContentDialogButton button)
    {
        this._contentDialog.DefaultButton = button;
        return this;
    }

    public IDialogBuilderService SetXamlRoot(XamlRoot xamlRoot)
    {
        this._contentDialog.XamlRoot = xamlRoot;
        return this;
    }

    public IDialogBuilderService SetDataContext(object dataContext)
    {
        this._contentDialog.DataContext = dataContext;
        return this;
    }

    public IDialogBuilderService AddContent(FrameworkElement element)
    {
        this._stackPanel.Children.Add(element); 
        this._contentDialog.Content = this._stackPanel;
        return this;
    }

    public ContentDialog Build()
    {
        return this._contentDialog;
    }
}
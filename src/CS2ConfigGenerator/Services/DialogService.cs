using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;

namespace CS2ConfigGenerator.Services;

/// <summary>Okna dialogowe i schowek — za interfejsem, żeby ViewModele dało się testować.</summary>
public interface IDialogService
{
    void ShowInfo(string title, string message);
    void ShowError(string title, string message);
    bool Confirm(string title, string message);
    string? PickSaveFile(string defaultFileName, string? initialDirectory);
    string? PickOpenFile(string? initialDirectory);
    string? PickFolder(string title, string? initialDirectory);
    bool CopyToClipboard(string text);
}

public sealed class DialogService : IDialogService
{
    public void ShowInfo(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool Confirm(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public string? PickSaveFile(string defaultFileName, string? initialDirectory)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Zapisz config",
            FileName = defaultFileName,
            DefaultExt = ".cfg",
            Filter = "Plik konfiguracyjny (*.cfg)|*.cfg|Wszystkie pliki (*.*)|*.*",
            InitialDirectory = initialDirectory ?? ""
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickOpenFile(string? initialDirectory)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Importuj config",
            Filter = "Plik konfiguracyjny (*.cfg)|*.cfg|Wszystkie pliki (*.*)|*.*",
            InitialDirectory = initialDirectory ?? ""
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickFolder(string title, string? initialDirectory)
    {
        var dialog = new OpenFolderDialog { Title = title, InitialDirectory = initialDirectory ?? "" };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public bool CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (COMException)
        {
            return false; // schowek zajęty przez inny program
        }
    }
}

using System.Windows;
using CS2ConfigGenerator.Services;
using CS2ConfigGenerator.ViewModels;

namespace CS2ConfigGenerator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var data = new DataService();
            var viewModel = new MainViewModel(
                data,
                new PresetService(),
                new ConfigGeneratorService(data),
                new ConfigFileService(data),
                new Cs2PathService(),
                new SettingsService(),
                new DialogService());

            new Views.MainWindow { DataContext = viewModel }.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie udało się uruchomić aplikacji:\n\n{ex.Message}", "CS2 Config Generator",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}

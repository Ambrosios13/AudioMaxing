using System.Windows;

namespace AudioMaxing;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        RecoveryState.TryRestore();
        DispatcherUnhandledException += (_, args) =>
        {
            AudioEngine.EmergencyStopAll();
            MessageBox.Show($"O processamento foi interrompido por segurança.\n\n{args.Exception.Message}",
                "Falha no AudioMaxing", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, _) => AudioEngine.EmergencyStopAll();
        base.OnStartup(e);
    }
}

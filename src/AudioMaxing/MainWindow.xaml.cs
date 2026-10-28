using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AudioMaxing;

public partial class MainWindow : Window
{
    private readonly AudioEngine engine = new();
    private readonly DispatcherTimer meterTimer;
    private bool highGainApproved;
    private bool revertingGain;

    public MainWindow()
    {
        InitializeComponent();
        meterTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(80), DispatcherPriority.Background, UpdateMeter, Dispatcher);
        Loaded += (_, _) => RefreshDevices();
        Closing += (_, _) => { meterTimer.Stop(); engine.Dispose(); };
    }

    private void RefreshDevices()
    {
        var devices = engine.GetOutputs();
        OutputDevices.ItemsSource = devices;
        OutputDevices.SelectedIndex = devices.Count > 0 ? 0 : -1;
        if (devices.Count == 0) StatusText.Text = "Nenhum dispositivo de saída ativo foi encontrado.";
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (engine.IsRunning) { StopProcessing("Amplificação desativada; saída anterior restaurada."); return; }
        if (OutputDevices.SelectedItem is not AudioDevice device)
        {
            MessageBox.Show("Selecione um dispositivo de saída.", "AudioMaxing"); return;
        }
        try
        {
            engine.Start(device.Id, (float)GainSlider.Value);
            ToggleButton.Content = "DESATIVAR";
            StatusText.Text = "Ativo: aplicativos → VB-CABLE → processamento → " + device.FriendlyName;
            meterTimer.Start();
        }
        catch (Exception ex)
        {
            StopProcessing("Não foi possível ativar.");
            MessageBox.Show(ex.Message, "Falha ao ativar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded || revertingGain) return;
        if (e.NewValue > 12 && !highGainApproved)
        {
            var answer = MessageBox.Show("Ganhos acima de +12 dB podem causar volume perigoso, distorção e danos. Deseja liberar até +24 dB nesta execução?",
                "Confirmar ganho elevado", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
            {
                revertingGain = true; GainSlider.Value = 12; revertingGain = false;
            }
            else highGainApproved = true;
        }
        GainLabel.Text = $"{GainSlider.Value:0.0} dB";
        engine.SetGain((float)GainSlider.Value);
    }

    private void UpdateMeter(object? sender, EventArgs e)
    {
        float peak = engine.Peak;
        double db = peak > 0.000001f ? 20 * Math.Log10(peak) : -60;
        LevelMeter.Value = Math.Clamp(db, -60, 0);
        LevelLabel.Text = db <= -59.9 ? "−∞ dBFS" : $"{db:0.0} dBFS";
        if (engine.IsLimiting)
        {
            ClipLabel.Text = $"LIMITADOR ATIVO ({engine.GainReductionDb:0.0} dB)";
            ClipLabel.Foreground = System.Windows.Media.Brushes.OrangeRed;
        }
        else
        {
            ClipLabel.Text = "SEM SATURAÇÃO";
            ClipLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(118, 211, 155));
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        GainSlider.Value = 0; highGainApproved = false;
        StopProcessing("Padrões restaurados: desligado e ganho em 0 dB.");
    }

    private void EmergencyButton_Click(object sender, RoutedEventArgs e)
    {
        GainSlider.Value = 0;
        StopProcessing("EMERGÊNCIA: processamento interrompido e saída anterior restaurada.");
    }

    private void OutputDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (engine.IsRunning) StopProcessing("A saída mudou; reative quando estiver pronto.");
    }

    private void StopProcessing(string status)
    {
        engine.Stop(); meterTimer.Stop(); ToggleButton.Content = "ATIVAR";
        LevelMeter.Value = -60; LevelLabel.Text = "−∞ dBFS"; StatusText.Text = status;
    }
}

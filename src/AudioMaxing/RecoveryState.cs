using System.IO;

namespace AudioMaxing;

internal static class RecoveryState
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AudioMaxing");
    private static readonly string FilePath = Path.Combine(Folder, "previous-output.txt");

    public static void Save(string endpointId)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(FilePath, endpointId);
    }

    public static void Clear()
    {
        try { File.Delete(FilePath); } catch { }
    }

    public static void TryRestore()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            string id = File.ReadAllText(FilePath).Trim();
            if (id.Length > 0) PolicyConfig.SetDefaultEndpoint(id);
            Clear();
        }
        catch { /* A interface exibirá dispositivos disponíveis mesmo se o endpoint sumiu. */ }
    }
}

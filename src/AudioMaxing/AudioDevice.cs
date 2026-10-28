namespace AudioMaxing;

public sealed record AudioDevice(string Id, string FriendlyName)
{
    public override string ToString() => FriendlyName;
}

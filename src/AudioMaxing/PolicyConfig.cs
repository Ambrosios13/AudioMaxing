using System.Runtime.InteropServices;

namespace AudioMaxing;

// Interface COM não documentada, amplamente usada por utilitários de áudio no Windows 10/11.
internal static class PolicyConfig
{
    private enum ERole { Console, Multimedia, Communications }

    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private class PolicyConfigClient { }

    [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        void GetMixFormat(); void GetDeviceFormat(); void ResetDeviceFormat(); void SetDeviceFormat();
        void GetProcessingPeriod(); void SetProcessingPeriod(); void GetShareMode(); void SetShareMode();
        void GetPropertyValue(); void SetPropertyValue();
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ERole role);
        void SetEndpointVisibility();
    }

    public static void SetDefaultEndpoint(string id)
    {
        var policy = (IPolicyConfig)new PolicyConfigClient();
        foreach (ERole role in Enum.GetValues(typeof(ERole)))
        {
            int hr = policy.SetDefaultEndpoint(id, role);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
        }
        Marshal.FinalReleaseComObject(policy);
    }
}

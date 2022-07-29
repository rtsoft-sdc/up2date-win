using System;
using System.Runtime.InteropServices;

namespace Up2dateClient
{
    static class Wrapper
    {
#if x64
        [DllImport(@"cppclient\bin-x64\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport(@"cppclient\bin-x86\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr CreateDispatcher(ConfigRequestFunc onConfigRequest, DeploymentActionFunc onDeploymentAction, CancelActionFunc onCancelAction);

#if x64
        [DllImport(@"cppclient\bin-x64\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport(@"cppclient\bin-x86\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern void DeleteDispatcher(IntPtr dispatcher);

#if x64
        [DllImport(@"cppclient\bin-x64\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport(@"cppclient\bin-x86\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern void DownloadArtifact(IntPtr artifact, string location);

#if x64
        [DllImport(@"cppclient\bin-x64\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport(@"cppclient\bin-x86\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern void AddConfigAttribute(IntPtr responseBuilder, string key, string value);

#if x64
        [DllImport(@"cppclient\bin-x64\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport(@"cppclient\bin-x86\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern void RunClient(string clientCertificate, string provisioningEndpoint, string xApigToken, IntPtr dispatcher, AuthErrorActionFunc onAuthErrorAction);

        public delegate void ConfigRequestFunc(IntPtr responseBuilder);

        public delegate bool DeploymentActionFunc(IntPtr artifact, DeploymentInfo info);

        public delegate bool CancelActionFunc(int stopId);

        public delegate void AuthErrorActionFunc(string errorMessage);
    }
}

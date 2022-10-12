using System;
using System.Runtime.InteropServices;

namespace Up2dateClient
{
    public class Wrapper : IWrapper
    {
        public IntPtr CreateDispatcher(ConfigRequestFunc onConfigRequest, DeploymentActionFunc onDeploymentAction, CancelActionFunc onCancelAction)
        {
            return IntPtr.Size == 4
                ? Wrapper32.CreateDispatcher(onConfigRequest, onDeploymentAction, onCancelAction)
                : Wrapper64.CreateDispatcher(onConfigRequest, onDeploymentAction, onCancelAction);
        }

        public void DeleteDispatcher(IntPtr dispatcher)
        {
            if (IntPtr.Size == 4)
            {
                Wrapper32.DeleteDispatcher(dispatcher);
            }
            else
            {
                Wrapper64.DeleteDispatcher(dispatcher);
            }
        }

        public void DownloadArtifact(IntPtr artifact, string location)
        {
            if (IntPtr.Size == 4)
            {
                Wrapper32.DownloadArtifact(artifact, location);
            }
            else
            {
                Wrapper64.DownloadArtifact(artifact, location);
            }
        }

        public void AddConfigAttribute(IntPtr responseBuilder, string key, string value)
        {
            if (IntPtr.Size == 4)
            {
                Wrapper32.AddConfigAttribute(responseBuilder, key, value);
            }
            else
            {
                Wrapper64.AddConfigAttribute(responseBuilder, key, value);
            }
        }

        public void RunClient(string clientCertificate, string provisioningEndpoint, string xApigToken, IntPtr dispatcher, AuthErrorActionFunc onAuthErrorAction)
        {
            if (IntPtr.Size == 4)
            {
                Wrapper32.RunClient(clientCertificate, provisioningEndpoint, xApigToken, dispatcher, onAuthErrorAction);
            }
            else
            {
                Wrapper64.RunClient(clientCertificate, provisioningEndpoint, xApigToken, dispatcher, onAuthErrorAction);
            }
        }
    }

    static class Wrapper64
    {
        [DllImport(@"cppclient\bin-x64\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateDispatcher(ConfigRequestFunc onConfigRequest, DeploymentActionFunc onDeploymentAction, CancelActionFunc onCancelAction);

        [DllImport(@"cppclient\bin-x64\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DeleteDispatcher(IntPtr dispatcher);

        [DllImport(@"cppclient\bin-x64\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DownloadArtifact(IntPtr artifact, string location);

        [DllImport(@"cppclient\bin-x64\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void AddConfigAttribute(IntPtr responseBuilder, string key, string value);

        [DllImport(@"cppclient\bin-x64\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RunClient(string clientCertificate, string provisioningEndpoint, string xApigToken, IntPtr dispatcher, AuthErrorActionFunc onAuthErrorAction);
    }

    static class Wrapper32
    {
        [DllImport(@"cppclient\bin-x86\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateDispatcher(ConfigRequestFunc onConfigRequest, DeploymentActionFunc onDeploymentAction, CancelActionFunc onCancelAction);

        [DllImport(@"cppclient\bin-x86\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DeleteDispatcher(IntPtr dispatcher);

        [DllImport(@"cppclient\bin-x86\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DownloadArtifact(IntPtr artifact, string location);

        [DllImport(@"cppclient\bin-x86\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void AddConfigAttribute(IntPtr responseBuilder, string key, string value);

        [DllImport(@"cppclient\bin-x86\wrapperdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RunClient(string clientCertificate, string provisioningEndpoint, string xApigToken, IntPtr dispatcher, AuthErrorActionFunc onAuthErrorAction);
    }

}

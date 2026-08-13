using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace XiYouJi.EditorTools
{
    [InitializeOnLoad]
    internal static class XiYouJiPackageResolveBootstrap
    {
        private const string SessionKey = "XiYouJi.PackageResolveBootstrap.Ran";

        static XiYouJiPackageResolveBootstrap()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += ResolvePackages;
        }

        [MenuItem("Tools/XiYouJi/Resolve Packages and Start MCP")]
        private static void ResolvePackages()
        {
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            Client.Resolve();
            Debug.Log("[XiYouJi] Package resolution requested. MCP auto-start enabled.");
        }
    }
}

using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace HeroVR.EnvironmentTools
{
    /// <summary>
    /// Pins Windows Standalone to Direct3D11.
    ///
    /// With automatic graphics API selection Unity chose D3D12 for this project, and on the
    /// Built-in Render Pipeline the OpenXR session then presented nothing to the headset: the
    /// scene rendered correctly to the flat Game view but the HMD showed only the skybox, with no
    /// errors or warnings logged. Forcing D3D11 fixes it.
    ///
    /// Windows-only. Android/Quest graphics APIs are untouched, so the standalone Quest build
    /// path is unaffected.
    /// </summary>
    public static class PcvrGraphicsApiFix
    {
        [MenuItem("Tools/HeroVR/Environment/Pin PCVR to D3D11")]
        public static void PinStandaloneToD3D11()
        {
            const BuildTarget target = BuildTarget.StandaloneWindows64;

            GraphicsDeviceType[] current = PlayerSettings.GetGraphicsAPIs(target);
            Debug.Log("[PcvrGraphicsApiFix] Before: automatic=" +
                      PlayerSettings.GetUseDefaultGraphicsAPIs(target) +
                      ", apis=" + string.Join(", ", current));

            PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
            PlayerSettings.SetGraphicsAPIs(target, new[] { GraphicsDeviceType.Direct3D11 });

            AssetDatabase.SaveAssets();

            Debug.Log("[PcvrGraphicsApiFix] After: automatic=" +
                      PlayerSettings.GetUseDefaultGraphicsAPIs(target) +
                      ", apis=" + string.Join(", ", PlayerSettings.GetGraphicsAPIs(target)) +
                      ". Restart the editor for the change to take effect.");
        }
    }
}

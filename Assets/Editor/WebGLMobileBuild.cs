using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MarbleOrchestra.EditorTools
{
    /// <summary>
    /// One-click WebGL build for testing on a phone in portrait fullscreen:
    /// uses the MobilePortrait WebGL template (Assets/WebGLTemplates) and an
    /// uncompressed build so any plain static web server (incl. the local
    /// Python one started below) can serve it without special headers.
    /// </summary>
    public static class WebGLMobileBuild
    {
        private const string OutputDir = "Builds/WebGL";
        private const int Port = 8000;

        [MenuItem("Marble Orchestra/WebGL/Build for Mobile (Portrait)")]
        public static void Build()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog("WebGL-Modul fehlt",
                    "Das 'Web Build Support'-Modul ist für diese Unity-Version nicht installiert.\n\n" +
                    "Unity Hub > Installs > 6000.5.8f1 > Zahnrad > Add modules > 'Web Build Support'.", "OK");
                return;
            }

            PlayerSettings.WebGL.template = "PROJECT:MobilePortrait";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"WebGL build OK ({report.summary.totalSize / (1024 * 1024)} MB) -> {Path.GetFullPath(OutputDir)}");
                EditorUtility.RevealInFinder(Path.Combine(OutputDir, "index.html"));
            }
            else
            {
                Debug.LogError($"WebGL build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
            }
        }

        [MenuItem("Marble Orchestra/WebGL/Start Local Server (WLAN)")]
        public static void StartServer()
        {
            string dir = Path.GetFullPath(OutputDir);
            if (!File.Exists(Path.Combine(dir, "index.html")))
            {
                EditorUtility.DisplayDialog("Kein Build", "Erst 'Build for Mobile (Portrait)' ausführen.", "OK");
                return;
            }

            // Opens a console window running Python's static file server;
            // closing that window stops the server.
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k python -m http.server {Port} --bind 0.0.0.0",
                WorkingDirectory = dir,
                UseShellExecute = true,
            });

            string url = $"http://{GetLanIp()}:{Port}/";
            GUIUtility.systemCopyBuffer = url;
            Debug.Log($"Local server started. On the phone (same WLAN) open: {url} (copied to clipboard)");
            EditorUtility.DisplayDialog("Server läuft",
                $"Auf dem Handy (gleiches WLAN) öffnen:\n\n{url}\n\n(URL ist in der Zwischenablage.)", "OK");
        }

        private static string GetLanIp()
        {
            try
            {
                // No packet is sent - this just asks the OS which local
                // address it would route outbound traffic through.
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("8.8.8.8", 65530);
                return ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
            }
            catch
            {
                return Dns.GetHostEntry(Dns.GetHostName()).AddressList
                    .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "localhost";
            }
        }
    }
}

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using FrooxEngine;
using BaseX;
using CodeX;
using HarmonyLib;
using NeosModLoader;
using Aspose.ThreeD;

namespace VRMLImporter
{
    public class VRMLImporter : NeosMod
    {
        public override string Name => "VRMLImporter";
        public override string Author => "dfgHiatus";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/dfgHiatus/NeosVRMLImporter/";
        public static ModConfiguration config;
        public override void OnEngineInit()
        {
            new Harmony("net.dfgHiatus.VRMLImporter").PatchAll();
            config = GetConfiguration();
            Engine.Current.RunPostInit(() => AssetPatch());
        }

        public static void AssetPatch()
        {
            var aExt = Traverse.Create(typeof(AssetHelper)).Field<Dictionary<AssetClass, List<string>>>("associatedExtensions");
            aExt.Value[AssetClass.Model].Add("wrl");
            aExt.Value[AssetClass.Model].Add("x3d");
        }

        [HarmonyPatch(typeof(ModelPreimporter), "Preimport")]
        public class FileImporterPatch
        {
            public static void Postfix(ref string __result, string model, string tempPath)
            {
                string normalizedExtension = Path.GetExtension(model).Replace(".", "").ToLower();
                if (normalizedExtension == "wrl")
                {
                    Scene scene = new Scene();
                    scene.Open(model);
                    var time = DateTime.Now.Ticks.ToString();
                    var aspose = Path.Combine(Path.GetDirectoryName(model), $"{Path.GetFileNameWithoutExtension(model)}_v2_{time}.glb");
                    var asposePath = Path.Combine(Engine.Current.CachePath, aspose);
                    scene.Save(asposePath);
                    __result = asposePath;
                    return;
                }
                else if (normalizedExtension == "x3d" && BlenderInterface.IsAvailable)
                {
                    var time = DateTime.Now.Ticks.ToString();
                    var blenderTarget = Path.Combine(Engine.Current.CachePath, $"{Path.GetFileNameWithoutExtension(model)}_v2_{time}.glb");
                    ConvertToGLTF(model, blenderTarget);
                    __result = blenderTarget;
                    return;
                }
            }

            private static void ConvertToGLTF(string input, string output)
            {
                // TODO Check if escaping the output path is neccesary for linux
                RunBlenderScript($"import bpy\nbpy.ops.import_scene.x3d(filepath = '{input}')\nbpy.ops.export_scene.gltf(filepath = '{output}')");
            }

            private static void RunBlenderScript(string script, string arguments = "-b -P \"{0}\"")
            {
                string tempBlenderScript = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + ".py");
                File.WriteAllText(tempBlenderScript, script);
                string blenderArgs = string.Format(arguments, tempBlenderScript);
                blenderArgs = "--disable-autoexec " + blenderArgs;
                Process.Start(new ProcessStartInfo(BlenderInterface.Executable, blenderArgs)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }).WaitForExit();
                File.Delete(tempBlenderScript);
            }
        }
    }
}
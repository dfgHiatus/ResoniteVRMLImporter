using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aspose.ThreeD;
using FrooxEngine;
using BaseX;
using CodeX;
using HarmonyLib;
using NeosModLoader;
using System.Linq;
using System.Diagnostics;

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
                if (normalizedExtension == "wrl" && BlenderInterface.IsAvailable)
                {
                    var vrmlConverter = Path.Combine("nml_mods", "vrml_importer", "vrml1tovrml2.exe");
                    if (!File.Exists(vrmlConverter))
                    {
                        UniLog.Warning("VRML v1-v2 Converter was not installed.");
                        return;
                    }

                    // Only convert if VRML 1.0
                    var time = DateTime.Now.Ticks.ToString();
                    string blenderTarget = Path.Combine(Path.GetDirectoryName(model), $"{Path.GetFileNameWithoutExtension(model)}_v2_{time}.glb");
                    using (StreamReader sr = File.OpenText(model))
                    {
                        string s = sr.ReadLine();
                        if (s.StartsWith("#VRML V1.0"))
                        {
                            var convertedModel = $"{Path.GetFileNameWithoutExtension(model)}_v2_{time}.wrl";
                            Process.Start(new ProcessStartInfo(vrmlConverter, string.Format($"{model} {Path.Combine(Path.GetDirectoryName(model), convertedModel)}"))
                            {
                                WindowStyle = ProcessWindowStyle.Hidden,
                                CreateNoWindow = true,
                                UseShellExecute = true
                            }).WaitForExit();

                            ConvertToGLTF(Path.Combine(Path.GetDirectoryName(model), convertedModel), blenderTarget);
                            __result = blenderTarget;
                            return;
                        }
                        else if (s.StartsWith("#VRML V2.0"))
                        {
                            ConvertToGLTF(model, blenderTarget);
                            __result = blenderTarget;
                            return;
                        }
                    }
                }
                else if (normalizedExtension == "x3d" && BlenderInterface.IsAvailable)
                {
                    var time = DateTime.Now.Ticks.ToString();
                    string blenderTarget = Path.Combine(Path.GetDirectoryName(model), $"{Path.GetFileNameWithoutExtension(model)}_v2_{time}.glb");
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
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
        } 

        [HarmonyPatch(typeof(ModelPreimporter), "Preimport")]
        public class FileImporterPatch
        {
            public static void Prefix(ref string __result, string model, string tempPath)
            {
                string normalizedExtension = Path.GetExtension(model).Replace(".", "").ToLower();
                if (FreeCADInterface.IsAvailable && FreeCADInterface.SupportedFormats.Contains(normalizedExtension))
                {
                    string cadTarget = Path.Combine(tempPath, Path.ChangeExtension(Path.GetTempFileName(), "obj"));
                    FreeCADInterface.Tesselate(model, cadTarget, 0.5f);
                    __result = cadTarget;
                    return;
                }
                else if (normalizedExtension == "blend" && BlenderInterface.IsAvailable)
                {
                    string blenderTarget = Path.Combine(tempPath, Path.ChangeExtension(Path.GetTempFileName(), "glb"));
                    BlenderInterface.ExportToGLTF(model, blenderTarget);
                    __result = blenderTarget;
                    return;
                }
                else if (normalizedExtension == "wrl" && BlenderInterface.IsAvailable)
                {
                    var vrmlConverter = Path.Combine("nml_mods", "vrml_importer", "vrml1tovrml2.exe");
                    if (!File.Exists(vrmlConverter))
                    {
                        UniLog.Warning("VRML v1-v2 Converter was not installed.");
                        __result = null;
                        return;
                    }

                    // Only convert if VRML 1.0
                    string blenderTarget = Path.Combine(tempPath, Path.ChangeExtension(Path.GetTempFileName(), "glb"));
                    var rawVRMLwithPath = Path.Combine(tempPath, model);
                    using (StreamReader sr = File.OpenText(rawVRMLwithPath))
                    {
                        string s = sr.ReadLine();
                        if (s.StartsWith("#VRML V1.0"))
                        {
                            var time = DateTime.Now.Ticks.ToString();
                            string converted_v2 = string.Format($"{Path.GetFileNameWithoutExtension(model)}_v2_{time}{normalizedExtension}"); //.wrl
                            Process.Start(new ProcessStartInfo(vrmlConverter, string.Format($"{rawVRMLwithPath} {converted_v2}"))
                            {
                                WindowStyle = ProcessWindowStyle.Normal,
                                CreateNoWindow = false,
                                UseShellExecute = false
                            }).WaitForExit();

                            VRML2ToGLTF(Path.Combine(tempPath, converted_v2), blenderTarget);
                            __result = blenderTarget;
                            UniLog.Log("File format - " + blenderTarget);
                            return;
                        }
                        else if (s.StartsWith("#VRML V2.0"))
                        {
                            VRML2ToGLTF(Path.Combine(tempPath, Path.ChangeExtension(Path.GetTempFileName(), "glb")), blenderTarget);
                            UniLog.Log("File format - " + blenderTarget);
                            __result = blenderTarget;
                            return;
                        }
                    }
                }
                else
                {
                    __result = null;
                }
                
            }

            private static void VRML2ToGLTF(string input, string output)
            {
                // TODO Check if escaping the output path is neccesary for linux
                UniLog.Log("File input - " + input);
                UniLog.Log("File output - " + output);
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
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = false,
                    UseShellExecute = false
                }).WaitForExit();
                File.Delete(tempBlenderScript);
            }
        }
    }
}
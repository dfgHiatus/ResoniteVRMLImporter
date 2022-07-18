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
            // ((Dictionary<AssetClass, List<string>>)typeof(AssetHelper).GetMethod("get_associatedExtensions").Invoke(null, null))[AssetClass.Model].Add(".wrl");
            new Harmony("net.dfgHiatus.VRMLImporter").PatchAll();
            config = GetConfiguration();
        }

        // Check ImportModelAsync
        [HarmonyPatch(typeof(ModelPreimporter), "Preimport")]
        public class FileImporterPatch
        {
            public static void Prefix(ref string __result, string model, string tempPath)
            {
                string normalizedExtension = Path.GetExtension(model).Replace(".", "").ToLower();
                if (FreeCADInterface.IsAvailable && FreeCADInterface.SupportedFormats.Contains(normalizedExtension))
                {
                    string cad = Path.Combine(tempPath, Path.ChangeExtension(Path.GetTempFileName(), "obj"));
                    FreeCADInterface.Tesselate(model, cad, 0.5f);
                    __result = cad;
                }
                if (normalizedExtension == "blend" && BlenderInterface.IsAvailable)
                {
                    string blender = Path.Combine(tempPath, Path.ChangeExtension(Path.GetTempFileName(), "glb"));
                    BlenderInterface.ExportToGLTF(model, blender);
                    __result = blender;
                }
                if (normalizedExtension == "wrl")
                {
                    var vrml = Path.Combine("nml_mods", "vrml_importer", "vr1tovr2.exe");
                    if (File.Exists(vrml))
                    {
                        // Only convert if VRML 1.0
                        var raw = Path.Combine(tempPath, model);
                        using (StreamReader sr = File.OpenText(raw))
                        {
                            string s = sr.ReadLine();
                            if (s.StartsWith("#VRML V1.0"))
                            {
                                var converted = string.Format($"{Path.GetFileNameWithoutExtension(model)}2{normalizedExtension}");
                                Process.Start(new ProcessStartInfo(vrml, string.Format($"{raw} -o {converted}"))
                                {
                                    WindowStyle = ProcessWindowStyle.Hidden
                                }).WaitForExit();
                                __result = Path.Combine(tempPath, converted);
                            }
                        }
                    }
                    else
                    {
                        UniLog.Warning("VRML 1-2 Converter was not installed.");
                    }
                }
                __result = null;
            }
        }
    }
}
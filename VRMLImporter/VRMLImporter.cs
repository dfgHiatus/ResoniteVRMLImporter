using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using FrooxEngine;
using Elements.Core;
using Elements.Assets;
using HarmonyLib;
using ResoniteModLoader;

namespace VRMLImporter;

public class VRMLImporter : ResoniteMod
{
    public override string Name => "VRMLImporter";
    public override string Author => "dfgHiatus";
    public override string Version => "2.0.0";
    public override string Link => "https://github.com/dfgHiatus/ResoniteVRMLmporter/";
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
            var modelName = Path.GetFileNameWithoutExtension(model);
            if (ContainsUnicodeCharacter(modelName))
            {
                throw new ArgumentException("Imported model cannot have unicode characters in its file name.");
            }

            var normalizedExtension = Path.GetExtension(model).Replace(".", "").ToLower();
            var trueCachePath = Path.Combine(Engine.Current.CachePath, "Cache");
            var time = DateTime.Now.Ticks.ToString();

            if (normalizedExtension == "wrl" && BlenderInterface.IsAvailable)
            {
                var vrmlConverter = Path.Combine("rml_mods", "vrml_importer", "vrml1tovrml2.exe");
                if (!File.Exists(vrmlConverter))
                {
                    throw new FileNotFoundException("VRML v1-v2 Converter was not installed.");
                }

                // Only convert if VRML 1.0
                var blenderTarget = Path.Combine(trueCachePath, $"{modelName}_v2_{time}.glb");
                using (StreamReader sr = File.OpenText(model))
                {
                    string s = sr.ReadLine();
                    if (s.StartsWith("#VRML V1.0"))
                    {
                        var convertedModel = $"{modelName}_v2_{time}.wrl";
                        Process.Start(new ProcessStartInfo(vrmlConverter, string.Format($"{model} {Path.Combine(trueCachePath, convertedModel)}"))
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                            UseShellExecute = true
                        }).WaitForExit();

                        VRMLToGLTF(Path.Combine(trueCachePath, convertedModel), blenderTarget);
                        __result = blenderTarget;
                        return;
                    }
                    else if (s.StartsWith("#VRML V2.0"))
                    {
                        VRMLToGLTF(model, blenderTarget);
                        __result = blenderTarget;
                        return;
                    }
                }
            }
            else if (normalizedExtension == "x3d" && BlenderInterface.IsAvailable)
            {
                var blenderTarget = Path.Combine(trueCachePath, $"{modelName}_v2_{time}.glb");
                X3DToGLTF(model, blenderTarget);
                __result = blenderTarget;
                return;
            }
        }

        private static bool ContainsUnicodeCharacter(string input)
        {
            const int MaxAnsiCode = 255;
            return input.Any(c => c > MaxAnsiCode);
        }

        private static void VRMLToGLTF(string input, string output)
        {
            RunBlenderScript($"import bpy\nbpy.ops.import_scene.x3d(filepath = '{input}')\nbpy.ops.export_scene.gltf(filepath = '{output}')");
        }

        private static void X3DToGLTF(string input, string output)
        {
            // This is ridiculous. This only occurs when importing X3Ds, the only reason this works is because Blender renames submeshes on import to Shape_IndexedFaceSet.XXX
            // Someone, break this please so I have a reason to improve it
            RunBlenderScript($"import bpy\nbpy.ops.import_scene.x3d(filepath = '{input}')\nobjs = bpy.data.objects\ntry:\n  objs.remove(objs['Cube'], do_unlink = True)\nexcept:\n  pass\nbpy.ops.export_scene.gltf(filepath = '{output}')");
        }

        private static void RunBlenderScript(string script, string arguments = "-b -P \"{0}\"")
        {
            var tempBlenderScript = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + ".py");
            File.WriteAllText(tempBlenderScript, script);
            var blenderArgs = string.Format(arguments, tempBlenderScript);
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
using System.Diagnostics;

namespace VRMLImporter.Tests
{
    public class Program
    {
        static void Main(string[] args)
        {
            var path = Path.Combine("models/egret.wrl");
            using (StreamReader sr = File.OpenText(path))
            {
                string s = sr.ReadLine() ?? throw new ArgumentException("File was empty");

                var vrmlConverter = Path.Combine("vrml_importer", "vrml1tovrml2.exe");
                if (!File.Exists(vrmlConverter))
                {
                    Console.WriteLine("VRML v1-v2 Converter was not installed.");
                    return;
                }

                if (s.StartsWith("#VRML V1.0"))
                {
                    var time = DateTime.Now.Ticks.ToString();
                    Process.Start(new ProcessStartInfo(Path.Combine("vrml_importer", "vrml1tovrml2.exe"), string.Format($"{path} {$"models/{Path.GetFileNameWithoutExtension(path)}_v2_{time}.wrl"}"))
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = false,
                        UseShellExecute = false
                    }).WaitForExit();
                    Console.WriteLine("VRML V1.0");
                    VRML2ToGLTF($"models/egret_v2_{time}.wrl", $"models/egret_v2_{time}.glb");
                }
                else if (s.StartsWith("#VRML V2.0"))
                {  
                    Console.WriteLine("VRML V2.0");
                    VRML2ToGLTF("models/egret.wrl", "models/egret.glb");
                }
            }
        }

        public static void VRML2ToGLTF(string input, string output)
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
            Process.Start(new ProcessStartInfo(@"C:\Program Files (x86)\Steam\steamapps\common\NeosVR\Tools\Blender\blender.exe", blenderArgs)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = false,
                UseShellExecute = false
            }).WaitForExit();
            File.Delete(tempBlenderScript);
        }
    }
}
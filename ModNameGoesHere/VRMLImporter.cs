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
            ((Dictionary<AssetClass, List<string>>)typeof(AssetHelper).GetMethod("get_associatedExtensions").Invoke(null, null))[AssetClass.Model].Add(".wrl");
            new Harmony("net.dfgHiatus.VRMLImporter").PatchAll();
            config = GetConfiguration();
        }

        // Check ImportModelAsync
        [HarmonyPatch(typeof(ModelImporter), "ImportModel")]
        public class FileImporterPatch
        {
            // TODO: Is file passed as relative or absolute?
            public static bool Prefix(string file, ref Task __result, Slot targetSlot, ModelImportSettings settings, Slot assetsSlot = null, IProgressIndicator progressIndicator = null)
            {
                Uri uriRaw = new Uri(file);
                if (!Uri.IsWellFormedUriString(file, UriKind.RelativeOrAbsolute))
                {
                    throw new ArgumentException($"Provided file path {file} was invalid for .wrl import.");
                }

                string uriConverted = null;
                if (uriRaw.Scheme == "file" && string.Equals(Path.GetExtension(file), ".wrl", StringComparison.OrdinalIgnoreCase))
                {
                    uriConverted = VRMLConverter(file);

                }
                else if (uriRaw.Scheme == "http" || uriRaw.Scheme == "https")
                {
                    uriConverted = VRMLConverter(file);
                }
                /* 
                TODO: Support neosdb links
				else if (uri.Scheme == "neosdb")
                {
					
				} 
                */

                if (uriConverted == null)
                {
                    return false;
                }

                __result = targetSlot.StartTask(async delegate ()
                {
                    await default(ToBackground);
                    LocalDB localDB = targetSlot.World.Engine.LocalDB;
                    await localDB.ImportLocalAssetAsync(uriConverted, LocalDB.ImportLocation.Copy).ConfigureAwait(continueOnCapturedContext: false);

                    await default(ToWorld);
                    targetSlot.Name = Path.GetFileNameWithoutExtension(uriConverted);
                    targetSlot.AttachComponent<MeshCollider>();
                    targetSlot.AttachComponent<ObjectRoot>();
                    var grab = targetSlot.AttachComponent<Grabbable>();
                    grab.Scalable.Value = true;
                });

                return false;
            }

            // TODO possibly put this onto the background task?
            private static string VRMLConverter(string file)
            {
                Scene.FromFile(file).Save(string.Format(Engine.Current.CachePath, Path.GetFileNameWithoutExtension(file), ".gltf"), FileFormat.GLTF2_Binary);
                return string.Format(Engine.Current.CachePath, Path.GetFileNameWithoutExtension(file), ".gltf");
            }
        }
    }
}
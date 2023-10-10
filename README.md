#  ResoniteMLImporter

![VRML](https://user-images.githubusercontent.com/51272212/179760003-51301efc-8bc9-4936-aad4-6421bf7160b5.PNG)

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that enables the import of VRML 1.0, VRML 2.0 and X3D 3D models.

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Place [VRMLImporter.dll](https://github.com/dfgHiatus/ResoniteMLImporter/releases/tag/v1.0.0) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
1. If you do not have [Blender](https://www.blender.org/download/) installed to your C Drive, install and extract it to your Resonite tools folder. This can be found under `C:\Program Files (x86)\Steam\steamapps\common\Resonite\tools\Blender` for a default install.
1. Start the game. If you want to verify that the mod is working you can check your Resonite logs.

Tested only on Windows. Depending on the size of the file, it may take up to a few minutes to fully import. It's OK if the import dialogue seems to hang on "Preimporting" for a little bit - this is where the bulk of the conversion is done.

### Credits
- Thanks to [badhaloninja](https://github.com/badhaloninja) for reflection help
- Thanks to [Roger Kaufman](http://www.interocitors.com/polyhedra/vr1tovr2/) for their VRML 1.0 to VRML 2.0 converter

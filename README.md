# NaninovelPostProcessFX
Postprocessing commands for use within Naninovel. Developed with Naninovel 1.18+ and Post Processing 3.2+.
![naninovelpostprocessgif](https://user-images.githubusercontent.com/77254066/190400417-c9261f8f-93e7-4a5b-a745-6a93733d2ebb.gif)

**Please note that the extension won't work in projects that use URP or HDRP.**

# Post Processing documentation 

You can find information on the functions and parameters of each effect in the Effects section in the Post Processing documentation.
https://docs.unity3d.com/Packages/com.unity.postprocessing@3.2/manual/index.html

Notice that the ColorGrading effect has been split into three different parts: HDR, LDR and External. Check the documentation for info on which one suits you best.  

# Installation

### New Version (V2)
Installation 
1. In Unity's Package Manager, click the plus sign and navigate to *Add package from git URL...*. If you don't have git installed, install it and restart the computer. 
2. Type in https://github.com/idaocracy/NaninovelPostProcess.git and it should install automatically. 
3. Follow step 3 and onwards in the old installation instructions.

**NOTE**: In case you want to make changes to the default settings of an effect prefab, make a copy of it and assign it to the Spawn resources instead.

### Old version (V1)
Check this video for a quick guide on installation and usage:

[![IMAGE ALT TEXT HERE](https://img.youtube.com/vi/iAot5FwPO8w/0.jpg)](https://www.youtube.com/watch?v=iAot5FwPO8w)  

#### 1. Install the Post Processing package via Unity's package manager
![image](https://user-images.githubusercontent.com/77254066/189064761-83970d6f-3c8e-4077-b064-27bfebec6aa2.png)

#### 2. Import the NaninovelPostProcess folder in the github repository into the project.

#### 3. Add Idaocracy.NaninovelPostProcess.Runtime and Idaocracy.NaninovelPostProcess.Editor under Type assemblies in the Engine configuration.
![image](https://user-images.githubusercontent.com/77254066/189537566-d564e248-4073-4917-b71a-4dcae11e4afd.png)

#### 4. If "Add Post Processing Layer to Camera" is enabled, you need to add a PostProcessResources asset. You can find the built-in one by searching PostProcessResources with the All filter.
![image](https://user-images.githubusercontent.com/77254066/189537431-001c919f-b9f6-4041-9342-c335d04453cf.png)

#### 5. Add the included prefabs to the list of Spawn resources. You are free to name them however you want. (Note that in the new version (V2) the extension folder is located under the Packages folder)
![image](https://user-images.githubusercontent.com/77254066/189537667-f873dccb-e740-4427-8931-08fc4e2dd4cf.png)

#### 6. Use my other extension, NaninovelSceneAssistant, or special editor tools in the spawn object inspector to modify the effect and copy the values over to the clipboard

NaninovelSceneAssistant: https://github.com/idaocracy/NaninovelSceneAssistant

![image](https://user-images.githubusercontent.com/77254066/190382028-34050f97-74a7-4100-9add-182596739239.png)

# Texture parameters

**Bloom, Chromatic Aberration, ColorGradingLDR, ColorGradingEXT and Vignette** have a Texture parameter which needs to be set in the editor in prefab mode. Once you have added your textures to the list, you can refer to them by their file name. 

![image](https://user-images.githubusercontent.com/77254066/190378680-f0b69c29-b8a6-4bd3-90c7-112ff6708171.png)

# Tweening tips

All float, color, vector and int values can be tweened, however some effects won't tween in a seamless manner. This is a PostProcessing design issue and not related to this extension. 

As with most built-in spawn effects, you can spawn multiple instances of the effect. Consider doing this to create more seamless transitions. 

Example:
```
@despawn Bloom#1 params:2 wait:false
@spawn Bloom#2 params:2,1 
```

# Performance

Some of the effects may not be designed for lower end platforms. Be sure to profile your game to see if they're a fit. In particular, consider using Naninovel's built in Depth of Field (https://naninovel.com/guide/special-effects.html#depth-of-field-bokeh) or Blur (https://naninovel.com/guide/special-effects.html#blur) over the PostProcessing equivalent as that one seems to have the biggest impact on overall performance (and doesn't look particularly good in orthographic/2D setups)

# Contact

If you need help with the extension, you can contact me on here or on Discord. I am the tech support person (only yellow username) on the official Naninovel discord.

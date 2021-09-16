# CameraAnimation
Create animations for recording scenes in VRChat. Only usable with VR in the current version


Requires [VRChatUtilityKit](https://github.com/loukylor/VRC-Mods/tree/main/VRChatUtilityKit) and [ActionMenuApi](https://github.com/gompocp/ActionMenuApi)

All controls can be found in the actionmenu

![image](https://user-images.githubusercontent.com/20169013/133582222-6a47a60e-7900-4f1b-8690-9cae4441dd5f.png)

Save Pos saves the current position of the camera to add a new point

When creating multiple points a linerenderer and camera lenses are used to indicate where the camera will move along.

![image](https://user-images.githubusercontent.com/20169013/133582714-89299853-4aab-48b6-939e-c167866c77d1.png)

Smoothing describes at what point during the path the interpolation with ne next path already starts. Around 40% seems to work good.
Speed describes the speed when the animation is played. A speed of 50% represents 1 second between points. A slower value here semms better.

The speed value can be changed during the animation if wanted.

Upon playing the animation the stream camera will be imitated and the scene will be send to the VRC window

A quickresult using a animation together with [FreezeFrames](https://github.com/Er1807/FreezeFrame)

https://user-images.githubusercontent.com/20169013/133583821-70713109-cd78-46f9-9290-b432475ddd79.mp4

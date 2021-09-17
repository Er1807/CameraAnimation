# CameraAnimation
Create animations for recording scenes in VRChat. Only usable with VR in the current version


Requires [VRChatUtilityKit](https://github.com/loukylor/VRC-Mods/tree/main/VRChatUtilityKit) and [ActionMenuApi](https://github.com/gompocp/ActionMenuApi)

All controls can be found in the actionmenu

![image](https://user-images.githubusercontent.com/20169013/133786029-e9065da8-a1f7-40ce-857a-4608afe5b747.png)

Save Pos saves the current position of the camera to add a new point

When creating multiple points a linerenderer and camera lenses are used to indicate where the camera will move along.

![image](https://user-images.githubusercontent.com/20169013/133582714-89299853-4aab-48b6-939e-c167866c77d1.png)

Speed describes the speed when the animation is played. A speed of 0% represents 1/2 seconds between points 100% is 4seconds. (might be changed in future)

The speed value can be changed during the animation if wanted.

Upon playing the animation the stream camera will be imitated and the scene will be send to the VRC window

A quickresult 

https://user-images.githubusercontent.com/20169013/133786376-73ff295f-3df4-421f-be62-c127c5a34aea.mp4

And an older version (not smoothed) using a animation together with [FreezeFrames](https://github.com/Er1807/FreezeFrame)

https://user-images.githubusercontent.com/20169013/133583821-70713109-cd78-46f9-9290-b432475ddd79.mp4

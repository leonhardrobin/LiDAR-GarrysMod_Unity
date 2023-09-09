# LiDAR from <s>Garry's Mod</s> Scanner Sombre in Unity3D
This project tries to replicate the LiDAR extensions from Garry's Mod in the Unity game engine. It uses the VFX-Graph to spawn and manage the dots.
But there is a game that already does this: [Scanner Sombre](https://store.steampowered.com/app/475190/Scanner_Sombre/)

## [Youtube Video of Project](https://www.youtube.com/watch?v=r8iuUHw-hjk&t=6s)
## [Youtube Tutorial on how to set it up](https://youtu.be/pbRWRinsbWM)

### Development Platforms
- Windows 10
- Windows 11

### Unity Version
- Unity Version: 2023.1.3f1
- Using the High Definition Render Pipeline

### Target Platform
- Standalone Windows x64
- MacOS Standalone

### What is LiDAR Garry's Mod?
- [Steam page](https://steamcommunity.com/sharedfiles/filedetails/?id=2813176307)
- [The Librarian Youtube Gameplay](https://www.youtube.com/watch?v=ac1LXZUkn8c)


### How?
*Using the HDRP, VFX Graph and Raycasts mostly.*
The Raycasts get the points on the mesh that need a particle to be spawned at. By encoding the position information in a 2D texture, we send it to the GPU (the VFX-Graph). Every particle gets assigned a uv coordinate which represents its position. The uv coordinates position gets set and the particles spawn.


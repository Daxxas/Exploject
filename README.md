# Exploject

Exploject is a prototype for procedural terrain generation
All the terrain generated is fully procedural in 3D (allowing to have caves and overhangs), with collisions and is done in realtime with marching cube and Unity's job system

The core generation of a chunk is done in 3 step here: ```Assets/Scripts/Generation/Terrain/TerrainChunk.cs``` ([Go to function](https://github.com/Daxxas/Exploject/blob/4d5713ebcba29a55652e521c7db610eecf63f04d/Assets/Scripts/Generation/Terrain/TerrainChunk.cs#L192))
  
1. Generate marching cube data 
```Assets/Scripts/Generation/Terrain/Jobs/MarchCubeJob.cs``` ([Go to file](https://github.com/Daxxas/Exploject/blob/main/Assets/Scripts/Generation/Terrain/Jobs/MarchCubeJob.cs))

2. Find the biomes
```Assets/Scripts/Generation/Terrain/Jobs/FindVertexColorJob.cs``` ([Go to file](https://github.com/Daxxas/Exploject/blob/main/Assets/Scripts/Generation/Terrain/Jobs/FindVertexColorJob.cs))

3. Generate the chunk mesh
```Assets/Scripts/Generation/Terrain/Jobs/ChunkMeshJob.cs``` ([Go to file](https://github.com/Daxxas/Exploject/blob/main/Assets/Scripts/Generation/Terrain/Jobs/ChunkMeshJob.cs))

The endless terrain is done here ([Go to file](https://github.com/Daxxas/Exploject/blob/main/Assets/Scripts/Generation/Terrain/EndlessTerrain.cs))

Preview of the project:
https://github.com/Daxxas/Exploject/assets/21078787/2542d5ae-12d9-4169-b087-0a3236b2c243


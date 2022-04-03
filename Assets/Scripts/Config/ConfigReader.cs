// using System;
// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;
//
//
// public class ConfigReader : MonoBehaviour
// {
//
//     [SerializeField] private MapGenerator generator;
//     private string configPath;
//     
//     private void Awake()
//     {
//         string configDirectory = Application.dataPath;
//         
//         if (Application.platform == RuntimePlatform.OSXPlayer) {
//             configDirectory += "/../../";
//         }
//         else if (Application.platform == RuntimePlatform.WindowsPlayer) {
//             configDirectory += "/../";
//         }
//         else if (Application.platform == RuntimePlatform.WindowsEditor) {
//             configDirectory = Application.persistentDataPath + "/";
//         }
//         // Debug.Log(configDirectory);
//
//         if (!Directory.Exists(configDirectory + "config"))
//         {
//             var dir = Directory.CreateDirectory(configDirectory + "config");
//         }
//         configPath = configDirectory + "config/" + "config.yml";
//
//         if (!File.Exists(configPath))
//         {
//             File.Create(configPath);
//         }
//         
//         
//         UpdateConfig();
//         
//     }
//
//     public void UpdateConfig()
//     {
//         // generator.equationHandler.NoiseList.Clear();
//
//         string configContent;
//         if (File.Exists(configPath))
//         {
//             configContent = File.ReadAllText(configPath);
//         }
//         else
//         {
//             File.Create(configPath);
//             return;
//         }
//
//         
//         var deserializer = new DeserializerBuilder()
//             .WithNamingConvention(UnderscoredNamingConvention.Instance)
//             .Build();
//             
//         Config config = deserializer.Deserialize<Config>(configContent);
//
//         foreach (var noiseConfig in config.noise)
//         {
//             FastNoiseLite noise = new FastNoiseLite();
//
//             try
//             {
//                 noise.SetFractalType(noiseConfig.Value.GetTypeAsFractal());
//                 noise.SetNoiseType(noiseConfig.Value.function.GetTypeAsNoise());
//                 noise.SetFrequency(noiseConfig.Value.function.GetFrequency());
//             }
//             catch (Exception e)
//             {
//                 noise.SetNoiseType(noiseConfig.Value.GetTypeAsNoise());
//                 noise.SetFrequency(noiseConfig.Value.GetFrequency());
//             }
//
//             if (noiseConfig.Value.GetOctaves() != null)
//             {
//                 noise.SetFractalOctaves(noiseConfig.Value.GetOctaves());
//             }
//             
//             EquationNoise equationNoise = new EquationNoise();
//             equationNoise.dimension = noiseConfig.Value.GetDimensions();
//             equationNoise.noise = noise;
//             
//             // generator.NoiseList.Add(noiseConfig.Key, equationNoise);
//             
//         }
//     }
// }

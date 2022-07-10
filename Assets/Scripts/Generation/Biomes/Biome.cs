using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


[CreateAssetMenu(fileName = "New Biome", menuName = "Biomes/Biome", order = 1)]
public class Biome : ScriptableObject, ISerializationCallbackReceiver
{
    public bool isSelf = false;
    
    [HideIf("isSelf")] [SerializeField]
    private string id;
    public string Id => id;
    
    [HideIf("isSelf")] 
    public Color color;

    [HideIf("isSelf")] [SerializeField] 
    private List<string> serializedTags = new List<string>();

    public HashSet<string> tags = new HashSet<string>();
 

    public void OnBeforeSerialize ()
    {
        tags = new HashSet<string> (serializedTags);
        tags.Add(Id);

        foreach (var val in tags) {
            if (!serializedTags.Contains (val)) {
                serializedTags.Add (val);
            }
        } 
    }

    public void OnAfterDeserialize ()
    {
        tags.Clear ();

        foreach (var val in serializedTags) {
            tags.Add (val);
        }
    }

    public override string ToString()
    {
        return $"{Id}";
    }
}

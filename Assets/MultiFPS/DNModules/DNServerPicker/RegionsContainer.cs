using UnityEngine;

[CreateAssetMenu(fileName = "RegionsContainer", menuName = "Scriptable Objects/RegionsContainer")]
public class RegionsContainer : ScriptableObject
{
    public RegionDefinition[] Regions;
}

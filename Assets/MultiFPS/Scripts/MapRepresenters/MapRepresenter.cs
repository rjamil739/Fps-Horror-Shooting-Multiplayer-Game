using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using MultiFPS.Gameplay.Gamemodes;

namespace MultiFPS
{

    [CreateAssetMenu(fileName = "representer_map_XXX", menuName = "MultiFPS/MapRepresenter")]
    public class MapRepresenter : ScriptableObject
    {
        [Scene]
        public string Scene;

        public string Name;
        public Gamemodes[] AvailableGamemodes;

        public int[] MaxPlayersPresets = new int[3] {4,8,16};

        public Sprite Icon;
    }
}
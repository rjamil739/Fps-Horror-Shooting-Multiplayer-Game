using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace MultiFPS
{
    public class LayerSetupEditor : Editor
    {
        public async static Task SetupLayers()
        {
            Dictionary<string, int> dic = GetAllLayers();

            ClearAllLayers();
            CreateLayers();

            await Task.Delay(500); //wait for unity to save layers

            //ignore all collisions
            foreach (KeyValuePair<string, int> d1 in dic)
            {
                foreach (KeyValuePair<string, int> d2 in dic)
                {
                    Physics.IgnoreLayerCollision(d1.Value, d2.Value, true);
                }
            }

            //set appropriate collider flags
            Physics.IgnoreLayerCollision((int)GameLayers.character, 0, false);
            Physics.IgnoreLayerCollision((int)GameLayers.item, 0, false);
            Physics.IgnoreLayerCollision((int)GameLayers.ragdoll, 0, false);

            Physics.IgnoreLayerCollision((int)GameLayers.ragdoll, (int)GameLayers.ragdoll, false);
            Physics.IgnoreLayerCollision((int)GameLayers.ragdoll, (int)GameLayers.noBulletProof, false);

            Physics.IgnoreLayerCollision((int)GameLayers.item, (int)GameLayers.item, false);
            Physics.IgnoreLayerCollision((int)GameLayers.item, (int)GameLayers.noBulletProof, false);

            Physics.IgnoreLayerCollision((int)GameLayers.character, (int)GameLayers.character, false);
            Physics.IgnoreLayerCollision((int)GameLayers.character, (int)GameLayers.noBulletProof, false);

            Physics.IgnoreLayerCollision((int)GameLayers.throwables, (int)GameLayers.hitbox, false);
            Physics.IgnoreLayerCollision((int)GameLayers.throwables, (int)GameLayers.throwables, false);
            Physics.IgnoreLayerCollision((int)GameLayers.throwables, (int)GameLayers.noBulletProof, false);
            Physics.IgnoreLayerCollision((int)GameLayers.throwables, 0, false);

            Physics.IgnoreLayerCollision((int)GameLayers.launchedThrowables, 0, false);

            Physics.IgnoreLayerCollision((int)GameLayers.trigger, (int)GameLayers.character, false);
            Physics.IgnoreLayerCollision((int)GameLayers.trigger, (int)GameLayers.item, false);

            var temp = new GameObject();
            temp.name = "Setup...";

            MonoBehaviour ni = temp.AddComponent<NetworkIdentity>(); //avoid getting error
            MonoBehaviour playerGameplayInput = temp.AddComponent<PlayerGameplayInput>();
            MonoBehaviour characterMotor = temp.AddComponent<CharacterMotor>();
            MonoBehaviour characterInstance = temp.AddComponent<CharacterInstance>();
            MonoBehaviour modelSticker = temp.AddComponent<ModelSticker>();
            MonoBehaviour gameplayCamera = temp.AddComponent<GameplayCamera>();

            SetExecOrderFor(playerGameplayInput, 15);
            SetExecOrderFor(characterMotor, 20);
            SetExecOrderFor(characterInstance, 25);
            SetExecOrderFor(modelSticker, 30); //we want to position weapon after player had rotated and moved
                                               //due to animation, so weapon will always perfectly stick to hand

            SetExecOrderFor(gameplayCamera, 35); //we want to position camera after everything else is done with positioning

            void SetExecOrderFor(MonoBehaviour monoBehaviour, int execOrder)
            {
                MonoScript readerScript = MonoScript.FromMonoBehaviour(monoBehaviour);
                if (MonoImporter.GetExecutionOrder(readerScript) != execOrder)
                {
                    MonoImporter.SetExecutionOrder(readerScript, execOrder);
                    Debug.Log("Set exec order for " + readerScript.name);
                }
            }

            DestroyImmediate(temp);
        }
        public static Dictionary<string, int> GetAllLayers()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            int layerSize = layers.arraySize;

            Dictionary<string, int> LayerDictionary = new Dictionary<string, int>();

            for (int i = 0; i < layerSize; i++)
            {
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                string layerName = element.stringValue;

                if (!string.IsNullOrEmpty(layerName))
                {
                    LayerDictionary.Add(layerName, i);
                }
            }
            return LayerDictionary;
        }
        //tools
        public static void ClearAllLayers()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 6; i < 31; i++)
            {
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                element.stringValue = string.Empty;
            }
            tagManager.ApplyModifiedProperties();
        }

        public static void CreateLayers()
        {
            GameLayers[] arr = System.Enum.GetValues(typeof(GameLayers)) as GameLayers[];

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 0; i < arr.Length; i++)
            {
                GameLayers layer = arr[i];
                SerializedProperty element = layers.GetArrayElementAtIndex((int)layer);
                element.stringValue = layer.ToString();
            }

            tagManager.ApplyModifiedProperties(); //save changes
        }
    }
}
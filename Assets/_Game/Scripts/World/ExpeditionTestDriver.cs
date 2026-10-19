#if UNITY_EDITOR
using AbyssHunter.Character;
using AbyssHunter.Enemy;
using AbyssHunter.Inventory;
using UnityEditor;
using UnityEngine;

namespace AbyssHunter.World
{
    // Only for the legacy UI/combat fixtures; the dedicated expedition suite walks the real route.
    public static class ExpeditionTestDriver
    {
        public static void Advance(PlayerMovement player)
        {
            var mission=Object.FindFirstObjectByType<AbyssExpedition>();
            var spawner=Object.FindFirstObjectByType<EnemyWaveSpawner>();
            if(mission==null||player==null)return;
            Transform destination=null;
            if(!mission.HuntComplete){if(spawner.ActiveEnemies==0)destination=spawner.NextApproach;}
            else if(!mission.HasCore)destination=mission.Core;
            else
            {
                destination=mission.Beacon;
                if(!mission.BeaconLit&&mission.CarriedShards<4)
                    player.GetComponent<PlayerInventory>().TryAdd(AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Game/Items/AbyssShard.asset"),4-mission.CarriedShards,out _);
            }
            if(destination!=null)
            {
                var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=destination.position+Vector3.up;cc.enabled=true;
                mission.TryInteract();
            }
        }
    }
}
#endif

using System.Collections;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static UnityEngine.Debug;

namespace PlayerToolsMenu
{
    public class PortableAssetModule : LevelModule
    {
        public string prefabAddress;
        private bool menuActive;
        private bool currentUse;
        private Vector3 cachedPosition;
        private Vector3 cachedRotation;
        private GameObject spawnedObject;
        private AsyncOperationHandle<GameObject> menuAsset;

        public override IEnumerator OnLoadCoroutine()
        {
            Player.onSpawn += new Player.SpawnEvent(PlayerSpawn);
            menuAsset = Addressables.LoadAssetAsync<GameObject>(prefabAddress);
            menuAsset.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Log($"[PlayerToolsMenu][OnLoadCoroutine] Success in Loading Asset: {prefabAddress}");
                    InstantiateLoadedAsset(menuAsset, visible: false);
                    Log($"[PlayerToolsMenu][OnLoadCoroutine] GameObject Instantiated: {spawnedObject.name}");
                }
                else
                {
                    LogWarning($"[PlayerToolsMenu][OnLoadCoroutine][WARNING] Unable to load item from address {prefabAddress}");
                    Addressables.Release(handle);
                }
            };
            yield return null;
        }

        public override void Update()
        {
            base.Update();
            if (Player.currentCreature == null) return;
            CheckInputs();
            if (!menuActive || ((cachedPosition == Player.currentCreature.handLeft.grip.position) && (cachedRotation == Player.currentCreature.handLeft.grip.eulerAngles)) || spawnedObject == null) return;
            spawnedObject.transform.position = Player.currentCreature.handLeft.grip.position;
            spawnedObject.transform.rotation = Quaternion.Euler(Player.currentCreature.handLeft.grip.eulerAngles);
            cachedPosition = spawnedObject.transform.position;
            cachedRotation = spawnedObject.transform.eulerAngles;
        }
        
        void CheckInputs()
        {
            if (PlayerControl.handLeft.usePressed == currentUse) return;
            currentUse = PlayerControl.handLeft.usePressed;
            if (!currentUse || (!menuActive && (PlayerControl.handLeft.gripPressed || Player.currentCreature.handLeft.caster.spellInstance != null || Player.currentCreature.handLeft.grabbedHandle != null))) return;
            ToggleMenuVisible(ref menuActive);
        }

        void InstantiateLoadedAsset(AsyncOperationHandle<GameObject> asset, bool visible = false)
        {
            spawnedObject = Object.Instantiate(asset.Result);
            spawnedObject.SetActive(visible);
            cachedPosition = spawnedObject.transform.position;
            cachedRotation = spawnedObject.transform.eulerAngles;
        }

        void PlayerSpawn(Player player)
        {
            menuActive = false;
            if (spawnedObject == null && menuAsset.Status == AsyncOperationStatus.Succeeded)
            {
                Log($"[PlayerToolsMenu][Player.SpawnEvent] Instantiating player menu...");
                InstantiateLoadedAsset(menuAsset, visible: false);
            }
        }

        void ToggleMenuVisible(ref bool state)
        {
            if (spawnedObject == null || Player.currentCreature == null || Player.currentCreature.ragdoll == null) return;
            state = !state;
            spawnedObject.SetActive(state);
        }
    }
}

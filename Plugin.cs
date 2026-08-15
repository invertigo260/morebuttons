using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using GameNetcodeStuff;

namespace MoreButtons
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.invertigo.morebuttons";
        public const string PLUGIN_NAME = "MoreButtons";
        public const string PLUGIN_VERSION = "1.0.0";

        public ConfigEntry<KeyCode> TeleportKey;
        public ConfigEntry<KeyCode> LightSwitchKey;

        private Harmony harmony;

        private void Awake()
        {
            TeleportKey = Config.Bind("General", "TeleportKey", KeyCode.F5, "Hotkey to teleport. Contextual based on if you are in the ship.");
            LightSwitchKey = Config.Bind("General", "LightSwitchKey", KeyCode.F7, "Hotkey to toggle the ship lights.");

            harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();

            Logger.LogInfo($"Plugin MoreButtons is loaded!");
        }

        private void Update()
        {
            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
                return;

            var localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (localPlayer.isTypingChat || localPlayer.inTerminalMenu || localPlayer.isPlayerDead)
                return;

            if (UnityInput.Current.GetKeyDown(TeleportKey.Value))
            {
                ShipTeleporter[] teleporters = FindObjectsOfType<ShipTeleporter>();
                
                if (localPlayer.isInHangarShipRoom)
                {
                    // Inside ship: Use Inverse Teleporter
                    ShipTeleporter inverseTeleporter = teleporters.FirstOrDefault(t => t.isInverseTeleporter);
                    if (inverseTeleporter != null && (inverseTeleporter.buttonTrigger == null || inverseTeleporter.buttonTrigger.interactable))
                    {
                        inverseTeleporter.PressTeleportButtonOnLocalClient();
                    }
                }
                else
                {
                    // Outside ship: Use Regular Teleporter
                    ShipTeleporter regularTeleporter = teleporters.FirstOrDefault(t => !t.isInverseTeleporter);
                    if (regularTeleporter != null && (regularTeleporter.buttonTrigger == null || regularTeleporter.buttonTrigger.interactable))
                    {
                        if (StartOfRound.Instance != null && StartOfRound.Instance.mapScreen != null)
                        {
                            int targetIndex = -1;
                            var map = StartOfRound.Instance.mapScreen;
                            if (map.radarTargets != null)
                            {
                                for (int i = 0; i < map.radarTargets.Count; i++)
                                {
                                    var target = map.radarTargets[i];
                                    if (target != null && target.transform != null)
                                    {
                                        if (target.transform.GetComponent<PlayerControllerB>() == localPlayer || target.transform == localPlayer.transform)
                                        {
                                            targetIndex = i;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (targetIndex != -1)
                            {
                                map.SwitchRadarTargetAndSync(targetIndex);
                            }
                            else
                            {
                                map.SwitchRadarTargetAndSync((int)localPlayer.playerClientId);
                            }

                            regularTeleporter.PressTeleportButtonOnLocalClient();
                        }
                    }
                }
            }

            if (UnityInput.Current.GetKeyDown(LightSwitchKey.Value))
            {
                ShipLights shipLights = FindObjectOfType<ShipLights>();
                if (shipLights != null)
                {
                    shipLights.ToggleShipLights();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Unity.Netcode.NetworkManager))]
    internal static class NetworkPrefabPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Unity.Netcode.NetworkManager.SetSingleton))]
        private static void RegisterPrefab()
        {
            var prefab = new GameObject(Plugin.PLUGIN_GUID + " Prefab");
            prefab.hideFlags |= HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(prefab);

            var networkObject = prefab.AddComponent<Unity.Netcode.NetworkObject>();

            var fieldInfo = typeof(Unity.Netcode.NetworkObject).GetField("GlobalObjectIdHash", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(networkObject, GetHash(Plugin.PLUGIN_GUID));
            }

            Unity.Netcode.NetworkManager.Singleton.PrefabHandler.AddNetworkPrefab(prefab);
        }

        private static uint GetHash(string value)
        {
            return value?.Aggregate(17u, (current, c) => unchecked((current * 31) ^ c)) ?? 0u;
        }
    }
}

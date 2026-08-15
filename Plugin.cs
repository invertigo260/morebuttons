using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System.Linq;
using GameNetcodeStuff;

namespace MoreButtons
{
    [BepInPlugin("com.invertigo.morebuttons", "MoreButtons", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public ConfigEntry<KeyCode> TeleportKey;
        public ConfigEntry<KeyCode> LightSwitchKey;

        private void Awake()
        {
            TeleportKey = Config.Bind("General", "TeleportKey", KeyCode.F5, "Hotkey to teleport. Contextual based on if you are in the ship.");
            LightSwitchKey = Config.Bind("General", "LightSwitchKey", KeyCode.F7, "Hotkey to toggle the ship lights.");

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
}

using BepInEx;
using BepInEx.Logging;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Photon.Voice.PUN;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GorillaNetworking;
using UnityEngine.InputSystem;
using ExitGames.Client.Photon;
using Newtonsoft.Json.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace GorillaBotIntegrated
{
    [BepInPlugin("com.gorilla.botintegrated", "Gorilla Bot Integrated", "1.1")]
    public class GorillaBotPlugin : BaseUnityPlugin
    {
        public static GorillaBotPlugin Instance;
        public static ManualLogSource Log;
        private SharedSystemMicrophone _sharedMicrophone;

        private List<BotInstance> _bots = new List<BotInstance>();
        private bool _uiVisible = true;
        private Rect _windowRect = new Rect(20, 20, 600, 820);
        private Vector2 _scrollPos = Vector2.zero;

        private string _botCountInput = "1";
        private string _roomInput = "";
        private string _namePrefix = "Bot";
        private string _mp3PathAll = "";
        private string _gameMode = "forestDEFAULTINFECTION";
        private bool _use2023 = false;
        private bool _useMetro2024 = false;
        private bool _useRandomBotColor = true;
        private float _botColorR = 0.5f;
        private float _botColorG = 0.5f;
        private float _botColorB = 0.5f;
        private readonly string[] _regions = { "usw", "us", "eu" };

        private Dictionary<string, string> _renameBuffers = new Dictionary<string, string>();
        private Dictionary<string, string> _roomBuffers = new Dictionary<string, string>();
        private Dictionary<string, string> _mp3Buffers = new Dictionary<string, string>();
        private bool _autoWardrobeEnabled = false;
        private Coroutine _autoWardrobeCoroutine;
        private readonly string[] _autoWardrobeCategories = { "hat", "badge", "face" };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Log = Logger;
                DontDestroyOnLoad(gameObject);
                Logger.LogInfo("=== Gorilla Bot Integrated Loaded ===");
                Logger.LogInfo("Press F8 to toggle UI");
            }
            else Destroy(gameObject);

            var patcher = new GameObject("BotNetworkPatcher");
            DontDestroyOnLoad(patcher);
            patcher.AddComponent<BotNetworkPatcher>();
        }

        private void CapturePlayerPosition()
        {
            try
            {
                if (GorillaTagger.Instance == null)
                {
                    Log.LogWarning("[FORMATIONS] GorillaTagger.Instance is null.");
                    return;
                }

                Transform playerTransform = null;

                // Try the main GorillaTagger player object first.
                if (GorillaTagger.Instance.offlineVRRig != null)
                {
                    playerTransform = GorillaTagger.Instance.offlineVRRig.transform;
                }

                if (playerTransform == null)
                {
                    Log.LogWarning(
                        "[FORMATIONS] Could not find the local Gorilla player transform."
                    );

                    return;
                }

                _customWorldPosition = playerTransform.position;
                _customLocalPosition = playerTransform.localPosition;
                _customWorldRotation = playerTransform.rotation;
                _customLocalRotation = playerTransform.localRotation;
                _customLocalScale = playerTransform.localScale;

                _customPositionSaved = true;

                Log.LogInfo(
                    "[FORMATIONS] Created custom position:\n" +
                    $"World Position: {_customWorldPosition}\n" +
                    $"Local Position: {_customLocalPosition}\n" +
                    $"World Rotation: {_customWorldRotation.eulerAngles}\n" +
                    $"Local Rotation: {_customLocalRotation.eulerAngles}\n" +
                    $"Local Scale: {_customLocalScale}"
                );
            }
            catch (Exception ex)
            {
                Log.LogError(
                    $"[FORMATIONS] CapturePlayerPosition failed: {ex}"
                );
            }
        }


        internal IAudioReader<float> GetSystemMicrophoneReader()
        {
            try
            {
                if (_sharedMicrophone == null)
                {
                    var go = new GameObject("GorillaBotSharedSystemMicrophone");
                    DontDestroyOnLoad(go);
                    _sharedMicrophone = go.AddComponent<SharedSystemMicrophone>();
                }

                if (!_sharedMicrophone.Active && !_sharedMicrophone.StartCapture())
                    return null;

                return _sharedMicrophone.CreateReader();
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[MIC] GetSystemMicrophoneReader: {ex.Message}");
                return null;
            }
        }

        private void OnDestroy()
        {
            try { _autoWardrobeEnabled = false; if (_autoWardrobeCoroutine != null) StopCoroutine(_autoWardrobeCoroutine); } catch { }
            try { _sharedMicrophone?.StopCapture(); } catch { }
        }

        public class BotNetworkPatcher : MonoBehaviour, Photon.Realtime.IInRoomCallbacks
        {
            private bool registered = false;

            private void Update()
            {
                if (!registered && PhotonNetwork.InRoom)
                {
                    PhotonNetwork.AddCallbackTarget(this);
                    registered = true;
                    GorillaBotPlugin.Log.LogInfo("[Patcher] Registered callback target");
                }

                if (registered && !PhotonNetwork.InRoom)
                {
                    PhotonNetwork.RemoveCallbackTarget(this);
                    registered = false;
                    GorillaBotPlugin.Log.LogInfo("[Patcher] Unregistered callback target");
                }
            }

            private void OnDestroy()
            {
                if (registered)
                {
                    try { PhotonNetwork.RemoveCallbackTarget(this); } catch { }
                    registered = false;
                }
            }

            public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
            {
                try
                {
                    GorillaBotPlugin.Log.LogInfo(
                        $"[Patcher] PUN player entered: actor={newPlayer.ActorNumber} " +
                        $"userId={newPlayer.UserId} nick={newPlayer.NickName}");

                    // The real game's NetworkSystemPUN already handles the normal
                    // player-enter lifecycle. Do not manually allocate a rig here;
                    // doing so can consume a pooled RigContainer before the
                    // Player Network Controller / VRRigSerializer arrives.
                }
                catch (Exception ex)
                {
                    GorillaBotPlugin.Log.LogError(
                        $"[Patcher] OnPlayerEnteredRoom failed:\n{ex}");
                }
            }



            private static Type FindGameType(string typeName)
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        Type direct = asm.GetType(typeName);
                        if (direct != null) return direct;

                        Type byName = asm.GetTypes().FirstOrDefault(t => t.Name == typeName);
                        if (byName != null) return byName;
                    }
                    catch { }
                }
                return null;
            }

            public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) { }
            public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
            public void OnPlayerPropertiesUpdate(
                Photon.Realtime.Player targetPlayer,
                ExitGames.Client.Photon.Hashtable changedProps)
            { }
            public void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient) { }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f8Key.wasPressedThisFrame)
                _uiVisible = !_uiVisible;

            foreach (var bot in _bots.ToList())
                bot.Pump();
        }

        private Rect _formationWindowRect = new Rect(570, 10, 360, 900);
        private bool _formationWindowInitialized = false;
        private Vector2 _formationScrollPos = Vector2.zero;
        private float _formationSpeed = 1.5f;

        private bool _customPositionSaved = false;
        private Vector3 _customWorldPosition;
        private Vector3 _customLocalPosition;
        private Quaternion _customWorldRotation;
        private Quaternion _customLocalRotation;
        private Vector3 _customLocalScale;

        private void OnGUI()
        {
            if (!_uiVisible) return;

            _windowRect = GUI.Window(99123, _windowRect, DrawWindow, "tables photon bots");

            if (!_formationWindowInitialized)
            {
                _formationWindowRect.x = _windowRect.x + _windowRect.width + 10f;
                _formationWindowRect.y = _windowRect.y;
                _formationWindowInitialized = true;
            }

            _formationWindowRect = GUI.Window(99124, _formationWindowRect, DrawFormationWindow, "BOT FORMATIONS");
        }

        private void ApplyCustomPosition()
        {
            if (!_customPositionSaved)
            {
                Log.LogWarning("[FORMATIONS] No custom position has been created.");
                return;
            }

            if (_bots == null || _bots.Count == 0)
            {
                Log.LogWarning("[FORMATIONS] No bots are active.");
                return;
            }

            for (int i = 0; i < _bots.Count; i++)
            {
                _bots[i].SetCustomFormationPosition(
                    _customWorldPosition,
                    _customWorldRotation,
                    i,
                    _bots.Count
                );
            }

            Log.LogInfo(
                $"[FORMATIONS] Applied custom player position to {_bots.Count} bots."
            );
        }

        private void BrowseWardrobeCategory(string category)
        {
            try
            {
                CosmeticsController controller = CosmeticsController.instance;
                if (controller == null) return;
                controller.PressWardrobeFunctionButton(category);
                controller.UpdateWardrobeModelsAndButtons();
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[WARDROBE] Category '{category}' failed: {ex.Message}");
            }
        }

        private void BrowseWardrobe(string direction)
        {
            try
            {
                CosmeticsController controller = CosmeticsController.instance;
                if (controller == null) return;
                controller.PressWardrobeFunctionButton(direction);
                controller.UpdateWardrobeModelsAndButtons();
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[WARDROBE] Browse '{direction}' failed: {ex.Message}");
            }
        }

        private bool RandomizeWardrobeAndApplyToBots()
        {
            // One-shot version kept for the existing button/API.
            try
            {
                CosmeticsController controller = CosmeticsController.instance;
                if (controller == null)
                {
                    Log.LogWarning("[WARDROBE] CosmeticsController.instance is NULL.");
                    return false;
                }

                string category = _autoWardrobeCategories[UnityEngine.Random.Range(0, _autoWardrobeCategories.Length)];
                controller.PressWardrobeFunctionButton(category);
                controller.UpdateWardrobeModelsAndButtons();

                int presses = UnityEngine.Random.Range(1, 4); // 1-3 displayed item buttons.
                PressRandomVisibleWardrobeButtons(presses);

                controller.PressWardrobeFunctionButton("right");
                controller.UpdateWardrobeModelsAndButtons();
                ApplyCurrentWornSetToBots();

                Log.LogInfo($"[WARDROBE] One-shot: category={category}, itemPresses={presses}, then RIGHT.");
                return true;
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[WARDROBE] RandomizeWardrobeAndApplyToBots failed: {ex}");
                return false;
            }
        }

        private void ToggleAutoWardrobe()
        {
            _autoWardrobeEnabled = !_autoWardrobeEnabled;

            if (_autoWardrobeEnabled)
            {
                if (_autoWardrobeCoroutine != null)
                    StopCoroutine(_autoWardrobeCoroutine);

                _autoWardrobeCoroutine = StartCoroutine(AutoWardrobeLoop());
                Log.LogInfo("[WARDROBE] Automatic wardrobe browsing STARTED.");
            }
            else
            {
                if (_autoWardrobeCoroutine != null)
                {
                    StopCoroutine(_autoWardrobeCoroutine);
                    _autoWardrobeCoroutine = null;
                }

                Log.LogInfo("[WARDROBE] Automatic wardrobe browsing STOPPED.");
            }
        }

        private System.Collections.IEnumerator AutoWardrobeLoop()
        {
            while (_autoWardrobeEnabled)
            {
                CosmeticsController controller = CosmeticsController.instance;
                if (controller == null)
                {
                    yield return new WaitForSecondsRealtime(1f);
                    continue;
                }

                // C# iterator blocks cannot yield from a try block that has a catch.
                // Keep all exception handling around the non-yield work, and yield
                // only after the try/catch has completed.
                bool cycleFailed = false;
                string cycleError = null;

                try
                {
                    // Only the requested categories: hat, badge, face.
                    string category = _autoWardrobeCategories[
                        UnityEngine.Random.Range(0, _autoWardrobeCategories.Length)];

                    controller.PressWardrobeFunctionButton(category);
                    controller.UpdateWardrobeModelsAndButtons();

                    int presses = UnityEngine.Random.Range(1, 4);
                    PressRandomVisibleWardrobeButtons(presses);

                    // Page right, then apply the current worn set to the bots.
                    controller.PressWardrobeFunctionButton("right");
                    controller.UpdateWardrobeModelsAndButtons();
                    ApplyCurrentWornSetToBots();
                }
                catch (Exception ex)
                {
                    cycleFailed = true;
                    cycleError = ex.Message;
                }

                if (cycleFailed)
                {
                    Log.LogWarning($"[WARDROBE] Auto wardrobe cycle failed: {cycleError}");
                    yield return new WaitForSecondsRealtime(0.5f);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(0.35f);
                }
            }

            _autoWardrobeCoroutine = null;
        }

        private void PressRandomVisibleWardrobeButtons(int count)
        {
            WardrobeItemButton[] buttons =
                UnityEngine.Object.FindObjectsOfType<WardrobeItemButton>(true);

            if (buttons == null || buttons.Length == 0)
            {
                Log.LogWarning("[WARDROBE] No visible WardrobeItemButton objects found.");
                return;
            }

            List<WardrobeItemButton> usable = new List<WardrobeItemButton>();
            foreach (WardrobeItemButton button in buttons)
            {
                if (button == null) continue;

                CosmeticsController.CosmeticItem item = button.currentCosmeticItem;
                if (item.isNullItem) continue;

                usable.Add(button);
            }

            if (usable.Count == 0) return;

            int actualCount = Mathf.Clamp(count, 1, Math.Min(3, usable.Count));
            for (int i = 0; i < actualCount; i++)
            {
                int index = UnityEngine.Random.Range(0, usable.Count);
                WardrobeItemButton selected = usable[index];
                usable.RemoveAt(index);

                try
                {
                    selected.ButtonActivationWithHand(false);
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"[WARDROBE] Item button activation failed: {ex.Message}");
                }
            }
        }

        private void ApplyCurrentWornSetToBots()
        {
            try
            {
                CosmeticsController controller = CosmeticsController.instance;
                if (controller == null) return;

                string[] selectedCosmetics = controller.currentWornSet.ToDisplayNameArray();
                if (selectedCosmetics == null || selectedCosmetics.Length != 11)
                {
                    Log.LogWarning("[WARDROBE] currentWornSet did not produce 11 slots.");
                    return;
                }

                _lastWardrobeCosmetics = selectedCosmetics.ToArray();
                SendCosmeticSetToAllBots(_lastWardrobeCosmetics);
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[WARDROBE] Applying current worn set failed: {ex.Message}");
            }
        }

        private string[] _lastWardrobeCosmetics;

        private void ApplyRandomCosmeticToCurrentWornSet(CosmeticsController controller)
        {
            try
            {
                if (controller.allCosmetics == null || controller.allCosmetics.Count == 0) return;

                List<CosmeticsController.CosmeticItem> candidates =
                    controller.allCosmetics
                        .Where(x => !x.isNullItem && !string.IsNullOrWhiteSpace(x.itemName))
                        .ToList();

                if (candidates.Count == 0) return;

                int presses = UnityEngine.Random.Range(2, 5);
                for (int i = 0; i < presses; i++)
                {
                    CosmeticsController.CosmeticItem item =
                        candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    controller.ApplyCosmeticItemToSet(
                        controller.currentWornSet, item, false, false);
                }

                controller.UpdateWornCosmetics(true);
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[WARDROBE] Fallback cosmetic selection failed: {ex.Message}");
            }
        }

        private void SendCosmeticSetToAllBots(string[] displayNames)
        {
            if (displayNames == null || displayNames.Length != 11) return;

            foreach (BotInstance bot in _bots.ToList())
            {
                try { bot.SetCosmeticDisplayNames(displayNames); }
                catch (Exception ex)
                {
                    Log.LogWarning($"[WARDROBE] Bot cosmetic update failed: {ex.Message}");
                }
            }
        }

        private void RandomizeWardrobeLocally()
        {
            try
            {
                // The supplied Assembly-CSharp has WardrobeItemButton as the item
                // button class. Randomly activate a few visible item buttons using
                // the exact ButtonActivationWithHand(false) path.
                WardrobeItemButton[] itemButtons =
                    UnityEngine.Object.FindObjectsOfType<WardrobeItemButton>(true);

                if (itemButtons == null || itemButtons.Length == 0)
                {
                    Log.LogWarning("[WARDROBE] No WardrobeItemButton objects found.");
                    return;
                }

                int presses = UnityEngine.Random.Range(1, Mathf.Min(4, itemButtons.Length) + 1);
                var used = new HashSet<int>();

                for (int i = 0; i < presses; i++)
                {
                    int index;
                    int guard = 0;
                    do
                    {
                        index = UnityEngine.Random.Range(0, itemButtons.Length);
                        guard++;
                    }
                    while (used.Contains(index) && guard < 20);

                    used.Add(index);

                    try
                    {
                        itemButtons[index].ButtonActivationWithHand(false);
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"[WARDROBE] Item button activation failed: {ex.Message}");
                    }
                }

                // WardrobeLeftButton / WardrobeRightButton are GameObject names in
                // this build, not distinct C# classes. Find their GorillaPressableButton
                // component by type name and randomly page left/right.
                Type pressableType = FindGameTypeForPlugin("GorillaPressableButton");
                if (pressableType != null)
                {
                    GameObject left = GameObject.Find("WardrobeLeftButton");
                    GameObject right = GameObject.Find("WardrobeRightButton");
                    bool goRight = UnityEngine.Random.value > 0.5f;
                    GameObject nav = goRight ? right : left;

                    if (nav != null)
                    {
                        Component button = nav.GetComponent(pressableType);
                        if (button != null)
                        {
                            MethodInfo activate = pressableType.GetMethod(
                                "ButtonActivationWithHand",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                null, new[] { typeof(bool) }, null);
                            activate?.Invoke(button, new object[] { false });
                        }
                    }
                }

                Log.LogInfo($"[WARDROBE] Randomized local wardrobe using {presses} item button(s).");
            }
            catch (Exception ex)
            {
                Log.LogError($"[WARDROBE] RandomizeWardrobeLocally failed: {ex}");
            }
        }

        private static Type FindGameTypeForPlugin(string typeName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type direct = asm.GetType(typeName);
                    if (direct != null) return direct;
                    Type byName = asm.GetTypes().FirstOrDefault(t => t.Name == typeName);
                    if (byName != null) return byName;
                }
                catch { }
            }
            return null;
        }

        private void DrawFormationWindow(int id)
        {
            GUILayout.BeginVertical();
            _formationScrollPos = GUILayout.BeginScrollView(
                _formationScrollPos,
                false,
                true,
                GUILayout.Height(Mathf.Max(180f, _formationWindowRect.height - 35f)));

            GUILayout.Label("=== FORMATIONS ===", Bold());
            GUILayout.Label($"Active bots: {_bots.Count}");

            GUILayout.Space(5);
            GUILayout.Label($"Formation Speed: {_formationSpeed:F2}");

            float newSpeed = GUILayout.HorizontalSlider(
                _formationSpeed, 0.05f, 10.0f, GUILayout.Width(330));

            if (!Mathf.Approximately(newSpeed, _formationSpeed))
            {
                _formationSpeed = newSpeed;
                foreach (BotInstance bot in _bots.ToList())
                    bot.SetFormationSpeed(_formationSpeed);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("SLOW", GUILayout.Height(26)))
            {
                _formationSpeed = Mathf.Max(0.05f, _formationSpeed - 0.25f);
                foreach (BotInstance bot in _bots.ToList())
                    bot.SetFormationSpeed(_formationSpeed);
            }
            if (GUILayout.Button("FAST", GUILayout.Height(26)))
            {
                _formationSpeed = Mathf.Min(10f, _formationSpeed + 0.25f);
                foreach (BotInstance bot in _bots.ToList())
                    bot.SetFormationSpeed(_formationSpeed);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("=== CUSTOM POSITION ===", Bold());

            if (GUILayout.Button("CREATE POSITION", GUILayout.Height(32)))
                CapturePlayerPosition();

            if (_customPositionSaved)
            {
                GUILayout.Label($"World: {_customWorldPosition.x:F2}, {_customWorldPosition.y:F2}, {_customWorldPosition.z:F2}");
                GUILayout.Label($"Local: {_customLocalPosition.x:F2}, {_customLocalPosition.y:F2}, {_customLocalPosition.z:F2}");
                if (GUILayout.Button("USE SAVED POSITION", GUILayout.Height(30)))
                    ApplyCustomPosition();
            }
            else
            {
                GUILayout.Label("No custom position saved.");
            }

            GUILayout.Space(8);

            if (GUILayout.Button("DOWN", GUILayout.Height(30))) ApplyFormation("Down");
            if (GUILayout.Button("BOUNCE", GUILayout.Height(30))) ApplyFormation("Bounce");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CIRCLE", GUILayout.Height(30))) ApplyFormation("Circle");
            if (GUILayout.Button("SQUARE", GUILayout.Height(30))) ApplyFormation("Square");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("WAVE", GUILayout.Height(30))) ApplyFormation("Wave");
            if (GUILayout.Button("LINE", GUILayout.Height(30))) ApplyFormation("Line");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("TRIANGLE", GUILayout.Height(30))) ApplyFormation("Triangle");
            if (GUILayout.Button("DIAMOND", GUILayout.Height(30))) ApplyFormation("Diamond");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("GRID", GUILayout.Height(30))) ApplyFormation("Grid");
            if (GUILayout.Button("SPIRAL", GUILayout.Height(30))) ApplyFormation("Spiral");
            GUILayout.EndHorizontal();

            if (GUILayout.Button("HELIX", GUILayout.Height(30))) ApplyFormation("Helix");
            if (GUILayout.Button("SPAZ", GUILayout.Height(36))) ApplyFormation("Spaz");
            if (GUILayout.Button("TELEPORTER", GUILayout.Height(36))) ApplyFormation("Teleporter");

            GUILayout.Label("TELEPORTER relocates each bot every 0.5 seconds around the saved/custom point.");
            GUILayout.Label("SPAZ jitters body, head and both hands near the forest spawn point.");

            GUILayout.Space(8);
            GUILayout.Label("=== MOCK WARDROBE ===", Bold());
            GUILayout.Label("These buttons call CosmeticsController.PressWardrobeFunctionButton() on the local wardrobe.");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("HAT", GUILayout.Height(28))) BrowseWardrobeCategory("hat");
            if (GUILayout.Button("FACE", GUILayout.Height(28))) BrowseWardrobeCategory("face");
            if (GUILayout.Button("BADGE", GUILayout.Height(28))) BrowseWardrobeCategory("badge");
            if (GUILayout.Button("HAND", GUILayout.Height(28))) BrowseWardrobeCategory("hand");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("< LEFT", GUILayout.Height(30))) BrowseWardrobe("left");
            if (GUILayout.Button("RIGHT >", GUILayout.Height(30))) BrowseWardrobe("right");
            GUILayout.EndHorizontal();

            if (GUILayout.Button("RANDOM COSMETIC -> ALL BOTS", GUILayout.Height(34)))
                RandomizeWardrobeAndApplyToBots();

            GUILayout.Space(4);
            if (GUILayout.Button("RANDOMIZE LOCAL WARDROBE", GUILayout.Height(30)))
                RandomizeWardrobeLocally();

            string autoWardrobeLabel = _autoWardrobeEnabled
                ? "STOP AUTO WARDROBE"
                : "START AUTO WARDROBE";
            if (GUILayout.Button(autoWardrobeLabel, GUILayout.Height(34)))
                ToggleAutoWardrobe();

            GUILayout.Label("Auto wardrobe: HAT / BADGE / FACE -> 1-3 item buttons -> RIGHT -> repeat.");

            GUILayout.Space(8);
            if (GUILayout.Button("DISCONNECT ALL BOTS", GUILayout.Height(34)))
                DisconnectAllBots();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 22));
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("spawn HERE", Bold());
            GUILayout.BeginHorizontal();
            GUILayout.Label("count:", GUILayout.Width(50));
            _botCountInput = GUILayout.TextField(_botCountInput, GUILayout.Width(40));
            GUILayout.Label("name:", GUILayout.Width(80));
            _namePrefix = GUILayout.TextField(_namePrefix, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("room code (empty = public hop):", GUILayout.Width(180));
            _roomInput = GUILayout.TextField(_roomInput, GUILayout.Width(120));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("GameMode:", GUILayout.Width(70));
            _gameMode = GUILayout.TextField(_gameMode, GUILayout.Width(220));
            GUILayout.EndHorizontal();

            GUILayout.Label("Bot build", Bold());
            GUILayout.BeginHorizontal();
            bool old2023 = _use2023 && !_useMetro2024;
            bool old2024 = !_use2023 && !_useMetro2024;
            bool oldMetro2024 = _useMetro2024;

            bool new2023 = GUILayout.Toggle(old2023, "2023", GUILayout.Width(65));
            bool new2024 = GUILayout.Toggle(old2024, "2024", GUILayout.Width(65));
            bool newMetro2024 = GUILayout.Toggle(oldMetro2024, "Metro 2024", GUILayout.Width(95));

            if (new2023 && !old2023)
            {
                _use2023 = true;
                _useMetro2024 = false;
            }
            else if (new2024 && !old2024)
            {
                _use2023 = false;
                _useMetro2024 = false;
            }
            else if (newMetro2024 && !oldMetro2024)
            {
                // Metro 2024 uses the normal 2024 connection/auth configuration,
                // but uses the Metro player identity + startup packet sequence.
                _use2023 = false;
                _useMetro2024 = true;
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Bot color", Bold());
            _useRandomBotColor = GUILayout.Toggle(_useRandomBotColor, "Random color per bot");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"R {_botColorR:F2}", GUILayout.Width(55));
            _botColorR = GUILayout.HorizontalSlider(_botColorR, 0f, 1f, GUILayout.Width(120));
            GUILayout.Label($"G {_botColorG:F2}", GUILayout.Width(55));
            _botColorG = GUILayout.HorizontalSlider(_botColorG, 0f, 1f, GUILayout.Width(120));
            GUILayout.Label($"B {_botColorB:F2}", GUILayout.Width(55));
            _botColorB = GUILayout.HorizontalSlider(_botColorB, 0f, 1f, GUILayout.Width(120));
            GUILayout.EndHorizontal();
            GUILayout.Label("RGB values are 0-1. Random is used when enabled.");

            if (GUILayout.Button("spawn seals", GUILayout.Height(32)))
            {
                int count = int.TryParse(_botCountInput, out int c) ? Math.Max(1, c) : 1;
                SpawnMultipleBots(count, _roomInput, _namePrefix, _gameMode, _use2023, _useMetro2024, _useRandomBotColor, new Color(_botColorR, _botColorG, _botColorB, 1f));
            }

            GUILayout.Space(6);
            GUILayout.Label("audio player", Bold());
            _mp3PathAll = GUILayout.TextField(_mp3PathAll, GUILayout.Width(420));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("play MP3 on ALL", GUILayout.Height(26)))
                foreach (var b in _bots) b.PlayMp3(_mp3PathAll);
            if (GUILayout.Button("stop ALL audio", GUILayout.Height(26)))
                foreach (var b in _bots) b.StopAudio();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            string youRoom = "not in room";
            string youRegion = "";
            try
            {
                if (Photon.Pun.PhotonNetwork.InRoom && Photon.Pun.PhotonNetwork.CurrentRoom != null)
                    youRoom = Photon.Pun.PhotonNetwork.CurrentRoom.Name ?? "not in room";
                youRegion = Photon.Pun.PhotonNetwork.CloudRegion ?? "";
            }
            catch { }
            GUILayout.Label($"active: {_bots.Count}  |  You: {youRoom} @ {youRegion}", Bold());
            GUILayout.Label($"Build: {(_useMetro2024 ? "Metro 2024" : (_use2023 ? "2023" : "2024"))} | Color: {(_useRandomBotColor ? "random" : "custom")}");
            GUILayout.Label("(host photon not modified)");

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(360));
            if (_bots.Count == 0)
            {
                GUILayout.Label("No bots yet.");
            }
            else
            {
                foreach (var bot in _bots.ToList())
                {
                    if (!_renameBuffers.ContainsKey(bot.Id)) _renameBuffers[bot.Id] = bot.Name;
                    if (!_roomBuffers.ContainsKey(bot.Id)) _roomBuffers[bot.Id] = bot.TargetRoom ?? "";
                    if (!_mp3Buffers.ContainsKey(bot.Id)) _mp3Buffers[bot.Id] = "";

                    GUILayout.BeginVertical(GUI.skin.box);

                    string status = bot.IsConnected ? "IN ROOM" : (bot.IsConnecting ? "connecting..." : "idle");
                    GUILayout.Label($"{bot.Name}  [{status}]  room={bot.CurrentRoom ?? "-"}  region={bot.CurrentRegion ?? "-"}");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Name:", GUILayout.Width(40));
                    _renameBuffers[bot.Id] = GUILayout.TextField(_renameBuffers[bot.Id], GUILayout.Width(140));
                    if (GUILayout.Button("Rename", GUILayout.Width(60)))
                        bot.SetName(_renameBuffers[bot.Id]);
                    if (GUILayout.Button("X Kill", GUILayout.Width(50)))
                    {
                        bot.Disconnect();
                        _bots.Remove(bot);
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        break;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Room:", GUILayout.Width(40));
                    _roomBuffers[bot.Id] = GUILayout.TextField(_roomBuffers[bot.Id], GUILayout.Width(100));
                    if (GUILayout.Button("Join room", GUILayout.Width(80)))
                        bot.JoinSpecificRoom(_roomBuffers[bot.Id]);
                    if (GUILayout.Button("Public hop", GUILayout.Width(80)))
                        bot.HopPublicRoom(_gameMode);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("MP3:", GUILayout.Width(40));
                    _mp3Buffers[bot.Id] = GUILayout.TextField(_mp3Buffers[bot.Id], GUILayout.Width(260));
                    if (GUILayout.Button("Play", GUILayout.Width(50)))
                        bot.PlayMp3(_mp3Buffers[bot.Id]);
                    if (GUILayout.Button("Stop", GUILayout.Width(50)))
                        bot.StopAudio();
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                    GUILayout.Space(4);
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("DISCONNECT ALL BOTS", GUILayout.Height(28)))
            {
                DisconnectAllBots();
            }

            GUILayout.Label("F8 toggle | Regions tried: usw, us, eu | Room code = 4-char code");
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        public List<BotInstance> GetBotsSnapshot()
        {
            return _bots.ToList();
        }

        private void ApplyFormation(string mode)
        {
            if (_bots == null || _bots.Count == 0)
            {
                Log.LogWarning($"[FORMATIONS] Cannot apply {mode}: no bots are active.");
                return;
            }

            for (int i = 0; i < _bots.Count; i++)
                _bots[i].ConfigureFormation(mode, i, _bots.Count);

            Log.LogInfo($"[FORMATIONS] Applied {mode} to {_bots.Count} bots.");
        }

        public void DisconnectAllBots()
        {
            foreach (var bot in _bots.ToList())
            {
                try { bot.Disconnect(); } catch { }
            }
            _bots.Clear();
            Log.LogInfo("[BOT] All bots disconnected.");
        }

        private GUIStyle Bold() => new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

        public void SpawnMultipleBots(int count, string roomToJoin, string namePrefix, string gameMode,
            bool use2023, bool useMetro2024, bool randomColor, Color customColor)
        {
            Log.LogInfo($"[BOT] Spawning {count} bots...");

            for (int i = 0; i < count; i++)
            {
                // Metro 2024 intentionally uses the identity shown in the supplied
                // Photon join trace.  The server-generated event 255 will then expose
                // these values to the other clients after the bot joins.
                string botName = useMetro2024
                    ? "gorilla1563"
                    : $"{namePrefix}_{i}_{UnityEngine.Random.Range(1000, 9999)}";

                var bot = new BotInstance(
                    botName,
                    roomToJoin,
                    gameMode,
                    _regions,
                    use2023,
                    useMetro2024,
                    randomColor,
                    customColor);

                _bots.Add(bot);
                bot.Connect();

                // Do not block Unity's main thread between bots.
                // The previous Thread.Sleep(400) was a major source of frame stalls.
                if (i + 1 < count)
                    StartCoroutine(DelayedBotConnectNotice(i + 1));
            }
        }

        private IEnumerator DelayedBotConnectNotice(int nextIndex)
        {
            // Merely yields; each BotInstance owns its own connection state.
            yield return null;
        }
    }

    // Shared microphone capture. One Unity microphone stream is used by all bots,
    // avoiding multiple Microphone.Start calls fighting over the same input device.
    internal sealed class SharedSystemMicrophone : MonoBehaviour
    {
        private object _clip;
        private string _device;
        private Type _micType;
        private MethodInfo _getPosition;
        private MethodInfo _getData;
        private int _sampleFrames;
        private int _channels;
        private int _frequency;
        private int _lastPosition = -1;
        private float[] _scratch;
        private readonly object _sync = new object();
        private float[] _ring;
        private long _writeIndex;
        private bool _active;

        public int SamplingRate => _frequency > 0 ? _frequency : 16000;
        public int Channels => 1;
        public bool Active => _active;

        public bool StartCapture()
        {
            if (_active) return true;

            try
            {
                _micType = Type.GetType("UnityEngine.Microphone, UnityEngine.AudioModule")
                        ?? Type.GetType("UnityEngine.Microphone, UnityEngine");
                if (_micType == null)
                {
                    GorillaBotPlugin.Log.LogWarning("[MIC] UnityEngine.Microphone unavailable.");
                    return false;
                }

                PropertyInfo devicesProp = _micType.GetProperty("devices", BindingFlags.Public | BindingFlags.Static);
                string[] devices = devicesProp?.GetValue(null, null) as string[];
                if (devices == null || devices.Length == 0)
                {
                    GorillaBotPlugin.Log.LogWarning("[MIC] No microphone devices found.");
                    return false;
                }

                _device = devices[0];
                _getPosition = _micType.GetMethod("GetPosition", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string) }, null);
                MethodInfo start = _micType.GetMethod("Start", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string), typeof(bool), typeof(int), typeof(int) }, null);
                if (start == null)
                {
                    GorillaBotPlugin.Log.LogWarning("[MIC] Microphone.Start not found.");
                    return false;
                }

                _frequency = 16000;
                _clip = start.Invoke(null, new object[] { _device, true, 2, _frequency });
                if (_clip == null)
                {
                    GorillaBotPlugin.Log.LogWarning("[MIC] Microphone.Start returned null.");
                    return false;
                }

                Type clipType = _clip.GetType();
                PropertyInfo samplesProp = clipType.GetProperty("samples");
                PropertyInfo channelsProp = clipType.GetProperty("channels");
                PropertyInfo frequencyProp = clipType.GetProperty("frequency");
                _sampleFrames = Convert.ToInt32(samplesProp.GetValue(_clip, null));
                _channels = Math.Max(1, Convert.ToInt32(channelsProp.GetValue(_clip, null)));
                _frequency = Convert.ToInt32(frequencyProp.GetValue(_clip, null));
                _getData = clipType.GetMethod("GetData", new[] { typeof(float[]), typeof(int) });

                if (_sampleFrames <= 0 || _getData == null)
                {
                    GorillaBotPlugin.Log.LogWarning("[MIC] Microphone AudioClip does not expose expected data API.");
                    StopCapture();
                    return false;
                }

                // ~2 seconds of mono history, enough for a little buffering without
                // adding a large delay to the bot voice.
                _ring = new float[Math.Max(_frequency * 2, 32000)];
                _scratch = new float[Math.Min(2048 * _channels, _sampleFrames * _channels)];
                _lastPosition = -1;
                _writeIndex = 0;
                _active = true;

                GorillaBotPlugin.Log.LogInfo($"[MIC] Capturing system microphone: '{_device}' {_frequency}Hz channels={_channels}");
                return true;
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning($"[MIC] StartCapture: {ex.Message}");
                StopCapture();
                return false;
            }
        }

        private void Update()
        {
            if (!_active || _clip == null || _getPosition == null || _getData == null)
                return;

            try
            {
                int pos = Convert.ToInt32(_getPosition.Invoke(null, new object[] { _device }));
                if (pos < 0) return;

                if (_lastPosition < 0)
                {
                    _lastPosition = pos;
                    return;
                }

                int available = pos >= _lastPosition
                    ? pos - _lastPosition
                    : (_sampleFrames - _lastPosition) + pos;

                // Prevent pathological jumps from dumping a whole ring in one frame.
                available = Math.Min(available, _sampleFrames);
                if (available <= 0) return;

                int remaining = available;
                int framePos = _lastPosition;
                while (remaining > 0)
                {
                    int chunkFrames = Math.Min(remaining, 2048);
                    int chunkFloats = chunkFrames * _channels;
                    if (_scratch == null || _scratch.Length < chunkFloats)
                        _scratch = new float[chunkFloats];

                    int firstFrames = Math.Min(chunkFrames, _sampleFrames - framePos);
                    int firstFloats = firstFrames * _channels;
                    Array.Clear(_scratch, 0, firstFloats);
                    _getData.Invoke(_clip, new object[] { _scratch, framePos });
                    AppendMono(_scratch, firstFrames);

                    framePos += firstFrames;
                    if (framePos >= _sampleFrames) framePos = 0;
                    remaining -= firstFrames;
                }

                _lastPosition = pos;
            }
            catch
            {
                // Microphone APIs can briefly fail during device changes; keep capture alive.
            }
        }

        private void AppendMono(float[] interleaved, int frames)
        {
            lock (_sync)
            {
                for (int i = 0; i < frames; i++)
                {
                    float sum = 0f;
                    int baseIndex = i * _channels;
                    for (int c = 0; c < _channels; c++)
                        sum += interleaved[baseIndex + c];

                    _ring[_writeIndex % _ring.Length] = sum / _channels;
                    _writeIndex++;
                }
            }
        }

        public IAudioReader<float> CreateReader()
        {
            return new SharedMicReader(this);
        }

        internal int ReadSamples(ref long cursor, float[] buffer)
        {
            lock (_sync)
            {
                long oldest = Math.Max(0, _writeIndex - _ring.Length);
                if (cursor < oldest) cursor = oldest;

                int produced = 0;
                while (produced < buffer.Length && cursor < _writeIndex)
                {
                    buffer[produced++] = _ring[cursor % _ring.Length];
                    cursor++;
                }

                return produced;
            }
        }

        public void StopCapture()
        {
            try
            {
                if (!string.IsNullOrEmpty(_device) && _micType != null)
                {
                    MethodInfo end = _micType.GetMethod("End", BindingFlags.Public | BindingFlags.Static,
                        null, new[] { typeof(string) }, null);
                    end?.Invoke(null, new object[] { _device });
                }
            }
            catch { }

            lock (_sync)
            {
                _writeIndex = 0;
                _lastPosition = -1;
            }

            _active = false;
            _clip = null;
            _device = null;
        }

        private sealed class SharedMicReader : IAudioReader<float>
        {
            private readonly SharedSystemMicrophone _owner;
            private long _cursor;

            public int Channels => 1;
            public int SamplingRate => _owner.SamplingRate;
            public string Error => null;

            public SharedMicReader(SharedSystemMicrophone owner)
            {
                _owner = owner;
                _cursor = 0;
            }

            public bool Read(float[] buffer)
            {
                int got = _owner.ReadSamples(ref _cursor, buffer);
                if (got < buffer.Length)
                    Array.Clear(buffer, got, buffer.Length - got);
                return true;
            }

            public void Dispose() { }
        }
    }

    internal class VoiceLog : Photon.Voice.ILogger
    {
        private readonly string _tag;
        public VoiceLog(string tag) { _tag = tag; }
        public void LogError(string fmt, params object[] args)
        {
            try { GorillaBotPlugin.Log.LogError($"[{_tag}] " + (args != null && args.Length > 0 ? string.Format(fmt, args) : fmt)); } catch { }
        }
        public void LogWarning(string fmt, params object[] args)
        {
            try { GorillaBotPlugin.Log.LogWarning($"[{_tag}] " + (args != null && args.Length > 0 ? string.Format(fmt, args) : fmt)); } catch { }
        }
        public void LogInfo(string fmt, params object[] args)
        {
            try { GorillaBotPlugin.Log.LogInfo($"[{_tag}] " + (args != null && args.Length > 0 ? string.Format(fmt, args) : fmt)); } catch { }
        }
        public void LogDebug(string fmt, params object[] args) { }
    }

    public class BotInstance : IConnectionCallbacks, IMatchmakingCallbacks, IOnEventCallback
    {
        private static int _idCounter;
        public readonly string Id = Interlocked.Increment(ref _idCounter).ToString();

        private string _name;
        private string _targetRoom;
        private string _gameMode;
        private string[] _regions;
        private int _regionIndex;
        private bool _searchingRegions;
        private bool _allowCreate;
        private bool _hopPublic;

        private LoadBalancingTransport _photonClient;
        private bool _connected;
        private bool _inRoom;
        private bool _connecting;
        private string _currentRoom;
        private string _currentRegion;
        private string _savedPfid, _savedTicket, _savedNonce;
        private readonly bool _use2023;
        private readonly bool _useMetro2024;
        private readonly bool _randomColor;
        private readonly Color _customColor;
        private Color _assignedColor;

        private LoadBalancingTransport2 _voiceTransport;
        private bool _voiceRoomJoined;
        private VoiceClient _voiceClient;
        private LocalVoice _localVoice;
        private GameObject _audioGo;
        private object _loadedClip;
        private bool _voiceReady;
        private bool _voiceJoinAttempted;
        private bool _voiceCreateFallbackAttempted;

        // Default system microphone -> Photon Voice. Each bot uses the same
        // system input stream, which is useful for a fan-game bot voice relay.
        private object _microphoneClip; // legacy field; shared capture is used for actual mic data
        private string _microphoneDevice;
        private bool _microphoneActive = false;

        private int _botRigViewId;
        private bool _botRigSpawnSent;
        private Vector3 _rigPosition;
        private int _rigYaw;
        private bool _rigPositionSet;
        private float _lastRigSendTime;
        private VRRig _cachedBotRig;
        private float _nextRigLookupTime;

        // Cosmetic IDs supplied for the bot cosmetic pool. The game RPC expects
        // display names, so each ID is resolved through CosmeticsController.
        private readonly string[] BotCosmeticItemIds =
        {
    "LHADL.",
    "LHAEC.",
    "LBADO.",
    "LFABB.",
    "LBAAQ.",
    "LBAAP.",
    "LSAAC.",
    "LHAAX.",
    "LFAAT.",
    "LBAAN.",
    "LBAAE.",
    "LFAAM.",
    "LFAAN.",
    "LHAAA.",
    "LHAAK.",
    "LHAAL.",
    "LHAAM.",
    "LHAAN.",
    "LHAAO.",
    "LHAAP.",
    "LHABA.",
    "LHABB."
};

        private string _equippedCosmeticId;
        private string[] _equippedCosmeticDisplayNames;
        private bool _cosmeticsBuilt;

        private enum FormationMode
        {
            Down,
            Bounce,
            Circle,
            Square,
            Wave,
            Line,
            Triangle,
            Diamond,
            Grid,
            Spiral,
            Helix,
            Spaz,
            Teleporter
        }

        private FormationMode _formationMode = FormationMode.Down;
        private int _formationSlot;
        private int _formationTotal = 1;
        private float _formationSpeed = 1.5f;
        private float _formationRadius = 4.0f;
        private float _formationHeight = 3.0f;
        private float _formationPhase;

        private float _teleporterTimer;
        private const float TeleporterInterval = 0.5f;

        private Vector3 _teleporterPosition;
        private Quaternion _teleporterHeadRotation;
        private Quaternion _teleporterRightHandRotation;
        private Quaternion _teleporterLeftHandRotation;

        private Vector3 _teleporterRightHandOffset;
        private Vector3 _teleporterLeftHandOffset;

        private static readonly Vector3 ForestCenter = new Vector3(-63.601f, 3.299f, -63.485f);
        private Vector3 _forestSpawnPosition;
        private bool _forestSpawnPositionSet;

        public string Name => _name;
        public string TargetRoom => _targetRoom;
        public bool IsConnected => _connected && _inRoom;
        public bool IsConnecting => _connecting;
        public string CurrentRoom => _currentRoom;
        public string CurrentRegion => _currentRegion;

        public BotInstance(string name, string roomToJoin, string gameMode, string[] regions, bool use2023 = false, bool useMetro2024 = false, bool randomColor = true, Color customColor = default(Color))
        {
            _name = name;
            _use2023 = use2023;
            _useMetro2024 = useMetro2024;
            _randomColor = randomColor;
            _customColor = customColor;
            _assignedColor = _randomColor
                ? new Color(UnityEngine.Random.Range(0.16f, 1f), UnityEngine.Random.Range(0.16f, 1f), UnityEngine.Random.Range(0.16f, 1f), 1f)
                : new Color(Mathf.Clamp01(_customColor.r), Mathf.Clamp01(_customColor.g), Mathf.Clamp01(_customColor.b), 1f);
            _targetRoom = roomToJoin ?? "";
            _gameMode = string.IsNullOrEmpty(gameMode) ? "forestDEFAULTINFECTION" : gameMode;
            _regions = regions ?? new[] { "usw", "us", "eu" };
            _regionIndex = 0;
            _searchingRegions = !string.IsNullOrEmpty(_targetRoom);
            _allowCreate = false;
            _hopPublic = string.IsNullOrEmpty(_targetRoom);
        }

        private void GenerateTeleporterPose()
        {
            // Custom formation positions become the teleporter center too.
            Vector3 center = _useCustomFormationPosition
                ? _customFormationPosition
                : (_forestSpawnPositionSet ? _forestSpawnPosition : ForestCenter);

            // Random position around the center.
            float radius = UnityEngine.Random.Range(0.5f, 5.0f);
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            _teleporterPosition =
                center +
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    UnityEngine.Random.Range(0.0f, 3.5f),
                    Mathf.Sin(angle) * radius
                );

            // Completely random head rotation.
            _teleporterHeadRotation = UnityEngine.Random.rotation;

            // Random arm directions.
            _teleporterRightHandRotation = UnityEngine.Random.rotation;
            _teleporterLeftHandRotation = UnityEngine.Random.rotation;

            // Teleporter hands are deliberately limited to four local directions:
            // UP, DOWN, LEFT, or RIGHT.
            _teleporterRightHandOffset = GetRandomTeleportHandOffset();
            _teleporterLeftHandOffset = GetRandomTeleportHandOffset();

            _rigYaw = UnityEngine.Random.Range(0, 360);

            _teleporterTimer = Time.time + TeleporterInterval;
        }



        private static Vector3 GetRandomTeleportHandOffset()
        {
            const float distance = 1.35f;
            switch (UnityEngine.Random.Range(0, 4))
            {
                case 0: return new Vector3(0f, distance, 0f);
                case 1: return new Vector3(0f, -distance, 0f);
                case 2: return new Vector3(-distance, 0f, 0f);
                default: return new Vector3(distance, 0f, 0f);
            }
        }

        private void SetBotDisplayName(VRRig rig)
        {
            try
            {
                if (rig == null) return;

                // VRRig.playerText is a UnityEngine.UI.Text in this Assembly-CSharp
                // build. Use reflection here so the plugin does not need a direct
                // UnityEngine.UI namespace reference.
                FieldInfo textField = rig.GetType().GetField(
                    "playerText", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                object text = textField?.GetValue(rig);
                if (text != null)
                {
                    PropertyInfo textProp = text.GetType().GetProperty("text",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (textProp?.CanWrite == true)
                        textProp.SetValue(text, _name, null);
                }
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning($"[{_name}] SetBotDisplayName failed: {ex.Message}");
            }
        }

        private void SetRandomBotColor(VRRig rig)
        {
            try
            {
                if (rig == null) return;

                Color color = _assignedColor;
                float red = color.r;
                float green = color.g;
                float blue = color.b;

                // VRRig.SetColor updates VRRig.playerColor/colorInitialized.
                rig.SetColor(color);

                // This build has the three-argument overload.
                rig.InitializeNoobMaterialLocal(red, green, blue);

                // IUserCosmeticsCallback.PendingUpdate is an explicit interface
                // member, so set it through reflection.
                Type callbackType = rig.GetType().GetInterface("IUserCosmeticsCallback");
                PropertyInfo pending = callbackType?.GetProperty(
                    "PendingUpdate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (pending?.CanWrite == true)
                    pending.SetValue(rig, true, null);

                // ApplyColorCode is private and reads the local PlayerPrefs color.
                // Do not overwrite the bot's random color with the local player's color.
                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Color applied: {red:F3}, {green:F3}, {blue:F3}");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] SetRandomBotColor failed: {ex.Message}");
            }
        }

        private static FieldInfo FindFieldDeep(Type type, string name)
        {
            while (type != null)
            {
                try
                {
                    FieldInfo f = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (f != null) return f;
                }
                catch { }
                type = type.BaseType;
            }
            return null;
        }

        private void ConfigureBotRigIdentity(VRRig rig)
        {
            try
            {
                if (rig == null) return;

                Player owner = null;
                int actor = 0;
                try
                {
                    owner = _photonClient?.LocalPlayer;
                    actor = owner?.ActorNumber ?? 0;
                }
                catch { }

                // VRRig.Creator is read-only in the supplied Assembly-CSharp,
                // but its backing field is the actual Photon.Realtime.Player.
                // RigContainer.Creator also writes that same field when the
                // game's normal network lifecycle is used.
                Type rigContainerType = FindGameType("RigContainer");
                Component container = null;
                if (rigContainerType != null)
                {
                    container = rig.GetComponent(rigContainerType) as Component;
                    if (container == null) container = rig.GetComponentInParent(rigContainerType);
                    if (container == null) container = rig.GetComponentInChildren(rigContainerType, true);
                }

                if (container != null && owner != null)
                {
                    try
                    {
                        PropertyInfo creatorProp = rigContainerType.GetProperty(
                            "Creator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (creatorProp?.CanWrite == true)
                            creatorProp.SetValue(container, owner, null);
                    }
                    catch { }
                }

                if (owner != null)
                {
                    try
                    {
                        FieldInfo creatorField = FindFieldDeep(rig.GetType(), "creator");
                        if (creatorField != null && creatorField.FieldType.IsInstanceOfType(owner))
                            creatorField.SetValue(rig, owner);
                    }
                    catch { }
                }

                // VRRig.OwningNetPlayer and creatorWrapped are the game's own
                // network-side identity values. Get the NetPlayer for the bot's
                // actor and assign both where the concrete type matches.
                object netPlayer = null;
                try
                {
                    if (actor > 0 && NetworkSystem.Instance != null)
                    {
                        MethodInfo getPlayer = NetworkSystem.Instance.GetType().GetMethod(
                            "GetPlayer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            null, new[] { typeof(int) }, null);
                        netPlayer = getPlayer?.Invoke(NetworkSystem.Instance, new object[] { actor });

                        if (netPlayer != null)
                        {
                            FieldInfo owning = FindFieldDeep(rig.GetType(), "OwningNetPlayer");
                            if (owning != null && owning.FieldType.IsInstanceOfType(netPlayer))
                                owning.SetValue(rig, netPlayer);

                            FieldInfo wrapped = FindFieldDeep(rig.GetType(), "creatorWrapped");
                            if (wrapped != null && wrapped.FieldType.IsInstanceOfType(netPlayer))
                                wrapped.SetValue(rig, netPlayer);

                            if (container != null)
                            {
                                PropertyInfo wrappedProp = rigContainerType.GetProperty(
                                    "CreatorWrapped", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                if (wrappedProp?.CanWrite == true && wrappedProp.PropertyType.IsInstanceOfType(netPlayer))
                                    wrappedProp.SetValue(container, netPlayer, null);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] NetPlayer identity setup: {ex.Message}");
                }

                // Keep the bot's mic flag enabled.
                rig.IsMicEnabled = true;

                // IMPORTANT: the voice view is normally supplied to
                // RigContainer.InitializeNetwork(...). The field below is
                // private on VRRig, so explicitly bind the PhotonVoiceView
                // that lives on the Gorilla Player Networked object.
                PhotonVoiceView voiceView = FindBotPhotonVoiceView(rig);
                if (voiceView != null)
                {
                    FieldInfo voiceField = FindFieldDeep(rig.GetType(), "myPhotonVoiceView");
                    if (voiceField != null && voiceField.FieldType.IsInstanceOfType(voiceView))
                        voiceField.SetValue(rig, voiceView);

                    if (container != null)
                    {
                        try
                        {
                            PropertyInfo voiceProp = rigContainerType.GetProperty(
                                "Voice", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (voiceProp?.CanWrite == true && voiceProp.PropertyType.IsInstanceOfType(voiceView))
                                voiceProp.SetValue(container, voiceView, null);
                        }
                        catch { }

                        // RefreshVoiceChat enables the actual SpeakerInUse path.
                        try
                        {
                            MethodInfo refresh = rigContainerType.GetMethod(
                                "RefreshVoiceChat", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            refresh?.Invoke(container, null);
                        }
                        catch { }
                    }
                }

                // InitializeNoobMaterialLocal() calls SetColor() and may also
                // refresh playerText from OwningNetPlayer. Apply our bot name
                // LAST so it cannot be overwritten by the color initialization.
                rig.playerName = _name;
                SetBotDisplayName(rig);

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] VRRig configured: actor={actor}, " +
                    $"root='{FindBotNetworkedRoot(rig)?.name}', " +
                    $"Creator={(rig.Creator != null ? rig.Creator.NickName : "NULL")}, " +
                    $"playerName='{rig.playerName}', IsMicEnabled={rig.IsMicEnabled}, " +
                    $"myPhotonVoiceView={(voiceView != null ? "SET" : "NULL")}, " +
                    $"OwningNetPlayer={(rig.OwningNetPlayer != null ? rig.OwningNetPlayer.NickName : "NULL")}");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] ConfigureBotRigIdentity failed: {ex.Message}");
            }
        }

        private static Type FindGameType(string typeName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type direct = asm.GetType(typeName);
                    if (direct != null) return direct;
                    Type byName = asm.GetTypes().FirstOrDefault(t => t.Name == typeName);
                    if (byName != null) return byName;
                }
                catch { }
            }
            return null;
        }

        private VRRig FindBotVRRig()
        {
            try
            {
                if (_cachedBotRig != null)
                    return _cachedBotRig;

                if (Time.unscaledTime < _nextRigLookupTime)
                    return null;

                _nextRigLookupTime = Time.unscaledTime + 0.50f;

                int actor = 0;
                try { actor = _photonClient?.LocalPlayer?.ActorNumber ?? 0; } catch { }

                VRRig[] rigs = UnityEngine.Object.FindObjectsOfType<VRRig>(true);
                VRRig fallback = null;

                foreach (VRRig rig in rigs)
                {
                    if (rig == null) continue;

                    Transform root = rig.transform;
                    while (root.parent != null && root.parent.name != "Gorilla Player Networked(Clone)")
                        root = root.parent;

                    bool networkedRoot =
                        root.name == "Gorilla Player Networked(Clone)" ||
                        rig.transform.name == "Gorilla Player Networked(Clone)";

                    PhotonView[] views = root.GetComponentsInChildren<PhotonView>(true);
                    foreach (PhotonView view in views)
                    {
                        if (view == null) continue;
                        if (_botRigViewId > 0 && view.ViewID == _botRigViewId)
                        {
                            _cachedBotRig = rig;
                            return rig;
                        }
                        if (actor > 0 && view.Owner != null && view.Owner.ActorNumber == actor)
                        {
                            if (networkedRoot)
                            {
                                _cachedBotRig = rig;
                                return rig;
                            }
                            fallback = rig;
                        }
                    }
                }

                if (fallback != null) _cachedBotRig = fallback;
                return fallback;
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning($"[{_name}] FindBotVRRig failed: {ex.Message}");
                return null;
            }
        }

        private Transform FindBotNetworkedRoot(VRRig rig)
        {
            if (rig == null) return null;

            Transform current = rig.transform;
            while (current != null)
            {
                if (current.name == "Gorilla Player Networked(Clone)")
                    return current;
                current = current.parent;
            }

            return rig.transform;
        }

        private PhotonVoiceView FindBotPhotonVoiceView(VRRig rig)
        {
            try
            {
                Transform root = FindBotNetworkedRoot(rig);
                if (root == null) return null;

                PhotonVoiceView direct = root.GetComponent<PhotonVoiceView>();
                if (direct != null) return direct;

                direct = root.GetComponentInChildren<PhotonVoiceView>(true);
                if (direct != null) return direct;

                direct = root.GetComponentInParent<PhotonVoiceView>();
                if (direct != null) return direct;

                // Last resort: locate the voice view whose PhotonView belongs
                // to this bot's actor/view.
                int actor = 0;
                try { actor = _photonClient?.LocalPlayer?.ActorNumber ?? 0; } catch { }
                foreach (PhotonVoiceView vv in UnityEngine.Object.FindObjectsOfType<PhotonVoiceView>(true))
                {
                    if (vv == null) continue;
                    PhotonView pv = vv.GetComponent<PhotonView>() ?? vv.GetComponentInParent<PhotonView>();
                    if (pv != null && ((_botRigViewId > 0 && pv.ViewID == _botRigViewId) ||
                                       (actor > 0 && pv.Owner != null && pv.Owner.ActorNumber == actor)))
                        return vv;
                }
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] FindBotPhotonVoiceView failed: {ex.Message}");
            }

            return null;
        }

        private IEnumerator ApplyBotVisualsWhenReady()
        {
            float elapsed = 0f;
            float nextScan = 0f;
            bool colorApplied = false;

            while (elapsed < 12f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (Time.unscaledTime < nextScan)
                {
                    yield return null;
                    continue;
                }
                nextScan = Time.unscaledTime + 0.40f;

                VRRig rig = FindBotVRRig();
                if (rig != null)
                {
                    ConfigureBotRigIdentity(rig);
                    if (!colorApplied)
                    {
                        SetRandomBotColor(rig);
                        colorApplied = true;
                    }
                }

                yield return null;
            }

            if (!colorApplied)
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] Could not find VRRig after 8 seconds.");
        }

        private string ResolveAppVersion()
        {
            try
            {
                string ver = Photon.Pun.PhotonNetwork.AppVersion;
                if (!string.IsNullOrWhiteSpace(ver) && !IsJunkAppVersion(ver))
                    return ver.Trim();
            }
            catch { }

            try
            {
                var s = Photon.Pun.PhotonNetwork.PhotonServerSettings;
                if (s?.AppSettings != null)
                {
                    string ver = s.AppSettings.AppVersion;
                    if (!string.IsNullOrWhiteSpace(ver) && !IsJunkAppVersion(ver))
                        return ver.Trim();
                }
            }
            catch { }

            try
            {
                var inst = GorillaNetworking.PhotonNetworkController.Instance;
                if (inst != null)
                {
                    var field = inst.GetType().GetField("_gameVersionString",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var val = field?.GetValue(inst) as string;
                    if (!string.IsNullOrWhiteSpace(val) && !IsJunkAppVersion(val))
                        return val.Trim();
                }
            }
            catch { }

            return _use2023 && !_useMetro2024 ? "live1.1.1.60" : "live1.1.1.73";
        }

        private static bool IsJunkAppVersion(string ver)
        {
            if (string.IsNullOrWhiteSpace(ver)) return true;
            ver = ver.Trim();
            if (ver.Equals("MODDED", StringComparison.OrdinalIgnoreCase)) return true;
            if (ver.Equals("modded", StringComparison.Ordinal)) return true;
            return false;
        }

        private static string ResolvePreferredRegion(string[] fallback)
        {
            try
            {
                if (Photon.Pun.PhotonNetwork.InRoom || Photon.Pun.PhotonNetwork.IsConnected)
                {
                    var r = Photon.Pun.PhotonNetwork.CloudRegion;
                    if (!string.IsNullOrEmpty(r))
                    {
                        r = SanitizeRegion(r);
                        if (!string.IsNullOrEmpty(r)) return r;
                    }
                }
            }
            catch { }
            return fallback != null && fallback.Length > 0 ? SanitizeRegion(fallback[0]) : "usw";
        }

        private static string SanitizeRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region)) return "";
            region = region.Trim().ToLowerInvariant();
            int slash = region.IndexOf('/');
            if (slash >= 0) region = region.Substring(0, slash);
            region = region.TrimEnd('*', '/', ' ');
            return region;
        }

        public void SetName(string newName)
        {
            if (string.IsNullOrEmpty(newName)) return;
            _name = newName;
            if (_photonClient != null) _photonClient.NickName = newName;
            _cachedBotRig = null;
            _nextRigLookupTime = 0f;
            if (_botRigSpawnSent)
                GorillaBotPlugin.Instance.StartCoroutine(ApplyBotVisualsWhenReady());
            GorillaBotPlugin.Log.LogInfo($"[{Id}] Renamed to {_name}");
        }

        public void Connect()
        {
            _connecting = true;
            Task.Run(() => ConnectAsync());
        }

        public void JoinSpecificRoom(string room)
        {
            if (string.IsNullOrEmpty(room)) return;
            _targetRoom = room.Trim().ToUpperInvariant();
            _hopPublic = false;
            _searchingRegions = true;
            _regionIndex = 0;
            _allowCreate = false;
            _inRoom = false;
            if (_photonClient != null && _photonClient.IsConnected)
            {
                if (_photonClient.InRoom) _photonClient.OpLeaveRoom(false);
                DisconnectSoft();
            }
            Connect();
        }

        public void HopPublicRoom(string gameMode)
        {
            if (!string.IsNullOrEmpty(gameMode)) _gameMode = gameMode;
            _targetRoom = "";
            _hopPublic = true;
            _searchingRegions = false;
            _allowCreate = true;
            _inRoom = false;
            if (_photonClient != null && _photonClient.IsConnected)
            {
                if (_photonClient.InRoom) _photonClient.OpLeaveRoom(false);
                DisconnectSoft();
            }
            Connect();
        }

        private void DisconnectSoft()
        {
            try
            {
                if (_photonClient != null)
                {
                    try { _photonClient.RemoveCallbackTarget(this); } catch { }
                    if (_photonClient.IsConnected) _photonClient.Disconnect();
                }
            }
            catch { }
            _photonClient = null;
            _connected = false;
            _inRoom = false;
        }

        private async Task ConnectAsync()
        {
            try
            {
                GorillaBotPlugin.Log.LogInfo($"[{_name}] Connecting...");
                var (pfid, token, nonce, ticket) = await GetAuthCredentials();
                if (string.IsNullOrEmpty(pfid))
                {
                    GorillaBotPlugin.Log.LogError($"[{_name}] Failed to get auth credentials");
                    _connecting = false;
                    return;
                }

                _savedPfid = pfid;
                _savedTicket = ticket;
                _savedNonce = nonce;

                string appVer = ResolveAppVersion();
                GorillaBotPlugin.Log.LogInfo($"[{_name}] AppVersion: '{appVer}'");

                _photonClient = new LoadBalancingTransport2(new VoiceLog("voice"), ConnectionProtocol.Udp);
                _photonClient.AppId = _use2023 && !_useMetro2024 ? "d3e694ed-b6d5-494f-8274-009a5cbca8a5" : "d3e694ed-b6d5-494f-8274-009a5cbca8a5";
                _photonClient.AppVersion = appVer;
                _photonClient.NameServerHost = "ns.exitgames.com";
                _photonClient.AddCallbackTarget(this);

                var auth = new AuthenticationValues(Guid.NewGuid().ToString("N"));
                auth.AuthType = CustomAuthenticationType.Custom;
                auth.AddAuthParameter("username", pfid);
                auth.AddAuthParameter("token", "");
                auth.SetAuthPostData(new Dictionary<string, object>
                {
                    { "AppId", "29E62" },
                    { "AppVersion", appVer },
                    { "Ticket", ticket ?? "" },
                    { "Token", "" },
                    { "Nonce", nonce ?? "" }
                });
                _photonClient.AuthValues = auth;
                _photonClient.NickName = _name;

                string preferred = ResolvePreferredRegion(_regions);
                if (_regions != null && _regions.Length > 0)
                {
                    var list = new List<string>();
                    foreach (var r in _regions)
                    {
                        string s = SanitizeRegion(r);
                        if (!string.IsNullOrEmpty(s) && !list.Contains(s)) list.Add(s);
                    }
                    if (!string.IsNullOrEmpty(preferred)) { list.Remove(preferred); list.Insert(0, preferred); }
                    if (list.Count == 0) list.Add("usw");
                    _regions = list.ToArray();
                    _regionIndex = 0;
                }

                string region = _regions[0];
                _currentRegion = region;
                GorillaBotPlugin.Log.LogInfo($"[{_name}] Connecting to {region} appVer={appVer}");

                if (!_photonClient.ConnectToRegionMaster(region))
                {
                    GorillaBotPlugin.Log.LogError($"[{_name}] ConnectToRegionMaster failed");
                    _connecting = false;
                    return;
                }

                int timeout = 0;
                while (!_inRoom && timeout < 800)
                {
                    await Task.Delay(50);
                    timeout++;
                }

                if (!_inRoom)
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] Timeout (state={_photonClient?.State})");
                _connecting = false;
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogError($"[{_name}] Connect error: {ex.Message}");
                _connecting = false;
            }
        }

        private void TryNextRegionOrCreate()
        {
            _regionIndex++;
            if (_regionIndex < _regions.Length)
            {
                string next = _regions[_regionIndex];
                GorillaBotPlugin.Log.LogInfo($"[{_name}] Room missing — next region {next}");
                _currentRegion = next;
                _allowCreate = false;
                ReconnectToRegion(next);
            }
            else
            {
                GorillaBotPlugin.Log.LogError($"[{_name}] Room '{_targetRoom}' not found in any region.");
                _connecting = false;
                _allowCreate = false;
            }
        }

        private void ReconnectToRegion(string region)
        {
            try
            {
                if (_photonClient != null)
                {
                    try { _photonClient.RemoveCallbackTarget(this); } catch { }
                    if (_photonClient.IsConnected) _photonClient.Disconnect();
                }
            }
            catch { }

            _photonClient = new LoadBalancingTransport2(new VoiceLog("voice"), ConnectionProtocol.Udp);
            _photonClient.AppId = _use2023 && !_useMetro2024 ? "d3e694ed-b6d5-494f-8274-009a5cbca8a5" : "d3e694ed-b6d5-494f-8274-009a5cbca8a5";
            string appVer = ResolveAppVersion();
            _photonClient.AppVersion = appVer;
            _photonClient.NameServerHost = "ns.exitgames.com";
            _photonClient.AddCallbackTarget(this);

            if (!string.IsNullOrEmpty(_savedPfid))
            {
                var auth = new AuthenticationValues(Guid.NewGuid().ToString("N"));
                auth.AuthType = CustomAuthenticationType.Custom;
                auth.AddAuthParameter("username", _savedPfid);
                auth.AddAuthParameter("token", "");
                auth.SetAuthPostData(new Dictionary<string, object>
                {
                    { "AppId", "29E62" },
                    { "AppVersion", appVer },
                    { "Ticket", _savedTicket ?? "" },
                    { "Token", "" },
                    { "Nonce", _savedNonce ?? "" }
                });
                _photonClient.AuthValues = auth;
            }
            _photonClient.NickName = _name;
            _currentRegion = region;
            GorillaBotPlugin.Log.LogInfo($"[{_name}] ConnectToRegionMaster({region})");
            _photonClient.ConnectToRegionMaster(region);
        }

        private Hashtable BuildBotPlayerProperties()
        {
            var props = new Hashtable();

            props["didTutorial"] = true;

            if (_useMetro2024)
            {
                // Metro 2024 trace supplied by the user:
                // [255] = "gorilla1563"
                // [253] = "D3F8E8791E36B86E"
                //
                // Event 255 itself is emitted by Photon as the server-side Join
                // event; clients should not try to RaiseEvent(255). Supplying these
                // properties during room entry is what makes the server-generated
                // join packet contain the desired identity.
                props[(byte)255] = "gorilla1563";
                props[(byte)253] = "D3F8E8791E36B86E";
            }
            else
            {
                props[(byte)255] = "0";
                props[(byte)253] = _savedPfid ?? "";
            }

            return props;
        }

        private void SetLocalPlayerPropertiesBeforeJoin()
        {
            try
            {
                var local = _photonClient?.LocalPlayer;
                if (local == null)
                    return;

                local.SetCustomProperties(BuildBotPlayerProperties());

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Prepared join player properties: " +
                    $"PlayFabId={_savedPfid ?? "(none)"}");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] SetLocalPlayerPropertiesBeforeJoin: {ex.Message}");
            }
        }

        private void DoJoinOrCreate()
        {
            if (string.IsNullOrEmpty(_targetRoom))
            {
                DoJoinRandomOrCreate();
                return;
            }

            _targetRoom = _targetRoom.Trim().ToUpperInvariant();

            // Put the player properties in the actual room-entry operation.
            // This is earlier than the old OnJoinedRoom -> OpSetCustomProperties
            // approach and matches the normal PUN join lifecycle.
            var enter = new EnterRoomParams
            {
                RoomName = _targetRoom,
                PlayerProperties = BuildBotPlayerProperties()
            };

            GorillaBotPlugin.Log.LogInfo(
                $"[{_name}] OpJoinRoom('{_targetRoom}') on {_currentRegion} " +
                $"with PlayerProperties PlayFabId={_savedPfid ?? "(none)"}");

            _photonClient.OpJoinRoom(enter);
        }

        private void DoJoinRandomOrCreate()
        {
            SetLocalPlayerPropertiesBeforeJoin();

            var expected = new ExitGames.Client.Photon.Hashtable
            {
                { "gameMode", _gameMode }
            };

            var joinRandom = new OpJoinRandomRoomParams
            {
                ExpectedCustomRoomProperties = expected,
                ExpectedMaxPlayers = 0
            };

            if (!_photonClient.OpJoinRandomRoom(joinRandom))
                CreatePublicRoom();
        }

        private void CreatePublicRoom()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rng = new System.Random();
            var code = new char[4];
            for (int i = 0; i < 4; i++) code[i] = chars[rng.Next(chars.Length)];
            string roomName = new string(code);

            var create = new EnterRoomParams
            {
                RoomName = roomName,
                PlayerProperties = BuildBotPlayerProperties(),
                RoomOptions = new RoomOptions
                {
                    IsVisible = true,
                    IsOpen = true,
                    MaxPlayers = 10,
                    CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "gameMode", _gameMode } },
                    CustomRoomPropertiesForLobby = new[] { "gameMode" }
                }
            };
            GorillaBotPlugin.Log.LogInfo($"[{_name}] Creating room {roomName} mode={_gameMode}");
            _photonClient.OpCreateRoom(create);
        }

        public void OnConnected() { }

        public void OnConnectedToMaster()
        {
            GorillaBotPlugin.Log.LogInfo($"[{_name}] OnConnectedToMaster ({_currentRegion})");
            if (_hopPublic || string.IsNullOrEmpty(_targetRoom))
                DoJoinRandomOrCreate();
            else
            {
                _targetRoom = _targetRoom.Trim().ToUpperInvariant();
                DoJoinOrCreate();
            }
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            GorillaBotPlugin.Log.LogWarning($"[{_name}] Disconnected: {cause}");
            _connected = false;
            _inRoom = false;
        }

        public void OnRegionListReceived(RegionHandler regionHandler) { }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            GorillaBotPlugin.Log.LogInfo($"[{_name}] Custom auth OK");
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            GorillaBotPlugin.Log.LogError($"[{_name}] Custom auth failed: {debugMessage}");
        }

        public void OnJoinedLobby() { }
        public void OnLeftLobby() { }
        public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics) { }

        public void OnEvent(EventData photonEvent)
        {
            try
            {
                // PUN RPC traffic uses event code 200. The game's VRRig asks
                // its owner for cosmetics with RequestCosmetics; answer with
                // the normal UpdateCosmeticsWithTryon RPC format.
                if (photonEvent == null || photonEvent.Code != 200)
                    return;

                if (!(photonEvent.CustomData is Hashtable rpc))
                    return;

                int viewId = 0;
                if (rpc.ContainsKey((byte)0))
                    viewId = Convert.ToInt32(rpc[(byte)0]);

                if (_botRigViewId <= 0 || viewId != _botRigViewId)
                    return;

                string methodName = null;
                if (rpc.ContainsKey((byte)3))
                    methodName = rpc[(byte)3] as string;

                // If the build uses an RPC shortcut, there may be no string
                // method name. In that case the request is still scoped to our
                // newly-created rig, so answering with the cosmetic update is
                // compatible with the normal startup sequence.
                if (!string.IsNullOrEmpty(methodName) &&
                    !methodName.Equals("RequestCosmetics", StringComparison.Ordinal))
                    return;

                int requesterActor = photonEvent.Sender;
                if (requesterActor <= 0)
                    return;

                SendCosmeticsRpc(requesterActor, viewId);
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] Cosmetic RPC receive failed: {ex.Message}");
            }
        }

        public void SetCosmeticDisplayNames(string[] displayNames)
        {
            try
            {
                if (displayNames == null || displayNames.Length != 11) return;

                _equippedCosmeticDisplayNames = displayNames.ToArray();
                _equippedCosmeticId = null;

                if (_botRigViewId > 0 && _photonClient != null && _photonClient.InRoom)
                {
                    string[] tryOn = BuildEmptyCosmeticDisplayNames();
                    if (tryOn == null || tryOn.Length != 11) return;

                    var rpc = new Hashtable
                    {
                        [(byte)0] = _botRigViewId,
                        [(byte)2] = GetBotServerTimestamp(),
                        [(byte)3] = "UpdateCosmeticsWithTryon",
                        [(byte)4] = new object[] { _equippedCosmeticDisplayNames, tryOn }
                    };

                    bool ok = _photonClient.OpRaiseEvent(
                        200,
                        rpc,
                        new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                        new SendOptions { Reliability = true });

                    GorillaBotPlugin.Log.LogInfo(
                        $"[{_name}] Applied wardrobe selection to bot: ok={ok}");
                }
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] SetCosmeticDisplayNames failed: {ex.Message}");
            }
        }

        private string[] BuildRandomCosmeticDisplayNames()
        {
            try
            {
                CosmeticsController controller = CosmeticsController.instance;
                if (controller == null)
                {
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] CosmeticsController.instance is NULL.");
                    return null;
                }

                // Use the actual cosmetic table from the supplied Assembly-CSharp
                // instead of relying on a hard-coded list of IDs. This matches the
                // game's CosmeticItem -> CosmeticSet -> ToDisplayNameArray path.
                if (controller.allCosmetics == null || controller.allCosmetics.Count == 0)
                {
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] CosmeticsController.allCosmetics is empty.");
                    return null;
                }

                List<CosmeticsController.CosmeticItem> candidates =
                    new List<CosmeticsController.CosmeticItem>();

                foreach (CosmeticsController.CosmeticItem item in controller.allCosmetics)
                {
                    if (item.isNullItem) continue;
                    if (string.IsNullOrWhiteSpace(item.itemName)) continue;
                    if (string.IsNullOrWhiteSpace(item.displayName)) continue;
                    candidates.Add(item);
                }

                if (candidates.Count == 0)
                {
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] No usable cosmetics were found.");
                    return null;
                }

                CosmeticsController.CosmeticSet set =
                    new CosmeticsController.CosmeticSet();
                set.ClearSet(controller.nullItem);

                // Pick a small random number of real cosmetic entries. The game's
                // ApplyCosmeticItemToSet handles slot conflicts/categories for us.
                int count = UnityEngine.Random.Range(1, Mathf.Min(4, candidates.Count) + 1);
                for (int i = 0; i < count; i++)
                {
                    CosmeticsController.CosmeticItem item =
                        candidates[UnityEngine.Random.Range(0, candidates.Count)];

                    try
                    {
                        controller.ApplyCosmeticItemToSet(set, item, false, false);
                    }
                    catch (Exception ex)
                    {
                        GorillaBotPlugin.Log.LogWarning(
                            $"[{_name}] Could not apply cosmetic '{item.displayName}': {ex.Message}");
                    }
                }

                string[] result = set.ToDisplayNameArray();
                if (result == null || result.Length != 11)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] CosmeticSet returned {(result == null ? 0 : result.Length)} slots; expected 11.");
                    return null;
                }

                _equippedCosmeticDisplayNames = result;
                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Built random cosmetics from Assembly-CSharp table: {count} item(s).");
                return result;
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] BuildRandomCosmeticDisplayNames failed: {ex}");
                return null;
            }
        }

        private string[] BuildEmptyCosmeticDisplayNames()
        {
            try
            {
                var controller =
                    GorillaNetworking.CosmeticsController.instance;

                if (controller == null)
                    return null;

                if (controller.nullItem.isNullItem)
                {
                    // This is normally fine if nullItem represents the game's
                    // "nothing equipped" cosmetic.
                }

                string[] names = new string[11];

                for (int i = 0; i < names.Length; i++)
                    names[i] = controller.nullItem.displayName;

                var set =
                    new GorillaNetworking.CosmeticsController.CosmeticSet(
                        names,
                        controller);

                string[] result = set.ToDisplayNameArray();

                if (result == null || result.Length != 11)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] Empty CosmeticSet returned " +
                        $"{(result == null ? 0 : result.Length)} slots.");
                    return null;
                }

                return result;
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] BuildEmptyCosmeticDisplayNames failed: {ex}");

                return null;
            }
        }


        private void SendCosmeticsToOthers()
        {
            try
            {
                if (_photonClient == null ||
                    !_photonClient.InRoom ||
                    _botRigViewId <= 0)
                {
                    return;
                }

                string[] cosmeticNames =
                    BuildRandomCosmeticDisplayNames();

                if (cosmeticNames == null)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] Failed to build cosmetic names.");
                    return;
                }

                string[] tryOnNames =
                    BuildEmptyCosmeticDisplayNames();

                if (tryOnNames == null)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] Failed to build try-on names.");
                    return;
                }

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Cosmetic payload: " +
                    $"cosmetics={cosmeticNames.Length}, " +
                    $"tryOn={tryOnNames.Length}");

                var rpc = new Hashtable
                {
                    [(byte)0] = _botRigViewId,
                    [(byte)2] = GetBotServerTimestamp(),
                    [(byte)3] = "UpdateCosmeticsWithTryon",
                    [(byte)4] = new object[]
                    {
                cosmeticNames,
                tryOnNames
                    }
                };

                var options = new RaiseEventOptions
                {
                    Receivers = ReceiverGroup.Others
                };

                bool ok =
                    _photonClient.OpRaiseEvent(
                        200,
                        rpc,
                        options,
                        new SendOptions
                        {
                            Reliability = true
                        });

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Published cosmetics to room clients: " +
                    $"item={_equippedCosmeticId ?? "(none)"} " +
                    $"cosmeticSlots={cosmeticNames.Length} " +
                    $"tryOnSlots={tryOnNames.Length} " +
                    $"ok={ok}");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] SendCosmeticsToOthers failed: {ex}");
            }
        }

        private void SendCosmeticsRpc(int targetActor, int viewId)
        {
            try
            {
                if (_photonClient == null || !_photonClient.InRoom || viewId <= 0)
                    return;

                string[] cosmeticNames = BuildRandomCosmeticDisplayNames();
                string[] tryOnNames = BuildEmptyCosmeticDisplayNames();

                if (cosmeticNames == null || cosmeticNames.Length != 11 ||
                    tryOnNames == null || tryOnNames.Length != 11)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] Invalid cosmetic payload.");
                    return;
                }

                var rpc = new Hashtable
                {
                    [(byte)0] = viewId,
                    [(byte)2] = GetBotServerTimestamp(),
                    [(byte)3] = "UpdateCosmeticsWithTryon",
                    [(byte)4] = new object[]
                    {
                cosmeticNames,
                tryOnNames
                    }
                };

                var options = new RaiseEventOptions
                {
                    TargetActors = new[] { targetActor }
                };

                bool ok = _photonClient.OpRaiseEvent(
                    200,
                    rpc,
                    options,
                    new SendOptions
                    {
                        Reliability = true
                    });

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Sent cosmetics RPC to actor {targetActor}: ok={ok}");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] SendCosmeticsRpc failed: {ex}");
            }
        }

        public void OnJoinedRoom()
        {
            _inRoom = true;
            _connected = true;
            _connecting = false;
            _currentRoom = _photonClient.CurrentRoom?.Name;

            GorillaBotPlugin.Log.LogInfo(
                $"[{_name}] Joined room {_currentRoom} on {_currentRegion}!");

            // The important difference from the old implementation:
            // player properties were supplied during room entry, so the host's
            // Join/PlayerEnteredRoom lifecycle sees the bot's PlayFab ID before
            // the bot asks for its networked Player Controller.
            GorillaBotPlugin.Instance.StartCoroutine(
                MetroPostJoinSequence());

            ConnectVoiceRoom();
        }

        private void SendMetroState200(int stateCode)
        {
            if (_photonClient == null || !_photonClient.InRoom)
                return;

            try
            {
                // Metro 2024 reference state packet. This is deliberately kept
                // separate from the normal cosmetics RPC (which also uses 200).
                var state = new Hashtable
                {
                    [(byte)5] = stateCode,
                    [(byte)4] = new object[] { "#02 '" + _name + "'" },
                    [(byte)2] = GetBotServerTimestamp(),
                    [(byte)0] = 1001
                };

                bool ok = _photonClient.OpRaiseEvent(
                    200,
                    state,
                    new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                    new SendOptions { Reliability = true });

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Sent Metro 200 state={stateCode} ok={ok}");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] SendMetroState200 failed: {ex}");
            }
        }

        private IEnumerator MetroPostJoinSequence()
        {
            // 253 #1 is emitted by Photon as the player's room-entry properties.
            // The real client then publishes its tutorial/player-property state again.
            if (_useMetro2024 && _photonClient != null && _photonClient.InRoom)
            {
                // Reference startup begins with the server-generated 253, then a
                // 200 player-state packet, then the explicit tutorial 253.
                SendMetroState200(31);
                yield return new WaitForSeconds(0.06f);

                try
                {
                    var tutorialProps = new Hashtable
                    {
                        { "didTutorial", true }
                    };
                    _photonClient.LocalPlayer.SetCustomProperties(tutorialProps);
                    GorillaBotPlugin.Log.LogInfo(
                        $"[{_name}] Metro 253 tutorial-status properties sent (didTutorial=True).");
                }
                catch (Exception ex)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] Metro 253 tutorial-status update failed: {ex.Message}");
                }

                yield return new WaitForSeconds(0.12f);
            }

            yield return SpawnPlayerControllerSequence();
        }

        private IEnumerator SpawnPlayerControllerSequence()
        {
            // Match the timing of the game's post-join player-spawn path.
            yield return new WaitForSeconds(0.25f);

            if (!_inRoom || _photonClient == null || !_photonClient.InRoom)
                yield break;

            ComputeRigPosition();
            UpdateFormationPosition(true);

            int actorNr = 0;
            try
            {
                actorNr = _photonClient.LocalPlayer?.ActorNumber ?? 0;
            }
            catch { }

            if (actorNr <= 0)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] Invalid actor number; player spawn cancelled.");
                yield break;
            }

            // PUN's normal owner ViewID allocation.
            _botRigViewId = actorNr * 1000 + 1;

            GorillaBotPlugin.Log.LogInfo(
                $"[{_name}] Spawning Player Network Controller " +
                $"actor={actorNr} viewId={_botRigViewId}");

            if (!RaisePlayerNetworkControllerInstantiate())
            {
                GorillaBotPlugin.Log.LogError(
                    $"[{_name}] Player Network Controller instantiate failed.");
                yield break;
            }

            _botRigSpawnSent = true;

            if (_useMetro2024)
            {
                // Match the reference order:
                // 253 -> 200 -> 253 -> 202 -> 201 -> 206 -> 201 -> 200 -> 201...
                // 255 is server-generated and is not raised by the bot.
                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Metro 2024 player-create sequence: 253 -> 200 -> 253 -> 202 -> 201 -> 206 -> 201 -> 200 -> 201...");

                // Initial VRRig serialization immediately after 202.
                SendFixedRigTransform();
                yield return new WaitForSeconds(0.04f);

                // Reliable serialization event (206), matching the second network
                // serialization stage in the supplied trace.
                SendReliable206();
                yield return new WaitForSeconds(0.04f);

                // 200 is the normal PUN/RPC state channel. Send the bot's cosmetics
                // using the existing game-shaped RPC, rather than a dummy event.
                SendCosmeticsToOthers();
                yield return new WaitForSeconds(0.08f);
            }

            // Give the host time to process the startup packets and let VRRigSerializer
            // bind the spawned PhotonView to its pooled RigContainer.
            yield return new WaitForSeconds(0.55f);

            GorillaBotPlugin.Log.LogInfo(
                $"[{_name}] Player Network Controller spawn sent; starting pose synchronization.");

            SendFixedRigTransform();

            GorillaBotPlugin.Instance.StartCoroutine(
                ApplyBotVisualsWhenReady());

            if (!_useMetro2024)
                SendCosmeticsToOthers();
        }

        // The supplied Metro trace contains event 202 for the actual Photon
        // Instantiate. The existing method below already builds the dynamic ViewID
        // array required by this prefab. Event 255 is a server-generated Join event,
        // not a client-created event, so Metro mode changes the room-entry properties
        // instead of incorrectly trying to raise 255.

        private bool RaisePlayerNetworkControllerInstantiate()
        {
            if (_photonClient == null || !_photonClient.InRoom)
                return false;

            try
            {
                // This is the prefab that the game's own OnJoinedRoom path logs:
                // "Spawning player" -> "Net instantiate: Player Network Controller".
                const string prefab = "Player Network Controller";

                int actorNr = _photonClient.LocalPlayer?.ActorNumber ?? 0;
                if (actorNr <= 0)
                    return false;

                int rootViewId = actorNr * 1000 + 1;

                // PUN requires one ViewID for EVERY PhotonView on the instantiated prefab.
                // The previous version sent only one ID, which causes NetworkInstantiate
                // to index past the array when Player Network Controller contains more
                // than one PhotonView. Resolve the prefab from the active PUN prefab pool
                // first; fall back to Resources if necessary.
                int viewCount = GetNetworkPrefabPhotonViewCount(prefab);
                if (viewCount <= 0)
                {
                    GorillaBotPlugin.Log.LogError(
                        $"[{_name}] Could not resolve PhotonView count for '{prefab}'.");
                    return false;
                }

                int[] viewIds = new int[viewCount];
                for (int i = 0; i < viewCount; i++)
                    viewIds[i] = rootViewId + i;

                var data = new Hashtable
                {
                    [(byte)0] = prefab,
                    [(byte)1] = _rigPosition,
                    [(byte)2] = Quaternion.Euler(0f, _rigYaw, 0f),
                    [(byte)4] = viewIds,
                    [(byte)6] = GetBotServerTimestamp(),
                    [(byte)7] = rootViewId
                };

                byte prefix = GetHostLevelPrefix();
                if (prefix != 0)
                    data[(byte)8] = prefix;

                var raiseOptions = new RaiseEventOptions
                {
                    Receivers = ReceiverGroup.Others,
                    CachingOption = EventCaching.AddToRoomCache
                };

                var sendOptions =
                    new SendOptions { Reliability = true };

                bool ok = _photonClient.OpRaiseEvent(
                    202,
                    data,
                    raiseOptions,
                    sendOptions);

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] PUN 202 Player Controller sent: " +
                    $"prefab='{prefab}' actor={actorNr} " +
                    $"rootViewId={rootViewId} viewCount={viewIds.Length} " +
                    $"timestamp={data[(byte)6]} ok={ok}");

                return ok;
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogError(
                    $"[{_name}] RaisePlayerNetworkControllerInstantiate failed:\n{ex}");
                return false;
            }
        }

        private void ComputeRigPosition()
        {
            if (_useCustomFormationPosition)
            {
                _rigPosition = _customFormationPosition;
                _rigYaw = Mathf.RoundToInt(_customFormationRotation.eulerAngles.y);
            }
            else
            {
                // Requested forest base position: basePos + (-63.601, 3.299, -63.485)
                // with a small +/-1 random offset on each axis. Keep this per-bot
                // position stable until a new room/spawn is created.
                Vector3 basePos = Vector3.zero;
                _forestSpawnPosition = basePos + new Vector3(
                    -63.601f + UnityEngine.Random.Range(-1f, 1f),
                    3.299f + UnityEngine.Random.Range(-1f, 1f),
                    -63.485f + UnityEngine.Random.Range(-1f, 1f));
                _forestSpawnPositionSet = true;
                _rigPosition = _forestSpawnPosition;
                _rigYaw = UnityEngine.Random.Range(0, 360);
            }

            _rigPositionSet = true;

            GorillaBotPlugin.Log.LogInfo(
                $"[{_name}] Rig position = {_rigPosition}");
        }

        private static int GetNetworkPrefabPhotonViewCount(string prefabId)
        {
            // 1) Ask the active PUN prefab pool. This is the most reliable source because
            // it is the exact prefab PUN will instantiate for this ID.
            try
            {
                object pool = PhotonNetwork.PrefabPool;
                if (pool != null)
                {
                    Type poolType = pool.GetType();
                    FieldInfo dictField = poolType.GetField(
                        "networkPrefabs",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    object dictionary = dictField?.GetValue(pool);
                    if (dictionary is System.Collections.IDictionary map && map.Contains(prefabId))
                    {
                        GameObject pooledPrefab = map[prefabId] as GameObject;
                        if (pooledPrefab != null)
                        {
                            PhotonView[] views = pooledPrefab.GetComponentsInChildren<PhotonView>(true);
                            if (views != null && views.Length > 0)
                                return views.Length;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[Rig] PrefabPool PhotonView count lookup failed: {ex.Message}");
            }

            // 2) Fall back to the normal Resources path used by this game.
            string[] resourceCandidates =
            {
                "GorillaPrefabs/" + prefabId,
                prefabId
            };

            foreach (string path in resourceCandidates)
            {
                try
                {
                    GameObject prefab = Resources.Load<GameObject>(path);
                    if (prefab == null)
                        continue;

                    PhotonView[] views =
                        prefab.GetComponentsInChildren<PhotonView>(true);

                    if (views != null && views.Length > 0)
                        return views.Length;
                }
                catch (Exception ex)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[Rig] Resources prefab lookup '{path}': {ex.Message}");
                }
            }

            return 0;
        }



        public void ConfigureFormation(string mode, int slot, int total)
        {
            if (!Enum.TryParse(mode, true, out FormationMode parsed))
                parsed = FormationMode.Down;

            _formationMode = parsed;
            _formationSlot = Mathf.Max(0, slot);
            _formationTotal = Mathf.Max(1, total);
            _formationPhase = 0f;

            // Immediately place the bot at the requested formation position.
            UpdateFormationPosition(true);

            GorillaBotPlugin.Log.LogInfo(
                $"[{_name}] Formation={_formationMode} slot={_formationSlot + 1}/{_formationTotal}");
        }

        public void SetFormationSpeed(float speed)
        {
            _formationSpeed = Mathf.Clamp(speed, 0.05f, 10f);

            UpdateFormationPosition(true);
        }

        private bool _useCustomFormationPosition;
        private Vector3 _customFormationPosition;
        private Quaternion _customFormationRotation;

        public void SetCustomFormationPosition(
            Vector3 position,
            Quaternion rotation,
            int slot,
            int total)
        {
            _useCustomFormationPosition = true;

            _customFormationPosition = position;
            _customFormationRotation = rotation;

            _formationSlot = Mathf.Max(0, slot);
            _formationTotal = Mathf.Max(1, total);

            _rigPosition = position;
            _rigYaw = Mathf.RoundToInt(rotation.eulerAngles.y);
            _rigPositionSet = true;

            GorillaBotPlugin.Log.LogInfo(
                $"[{_name}] Custom formation position set to " +
                $"{position} yaw={_rigYaw}"
            );
        }

        private Vector3 GetFormationPosition()
        {
            Vector3 center =
                _useCustomFormationPosition
                    ? _customFormationPosition
                    : (_forestSpawnPositionSet ? _forestSpawnPosition : ForestCenter);


            float total = Mathf.Max(1, _formationTotal);
            float slot = _formationSlot;
            float t = Time.time * _formationSpeed + _formationPhase;
            float radius = Mathf.Max(1.0f, _formationRadius);

            switch (_formationMode)
            {
                case FormationMode.Down:
                    {
                        // DOWN is intentionally deterministic.  Do not add a
                        // random offset here; only TELEPORTER gets random spawn
                        // positions.  Keep multiple bots separated so they do
                        // not occupy the exact same point.
                        int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(total)));
                        int row = _formationSlot / columns;
                        int col = _formationSlot % columns;
                        float spacing = Mathf.Max(1.25f, radius * 0.75f);
                        float rows = Mathf.Ceil(total / columns);
                        float x = (col - (columns - 1f) * 0.5f) * spacing;
                        float z = (row - (rows - 1f) * 0.5f) * spacing;
                        return center + new Vector3(x, 0f, z);
                    }

                case FormationMode.Bounce:
                    {
                        float s = (Mathf.Sin(t) + 1f) * 0.5f;
                        return center + Vector3.up * (s * _formationHeight);
                    }

                case FormationMode.Circle:
                    {
                        float angle = (slot / total) * Mathf.PI * 2f + t;
                        return center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    }

                case FormationMode.Square:
                    {
                        float half = radius;
                        float perimeter = half * 8f;
                        float d = Mathf.Repeat((slot / total) * perimeter + t * radius, perimeter);
                        float side = half * 2f;
                        if (d < side) return center + new Vector3(-half + d, 0f, -half);
                        d -= side;
                        if (d < side) return center + new Vector3(half, 0f, -half + d);
                        d -= side;
                        if (d < side) return center + new Vector3(half - d, 0f, half);
                        d -= side;
                        return center + new Vector3(-half, 0f, half - d);
                    }

                case FormationMode.Wave:
                    {
                        float spacing = Mathf.Max(0.9f, radius * 0.55f);
                        float x = (slot - (total - 1f) * 0.5f) * spacing;
                        float z = Mathf.Sin(t + slot * 0.55f) * radius * 0.5f;
                        return center + new Vector3(x, 0f, z);
                    }

                case FormationMode.Line:
                    {
                        float spacing = Mathf.Max(0.9f, radius * 0.55f);
                        float x = (slot - (total - 1f) * 0.5f) * spacing;
                        return center + new Vector3(x, 0f, 0f);
                    }

                case FormationMode.Triangle:
                    {
                        int rows = Mathf.Max(1, Mathf.CeilToInt((Mathf.Sqrt(8f * total + 1f) - 1f) * 0.5f));
                        int row = 0, first = 0;
                        while (row + 1 < rows && first + row + 1 <= _formationSlot)
                        {
                            first += row + 1;
                            row++;
                        }
                        int col = _formationSlot - first;
                        float x = (col - row * 0.5f) * radius;
                        float z = row * radius * 0.8f;
                        return center + new Vector3(x, 0f, z);
                    }

                case FormationMode.Diamond:
                    {
                        float angle = (slot / total) * Mathf.PI * 2f + t * 0.5f;
                        float c = Mathf.Cos(angle), sn = Mathf.Sin(angle);
                        float scale = radius / Mathf.Max(0.001f, Mathf.Abs(c) + Mathf.Abs(sn));
                        return center + new Vector3(c * scale, 0f, sn * scale);
                    }

                case FormationMode.Grid:
                    {
                        int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(total)));
                        int row = _formationSlot / columns;
                        int col = _formationSlot % columns;
                        float spacing = Mathf.Max(1.0f, radius * 0.6f);
                        float x = (col - (columns - 1) * 0.5f) * spacing;
                        float z = (row - (Mathf.Ceil(total / columns) - 1f) * 0.5f) * spacing;
                        return center + new Vector3(x, 0f, z);
                    }

                case FormationMode.Spiral:
                    {
                        float angle = slot * 0.9f + t * 0.7f;
                        float r = Mathf.Max(0.5f, radius * (0.25f + slot / Mathf.Max(1f, total) * 0.9f));
                        return center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                    }

                case FormationMode.Helix:
                    {
                        float angle = (slot / total) * Mathf.PI * 4f + t * 0.75f;
                        float r = radius * (0.5f + 0.5f * (slot / total));
                        float y = Mathf.Sin(angle * 0.5f) * _formationHeight;
                        return center + new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
                    }

                case FormationMode.Spaz:
                    {
                        // Small high-frequency body jitter. Head/hand jitter is applied separately
                        // in SendFixedRigTransform so the entire rig looks unstable, not just the body.
                        float amp = Mathf.Max(0.6f, radius * 0.35f);
                        float x = (Mathf.PerlinNoise(t * 3.7f + slot, 0.2f) - 0.5f) * amp * 2f;
                        float z = (Mathf.PerlinNoise(0.4f, t * 4.1f + slot) - 0.5f) * amp * 2f;
                        float y = (Mathf.PerlinNoise(t * 4.7f + slot, t * 2.3f) - 0.5f) * Mathf.Max(0.5f, _formationHeight * 0.5f);
                        return center + new Vector3(x, y, z);
                    }

                case FormationMode.Teleporter:
                    {
                        if (Time.time >= _teleporterTimer)
                            GenerateTeleporterPose();

                        return _teleporterPosition;
                    }
            }

            return center;
        }

        private void UpdateFormationPosition(bool force)
        {
            if (!force &&
                Time.frameCount % 3 != 0)
                return;

            _rigPosition = GetFormationPosition();
            _rigPositionSet = true;
        }

        private static byte GetHostLevelPrefix()
        {
            try
            {
                FieldInfo field = typeof(PhotonNetwork).GetField(
                    "currentLevelPrefix", BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null) return Convert.ToByte(field.GetValue(null));
            }
            catch { }
            return 0;
        }

        private int GetBotServerTimestamp()
        {
            try
            {
                PropertyInfo p = _photonClient?.GetType().GetProperty(
                    "ServerTimestamp", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null)
                {
                    object value = p.GetValue(_photonClient, null);
                    if (value is int i) return i;
                }
            }
            catch { }
            return Environment.TickCount;
        }

        [StructLayout(LayoutKind.Explicit, Size = 136)]
        private struct PackedRigState
        {
            [FieldOffset(0)] public Quaternion headRotation;
            [FieldOffset(16)] public Vector3 rightHandPosition;
            [FieldOffset(28)] public Quaternion rightHandRotation;
            [FieldOffset(44)] public Vector3 leftHandPosition;
            [FieldOffset(56)] public Quaternion leftHandRotation;
            [FieldOffset(72)] public Vector3 position;
            [FieldOffset(84)] public int roundedRotation;
            [FieldOffset(88)] public int handPosition;
            [FieldOffset(92)] public int state;
            [FieldOffset(96)] public int grabbedRopeIndex;
            [FieldOffset(100)] public int ropeBoneIndex;
            [FieldOffset(104)] public bool ropeGrabIsLeft;
            [FieldOffset(108)] public Vector3 ropeGrabOffset;
            [FieldOffset(120)] public double serverTimeStamp;
            [FieldOffset(128)] public bool remoteUseReplacementVoice;
            [FieldOffset(132)] public float speakingLoudness;
        }

        private object[] SerializePackedRigState(
            Vector3 position,
            int yaw,
            Quaternion headRotation,
            Vector3 rightHandPosition,
            Quaternion rightHandRotation,
            Vector3 leftHandPosition,
            Quaternion leftHandRotation)
        {
            // Metro 2024 VRRig does NOT receive a marshalled byte buffer here.
            // Its Photon reader explicitly casts the stream values to:
            // int, long, long, long, int, int, [rope fields], float.
            // Sending byte-by-byte data was the cause of the InvalidCastException.
            int packedHead = BotPackQuaternionForNetwork(headRotation);
            long packedRight = BotPackHandPosRotForNetwork(rightHandPosition, rightHandRotation);
            long packedLeft = BotPackHandPosRotForNetwork(leftHandPosition, leftHandRotation);
            long packedWorld = BotPackWorldPosForNetwork(position);

            int packedFlags = Mathf.Clamp(Mathf.RoundToInt(yaw + 360f) % 360, 0, 360);
            // No replacement voice, no rope, zero loudness.
            int packedVoiceFlags = packedFlags;

            // Exact non-rope VRRig stream: 7 values. The surrounding PUN
            // serialization envelope adds the view id, producing the 10-item
            // payload seen in the Metro 2024 trace.
            return new object[]
            {
                _botRigViewId,
                false,
                null,
                packedHead,
                packedRight,
                packedLeft,
                packedWorld,
                0,
                packedVoiceFlags,
                1f
            };
        }

        private static int BotPackQuaternionForNetwork(Quaternion q)
        {
            q.Normalize();
            float ax = Mathf.Abs(q.x);
            float ay = Mathf.Abs(q.y);
            float az = Mathf.Abs(q.z);
            float aw = Mathf.Abs(q.w);
            float largest = ax;
            int axis = 0; // X

            if (ay > largest) { largest = ay; axis = 1; }
            if (az > largest) { largest = az; axis = 2; }
            if (aw > largest) { axis = 3; }

            bool negative;
            float a, b, c;
            switch (axis)
            {
                case 0: negative = q.x < 0f; a = q.y; b = q.z; c = q.w; break;
                case 1: negative = q.y < 0f; a = q.x; b = q.z; c = q.w; break;
                case 2: negative = q.z < 0f; a = q.x; b = q.y; c = q.w; break;
                default: negative = q.w < 0f; a = q.x; b = q.y; c = q.z; break;
            }

            if (negative) { a = -a; b = -b; c = -c; }

            int p0 = Mathf.Clamp(Mathf.RoundToInt((a + 0.707107f) * 361.33145f), 0, 511);
            int p1 = Mathf.Clamp(Mathf.RoundToInt((b + 0.707107f) * 361.33145f), 0, 511);
            int p2 = Mathf.Clamp(Mathf.RoundToInt((c + 0.707107f) * 361.33145f), 0, 511);
            return p0 + (p1 << 9) + (p2 << 18) + (axis << 27);
        }

        private static long BotPackHandPosRotForNetwork(Vector3 localPos, Quaternion rot)
        {
            long x = Mathf.Clamp(Mathf.RoundToInt(localPos.x * 512f) + 1024, 0, 2047);
            long y = Mathf.Clamp(Mathf.RoundToInt(localPos.y * 512f) + 1024, 0, 2047);
            long z = Mathf.Clamp(Mathf.RoundToInt(localPos.z * 512f) + 1024, 0, 2047);
            long r = (long)BotPackQuaternionForNetwork(rot);
            return x + (y << 11) + (z << 22) + (r << 33);
        }

        private static long BotPackWorldPosForNetwork(Vector3 worldPos)
        {
            long x = Mathf.Clamp(Mathf.RoundToInt(worldPos.x * 1024f) + 1048576, 0, 2097151);
            long y = Mathf.Clamp(Mathf.RoundToInt(worldPos.y * 1024f) + 1048576, 0, 2097151);
            long z = Mathf.Clamp(Mathf.RoundToInt(worldPos.z * 1024f) + 1048576, 0, 2097151);
            return x + (y << 21) + (z << 42);
        }

        private void SendReliable206()
        {
            if (_photonClient == null || !_photonClient.InRoom || _botRigViewId <= 0)
                return;

            try
            {
                // The Metro trace shows 206 as the reliable serialization path.
                // The Player Network Controller advertises a second PhotonView
                // (rootViewId + 1), so use that view when it exists.
                int reliableViewId = _botRigViewId + 1;
                object[] payload = new object[]
                {
                    reliableViewId,
                    false,
                    null,
                    -65504,
                    0,
                    -1,
                    -1,
                    1,
                    0
                };

                object[] eventData = new object[]
                {
                    GetBotServerTimestamp(),
                    null,
                    payload
                };

                bool ok = _photonClient.OpRaiseEvent(
                    206,
                    eventData,
                    new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                    new SendOptions { Reliability = true });

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Sent Metro 206: viewId={reliableViewId} payloadItems={payload.Length} ok={ok}");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning($"[{_name}] SendReliable206 failed: {ex}");
            }
        }

        private void SendFixedRigTransform()
        {
            UpdateFormationPosition(false);

            if (!_botRigSpawnSent ||
                !_rigPositionSet ||
                _botRigViewId <= 0 ||
                _photonClient == null ||
                !_photonClient.InRoom)
                return;

            try
            {
                /*
                 * IMPORTANT:
                 *
                 * The game's VRRigSerializer does NOT put Quaternion/Vector3
                 * objects directly into event 201.
                 *
                 * VRRig.OnSerializeWrite() returns InputStruct, and
                 * GorillaWrappedSerializer -> NetCrossoverUtils writes:
                 *
                 *   SendNext(136)
                 *   SendNext(byte[0])
                 *   ...
                 *   SendNext(byte[135])
                 *
                 * PUN therefore receives:
                 *
                 *   [ViewID, false, null, 136, <136 bytes>]
                 *
                 * which is the 140-element payload observed in the
                 * working packet.
                 */

                Quaternion headRotation = Quaternion.identity;
                Quaternion rightHandRotation = Quaternion.identity;
                Quaternion leftHandRotation = Quaternion.identity;

                Vector3 headPos =
                    _rigPosition + new Vector3(0f, 0.6f, 0f);

                // VRRig.OnSerializeWrite() stores hand positions from
                // rightHand.rigTarget.localPosition / leftHand.rigTarget.localPosition.
                // Send LOCAL positions here; sending world positions makes the
                // arms appear detached/behind the body on remote rigs.
                Vector3 rightHand = new Vector3(0.22f, 0f, 0f);
                Vector3 leftHand = new Vector3(-0.22f, 0f, 0f);

                if (_formationMode == FormationMode.Teleporter)
                {
                    if (Time.time >= _teleporterTimer)
                        GenerateTeleporterPose();

                    headPos = _rigPosition + new Vector3(
                        UnityEngine.Random.Range(-0.15f, 0.15f),
                        UnityEngine.Random.Range(0.45f, 1.35f),
                        UnityEngine.Random.Range(-0.15f, 0.15f));

                    rightHand = _teleporterRightHandOffset;
                    leftHand = _teleporterLeftHandOffset;

                    headRotation = _teleporterHeadRotation;
                    rightHandRotation = _teleporterRightHandRotation;
                    leftHandRotation = _teleporterLeftHandRotation;
                }

                if (_formationMode == FormationMode.Spaz)
                {
                    float t = Time.time * Mathf.Max(0.25f, _formationSpeed) * 4f + _formationSlot;
                    float a = Mathf.Sin(t * 1.71f);
                    float b = Mathf.Cos(t * 2.37f);
                    float c = Mathf.Sin(t * 3.11f);
                    float amp = Mathf.Max(0.12f, _formationRadius * 0.14f);

                    Vector3 jitter = new Vector3(
                        a * amp,
                        b * amp * 0.65f,
                        c * amp);

                    Vector3 center = _useCustomFormationPosition
                        ? _customFormationPosition
                        : (_forestSpawnPositionSet ? _forestSpawnPosition : ForestCenter);
                    _rigPosition = center + jitter;
                    headPos = _rigPosition + new Vector3(0f, 0.6f, 0f) + jitter * 0.8f;
                    rightHand = new Vector3(0.22f, 0.02f, 0f) + new Vector3(b, c, a) * amp * 1.8f;
                    leftHand = new Vector3(-0.22f, 0.02f, 0f) + new Vector3(c, a, b) * amp * 1.8f;

                    headRotation = Quaternion.Euler(a * 75f, c * 120f, b * 60f);
                    rightHandRotation = Quaternion.Euler(b * 120f, a * 160f, c * 100f);
                    leftHandRotation = Quaternion.Euler(c * 120f, b * 160f, a * 100f);
                }

                object[] serialized =
                    SerializePackedRigState(
                        _rigPosition,
                        _rigYaw,
                        headRotation,
                        rightHand,
                        rightHandRotation,
                        leftHand,
                        leftHandRotation);

                // SerializePackedRigState returns the exact typed payload expected
                // by the Metro 2024 VRRig reader. Do not turn it into individual bytes.
                object[] viewData = serialized;

                object[] eventData =
                    new object[3];

                eventData[0] =
                    GetBotServerTimestamp();

                eventData[1] = null;
                eventData[2] = viewData;

                var options =
                    new RaiseEventOptions
                    {
                        Receivers =
                            ReceiverGroup.Others
                    };

                var sendOptions =
                    new SendOptions
                    {
                        Reliability = false
                    };

                bool ok =
                    _photonClient.OpRaiseEvent(
                        201,
                        eventData,
                        options,
                        sendOptions);

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Sent packed 201: " +
                    $"viewId={_botRigViewId} " +
                    $"payloadItems={viewData.Length} " +
                    $"ok={ok}");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] SendFixedRigTransform: {ex}");
            }
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            GorillaBotPlugin.Log.LogWarning($"[{_name}] JoinRoom failed ({returnCode}): {message}");
            if (returnCode == ErrorCode.GameFull) { _connecting = false; return; }
            if (_searchingRegions && !_allowCreate && returnCode == ErrorCode.GameDoesNotExist)
            { TryNextRegionOrCreate(); return; }
            if (_allowCreate) DoJoinOrCreate();
            else TryNextRegionOrCreate();
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            GorillaBotPlugin.Log.LogWarning($"[{_name}] JoinRandom failed: {message} — creating");
            CreatePublicRoom();
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            GorillaBotPlugin.Log.LogError($"[{_name}] CreateRoom failed ({returnCode}): {message}");
        }

        public void OnCreatedRoom()
        {
            GorillaBotPlugin.Log.LogInfo($"[{_name}] Created room");
        }

        public void OnPreLeavingRoom() { }

        public void OnLeftRoom()
        {
            _inRoom = false;
            _botRigSpawnSent = false;
            _botRigViewId = 0;
            _rigPositionSet = false;
            _forestSpawnPositionSet = false;
            _cachedBotRig = null;
            _nextRigLookupTime = 0f;
            TeardownPhotonVoice();
            GorillaBotPlugin.Log.LogInfo($"[{_name}] Left room");
        }

        public void OnFriendListUpdate(List<FriendInfo> friendList) { }
        public void OnRoomListUpdate(List<RoomInfo> roomList) { }

        public void Pump()
        {
            try
            {
                if (_photonClient != null && _photonClient.IsConnected)
                    _photonClient.Service();

                if (_voiceTransport != null && _voiceTransport.IsConnected)
                    _voiceTransport.Service();

                if (_botRigSpawnSent && _rigPositionSet && Time.time - _lastRigSendTime >= 0.0333f)
                {
                    _lastRigSendTime = Time.time;
                    SendFixedRigTransform();
                }
            }
            catch { }
        }

        public void Disconnect()
        {
            StopAudio();
            TeardownPhotonVoice();
            try
            {
                if (_photonClient != null)
                {
                    try { _photonClient.RemoveCallbackTarget(this); } catch { }
                    if (_photonClient.IsConnected) _photonClient.Disconnect();
                }
            }
            catch { }
            _connected = false;
            _inRoom = false;
            _botRigSpawnSent = false;
            _botRigViewId = 0;
            _rigPositionSet = false;
            _formationMode = FormationMode.Down;
            GorillaBotPlugin.Log.LogInfo($"[{_name}] Disconnected");
        }

        private void SetupPhotonVoice()
        {
            try
            {
                if (_photonClient == null || !_inRoom) { _voiceReady = false; return; }
                if (_voiceTransport != null) return;
                ConnectVoiceRoom();
            }
            catch (Exception ex)
            {
                _voiceReady = false;
                GorillaBotPlugin.Log.LogWarning($"[{_name}] Photon Voice setup failed: {ex.Message}");
            }
        }

        private void ConnectVoiceRoom()
        {
            try
            {
                if (_photonClient == null || !_inRoom || _voiceTransport != null) return;
                string voiceRoom = _currentRoom;
                string region = string.IsNullOrEmpty(_currentRegion) ? "usw" : _currentRegion;

                _voiceTransport = new LoadBalancingTransport2(new VoiceLog("voice"), ConnectionProtocol.Udp);
                _voiceTransport.AppId = "9cb5d492-bcbc-440d-af40-5ad5a3d12fc9";
                _voiceTransport.AppVersion = ResolveAppVersion();
                _voiceTransport.NameServerHost = "ns.exitgames.com";
                _voiceTransport.AddCallbackTarget(new VoiceRoomCallbacks(this, _currentRoom, region));

                if (!string.IsNullOrEmpty(_savedPfid))
                {
                    var auth = new AuthenticationValues(Guid.NewGuid().ToString("N"));
                    auth.AuthType = CustomAuthenticationType.Custom;
                    auth.AddAuthParameter("username", _savedPfid);
                    auth.AddAuthParameter("token", "");
                    auth.SetAuthPostData(new Dictionary<string, object>
                    {
                        { "AppId", "29E62" },
                        { "AppVersion", _voiceTransport.AppVersion },
                        { "Ticket", _savedTicket ?? "" },
                        { "Token", "" },
                        { "Nonce", _savedNonce ?? "" }
                    });
                    _voiceTransport.AuthValues = auth;
                }
                _voiceTransport.NickName = _name + "_voice";

                GorillaBotPlugin.Log.LogInfo($"[{_name}] Connecting voice to {region} for room {voiceRoom}");
                if (!_voiceTransport.ConnectToRegionMaster(region))
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] Voice ConnectToRegionMaster failed");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning($"[{_name}] ConnectVoiceRoom: {ex.Message}");
            }
        }

        private void OnVoiceRoomJoined()
        {
            _voiceRoomJoined = true;
            _voiceClient = _voiceTransport?.VoiceClient;
            _voiceReady = _voiceClient != null;
            _voiceCreateFallbackAttempted = false;
            ConfigureRemoteVoiceDecoders();
            GorillaBotPlugin.Log.LogInfo($"[{_name}] Voice room joined, ready={_voiceReady}");
            if (_voiceReady)
                GorillaBotPlugin.Instance.StartCoroutine(StartSystemMicrophoneWhenReady());
        }

        private void ConfigureRemoteVoiceDecoders()
        {
            try
            {
                if (_voiceClient == null) return;

                // The log "decoder is null" means the incoming Photon Voice stream
                // reached this client without RemoteVoiceOptions.SetOutput/Decoder.
                // SetOutput creates the default decoder for supported audio streams.

                // OnRemoteVoiceInfoAction uses a ref RemoteVoiceOptions parameter,
                // so the strongly typed delegate above cannot be assigned directly
                // on every Voice API version. Build the exact delegate with reflection.
                PropertyInfo prop = _voiceClient.GetType().GetProperty(
                    "OnRemoteVoiceInfoAction",
                    BindingFlags.Public | BindingFlags.Instance);

                if (prop == null || !prop.CanWrite)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] Voice client has no writable OnRemoteVoiceInfoAction.");
                    return;
                }

                MethodInfo callbackMethod = GetType().GetMethod(
                    nameof(RemoteVoiceInfoDecoderCallback),
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (callbackMethod == null) return;

                Delegate callback = Delegate.CreateDelegate(prop.PropertyType, this, callbackMethod);
                prop.SetValue(_voiceClient, callback, null);

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Remote Photon Voice decoder callback installed.");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] ConfigureRemoteVoiceDecoders: {ex.Message}");
            }
        }

        private void RemoteVoiceInfoDecoderCallback(
            int channelId,
            int playerId,
            byte voiceId,
            VoiceInfo voiceInfo,
            ref RemoteVoiceOptions options)
        {
            try
            {
                if (voiceInfo.Codec == Codec.AudioOpus)
                {
                    options.SetOutput((FrameOut<float> frame) => { });
                }
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] Remote voice decoder callback failed: {ex.Message}");
            }
        }

        private class VoiceRoomCallbacks : IConnectionCallbacks, IMatchmakingCallbacks
        {
            private readonly BotInstance _bot;
            private readonly string _voiceRoom;
            private readonly string _region;

            public VoiceRoomCallbacks(BotInstance bot, string voiceRoom, string region)
            {
                _bot = bot; _voiceRoom = voiceRoom; _region = region;
            }

            public void OnConnected() { }
            public void OnConnectedToMaster()
            {
                GorillaBotPlugin.Log.LogInfo($"[{_bot._name}] Voice connected, joining room {_voiceRoom}");
                _bot._voiceTransport.OpJoinRoom(new EnterRoomParams { RoomName = _voiceRoom });
            }
            public void OnDisconnected(DisconnectCause cause)
            {
                GorillaBotPlugin.Log.LogWarning($"[{_bot._name}] Voice disconnected: {cause}");
            }
            public void OnRegionListReceived(RegionHandler regionHandler) { }
            public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
            public void OnCustomAuthenticationFailed(string debugMessage)
            {
                GorillaBotPlugin.Log.LogError($"[{_bot._name}] Voice auth failed: {debugMessage}");
            }
            public void OnFriendListUpdate(List<FriendInfo> friendList) { }
            public void OnCreatedRoom() { }
            public void OnCreateRoomFailed(short returnCode, string message) { }
            public void OnJoinedRoom() { _bot.OnVoiceRoomJoined(); }
            public void OnJoinRoomFailed(short returnCode, string message)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_bot._name}] Voice room join failed ({returnCode}): {message}");

                // Photon Voice can return GameDoesNotExist when nobody has created
                // the voice room yet. For the fan-game bot transport, create the
                // same-named voice room as a fallback.
                if (returnCode == ErrorCode.GameDoesNotExist &&
                    !_bot._voiceCreateFallbackAttempted &&
                    _bot._voiceTransport != null)
                {
                    _bot._voiceCreateFallbackAttempted = true;

                    try
                    {
                        var create = new EnterRoomParams
                        {
                            RoomName = _voiceRoom,
                            RoomOptions = new RoomOptions
                            {
                                IsVisible = false,
                                IsOpen = true,
                                MaxPlayers = 20
                            }
                        };

                        bool ok = _bot._voiceTransport.OpCreateRoom(create);
                        GorillaBotPlugin.Log.LogInfo(
                            $"[{_bot._name}] Voice room did not exist; create fallback sent ok={ok}");
                    }
                    catch (Exception ex)
                    {
                        GorillaBotPlugin.Log.LogWarning(
                            $"[{_bot._name}] Voice create fallback failed: {ex.Message}");
                    }
                }
            }
            public void OnJoinRandomFailed(short returnCode, string message) { }
            public void OnLeftRoom() { }
            public void OnPreLeavingRoom() { }
            public void OnRoomListUpdate(List<RoomInfo> roomList) { }
        }

        private void TeardownPhotonVoice()
        {
            try
            {
                StopSystemMicrophone();

                if (_localVoice != null)
                {
                    try { _localVoice.RemoveSelf(); } catch { }
                    try { _localVoice.Dispose(); } catch { }
                    _localVoice = null;
                }
                if (_voiceTransport != null)
                {
                    try { _voiceTransport.RemoveCallbackTarget(this); } catch { }
                    try { if (_voiceTransport.IsConnected) _voiceTransport.Disconnect(); } catch { }
                    _voiceTransport = null;
                }
                _voiceClient = null;
                _voiceReady = false;
                _voiceRoomJoined = false;
                _voiceJoinAttempted = false;
                _voiceCreateFallbackAttempted = false;
            }
            catch { }
        }

        private IEnumerator StartSystemMicrophoneWhenReady()
        {
            // Bots do not automatically capture the host microphone.
            // This prevents the Photon Voice AudioOpus producer from
            // continuously filling its PushData queue.
            _microphoneActive = false;

            GorillaBotPlugin.Log.LogInfo(
                $"[{_name}] Automatic microphone capture disabled.");

            yield break;
        }

        private void StopSystemMicrophone()
        {
            _microphoneActive = false;
            _microphoneDevice = null;
            _microphoneClip = null;
        }

        public void PlayMp3(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            path = path.Trim().Trim('"').Trim('\'');
            if (!File.Exists(path)) { GorillaBotPlugin.Log.LogWarning($"[{_name}] MP3 not found: {path}"); return; }
            if (!_inRoom) { GorillaBotPlugin.Log.LogWarning($"[{_name}] Join a room first"); return; }
            if (!_voiceReady)
            {
                SetupPhotonVoice();
                GorillaBotPlugin.Instance.StartCoroutine(WaitForVoiceAndPlayCoroutine(path));
                return;
            }
            GorillaBotPlugin.Instance.StartCoroutine(PlayMp3AsVoiceCoroutine(path));
        }

        private System.Collections.IEnumerator WaitForVoiceAndPlayCoroutine(string path)
        {
            float timeout = 15f;

            while (!_voiceReady && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_voiceReady)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] Voice was not ready after waiting. " +
                    "Check that the Voice AppId/region matches the fan-game voice backend.");
                yield break;
            }

            yield return PlayMp3AsVoiceCoroutine(path);
        }

        private System.Collections.IEnumerator PlayMp3AsVoiceCoroutine(string path)
        {
            object clip = null;
            string fail = null;
            object req = null;
            object op = null;
            Type dhType = null;

            try
            {
                string url = "file:///" + path.Replace("\\", "/");
                var uwrType = Type.GetType("UnityEngine.Networking.UnityWebRequestMultimedia, UnityEngine.UnityWebRequestAudioModule")
                           ?? Type.GetType("UnityEngine.Networking.UnityWebRequestMultimedia, UnityEngine");
                var audioTypeEnum = Type.GetType("UnityEngine.AudioType, UnityEngine.AudioModule")
                                 ?? Type.GetType("UnityEngine.AudioType, UnityEngine");
                if (uwrType == null || audioTypeEnum == null) { fail = "Audio modules missing"; }
                else
                {
                    var mpeg = Enum.Parse(audioTypeEnum, "MPEG");
                    var getClip = uwrType.GetMethod("GetAudioClip", new[] { typeof(string), typeof(bool), audioTypeEnum })
                               ?? uwrType.GetMethod("GetAudioClip", new[] { typeof(string), audioTypeEnum });
                    if (getClip == null) { fail = "GetAudioClip not found"; }
                    else
                    {
                        req = getClip.GetParameters().Length == 3
                            ? getClip.Invoke(null, new object[] { url, false, mpeg })
                            : getClip.Invoke(null, new object[] { url, mpeg });
                        var send = req.GetType().GetMethod("SendWebRequest", Type.EmptyTypes)
                                ?? req.GetType().GetMethod("Send", Type.EmptyTypes);
                        op = send.Invoke(req, null);
                        dhType = Type.GetType("UnityEngine.Networking.DownloadHandlerAudioClip, UnityEngine.UnityWebRequestAudioModule")
                              ?? Type.GetType("UnityEngine.Networking.DownloadHandlerAudioClip, UnityEngine");
                    }
                }
            }
            catch (Exception ex) { fail = ex.Message; }

            if (fail != null) { GorillaBotPlugin.Log.LogWarning($"[{_name}] MP3 load error: {fail}"); yield break; }

            var isDoneProp = op.GetType().GetProperty("isDone");
            while (!(bool)isDoneProp.GetValue(op)) yield return null;

            try
            {
                var resultProp = req.GetType().GetProperty("result");
                var result = resultProp?.GetValue(req)?.ToString();
                if (result != null && result != "Success")
                {
                    var err = req.GetType().GetProperty("error")?.GetValue(req);
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] MP3 load failed: {err}");
                    yield break;
                }
                var getContent = dhType.GetMethod("GetContent", new[] { req.GetType() });
                clip = getContent.Invoke(null, new[] { req });
                _loadedClip = clip;
                if (clip == null) { GorillaBotPlugin.Log.LogWarning($"[{_name}] MP3 clip null"); yield break; }
                bool ok = StartVoiceFromClip(clip);
                if (ok) GorillaBotPlugin.Log.LogInfo($"[{_name}] Streaming MP3: {path}");
                else GorillaBotPlugin.Log.LogWarning($"[{_name}] MP3 loaded but voice transmit failed");
            }
            catch (Exception ex) { GorillaBotPlugin.Log.LogWarning($"[{_name}] MP3→voice error: {ex.Message}"); }
        }

        private static Type FindVoiceType(string fullName)
        {
            try { var t = typeof(VoiceClient).Assembly.GetType(fullName); if (t != null) return t; } catch { }
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { var t = asm.GetType(fullName); if (t != null) return t; } catch { }
            }
            string shortName = fullName.Contains('.') ? fullName.Substring(fullName.LastIndexOf('.') + 1) : fullName;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in asm.GetTypes())
                        if (t.Name == shortName && (t.Namespace == null || t.Namespace.StartsWith("Photon.Voice")))
                            return t;
                }
                catch { }
            }
            return null;
        }

        private bool StartVoiceFromClip(object audioClip)
        {
            try
            {
                Type wrapperType = Type.GetType("Photon.Voice.Unity.AudioClipWrapper, PhotonVoice.API")
                                ?? Type.GetType("Photon.Voice.Unity.AudioClipWrapper, PhotonVoice");
                if (wrapperType == null)
                {
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] AudioClipWrapper not found");
                    return false;
                }

                object reader = Activator.CreateInstance(wrapperType, new[] { audioClip });
                var loopProp = wrapperType.GetProperty("Loop");
                if (loopProp != null && loopProp.CanWrite)
                    loopProp.SetValue(reader, false);

                return StartVoiceFromReader(reader, "audio clip");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning($"[{_name}] StartVoiceFromClip: {ex.Message}");
                return false;
            }
        }

        private bool StartVoiceFromReader(object reader, string sourceName)
        {
            if (_voiceClient == null)
            {
                SetupPhotonVoice();
                if (_voiceClient == null) return false;
            }

            try
            {
                if (_localVoice != null)
                {
                    try { _localVoice.TransmitEnabled = false; } catch { }
                    try { _localVoice.RemoveSelf(); } catch { }
                    try { _localVoice.Dispose(); } catch { }
                    _localVoice = null;
                }
            }
            catch { }

            try
            {
                Type voiceInfoType = FindVoiceType("Photon.Voice.VoiceInfo");
                if (voiceInfoType == null)
                {
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] VoiceInfo not found");
                    return false;
                }

                Type samplingType = FindVoiceType("Photon.Voice.SamplingRate");
                object sampling16k = null;
                if (samplingType != null)
                {
                    foreach (var name in new[] { "Sampling16000", "SamplingRate16KHz", "Sampling16K" })
                    {
                        try { sampling16k = Enum.Parse(samplingType, name); break; } catch { }
                    }
                    if (sampling16k == null)
                        sampling16k = Enum.ToObject(samplingType, 16000);
                }
                else sampling16k = 16000;

                Type frameType = FindVoiceType("Photon.Voice.FrameDuration");
                object frame20ms = null;
                if (frameType != null)
                {
                    foreach (var name in new[] { "Frame20ms", "FrameDuration20ms" })
                    {
                        try { frame20ms = Enum.Parse(frameType, name); break; } catch { }
                    }
                    if (frame20ms == null)
                        frame20ms = Enum.ToObject(frameType, 20);
                }
                else frame20ms = 20;

                object voiceInfo = null;
                foreach (var m in voiceInfoType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (m.Name != "CreateAudioOpus") continue;
                    var ps = m.GetParameters();
                    if (ps.Length < 4) continue;
                    try
                    {
                        var args = new object[ps.Length];
                        args[0] = sampling16k;
                        args[1] = 1;
                        args[2] = frame20ms;
                        args[3] = 16000;
                        for (int i = 4; i < ps.Length; i++)
                            args[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue :
                                (ps[i].ParameterType.IsValueType ? Activator.CreateInstance(ps[i].ParameterType) : null);

                        voiceInfo = m.Invoke(null, args);
                        if (voiceInfo != null) break;
                    }
                    catch { }
                }

                if (voiceInfo == null)
                {
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] Could not create VoiceInfo");
                    return false;
                }

                Type audioSampleType = FindVoiceType("Photon.Voice.AudioSampleType");
                object sampleFloat = null;
                if (audioSampleType != null)
                {
                    try { sampleFloat = Enum.Parse(audioSampleType, "Float"); }
                    catch { sampleFloat = Enum.ToObject(audioSampleType, 1); }
                }

                // Photon Voice has multiple CreateLocalVoiceAudioFromSource overloads.
                // Do not assume parameter #3 is AudioSampleType: in the version used by
                // this game it can be an Int32 channel ID followed by an IEncoder.
                // The old positional call caused: Int32 -> IEncoder conversion failure.
                MethodInfo createMethod = null;
                object[] args2 = null;

                foreach (var candidate in _voiceClient.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (candidate.Name != "CreateLocalVoiceAudioFromSource") continue;

                    var ps = candidate.GetParameters();
                    if (ps.Length < 2) continue;

                    try
                    {
                        var candidateArgs = new object[ps.Length];
                        bool hasVoiceInfo = false;
                        bool hasReader = false;
                        bool compatible = true;

                        for (int i = 0; i < ps.Length; i++)
                        {
                            Type pt = ps[i].ParameterType;

                            if (!hasVoiceInfo && pt.IsInstanceOfType(voiceInfo))
                            {
                                candidateArgs[i] = voiceInfo;
                                hasVoiceInfo = true;
                                continue;
                            }

                            if (!hasReader && pt.IsInstanceOfType(reader))
                            {
                                candidateArgs[i] = reader;
                                hasReader = true;
                                continue;
                            }

                            string typeName = pt.FullName ?? pt.Name;
                            string paramName = ps[i].Name ?? string.Empty;

                            if (audioSampleType != null && pt == audioSampleType)
                            {
                                candidateArgs[i] = sampleFloat;
                            }
                            else if (typeName.IndexOf("IEncoder", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                candidateArgs[i] = null;
                            }
                            else if (pt == typeof(bool))
                            {
                                candidateArgs[i] = false;
                            }
                            else if (pt == typeof(byte))
                            {
                                candidateArgs[i] = (byte)0;
                            }
                            else if (pt == typeof(short))
                            {
                                candidateArgs[i] = (short)0;
                            }
                            else if (pt == typeof(int))
                            {
                                candidateArgs[i] = 0;
                            }
                            else if (pt == typeof(long))
                            {
                                candidateArgs[i] = 0L;
                            }
                            else if (pt.IsEnum)
                            {
                                candidateArgs[i] = Activator.CreateInstance(pt);
                            }
                            else if (ps[i].HasDefaultValue)
                            {
                                candidateArgs[i] = ps[i].DefaultValue;
                            }
                            else if (!pt.IsValueType)
                            {
                                candidateArgs[i] = null;
                            }
                            else
                            {
                                candidateArgs[i] = Activator.CreateInstance(pt);
                            }
                        }

                        if (!hasVoiceInfo || !hasReader)
                            compatible = false;

                        if (!compatible) continue;

                        createMethod = candidate;
                        args2 = candidateArgs;
                        break;
                    }
                    catch { }
                }

                if (createMethod == null)
                {
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] No compatible CreateLocalVoiceAudioFromSource overload found");
                    return false;
                }

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Creating local voice with overload: {createMethod}");

                _localVoice = createMethod.Invoke(_voiceClient, args2) as LocalVoice;
                if (_localVoice == null)
                {
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] CreateLocalVoiceAudioFromSource returned null");
                    return false;
                }

                _localVoice.TransmitEnabled = true;

                try
                {
                    var vdProp = _localVoice.GetType().GetProperty("VoiceDetector");
                    if (vdProp?.CanRead == true)
                    {
                        var vd = vdProp.GetValue(_localVoice);
                        if (vd != null)
                        {
                            foreach (var propName in new[] { "On", "Enabled" })
                            {
                                var p = vd.GetType().GetProperty(propName);
                                if (p?.CanWrite == true && p.PropertyType == typeof(bool))
                                {
                                    p.SetValue(vd, false);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }

                try
                {
                    var igProp = _localVoice.GetType().GetProperty("InterestGroup");
                    if (igProp?.CanWrite == true)
                        igProp.SetValue(_localVoice, (byte)0);
                }
                catch { }

                GorillaBotPlugin.Log.LogInfo($"[{_name}] VOICE READY ({sourceName})");
                return true;
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning($"[{_name}] StartVoiceFromReader: {ex.Message}");
                return false;
            }
        }

        private object BuildFloatReaderFromClip(object audioClip)
        {
            try
            {
                int samples = (int)audioClip.GetType().GetProperty("samples").GetValue(audioClip, null);
                int channels = (int)audioClip.GetType().GetProperty("channels").GetValue(audioClip, null);
                int freq = (int)audioClip.GetType().GetProperty("frequency").GetValue(audioClip, null);
                float[] data = new float[samples * channels];
                var getData = audioClip.GetType().GetMethod("GetData", new[] { typeof(float[]), typeof(int) });
                getData.Invoke(audioClip, new object[] { data, 0 });
                float[] mono = new float[samples];
                if (channels <= 1) Array.Copy(data, mono, samples);
                else for (int i = 0; i < samples; i++)
                {
                    float s = 0;
                    for (int c = 0; c < channels; c++) s += data[i * channels + c];
                    mono[i] = s / channels;
                }
                return new LoopingFloatReader(mono, freq);
            }
            catch (Exception ex) { GorillaBotPlugin.Log.LogWarning($"[{_name}] BuildFloatReader: {ex.Message}"); return null; }
        }

        private class LoopingFloatReader : IAudioReader<float>
        {
            private readonly float[] _data;
            private int _pos;
            public int Channels => 1;
            public int SamplingRate { get; }
            public string Error => null;
            public LoopingFloatReader(float[] data, int rate) { _data = data; SamplingRate = rate; }
            public bool Read(float[] buf)
            {
                if (_data == null || _data.Length == 0) return false;
                for (int i = 0; i < buf.Length; i++) { buf[i] = _data[_pos]; if (++_pos >= _data.Length) _pos = 0; }
                return true;
            }
            public void Dispose() { }
        }

        public void StopAudio()
        {
            try
            {
                if (_localVoice != null)
                {
                    try { _localVoice.TransmitEnabled = false; } catch { }
                    try { _localVoice.RemoveSelf(); } catch { }
                    try { _localVoice.Dispose(); } catch { }
                    _localVoice = null;
                    GorillaBotPlugin.Log.LogInfo($"[{_name}] Voice stopped");
                }
            }
            catch { }
        }

        private static bool _sslBypassApplied;
        private static void EnsureSslBypass()
        {
            if (_sslBypassApplied) return;
            _sslBypassApplied = true;
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = (_, __, ___, ____) => true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                ServicePointManager.Expect100Continue = false;
            }
            catch { }
        }

        private async Task<(string pfid, string token, string nonce, string ticket)> GetAuthCredentials()
        {
            EnsureSslBypass();
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(20);

                    using (var genReq = new HttpRequestMessage(HttpMethod.Post,
                        (_use2023 && !_useMetro2024 ? "https://oculus.folkvalley.xyz/api/apps/gen?app_id=1123465617516702" : "https://oculus.folkvalley.xyz/api/apps/gen?app_id=1123465617516702")))
                    {
                        genReq.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        genReq.Headers.TryAddWithoutValidation("Accept", "application/json");
                        genReq.Content = new ByteArrayContent(new byte[0]);
                        genReq.Content.Headers.TryAddWithoutValidation("Content-Type", "application/json");

                        var genResp = await client.SendAsync(genReq);
                        var genBody = await genResp.Content.ReadAsStringAsync();
                        GorillaBotPlugin.Log.LogInfo($"[{_name}] gen status={(int)genResp.StatusCode}");
                        if (!genResp.IsSuccessStatusCode) return ("", "", "", "");

                        var genJson = JObject.Parse(genBody);
                        if (genJson["message"] != null) return ("", "", "", "");

                        string orgScopedId = genJson["org_scoped_id"]?.ToString();
                        string nonce = genJson["nonce"]?.ToString();
                        string oculusId = genJson["id"]?.ToString();
                        if (string.IsNullOrEmpty(orgScopedId) || string.IsNullOrEmpty(nonce) || string.IsNullOrEmpty(oculusId))
                            return ("", "", "", "");

                        var authPayload = new JObject
                        {
                            ["OculusId"] = oculusId,
                            ["Platform"] = "Quest",
                            ["AppId"] = "29E62",
                            ["CustomId"] = "OCULUS" + orgScopedId,
                            ["Nonce"] = nonce,
                            ["AppVersion"] = ResolveAppVersion()
                        };
                        var authBytes = Encoding.UTF8.GetBytes(authPayload.ToString(Newtonsoft.Json.Formatting.None));

                        using (var authReq = new HttpRequestMessage(HttpMethod.Post, (_use2023 ? "https://purplenurp.vercel.app/api/PlayFabAuthentication" : "https://purplenurp.vercel.app/api/PlayFabAuthentication")))
                        {
                            authReq.Headers.TryAddWithoutValidation("User-Agent",
                                "UnityPlayer/2022.3.2f1 (UnityWebRequest/1.0, libcurl/7.84.0-DEV)");
                            authReq.Content = new ByteArrayContent(authBytes);
                            authReq.Content.Headers.TryAddWithoutValidation("Content-Type", "application/json");

                            var authResp = await client.SendAsync(authReq);
                            var authBody = await authResp.Content.ReadAsStringAsync();
                            GorillaBotPlugin.Log.LogInfo($"[{_name}] auth status={(int)authResp.StatusCode}");
                            if (!authResp.IsSuccessStatusCode) return ("", "", "", "");

                            var authJson = JObject.Parse(authBody);
                            string pfid = authJson["PlayFabId"]?.ToString();
                            string sessionTicket = authJson["SessionTicket"]?.ToString();
                            if (!string.IsNullOrEmpty(pfid) && !string.IsNullOrEmpty(sessionTicket))
                            {
                                GorillaBotPlugin.Log.LogInfo($"[{_name}] Auth OK PlayFabId={pfid}");
                                return (pfid, sessionTicket, nonce, sessionTicket);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning($"[{_name}] Auth error: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] Inner: {ex.InnerException.Message}");
            }
            return ("", "", "", "");
        }
    }
}

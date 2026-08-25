using BepInEx;
using BepInEx.Logging;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
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
using UnityEngine.UI;
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
        private Rect _windowRect = new Rect(20, 20, 560, 720);
        private Vector2 _scrollPos = Vector2.zero;

        private string _botCountInput = "1";
        private string _roomInput = "";
        private string _namePrefix = "Bot";
        private string _mp3PathAll = "";
        private string _gameMode = "forestDEFAULTINFECTION";
        private readonly string[] _regions = { "usw", "us", "eu" };

        private Dictionary<string, string> _renameBuffers = new Dictionary<string, string>();
        private Dictionary<string, string> _roomBuffers = new Dictionary<string, string>();
        private Dictionary<string, string> _mp3Buffers = new Dictionary<string, string>();

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

            private VRRig FindBotVRRig()
            {
                VRRig[] rigs = UnityEngine.Object.FindObjectsOfType<VRRig>();

                foreach (VRRig rig in rigs)
                {
                    if (rig == null)
                        continue;

                    if (rig.rigSerializer == null)
                        continue;

                    try
                    {
                        int ownerId = NetworkSystem.Instance.GetOwningPlayerID(
                            rig.rigSerializer.gameObject);

                        if (ownerId == _photonClient.LocalPlayer.ActorNumber)
                            return rig;
                    }
                    catch
                    {
                        // Ignore rigs that aren't fully initialized yet.
                    }
                }

                return null;
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

        private Rect _formationWindowRect = new Rect(570, 10, 310, 540);
        private bool _formationWindowInitialized = false;

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

        private void DrawFormationWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("=== FORMATIONS ===", Bold());
            GUILayout.Label($"Active bots: {_bots.Count}");

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

            GUILayout.Space(8);
            GUILayout.Label("SPAZ jitters body, head and both hands near the forest spawn point.");

            if (GUILayout.Button("DISCONNECT ALL BOTS", GUILayout.Height(34)))
                DisconnectAllBots();

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

            if (GUILayout.Button("spawn seals", GUILayout.Height(32)))
            {
                int count = int.TryParse(_botCountInput, out int c) ? Math.Max(1, c) : 1;
                SpawnMultipleBots(count, _roomInput, _namePrefix, _gameMode);
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

        public void SpawnMultipleBots(int count, string roomToJoin, string namePrefix, string gameMode)
        {
            Log.LogInfo($"[BOT] Spawning {count} bots...");
            for (int i = 0; i < count; i++)
            {
                var bot = new BotInstance(namePrefix, roomToJoin, gameMode, _regions);
                _bots.Add(bot);
                bot.Connect();
                Thread.Sleep(400);
            }
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
            Spaz
        }

        private FormationMode _formationMode = FormationMode.Down;
        private int _formationSlot;
        private int _formationTotal = 1;
        private float _formationSpeed = 1.5f;
        private float _formationRadius = 4.0f;
        private float _formationHeight = 3.0f;
        private float _formationPhase;

        private static readonly Vector3 ForestCenter = new Vector3(-63.735f, 3.4254f, -63.9312f);

        public string Name => _name;
        public string TargetRoom => _targetRoom;
        public bool IsConnected => _connected && _inRoom;
        public bool IsConnecting => _connecting;
        public string CurrentRoom => _currentRoom;
        public string CurrentRegion => _currentRegion;

        public BotInstance(string name, string roomToJoin, string gameMode, string[] regions)
        {
            _name = name;
            _targetRoom = roomToJoin ?? "";
            _gameMode = string.IsNullOrEmpty(gameMode) ? "forestDEFAULTINFECTION" : gameMode;
            _regions = regions ?? new[] { "usw", "us", "eu" };
            _regionIndex = 0;
            _searchingRegions = !string.IsNullOrEmpty(_targetRoom);
            _allowCreate = false;
            _hopPublic = string.IsNullOrEmpty(_targetRoom);
        }

        private void SetBotDisplayName(VRRig rig)
        {
            if (rig == null || rig.playerText == null)
                return;

            rig.playerText.text = _name;

            Canvas canvas =
                rig.playerText.transform.parent.GetComponent<Canvas>();

            if (canvas != null &&
                GorillaTagger.Instance != null &&
                GorillaTagger.Instance.mainCamera != null)
            {
                canvas.worldCamera =
                    GorillaTagger.Instance.mainCamera.GetComponent<Camera>();
            }
        }

        private void SetRandomBotColor(VRRig rig)
        {
            try
            {
                if (rig == null)
                    return;

                float red = UnityEngine.Random.Range(0f, 1f);
                float green = UnityEngine.Random.Range(0f, 1f);
                float blue = UnityEngine.Random.Range(0f, 1f);

                rig.InitializeNoobMaterialLocal(
                     red,
                     green,
                     blue,
                     false
                );

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Random color: " +
                    $"{red:F2}, {green:F2}, {blue:F2}");
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogWarning(
                    $"[{_name}] SetRandomBotColor failed: {ex.Message}");
            }
        }

        private IEnumerator ApplyBotVisualsWhenReady()
        {
            float elapsed = 0f;

            while (elapsed < 5f)
            {
                elapsed += Time.deltaTime;

                VRRig rig = FindBotVRRig();

                if (rig != null)
                {
                    SetBotDisplayName(rig);
                    SetRandomBotColor(rig);
                    yield break;
                }

                yield return null;
            }

            GorillaBotPlugin.Log.LogWarning(
                $"[{_name}] Could not find VRRig after 5 seconds.");
        }

        private static string ResolveAppVersion()
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

            return "live1.1.1.73";
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
                _photonClient.AppId = "a24076db-10e1-4bb5-b2e5-8462364f4072";
                _photonClient.AppVersion = appVer;
                _photonClient.NameServerHost = "ns.exitgames.com";
                _photonClient.AddCallbackTarget(this);

                var auth = new AuthenticationValues(Guid.NewGuid().ToString("N"));
                auth.AuthType = CustomAuthenticationType.Custom;
                auth.AddAuthParameter("username", pfid);
                auth.AddAuthParameter("token", "");
                auth.SetAuthPostData(new Dictionary<string, object>
                {
                    { "AppId", "CED21" },
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
            _photonClient.AppId = "a24076db-10e1-4bb5-b2e5-8462364f4072";
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
                    { "AppId", "CED21" },
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

            // These are the player properties visible in the normal join packet.
            // Use the bot's own authenticated PlayFab ID; do not copy another user's ID.
            props["didTutorial"] = true;
            props[(byte)255] = "0";
            props[(byte)253] = _savedPfid ?? "";

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

        private string[] BuildRandomCosmeticDisplayNames()
        {
            try
            {
                var controller = GorillaNetworking.CosmeticsController.instance;

                if (controller == null)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] CosmeticsController.instance is NULL.");
                    return null;
                }

                if (BotCosmeticItemIds == null || BotCosmeticItemIds.Length == 0)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] BotCosmeticItemIds is empty.");
                    return null;
                }

                string displayName = BotCosmeticItemIds[
                    UnityEngine.Random.Range(0, BotCosmeticItemIds.Length)
                ];

                if (string.IsNullOrEmpty(displayName))
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] Selected cosmetic ID is empty.");
                    return null;
                }

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Selected cosmetic ID: {displayName}");

                /*
                 * The cosmetic list you're using contains display names such as:
                 *
                 *     LFABB.
                 *
                 * GetItemNameFromDisplayName() expects the display-name value,
                 * so pass the value exactly as it appears in the cosmetic list.
                 */
                string internalName;
                string lookupName = displayName.TrimEnd('.');

                try
                {
                    internalName =
                        controller.GetItemNameFromDisplayName(lookupName);
                }
                catch (Exception ex)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] GetItemNameFromDisplayName failed for " +
                        $"'{displayName}': {ex.Message}");
                    return null;
                }

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Display name '{displayName}' -> " +
                    $"internal name '{internalName}'");

                if (string.IsNullOrEmpty(internalName))
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] Could not resolve cosmetic " +
                        $"display name '{displayName}'.");
                    return null;
                }

                /*
                 * GetItemFromDict() returns nullItem when the dictionary
                 * doesn't contain the requested internal name.
                 */
                var item = controller.GetItemFromDict(internalName);

                if (item.isNullItem)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] Cosmetic '{displayName}' resolved " +
                        $"to the null item. Internal name: '{internalName}'");

                    return null;
                }

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Resolved cosmetic '{displayName}' -> " +
                    $"'{item.itemName}'");

                var set =
                    new GorillaNetworking.CosmeticsController.CosmeticSet();

                /*
                 * Start with all 11 slots empty.
                 */
                set.ClearSet(controller.nullItem);

                /*
                 * ApplyCosmeticItemToSet belongs to CosmeticsController,
                 * NOT CosmeticSet.
                 */
                controller.ApplyCosmeticItemToSet(
                    set,
                    item,
                    false,
                    false
                );

                string[] result = set.ToDisplayNameArray();

                if (result == null)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] ToDisplayNameArray returned NULL.");
                    return null;
                }

                if (result.Length != 11)
                {
                    GorillaBotPlugin.Log.LogWarning(
                        $"[{_name}] Cosmetic array has {result.Length} " +
                        $"entries; expected 11.");
                    return null;
                }

                GorillaBotPlugin.Log.LogInfo(
                    $"[{_name}] Cosmetic array successfully built.");

                for (int i = 0; i < result.Length; i++)
                {
                    GorillaBotPlugin.Log.LogInfo(
                        $"[{_name}] Cosmetic[{i}] = '{result[i]}'");
                }

                return result;
            }
            catch (Exception ex)
            {
                GorillaBotPlugin.Log.LogError(
                    $"[{_name}] Exception while building cosmetics: {ex}");
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
                SpawnPlayerControllerSequence());

            ConnectVoiceRoom();
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

            // Give the host time to process the 202 and let VRRigSerializer
            // bind the spawned PhotonView to its pooled RigContainer.
            yield return new WaitForSeconds(0.75f);

            GorillaBotPlugin.Log.LogInfo(
                $"[{_name}] Player Network Controller spawn sent; " +
                "starting pose synchronization.");

            SendFixedRigTransform();

            GorillaBotPlugin.Instance.StartCoroutine(
                ApplyBotVisualsWhenReady());

            SendCosmeticsToOthers();
        }

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
            // Fixed forest spawn point requested for the fan game.
            _rigPosition = ForestCenter;
            _rigYaw = 0;
            _rigPositionSet = true;

            GorillaBotPlugin.Log.LogInfo(
                $"[{_name}] Rig position = {_rigPosition} (fixed forest position)");
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

        private Vector3 GetFormationPosition()
        {
            Vector3 center = ForestCenter;
            float total = Mathf.Max(1, _formationTotal);
            float slot = _formationSlot;
            float t = Time.time * _formationSpeed + _formationPhase;
            float radius = Mathf.Max(1.0f, _formationRadius);

            switch (_formationMode)
            {
                case FormationMode.Down:
                    return center;

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

        private byte[] SerializePackedRigState(
            Vector3 position,
            int yaw,
            Quaternion headRotation,
            Vector3 rightHandPosition,
            Quaternion rightHandRotation,
            Vector3 leftHandPosition,
            Quaternion leftHandRotation)
        {
            PackedRigState state = default(PackedRigState);

            state.headRotation = headRotation;
            state.rightHandPosition = rightHandPosition;
            state.rightHandRotation = rightHandRotation;
            state.leftHandPosition = leftHandPosition;
            state.leftHandRotation = leftHandRotation;
            state.position = position;
            state.roundedRotation = yaw;
            state.handPosition = 0;
            state.state = 0;
            state.grabbedRopeIndex = 0;
            state.ropeBoneIndex = 0;
            state.ropeGrabIsLeft = false;
            state.ropeGrabOffset = Vector3.zero;
            state.serverTimeStamp = GetBotServerTimestamp() / 1000.0;
            state.remoteUseReplacementVoice = false;
            state.speakingLoudness = 0f;

            int size = Marshal.SizeOf(typeof(PackedRigState));
            if (size != 136)
                throw new InvalidOperationException(
                    $"PackedRigState size is {size}, expected 136.");

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(state, ptr, false);

                byte[] bytes = new byte[size];
                Marshal.Copy(ptr, bytes, 0, size);
                return bytes;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
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

                Vector3 headPos =
                    _rigPosition + new Vector3(0f, 0.6f, 0f);

                Vector3 rightHand =
                    _rigPosition + new Vector3(0.22f, 0f, 0f);

                Vector3 leftHand =
                    _rigPosition + new Vector3(-0.22f, 0f, 0f);

                Quaternion headRotation = Quaternion.identity;
                Quaternion rightHandRotation = Quaternion.identity;
                Quaternion leftHandRotation = Quaternion.identity;

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

                    _rigPosition = ForestCenter + jitter;
                    headPos = _rigPosition + new Vector3(0f, 0.6f, 0f) + jitter * 0.8f;
                    rightHand = _rigPosition + new Vector3(0.22f, 0.02f, 0f) + new Vector3(b, c, a) * amp * 1.8f;
                    leftHand = _rigPosition + new Vector3(-0.22f, 0.02f, 0f) + new Vector3(c, a, b) * amp * 1.8f;

                    headRotation = Quaternion.Euler(a * 75f, c * 120f, b * 60f);
                    rightHandRotation = Quaternion.Euler(b * 120f, a * 160f, c * 100f);
                    leftHandRotation = Quaternion.Euler(c * 120f, b * 160f, a * 100f);
                }

                byte[] serialized =
                    SerializePackedRigState(
                        _rigPosition,
                        _rigYaw,
                        headRotation,
                        rightHand,
                        rightHandRotation,
                        leftHand,
                        leftHandRotation);

                // Exactly matches the logical object[] produced by
                // NetCrossoverUtils.WriteNetDataToBuffer().
                object[] viewData =
                    new object[4 + serialized.Length];

                int index = 0;

                viewData[index++] = _botRigViewId;
                viewData[index++] = false;
                viewData[index++] = null;
                viewData[index++] = serialized.Length;

                for (int i = 0; i < serialized.Length; i++)
                    viewData[index++] = serialized[i];

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
                    $"serializedBytes={serialized.Length} " +
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
                _voiceTransport.AppId = "8d438081-480b-460f-ad60-d510fd2f23d0";
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
                        { "AppId", "CED21" },
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
            GorillaBotPlugin.Log.LogInfo($"[{_name}] Voice room joined, ready={_voiceReady}");
            if (_voiceReady)
                GorillaBotPlugin.Instance.StartCoroutine(StartSystemMicrophoneWhenReady());
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
                    loopProp.SetValue(reader, true);

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

                var createMethod = _voiceClient.GetType().GetMethod("CreateLocalVoiceAudioFromSource");
                if (createMethod == null)
                {
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] CreateLocalVoiceAudioFromSource not found");
                    return false;
                }

                Type audioSampleType = FindVoiceType("Photon.Voice.AudioSampleType");
                object sampleFloat = null;
                if (audioSampleType != null)
                {
                    try { sampleFloat = Enum.Parse(audioSampleType, "Float"); }
                    catch { sampleFloat = Enum.ToObject(audioSampleType, 1); }
                }

                var createParams = createMethod.GetParameters();
                var args2 = new object[createParams.Length];
                args2[0] = voiceInfo;
                args2[1] = reader;
                if (createParams.Length > 2) args2[2] = sampleFloat ?? 1;
                if (createParams.Length > 3) args2[3] = null;
                if (createParams.Length > 4) args2[4] = 0;

                _localVoice = createMethod.Invoke(_voiceClient, args2) as LocalVoice;
                if (_localVoice == null)
                {
                    GorillaBotPlugin.Log.LogWarning($"[{_name}] CreateLocalVoiceAudioFromSource returned null");
                    return false;
                }

                _localVoice.TransmitEnabled = false;

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
                        "https://netanyahu.one/api/apps/gen?app_id=1195639176961698"))
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
                            ["AppId"] = "CED21",
                            ["CustomId"] = "OCULUS" + orgScopedId,
                            ["Nonce"] = nonce,
                            ["AppVersion"] = ResolveAppVersion()
                        };
                        var authBytes = Encoding.UTF8.GetBytes(authPayload.ToString(Newtonsoft.Json.Formatting.None));

                        using (var authReq = new HttpRequestMessage(HttpMethod.Post, "https://pcvred-by-table.vercel.app/"))
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

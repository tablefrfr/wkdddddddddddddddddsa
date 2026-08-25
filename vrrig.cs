using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Gameplay;
using GorillaNetworking;
using GorillaTag;
using GorillaTag.GuidedRefs;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using WebSocketSharp;

// Token: 0x0200020F RID: 527
public class VRRig : MonoBehaviour, IWrappedSerializable, INetworkStruct, IPreDisable, IUserCosmeticsCallback, IGuidedRefTargetMono, IGuidedRefMonoBehaviour, IGuidedRefObject, IGuidedRefReceiverMono
{
	// Token: 0x06000BA0 RID: 2976 RVA: 0x00022812 File Offset: 0x00020A12
	public int ActiveTransferrableObjectIndex(int idx)
	{
		return this.reliableState.activeTransferrableObjectIndex[idx];
	}

	// Token: 0x06000BA1 RID: 2977 RVA: 0x00022821 File Offset: 0x00020A21
	public int ActiveTransferrableObjectIndexLength()
	{
		return this.reliableState.activeTransferrableObjectIndex.Length;
	}

	// Token: 0x06000BA2 RID: 2978 RVA: 0x00022830 File Offset: 0x00020A30
	public void SetActiveTransferrableObjectIndex(int idx, int v)
	{
		if (this.reliableState.activeTransferrableObjectIndex[idx] != v)
		{
			this.reliableState.activeTransferrableObjectIndex[idx] = v;
			this.reliableState.SetIsDirty();
		}
	}

	// Token: 0x06000BA3 RID: 2979 RVA: 0x0002285B File Offset: 0x00020A5B
	public TransferrableObject.PositionState TransferrablePosStates(int idx)
	{
		return this.reliableState.transferrablePosStates[idx];
	}

	// Token: 0x06000BA4 RID: 2980 RVA: 0x0002286A File Offset: 0x00020A6A
	public void SetTransferrablePosStates(int idx, TransferrableObject.PositionState v)
	{
		if (this.reliableState.transferrablePosStates[idx] != v)
		{
			this.reliableState.transferrablePosStates[idx] = v;
			this.reliableState.SetIsDirty();
		}
	}

	// Token: 0x06000BA5 RID: 2981 RVA: 0x00022895 File Offset: 0x00020A95
	public TransferrableObject.ItemStates TransferrableItemStates(int idx)
	{
		return this.reliableState.transferrableItemStates[idx];
	}

	// Token: 0x06000BA6 RID: 2982 RVA: 0x000228A4 File Offset: 0x00020AA4
	public void SetTransferrableItemStates(int idx, TransferrableObject.ItemStates v)
	{
		if (this.reliableState.transferrableItemStates[idx] != v)
		{
			this.reliableState.transferrableItemStates[idx] = v;
			this.reliableState.SetIsDirty();
		}
	}

	// Token: 0x06000BA7 RID: 2983 RVA: 0x000228CF File Offset: 0x00020ACF
	public void SetTransferrableDockPosition(int idx, BodyDockPositions.DropPositions v)
	{
		if (this.reliableState.transferableDockPositions[idx] != v)
		{
			this.reliableState.transferableDockPositions[idx] = v;
			this.reliableState.SetIsDirty();
		}
	}

	// Token: 0x06000BA8 RID: 2984 RVA: 0x000228FA File Offset: 0x00020AFA
	public BodyDockPositions.DropPositions TransferrableDockPosition(int idx)
	{
		return this.reliableState.transferableDockPositions[idx];
	}

	// Token: 0x1700016C RID: 364
	// (get) Token: 0x06000BA9 RID: 2985 RVA: 0x00022909 File Offset: 0x00020B09
	// (set) Token: 0x06000BAA RID: 2986 RVA: 0x00022916 File Offset: 0x00020B16
	public int WearablePackedStates
	{
		get
		{
			return this.reliableState.wearablesPackedStates;
		}
		set
		{
			if (this.reliableState.wearablesPackedStates != value)
			{
				this.reliableState.wearablesPackedStates = value;
				this.reliableState.SetIsDirty();
			}
		}
	}

	// Token: 0x1700016D RID: 365
	// (get) Token: 0x06000BAB RID: 2987 RVA: 0x0002293D File Offset: 0x00020B3D
	// (set) Token: 0x06000BAC RID: 2988 RVA: 0x0002294A File Offset: 0x00020B4A
	public int LeftThrowableProjectileIndex
	{
		get
		{
			return this.reliableState.lThrowableProjectileIndex;
		}
		set
		{
			if (this.reliableState.lThrowableProjectileIndex != value)
			{
				this.reliableState.lThrowableProjectileIndex = value;
				this.reliableState.SetIsDirty();
			}
		}
	}

	// Token: 0x1700016E RID: 366
	// (get) Token: 0x06000BAD RID: 2989 RVA: 0x00022971 File Offset: 0x00020B71
	// (set) Token: 0x06000BAE RID: 2990 RVA: 0x0002297E File Offset: 0x00020B7E
	public int RightThrowableProjectileIndex
	{
		get
		{
			return this.reliableState.rThrowableProjectileIndex;
		}
		set
		{
			if (this.reliableState.rThrowableProjectileIndex != value)
			{
				this.reliableState.rThrowableProjectileIndex = value;
				this.reliableState.SetIsDirty();
			}
		}
	}

	// Token: 0x1700016F RID: 367
	// (get) Token: 0x06000BAF RID: 2991 RVA: 0x000229A5 File Offset: 0x00020BA5
	// (set) Token: 0x06000BB0 RID: 2992 RVA: 0x000229B2 File Offset: 0x00020BB2
	public Color LeftThrowableProjectileColor
	{
		get
		{
			return this.reliableState.lThrowableProjectileColor;
		}
		set
		{
			if (!this.reliableState.lThrowableProjectileColor.CompareAs255Unclamped(value))
			{
				this.reliableState.lThrowableProjectileColor = value;
				this.reliableState.SetIsDirty();
			}
		}
	}

	// Token: 0x17000170 RID: 368
	// (get) Token: 0x06000BB1 RID: 2993 RVA: 0x000229DE File Offset: 0x00020BDE
	// (set) Token: 0x06000BB2 RID: 2994 RVA: 0x000229EB File Offset: 0x00020BEB
	public Color RightThrowableProjectileColor
	{
		get
		{
			return this.reliableState.rThrowableProjectileColor;
		}
		set
		{
			if (!this.reliableState.rThrowableProjectileColor.CompareAs255Unclamped(value))
			{
				this.reliableState.rThrowableProjectileColor = value;
				this.reliableState.SetIsDirty();
			}
		}
	}

	// Token: 0x06000BB3 RID: 2995 RVA: 0x00022A17 File Offset: 0x00020C17
	public Color GetThrowableProjectileColor(bool isLeftHand)
	{
		if (!isLeftHand)
		{
			return this.RightThrowableProjectileColor;
		}
		return this.LeftThrowableProjectileColor;
	}

	// Token: 0x06000BB4 RID: 2996 RVA: 0x00022A29 File Offset: 0x00020C29
	public void SetThrowableProjectileColor(bool isLeftHand, Color color)
	{
		if (isLeftHand)
		{
			this.LeftThrowableProjectileColor = color;
			return;
		}
		this.RightThrowableProjectileColor = color;
	}

	// Token: 0x06000BB5 RID: 2997 RVA: 0x00022A3D File Offset: 0x00020C3D
	public void SetRandomThrowableModelIndex(int randModelIndex)
	{
		this.RandomThrowableIndex = randModelIndex;
	}

	// Token: 0x06000BB6 RID: 2998 RVA: 0x00022A46 File Offset: 0x00020C46
	public int GetRandomThrowableModelIndex()
	{
		return this.RandomThrowableIndex;
	}

	// Token: 0x17000171 RID: 369
	// (get) Token: 0x06000BB7 RID: 2999 RVA: 0x00022A4E File Offset: 0x00020C4E
	// (set) Token: 0x06000BB8 RID: 3000 RVA: 0x00022A5B File Offset: 0x00020C5B
	private int RandomThrowableIndex
	{
		get
		{
			return this.reliableState.randomThrowableIndex;
		}
		set
		{
			if (this.reliableState.randomThrowableIndex != value)
			{
				this.reliableState.randomThrowableIndex = value;
				this.reliableState.SetIsDirty();
			}
		}
	}

	// Token: 0x17000172 RID: 370
	// (get) Token: 0x06000BB9 RID: 3001 RVA: 0x00022A82 File Offset: 0x00020C82
	// (set) Token: 0x06000BBA RID: 3002 RVA: 0x00022A8F File Offset: 0x00020C8F
	public bool IsMicEnabled
	{
		get
		{
			return this.reliableState.isMicEnabled;
		}
		set
		{
			if (this.reliableState.isMicEnabled != value)
			{
				this.reliableState.isMicEnabled = value;
				this.reliableState.SetIsDirty();
			}
		}
	}

	// Token: 0x17000173 RID: 371
	// (get) Token: 0x06000BBB RID: 3003 RVA: 0x00022AB6 File Offset: 0x00020CB6
	// (set) Token: 0x06000BBC RID: 3004 RVA: 0x00022AC3 File Offset: 0x00020CC3
	public int SizeLayerMask
	{
		get
		{
			return this.reliableState.sizeLayerMask;
		}
		set
		{
			if (this.reliableState.sizeLayerMask != value)
			{
				this.reliableState.sizeLayerMask = value;
				this.reliableState.SetIsDirty();
			}
		}
	}

	// Token: 0x17000174 RID: 372
	// (get) Token: 0x06000BBD RID: 3005 RVA: 0x00022AEA File Offset: 0x00020CEA
	public Photon.Realtime.Player Creator
	{
		get
		{
			return this.creator;
		}
	}

	// Token: 0x17000175 RID: 373
	// (get) Token: 0x06000BBE RID: 3006 RVA: 0x00022AF2 File Offset: 0x00020CF2
	internal bool Initialized
	{
		get
		{
			return this.initialized;
		}
	}

	// Token: 0x17000176 RID: 374
	// (get) Token: 0x06000BBF RID: 3007 RVA: 0x00022AFA File Offset: 0x00020CFA
	public float SpeakingLoudness
	{
		get
		{
			return this.speakingLoudness;
		}
	}

	// Token: 0x06000BC0 RID: 3008 RVA: 0x00066934 File Offset: 0x00064B34
	private void Awake()
	{
		this.fxSettings = UnityEngine.Object.Instantiate<FXSystemSettings>(this.sharedFXSettings);
		this.fxSettings.forLocalRig = this.isOfflineVRRig;
		Dictionary<string, GameObject> dictionary = new Dictionary<string, GameObject>();
		foreach (GameObject gameObject in this.cosmetics)
		{
			GameObject gameObject2;
			if (!dictionary.TryGetValue(gameObject.name, out gameObject2))
			{
				dictionary.Add(gameObject.name, gameObject);
			}
		}
		foreach (GameObject gameObject3 in this.overrideCosmetics)
		{
			GameObject gameObject2;
			if (dictionary.TryGetValue(gameObject3.name, out gameObject2) && gameObject2.name == gameObject3.name)
			{
				gameObject2.name = "OVERRIDDEN";
			}
		}
		this.cosmetics = this.cosmetics.Concat(this.overrideCosmetics).ToArray<GameObject>();
		this.cosmeticsObjectRegistry.Initialize(this.cosmetics);
		this.lastPosition = base.transform.position;
		this.SharedStart();
	}

	// Token: 0x06000BC1 RID: 3009 RVA: 0x0001B2AB File Offset: 0x000194AB
	private void Start()
	{
	}

	// Token: 0x06000BC2 RID: 3010 RVA: 0x00022B02 File Offset: 0x00020D02
	private void EnsureInstantiatedMaterial()
	{
		if (this.myDefaultSkinMaterialInstance == null)
		{
			this.myDefaultSkinMaterialInstance = UnityEngine.Object.Instantiate<Material>(this.materialsToChangeTo[0]);
			this.materialsToChangeTo[0] = this.myDefaultSkinMaterialInstance;
		}
	}

	// Token: 0x06000BC3 RID: 3011 RVA: 0x00066A34 File Offset: 0x00064C34
	private void ApplyColorCode()
	{
		float @float = PlayerPrefs.GetFloat("redValue", 0.16f);
		float float2 = PlayerPrefs.GetFloat("greenValue", 0.16f);
		float float3 = PlayerPrefs.GetFloat("blueValue", 0.16f);
		GorillaTagger.Instance.UpdateColor(@float, float2, float3);
	}

	// Token: 0x06000BC4 RID: 3012 RVA: 0x00066A80 File Offset: 0x00064C80
	private void SharedStart()
	{
		if (this.isInitialized)
		{
			return;
		}
		this.isInitialized = true;
		this.myBodyDockPositions = base.GetComponent<BodyDockPositions>();
		this.reliableState.SharedStart(this.isOfflineVRRig, this.myBodyDockPositions);
		this.concatStringOfCosmeticsAllowed = "";
		this.playerText.transform.parent.GetComponent<Canvas>().worldCamera = GorillaTagger.Instance.mainCamera.GetComponent<Camera>();
		this.EnsureInstantiatedMaterial();
		this.initialized = false;
		this.currentState = TransferrableObject.PositionState.OnChest;
		if (this.setMatIndex > -1 && this.setMatIndex < this.materialsToChangeTo.Length)
		{
			this.mainSkin.material = this.materialsToChangeTo[this.setMatIndex];
		}
		if (this.isOfflineVRRig)
		{
			CosmeticsController.instance.currentWornSet.LoadFromPlayerPreferences(CosmeticsController.instance);
			if (Application.platform == RuntimePlatform.Android && this.spectatorSkin != null)
			{
				UnityEngine.Object.Destroy(this.spectatorSkin);
			}
			base.StartCoroutine(this.OccasionalUpdate());
		}
		else if (!this.isOfflineVRRig)
		{
			if (this.spectatorSkin != null)
			{
				UnityEngine.Object.Destroy(this.spectatorSkin);
			}
			this.head.syncPos = -this.headBodyOffset;
		}
		if (base.transform.parent == null)
		{
			base.transform.parent = GorillaParent.instance.transform;
		}
		GorillaSkin.ApplyToRig(this, this.defaultSkin);
		base.Invoke("ApplyColorCode", 1f);
	}

	// Token: 0x06000BC5 RID: 3013 RVA: 0x00022B33 File Offset: 0x00020D33
	private IEnumerator OccasionalUpdate()
	{
		for (;;)
		{
			try
			{
				if (RoomSystem.JoinedRoom && NetworkSystem.Instance.IsMasterClient && GorillaGameModes.GameMode.ActiveNetworkHandler.IsNull())
				{
					GorillaGameModes.GameMode.LoadGameModeFromProperty();
				}
			}
			catch
			{
			}
			yield return new WaitForSeconds(1f);
		}
		yield break;
	}

	// Token: 0x06000BC6 RID: 3014 RVA: 0x00066C08 File Offset: 0x00064E08
	public bool IsItemAllowed(string itemName)
	{
		if (itemName == "Slingshot")
		{
			return PhotonNetwork.InRoom && GorillaGameManager.instance is GorillaBattleManager;
		}
		if (this.concatStringOfCosmeticsAllowed == null)
		{
			return false;
		}
		if (this.concatStringOfCosmeticsAllowed.Contains(itemName))
		{
			return true;
		}
		bool canTryOn = CosmeticsController.instance.GetItemFromDict(itemName).canTryOn;
		return this.inTryOnRoom && canTryOn;
	}

	// Token: 0x06000BC7 RID: 3015 RVA: 0x00066C74 File Offset: 0x00064E74
	private void LateUpdate()
	{
		base.transform.localScale = Vector3.one * this.scaleFactor;
		if (this.isOfflineVRRig)
		{
			if (GorillaGameManager.instance != null)
			{
				this.speedArray = GorillaGameManager.instance.LocalPlayerSpeed();
				GorillaLocomotion.Player.Instance.jumpMultiplier = this.speedArray[1];
				GorillaLocomotion.Player.Instance.maxJumpSpeed = this.speedArray[0];
			}
			else
			{
				GorillaLocomotion.Player.Instance.jumpMultiplier = 1.1f;
				GorillaLocomotion.Player.Instance.maxJumpSpeed = 6.5f;
			}
			this.scaleFactor = GorillaLocomotion.Player.Instance.scale;
			base.transform.localScale = Vector3.one * this.scaleFactor;
			base.transform.eulerAngles = new Vector3(0f, this.mainCamera.transform.rotation.eulerAngles.y, 0f);
			this.syncPos = this.mainCamera.transform.position + this.headConstraint.rotation * this.head.trackingPositionOffset * this.scaleFactor + base.transform.rotation * this.headBodyOffset * this.scaleFactor;
			base.transform.position = this.syncPos;
			this.head.MapMine(this.scaleFactor, this.playerOffsetTransform);
			this.rightHand.MapMine(this.scaleFactor, this.playerOffsetTransform);
			this.leftHand.MapMine(this.scaleFactor, this.playerOffsetTransform);
			this.rightIndex.MapMyFinger(this.lerpValueFingers);
			this.rightMiddle.MapMyFinger(this.lerpValueFingers);
			this.rightThumb.MapMyFinger(this.lerpValueFingers);
			this.leftIndex.MapMyFinger(this.lerpValueFingers);
			this.leftMiddle.MapMyFinger(this.lerpValueFingers);
			this.leftThumb.MapMyFinger(this.lerpValueFingers);
			if (GorillaTagger.Instance.loadedDeviceName == "Oculus")
			{
				this.mainSkin.enabled = OVRManager.hasInputFocus;
			}
			this.mainSkin.enabled = !GorillaLocomotion.Player.Instance.inOverlay;
			this.speakingLoudness = 0f;
			if (this.shouldSendSpeakingLoudness && this.photonView)
			{
				PhotonVoiceView component = this.photonView.GetComponent<PhotonVoiceView>();
				if (component && component.RecorderInUse)
				{
					if (this.audioDesc != component.RecorderInUse.InputSource)
					{
						this.audioDesc = component.RecorderInUse.InputSource;
						this.currentMicWrapper = (this.audioDesc as MicWrapper);
					}
					if (this.currentMicWrapper != null)
					{
						int num = this.replacementVoiceDetectionDelay;
						float[] array = new float[num];
						if (this.currentMicWrapper.Mic.samples >= num && this.currentMicWrapper.Mic.GetData(array, this.currentMicWrapper.Mic.samples - num))
						{
							float num2 = 0f;
							for (int i = 0; i < num; i++)
							{
								float num3 = Mathf.Sqrt(array[i]);
								if (num3 > num2)
								{
									num2 = num3;
								}
							}
							this.speakingLoudness = num2;
						}
					}
				}
			}
		}
		else
		{
			if (this.voiceAudio != null)
			{
				float num4 = (GorillaTagger.Instance.offlineVRRig.transform.localScale.x - base.transform.localScale.x) / this.pitchScale + this.pitchOffset;
				float num5 = this.UsingHauntedRing ? this.HauntedRingVoicePitch : num4;
				num5 = (this.IsHaunted ? this.HauntedVoicePitch : num5);
				if (!Mathf.Approximately(this.voiceAudio.pitch, num5))
				{
					this.voiceAudio.pitch = num5;
				}
				bool isHaunted = GorillaTagger.Instance.offlineVRRig.IsHaunted;
				if (isHaunted != this.playerWasHaunted)
				{
					if (isHaunted)
					{
						this.nonHauntedVolume = this.voiceAudio.volume;
						this.voiceAudio.volume = this.HauntedHearingVolume;
					}
					else
					{
						this.voiceAudio.volume = this.nonHauntedVolume;
					}
					this.playerWasHaunted = isHaunted;
				}
			}
			if (Time.time > this.timeSpawned + this.doNotLerpConstant)
			{
				base.transform.position = Vector3.Lerp(base.transform.position, this.syncPos, this.lerpValueBody * 0.66f);
				if (this.currentRopeSwing && this.currentRopeSwingTarget)
				{
					Vector3 b;
					if (this.grabbedRopeIsLeft)
					{
						b = this.currentRopeSwingTarget.position - this.leftHandTransform.position;
					}
					else
					{
						b = this.currentRopeSwingTarget.position - this.rightHandTransform.position;
					}
					if (this.shouldLerpToRope)
					{
						base.transform.position += Vector3.Lerp(Vector3.zero, b, this.lastRopeGrabTimer * 4f);
						if (this.lastRopeGrabTimer < 1f)
						{
							this.lastRopeGrabTimer += Time.deltaTime;
						}
					}
					else
					{
						base.transform.position += b;
					}
				}
				else if (this.currentHoldParent != null)
				{
					base.transform.position += this.currentHoldParent.TransformPoint(this.grabbedRopeOffset) - (this.grabbedRopeIsLeft ? this.leftHandTransform : this.rightHandTransform).position;
				}
			}
			else
			{
				base.transform.position = this.syncPos;
			}
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, this.syncRotation, this.lerpValueBody);
			base.transform.position = this.SanitizeVector3(base.transform.position);
			base.transform.rotation = this.SanitizeQuaternion(base.transform.rotation);
			this.head.syncPos = base.transform.rotation * -this.headBodyOffset * this.scaleFactor;
			this.head.MapOther(this.lerpValueBody);
			this.rightHand.MapOther(this.lerpValueBody);
			this.leftHand.MapOther(this.lerpValueBody);
			this.rightIndex.MapOtherFinger((float)(this.handSync % 10) / 10f, this.lerpValueFingers);
			this.rightMiddle.MapOtherFinger((float)(this.handSync % 100) / 100f, this.lerpValueFingers);
			this.rightThumb.MapOtherFinger((float)(this.handSync % 1000) / 1000f, this.lerpValueFingers);
			this.leftIndex.MapOtherFinger((float)(this.handSync % 10000) / 10000f, this.lerpValueFingers);
			this.leftMiddle.MapOtherFinger((float)(this.handSync % 100000) / 100000f, this.lerpValueFingers);
			this.leftThumb.MapOtherFinger((float)(this.handSync % 1000000) / 1000000f, this.lerpValueFingers);
			this.leftHandHoldableStatus = this.handSync % 10000000 / 1000000;
			this.rightHandHoldableStatus = this.handSync % 100000000 / 10000000;
		}
		if (this.creator != null)
		{
			ScienceExperimentManager instance = ScienceExperimentManager.instance;
			int num6;
			if (instance != null && instance.GetMaterialIfPlayerInGame(this.creator.ActorNumber, out num6))
			{
				this.tempMatIndex = num6;
			}
			else
			{
				this.tempMatIndex = ((GorillaGameManager.instance != null) ? GorillaGameManager.instance.MyMatIndex(this.creator) : 0);
			}
			if (this.setMatIndex != this.tempMatIndex)
			{
				this.setMatIndex = this.tempMatIndex;
				this.ChangeMaterialLocal(this.setMatIndex);
			}
		}
	}

	// Token: 0x06000BC8 RID: 3016 RVA: 0x0001B2AB File Offset: 0x000194AB
	public void SetHeadBodyOffset()
	{
	}

	// Token: 0x06000BC9 RID: 3017 RVA: 0x00022B3B File Offset: 0x00020D3B
	public void VRRigResize(float ratioVar)
	{
		this.ratio *= ratioVar;
	}

	// Token: 0x06000BCA RID: 3018 RVA: 0x000674A8 File Offset: 0x000656A8
	public int ReturnHandPosition()
	{
		return 0 + Mathf.FloorToInt(this.rightIndex.calcT * 9.99f) + Mathf.FloorToInt(this.rightMiddle.calcT * 9.99f) * 10 + Mathf.FloorToInt(this.rightThumb.calcT * 9.99f) * 100 + Mathf.FloorToInt(this.leftIndex.calcT * 9.99f) * 1000 + Mathf.FloorToInt(this.leftMiddle.calcT * 9.99f) * 10000 + Mathf.FloorToInt(this.leftThumb.calcT * 9.99f) * 100000 + this.leftHandHoldableStatus * 1000000 + this.rightHandHoldableStatus * 10000000;
	}

	// Token: 0x06000BCB RID: 3019 RVA: 0x00067574 File Offset: 0x00065774
	public void OnDestroy()
	{
		if (ApplicationQuittingState.IsQuitting)
		{
			return;
		}
		GuidedRefHub.UnregisterTarget<VRRig>(this, true);
		if (this.currentRopeSwingTarget && this.currentRopeSwingTarget.gameObject)
		{
			UnityEngine.Object.Destroy(this.currentRopeSwingTarget.gameObject);
		}
		this.ClearRopeData();
	}

	// Token: 0x06000BCC RID: 3020 RVA: 0x000675C8 File Offset: 0x000657C8
	public object OnSerializeWrite()
	{
		InputStruct inputStruct = default(InputStruct);
		inputStruct.headRotation = this.head.rigTarget.localRotation;
		inputStruct.rightHandPosition = this.rightHand.rigTarget.localPosition;
		inputStruct.rightHandRotation = this.rightHand.rigTarget.localRotation;
		inputStruct.leftHandPosition = this.leftHand.rigTarget.localPosition;
		inputStruct.leftHandRotation = this.leftHand.rigTarget.localRotation;
		inputStruct.position = base.transform.position;
		inputStruct.roundedRotation = Mathf.RoundToInt(base.transform.rotation.eulerAngles.y);
		inputStruct.handPosition = this.ReturnHandPosition();
		inputStruct.state = this.currentState;
		inputStruct.remoteUseReplacementVoice = this.remoteUseReplacementVoice;
		inputStruct.speakingLoudness = this.speakingLoudness;
		inputStruct.grabbedRopeIndex = this.grabbedRopeIndex;
		if (this.grabbedRopeIndex > 0)
		{
			inputStruct.ropeBoneIndex = this.grabbedRopeBoneIndex;
			inputStruct.ropeGrabIsLeft = this.grabbedRopeIsLeft;
			inputStruct.ropeGrabOffset = this.grabbedRopeOffset;
		}
		double serverTimeStamp = NetworkSystem.Instance.SimTick / 1000.0;
		inputStruct.serverTimeStamp = serverTimeStamp;
		return inputStruct;
	}

	// Token: 0x06000BCD RID: 3021 RVA: 0x0006771C File Offset: 0x0006591C
	public void OnSerializeRead(object objectData)
	{
		InputStruct inputStruct = (InputStruct)objectData;
		this.head.syncRotation = this.SanitizeQuaternion(inputStruct.headRotation);
		this.rightHand.syncPos = this.SanitizeVector3(inputStruct.rightHandPosition);
		this.rightHand.syncRotation = this.SanitizeQuaternion(inputStruct.rightHandRotation);
		this.leftHand.syncPos = this.SanitizeVector3(inputStruct.leftHandPosition);
		this.leftHand.syncRotation = this.SanitizeQuaternion(inputStruct.leftHandRotation);
		this.syncPos = this.SanitizeVector3(inputStruct.position);
		this.syncRotation.eulerAngles = this.SanitizeVector3(new Vector3(0f, (float)inputStruct.roundedRotation, 0f));
		this.handSync = inputStruct.handPosition;
		this.currentState = inputStruct.state;
		this.remoteUseReplacementVoice = inputStruct.remoteUseReplacementVoice;
		this.speakingLoudness = inputStruct.speakingLoudness;
		this.UpdateReplacementVoice();
		this.lastPosition = this.syncPos;
		this.grabbedRopeIndex = inputStruct.grabbedRopeIndex;
		if (this.grabbedRopeIndex > 0)
		{
			this.grabbedRopeBoneIndex = inputStruct.ropeBoneIndex;
			this.grabbedRopeIsLeft = inputStruct.ropeGrabIsLeft;
			this.grabbedRopeOffset = this.SanitizeVector3(inputStruct.ropeGrabOffset);
		}
		this.UpdateRopeData();
		this.AddVelocityToQueue(this.syncPos, inputStruct.serverTimeStamp);
	}

	// Token: 0x06000BCE RID: 3022 RVA: 0x00067878 File Offset: 0x00065A78
	private void UpdateRopeData()
	{
		if (this.previousGrabbedRope == this.grabbedRopeIndex && this.previousGrabbedRopeBoneIndex == this.grabbedRopeBoneIndex && this.previousGrabbedRopeWasLeft == this.grabbedRopeIsLeft)
		{
			return;
		}
		this.ClearRopeData();
		if (this.grabbedRopeIndex > 0)
		{
			PhotonView photonView = PhotonView.Find(this.grabbedRopeIndex);
			GorillaRopeSwing gorillaRopeSwing;
			GorillaClimbable gorillaClimbable;
			if (photonView && photonView.TryGetComponent<GorillaRopeSwing>(out gorillaRopeSwing))
			{
				if (this.currentRopeSwingTarget == null || this.currentRopeSwingTarget.gameObject == null)
				{
					this.currentRopeSwingTarget = new GameObject("RopeSwingTarget").transform;
				}
				if (gorillaRopeSwing.AttachRemotePlayer(this.creator.ActorNumber, this.grabbedRopeBoneIndex, this.currentRopeSwingTarget, this.grabbedRopeOffset))
				{
					this.currentRopeSwing = gorillaRopeSwing;
				}
				this.lastRopeGrabTimer = 0f;
			}
			else if (photonView && photonView.TryGetComponent<GorillaClimbable>(out gorillaClimbable))
			{
				this.currentHoldParent = photonView.transform;
			}
		}
		this.shouldLerpToRope = true;
		this.previousGrabbedRope = this.grabbedRopeIndex;
		this.previousGrabbedRopeBoneIndex = this.grabbedRopeBoneIndex;
		this.previousGrabbedRopeWasLeft = this.grabbedRopeIsLeft;
	}

	// Token: 0x06000BCF RID: 3023 RVA: 0x00067998 File Offset: 0x00065B98
	public static void AttachLocalPlayerToPhotonView(PhotonView view, XRNode xrNode, Vector3 offset, Vector3 velocity)
	{
		if (GorillaTagger.hasInstance && GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIndex = view.ViewID;
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIsLeft = (xrNode == XRNode.LeftHand);
			GorillaTagger.Instance.offlineVRRig.grabbedRopeOffset = offset;
		}
	}

	// Token: 0x06000BD0 RID: 3024 RVA: 0x00022B4B File Offset: 0x00020D4B
	public static void DetachLocalPlayerFromPhotonView()
	{
		if (GorillaTagger.hasInstance && GorillaTagger.Instance.offlineVRRig)
		{
			GorillaTagger.Instance.offlineVRRig.grabbedRopeIndex = -1;
		}
	}

	// Token: 0x06000BD1 RID: 3025 RVA: 0x000679F8 File Offset: 0x00065BF8
	private void ClearRopeData()
	{
		if (this.currentRopeSwing)
		{
			this.currentRopeSwing.DetachRemotePlayer(this.creator.ActorNumber);
		}
		if (this.currentRopeSwingTarget)
		{
			this.currentRopeSwingTarget.SetParent(null);
		}
		this.currentRopeSwing = null;
		this.currentHoldParent = null;
	}

	// Token: 0x06000BD2 RID: 3026 RVA: 0x00022B75 File Offset: 0x00020D75
	public void ChangeMaterial(int materialIndex, PhotonMessageInfo info)
	{
		if (info.Sender == PhotonNetwork.MasterClient)
		{
			this.ChangeMaterialLocal(materialIndex);
		}
	}

	// Token: 0x06000BD3 RID: 3027 RVA: 0x00067A50 File Offset: 0x00065C50
	public void ChangeMaterialLocal(int materialIndex)
	{
		Debug.Log("ChangeMatLocal");
		this.setMatIndex = materialIndex;
		if (this.setMatIndex > -1 && this.setMatIndex < this.materialsToChangeTo.Length)
		{
			this.mainSkin.material = this.materialsToChangeTo[this.setMatIndex];
		}
		if (this.lavaParticleSystem != null)
		{
			if (!this.isOfflineVRRig && materialIndex == 2 && this.lavaParticleSystem.isStopped)
			{
				this.lavaParticleSystem.Play();
			}
			else if (!this.isOfflineVRRig && this.lavaParticleSystem.isPlaying)
			{
				this.lavaParticleSystem.Stop();
			}
		}
		if (this.rockParticleSystem != null)
		{
			if (!this.isOfflineVRRig && materialIndex == 1 && this.rockParticleSystem.isStopped)
			{
				this.rockParticleSystem.Play();
			}
			else if (!this.isOfflineVRRig && this.rockParticleSystem.isPlaying)
			{
				this.rockParticleSystem.Stop();
			}
		}
		if (this.iceParticleSystem != null)
		{
			if (!this.isOfflineVRRig && materialIndex == 3 && this.rockParticleSystem.isStopped)
			{
				this.iceParticleSystem.Play();
				return;
			}
			if (!this.isOfflineVRRig && this.iceParticleSystem.isPlaying)
			{
				this.iceParticleSystem.Stop();
			}
		}
	}

	// Token: 0x06000BD4 RID: 3028 RVA: 0x00067B9C File Offset: 0x00065D9C
	public void InitializeNoobMaterial(float red, float green, float blue, PhotonMessageInfoWrapped info)
	{
		this.IncrementRPC(info, "InitializeNoobMaterial");
		NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
		Debug.Log("InitNoobMat senderID from info is " + info.senderID.ToString() + ". My ID is " + NetworkSystem.Instance.LocalPlayerID.ToString());
		Debug.Log("Rig ID = " + NetworkSystem.Instance.GetOwningPlayerID(this.rigSerializer.gameObject).ToString());
		string userID = NetworkSystem.Instance.GetUserID(info.senderID);
		Debug.Log(info.senderID == NetworkSystem.Instance.GetOwningPlayerID(this.rigSerializer.gameObject));
		Debug.Log(!this.initialized);
		Debug.Log(GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(userID));
		if (info.senderID == NetworkSystem.Instance.GetOwningPlayerID(this.rigSerializer.gameObject) && (!this.initialized || (this.initialized && GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(userID))))
		{
			this.initialized = true;
			red = Mathf.Clamp(red, 0f, 1f);
			green = Mathf.Clamp(green, 0f, 1f);
			blue = Mathf.Clamp(blue, 0f, 1f);
			Debug.Log(string.Concat(new string[]
			{
				"Setting colour values to: red - ",
				red.ToString(),
				", green - ",
				green.ToString(),
				" blue - ",
				blue.ToString()
			}));
			this.InitializeNoobMaterialLocal(red, green, blue);
			return;
		}
		Debug.Log("inappropriate tag data being sent init noob");
		GorillaNot.instance.SendReport("inappropriate tag data being sent init noob", player.UserId, player.NickName);
	}

	// Token: 0x06000BD5 RID: 3029 RVA: 0x00067D98 File Offset: 0x00065F98
	public void InitializeNoobMaterialLocal(float red, float green, float blue)
	{
		Color color = new Color(red, green, blue);
		this.EnsureInstantiatedMaterial();
		if (this.myDefaultSkinMaterialInstance != null)
		{
			color.r = Mathf.Clamp(color.r, 0.16f, 1f);
			color.g = Mathf.Clamp(color.g, 0.16f, 1f);
			color.b = Mathf.Clamp(color.b, 0.16f, 1f);
			this.myDefaultSkinMaterialInstance.color = color;
		}
		if (this.rigSerializer != null)
		{
			string nickName = this.OwningNetPlayer.NickName;
			this.playerText.text = this.NormalizeName(true, nickName);
		}
		else if (this.showName)
		{
			this.playerText.text = NetworkSystem.Instance.GetMyNickName();
		}
		Debug.Log("Set Color");
		this.SetColor(color);
	}

	// Token: 0x06000BD6 RID: 3030 RVA: 0x00067E80 File Offset: 0x00066080
	public string NormalizeName(bool doIt, string text)
	{
		if (doIt)
		{
			if (GorillaComputer.instance.CheckAutoBanListForName(text))
			{
				text = new string(Array.FindAll<char>(text.ToCharArray(), (char c) => char.IsLetterOrDigit(c)));
				if (text.Length > 12)
				{
					text = text.Substring(0, 11);
				}
				text = text.ToUpper();
			}
			else
			{
				text = "BADGORILLA";
			}
		}
		return text;
	}

	// Token: 0x06000BD7 RID: 3031 RVA: 0x00022B8B File Offset: 0x00020D8B
	public void SetJumpLimitLocal(float maxJumpSpeed)
	{
		GorillaLocomotion.Player.Instance.maxJumpSpeed = maxJumpSpeed;
	}

	// Token: 0x06000BD8 RID: 3032 RVA: 0x00022B98 File Offset: 0x00020D98
	public void SetJumpMultiplierLocal(float jumpMultiplier)
	{
		GorillaLocomotion.Player.Instance.jumpMultiplier = jumpMultiplier;
	}

	// Token: 0x06000BD9 RID: 3033 RVA: 0x00067EF8 File Offset: 0x000660F8
	[PunRPC]
	public void RequestMaterialColor(int askingPlayerID, PhotonMessageInfoWrapped info)
	{
		Debug.Log("Request Mat Color from rig");
		this.IncrementRPC(info, "RequestMaterialColor");
		Photon.Realtime.Player playerRef = ((PunNetPlayer)NetworkSystem.Instance.GetPlayer(info.senderID)).playerRef;
		if (this.photonView.IsMine)
		{
			this.photonView.RPC("InitializeNoobMaterial", playerRef, new object[]
			{
				this.myDefaultSkinMaterialInstance.color.r,
				this.myDefaultSkinMaterialInstance.color.g,
				this.myDefaultSkinMaterialInstance.color.b
			});
		}
	}

	// Token: 0x06000BDA RID: 3034 RVA: 0x00067FA4 File Offset: 0x000661A4
	public void RequestCosmetics(PhotonMessageInfo info)
	{
		this.IncrementRPC(info, "RequestCosmetics");
		if (this.photonView.IsMine && CosmeticsController.instance != null)
		{
			string[] array = CosmeticsController.instance.currentWornSet.ToDisplayNameArray();
			string[] array2 = CosmeticsController.instance.tryOnSet.ToDisplayNameArray();
			this.photonView.RPC("UpdateCosmeticsWithTryon", info.Sender, new object[]
			{
				array,
				array2
			});
		}
	}

	// Token: 0x06000BDB RID: 3035 RVA: 0x00022BA5 File Offset: 0x00020DA5
	public void PlayTagSoundLocal(int soundIndex, float soundVolume)
	{
		if (soundIndex < 0 || soundIndex >= this.clipToPlay.Length)
		{
			return;
		}
		this.tagSound.volume = Mathf.Min(0.25f, soundVolume);
		this.tagSound.PlayOneShot(this.clipToPlay[soundIndex]);
	}

	// Token: 0x06000BDC RID: 3036 RVA: 0x00068024 File Offset: 0x00066224
	public void Bonk(int soundIndex, float bonkPercent, PhotonMessageInfo info)
	{
		if (info.Sender == this.photonView.Owner)
		{
			if (this.bonkTime + this.bonkCooldown < Time.time)
			{
				this.bonkTime = Time.time;
				this.tagSound.volume = bonkPercent * 0.25f;
				this.tagSound.PlayOneShot(this.clipToPlay[soundIndex]);
				if (this.photonView.IsMine)
				{
					GorillaTagger.Instance.StartVibration(true, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
					GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
					return;
				}
			}
		}
		else
		{
			GorillaNot.instance.SendReport("inappropriate tag data being sent bonk", info.Sender.UserId, info.Sender.NickName);
		}
	}

	// Token: 0x06000BDD RID: 3037 RVA: 0x00068104 File Offset: 0x00066304
	public void PlayDrum(int drumIndex, float drumVolume, PhotonMessageInfo info)
	{
		this.IncrementRPC(info, "PlayDrum");
		this.senderRig = GorillaGameManager.StaticFindRigForPlayer(info.Sender);
		if (this.senderRig == null || this.senderRig.muted)
		{
			return;
		}
		if (drumIndex < 0 || drumIndex >= this.musicDrums.Length || (this.senderRig.transform.position - base.transform.position).sqrMagnitude > 9f || !float.IsFinite(drumVolume))
		{
			GorillaNot.instance.SendReport("inappropriate tag data being sent drum", info.Sender.UserId, info.Sender.NickName);
			return;
		}
		AudioSource audioSource = this.photonView.IsMine ? GorillaTagger.Instance.offlineVRRig.musicDrums[drumIndex] : this.musicDrums[drumIndex];
		if (!audioSource.gameObject.activeSelf)
		{
			return;
		}
		float instrumentVolume = GorillaComputer.instance.instrumentVolume;
		audioSource.time = 0f;
		audioSource.volume = Mathf.Max(Mathf.Min(instrumentVolume, drumVolume * instrumentVolume), 0f);
		audioSource.Play();
	}

	// Token: 0x06000BDE RID: 3038 RVA: 0x00068228 File Offset: 0x00066428
	public void PlaySelfOnlyInstrument(int selfOnlyIndex, int noteIndex, float instrumentVol, PhotonMessageInfo info)
	{
		this.IncrementRPC(info, "PlaySelfOnlyInstrument");
		if (info.Sender == this.photonView.Owner && !this.muted)
		{
			if (selfOnlyIndex >= 0 && selfOnlyIndex < this.instrumentSelfOnly.Length && info.Sender == this.photonView.Owner && float.IsFinite(instrumentVol))
			{
				if (this.instrumentSelfOnly[selfOnlyIndex].gameObject.activeSelf)
				{
					this.instrumentSelfOnly[selfOnlyIndex].PlayNote(noteIndex, Mathf.Max(Mathf.Min(GorillaComputer.instance.instrumentVolume, instrumentVol * GorillaComputer.instance.instrumentVolume), 0f) / 2f);
					return;
				}
			}
			else
			{
				GorillaNot.instance.SendReport("inappropriate tag data being sent self only instrument", info.Sender.UserId, info.Sender.NickName);
			}
		}
	}

	// Token: 0x06000BDF RID: 3039 RVA: 0x0006830C File Offset: 0x0006650C
	public void PlayHandTapLocal(int soundIndex, bool isLeftHand, float tapVolume)
	{
		if (soundIndex > -1 && soundIndex < GorillaLocomotion.Player.Instance.materialData.Count)
		{
			if (isLeftHand)
			{
				this.leftHandPlayer.volume = tapVolume;
				this.leftHandPlayer.clip = (GorillaLocomotion.Player.Instance.materialData[soundIndex].overrideAudio ? GorillaLocomotion.Player.Instance.materialData[soundIndex].audio : GorillaLocomotion.Player.Instance.materialData[0].audio);
				this.leftHandPlayer.PlayOneShot(this.leftHandPlayer.clip);
				return;
			}
			this.rightHandPlayer.volume = tapVolume;
			this.rightHandPlayer.clip = (GorillaLocomotion.Player.Instance.materialData[soundIndex].overrideAudio ? GorillaLocomotion.Player.Instance.materialData[soundIndex].audio : GorillaLocomotion.Player.Instance.materialData[0].audio);
			this.rightHandPlayer.PlayOneShot(this.rightHandPlayer.clip);
		}
	}

	// Token: 0x06000BE0 RID: 3040 RVA: 0x0006841C File Offset: 0x0006661C
	public void PlaySplashEffect(Vector3 splashPosition, Quaternion splashRotation, float splashScale, float boundingRadius, bool bigSplash, bool enteringWater, PhotonMessageInfo info)
	{
		this.IncrementRPC(info, "PlaySplashEffect");
		if (info.Sender == this.photonView.Owner && splashPosition.IsValid() && splashRotation.IsValid() && float.IsFinite(splashScale) && float.IsFinite(boundingRadius))
		{
			if ((base.transform.position - splashPosition).sqrMagnitude < 9f)
			{
				float time = Time.time;
				int num = -1;
				float num2 = time + 10f;
				for (int i = 0; i < this.splashEffectTimes.Length; i++)
				{
					if (this.splashEffectTimes[i] < num2)
					{
						num2 = this.splashEffectTimes[i];
						num = i;
					}
				}
				if (time - 0.5f > num2)
				{
					this.splashEffectTimes[num] = time;
					boundingRadius = Mathf.Clamp(boundingRadius, 0.0001f, 0.5f);
					ObjectPools.instance.Instantiate(GorillaLocomotion.Player.Instance.waterParams.rippleEffect, splashPosition, splashRotation, GorillaLocomotion.Player.Instance.waterParams.rippleEffectScale * boundingRadius * 2f);
					splashScale = Mathf.Clamp(splashScale, 1E-05f, 1f);
					ObjectPools.instance.Instantiate(GorillaLocomotion.Player.Instance.waterParams.splashEffect, splashPosition, splashRotation, splashScale).GetComponent<WaterSplashEffect>().PlayEffect(bigSplash, enteringWater, splashScale, null);
					return;
				}
			}
		}
		else
		{
			GorillaNot.instance.SendReport("inappropriate tag data being sent splash effect", info.Sender.UserId, info.Sender.NickName);
		}
	}

	// Token: 0x06000BE1 RID: 3041 RVA: 0x000685A4 File Offset: 0x000667A4
	[PunRPC]
	public void EnableNonCosmeticHandItemRPC(bool enable, bool isLeftHand, PhotonMessageInfo info)
	{
		this.IncrementRPC(info, "EnableNonCosmeticHandItem");
		if (info.Sender == this.photonView.Owner)
		{
			this.senderRig = GorillaGameManager.StaticFindRigForPlayer(info.Sender);
			if (this.senderRig == null)
			{
				return;
			}
			if (isLeftHand && this.nonCosmeticLeftHandItem)
			{
				this.senderRig.nonCosmeticLeftHandItem.EnableItem(enable);
				return;
			}
			if (!isLeftHand && this.nonCosmeticRightHandItem)
			{
				this.senderRig.nonCosmeticRightHandItem.EnableItem(enable);
				return;
			}
		}
		else
		{
			GorillaNot.instance.SendReport("inappropriate tag data being sent Enable Non Cosmetic Hand Item", info.Sender.UserId, info.Sender.NickName);
		}
	}

	// Token: 0x06000BE2 RID: 3042 RVA: 0x0006865C File Offset: 0x0006685C
	public void PlayGeodeEffect(Vector3 hitPosition)
	{
		if ((base.transform.position - hitPosition).sqrMagnitude < 9f && this.geodeCrackingSound)
		{
			this.geodeCrackingSound.Play();
		}
	}

	// Token: 0x06000BE3 RID: 3043 RVA: 0x000686A4 File Offset: 0x000668A4
	public void PlayClimbSound(AudioClip clip, bool isLeftHand)
	{
		if (isLeftHand)
		{
			this.leftHandPlayer.volume = 0.1f;
			this.leftHandPlayer.clip = clip;
			this.leftHandPlayer.PlayOneShot(this.leftHandPlayer.clip);
			return;
		}
		this.rightHandPlayer.volume = 0.1f;
		this.rightHandPlayer.clip = clip;
		this.rightHandPlayer.PlayOneShot(this.rightHandPlayer.clip);
	}

	// Token: 0x06000BE4 RID: 3044 RVA: 0x0006871C File Offset: 0x0006691C
	public void UpdateCosmetics(string[] currentItems, PhotonMessageInfo info)
	{
		this.IncrementRPC(info, "UpdateCosmetics");
		if (info.Sender == this.photonView.Owner)
		{
			CosmeticsController.CosmeticSet newSet = new CosmeticsController.CosmeticSet(currentItems, CosmeticsController.instance);
			this.LocalUpdateCosmetics(newSet);
			return;
		}
		GorillaNot.instance.SendReport("inappropriate tag data being sent update cosmetics", info.Sender.UserId, info.Sender.NickName);
	}

	// Token: 0x06000BE5 RID: 3045 RVA: 0x00068788 File Offset: 0x00066988
	public void UpdateCosmeticsWithTryon(string[] currentItems, string[] tryOnItems, PhotonMessageInfo info)
	{
		this.IncrementRPC(info, "UpdateCosmeticsWithTryon");
		if (info.Sender == this.photonView.Owner)
		{
			CosmeticsController.CosmeticSet newSet = new CosmeticsController.CosmeticSet(currentItems, CosmeticsController.instance);
			CosmeticsController.CosmeticSet newTryOnSet = new CosmeticsController.CosmeticSet(tryOnItems, CosmeticsController.instance);
			this.LocalUpdateCosmeticsWithTryon(newSet, newTryOnSet);
			return;
		}
		GorillaNot.instance.SendReport("inappropriate tag data being sent update cosmetics with tryon", info.Sender.UserId, info.Sender.NickName);
	}

	// Token: 0x06000BE6 RID: 3046 RVA: 0x00022BE0 File Offset: 0x00020DE0
	public void LocalUpdateCosmetics(CosmeticsController.CosmeticSet newSet)
	{
		this.cosmeticSet = newSet;
		if (this.initializedCosmetics)
		{
			this.SetCosmeticsActive();
		}
	}

	// Token: 0x06000BE7 RID: 3047 RVA: 0x00022BF7 File Offset: 0x00020DF7
	public void LocalUpdateCosmeticsWithTryon(CosmeticsController.CosmeticSet newSet, CosmeticsController.CosmeticSet newTryOnSet)
	{
		this.cosmeticSet = newSet;
		this.tryOnSet = newTryOnSet;
		if (this.initializedCosmetics)
		{
			this.SetCosmeticsActive();
		}
	}

	// Token: 0x06000BE8 RID: 3048 RVA: 0x00022C15 File Offset: 0x00020E15
	private void CheckForEarlyAccess()
	{
		if (this.concatStringOfCosmeticsAllowed.Contains("Early Access Supporter Pack"))
		{
			this.concatStringOfCosmeticsAllowed += "LBAAE.LFAAM.LFAAN.LHAAA.LHAAK.LHAAL.LHAAM.LHAAN.LHAAO.LHAAP.LHABA.LHABB.";
		}
		this.initializedCosmetics = true;
	}

	// Token: 0x06000BE9 RID: 3049 RVA: 0x00068800 File Offset: 0x00066A00
	public void SetCosmeticsActive()
	{
		if (CosmeticsController.instance == null)
		{
			return;
		}
		this.prevSet.CopyItems(this.mergedSet);
		this.mergedSet.MergeSets(this.inTryOnRoom ? this.tryOnSet : null, this.cosmeticSet);
		BodyDockPositions component = base.GetComponent<BodyDockPositions>();
		this.mergedSet.ActivateCosmetics(this.prevSet, this, component, CosmeticsController.instance.nullItem, this.cosmeticsObjectRegistry);
	}

	// Token: 0x06000BEA RID: 3050 RVA: 0x0006887C File Offset: 0x00066A7C
	public void GetUserCosmeticsAllowed()
	{
		if (CosmeticsController.instance != null)
		{
			PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), delegate(GetUserInventoryResult result)
			{
				foreach (ItemInstance itemInstance in result.Inventory)
				{
					if (itemInstance.CatalogVersion == CosmeticsController.instance.catalog)
					{
						this.concatStringOfCosmeticsAllowed += itemInstance.ItemId;
					}
				}
				Debug.Log("successful result. allowed cosmetics are: " + this.concatStringOfCosmeticsAllowed);
				this.CheckForEarlyAccess();
				this.SetCosmeticsActive();
			}, delegate(PlayFabError error)
			{
				Debug.Log("Got error retrieving user data:");
				Debug.Log(error.GenerateErrorReport());
				this.initializedCosmetics = true;
				this.SetCosmeticsActive();
			}, null, null);
		}
		this.concatStringOfCosmeticsAllowed += "Slingshot";
	}

	// Token: 0x06000BEB RID: 3051 RVA: 0x000688D4 File Offset: 0x00066AD4
	public void GenerateFingerAngleLookupTables()
	{
		this.GenerateTableIndex(ref this.leftIndex);
		this.GenerateTableIndex(ref this.rightIndex);
		this.GenerateTableMiddle(ref this.leftMiddle);
		this.GenerateTableMiddle(ref this.rightMiddle);
		this.GenerateTableThumb(ref this.leftThumb);
		this.GenerateTableThumb(ref this.rightThumb);
	}

	// Token: 0x06000BEC RID: 3052 RVA: 0x0006892C File Offset: 0x00066B2C
	private void GenerateTableThumb(ref VRMapThumb thumb)
	{
		thumb.angle1Table = new Quaternion[11];
		thumb.angle2Table = new Quaternion[11];
		for (int i = 0; i < thumb.angle1Table.Length; i++)
		{
			Debug.Log((float)i / 10f);
			thumb.angle1Table[i] = Quaternion.Lerp(Quaternion.Euler(thumb.startingAngle1), Quaternion.Euler(thumb.closedAngle1), (float)i / 10f);
			thumb.angle2Table[i] = Quaternion.Lerp(Quaternion.Euler(thumb.startingAngle2), Quaternion.Euler(thumb.closedAngle2), (float)i / 10f);
		}
	}

	// Token: 0x06000BED RID: 3053 RVA: 0x000689E4 File Offset: 0x00066BE4
	private void GenerateTableIndex(ref VRMapIndex index)
	{
		index.angle1Table = new Quaternion[11];
		index.angle2Table = new Quaternion[11];
		index.angle3Table = new Quaternion[11];
		for (int i = 0; i < index.angle1Table.Length; i++)
		{
			index.angle1Table[i] = Quaternion.Lerp(Quaternion.Euler(index.startingAngle1), Quaternion.Euler(index.closedAngle1), (float)i / 10f);
			index.angle2Table[i] = Quaternion.Lerp(Quaternion.Euler(index.startingAngle2), Quaternion.Euler(index.closedAngle2), (float)i / 10f);
			index.angle3Table[i] = Quaternion.Lerp(Quaternion.Euler(index.startingAngle3), Quaternion.Euler(index.closedAngle3), (float)i / 10f);
		}
	}

	// Token: 0x06000BEE RID: 3054 RVA: 0x00068ACC File Offset: 0x00066CCC
	private void GenerateTableMiddle(ref VRMapMiddle middle)
	{
		middle.angle1Table = new Quaternion[11];
		middle.angle2Table = new Quaternion[11];
		middle.angle3Table = new Quaternion[11];
		for (int i = 0; i < middle.angle1Table.Length; i++)
		{
			middle.angle1Table[i] = Quaternion.Lerp(Quaternion.Euler(middle.startingAngle1), Quaternion.Euler(middle.closedAngle1), (float)i / 10f);
			middle.angle2Table[i] = Quaternion.Lerp(Quaternion.Euler(middle.startingAngle2), Quaternion.Euler(middle.closedAngle2), (float)i / 10f);
			middle.angle3Table[i] = Quaternion.Lerp(Quaternion.Euler(middle.startingAngle3), Quaternion.Euler(middle.closedAngle3), (float)i / 10f);
		}
	}

	// Token: 0x06000BEF RID: 3055 RVA: 0x00068BB4 File Offset: 0x00066DB4
	private Quaternion SanitizeQuaternion(Quaternion quat)
	{
		if (float.IsNaN(quat.w) || float.IsNaN(quat.x) || float.IsNaN(quat.y) || float.IsNaN(quat.z) || float.IsInfinity(quat.w) || float.IsInfinity(quat.x) || float.IsInfinity(quat.y) || float.IsInfinity(quat.z))
		{
			return Quaternion.identity;
		}
		return quat;
	}

	// Token: 0x06000BF0 RID: 3056 RVA: 0x00068C30 File Offset: 0x00066E30
	private Vector3 SanitizeVector3(Vector3 vec)
	{
		if (float.IsNaN(vec.x) || float.IsNaN(vec.y) || float.IsNaN(vec.z) || float.IsInfinity(vec.x) || float.IsInfinity(vec.y) || float.IsInfinity(vec.z))
		{
			return Vector3.zero;
		}
		return Vector3.ClampMagnitude(vec, 1000f);
	}

	// Token: 0x06000BF1 RID: 3057 RVA: 0x00022C46 File Offset: 0x00020E46
	private void IncrementRPC(PhotonMessageInfoWrapped info, string sourceCall)
	{
		if (GorillaGameManager.instance != null)
		{
			GorillaNot.IncrementRPCCall(info, sourceCall);
		}
	}

	// Token: 0x06000BF2 RID: 3058 RVA: 0x00022C5C File Offset: 0x00020E5C
	private void IncrementRPC(PhotonMessageInfo info, string sourceCall)
	{
		if (GorillaGameManager.instance != null)
		{
			GorillaNot.IncrementRPCCall(info, sourceCall);
		}
	}

	// Token: 0x06000BF3 RID: 3059 RVA: 0x00068C9C File Offset: 0x00066E9C
	private void AddVelocityToQueue(Vector3 position, double serverTime)
	{
		Vector3 velocity;
		if (this.velocityHistoryList.Count == 0)
		{
			velocity = Vector3.zero;
			this.lastPosition = position;
		}
		else
		{
			velocity = (position - this.lastPosition) / (float)(serverTime - this.velocityHistoryList[0].time);
		}
		this.velocityHistoryList.Insert(0, new VRRig.VelocityTime(velocity, serverTime));
		if (this.velocityHistoryList.Count > this.velocityHistoryMaxLength)
		{
			this.velocityHistoryList.RemoveRange(this.velocityHistoryMaxLength, this.velocityHistoryList.Count - this.velocityHistoryMaxLength);
		}
	}

	// Token: 0x06000BF4 RID: 3060 RVA: 0x00068D34 File Offset: 0x00066F34
	private Vector3 ReturnVelocityAtTime(double timeToReturn)
	{
		if (this.velocityHistoryList.Count <= 1)
		{
			return Vector3.zero;
		}
		int num = 0;
		int num2 = this.velocityHistoryList.Count - 1;
		int num3 = 0;
		if (num2 == num)
		{
			return this.velocityHistoryList[num].vel;
		}
		while (num2 - num > 1 && num3 < 1000)
		{
			num3++;
			int num4 = (num2 - num) / 2;
			if (this.velocityHistoryList[num4].time > timeToReturn)
			{
				num2 = num4;
			}
			else
			{
				num = num4;
			}
		}
		float num5 = (float)(this.velocityHistoryList[num].time - timeToReturn);
		double num6 = this.velocityHistoryList[num].time - this.velocityHistoryList[num2].time;
		if (num6 == 0.0)
		{
			num6 = 0.001;
		}
		num5 /= (float)num6;
		num5 = Mathf.Clamp(num5, 0f, 1f);
		return Vector3.Lerp(this.velocityHistoryList[num].vel, this.velocityHistoryList[num2].vel, num5);
	}

	// Token: 0x06000BF5 RID: 3061 RVA: 0x00022C72 File Offset: 0x00020E72
	public bool CheckDistance(Vector3 position, float max)
	{
		max = max * max * this.scaleFactor;
		return Vector3.SqrMagnitude(this.syncPos - position) < max;
	}

	// Token: 0x06000BF6 RID: 3062 RVA: 0x00068E48 File Offset: 0x00067048
	public void SetColor(Color color)
	{
		Action<Color> action = this.onColorInitialized;
		if (action != null)
		{
			action(color);
		}
		this.onColorInitialized = delegate(Color color1)
		{
		};
		this.colorInitialized = true;
		this.playerColor = color;
	}

	// Token: 0x06000BF7 RID: 3063 RVA: 0x00022C94 File Offset: 0x00020E94
	public void OnColorInitialized(Action<Color> action)
	{
		if (this.colorInitialized)
		{
			action(this.playerColor);
			return;
		}
		this.onColorInitialized = (Action<Color>)Delegate.Combine(this.onColorInitialized, action);
	}

	// Token: 0x06000BF8 RID: 3064 RVA: 0x00022CC2 File Offset: 0x00020EC2
	private void OnEnable()
	{
		if (this.currentRopeSwingTarget != null)
		{
			this.currentRopeSwingTarget.SetParent(null);
		}
		if (!this.isOfflineVRRig)
		{
			PlayerCosmeticsSystem.RegisterCosmeticCallback(this.creator.ActorNumber, this);
		}
	}

	// Token: 0x06000BF9 RID: 3065 RVA: 0x00068E9C File Offset: 0x0006709C
	void IPreDisable.PreDisable()
	{
		this.ClearRopeData();
		if (this.currentRopeSwingTarget)
		{
			this.currentRopeSwingTarget.SetParent(base.transform);
		}
		this.EnableHuntWatch(false);
		this.EnableBattleCosmetics(false);
		this.concatStringOfCosmeticsAllowed = "";
		this.rawCosmeticString = "";
		if (this.cosmeticSet != null)
		{
			this.mergedSet.DeactivateAllCosmetcs(this.myBodyDockPositions, CosmeticsController.instance.nullItem, this.cosmeticsObjectRegistry);
			this.mergedSet.ClearSet(CosmeticsController.instance.nullItem);
			this.prevSet.ClearSet(CosmeticsController.instance.nullItem);
			this.tryOnSet.ClearSet(CosmeticsController.instance.nullItem);
			this.cosmeticSet.ClearSet(CosmeticsController.instance.nullItem);
		}
		if (!this.isOfflineVRRig)
		{
			PlayerCosmeticsSystem.RemoveCosmeticCallback(this.creator.ActorNumber);
			this.pendingCosmeticUpdate = true;
		}
	}

	// Token: 0x06000BFA RID: 3066 RVA: 0x00068F98 File Offset: 0x00067198
	private void OnDisable()
	{
		Debug.Log("ON DISABLE!");
		this.initialized = false;
		this.muted = false;
		this.photonView = null;
		this.voiceAudio = null;
		this.tempRig = null;
		this.timeSpawned = 0f;
		this.initializedCosmetics = false;
		this.velocityHistoryList.Clear();
		this.tempMatIndex = 0;
		this.setMatIndex = 0;
		this.ChangeMaterialLocal(this.setMatIndex);
		this.currentCosmeticTries = 0;
		this.creator = null;
		Debug.Log("ON DISABLE! Finished cleanup" + (this.photonView == null).ToString());
		try
		{
			CallLimitType<CallLimiter>[] callSettings = this.fxSettings.callSettings;
			for (int i = 0; i < callSettings.Length; i++)
			{
				callSettings[i].CallLimitSettings.Reset();
			}
		}
		catch
		{
			Debug.LogError("fxtype missing in fxSettings, please fix or remove this");
		}
	}

	// Token: 0x06000BFB RID: 3067 RVA: 0x00069080 File Offset: 0x00067280
	public void NetInitialize()
	{
		this.timeSpawned = Time.time;
		if (NetworkSystem.Instance.InRoom)
		{
			GorillaGameManager instance = GorillaGameManager.instance;
			if (instance != null)
			{
				if (instance is GorillaHuntManager || instance.GameModeName() == "HUNT")
				{
					this.EnableHuntWatch(true);
				}
				else if (instance is GorillaBattleManager || instance.GameModeName() == "BATTLE")
				{
					this.EnableBattleCosmetics(true);
				}
			}
			else
			{
				string gameModeString = NetworkSystem.Instance.GameModeString;
				if (!gameModeString.IsNullOrEmpty())
				{
					string text = gameModeString;
					if (text.Contains("HUNT"))
					{
						this.EnableHuntWatch(true);
					}
					else if (text.Contains("BATTLE"))
					{
						this.EnableBattleCosmetics(true);
					}
				}
			}
		}
		if (this.photonView != null)
		{
			base.transform.position = this.photonView.gameObject.transform.position;
			base.transform.rotation = this.photonView.gameObject.transform.rotation;
		}
		try
		{
			Action action = VRRig.newPlayerJoined;
			if (action != null)
			{
				action();
			}
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	// Token: 0x06000BFC RID: 3068 RVA: 0x00022CF7 File Offset: 0x00020EF7
	public void EnableHuntWatch(bool on)
	{
		this.huntComputer.SetActive(on);
	}

	// Token: 0x06000BFD RID: 3069 RVA: 0x00022D05 File Offset: 0x00020F05
	public void EnableBattleCosmetics(bool on)
	{
		this.battleBalloons.gameObject.SetActive(on);
	}

	// Token: 0x06000BFE RID: 3070 RVA: 0x000691B4 File Offset: 0x000673B4
	private void UpdateReplacementVoice()
	{
		if (this.remoteUseReplacementVoice || this.localUseReplacementVoice || GorillaComputer.instance.voiceChatOn != "TRUE")
		{
			this.voiceAudio.mute = true;
			return;
		}
		this.voiceAudio.mute = false;
	}

	// Token: 0x06000BFF RID: 3071 RVA: 0x00069204 File Offset: 0x00067404
	public bool ShouldPlayReplacementVoice()
	{
		return this.photonView && !this.photonView.IsMine && !(GorillaComputer.instance.voiceChatOn == "OFF") && (this.remoteUseReplacementVoice || this.localUseReplacementVoice || GorillaComputer.instance.voiceChatOn == "FALSE") && this.speakingLoudness > this.replacementVoiceLoudnessThreshold;
	}

	// Token: 0x17000177 RID: 375
	// (get) Token: 0x06000C00 RID: 3072 RVA: 0x00022D18 File Offset: 0x00020F18
	// (set) Token: 0x06000C01 RID: 3073 RVA: 0x00022D20 File Offset: 0x00020F20
	bool IUserCosmeticsCallback.PendingUpdate
	{
		get
		{
			return this.pendingCosmeticUpdate;
		}
		set
		{
			this.pendingCosmeticUpdate = value;
		}
	}

	// Token: 0x06000C02 RID: 3074 RVA: 0x00069280 File Offset: 0x00067480
	bool IUserCosmeticsCallback.OnGetUserCosmetics(string cosmetics)
	{
		if (cosmetics == this.rawCosmeticString && this.currentCosmeticTries < this.cosmeticRetries)
		{
			this.currentCosmeticTries++;
			return false;
		}
		this.rawCosmeticString = (cosmetics ?? "");
		this.concatStringOfCosmeticsAllowed = this.rawCosmeticString;
		this.initializedCosmetics = true;
		this.currentCosmeticTries = 0;
		this.CheckForEarlyAccess();
		this.SetCosmeticsActive();
		this.myBodyDockPositions.RefreshTransferrableItems();
		PhotonView photonView = this.photonView;
		if (photonView != null)
		{
			photonView.RPC("RequestCosmetics", this.photonView.Owner, Array.Empty<object>());
		}
		return true;
	}

	// Token: 0x06000C03 RID: 3075 RVA: 0x00022D29 File Offset: 0x00020F29
	int IGuidedRefObject.GetInstanceID()
	{
		return base.GetInstanceID();
	}

	// Token: 0x06000C04 RID: 3076 RVA: 0x00022D31 File Offset: 0x00020F31
	void IGuidedRefObject.GuidedRefInitialize()
	{
		GuidedRefHub.RegisterTarget<VRRig>(this, this.guidedRefTargetInfo.hubIds, this);
		GuidedRefHub.ReceiverFullyRegistered<VRRig>(this);
	}

	// Token: 0x17000178 RID: 376
	// (get) Token: 0x06000C05 RID: 3077 RVA: 0x00022D4B File Offset: 0x00020F4B
	// (set) Token: 0x06000C06 RID: 3078 RVA: 0x00022D53 File Offset: 0x00020F53
	GuidedRefBasicTargetInfo IGuidedRefTargetMono.GRefTargetInfo
	{
		get
		{
			return this.guidedRefTargetInfo;
		}
		set
		{
			this.guidedRefTargetInfo = value;
		}
	}

	// Token: 0x17000179 RID: 377
	// (get) Token: 0x06000C07 RID: 3079 RVA: 0x0001D728 File Offset: 0x0001B928
	UnityEngine.Object IGuidedRefTargetMono.GuidedRefTargetObject
	{
		get
		{
			return this;
		}
	}

	// Token: 0x1700017A RID: 378
	// (get) Token: 0x06000C08 RID: 3080 RVA: 0x00022D5C File Offset: 0x00020F5C
	// (set) Token: 0x06000C09 RID: 3081 RVA: 0x00022D64 File Offset: 0x00020F64
	int IGuidedRefReceiverMono.GuidedRefsWaitingToResolveCount { get; set; }

	// Token: 0x06000C0A RID: 3082 RVA: 0x0001B0D5 File Offset: 0x000192D5
	bool IGuidedRefReceiverMono.GuidedRefTryResolveReference(GuidedRefTryResolveInfo target)
	{
		return false;
	}

	// Token: 0x06000C0B RID: 3083 RVA: 0x0001B2AB File Offset: 0x000194AB
	void IGuidedRefReceiverMono.OnAllGuidedRefsResolved()
	{
	}

	// Token: 0x06000C0C RID: 3084 RVA: 0x0001B2AB File Offset: 0x000194AB
	public void OnGuidedRefTargetDestroyed(int fieldId)
	{
	}

	// Token: 0x06000C0F RID: 3087 RVA: 0x00021147 File Offset: 0x0001F347
	Transform IGuidedRefMonoBehaviour.get_transform()
	{
		return base.transform;
	}

	// Token: 0x04000C66 RID: 3174
	public static Action newPlayerJoined;

	// Token: 0x04000C67 RID: 3175
	public VRMap head;

	// Token: 0x04000C68 RID: 3176
	public VRMap rightHand;

	// Token: 0x04000C69 RID: 3177
	public VRMap leftHand;

	// Token: 0x04000C6A RID: 3178
	public VRMapThumb leftThumb;

	// Token: 0x04000C6B RID: 3179
	public VRMapIndex leftIndex;

	// Token: 0x04000C6C RID: 3180
	public VRMapMiddle leftMiddle;

	// Token: 0x04000C6D RID: 3181
	public VRMapThumb rightThumb;

	// Token: 0x04000C6E RID: 3182
	public VRMapIndex rightIndex;

	// Token: 0x04000C6F RID: 3183
	public VRMapMiddle rightMiddle;

	// Token: 0x04000C70 RID: 3184
	private int previousGrabbedRope = -1;

	// Token: 0x04000C71 RID: 3185
	private int previousGrabbedRopeBoneIndex;

	// Token: 0x04000C72 RID: 3186
	private bool previousGrabbedRopeWasLeft;

	// Token: 0x04000C73 RID: 3187
	private GorillaRopeSwing currentRopeSwing;

	// Token: 0x04000C74 RID: 3188
	private Transform currentHoldParent;

	// Token: 0x04000C75 RID: 3189
	private Transform currentRopeSwingTarget;

	// Token: 0x04000C76 RID: 3190
	private float lastRopeGrabTimer;

	// Token: 0x04000C77 RID: 3191
	private bool shouldLerpToRope;

	// Token: 0x04000C78 RID: 3192
	[NonSerialized]
	public int grabbedRopeIndex = -1;

	// Token: 0x04000C79 RID: 3193
	[NonSerialized]
	public int grabbedRopeBoneIndex;

	// Token: 0x04000C7A RID: 3194
	[NonSerialized]
	public bool grabbedRopeIsLeft;

	// Token: 0x04000C7B RID: 3195
	[NonSerialized]
	public Vector3 grabbedRopeOffset = Vector3.zero;

	// Token: 0x04000C7C RID: 3196
	[Tooltip("- False in 'Gorilla Player Networked.prefab'.\n- True in 'Local VRRig.prefab/Local Gorilla Player'.\n- False in 'Local VRRig.prefab/Actual Gorilla'")]
	public bool isOfflineVRRig;

	// Token: 0x04000C7D RID: 3197
	public GameObject mainCamera;

	// Token: 0x04000C7E RID: 3198
	public Transform playerOffsetTransform;

	// Token: 0x04000C7F RID: 3199
	public int SDKIndex;

	// Token: 0x04000C80 RID: 3200
	public bool isMyPlayer;

	// Token: 0x04000C81 RID: 3201
	public AudioSource leftHandPlayer;

	// Token: 0x04000C82 RID: 3202
	public AudioSource rightHandPlayer;

	// Token: 0x04000C83 RID: 3203
	public AudioSource tagSound;

	// Token: 0x04000C84 RID: 3204
	[SerializeField]
	private float ratio;

	// Token: 0x04000C85 RID: 3205
	public Transform headConstraint;

	// Token: 0x04000C86 RID: 3206
	public Vector3 headBodyOffset = Vector3.zero;

	// Token: 0x04000C87 RID: 3207
	public GameObject headMesh;

	// Token: 0x04000C88 RID: 3208
	public Vector3 syncPos;

	// Token: 0x04000C89 RID: 3209
	public Quaternion syncRotation;

	// Token: 0x04000C8A RID: 3210
	public AudioClip[] clipToPlay;

	// Token: 0x04000C8B RID: 3211
	public AudioClip[] handTapSound;

	// Token: 0x04000C8C RID: 3212
	public int currentMatIndex;

	// Token: 0x04000C8D RID: 3213
	public int setMatIndex;

	// Token: 0x04000C8E RID: 3214
	private int tempMatIndex;

	// Token: 0x04000C8F RID: 3215
	public float lerpValueFingers;

	// Token: 0x04000C90 RID: 3216
	public float lerpValueBody;

	// Token: 0x04000C91 RID: 3217
	public GameObject backpack;

	// Token: 0x04000C92 RID: 3218
	public Transform leftHandTransform;

	// Token: 0x04000C93 RID: 3219
	public Transform rightHandTransform;

	// Token: 0x04000C94 RID: 3220
	public SkinnedMeshRenderer mainSkin;

	// Token: 0x04000C95 RID: 3221
	public GorillaSkin defaultSkin;

	// Token: 0x04000C96 RID: 3222
	public Material myDefaultSkinMaterialInstance;

	// Token: 0x04000C97 RID: 3223
	public GameObject spectatorSkin;

	// Token: 0x04000C98 RID: 3224
	public int handSync;

	// Token: 0x04000C99 RID: 3225
	public Material[] materialsToChangeTo;

	// Token: 0x04000C9A RID: 3226
	public float red;

	// Token: 0x04000C9B RID: 3227
	public float green;

	// Token: 0x04000C9C RID: 3228
	public float blue;

	// Token: 0x04000C9D RID: 3229
	public string playerName;

	// Token: 0x04000C9E RID: 3230
	public Text playerText;

	// Token: 0x04000C9F RID: 3231
	[Tooltip("- True in 'Gorilla Player Networked.prefab'.\n- True in 'Local VRRig.prefab/Local Gorilla Player'.\n- False in 'Local VRRig.prefab/Actual Gorilla'")]
	public bool showName;

	// Token: 0x04000CA0 RID: 3232
	public CosmeticItemRegistry cosmeticsObjectRegistry = new CosmeticItemRegistry();

	// Token: 0x04000CA1 RID: 3233
	public GameObject[] cosmetics;

	// Token: 0x04000CA2 RID: 3234
	public GameObject[] overrideCosmetics;

	// Token: 0x04000CA3 RID: 3235
	public string concatStringOfCosmeticsAllowed = "";

	// Token: 0x04000CA4 RID: 3236
	public bool initializedCosmetics;

	// Token: 0x04000CA5 RID: 3237
	public CosmeticsController.CosmeticSet cosmeticSet;

	// Token: 0x04000CA6 RID: 3238
	public CosmeticsController.CosmeticSet tryOnSet;

	// Token: 0x04000CA7 RID: 3239
	public CosmeticsController.CosmeticSet mergedSet;

	// Token: 0x04000CA8 RID: 3240
	public CosmeticsController.CosmeticSet prevSet;

	// Token: 0x04000CA9 RID: 3241
	private int cosmeticRetries = 2;

	// Token: 0x04000CAA RID: 3242
	private int currentCosmeticTries;

	// Token: 0x04000CAB RID: 3243
	public SizeManager sizeManager;

	// Token: 0x04000CAC RID: 3244
	public float pitchScale = 0.3f;

	// Token: 0x04000CAD RID: 3245
	public float pitchOffset = 1f;

	// Token: 0x04000CAE RID: 3246
	[NonSerialized]
	public bool IsHaunted;

	// Token: 0x04000CAF RID: 3247
	public float HauntedVoicePitch = 0.5f;

	// Token: 0x04000CB0 RID: 3248
	public float HauntedHearingVolume = 0.15f;

	// Token: 0x04000CB1 RID: 3249
	[NonSerialized]
	public bool UsingHauntedRing;

	// Token: 0x04000CB2 RID: 3250
	[NonSerialized]
	public float HauntedRingVoicePitch;

	// Token: 0x04000CB3 RID: 3251
	public NonCosmeticHandItem nonCosmeticLeftHandItem;

	// Token: 0x04000CB4 RID: 3252
	public NonCosmeticHandItem nonCosmeticRightHandItem;

	// Token: 0x04000CB5 RID: 3253
	public VRRigReliableState reliableState;

	// Token: 0x04000CB6 RID: 3254
	internal RigContainer rigContainer;

	// Token: 0x04000CB7 RID: 3255
	public static readonly GTBitOps.BitWriteInfo[] WearablePackedStatesBitWriteInfos = new GTBitOps.BitWriteInfo[]
	{
		new GTBitOps.BitWriteInfo(0, 1),
		new GTBitOps.BitWriteInfo(1, 2),
		new GTBitOps.BitWriteInfo(3, 2)
	};

	// Token: 0x04000CB8 RID: 3256
	public bool inTryOnRoom;

	// Token: 0x04000CB9 RID: 3257
	public bool muted;

	// Token: 0x04000CBA RID: 3258
	public float scaleFactor;

	// Token: 0x04000CBB RID: 3259
	private float timeSpawned;

	// Token: 0x04000CBC RID: 3260
	public float doNotLerpConstant = 1f;

	// Token: 0x04000CBD RID: 3261
	public string tempString;

	// Token: 0x04000CBE RID: 3262
	private Photon.Realtime.Player tempPlayer;

	// Token: 0x04000CBF RID: 3263
	internal Photon.Realtime.Player creator;

	// Token: 0x04000CC0 RID: 3264
	internal NetPlayer creatorWrapped;

	// Token: 0x04000CC1 RID: 3265
	private VRRig tempRig;

	// Token: 0x04000CC2 RID: 3266
	private float[] speedArray;

	// Token: 0x04000CC3 RID: 3267
	private double handLerpValues;

	// Token: 0x04000CC4 RID: 3268
	private bool initialized;

	// Token: 0x04000CC5 RID: 3269
	public BattleBalloons battleBalloons;

	// Token: 0x04000CC6 RID: 3270
	private int tempInt;

	// Token: 0x04000CC7 RID: 3271
	public BodyDockPositions myBodyDockPositions;

	// Token: 0x04000CC8 RID: 3272
	public ParticleSystem lavaParticleSystem;

	// Token: 0x04000CC9 RID: 3273
	public ParticleSystem rockParticleSystem;

	// Token: 0x04000CCA RID: 3274
	public ParticleSystem iceParticleSystem;

	// Token: 0x04000CCB RID: 3275
	public string tempItemName;

	// Token: 0x04000CCC RID: 3276
	public CosmeticsController.CosmeticItem tempItem;

	// Token: 0x04000CCD RID: 3277
	public string tempItemId;

	// Token: 0x04000CCE RID: 3278
	public int tempItemCost;

	// Token: 0x04000CCF RID: 3279
	public int leftHandHoldableStatus;

	// Token: 0x04000CD0 RID: 3280
	public int rightHandHoldableStatus;

	// Token: 0x04000CD1 RID: 3281
	[Tooltip("This has to match the drumsAS array in DrumsItem.cs.")]
	[SerializeReference]
	public AudioSource[] musicDrums;

	// Token: 0x04000CD2 RID: 3282
	public TransferrableObject[] instrumentSelfOnly;

	// Token: 0x04000CD3 RID: 3283
	public AudioSource geodeCrackingSound;

	// Token: 0x04000CD4 RID: 3284
	public float bonkTime;

	// Token: 0x04000CD5 RID: 3285
	public float bonkCooldown = 2f;

	// Token: 0x04000CD6 RID: 3286
	private VRRig tempVRRig;

	// Token: 0x04000CD7 RID: 3287
	public GameObject huntComputer;

	// Token: 0x04000CD8 RID: 3288
	public Slingshot slingshot;

	// Token: 0x04000CD9 RID: 3289
	public Slingshot.SlingshotState slingshotState;

	// Token: 0x04000CDA RID: 3290
	private PhotonVoiceView myPhotonVoiceView;

	// Token: 0x04000CDB RID: 3291
	private VRRig senderRig;

	// Token: 0x04000CDC RID: 3292
	public TransferrableObject.PositionState currentState;

	// Token: 0x04000CDD RID: 3293
	private bool isInitialized;

	// Token: 0x04000CDE RID: 3294
	private List<VRRig.VelocityTime> velocityHistoryList = new List<VRRig.VelocityTime>();

	// Token: 0x04000CDF RID: 3295
	public int velocityHistoryMaxLength = 200;

	// Token: 0x04000CE0 RID: 3296
	private Vector3 lastPosition;

	// Token: 0x04000CE1 RID: 3297
	public const int splashLimitCount = 4;

	// Token: 0x04000CE2 RID: 3298
	public const float splashLimitCooldown = 0.5f;

	// Token: 0x04000CE3 RID: 3299
	private float[] splashEffectTimes = new float[4];

	// Token: 0x04000CE4 RID: 3300
	internal AudioSource voiceAudio;

	// Token: 0x04000CE5 RID: 3301
	public bool remoteUseReplacementVoice;

	// Token: 0x04000CE6 RID: 3302
	public bool localUseReplacementVoice;

	// Token: 0x04000CE7 RID: 3303
	private MicWrapper currentMicWrapper;

	// Token: 0x04000CE8 RID: 3304
	private IAudioDesc audioDesc;

	// Token: 0x04000CE9 RID: 3305
	private float speakingLoudness;

	// Token: 0x04000CEA RID: 3306
	public bool shouldSendSpeakingLoudness = true;

	// Token: 0x04000CEB RID: 3307
	public float replacementVoiceLoudnessThreshold = 0.05f;

	// Token: 0x04000CEC RID: 3308
	public int replacementVoiceDetectionDelay = 128;

	// Token: 0x04000CED RID: 3309
	[SerializeField]
	internal PhotonView photonView;

	// Token: 0x04000CEE RID: 3310
	[SerializeField]
	internal VRRigSerializer rigSerializer;

	// Token: 0x04000CEF RID: 3311
	public NetPlayer OwningNetPlayer;

	// Token: 0x04000CF0 RID: 3312
	[SerializeField]
	private FXSystemSettings sharedFXSettings;

	// Token: 0x04000CF1 RID: 3313
	[NonSerialized]
	public FXSystemSettings fxSettings;

	// Token: 0x04000CF2 RID: 3314
	private bool playerWasHaunted;

	// Token: 0x04000CF3 RID: 3315
	private float nonHauntedVolume;

	// Token: 0x04000CF4 RID: 3316
	private int count;

	// Token: 0x04000CF5 RID: 3317
	public Color playerColor;

	// Token: 0x04000CF6 RID: 3318
	public bool colorInitialized;

	// Token: 0x04000CF7 RID: 3319
	private Action<Color> onColorInitialized;

	// Token: 0x04000CF8 RID: 3320
	private bool pendingCosmeticUpdate = true;

	// Token: 0x04000CF9 RID: 3321
	private string rawCosmeticString = "";

	// Token: 0x04000CFA RID: 3322
	[SerializeField]
	private GuidedRefBasicTargetInfo guidedRefTargetInfo;

	// Token: 0x02000210 RID: 528
	public enum WearablePackedStateSlots
	{
		// Token: 0x04000CFD RID: 3325
		Hat,
		// Token: 0x04000CFE RID: 3326
		LeftHand,
		// Token: 0x04000CFF RID: 3327
		RightHand
	}

	// Token: 0x02000211 RID: 529
	public struct VelocityTime
	{
		// Token: 0x06000C12 RID: 3090 RVA: 0x00022DC8 File Offset: 0x00020FC8
		public VelocityTime(Vector3 velocity, double velTime)
		{
			this.vel = velocity;
			this.time = velTime;
		}

		// Token: 0x04000D00 RID: 3328
		public Vector3 vel;

		// Token: 0x04000D01 RID: 3329
		public double time;
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using GorillaExtensions;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Gameplay;
using GorillaNetworking;
using GorillaTag;
using GorillaTagScripts;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR;
using WebSocketSharp;

// Token: 0x0200026D RID: 621
public class VRRig : MonoBehaviour, IWrappedSerializable, INetworkStruct, IPreDisable, IUserCosmeticsCallback
{
    // Token: 0x06000D5D RID: 3421 RVA: 0x000289BD File Offset: 0x00026BBD
    private void CosmeticsV2\_Awake()
    {
        if (CosmeticsV2Spawner\_Dirty.allPartsInstantiated)
        {
            this.Handle\_CosmeticsV2\_OnPostInstantiateAllPrefabs\_DoEnableAllCosmetics();
            return;
        }
        if (!this.\_isListeningFor\_OnPostInstantiateAllPrefabs)
        {
            this.\_isListeningFor\_OnPostInstantiateAllPrefabs = true;
            CosmeticsV2Spawner\_Dirty.OnPostInstantiateAllPrefabs = (Action)Delegate.Combine(CosmeticsV2Spawner\_Dirty.OnPostInstantiateAllPrefabs, new Action(this.Handle\_CosmeticsV2\_OnPostInstantiateAllPrefabs\_DoEnableAllCosmetics));
        }
    }

    // Token: 0x06000D5E RID: 3422 RVA: 0x000289FC File Offset: 0x00026BFC
    private void CosmeticsV2\_OnDestroy()
    {
        if (CosmeticsV2Spawner\_Dirty.allPartsInstantiated)
        {
            this.Handle\_CosmeticsV2\_OnPostInstantiateAllPrefabs\_DoEnableAllCosmetics();
            return;
        }
        CosmeticsV2Spawner\_Dirty.OnPostInstantiateAllPrefabs = (Action)Delegate.Remove(CosmeticsV2Spawner\_Dirty.OnPostInstantiateAllPrefabs, new Action(this.Handle\_CosmeticsV2\_OnPostInstantiateAllPrefabs\_DoEnableAllCosmetics));
    }

    // Token: 0x06000D5F RID: 3423 RVA: 0x00028A2C File Offset: 0x00026C2C
    internal void Handle\_CosmeticsV2\_OnPostInstantiateAllPrefabs\_DoEnableAllCosmetics()
    {
        this.CheckForEarlyAccess();
        this.BuildInitialize\_AfterCosmeticsV2Instantiated();
        this.SetCosmeticsActive();
    }

    // Token: 0x17000193 RID: 403
    // (get) Token: 0x06000D60 RID: 3424 RVA: 0x00028A40 File Offset: 0x00026C40
    // (set) Token: 0x06000D61 RID: 3425 RVA: 0x00028A48 File Offset: 0x00026C48
    public GameObject[] cosmetics
    {
        get
        {
            return this.\_cosmetics;
        }
        set
        {
            this.\_cosmetics = value;
        }
    }

    // Token: 0x17000194 RID: 404
    // (get) Token: 0x06000D62 RID: 3426 RVA: 0x00028A51 File Offset: 0x00026C51
    // (set) Token: 0x06000D63 RID: 3427 RVA: 0x00028A59 File Offset: 0x00026C59
    public GameObject[] overrideCosmetics
    {
        get
        {
            return this.\_overrideCosmetics;
        }
        set
        {
            this.\_overrideCosmetics = value;
        }
    }

    // Token: 0x17000195 RID: 405
    // (get) Token: 0x06000D64 RID: 3428 RVA: 0x00028A62 File Offset: 0x00026C62
    public bool HasBracelet
    {
        get
        {
            return this.reliableState.HasBracelet;
        }
    }

    // Token: 0x06000D65 RID: 3429 RVA: 0x00028A6F File Offset: 0x00026C6F
    public Vector3 GetMouthPosition()
    {
        return this.MouthPosition.position;
    }

    // Token: 0x06000D66 RID: 3430 RVA: 0x00028A7C File Offset: 0x00026C7C
    public VRRig.PartyMemberStatus GetPartyMemberStatus()
    {
        if (this.partyMemberStatus == VRRig.PartyMemberStatus.NeedsUpdate)
        {
            this.partyMemberStatus = (FriendshipGroupDetection.Instance.IsInMyGroup(this.creator.UserId) ? VRRig.PartyMemberStatus.InLocalParty : VRRig.PartyMemberStatus.NotInLocalParty);
        }
        return this.partyMemberStatus;
    }

    // Token: 0x17000196 RID: 406
    // (get) Token: 0x06000D67 RID: 3431 RVA: 0x00028AAD File Offset: 0x00026CAD
    public bool IsLocalPartyMember
    {
        get
        {
            return this.GetPartyMemberStatus() != VRRig.PartyMemberStatus.NotInLocalParty;
        }
    }

    // Token: 0x06000D68 RID: 3432 RVA: 0x00028ABB File Offset: 0x00026CBB
    public void ClearPartyMemberStatus()
    {
        this.partyMemberStatus = VRRig.PartyMemberStatus.NeedsUpdate;
    }

    // Token: 0x06000D69 RID: 3433 RVA: 0x00028AC4 File Offset: 0x00026CC4
    public int ActiveTransferrableObjectIndex(int **idx**)
    {
        return this.reliableState.activeTransferrableObjectIndex[**idx**];
    }

    // Token: 0x06000D6A RID: 3434 RVA: 0x00028AD3 File Offset: 0x00026CD3
    public int ActiveTransferrableObjectIndexLength()
    {
        return this.reliableState.activeTransferrableObjectIndex.Length;
    }

    // Token: 0x06000D6B RID: 3435 RVA: 0x00028AE2 File Offset: 0x00026CE2
    public void SetActiveTransferrableObjectIndex(int **idx**, int **v**)
    {
        if (this.reliableState.activeTransferrableObjectIndex[**idx**] != **v**)
        {
            this.reliableState.activeTransferrableObjectIndex[**idx**] = **v**;
            this.reliableState.SetIsDirty();
        }
    }

    // Token: 0x06000D6C RID: 3436 RVA: 0x00028B0D File Offset: 0x00026D0D
    public TransferrableObject.PositionState TransferrablePosStates(int **idx**)
    {
        return this.reliableState.transferrablePosStates[**idx**];
    }

    // Token: 0x06000D6D RID: 3437 RVA: 0x00028B1C File Offset: 0x00026D1C
    public void SetTransferrablePosStates(int **idx**, TransferrableObject.PositionState **v**)
    {
        if (this.reliableState.transferrablePosStates[**idx**] != **v**)
        {
            this.reliableState.transferrablePosStates[**idx**] = **v**;
            this.reliableState.SetIsDirty();
        }
    }

    // Token: 0x06000D6E RID: 3438 RVA: 0x00028B47 File Offset: 0x00026D47
    public TransferrableObject.ItemStates TransferrableItemStates(int **idx**)
    {
        return this.reliableState.transferrableItemStates[**idx**];
    }

    // Token: 0x06000D6F RID: 3439 RVA: 0x00028B56 File Offset: 0x00026D56
    public void SetTransferrableItemStates(int **idx**, TransferrableObject.ItemStates **v**)
    {
        if (this.reliableState.transferrableItemStates[**idx**] != **v**)
        {
            this.reliableState.transferrableItemStates[**idx**] = **v**;
            this.reliableState.SetIsDirty();
        }
    }

    // Token: 0x06000D70 RID: 3440 RVA: 0x00028B81 File Offset: 0x00026D81
    public void SetTransferrableDockPosition(int **idx**, BodyDockPositions.DropPositions **v**)
    {
        if (this.reliableState.transferableDockPositions[**idx**] != **v**)
        {
            this.reliableState.transferableDockPositions[**idx**] = **v**;
            this.reliableState.SetIsDirty();
        }
    }

    // Token: 0x06000D71 RID: 3441 RVA: 0x00028BAC File Offset: 0x00026DAC
    public BodyDockPositions.DropPositions TransferrableDockPosition(int **idx**)
    {
        return this.reliableState.transferableDockPositions[**idx**];
    }

    // Token: 0x17000197 RID: 407
    // (get) Token: 0x06000D72 RID: 3442 RVA: 0x00028BBB File Offset: 0x00026DBB
    // (set) Token: 0x06000D73 RID: 3443 RVA: 0x00028BC8 File Offset: 0x00026DC8
    public int WearablePackedStates
    {
        get
        {
            return this.reliableState.wearablesPackedStates;
        }
        set
        {
            if (this.reliableState.wearablesPackedStates != value)
            {
                this.reliableState.wearablesPackedStates = value;
                this.reliableState.SetIsDirty();
            }
        }
    }

    // Token: 0x17000198 RID: 408
    // (get) Token: 0x06000D74 RID: 3444 RVA: 0x00028BEF File Offset: 0x00026DEF
    // (set) Token: 0x06000D75 RID: 3445 RVA: 0x00028BFC File Offset: 0x00026DFC
    public int LeftThrowableProjectileIndex
    {
        get
        {
            return this.reliableState.lThrowableProjectileIndex;
        }
        set
        {
            if (this.reliableState.lThrowableProjectileIndex != value)
            {
                this.reliableState.lThrowableProjectileIndex = value;
                this.reliableState.SetIsDirty();
            }
        }
    }

    // Token: 0x17000199 RID: 409
    // (get) Token: 0x06000D76 RID: 3446 RVA: 0x00028C23 File Offset: 0x00026E23
    // (set) Token: 0x06000D77 RID: 3447 RVA: 0x00028C30 File Offset: 0x00026E30
    public int RightThrowableProjectileIndex
    {
        get
        {
            return this.reliableState.rThrowableProjectileIndex;
        }
        set
        {
            if (this.reliableState.rThrowableProjectileIndex != value)
            {
                this.reliableState.rThrowableProjectileIndex = value;
                this.reliableState.SetIsDirty();
            }
        }
    }

    // Token: 0x1700019A RID: 410
    // (get) Token: 0x06000D78 RID: 3448 RVA: 0x00028C57 File Offset: 0x00026E57
    // (set) Token: 0x06000D79 RID: 3449 RVA: 0x00028C64 File Offset: 0x00026E64
    public Color32 LeftThrowableProjectileColor
    {
        get
        {
            return this.reliableState.lThrowableProjectileColor;
        }
        set
        {
            if (!this.reliableState.lThrowableProjectileColor.Equals(value))
            {
                this.reliableState.lThrowableProjectileColor = value;
                this.reliableState.SetIsDirty();
            }
        }
    }

    // Token: 0x1700019B RID: 411
    // (get) Token: 0x06000D7A RID: 3450 RVA: 0x00028C9B File Offset: 0x00026E9B
    // (set) Token: 0x06000D7B RID: 3451 RVA: 0x00028CA8 File Offset: 0x00026EA8
    public Color32 RightThrowableProjectileColor
    {
        get
        {
            return this.reliableState.rThrowableProjectileColor;
        }
        set
        {
            if (!this.reliableState.rThrowableProjectileColor.Equals(value))
            {
                this.reliableState.rThrowableProjectileColor = value;
                this.reliableState.SetIsDirty();
            }
        }
    }

    // Token: 0x06000D7C RID: 3452 RVA: 0x00028CDF File Offset: 0x00026EDF
    public Color32 GetThrowableProjectileColor(bool **isLeftHand**)
    {
        if (!**isLeftHand**)
        {
            return this.RightThrowableProjectileColor;
        }
        return this.LeftThrowableProjectileColor;
    }

    // Token: 0x06000D7D RID: 3453 RVA: 0x00028CF1 File Offset: 0x00026EF1
    public void SetThrowableProjectileColor(bool **isLeftHand**, Color32 **color**)
    {
        if (**isLeftHand**)
        {
            this.LeftThrowableProjectileColor = **color**;
            return;
        }
        this.RightThrowableProjectileColor = **color**;
    }

    // Token: 0x06000D7E RID: 3454 RVA: 0x00028D05 File Offset: 0x00026F05
    public void SetRandomThrowableModelIndex(int **randModelIndex**)
    {
        this.RandomThrowableIndex = **randModelIndex**;
    }

    // Token: 0x06000D7F RID: 3455 RVA: 0x00028D0E File Offset: 0x00026F0E
    public int GetRandomThrowableModelIndex()
    {
        return this.RandomThrowableIndex;
    }

    // Token: 0x1700019C RID: 412
    // (get) Token: 0x06000D80 RID: 3456 RVA: 0x00028D16 File Offset: 0x00026F16
    // (set) Token: 0x06000D81 RID: 3457 RVA: 0x00028D23 File Offset: 0x00026F23
    private int RandomThrowableIndex
    {
        get
        {
            return this.reliableState.randomThrowableIndex;
        }
        set
        {
            if (this.reliableState.randomThrowableIndex != value)
            {
                this.reliableState.randomThrowableIndex = value;
                this.reliableState.SetIsDirty();
            }
        }
    }

    // Token: 0x1700019D RID: 413
    // (get) Token: 0x06000D82 RID: 3458 RVA: 0x00028D4A File Offset: 0x00026F4A
    // (set) Token: 0x06000D83 RID: 3459 RVA: 0x00028D57 File Offset: 0x00026F57
    public bool IsMicEnabled
    {
        get
        {
            return this.reliableState.isMicEnabled;
        }
        set
        {
            if (this.reliableState.isMicEnabled != value)
            {
                this.reliableState.isMicEnabled = value;
                this.reliableState.SetIsDirty();
            }
        }
    }

    // Token: 0x1700019E RID: 414
    // (get) Token: 0x06000D84 RID: 3460 RVA: 0x00028D7E File Offset: 0x00026F7E
    // (set) Token: 0x06000D85 RID: 3461 RVA: 0x00028D8B File Offset: 0x00026F8B
    public int SizeLayerMask
    {
        get
        {
            return this.reliableState.sizeLayerMask;
        }
        set
        {
            if (this.reliableState.sizeLayerMask != value)
            {
                this.reliableState.sizeLayerMask = value;
                this.reliableState.SetIsDirty();
            }
        }
    }

    // Token: 0x1700019F RID: 415
    // (get) Token: 0x06000D86 RID: 3462 RVA: 0x00028DB2 File Offset: 0x00026FB2
    public Photon.Realtime.Player Creator
    {
        get
        {
            return this.creator;
        }
    }

    // Token: 0x170001A0 RID: 416
    // (get) Token: 0x06000D87 RID: 3463 RVA: 0x00028DBA File Offset: 0x00026FBA
    internal bool Initialized
    {
        get
        {
            return this.initialized;
        }
    }

    // Token: 0x170001A1 RID: 417
    // (get) Token: 0x06000D88 RID: 3464 RVA: 0x00028DC2 File Offset: 0x00026FC2
    public float SpeakingLoudness
    {
        get
        {
            return this.speakingLoudness;
        }
    }

    // Token: 0x06000D89 RID: 3465 RVA: 0x0007BB40 File Offset: 0x00079D40
    public void BuildInitialize()
    {
        this.fxSettings = UnityEngine.Object.Instantiate\<FXSystemSettings>(this.sharedFXSettings);
        this.fxSettings.forLocalRig = this.isOfflineVRRig;
        this.lastPosition = base.transform.position;
        if (!this.isOfflineVRRig)
        {
            base.transform.parent = null;
        }
        SizeManager component = base.GetComponent\<SizeManager>();
        if (component != null)
        {
            component.BuildInitialize();
        }
        this.myMouthFlap = base.GetComponent\<GorillaMouthFlap>();
        this.mySpeakerLoudness = base.GetComponent\<GorillaSpeakerLoudness>();
        if (this.myReplacementVoice == null)
        {
            this.myReplacementVoice = base.GetComponentInChildren\<ReplacementVoice>();
        }
        this.myEyeExpressions = base.GetComponent\<GorillaEyeExpressions>();
    }

    // Token: 0x06000D8A RID: 3466 RVA: 0x0007BBE4 File Offset: 0x00079DE4
    public void BuildInitialize\_AfterCosmeticsV2Instantiated()
    {
        Dictionary\<string, GameObject> dictionary = new Dictionary\<string, GameObject>();
        foreach (GameObject gameObject in this.cosmetics)
        {
            GameObject gameObject2;
            if (!dictionary.TryGetValue(gameObject.name, out gameObject2))
            {
                dictionary.Add(gameObject.name, gameObject);
            }
        }
        foreach (GameObject gameObject3 in this.overrideCosmetics)
        {
            GameObject gameObject2;
            if (dictionary.TryGetValue(gameObject3.name, out gameObject2) && gameObject2.name == gameObject3.name)
            {
                gameObject2.name = "OVERRIDDEN";
            }
        }
        this.cosmetics = this.cosmetics.*Concat*(this.overrideCosmetics).*ToArray*\<GameObject>();
        this.cosmeticsObjectRegistry.Initialize(this.cosmetics);
    }

    // Token: 0x06000D8B RID: 3467 RVA: 0x0007BCA8 File Offset: 0x00079EA8
    private void Awake()
    {
        this.CosmeticsV2\_Awake();
        PlayFabAuthenticator instance = PlayFabAuthenticator.instance;
        instance.OnSafetyUpdate = (Action\<bool>)Delegate.Combine(instance.OnSafetyUpdate, new Action\<bool>(this.UpdateName));
        if (this.isOfflineVRRig)
        {
            this.BuildInitialize();
        }
        this.SharedStart();
    }

    // Token: 0x06000D8C RID: 3468 RVA: 0x00028DCA File Offset: 0x00026FCA
    private void EnsureInstantiatedMaterial()
    {
        if (this.myDefaultSkinMaterialInstance == null)
        {
            this.myDefaultSkinMaterialInstance = UnityEngine.Object.Instantiate\<Material>(this.materialsToChangeTo[0]);
            this.materialsToChangeTo[0] = this.myDefaultSkinMaterialInstance;
        }
    }

    // Token: 0x06000D8D RID: 3469 RVA: 0x0007BCF8 File Offset: 0x00079EF8
    private void ApplyColorCode()
    {
        float defaultValue = 0f;
        float @float = PlayerPrefs.GetFloat("redValue", defaultValue);
        float float2 = PlayerPrefs.GetFloat("greenValue", defaultValue);
        float float3 = PlayerPrefs.GetFloat("blueValue", defaultValue);
        GorillaTagger.Instance.UpdateColor(@float, float2, float3);
    }

    // Token: 0x06000D8E RID: 3470 RVA: 0x0007BD3C File Offset: 0x00079F3C
    private void SharedStart()
    {
        if (this.isInitialized)
        {
            return;
        }
        this.isInitialized = true;
        this.myBodyDockPositions = base.GetComponent\<BodyDockPositions>();
        this.reliableState.SharedStart(this.isOfflineVRRig, this.myBodyDockPositions);
        this.concatStringOfCosmeticsAllowed = "";
        if (!Application.isBatchMode)
        {
            this.playerText.transform.parent.GetComponent\<Canvas>().worldCamera = GorillaTagger.Instance.mainCamera.GetComponent\<Camera>();
        }
        this.EnsureInstantiatedMaterial();
        this.initialized = false;
        if (this.setMatIndex > -1 && this.setMatIndex < this.materialsToChangeTo.Length)
        {
            this.mainSkin.material = this.materialsToChangeTo[this.setMatIndex];
        }
        if (this.isOfflineVRRig)
        {
            if (CosmeticsController.hasInstance && CosmeticsController.instance.v2\_allCosmeticsInfoAssetRef\_isLoaded)
            {
                CosmeticsController.instance.currentWornSet.LoadFromPlayerPreferences(CosmeticsController.instance);
            }
            if (Application.platform == RuntimePlatform.Android && this.spectatorSkin != null)
            {
                UnityEngine.Object.Destroy(this.spectatorSkin);
            }
            base.StartCoroutine(this.OccasionalUpdate());
            this.initialized = true;
        }
        else if (!this.isOfflineVRRig)
        {
            if (this.spectatorSkin != null)
            {
                UnityEngine.Object.Destroy(this.spectatorSkin);
            }
            this.head.syncPos = -this.headBodyOffset;
        }
        GorillaSkin.ApplyToRig(this, this.defaultSkin, true);
        base.Invoke("ApplyColorCode", 1f);
    }

    // Token: 0x06000D8F RID: 3471 RVA: 0x00028DFB File Offset: 0x00026FFB
    private IEnumerator OccasionalUpdate()
    {
        for (;;)
        {
            try
            {
                if (RoomSystem.JoinedRoom && NetworkSystem.Instance.IsMasterClient && GorillaGameModes.GameMode.ActiveNetworkHandler.*IsNull*())
                {
                    GorillaGameModes.GameMode.LoadGameModeFromProperty();
                }
            }
            catch
            {
            }
            yield return new WaitForSeconds(1f);
        }
        yield break;
    }

    // Token: 0x06000D90 RID: 3472 RVA: 0x0007BEB8 File Offset: 0x0007A0B8
    public bool IsItemAllowed(string **itemName**)
    {
        if (**itemName** == "Slingshot")
        {
            return PhotonNetwork.InRoom && GorillaGameManager.instance is GorillaBattleManager;
        }
        if (this.concatStringOfCosmeticsAllowed == null)
        {
            return false;
        }
        if (this.concatStringOfCosmeticsAllowed.Contains(**itemName**))
        {
            return true;
        }
        bool canTryOn = CosmeticsController.instance.GetItemFromDict(**itemName**).canTryOn;
        return this.inTryOnRoom && canTryOn;
    }

    // Token: 0x06000D91 RID: 3473 RVA: 0x0007BF24 File Offset: 0x0007A124
    public void RemoteRigUpdate()
    {
        if (this.scaleFactor != this.lastScaleFactor)
        {
            base.transform.localScale = Vector3.one \* this.scaleFactor;
        }
        this.lastScaleFactor = this.scaleFactor;
        if (this.voiceAudio != null)
        {
            float num = (GorillaTagger.Instance.offlineVRRig.scaleFactor - this.scaleFactor) / this.pitchScale + this.pitchOffset;
            float num2 = this.UsingHauntedRing ? this.HauntedRingVoicePitch : num;
            num2 = (this.IsHaunted ? this.HauntedVoicePitch : num2);
            if (!Mathf.Approximately(this.voiceAudio.pitch, num2))
            {
                this.voiceAudio.pitch = num2;
            }
        }
        this.jobPos = base.transform.position;
        if (Time.time > this.timeSpawned + this.doNotLerpConstant)
        {
            this.jobPos = Vector3.Lerp(base.transform.position, this.SanitizeVector3(this.syncPos), this.lerpValueBody \* 0.66f);
            if (this.currentRopeSwing && this.currentRopeSwingTarget)
            {
                Vector3 b;
                if (this.grabbedRopeIsLeft)
                {
                    b = this.currentRopeSwingTarget.position - this.leftHandTransform.position;
                }
                else
                {
                    b = this.currentRopeSwingTarget.position - this.rightHandTransform.position;
                }
                if (this.shouldLerpToRope)
                {
                    this.jobPos += Vector3.Lerp(Vector3.zero, b, this.lastRopeGrabTimer \* 4f);
                    if (this.lastRopeGrabTimer < 1f)
                    {
                        this.lastRopeGrabTimer += Time.deltaTime;
                    }
                }
                else
                {
                    this.jobPos += b;
                }
            }
            else if (this.currentHoldParent != null)
            {
                this.jobPos += this.currentHoldParent.TransformPoint(this.grabbedRopeOffset) - (this.grabbedRopeIsLeft ? this.leftHandTransform : this.rightHandTransform).position;
            }
        }
        else
        {
            this.jobPos = this.SanitizeVector3(this.syncPos);
        }
        this.jobRotation = Quaternion.Lerp(base.transform.rotation, this.SanitizeQuaternion(this.syncRotation), this.lerpValueBody);
        this.head.syncPos = base.transform.rotation \* -this.headBodyOffset \* this.scaleFactor;
        this.head.MapOther(this.lerpValueBody);
        this.rightHand.MapOther(this.lerpValueBody);
        this.leftHand.MapOther(this.lerpValueBody);
        this.rightIndex.MapOtherFinger((float)(this.handSync % 10) / 10f, this.lerpValueFingers);
        this.rightMiddle.MapOtherFinger((float)(this.handSync % 100) / 100f, this.lerpValueFingers);
        this.rightThumb.MapOtherFinger((float)(this.handSync % 1000) / 1000f, this.lerpValueFingers);
        this.leftIndex.MapOtherFinger((float)(this.handSync % 10000) / 10000f, this.lerpValueFingers);
        this.leftMiddle.MapOtherFinger((float)(this.handSync % 100000) / 100000f, this.lerpValueFingers);
        this.leftThumb.MapOtherFinger((float)(this.handSync % 1000000) / 1000000f, this.lerpValueFingers);
        this.leftHandHoldableStatus = this.handSync % 10000000 / 1000000;
        this.rightHandHoldableStatus = this.handSync % 100000000 / 10000000;
    }

    // Token: 0x06000D92 RID: 3474 RVA: 0x0007C2F4 File Offset: 0x0007A4F4
    private void LateUpdate()
    {
        if (this.isOfflineVRRig)
        {
            if (GorillaGameManager.instance != null)
            {
                this.speedArray = GorillaGameManager.instance.LocalPlayerSpeed();
                GorillaLocomotion.Player.Instance.jumpMultiplier = this.speedArray[1];
                GorillaLocomotion.Player.Instance.maxJumpSpeed = this.speedArray[0];
            }
            else
            {
                GorillaLocomotion.Player.Instance.jumpMultiplier = 1.1f;
                GorillaLocomotion.Player.Instance.maxJumpSpeed = 6.5f;
            }
            this.scaleFactor = GorillaLocomotion.Player.Instance.scale;
            base.transform.localScale = Vector3.one \* this.scaleFactor;
            base.transform.eulerAngles = new Vector3(0f, this.mainCamera.transform.rotation.eulerAngles.y, 0f);
            this.syncPos = this.mainCamera.transform.position + this.headConstraint.rotation \* this.head.trackingPositionOffset \* this.scaleFactor + base.transform.rotation \* this.headBodyOffset \* this.scaleFactor;
            base.transform.position = this.syncPos;
            this.head.MapMine(this.scaleFactor, this.playerOffsetTransform);
            this.rightHand.MapMine(this.scaleFactor, this.playerOffsetTransform);
            this.leftHand.MapMine(this.scaleFactor, this.playerOffsetTransform);
            this.rightIndex.MapMyFinger(this.lerpValueFingers);
            this.rightMiddle.MapMyFinger(this.lerpValueFingers);
            this.rightThumb.MapMyFinger(this.lerpValueFingers);
            this.leftIndex.MapMyFinger(this.lerpValueFingers);
            this.leftMiddle.MapMyFinger(this.lerpValueFingers);
            this.leftThumb.MapMyFinger(this.lerpValueFingers);
            if (GorillaTagger.Instance.loadedDeviceName == "Oculus")
            {
                this.mainSkin.enabled = OVRManager.hasInputFocus;
            }
            this.mainSkin.enabled = !GorillaLocomotion.Player.Instance.inOverlay;
            this.speakingLoudness = 0f;
            if (this.shouldSendSpeakingLoudness && this.photonView)
            {
                PhotonVoiceView component = this.photonView\.GetComponent\<PhotonVoiceView>();
                if (component && component.RecorderInUse)
                {
                    if (this.audioDesc != component.RecorderInUse.InputSource)
                    {
                        this.audioDesc = component.RecorderInUse.InputSource;
                        this.currentMicWrapper = (this.audioDesc as MicWrapper);
                    }
                    if (this.currentMicWrapper != null)
                    {
                        int num = this.replacementVoiceDetectionDelay;
                        float[] array = new float[num];
                        if (this.currentMicWrapper.Mic.samples >= num && this.currentMicWrapper.Mic.GetData(array, this.currentMicWrapper.Mic.samples - num))
                        {
                            float num2 = 0f;
                            for (int i = 0; i < num; i++)
                            {
                                float num3 = Mathf.Sqrt(array[i]);
                                if (num3 > num2)
                                {
                                    num2 = num3;
                                }
                            }
                            this.speakingLoudness = num2;
                        }
                    }
                }
            }
        }
        if (this.creator != null)
        {
            ScienceExperimentManager instance = ScienceExperimentManager.instance;
            int num4;
            if (instance != null && instance.GetMaterialIfPlayerInGame(this.creator.ActorNumber, out num4))
            {
                this.tempMatIndex = num4;
            }
            else
            {
                this.tempMatIndex = ((GorillaGameManager.instance != null) ? GorillaGameManager.instance.MyMatIndex(this.creator) : 0);
            }
            if (this.setMatIndex != this.tempMatIndex)
            {
                this.setMatIndex = this.tempMatIndex;
                this.ChangeMaterialLocal(this.setMatIndex);
            }
        }
        GorillaMouthFlap gorillaMouthFlap = this.myMouthFlap;
        if (gorillaMouthFlap != null)
        {
            gorillaMouthFlap.InvokeUpdate();
        }
        GorillaSpeakerLoudness gorillaSpeakerLoudness = this.mySpeakerLoudness;
        if (gorillaSpeakerLoudness != null)
        {
            gorillaSpeakerLoudness.InvokeUpdate();
        }
        ReplacementVoice replacementVoice = this.myReplacementVoice;
        if (replacementVoice != null)
        {
            replacementVoice.InvokeUpdate();
        }
        GorillaEyeExpressions gorillaEyeExpressions = this.myEyeExpressions;
        if (gorillaEyeExpressions == null)
        {
            return;
        }
        gorillaEyeExpressions.InvokeUpdate();
    }

    // Token: 0x06000D93 RID: 3475 RVA: 0x0002030E File Offset: 0x0001E50E
    public void SetHeadBodyOffset()
    {
    }

    // Token: 0x06000D94 RID: 3476 RVA: 0x00028E03 File Offset: 0x00027003
    public void VRRigResize(float **ratioVar**)
    {
        this.ratio \*= **ratioVar**;
    }

    // Token: 0x06000D95 RID: 3477 RVA: 0x0007C704 File Offset: 0x0007A904
    public int ReturnHandPosition()
    {
        return 0 + Mathf.FloorToInt(this.rightIndex.calcT \* 9.99f) + Mathf.FloorToInt(this.rightMiddle.calcT \* 9.99f) \* 10 + Mathf.FloorToInt(this.rightThumb.calcT \* 9.99f) \* 100 + Mathf.FloorToInt(this.leftIndex.calcT \* 9.99f) \* 1000 + Mathf.FloorToInt(this.leftMiddle.calcT \* 9.99f) \* 10000 + Mathf.FloorToInt(this.leftThumb.calcT \* 9.99f) \* 100000 + this.leftHandHoldableStatus \* 1000000 + this.rightHandHoldableStatus \* 10000000;
    }

    // Token: 0x06000D96 RID: 3478 RVA: 0x00028E13 File Offset: 0x00027013
    public void OnDestroy()
    {
        if (ApplicationQuittingState.IsQuitting)
        {
            return;
        }
        if (this.currentRopeSwingTarget && this.currentRopeSwingTarget.gameObject)
        {
            UnityEngine.Object.Destroy(this.currentRopeSwingTarget.gameObject);
        }
        this.ClearRopeData();
    }

    // Token: 0x06000D97 RID: 3479 RVA: 0x0007C7D0 File Offset: 0x0007A9D0
    public object OnSerializeWrite()
    {
        InputStruct inputStruct = default(InputStruct);
        inputStruct.headRotation = this.head.rigTarget.localRotation;
        inputStruct.rightHandPosition = this.rightHand.rigTarget.localPosition;
        inputStruct.rightHandRotation = this.rightHand.rigTarget.localRotation;
        inputStruct.leftHandPosition = this.leftHand.rigTarget.localPosition;
        inputStruct.leftHandRotation = this.leftHand.rigTarget.localRotation;
        inputStruct.position = base.transform.position;
        inputStruct.roundedRotation = Mathf.RoundToInt(base.transform.rotation.eulerAngles.y);
        inputStruct.handPosition = this.ReturnHandPosition();
        inputStruct.remoteUseReplacementVoice = this.remoteUseReplacementVoice;
        inputStruct.speakingLoudness = this.speakingLoudness;
        inputStruct.grabbedRopeIndex = this.grabbedRopeIndex;
        if (this.grabbedRopeIndex > 0)
        {
            inputStruct.ropeBoneIndex = this.grabbedRopeBoneIndex;
            inputStruct.ropeGrabIsLeft = this.grabbedRopeIsLeft;
            inputStruct.ropeGrabOffset = this.grabbedRopeOffset;
        }
        double serverTimeStamp = NetworkSystem.Instance.SimTick / 1000.0;
        inputStruct.serverTimeStamp = serverTimeStamp;
        return inputStruct;
    }

    // Token: 0x06000D98 RID: 3480 RVA: 0x0007C918 File Offset: 0x0007AB18
    public void OnSerializeRead(object **objectData**)
    {
        InputStruct inputStruct = (InputStruct)**objectData**;
        this.head.syncRotation = this.SanitizeQuaternion(inputStruct.headRotation);
        this.rightHand.syncPos = this.SanitizeVector3(inputStruct.rightHandPosition);
        this.rightHand.syncRotation = this.SanitizeQuaternion(inputStruct.rightHandRotation);
        this.leftHand.syncPos = this.SanitizeVector3(inputStruct.leftHandPosition);
        this.leftHand.syncRotation = this.SanitizeQuaternion(inputStruct.leftHandRotation);
        this.syncPos = this.SanitizeVector3(inputStruct.position);
        this.syncRotation.eulerAngles = this.SanitizeVector3(new Vector3(0f, (float)inputStruct.roundedRotation, 0f));
        this.handSync = inputStruct.handPosition;
        this.remoteUseReplacementVoice = inputStruct.remoteUseReplacementVoice;
        this.speakingLoudness = inputStruct.speakingLoudness;
        this.UpdateReplacementVoice();
        this.lastPosition = this.syncPos;
        this.grabbedRopeIndex = inputStruct.grabbedRopeIndex;
        if (this.grabbedRopeIndex > 0)
        {
            this.grabbedRopeBoneIndex = inputStruct.ropeBoneIndex;
            this.grabbedRopeIsLeft = inputStruct.ropeGrabIsLeft;
            this.grabbedRopeOffset = this.SanitizeVector3(inputStruct.ropeGrabOffset);
        }
        this.UpdateRopeData();
        this.AddVelocityToQueue(this.syncPos, inputStruct.serverTimeStamp);
    }

    // Token: 0x06000D99 RID: 3481 RVA: 0x0007CA68 File Offset: 0x0007AC68
    public static int PackQuaternionForNetwork(Quaternion **q**)
    {
        **q**.Normalize();
        float num = Mathf.Abs(**q**.x);
        float num2 = Mathf.Abs(**q**.y);
        float num3 = Mathf.Abs(**q**.z);
        float num4 = Mathf.Abs(**q**.w);
        float num5 = num;
        VRRig.QAxis qaxis = VRRig.QAxis.X;
        if (num2 > num5)
        {
            num5 = num2;
            qaxis = VRRig.QAxis.Y;
        }
        if (num3 > num5)
        {
            num5 = num3;
            qaxis = VRRig.QAxis.Z;
        }
        if (num4 > num5)
        {
            qaxis = VRRig.QAxis.W;
        }
        bool flag;
        float num6;
        float num7;
        float num8;
        switch (qaxis)
        {
        case VRRig.QAxis.X:
            flag = (**q**.x < 0f);
            num6 = **q**.y;
            num7 = **q**.z;
            num8 = **q**.w;
            goto IL\_11A;
        case VRRig.QAxis.Y:
            flag = (**q**.y < 0f);
            num6 = **q**.x;
            num7 = **q**.z;
            num8 = **q**.w;
            goto IL\_11A;
        case VRRig.QAxis.Z:
            flag = (**q**.z < 0f);
            num6 = **q**.x;
            num7 = **q**.y;
            num8 = **q**.w;
            goto IL\_11A;
        }
        flag = (**q**.w < 0f);
        num6 = **q**.x;
        num7 = **q**.y;
        num8 = **q**.z;
        IL\_11A:
        if (flag)
        {
            num6 = -num6;
            num7 = -num7;
            num8 = -num8;
        }
        int num9 = Mathf.Clamp(Mathf.RoundToInt((num6 + 0.707107f) \* 361.33145f), 0, 511);
        int num10 = Mathf.Clamp(Mathf.RoundToInt((num7 + 0.707107f) \* 361.33145f), 0, 511);
        int num11 = Mathf.Clamp(Mathf.RoundToInt((num8 + 0.707107f) \* 361.33145f), 0, 511);
        return (int)(num9 + (num10 << 9) + (num11 << 18) + ((int)qaxis << 27));
    }

    // Token: 0x06000D9A RID: 3482 RVA: 0x0007CC10 File Offset: 0x0007AE10
    public static Quaternion UnpackQuaterionFromNetwork(int **data**)
    {
        float num = (float)(**data** & 511) \* 0.0027675421f - 0.707107f;
        float num2 = (float)(**data** >> 9 & 511) \* 0.0027675421f - 0.707107f;
        float num3 = (float)(**data** >> 18 & 511) \* 0.0027675421f - 0.707107f;
        float num4 = Mathf.Sqrt(1f - (num \* num + num2 \* num2 + num3 \* num3));
        switch (**data** >> 27 & 3)
        {
        case 0:
            return new Quaternion(num4, num, num2, num3);
        case 1:
            return new Quaternion(num, num4, num2, num3);
        case 2:
            return new Quaternion(num, num2, num4, num3);
        }
        return new Quaternion(num, num2, num3, num4);
    }

    // Token: 0x06000D9B RID: 3483 RVA: 0x0007CCC4 File Offset: 0x0007AEC4
    public static long PackHandPosRotForNetwork(Vector3 **localPos**, Quaternion **rot**)
    {
        long num = (long)Mathf.Clamp(Mathf.RoundToInt(**localPos**.x \* 512f) + 1024, 0, 2047);
        long num2 = (long)Mathf.Clamp(Mathf.RoundToInt(**localPos**.y \* 512f) + 1024, 0, 2047);
        long num3 = (long)Mathf.Clamp(Mathf.RoundToInt(**localPos**.z \* 512f) + 1024, 0, 2047);
        long num4 = (long)VRRig.PackQuaternionForNetwork(**rot**);
        return num + (num2 << 11) + (num3 << 22) + (num4 << 33);
    }

    // Token: 0x06000D9C RID: 3484 RVA: 0x0007CD54 File Offset: 0x0007AF54
    public static void UnpackHandPosRotFromNetwork(long **data**, out Vector3 **localPos**, out Quaternion **handRot**)
    {
        long num = **data** & 2047L;
        long num2 = **data** >> 11 & 2047L;
        long num3 = **data** >> 22 & 2047L;
        **localPos** = new Vector3((float)(num - 1024L) \* 0.001953125f, (float)(num2 - 1024L) \* 0.001953125f, (float)(num3 - 1024L) \* 0.001953125f);
        int data2 = (int)(**data** >> 33);
        **handRot** = VRRig.UnpackQuaterionFromNetwork(data2);
    }

    // Token: 0x06000D9D RID: 3485 RVA: 0x0007CDCC File Offset: 0x0007AFCC
    public static long PackWorldPosForNetwork(Vector3 **worldPos**)
    {
        long num = (long)Mathf.Clamp(Mathf.RoundToInt(**worldPos**.x \* 1024f) + 1048576, 0, 2097151);
        long num2 = (long)Mathf.Clamp(Mathf.RoundToInt(**worldPos**.y \* 1024f) + 1048576, 0, 2097151);
        long num3 = (long)Mathf.Clamp(Mathf.RoundToInt(**worldPos**.z \* 1024f) + 1048576, 0, 2097151);
        return num + (num2 << 21) + (num3 << 42);
    }

    // Token: 0x06000D9E RID: 3486 RVA: 0x0007CE50 File Offset: 0x0007B050
    public static Vector3 UnpackWorldPosFromNetwork(long **data**)
    {
        float num = (float)(**data** & 2097151L);
        long num2 = **data** >> 21 & 2097151L;
        long num3 = **data** >> 42 & 2097151L;
        return new Vector3((float)((long)num - 1048576L) \* 0.0009765625f, (float)(num2 - 1048576L) \* 0.0009765625f, (float)(num3 - 1048576L) \* 0.0009765625f);
    }

    // Token: 0x06000D9F RID: 3487 RVA: 0x0007CEB0 File Offset: 0x0007B0B0
    void IWrappedSerializable.OnSerializeWrite(PhotonStream **stream**, PhotonMessageInfo **info**)
    {
        **stream**.SendNext(VRRig.PackQuaternionForNetwork(this.head.rigTarget.localRotation));
        **stream**.SendNext(VRRig.PackHandPosRotForNetwork(this.rightHand.rigTarget.localPosition, this.rightHand.rigTarget.localRotation));
        **stream**.SendNext(VRRig.PackHandPosRotForNetwork(this.leftHand.rigTarget.localPosition, this.leftHand.rigTarget.localRotation));
        **stream**.SendNext(VRRig.PackWorldPosForNetwork(base.transform.position));
        **stream**.SendNext(this.ReturnHandPosition());
        int num = Mathf.Clamp(Mathf.RoundToInt(base.transform.rotation.eulerAngles.y + 360f) % 360, 0, 360);
        int num2 = Mathf.RoundToInt(Mathf.Clamp01(this.speakingLoudness) \* 255f);
        int num3 = num + (this.remoteUseReplacementVoice ? 512 : 0) + ((this.grabbedRopeIndex > 0) ? 1024 : 0) + (num2 << 16);
        **stream**.SendNext(num3);
        if (this.grabbedRopeIndex > 0)
        {
            **stream**.SendNext(this.grabbedRopeIndex);
            **stream**.SendNext(this.grabbedRopeBoneIndex);
            **stream**.SendNext(this.grabbedRopeIsLeft);
            **stream**.SendNext(this.grabbedRopeOffset);
        }
        **stream**.SendNext(this.targetScale);
    }

    // Token: 0x06000DA0 RID: 3488 RVA: 0x0007D048 File Offset: 0x0007B248
    void IWrappedSerializable.OnSerializeRead(PhotonStream **stream**, PhotonMessageInfo **info**)
    {
        this.head.syncRotation = VRRig.UnpackQuaterionFromNetwork((int)**stream**.ReceiveNext());
        VRRig.UnpackHandPosRotFromNetwork((long)**stream**.ReceiveNext(), out this.rightHand.syncPos, out this.rightHand.syncRotation);
        VRRig.UnpackHandPosRotFromNetwork((long)**stream**.ReceiveNext(), out this.leftHand.syncPos, out this.leftHand.syncRotation);
        this.syncPos = VRRig.UnpackWorldPosFromNetwork((long)**stream**.ReceiveNext());
        this.handSync = (int)**stream**.ReceiveNext();
        this.lastPosition = this.syncPos;
        int num = (int)**stream**.ReceiveNext();
        int num2 = num & 511;
        this.syncRotation.eulerAngles = this.SanitizeVector3(new Vector3(0f, (float)num2, 0f));
        this.remoteUseReplacementVoice = ((num & 512) != 0);
        int num3 = num >> 16 & 255;
        this.speakingLoudness = (float)num3 / 255f;
        this.UpdateReplacementVoice();
        if ((num & 1024) != 0)
        {
            this.grabbedRopeIndex = (int)**stream**.ReceiveNext();
            this.grabbedRopeBoneIndex = (int)**stream**.ReceiveNext();
            this.grabbedRopeIsLeft = (bool)**stream**.ReceiveNext();
            this.grabbedRopeOffset = this.SanitizeVector3((Vector3)**stream**.ReceiveNext());
        }
        else
        {
            this.grabbedRopeIndex = 0;
        }
        this.UpdateRopeData();
        this.targetScale = (float)**stream**.ReceiveNext();
        this.AddVelocityToQueue(this.syncPos, **info**.timestamp);
    }

    // Token: 0x06000DA1 RID: 3489 RVA: 0x0007D1E0 File Offset: 0x0007B3E0
    private void UpdateExtrapolationTarget()
    {
        float num = (float)(PhotonNetwork.Time - this.remoteLatestTimestamp);
        num -= 0.15f;
        num = Mathf.Clamp(num, -0.5f, 0.5f);
        this.syncPos += this.remoteVelocity \* num;
        this.remoteCorrectionNeeded = this.syncPos - base.transform.position;
        if (this.remoteCorrectionNeeded.magnitude > 1.5f && this.grabbedRopeIndex <= 0)
        {
            base.transform.position = this.syncPos;
            this.remoteCorrectionNeeded = Vector3.zero;
        }
    }

    // Token: 0x06000DA2 RID: 3490 RVA: 0x0007D284 File Offset: 0x0007B484
    private void UpdateRopeData()
    {
        if (this.previousGrabbedRope == this.grabbedRopeIndex && this.previousGrabbedRopeBoneIndex == this.grabbedRopeBoneIndex && this.previousGrabbedRopeWasLeft == this.grabbedRopeIsLeft)
        {
            return;
        }
        this.ClearRopeData();
        if (this.grabbedRopeIndex > 0)
        {
            PhotonView photonView = PhotonView\.Find(this.grabbedRopeIndex);
            GorillaRopeSwing gorillaRopeSwing;
            GorillaClimbable gorillaClimbable;
            if (photonView && photonView\.TryGetComponent\<GorillaRopeSwing>(out gorillaRopeSwing))
            {
                if (this.currentRopeSwingTarget == null || this.currentRopeSwingTarget.gameObject == null)
                {
                    this.currentRopeSwingTarget = new GameObject("RopeSwingTarget").transform;
                }
                if (gorillaRopeSwing.AttachRemotePlayer(this.creator.ActorNumber, this.grabbedRopeBoneIndex, this.currentRopeSwingTarget, this.grabbedRopeOffset))
                {
                    this.currentRopeSwing = gorillaRopeSwing;
                }
                this.lastRopeGrabTimer = 0f;
            }
            else if (photonView && photonView\.TryGetComponent\<GorillaClimbable>(out gorillaClimbable))
            {
                this.currentHoldParent = photonView\.transform;
            }
        }
        this.shouldLerpToRope = true;
        this.previousGrabbedRope = this.grabbedRopeIndex;
        this.previousGrabbedRopeBoneIndex = this.grabbedRopeBoneIndex;
        this.previousGrabbedRopeWasLeft = this.grabbedRopeIsLeft;
    }

    // Token: 0x06000DA3 RID: 3491 RVA: 0x0007D3A4 File Offset: 0x0007B5A4
    public static void AttachLocalPlayerToPhotonView(PhotonView **view**, XRNode **xrNode**, Vector3 **offset**, Vector3 **velocity**)
    {
        if (GorillaTagger.hasInstance && GorillaTagger.Instance.offlineVRRig)
        {
            GorillaTagger.Instance.offlineVRRig.grabbedRopeIndex = **view**.ViewID;
            GorillaTagger.Instance.offlineVRRig.grabbedRopeIsLeft = (**xrNode** == XRNode.LeftHand);
            GorillaTagger.Instance.offlineVRRig.grabbedRopeOffset = **offset**;
        }
    }

    // Token: 0x06000DA4 RID: 3492 RVA: 0x00028E52 File Offset: 0x00027052
    public static void DetachLocalPlayerFromPhotonView()
    {
        if (GorillaTagger.hasInstance && GorillaTagger.Instance.offlineVRRig)
        {
            GorillaTagger.Instance.offlineVRRig.grabbedRopeIndex = -1;
        }
    }

    // Token: 0x06000DA5 RID: 3493 RVA: 0x0007D404 File Offset: 0x0007B604
    private void ClearRopeData()
    {
        if (this.currentRopeSwing)
        {
            this.currentRopeSwing.DetachRemotePlayer(this.creator.ActorNumber);
        }
        if (this.currentRopeSwingTarget)
        {
            this.currentRopeSwingTarget.SetParent(null);
        }
        this.currentRopeSwing = null;
        this.currentHoldParent = null;
    }

    // Token: 0x06000DA6 RID: 3494 RVA: 0x00028E7C File Offset: 0x0002707C
    public void ChangeMaterial(int **materialIndex**, PhotonMessageInfo **info**)
    {
        if (**info**.Sender == PhotonNetwork.MasterClient)
        {
            this.ChangeMaterialLocal(**materialIndex**);
        }
    }

    // Token: 0x06000DA7 RID: 3495 RVA: 0x0007D45C File Offset: 0x0007B65C
    public void ChangeMaterialLocal(int **materialIndex**)
    {
        this.setMatIndex = **materialIndex**;
        if (this.setMatIndex > -1 && this.setMatIndex < this.materialsToChangeTo.Length)
        {
            this.mainSkin.material = this.materialsToChangeTo[this.setMatIndex];
        }
        if (this.lavaParticleSystem != null)
        {
            if (!this.isOfflineVRRig && **materialIndex** == 2 && this.lavaParticleSystem.isStopped)
            {
                this.lavaParticleSystem.Play();
            }
            else if (!this.isOfflineVRRig && this.lavaParticleSystem.isPlaying)
            {
                this.lavaParticleSystem.Stop();
            }
        }
        if (this.rockParticleSystem != null)
        {
            if (!this.isOfflineVRRig && **materialIndex** == 1 && this.rockParticleSystem.isStopped)
            {
                this.rockParticleSystem.Play();
            }
            else if (!this.isOfflineVRRig && this.rockParticleSystem.isPlaying)
            {
                this.rockParticleSystem.Stop();
            }
        }
        if (this.iceParticleSystem != null)
        {
            if (!this.isOfflineVRRig && **materialIndex** == 3 && this.rockParticleSystem.isStopped)
            {
                this.iceParticleSystem.Play();
                return;
            }
            if (!this.isOfflineVRRig && this.iceParticleSystem.isPlaying)
            {
                this.iceParticleSystem.Stop();
            }
        }
    }

    // Token: 0x06000DA8 RID: 3496 RVA: 0x0007D59C File Offset: 0x0007B79C
    public void InitializeNoobMaterial(float **red**, float **green**, float **blue**, PhotonMessageInfoWrapped **info**)
    {
        this.IncrementRPC(**info**, "InitializeNoobMaterial");
        NetworkSystem.Instance.GetPlayer(**info**.senderID);
        string userID = NetworkSystem.Instance.GetUserID(**info**.senderID);
        if (**info**.senderID == NetworkSystem.Instance.GetOwningPlayerID(this.rigSerializer.gameObject) && (!this.initialized || (this.initialized && GorillaComputer.instance.friendJoinCollider.playerIDsCurrentlyTouching.Contains(userID))))
        {
            this.initialized = true;
            **blue** = **blue**.*ClampSafe*(0f, 1f);
            **red** = **red**.*ClampSafe*(0f, 1f);
            **green** = **green**.*ClampSafe*(0f, 1f);
            this.InitializeNoobMaterialLocal(**red**, **green**, **blue**);
        }
    }

    // Token: 0x06000DA9 RID: 3497 RVA: 0x0007D668 File Offset: 0x0007B868
    public void InitializeNoobMaterialLocal(float **red**, float **green**, float **blue**)
    {
        Color color = new Color(**red**, **green**, **blue**);
        this.EnsureInstantiatedMaterial();
        if (this.myDefaultSkinMaterialInstance != null)
        {
            color.r = Mathf.Clamp(color.r, 0f, 1f);
            color.g = Mathf.Clamp(color.g, 0f, 1f);
            color.b = Mathf.Clamp(color.b, 0f, 1f);
            this.myDefaultSkinMaterialInstance.color = color;
        }
        this.SetColor(color);
        this.UpdateName(PlayFabAuthenticator.instance.GetSafety());
    }

    // Token: 0x06000DAA RID: 3498 RVA: 0x0007D70C File Offset: 0x0007B90C
    public void UpdateName(bool **isSafety**)
    {
        if (this.rigSerializer != null)
        {
            string text = **isSafety** ? this.OwningNetPlayer.DefaultName : this.OwningNetPlayer.NickName;
            this.playerNameVisible = this.NormalizeName(true, text);
        }
        else if (this.showName && NetworkSystem.Instance != null)
        {
            this.playerNameVisible = (**isSafety** ? NetworkSystem.Instance.GetMyDefaultName() : NetworkSystem.Instance.GetMyNickName());
        }
        this.playerText.text = this.playerNameVisible;
    }

    // Token: 0x06000DAB RID: 3499 RVA: 0x0007D798 File Offset: 0x0007B998
    public string NormalizeName(bool **doIt**, string **text**)
    {
        if (**doIt**)
        {
            if (GorillaComputer.instance.CheckAutoBanListForName(**text**))
            {
                **text** = new string(Array.FindAll\<char>(**text**.ToCharArray(), (char **c**) => char.IsLetterOrDigit(**c**)));
                if (**text**.Length > 12)
                {
                    **text** = **text**.Substring(0, 11);
                }
                **text** = **text**.ToUpper();
            }
            else
            {
                **text** = "BADGORILLA";
            }
        }
        return **text**;
    }

    // Token: 0x06000DAC RID: 3500 RVA: 0x00028E92 File Offset: 0x00027092
    public void SetJumpLimitLocal(float **maxJumpSpeed**)
    {
        GorillaLocomotion.Player.Instance.maxJumpSpeed = **maxJumpSpeed**;
    }

    // Token: 0x06000DAD RID: 3501 RVA: 0x00028E9F File Offset: 0x0002709F
    public void SetJumpMultiplierLocal(float **jumpMultiplier**)
    {
        GorillaLocomotion.Player.Instance.jumpMultiplier = **jumpMultiplier**;
    }

    // Token: 0x06000DAE RID: 3502 RVA: 0x0007D810 File Offset: 0x0007BA10
    [PunRPC]
    public void RequestMaterialColor(int **askingPlayerID**, PhotonMessageInfoWrapped **info**)
    {
        this.IncrementRPC(**info**, "RequestMaterialColor");
        Photon.Realtime.Player playerRef = ((PunNetPlayer)NetworkSystem.Instance.GetPlayer(**info**.senderID)).playerRef;
        if (this.photonView\.IsMine)
        {
            this.photonView\.RPC("InitializeNoobMaterial", playerRef, new object[]
            {
                this.myDefaultSkinMaterialInstance.color.r,
                this.myDefaultSkinMaterialInstance.color.g,
                this.myDefaultSkinMaterialInstance.color.b
            });
        }
    }

    // Token: 0x06000DAF RID: 3503 RVA: 0x0007D8B0 File Offset: 0x0007BAB0
    public void RequestCosmetics(PhotonMessageInfo **info**)
    {
        this.IncrementRPC(**info**, "RequestCosmetics");
        if (this.photonView\.IsMine && CosmeticsController.hasInstance)
        {
            string[] array = CosmeticsController.instance.currentWornSet.ToDisplayNameArray();
            string[] array2 = CosmeticsController.instance.tryOnSet.ToDisplayNameArray();
            this.photonView\.RPC("UpdateCosmeticsWithTryon", **info**.Sender, new object[]
            {
                array,
                array2
            });
        }
    }

    // Token: 0x06000DB0 RID: 3504 RVA: 0x00028EAC File Offset: 0x000270AC
    public void PlayTagSoundLocal(int **soundIndex**, float **soundVolume**)
    {
        if (**soundIndex** < 0 || **soundIndex** >= this.clipToPlay.Length)
        {
            return;
        }
        this.tagSound.volume = Mathf.Min(0.25f, **soundVolume**);
        this.tagSound.PlayOneShot(this.clipToPlay[**soundIndex**]);
    }

    // Token: 0x06000DB1 RID: 3505 RVA: 0x0007D928 File Offset: 0x0007BB28
    public void Bonk(int **soundIndex**, float **bonkPercent**, PhotonMessageInfo **info**)
    {
        if (**info**.Sender == this.photonView\.Owner)
        {
            if (this.bonkTime + this.bonkCooldown < Time.time)
            {
                this.bonkTime = Time.time;
                this.tagSound.volume = **bonkPercent** \* 0.25f;
                this.tagSound.PlayOneShot(this.clipToPlay[**soundIndex**]);
                if (this.photonView\.IsMine)
                {
                    GorillaTagger.Instance.StartVibration(true, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
                    GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.taggedHapticStrength, GorillaTagger.Instance.taggedHapticDuration);
                    return;
                }
            }
        }
        else
        {
            GorillaNot.instance.SendReport("inappropriate tag data being sent bonk", **info**.Sender.UserId, **info**.Sender.NickName);
        }
    }

    // Token: 0x06000DB2 RID: 3506 RVA: 0x00028EE7 File Offset: 0x000270E7
    public void AssignDrumToMusicDrums(int **drumIndex**, AudioSource **drum**)
    {
        if (**drumIndex** >= 0 && **drumIndex** < this.musicDrums.Length && **drum** != null)
        {
            this.musicDrums[**drumIndex**] = **drum**;
        }
    }

    // Token: 0x06000DB3 RID: 3507 RVA: 0x0007DA08 File Offset: 0x0007BC08
    public void PlayDrum(int **drumIndex**, float **drumVolume**, PhotonMessageInfo **info**)
    {
        this.IncrementRPC(**info**, "PlayDrum");
        this.senderRig = GorillaGameManager.StaticFindRigForPlayer(**info**.Sender);
        if (this.senderRig == null || this.senderRig.muted)
        {
            return;
        }
        if (**drumIndex** < 0 || **drumIndex** >= this.musicDrums.Length || (this.senderRig.transform.position - base.transform.position).sqrMagnitude > 9f || !float.IsFinite(**drumVolume**))
        {
            GorillaNot.instance.SendReport("inappropriate tag data being sent drum", **info**.Sender.UserId, **info**.Sender.NickName);
            return;
        }
        AudioSource audioSource = this.photonView\.IsMine ? GorillaTagger.Instance.offlineVRRig.musicDrums[**drumIndex**] : this.musicDrums[**drumIndex**];
        if (!audioSource.gameObject.activeSelf)
        {
            return;
        }
        float instrumentVolume = GorillaComputer.instance.instrumentVolume;
        audioSource.time = 0f;
        audioSource.volume = Mathf.Max(Mathf.Min(instrumentVolume, **drumVolume** \* instrumentVolume), 0f);
        audioSource.Play();
    }

    // Token: 0x06000DB4 RID: 3508 RVA: 0x00028F0A File Offset: 0x0002710A
    public void AssignInstrumentToInstrumentSelfOnly(int **instrumentSelfOnlyIndex**, TransferrableObject **instrument**)
    {
        if (**instrumentSelfOnlyIndex** >= 0 && **instrumentSelfOnlyIndex** < this.instrumentSelfOnly.Length && **instrument** != null)
        {
            this.instrumentSelfOnly[**instrumentSelfOnlyIndex**] = **instrument**;
        }
    }

    // Token: 0x06000DB5 RID: 3509 RVA: 0x0007DB2C File Offset: 0x0007BD2C
    public void PlaySelfOnlyInstrument(int **selfOnlyIndex**, int **noteIndex**, float **instrumentVol**, PhotonMessageInfo **info**)
    {
        this.IncrementRPC(**info**, "PlaySelfOnlyInstrument");
        if (**info**.Sender == this.photonView\.Owner && !this.muted)
        {
            if (**selfOnlyIndex** >= 0 && **selfOnlyIndex** < this.instrumentSelfOnly.Length && **info**.Sender == this.photonView\.Owner && float.IsFinite(**instrumentVol**))
            {
                if (this.instrumentSelfOnly[**selfOnlyIndex**].gameObject.activeSelf)
                {
                    this.instrumentSelfOnly[**selfOnlyIndex**].PlayNote(**noteIndex**, Mathf.Max(Mathf.Min(GorillaComputer.instance.instrumentVolume, **instrumentVol** \* GorillaComputer.instance.instrumentVolume), 0f) / 2f);
                    return;
                }
            }
            else
            {
                GorillaNot.instance.SendReport("inappropriate tag data being sent self only instrument", **info**.Sender.UserId, **info**.Sender.NickName);
            }
        }
    }

    // Token: 0x06000DB6 RID: 3510 RVA: 0x0007DC10 File Offset: 0x0007BE10
    public void PlayHandTapLocal(int **soundIndex**, bool **isLeftHand**, float **tapVolume**)
    {
        if (**soundIndex** > -1 && **soundIndex** < GorillaLocomotion.Player.Instance.materialData.Count)
        {
            GorillaLocomotion.Player.MaterialData materialData = GorillaLocomotion.Player.Instance.materialData[**soundIndex**];
            AudioSource audioSource = **isLeftHand** ? this.leftHandPlayer : this.rightHandPlayer;
            audioSource.volume = **tapVolume**;
            AudioClip clip = materialData.overrideAudio ? materialData.audio : GorillaLocomotion.Player.Instance.materialData[0].audio;
            audioSource.PlayOneShot(clip);
        }
    }

    // Token: 0x06000DB7 RID: 3511 RVA: 0x0007DC88 File Offset: 0x0007BE88
    public void PlaySplashEffect(Vector3 **splashPosition**, Quaternion **splashRotation**, float **splashScale**, float **boundingRadius**, bool **bigSplash**, bool **enteringWater**, PhotonMessageInfo **info**)
    {
        this.IncrementRPC(**info**, "PlaySplashEffect");
        if (**info**.Sender == this.photonView\.Owner && **splashPosition**.*IsValid*() && **splashRotation**.*IsValid*() && float.IsFinite(**splashScale**) && float.IsFinite(**boundingRadius**))
        {
            if ((base.transform.position - **splashPosition**).sqrMagnitude < 9f)
            {
                float time = Time.time;
                int num = -1;
                float num2 = time + 10f;
                for (int i = 0; i < this.splashEffectTimes.Length; i++)
                {
                    if (this.splashEffectTimes[i] < num2)
                    {
                        num2 = this.splashEffectTimes[i];
                        num = i;
                    }
                }
                if (time - 0.5f > num2)
                {
                    this.splashEffectTimes[num] = time;
                    **boundingRadius** = Mathf.Clamp(**boundingRadius**, 0.0001f, 0.5f);
                    ObjectPools.instance.Instantiate(GorillaLocomotion.Player.Instance.waterParams.rippleEffect, **splashPosition**, **splashRotation**, GorillaLocomotion.Player.Instance.waterParams.rippleEffectScale \* **boundingRadius** \* 2f);
                    **splashScale** = Mathf.Clamp(**splashScale**, 1E-05f, 1f);
                    ObjectPools.instance.Instantiate(GorillaLocomotion.Player.Instance.waterParams.splashEffect, **splashPosition**, **splashRotation**, **splashScale**).GetComponent\<WaterSplashEffect>().PlayEffect(**bigSplash**, **enteringWater**, **splashScale**, null);
                    return;
                }
            }
        }
        else
        {
            GorillaNot.instance.SendReport("inappropriate tag data being sent splash effect", **info**.Sender.UserId, **info**.Sender.NickName);
        }
    }

    // Token: 0x06000DB8 RID: 3512 RVA: 0x0007DE10 File Offset: 0x0007C010
    [PunRPC]
    public void EnableNonCosmeticHandItemRPC(bool **enable**, bool **isLeftHand**, PhotonMessageInfo **info**)
    {
        this.IncrementRPC(**info**, "EnableNonCosmeticHandItem");
        if (**info**.Sender == this.photonView\.Owner)
        {
            this.senderRig = GorillaGameManager.StaticFindRigForPlayer(**info**.Sender);
            if (this.senderRig == null)
            {
                return;
            }
            if (**isLeftHand** && this.nonCosmeticLeftHandItem)
            {
                this.senderRig.nonCosmeticLeftHandItem.EnableItem(**enable**);
                return;
            }
            if (!**isLeftHand** && this.nonCosmeticRightHandItem)
            {
                this.senderRig.nonCosmeticRightHandItem.EnableItem(**enable**);
                return;
            }
        }
        else
        {
            GorillaNot.instance.SendReport("inappropriate tag data being sent Enable Non Cosmetic Hand Item", **info**.Sender.UserId, **info**.Sender.NickName);
        }
    }

    // Token: 0x06000DB9 RID: 3513 RVA: 0x0007DEC8 File Offset: 0x0007C0C8
    public bool IsMakingFistLeft()
    {
        if (this.isOfflineVRRig)
        {
            return ControllerInputPoller.GripFloat(XRNode.LeftHand) > 0.25f && ControllerInputPoller.TriggerFloat(XRNode.LeftHand) > 0.25f;
        }
        return this.leftIndex.calcT > 0.25f && this.leftMiddle.calcT > 0.25f;
    }

    // Token: 0x06000DBA RID: 3514 RVA: 0x0007DF20 File Offset: 0x0007C120
    public bool IsMakingFistRight()
    {
        if (this.isOfflineVRRig)
        {
            return ControllerInputPoller.GripFloat(XRNode.RightHand) > 0.25f && ControllerInputPoller.TriggerFloat(XRNode.RightHand) > 0.25f;
        }
        return this.rightIndex.calcT > 0.25f && this.rightMiddle.calcT > 0.25f;
    }

    // Token: 0x06000DBB RID: 3515 RVA: 0x00028F2D File Offset: 0x0002712D
    public VRMap GetMakingFist(bool **debug**, out bool **isLeftHand**)
    {
        if (this.IsMakingFistRight())
        {
            **isLeftHand** = false;
            return this.rightHand;
        }
        if (this.IsMakingFistLeft())
        {
            **isLeftHand** = true;
            return this.leftHand;
        }
        **isLeftHand** = false;
        return null;
    }

    // Token: 0x06000DBC RID: 3516 RVA: 0x0007DF78 File Offset: 0x0007C178
    public void PlayGeodeEffect(Vector3 **hitPosition**)
    {
        if ((base.transform.position - **hitPosition**).sqrMagnitude < 9f && this.geodeCrackingSound)
        {
            this.geodeCrackingSound.Play();
        }
    }

    // Token: 0x06000DBD RID: 3517 RVA: 0x0007DFC0 File Offset: 0x0007C1C0
    public void PlayClimbSound(AudioClip **clip**, bool **isLeftHand**)
    {
        if (**isLeftHand**)
        {
            this.leftHandPlayer.volume = 0.1f;
            this.leftHandPlayer.clip = **clip**;
            this.leftHandPlayer.PlayOneShot(this.leftHandPlayer.clip);
            return;
        }
        this.rightHandPlayer.volume = 0.1f;
        this.rightHandPlayer.clip = **clip**;
        this.rightHandPlayer.PlayOneShot(this.rightHandPlayer.clip);
    }

    // Token: 0x06000DBE RID: 3518 RVA: 0x0007E038 File Offset: 0x0007C238
    public void UpdateCosmetics(string[] **currentItems**, PhotonMessageInfo **info**)
    {
        this.IncrementRPC(**info**, "UpdateCosmetics");
        if (**info**.Sender == this.photonView\.Owner && **currentItems**.Length <= 16)
        {
            CosmeticsController.CosmeticSet newSet = new CosmeticsController.CosmeticSet(**currentItems**, CosmeticsController.instance);
            this.LocalUpdateCosmetics(newSet);
            return;
        }
        GorillaNot.instance.SendReport("inappropriate tag data being sent update cosmetics", **info**.Sender.UserId, **info**.Sender.NickName);
    }

    // Token: 0x06000DBF RID: 3519 RVA: 0x0007E0A8 File Offset: 0x0007C2A8
    public void UpdateCosmeticsWithTryon(string[] **currentItems**, string[] **tryOnItems**, PhotonMessageInfo **info**)
    {
        this.IncrementRPC(**info**, "UpdateCosmeticsWithTryon");
        if (**info**.Sender == this.photonView\.Owner && **currentItems**.Length <= 16 && **tryOnItems**.Length <= 16)
        {
            CosmeticsController.CosmeticSet newSet = new CosmeticsController.CosmeticSet(**currentItems**, CosmeticsController.instance);
            CosmeticsController.CosmeticSet newTryOnSet = new CosmeticsController.CosmeticSet(**tryOnItems**, CosmeticsController.instance);
            this.LocalUpdateCosmeticsWithTryon(newSet, newTryOnSet);
            return;
        }
        GorillaNot.instance.SendReport("inappropriate tag data being sent update cosmetics with tryon", **info**.Sender.UserId, **info**.Sender.NickName);
    }

    // Token: 0x06000DC0 RID: 3520 RVA: 0x00028F57 File Offset: 0x00027157
    public void LocalUpdateCosmetics(CosmeticsController.CosmeticSet **newSet**)
    {
        this.cosmeticSet = **newSet**;
        if (this.initializedCosmetics)
        {
            this.SetCosmeticsActive();
        }
    }

    // Token: 0x06000DC1 RID: 3521 RVA: 0x00028F6E File Offset: 0x0002716E
    public void LocalUpdateCosmeticsWithTryon(CosmeticsController.CosmeticSet **newSet**, CosmeticsController.CosmeticSet **newTryOnSet**)
    {
        this.cosmeticSet = **newSet**;
        this.tryOnSet = **newTryOnSet**;
        if (this.initializedCosmetics)
        {
            this.SetCosmeticsActive();
        }
    }

    // Token: 0x06000DC2 RID: 3522 RVA: 0x00028F8C File Offset: 0x0002718C
    private void CheckForEarlyAccess()
    {
        if (this.concatStringOfCosmeticsAllowed.Contains("Early Access Supporter Pack"))
        {
            this.concatStringOfCosmeticsAllowed += "LBAAE.LFAAM.LFAAN.LHAAA.LHAAK.LHAAL.LHAAM.LHAAN.LHAAO.LHAAP.LHABA.LHABB.";
        }
        this.initializedCosmetics = true;
    }

    // Token: 0x06000DC3 RID: 3523 RVA: 0x0007E130 File Offset: 0x0007C330
    public void SetCosmeticsActive()
    {
        if (CosmeticsController.instance == null || !CosmeticsV2Spawner\_Dirty.allPartsInstantiated)
        {
            return;
        }
        this.prevSet.CopyItems(this.mergedSet);
        this.mergedSet.MergeSets(this.inTryOnRoom ? this.tryOnSet : null, this.cosmeticSet);
        BodyDockPositions component = base.GetComponent\<BodyDockPositions>();
        this.mergedSet.ActivateCosmetics(this.prevSet, this, component, CosmeticsController.instance.nullItem, this.cosmeticsObjectRegistry);
    }

    // Token: 0x06000DC4 RID: 3524 RVA: 0x0007E1B4 File Offset: 0x0007C3B4
    public void GetCosmeticsPlayFabCatalogData()
    {
        if (CosmeticsController.instance != null)
        {
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), delegate(GetUserInventoryResult **result**)
            {
                foreach (ItemInstance itemInstance in **result**.Inventory)
                {
                    if (itemInstance.CatalogVersion == CosmeticsController.instance.catalog)
                    {
                        this.concatStringOfCosmeticsAllowed += itemInstance.ItemId;
                    }
                }
                if (CosmeticsV2Spawner\_Dirty.allPartsInstantiated)
                {
                    this.CheckForEarlyAccess();
                    this.SetCosmeticsActive();
                }
            }, delegate(PlayFabError **error**)
            {
                this.initializedCosmetics = true;
                if (CosmeticsV2Spawner\_Dirty.allPartsInstantiated)
                {
                    this.SetCosmeticsActive();
                }
            }, null, null);
        }
        this.concatStringOfCosmeticsAllowed += "Slingshot";
    }

    // Token: 0x06000DC5 RID: 3525 RVA: 0x0007E20C File Offset: 0x0007C40C
    public void GenerateFingerAngleLookupTables()
    {
        this.GenerateTableIndex(ref this.leftIndex);
        this.GenerateTableIndex(ref this.rightIndex);
        this.GenerateTableMiddle(ref this.leftMiddle);
        this.GenerateTableMiddle(ref this.rightMiddle);
        this.GenerateTableThumb(ref this.leftThumb);
        this.GenerateTableThumb(ref this.rightThumb);
    }

    // Token: 0x06000DC6 RID: 3526 RVA: 0x0007E264 File Offset: 0x0007C464
    private void GenerateTableThumb(ref VRMapThumb **thumb**)
    {
        **thumb**.angle1Table = new Quaternion[11];
        **thumb**.angle2Table = new Quaternion[11];
        for (int i = 0; i < **thumb**.angle1Table.Length; i++)
        {
            Debug.Log((float)i / 10f);
            **thumb**.angle1Table[i] = Quaternion.Lerp(Quaternion.Euler(**thumb**.startingAngle1), Quaternion.Euler(**thumb**.closedAngle1), (float)i / 10f);
            **thumb**.angle2Table[i] = Quaternion.Lerp(Quaternion.Euler(**thumb**.startingAngle2), Quaternion.Euler(**thumb**.closedAngle2), (float)i / 10f);
        }
    }

    // Token: 0x06000DC7 RID: 3527 RVA: 0x0007E31C File Offset: 0x0007C51C
    private void GenerateTableIndex(ref VRMapIndex **index**)
    {
        **index**.angle1Table = new Quaternion[11];
        **index**.angle2Table = new Quaternion[11];
        **index**.angle3Table = new Quaternion[11];
        for (int i = 0; i < **index**.angle1Table.Length; i++)
        {
            **index**.angle1Table[i] = Quaternion.Lerp(Quaternion.Euler(**index**.startingAngle1), Quaternion.Euler(**index**.closedAngle1), (float)i / 10f);
            **index**.angle2Table[i] = Quaternion.Lerp(Quaternion.Euler(**index**.startingAngle2), Quaternion.Euler(**index**.closedAngle2), (float)i / 10f);
            **index**.angle3Table[i] = Quaternion.Lerp(Quaternion.Euler(**index**.startingAngle3), Quaternion.Euler(**index**.closedAngle3), (float)i / 10f);
        }
    }

    // Token: 0x06000DC8 RID: 3528 RVA: 0x0007E404 File Offset: 0x0007C604
    private void GenerateTableMiddle(ref VRMapMiddle **middle**)
    {
        **middle**.angle1Table = new Quaternion[11];
        **middle**.angle2Table = new Quaternion[11];
        **middle**.angle3Table = new Quaternion[11];
        for (int i = 0; i < **middle**.angle1Table.Length; i++)
        {
            **middle**.angle1Table[i] = Quaternion.Lerp(Quaternion.Euler(**middle**.startingAngle1), Quaternion.Euler(**middle**.closedAngle1), (float)i / 10f);
            **middle**.angle2Table[i] = Quaternion.Lerp(Quaternion.Euler(**middle**.startingAngle2), Quaternion.Euler(**middle**.closedAngle2), (float)i / 10f);
            **middle**.angle3Table[i] = Quaternion.Lerp(Quaternion.Euler(**middle**.startingAngle3), Quaternion.Euler(**middle**.closedAngle3), (float)i / 10f);
        }
    }

    // Token: 0x06000DC9 RID: 3529 RVA: 0x0007E4EC File Offset: 0x0007C6EC
    private Quaternion SanitizeQuaternion(Quaternion **quat**)
    {
        if (float.IsNaN(**quat**.w) || float.IsNaN(**quat**.x) || float.IsNaN(**quat**.y) || float.IsNaN(**quat**.z) || float.IsInfinity(**quat**.w) || float.IsInfinity(**quat**.x) || float.IsInfinity(**quat**.y) || float.IsInfinity(**quat**.z))
        {
            return Quaternion.identity;
        }
        return **quat**;
    }

    // Token: 0x06000DCA RID: 3530 RVA: 0x0007E568 File Offset: 0x0007C768
    private Vector3 SanitizeVector3(Vector3 **vec**)
    {
        if (float.IsNaN(**vec**.x) || float.IsNaN(**vec**.y) || float.IsNaN(**vec**.z) || float.IsInfinity(**vec**.x) || float.IsInfinity(**vec**.y) || float.IsInfinity(**vec**.z))
        {
            return Vector3.zero;
        }
        return Vector3.ClampMagnitude(**vec**, 1000f);
    }

    // Token: 0x06000DCB RID: 3531 RVA: 0x00028FBD File Offset: 0x000271BD
    private void IncrementRPC(PhotonMessageInfoWrapped **info**, string **sourceCall**)
    {
        if (GorillaGameManager.instance != null)
        {
            GorillaNot.IncrementRPCCall(**info**, **sourceCall**);
        }
    }

    // Token: 0x06000DCC RID: 3532 RVA: 0x00028FD3 File Offset: 0x000271D3
    private void IncrementRPC(PhotonMessageInfo **info**, string **sourceCall**)
    {
        if (GorillaGameManager.instance != null)
        {
            GorillaNot.IncrementRPCCall(**info**, **sourceCall**);
        }
    }

    // Token: 0x06000DCD RID: 3533 RVA: 0x0007E5D4 File Offset: 0x0007C7D4
    private void AddVelocityToQueue(Vector3 **position**, double **serverTime**)
    {
        Vector3 velocity;
        if (this.velocityHistoryList.Count == 0)
        {
            velocity = Vector3.zero;
            this.lastPosition = **position**;
        }
        else
        {
            velocity = (**position** - this.lastPosition) / (float)(**serverTime** - this.velocityHistoryList[0].time);
        }
        this.velocityHistoryList.Insert(0, new VRRig.VelocityTime(velocity, **serverTime**));
        if (this.velocityHistoryList.Count > this.velocityHistoryMaxLength)
        {
            this.velocityHistoryList.RemoveRange(this.velocityHistoryMaxLength, this.velocityHistoryList.Count - this.velocityHistoryMaxLength);
        }
    }

    // Token: 0x06000DCE RID: 3534 RVA: 0x0007E66C File Offset: 0x0007C86C
    private Vector3 ReturnVelocityAtTime(double **timeToReturn**)
    {
        if (this.velocityHistoryList.Count <= 1)
        {
            return Vector3.zero;
        }
        int num = 0;
        int num2 = this.velocityHistoryList.Count - 1;
        int num3 = 0;
        if (num2 == num)
        {
            return this.velocityHistoryList[num].vel;
        }
        while (num2 - num > 1 && num3 < 1000)
        {
            num3++;
            int num4 = (num2 - num) / 2;
            if (this.velocityHistoryList[num4].time > **timeToReturn**)
            {
                num2 = num4;
            }
            else
            {
                num = num4;
            }
        }
        float num5 = (float)(this.velocityHistoryList[num].time - **timeToReturn**);
        double num6 = this.velocityHistoryList[num].time - this.velocityHistoryList[num2].time;
        if (num6 == 0.0)
        {
            num6 = 0.001;
        }
        num5 /= (float)num6;
        num5 = Mathf.Clamp(num5, 0f, 1f);
        return Vector3.Lerp(this.velocityHistoryList[num].vel, this.velocityHistoryList[num2].vel, num5);
    }

    // Token: 0x06000DCF RID: 3535 RVA: 0x00028FE9 File Offset: 0x000271E9
    public Vector3 LatestVelocity()
    {
        if (this.velocityHistoryList.Count > 0)
        {
            return this.velocityHistoryList[0].vel;
        }
        return Vector3.zero;
    }

    // Token: 0x06000DD0 RID: 3536 RVA: 0x00029010 File Offset: 0x00027210
    public bool CheckDistance(Vector3 **position**, float **max**)
    {
        **max** = **max** \* **max** \* this.scaleFactor;
        return Vector3.SqrMagnitude(this.syncPos - **position**) < **max**;
    }

    // Token: 0x06000DD1 RID: 3537 RVA: 0x0007E780 File Offset: 0x0007C980
    public bool CheckTagDistanceRollback(VRRig **otherRig**, float **max**, float **timeInterval**)
    {
        Vector3 a;
        Vector3 b;
        GorillaMath.LineSegClosestPoints(this.syncPos, -this.LatestVelocity() \* **timeInterval**, **otherRig**.syncPos, -**otherRig**.LatestVelocity() \* **timeInterval**, out a, out b);
        return Vector3.SqrMagnitude(a - b) < **max** \* **max** \* this.scaleFactor;
    }

    // Token: 0x1400001C RID: 28
    // (add) Token: 0x06000DD2 RID: 3538 RVA: 0x0007E7DC File Offset: 0x0007C9DC
    // (remove) Token: 0x06000DD3 RID: 3539 RVA: 0x0007E814 File Offset: 0x0007CA14
    public event Action\<Color> OnColorChanged;

    // Token: 0x06000DD4 RID: 3540 RVA: 0x0007E84C File Offset: 0x0007CA4C
    public void SetColor(Color **color**)
    {
        Action\<Color> onColorChanged = this.OnColorChanged;
        if (onColorChanged != null)
        {
            onColorChanged(**color**);
        }
        Action\<Color> action = this.onColorInitialized;
        if (action != null)
        {
            action(**color**);
        }
        this.onColorInitialized = delegate(Color **color1**)
        {
        };
        this.colorInitialized = true;
        this.playerColor = **color**;
    }

    // Token: 0x06000DD5 RID: 3541 RVA: 0x00029032 File Offset: 0x00027232
    public void OnColorInitialized(Action\<Color> **action**)
    {
        if (this.colorInitialized)
        {
            **action**(this.playerColor);
            return;
        }
        this.onColorInitialized = (Action\<Color>)Delegate.Combine(this.onColorInitialized, **action**);
    }

    // Token: 0x06000DD6 RID: 3542 RVA: 0x0007E8B0 File Offset: 0x0007CAB0
    private void OnEnable()
    {
        if (this.currentRopeSwingTarget != null)
        {
            this.currentRopeSwingTarget.SetParent(null);
        }
        if (!this.isOfflineVRRig)
        {
            PlayerCosmeticsSystem.RegisterCosmeticCallback(this.creator.ActorNumber, this);
        }
        if (!this.isOfflineVRRig)
        {
            VRRigJobManager.Instance.RegisterVRRig(this);
        }
    }

    // Token: 0x06000DD7 RID: 3543 RVA: 0x0007E904 File Offset: 0x0007CB04
    void IPreDisable.PreDisable()
    {
        this.ClearRopeData();
        if (this.currentRopeSwingTarget)
        {
            this.currentRopeSwingTarget.SetParent(base.transform);
        }
        this.EnableHuntWatch(false);
        this.EnableBattleCosmetics(false);
        this.ClearPartyMemberStatus();
        this.concatStringOfCosmeticsAllowed = "";
        this.rawCosmeticString = "";
        if (this.cosmeticSet != null)
        {
            this.mergedSet.DeactivateAllCosmetcs(this.myBodyDockPositions, CosmeticsController.instance.nullItem, this.cosmeticsObjectRegistry);
            this.mergedSet.ClearSet(CosmeticsController.instance.nullItem);
            this.prevSet.ClearSet(CosmeticsController.instance.nullItem);
            this.tryOnSet.ClearSet(CosmeticsController.instance.nullItem);
            this.cosmeticSet.ClearSet(CosmeticsController.instance.nullItem);
        }
        if (!this.isOfflineVRRig)
        {
            PlayerCosmeticsSystem.RemoveCosmeticCallback(this.creator.ActorNumber);
            this.pendingCosmeticUpdate = true;
        }
    }

    // Token: 0x06000DD8 RID: 3544 RVA: 0x0007EA08 File Offset: 0x0007CC08
    private void OnDisable()
    {
        this.initialized = false;
        this.muted = false;
        this.photonView = null;
        this.voiceAudio = null;
        this.tempRig = null;
        this.timeSpawned = 0f;
        this.initializedCosmetics = false;
        this.velocityHistoryList.Clear();
        this.tempMatIndex = 0;
        this.setMatIndex = 0;
        this.ChangeMaterialLocal(this.setMatIndex);
        this.currentCosmeticTries = 0;
        this.creator = null;
        try
        {
            CallLimitType\<CallLimiter>[] callSettings = this.fxSettings.callSettings;
            for (int i = 0; i < callSettings.Length; i++)
            {
                callSettings[i].CallLimitSettings.Reset();
            }
        }
        catch
        {
            Debug.LogError("fxtype missing in fxSettings, please fix or remove this");
        }
        if (!this.isOfflineVRRig)
        {
            VRRigJobManager.Instance.DeregisterVRRig(this);
        }
    }

    // Token: 0x06000DD9 RID: 3545 RVA: 0x0007EAD8 File Offset: 0x0007CCD8
    public void NetInitialize()
    {
        this.timeSpawned = Time.time;
        if (NetworkSystem.Instance.InRoom)
        {
            GorillaGameManager instance = GorillaGameManager.instance;
            if (instance != null)
            {
                if (instance is GorillaHuntManager || instance.GameModeName() == "HUNT")
                {
                    this.EnableHuntWatch(true);
                }
                else if (instance is GorillaBattleManager || instance.GameModeName() == "BATTLE")
                {
                    this.EnableBattleCosmetics(true);
                }
            }
            else
            {
                string gameModeString = NetworkSystem.Instance.GameModeString;
                if (!gameModeString.*IsNullOrEmpty*())
                {
                    string text = gameModeString;
                    if (text.Contains("HUNT"))
                    {
                        this.EnableHuntWatch(true);
                    }
                    else if (text.Contains("BATTLE"))
                    {
                        this.EnableBattleCosmetics(true);
                    }
                }
            }
            this.UpdateFriendshipBracelet();
            if (this.IsLocalPartyMember && !this.isOfflineVRRig)
            {
                FriendshipGroupDetection.Instance.SendVerifyPartyMember(this.creator);
            }
        }
        if (this.photonView != null)
        {
            base.transform.position = this.photonView\.gameObject.transform.position;
            base.transform.rotation = this.photonView\.gameObject.transform.rotation;
        }
        try
        {
            Action action = VRRig.newPlayerJoined;
            if (action != null)
            {
                action();
            }
        }
        catch (Exception message)
        {
            Debug.LogError(message);
        }
    }

    // Token: 0x06000DDA RID: 3546 RVA: 0x0007EC34 File Offset: 0x0007CE34
    public void UpdateFriendshipBracelet()
    {
        bool flag = false;
        if (this.isOfflineVRRig)
        {
            bool flag2 = false;
            VRRig.PartyMemberStatus partyMemberStatus = this.GetPartyMemberStatus();
            if (partyMemberStatus != VRRig.PartyMemberStatus.InLocalParty)
            {
                if (partyMemberStatus == VRRig.PartyMemberStatus.NotInLocalParty)
                {
                    flag2 = false;
                    this.reliableState.isBraceletLeftHanded = false;
                }
            }
            else
            {
                flag2 = true;
                this.reliableState.isBraceletLeftHanded = (FriendshipGroupDetection.Instance.DidJoinLeftHanded && !this.huntComputer.activeSelf);
            }
            if (this.reliableState.HasBracelet != flag2 || this.reliableState.braceletBeadColors.Count != FriendshipGroupDetection.Instance.myBeadColors.Count)
            {
                this.reliableState.SetIsDirty();
                flag = (this.reliableState.HasBracelet == flag2);
            }
            this.reliableState.braceletBeadColors.Clear();
            if (flag2)
            {
                this.reliableState.braceletBeadColors.AddRange(FriendshipGroupDetection.Instance.myBeadColors);
            }
            this.reliableState.braceletSelfIndex = FriendshipGroupDetection.Instance.MyBraceletSelfIndex;
        }
        if (this.nonCosmeticLeftHandItem != null)
        {
            bool flag3 = this.reliableState.HasBracelet && this.reliableState.isBraceletLeftHanded;
            this.nonCosmeticLeftHandItem.EnableItem(flag3);
            if (flag3)
            {
                this.friendshipBraceletLeftHand.UpdateBeads(this.reliableState.braceletBeadColors, this.reliableState.braceletSelfIndex);
                if (flag)
                {
                    this.friendshipBraceletLeftHand.PlayAppearEffects();
                }
            }
        }
        if (this.nonCosmeticRightHandItem != null)
        {
            bool flag4 = this.reliableState.HasBracelet && !this.reliableState.isBraceletLeftHanded;
            this.nonCosmeticRightHandItem.EnableItem(flag4);
            if (flag4)
            {
                this.friendshipBraceletRightHand.UpdateBeads(this.reliableState.braceletBeadColors, this.reliableState.braceletSelfIndex);
                if (flag)
                {
                    this.friendshipBraceletRightHand.PlayAppearEffects();
                }
            }
        }
    }

    // Token: 0x06000DDB RID: 3547 RVA: 0x0007EDF0 File Offset: 0x0007CFF0
    public void EnableHuntWatch(bool **on**)
    {
        this.huntComputer.SetActive(**on**);
        if (this.builderResizeWatch != null)
        {
            MeshRenderer component = this.builderResizeWatch.GetComponent\<MeshRenderer>();
            if (component != null)
            {
                component.enabled = !**on**;
            }
        }
    }

    // Token: 0x06000DDC RID: 3548 RVA: 0x00029060 File Offset: 0x00027260
    public void EnableBattleCosmetics(bool **on**)
    {
        this.battleBalloons.gameObject.SetActive(**on**);
    }

    // Token: 0x06000DDD RID: 3549 RVA: 0x0007EE38 File Offset: 0x0007D038
    public void EnableBuilderResizeWatch(bool **on**)
    {
        if (this.builderResizeWatch != null && this.builderResizeWatch.activeSelf != **on**)
        {
            this.builderResizeWatch.SetActive(**on**);
        }
        if (this.isOfflineVRRig)
        {
            bool flag = this.reliableState.isBuilderWatchEnabled != **on**;
            this.reliableState.isBuilderWatchEnabled = **on**;
            if (flag)
            {
                this.reliableState.SetIsDirty();
            }
        }
    }

    // Token: 0x06000DDE RID: 3550 RVA: 0x0007EEA0 File Offset: 0x0007D0A0
    private void UpdateReplacementVoice()
    {
        if (this.remoteUseReplacementVoice || this.localUseReplacementVoice || GorillaComputer.instance.voiceChatOn != "TRUE")
        {
            this.voiceAudio.mute = true;
            return;
        }
        this.voiceAudio.mute = false;
    }

    // Token: 0x06000DDF RID: 3551 RVA: 0x0007EEF0 File Offset: 0x0007D0F0
    public bool ShouldPlayReplacementVoice()
    {
        return this.photonView && !this.photonView\.IsMine && !(GorillaComputer.instance.voiceChatOn == "OFF") && (this.remoteUseReplacementVoice || this.localUseReplacementVoice || GorillaComputer.instance.voiceChatOn == "FALSE") && this.speakingLoudness > this.replacementVoiceLoudnessThreshold;
    }

    // Token: 0x170001A2 RID: 418
    // (get) Token: 0x06000DE0 RID: 3552 RVA: 0x00029073 File Offset: 0x00027273
    // (set) Token: 0x06000DE1 RID: 3553 RVA: 0x0002907B File Offset: 0x0002727B
    bool IUserCosmeticsCallback.PendingUpdate
    {
        get
        {
            return this.pendingCosmeticUpdate;
        }
        set
        {
            this.pendingCosmeticUpdate = value;
        }
    }

    // Token: 0x06000DE2 RID: 3554 RVA: 0x0007EF6C File Offset: 0x0007D16C
    bool IUserCosmeticsCallback.OnGetUserCosmetics(string **cosmetics**)
    {
        if (**cosmetics** == this.rawCosmeticString && this.currentCosmeticTries < this.cosmeticRetries)
        {
            this.currentCosmeticTries++;
            return false;
        }
        this.rawCosmeticString = (**cosmetics** ?? "");
        this.concatStringOfCosmeticsAllowed = this.rawCosmeticString;
        this.initializedCosmetics = true;
        this.currentCosmeticTries = 0;
        this.CheckForEarlyAccess();
        this.SetCosmeticsActive();
        this.myBodyDockPositions.RefreshTransferrableItems();
        PhotonView photonView = this.photonView;
        if (photonView != null)
        {
            photonView\.RPC("RequestCosmetics", this.photonView\.Owner, Array.Empty\<object>());
        }
        return true;
    }

    // Token: 0x06000DE3 RID: 3555 RVA: 0x00029084 File Offset: 0x00027284
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CacheLocalRig()
    {
        if (VRRig.gLocalRig != null)
        {
            return;
        }
        GameObject gameObject = GameObject.Find("Local Gorilla Player");
        VRRig.gLocalRig = ((gameObject != null) ? gameObject.GetComponentInChildren\<VRRig>() : null);
        VRRig.bCachedLocalRig = true;
    }

    // Token: 0x170001A3 RID: 419
    // (get) Token: 0x06000DE4 RID: 3556 RVA: 0x000290B5 File Offset: 0x000272B5
    public static VRRig LocalRig
    {
        get
        {
            return VRRig.gLocalRig;
        }
    }

    // Token: 0x170001A4 RID: 420
    // (get) Token: 0x06000DE5 RID: 3557 RVA: 0x000290BC File Offset: 0x000272BC
    public bool isLocal
    {
        get
        {
            return VRRig.gLocalRig == this;
        }
    }

    // Token: 0x04000EC0 RID: 3776
    private bool \_isListeningFor\_OnPostInstantiateAllPrefabs;

    // Token: 0x04000EC1 RID: 3777
    public static Action newPlayerJoined;

    // Token: 0x04000EC2 RID: 3778
    public VRMap head;

    // Token: 0x04000EC3 RID: 3779
    public VRMap rightHand;

    // Token: 0x04000EC4 RID: 3780
    public VRMap leftHand;

    // Token: 0x04000EC5 RID: 3781
    public VRMapThumb leftThumb;

    // Token: 0x04000EC6 RID: 3782
    public VRMapIndex leftIndex;

    // Token: 0x04000EC7 RID: 3783
    public VRMapMiddle leftMiddle;

    // Token: 0x04000EC8 RID: 3784
    public VRMapThumb rightThumb;

    // Token: 0x04000EC9 RID: 3785
    public VRMapIndex rightIndex;

    // Token: 0x04000ECA RID: 3786
    public VRMapMiddle rightMiddle;

    // Token: 0x04000ECB RID: 3787
    private int previousGrabbedRope = -1;

    // Token: 0x04000ECC RID: 3788
    private int previousGrabbedRopeBoneIndex;

    // Token: 0x04000ECD RID: 3789
    private bool previousGrabbedRopeWasLeft;

    // Token: 0x04000ECE RID: 3790
    private GorillaRopeSwing currentRopeSwing;

    // Token: 0x04000ECF RID: 3791
    private Transform currentHoldParent;

    // Token: 0x04000ED0 RID: 3792
    private Transform currentRopeSwingTarget;

    // Token: 0x04000ED1 RID: 3793
    private float lastRopeGrabTimer;

    // Token: 0x04000ED2 RID: 3794
    private bool shouldLerpToRope;

    // Token: 0x04000ED3 RID: 3795
    [NonSerialized]
    public int grabbedRopeIndex = -1;

    // Token: 0x04000ED4 RID: 3796
    [NonSerialized]
    public int grabbedRopeBoneIndex;

    // Token: 0x04000ED5 RID: 3797
    [NonSerialized]
    public bool grabbedRopeIsLeft;

    // Token: 0x04000ED6 RID: 3798
    [NonSerialized]
    public Vector3 grabbedRopeOffset = Vector3.zero;

    // Token: 0x04000ED7 RID: 3799
    [NonSerialized]
    public float targetScale = 1f;

    // Token: 0x04000ED8 RID: 3800
    [Tooltip("- False in 'Gorilla Player Networked.prefab'.\n- True in 'Local VRRig.prefab/Local Gorilla Player'.\n- False in 'Local VRRig.prefab/Actual Gorilla'")]
    public bool isOfflineVRRig;

    // Token: 0x04000ED9 RID: 3801
    public GameObject mainCamera;

    // Token: 0x04000EDA RID: 3802
    public Transform playerOffsetTransform;

    // Token: 0x04000EDB RID: 3803
    public int SDKIndex;

    // Token: 0x04000EDC RID: 3804
    public bool isMyPlayer;

    // Token: 0x04000EDD RID: 3805
    public AudioSource leftHandPlayer;

    // Token: 0x04000EDE RID: 3806
    public AudioSource rightHandPlayer;

    // Token: 0x04000EDF RID: 3807
    public AudioSource tagSound;

    // Token: 0x04000EE0 RID: 3808
    [SerializeField]
    private float ratio;

    // Token: 0x04000EE1 RID: 3809
    public Transform headConstraint;

    // Token: 0x04000EE2 RID: 3810
    public Vector3 headBodyOffset = Vector3.zero;

    // Token: 0x04000EE3 RID: 3811
    public GameObject headMesh;

    // Token: 0x04000EE4 RID: 3812
    public Vector3 syncPos;

    // Token: 0x04000EE5 RID: 3813
    public Vector3 jobPos;

    // Token: 0x04000EE6 RID: 3814
    public Quaternion syncRotation;

    // Token: 0x04000EE7 RID: 3815
    public Quaternion jobRotation;

    // Token: 0x04000EE8 RID: 3816
    public AudioClip[] clipToPlay;

    // Token: 0x04000EE9 RID: 3817
    public AudioClip[] handTapSound;

    // Token: 0x04000EEA RID: 3818
    public int currentMatIndex;

    // Token: 0x04000EEB RID: 3819
    public int setMatIndex;

    // Token: 0x04000EEC RID: 3820
    private int tempMatIndex;

    // Token: 0x04000EED RID: 3821
    public float lerpValueFingers;

    // Token: 0x04000EEE RID: 3822
    public float lerpValueBody;

    // Token: 0x04000EEF RID: 3823
    public GameObject backpack;

    // Token: 0x04000EF0 RID: 3824
    public Transform leftHandTransform;

    // Token: 0x04000EF1 RID: 3825
    public Transform rightHandTransform;

    // Token: 0x04000EF2 RID: 3826
    public SkinnedMeshRenderer mainSkin;

    // Token: 0x04000EF3 RID: 3827
    public GorillaSkin defaultSkin;

    // Token: 0x04000EF4 RID: 3828
    public ZoneEntity zoneEntity;

    // Token: 0x04000EF5 RID: 3829
    public Material myDefaultSkinMaterialInstance;

    // Token: 0x04000EF6 RID: 3830
    public Material scoreboardMaterial;

    // Token: 0x04000EF7 RID: 3831
    public GameObject spectatorSkin;

    // Token: 0x04000EF8 RID: 3832
    public int handSync;

    // Token: 0x04000EF9 RID: 3833
    public Material[] materialsToChangeTo;

    // Token: 0x04000EFA RID: 3834
    public float red;

    // Token: 0x04000EFB RID: 3835
    public float green;

    // Token: 0x04000EFC RID: 3836
    public float blue;

    // Token: 0x04000EFD RID: 3837
    public string playerName;

    // Token: 0x04000EFE RID: 3838
    public Text playerText;

    // Token: 0x04000EFF RID: 3839
    public string playerNameVisible;

    // Token: 0x04000F00 RID: 3840
    [Tooltip("- True in 'Gorilla Player Networked.prefab'.\n- True in 'Local VRRig.prefab/Local Gorilla Player'.\n- False in 'Local VRRig.prefab/Actual Gorilla'")]
    public bool showName;

    // Token: 0x04000F01 RID: 3841
    public CosmeticItemRegistry cosmeticsObjectRegistry = new CosmeticItemRegistry();

    // Token: 0x04000F02 RID: 3842
    [FormerlySerializedAs("cosmetics")]
    public GameObject[] \_cosmetics;

    // Token: 0x04000F03 RID: 3843
    [FormerlySerializedAs("overrideCosmetics")]
    public GameObject[] \_overrideCosmetics;

    // Token: 0x04000F04 RID: 3844
    public string concatStringOfCosmeticsAllowed = "";

    // Token: 0x04000F05 RID: 3845
    public bool initializedCosmetics;

    // Token: 0x04000F06 RID: 3846
    public CosmeticsController.CosmeticSet cosmeticSet;

    // Token: 0x04000F07 RID: 3847
    public CosmeticsController.CosmeticSet tryOnSet;

    // Token: 0x04000F08 RID: 3848
    public CosmeticsController.CosmeticSet mergedSet;

    // Token: 0x04000F09 RID: 3849
    public CosmeticsController.CosmeticSet prevSet;

    // Token: 0x04000F0A RID: 3850
    private int cosmeticRetries = 2;

    // Token: 0x04000F0B RID: 3851
    private int currentCosmeticTries;

    // Token: 0x04000F0C RID: 3852
    public SizeManager sizeManager;

    // Token: 0x04000F0D RID: 3853
    public float pitchScale = 0.3f;

    // Token: 0x04000F0E RID: 3854
    public float pitchOffset = 1f;

    // Token: 0x04000F0F RID: 3855
    [NonSerialized]
    public bool IsHaunted;

    // Token: 0x04000F10 RID: 3856
    public float HauntedVoicePitch = 0.5f;

    // Token: 0x04000F11 RID: 3857
    public float HauntedHearingVolume = 0.15f;

    // Token: 0x04000F12 RID: 3858
    [NonSerialized]
    public bool UsingHauntedRing;

    // Token: 0x04000F13 RID: 3859
    [NonSerialized]
    public float HauntedRingVoicePitch;

    // Token: 0x04000F14 RID: 3860
    public FriendshipBracelet friendshipBraceletLeftHand;

    // Token: 0x04000F15 RID: 3861
    public NonCosmeticHandItem nonCosmeticLeftHandItem;

    // Token: 0x04000F16 RID: 3862
    public FriendshipBracelet friendshipBraceletRightHand;

    // Token: 0x04000F17 RID: 3863
    public NonCosmeticHandItem nonCosmeticRightHandItem;

    // Token: 0x04000F18 RID: 3864
    public VRRigReliableState reliableState;

    // Token: 0x04000F19 RID: 3865
    [SerializeField]
    private Transform MouthPosition;

    // Token: 0x04000F1A RID: 3866
    internal RigContainer rigContainer;

    // Token: 0x04000F1B RID: 3867
    private Vector3 remoteVelocity;

    // Token: 0x04000F1C RID: 3868
    private double remoteLatestTimestamp;

    // Token: 0x04000F1D RID: 3869
    private Vector3 remoteCorrectionNeeded;

    // Token: 0x04000F1E RID: 3870
    private const float REMOTE\_CORRECTION\_RATE = 5f;

    // Token: 0x04000F1F RID: 3871
    private const bool USE\_NEW\_NETCODE = false;

    // Token: 0x04000F20 RID: 3872
    private VRRig.PartyMemberStatus partyMemberStatus;

    // Token: 0x04000F21 RID: 3873
    public static readonly GTBitOps.BitWriteInfo[] WearablePackedStatesBitWriteInfos = new GTBitOps.BitWriteInfo[]
    {
        new GTBitOps.BitWriteInfo(0, 1),
        new GTBitOps.BitWriteInfo(1, 2),
        new GTBitOps.BitWriteInfo(3, 2)
    };

    // Token: 0x04000F22 RID: 3874
    public bool inTryOnRoom;

    // Token: 0x04000F23 RID: 3875
    public bool muted;

    // Token: 0x04000F24 RID: 3876
    public float scaleFactor;

    // Token: 0x04000F25 RID: 3877
    public float lastScaleFactor;

    // Token: 0x04000F26 RID: 3878
    private float timeSpawned;

    // Token: 0x04000F27 RID: 3879
    public float doNotLerpConstant = 1f;

    // Token: 0x04000F28 RID: 3880
    public string tempString;

    // Token: 0x04000F29 RID: 3881
    private Photon.Realtime.Player tempPlayer;

    // Token: 0x04000F2A RID: 3882
    internal Photon.Realtime.Player creator;

    // Token: 0x04000F2B RID: 3883
    internal NetPlayer creatorWrapped;

    // Token: 0x04000F2C RID: 3884
    private VRRig tempRig;

    // Token: 0x04000F2D RID: 3885
    private float[] speedArray;

    // Token: 0x04000F2E RID: 3886
    private double handLerpValues;

    // Token: 0x04000F2F RID: 3887
    private bool initialized;

    // Token: 0x04000F30 RID: 3888
    public BattleBalloons battleBalloons;

    // Token: 0x04000F31 RID: 3889
    private int tempInt;

    // Token: 0x04000F32 RID: 3890
    public BodyDockPositions myBodyDockPositions;

    // Token: 0x04000F33 RID: 3891
    public ParticleSystem lavaParticleSystem;

    // Token: 0x04000F34 RID: 3892
    public ParticleSystem rockParticleSystem;

    // Token: 0x04000F35 RID: 3893
    public ParticleSystem iceParticleSystem;

    // Token: 0x04000F36 RID: 3894
    public string tempItemName;

    // Token: 0x04000F37 RID: 3895
    public CosmeticsController.CosmeticItem tempItem;

    // Token: 0x04000F38 RID: 3896
    public string tempItemId;

    // Token: 0x04000F39 RID: 3897
    public int tempItemCost;

    // Token: 0x04000F3A RID: 3898
    public int leftHandHoldableStatus;

    // Token: 0x04000F3B RID: 3899
    public int rightHandHoldableStatus;

    // Token: 0x04000F3C RID: 3900
    [Tooltip("This has to match the drumsAS array in DrumsItem.cs.")]
    [SerializeReference]
    public AudioSource[] musicDrums;

    // Token: 0x04000F3D RID: 3901
    public TransferrableObject[] instrumentSelfOnly;

    // Token: 0x04000F3E RID: 3902
    public AudioSource geodeCrackingSound;

    // Token: 0x04000F3F RID: 3903
    public float bonkTime;

    // Token: 0x04000F40 RID: 3904
    public float bonkCooldown = 2f;

    // Token: 0x04000F41 RID: 3905
    private VRRig tempVRRig;

    // Token: 0x04000F42 RID: 3906
    public GameObject huntComputer;

    // Token: 0x04000F43 RID: 3907
    public GameObject builderResizeWatch;

    // Token: 0x04000F44 RID: 3908
    public Slingshot slingshot;

    // Token: 0x04000F45 RID: 3909
    public Slingshot.SlingshotState slingshotState;

    // Token: 0x04000F46 RID: 3910
    private PhotonVoiceView myPhotonVoiceView;

    // Token: 0x04000F47 RID: 3911
    private VRRig senderRig;

    // Token: 0x04000F48 RID: 3912
    private bool isInitialized;

    // Token: 0x04000F49 RID: 3913
    private List\<VRRig.VelocityTime> velocityHistoryList = new List\<VRRig.VelocityTime>();

    // Token: 0x04000F4A RID: 3914
    public int velocityHistoryMaxLength = 200;

    // Token: 0x04000F4B RID: 3915
    private Vector3 lastPosition;

    // Token: 0x04000F4C RID: 3916
    public const int splashLimitCount = 4;

    // Token: 0x04000F4D RID: 3917
    public const float splashLimitCooldown = 0.5f;

    // Token: 0x04000F4E RID: 3918
    private float[] splashEffectTimes = new float[4];

    // Token: 0x04000F4F RID: 3919
    internal AudioSource voiceAudio;

    // Token: 0x04000F50 RID: 3920
    public bool remoteUseReplacementVoice;

    // Token: 0x04000F51 RID: 3921
    public bool localUseReplacementVoice;

    // Token: 0x04000F52 RID: 3922
    private MicWrapper currentMicWrapper;

    // Token: 0x04000F53 RID: 3923
    private IAudioDesc audioDesc;

    // Token: 0x04000F54 RID: 3924
    private float speakingLoudness;

    // Token: 0x04000F55 RID: 3925
    public bool shouldSendSpeakingLoudness = true;

    // Token: 0x04000F56 RID: 3926
    public float replacementVoiceLoudnessThreshold = 0.05f;

    // Token: 0x04000F57 RID: 3927
    public int replacementVoiceDetectionDelay = 128;

    // Token: 0x04000F58 RID: 3928
    private GorillaMouthFlap myMouthFlap;

    // Token: 0x04000F59 RID: 3929
    private GorillaSpeakerLoudness mySpeakerLoudness;

    // Token: 0x04000F5A RID: 3930
    public ReplacementVoice myReplacementVoice;

    // Token: 0x04000F5B RID: 3931
    private GorillaEyeExpressions myEyeExpressions;

    // Token: 0x04000F5C RID: 3932
    [SerializeField]
    internal PhotonView photonView;

    // Token: 0x04000F5D RID: 3933
    [SerializeField]
    internal VRRigSerializer rigSerializer;

    // Token: 0x04000F5E RID: 3934
    public NetPlayer OwningNetPlayer;

    // Token: 0x04000F5F RID: 3935
    [SerializeField]
    private FXSystemSettings sharedFXSettings;

    // Token: 0x04000F60 RID: 3936
    [NonSerialized]
    public FXSystemSettings fxSettings;

    // Token: 0x04000F61 RID: 3937
    private bool playerWasHaunted;

    // Token: 0x04000F62 RID: 3938
    private float nonHauntedVolume;

    // Token: 0x04000F63 RID: 3939
    private const float QPackMax = 0.707107f;

    // Token: 0x04000F64 RID: 3940
    private const float QPackScale = 361.33145f;

    // Token: 0x04000F65 RID: 3941
    private const float QPackInvScale = 0.0027675421f;

    // Token: 0x04000F66 RID: 3942
    public Color playerColor;

    // Token: 0x04000F67 RID: 3943
    public bool colorInitialized;

    // Token: 0x04000F68 RID: 3944
    private Action\<Color> onColorInitialized;

    // Token: 0x04000F6A RID: 3946
    private bool pendingCosmeticUpdate = true;

    // Token: 0x04000F6B RID: 3947
    private string rawCosmeticString = "";

    // Token: 0x04000F6C RID: 3948
    [DebugReadOnly]
    private static VRRig gLocalRig;

    // Token: 0x04000F6D RID: 3949
    private static bool bCachedLocalRig;

    // Token: 0x0200026E RID: 622
    public enum PartyMemberStatus
    {
        // Token: 0x04000F6F RID: 3951
        NeedsUpdate,
        // Token: 0x04000F70 RID: 3952
        InLocalParty,
        // Token: 0x04000F71 RID: 3953
        NotInLocalParty
    }

    // Token: 0x0200026F RID: 623
    public enum WearablePackedStateSlots
    {
        // Token: 0x04000F73 RID: 3955
        Hat,
        // Token: 0x04000F74 RID: 3956
        LeftHand,
        // Token: 0x04000F75 RID: 3957
        RightHand
    }

    // Token: 0x02000270 RID: 624
    public struct VelocityTime
    {
        // Token: 0x06000DEA RID: 3562 RVA: 0x00029116 File Offset: 0x00027316
        public VelocityTime(Vector3 **velocity**, double **velTime**)
        {
            this.vel = **velocity**;
            this.time = **velTime**;
        }

        // Token: 0x04000F76 RID: 3958
        public Vector3 vel;

        // Token: 0x04000F77 RID: 3959
        public double time;
    }

    // Token: 0x02000271 RID: 625
    private enum QAxis
    {
        // Token: 0x04000F79 RID: 3961
        X,
        // Token: 0x04000F7A RID: 3962
        Y,
        // Token: 0x04000F7B RID: 3963
        Z,
        // Token: 0x04000F7C RID: 3964
        W
    }
}
  

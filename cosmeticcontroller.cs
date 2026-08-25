using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ExitGames.Client.Photon;
using GorillaLocomotion;
using GorillaTag;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;



namespace GorillaNetworking
{
    // Token: 0x02000599 RID: 1433
    public class CosmeticsController : MonoBehaviour
    {
        // Token: 0x060020D1 RID: 8401 RVA: 0x0003027B File Offset: 0x0002E47B
        public void AddWardrobeInstance(WardrobeInstance instance)
        {
            this.wardrobes.Add(instance);
            this.UpdateWardrobeModelsAndButtons();
        }



        // Token: 0x060020D2 RID: 8402 RVA: 0x0003028F File Offset: 0x0002E48F
        public void RemoveWardrobeInstance(WardrobeInstance instance)
        {
            this.wardrobes.Remove(instance);
        }



        // Token: 0x060020D3 RID: 8403 RVA: 0x000B96BC File Offset: 0x000B78BC
        public void Awake()
        {
            if (CosmeticsController.instance == null)
            {
                CosmeticsController.instance = this;
            }
            else if (CosmeticsController.instance != this)
            {
                UnityEngine.Object.Destroy(base.gameObject);
            }
            if (base.gameObject.activeSelf)
            {
                this.catalog = "DLC";
                this.currencyName = "SR";
                this.nullItem = this.allCosmetics[0];
                this.nullItem.isNullItem = true;
                this.allCosmeticsDict[this.nullItem.itemName] = this.nullItem;
                this.allCosmeticsItemIDsfromDisplayNamesDict[this.nullItem.displayName] = this.nullItem.itemName;
                for (int i = 0; i < 11; i++)
                {
                    this.tryOnSet.items[i] = this.nullItem;
                }
                this.cosmeticsPages[0] = 0;
                this.cosmeticsPages[1] = 0;
                this.cosmeticsPages[2] = 0;
                this.cosmeticsPages[3] = 0;
                this.itemLists[0] = this.unlockedHats;
                this.itemLists[1] = this.unlockedFaces;
                this.itemLists[2] = this.unlockedBadges;
                this.itemLists[3] = this.unlockedHoldable;
                this.SwitchToStage(CosmeticsController.ATMStages.Unavailable);
                base.StartCoroutine(this.CheckCanGetDaily());
            }
        }



        // Token: 0x060020D4 RID: 8404 RVA: 0x0003029E File Offset: 0x0002E49E
        public void Start()
        {
            PlayFabTitleDataCache.Instance.GetTitleData("BundleData", delegate(string data)
            {
                this.bundleList.FromJson(data);
            }, delegate(PlayFabError e)
            {
                Debug.LogError(string.Format("Error getting bundle data: {0}", e));
            });
        }



        // Token: 0x060020D5 RID: 8405 RVA: 0x0001B2AB File Offset: 0x000194AB
        public void Update()
        {
        }



        // Token: 0x060020D6 RID: 8406 RVA: 0x000302DA File Offset: 0x0002E4DA
        private CosmeticsController.CosmeticSlots CategoryToNonTransferrableSlot(CosmeticsController.CosmeticCategory category)
        {
            switch (category)
            {
            case CosmeticsController.CosmeticCategory.Hat:
                return CosmeticsController.CosmeticSlots.Hat;
            case CosmeticsController.CosmeticCategory.Badge:
                return CosmeticsController.CosmeticSlots.Badge;
            case CosmeticsController.CosmeticCategory.Face:
                return CosmeticsController.CosmeticSlots.Face;
            case CosmeticsController.CosmeticCategory.Skin:
                return CosmeticsController.CosmeticSlots.Skin;
            }
            return CosmeticsController.CosmeticSlots.Count;
        }



        // Token: 0x060020D7 RID: 8407 RVA: 0x0003030D File Offset: 0x0002E50D
        private CosmeticsController.CosmeticSlots DropPositionToCosmeticSlot(BodyDockPositions.DropPositions pos)
        {
            switch (pos)
            {
            case BodyDockPositions.DropPositions.LeftArm:
                return CosmeticsController.CosmeticSlots.ArmLeft;
            case BodyDockPositions.DropPositions.RightArm:
                return CosmeticsController.CosmeticSlots.ArmRight;
            case BodyDockPositions.DropPositions.LeftArm | BodyDockPositions.DropPositions.RightArm:
                break;
            case BodyDockPositions.DropPositions.Chest:
                return CosmeticsController.CosmeticSlots.Chest;
            default:
                if (pos == BodyDockPositions.DropPositions.LeftBack)
                {
                    return CosmeticsController.CosmeticSlots.BackLeft;
                }
                if (pos == BodyDockPositions.DropPositions.RightBack)
                {
                    return CosmeticsController.CosmeticSlots.BackRight;
                }
                break;
            }
            return CosmeticsController.CosmeticSlots.Count;
        }



        // Token: 0x060020D8 RID: 8408 RVA: 0x0003033F File Offset: 0x0002E53F
        private static BodyDockPositions.DropPositions CosmeticSlotToDropPosition(CosmeticsController.CosmeticSlots slot)
        {
            switch (slot)
            {
            case CosmeticsController.CosmeticSlots.ArmLeft:
                return BodyDockPositions.DropPositions.LeftArm;
            case CosmeticsController.CosmeticSlots.ArmRight:
                return BodyDockPositions.DropPositions.RightArm;
            case CosmeticsController.CosmeticSlots.BackLeft:
                return BodyDockPositions.DropPositions.LeftBack;
            case CosmeticsController.CosmeticSlots.BackRight:
                return BodyDockPositions.DropPositions.RightBack;
            case CosmeticsController.CosmeticSlots.Chest:
                return BodyDockPositions.DropPositions.Chest;
            }
            return BodyDockPositions.DropPositions.None;
        }



        // Token: 0x060020D9 RID: 8409 RVA: 0x00030373 File Offset: 0x0002E573
        private void SaveItemPreference(CosmeticsController.CosmeticSlots slot, int slotIdx, CosmeticsController.CosmeticItem newItem)
        {
            PlayerPrefs.SetString(CosmeticsController.CosmeticSet.SlotPlayerPreferenceName(slot), newItem.itemName);
            PlayerPrefs.Save();
        }



        // Token: 0x060020DA RID: 8410 RVA: 0x000B9814 File Offset: 0x000B7A14
        public void SaveCurrentItemPreferences()
        {
            for (int i = 0; i < 11; i++)
            {
                CosmeticsController.CosmeticSlots slot = (CosmeticsController.CosmeticSlots)i;
                this.SaveItemPreference(slot, i, this.currentWornSet.items[i]);
            }
        }



        // Token: 0x060020DB RID: 8411 RVA: 0x000B984C File Offset: 0x000B7A4C
        private void ApplyCosmeticToSet(CosmeticsController.CosmeticSet set, CosmeticsController.CosmeticItem newItem, int slotIdx, CosmeticsController.CosmeticSlots slot, bool applyToPlayerPrefs, List<CosmeticsController.CosmeticSlots> appliedSlots)
        {
            CosmeticsController.CosmeticItem cosmeticItem = (set.items[slotIdx].itemName == newItem.itemName) ? this.nullItem : newItem;
            set.items[slotIdx] = cosmeticItem;
            if (applyToPlayerPrefs)
            {
                this.SaveItemPreference(slot, slotIdx, cosmeticItem);
            }
            appliedSlots.Add(slot);
        }



        // Token: 0x060020DC RID: 8412 RVA: 0x000B98A8 File Offset: 0x000B7AA8
        private void PrivApplyCosmeticItemToSet(CosmeticsController.CosmeticSet set, CosmeticsController.CosmeticItem newItem, bool isLeftHand, bool applyToPlayerPrefs, List<CosmeticsController.CosmeticSlots> appliedSlots)
        {
            if (newItem.isNullItem)
            {
                return;
            }
            if (CosmeticsController.CosmeticSet.IsHoldable(newItem))
            {
                BodyDockPositions.DockingResult dockingResult = GorillaTagger.Instance.offlineVRRig.GetComponent<BodyDockPositions>().ToggleWithHandedness(newItem.displayName, isLeftHand, newItem.bothHandsHoldable);
                foreach (BodyDockPositions.DropPositions pos in dockingResult.positionsDisabled)
                {
                    CosmeticsController.CosmeticSlots cosmeticSlots = this.DropPositionToCosmeticSlot(pos);
                    if (cosmeticSlots != CosmeticsController.CosmeticSlots.Count)
                    {
                        int num = (int)cosmeticSlots;
                        set.items[num] = this.nullItem;
                        if (applyToPlayerPrefs)
                        {
                            this.SaveItemPreference(cosmeticSlots, num, this.nullItem);
                        }
                    }
                }
                using (List<BodyDockPositions.DropPositions>.Enumerator enumerator = dockingResult.dockedPosition.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        BodyDockPositions.DropPositions dropPositions = enumerator.Current;
                        if (dropPositions != BodyDockPositions.DropPositions.None)
                        {
                            CosmeticsController.CosmeticSlots cosmeticSlots2 = this.DropPositionToCosmeticSlot(dropPositions);
                            int num2 = (int)cosmeticSlots2;
                            set.items[num2] = newItem;
                            if (applyToPlayerPrefs)
                            {
                                this.SaveItemPreference(cosmeticSlots2, num2, newItem);
                            }
                            appliedSlots.Add(cosmeticSlots2);
                        }
                    }
                    return;
                }
            }
            if (newItem.itemCategory == CosmeticsController.CosmeticCategory.Gloves)
            {
                CosmeticsController.CosmeticSlots cosmeticSlots3 = isLeftHand ? CosmeticsController.CosmeticSlots.HandLeft : CosmeticsController.CosmeticSlots.HandRight;
                int slotIdx = (int)cosmeticSlots3;
                this.ApplyCosmeticToSet(set, newItem, slotIdx, cosmeticSlots3, applyToPlayerPrefs, appliedSlots);
                CosmeticsController.CosmeticSlots cosmeticSlots4 = CosmeticsController.CosmeticSet.OppositeSlot(cosmeticSlots3);
                int num3 = (int)cosmeticSlots4;
                if (newItem.bothHandsHoldable)
                {
                    this.ApplyCosmeticToSet(set, this.nullItem, num3, cosmeticSlots4, applyToPlayerPrefs, appliedSlots);
                    return;
                }
                if (set.items[num3].itemName == newItem.itemName)
                {
                    this.ApplyCosmeticToSet(set, this.nullItem, num3, cosmeticSlots4, applyToPlayerPrefs, appliedSlots);
                }
                if (set.items[num3].bothHandsHoldable)
                {
                    this.ApplyCosmeticToSet(set, this.nullItem, num3, cosmeticSlots4, applyToPlayerPrefs, appliedSlots);
                    return;
                }
            }
            else
            {
                CosmeticsController.CosmeticSlots cosmeticSlots5 = this.CategoryToNonTransferrableSlot(newItem.itemCategory);
                int slotIdx2 = (int)cosmeticSlots5;
                this.ApplyCosmeticToSet(set, newItem, slotIdx2, cosmeticSlots5, applyToPlayerPrefs, appliedSlots);
            }
        }



        // Token: 0x060020DD RID: 8413 RVA: 0x000B9AAC File Offset: 0x000B7CAC
        public List<CosmeticsController.CosmeticSlots> ApplyCosmeticItemToSet(CosmeticsController.CosmeticSet set, CosmeticsController.CosmeticItem newItem, bool isLeftHand, bool applyToPlayerPrefs)
        {
            List<CosmeticsController.CosmeticSlots> list = new List<CosmeticsController.CosmeticSlots>(2);
            if (newItem.itemCategory == CosmeticsController.CosmeticCategory.Set)
            {
                foreach (string itemID in newItem.bundledItems)
                {
                    CosmeticsController.CosmeticItem itemFromDict = this.GetItemFromDict(itemID);
                    this.PrivApplyCosmeticItemToSet(set, itemFromDict, isLeftHand, applyToPlayerPrefs, list);
                }
            }
            else
            {
                this.PrivApplyCosmeticItemToSet(set, newItem, isLeftHand, applyToPlayerPrefs, list);
            }
            return list;
        }



        // Token: 0x060020DE RID: 8414 RVA: 0x000B9B08 File Offset: 0x000B7D08
        public void RemoveCosmeticItemFromSet(CosmeticsController.CosmeticSet set, string itemName, bool applyToPlayerPrefs)
        {
            this.cachedSet.CopyItems(set);
            for (int i = 0; i < 11; i++)
            {
                if (set.items[i].displayName == itemName)
                {
                    set.items[i] = this.nullItem;
                    if (applyToPlayerPrefs)
                    {
                        this.SaveItemPreference((CosmeticsController.CosmeticSlots)i, i, this.nullItem);
                    }
                }
            }
            VRRig offlineVRRig = GorillaTagger.Instance.offlineVRRig;
            BodyDockPositions component = offlineVRRig.GetComponent<BodyDockPositions>();
            set.ActivateCosmetics(this.cachedSet, offlineVRRig, component, CosmeticsController.instance.nullItem, offlineVRRig.cosmeticsObjectRegistry);
        }



        // Token: 0x060020DF RID: 8415 RVA: 0x0003038B File Offset: 0x0002E58B
        public void PressFittingRoomButton(FittingRoomButton pressedFittingRoomButton, bool isLeftHand)
        {
            this.ApplyCosmeticItemToSet(this.tryOnSet, pressedFittingRoomButton.currentCosmeticItem, isLeftHand, false);
            this.UpdateShoppingCart();
            this.UpdateWornCosmetics(true);
        }



        // Token: 0x060020E0 RID: 8416 RVA: 0x000B9B9C File Offset: 0x000B7D9C
        public void PressCosmeticStandButton(CosmeticStand pressedStand)
        {
            this.searchIndex = this.currentCart.IndexOf(pressedStand.thisCosmeticItem);
            if (this.searchIndex != -1)
            {
                this.currentCart.RemoveAt(this.searchIndex);
                pressedStand.isOn = false;
                for (int i = 0; i < 11; i++)
                {
                    if (pressedStand.thisCosmeticItem.itemName == this.tryOnSet.items[i].itemName)
                    {
                        this.tryOnSet.items[i] = this.nullItem;
                    }
                }
            }
            else
            {
                this.currentCart.Insert(0, pressedStand.thisCosmeticItem);
                pressedStand.isOn = true;
                if (this.currentCart.Count > this.fittingRoomButtons.Length)
                {
                    foreach (CosmeticStand cosmeticStand in this.cosmeticStands)
                    {
                        if (!(cosmeticStand == null) && cosmeticStand.thisCosmeticItem.itemName == this.currentCart[this.fittingRoomButtons.Length].itemName)
                        {
                            cosmeticStand.isOn = false;
                            cosmeticStand.UpdateColor();
                            break;
                        }
                    }
                    this.currentCart.RemoveAt(this.fittingRoomButtons.Length);
                }
            }
            pressedStand.UpdateColor();
            this.UpdateShoppingCart();
        }



        // Token: 0x060020E1 RID: 8417 RVA: 0x000B9CDC File Offset: 0x000B7EDC
        public void PressWardrobeItemButton(CosmeticsController.CosmeticItem cosmeticItem, bool isLeftHand)
        {
            if (cosmeticItem.isNullItem)
            {
                return;
            }
            CosmeticsController.CosmeticItem itemFromDict = this.GetItemFromDict(cosmeticItem.itemName);
            foreach (CosmeticsController.CosmeticSlots cosmeticSlots in this.ApplyCosmeticItemToSet(this.currentWornSet, itemFromDict, isLeftHand, true))
            {
                this.tryOnSet.items[(int)cosmeticSlots] = this.nullItem;
            }
            this.UpdateShoppingCart();
            this.UpdateWornCosmetics(true);
        }



        // Token: 0x060020E2 RID: 8418 RVA: 0x000B9D6C File Offset: 0x000B7F6C
        public void PressWardrobeFunctionButton(string function)
        {
            if (!(function == "left"))
            {
                if (!(function == "right"))
                {
                    if (!(function == "hat"))
                    {
                        if (!(function == "face"))
                        {
                            if (!(function == "badge"))
                            {
                                if (function == "hand")
                                {
                                    if (this.wardrobeType == 3)
                                    {
                                        return;
                                    }
                                    this.wardrobeType = 3;
                                }
                            }
                            else
                            {
                                if (this.wardrobeType == 2)
                                {
                                    return;
                                }
                                this.wardrobeType = 2;
                            }
                        }
                        else
                        {
                            if (this.wardrobeType == 1)
                            {
                                return;
                            }
                            this.wardrobeType = 1;
                        }
                    }
                    else
                    {
                        if (this.wardrobeType == 0)
                        {
                            return;
                        }
                        this.wardrobeType = 0;
                    }
                }
                else
                {
                    this.cosmeticsPages[this.wardrobeType] = this.cosmeticsPages[this.wardrobeType] + 1;
                    if (this.cosmeticsPages[this.wardrobeType] > (this.itemLists[this.wardrobeType].Count - 1) / 3)
                    {
                        this.cosmeticsPages[this.wardrobeType] = 0;
                    }
                }
            }
            else
            {
                this.cosmeticsPages[this.wardrobeType] = this.cosmeticsPages[this.wardrobeType] - 1;
                if (this.cosmeticsPages[this.wardrobeType] < 0)
                {
                    this.cosmeticsPages[this.wardrobeType] = (this.itemLists[this.wardrobeType].Count - 1) / 3;
                }
            }
            this.UpdateWardrobeModelsAndButtons();
        }



        // Token: 0x060020E3 RID: 8419 RVA: 0x000303AF File Offset: 0x0002E5AF
        public void ClearCheckout()
        {
            this.itemToBuy = this.allCosmetics[0];
            this.checkoutHeadModel.SetCosmeticActive(this.itemToBuy.displayName, false);
            this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Start;
            this.ProcessPurchaseItemState(null, false);
        }



        // Token: 0x060020E4 RID: 8420 RVA: 0x000B9ED4 File Offset: 0x000B80D4
        public bool RemoveItemFromCart(CosmeticsController.CosmeticItem cosmeticItem)
        {
            this.searchIndex = this.currentCart.IndexOf(cosmeticItem);
            if (this.searchIndex != -1)
            {
                this.currentCart.RemoveAt(this.searchIndex);
                for (int i = 0; i < 11; i++)
                {
                    if (cosmeticItem.itemName == this.tryOnSet.items[i].itemName)
                    {
                        this.tryOnSet.items[i] = this.nullItem;
                    }
                }
                return true;
            }
            return false;
        }



        // Token: 0x060020E5 RID: 8421 RVA: 0x000B9F58 File Offset: 0x000B8158
        public void PressCheckoutCartButton(CheckoutCartButton pressedCheckoutCartButton, bool isLeftHand)
        {
            if (this.currentPurchaseItemStage != CosmeticsController.PurchaseItemStages.Buying)
            {
                this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.CheckoutButtonPressed;
                this.tryOnSet.ClearSet(this.nullItem);
                if (this.itemToBuy.displayName == pressedCheckoutCartButton.currentCosmeticItem.displayName)
                {
                    this.itemToBuy = this.allCosmetics[0];
                    this.checkoutHeadModel.SetCosmeticActive(this.itemToBuy.displayName, false);
                }
                else
                {
                    this.itemToBuy = pressedCheckoutCartButton.currentCosmeticItem;
                    this.checkoutHeadModel.SetCosmeticActive(this.itemToBuy.displayName, false);
                    if (this.itemToBuy.bundledItems != null && this.itemToBuy.bundledItems.Length != 0)
                    {
                        List<string> list = new List<string>();
                        foreach (string itemID in this.itemToBuy.bundledItems)
                        {
                            this.tempItem = this.GetItemFromDict(itemID);
                            list.Add(this.tempItem.displayName);
                        }
                        this.checkoutHeadModel.SetCosmeticActiveArray(list.ToArray(), new bool[list.Count]);
                    }
                    this.ApplyCosmeticItemToSet(this.tryOnSet, pressedCheckoutCartButton.currentCosmeticItem, isLeftHand, false);
                    this.UpdateWornCosmetics(true);
                }
                this.ProcessPurchaseItemState(null, isLeftHand);
                this.UpdateShoppingCart();
            }
        }



        // Token: 0x060020E6 RID: 8422 RVA: 0x000303E9 File Offset: 0x0002E5E9
        public void PressPurchaseItemButton(PurchaseItemButton pressedPurchaseItemButton, bool isLeftHand)
        {
            this.ProcessPurchaseItemState(pressedPurchaseItemButton.buttonSide, isLeftHand);
        }



        // Token: 0x060020E7 RID: 8423 RVA: 0x000BA09C File Offset: 0x000B829C
        public void PressEarlyAccessButton()
        {
            this.SwitchToStage(CosmeticsController.ATMStages.Begin);
            this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Start;
            this.ProcessPurchaseItemState("left", false);
            this.itemToPurchase = this.BundlePlayfabItemName;
            this.shinyRocksCost = (float)this.BundleShinyRocks;
            this.SteamPurchase();
            this.SwitchToStage(CosmeticsController.ATMStages.Purchasing);
        }



        // Token: 0x060020E8 RID: 8424 RVA: 0x000BA0EC File Offset: 0x000B82EC
        public void ProcessPurchaseItemState(string buttonSide, bool isLeftHand)
        {
            switch (this.currentPurchaseItemStage)
            {
            case CosmeticsController.PurchaseItemStages.Start:
                this.itemToBuy = this.nullItem;
                this.FormattedPurchaseText("SELECT AN ITEM FROM YOUR CART TO PURCHASE!");
                this.UpdateShoppingCart();
                return;
            case CosmeticsController.PurchaseItemStages.CheckoutButtonPressed:
                this.searchIndex = this.unlockedCosmetics.FindIndex((CosmeticsController.CosmeticItem x) => this.itemToBuy.itemName == x.itemName);
                if (this.searchIndex > -1)
                {
                    this.FormattedPurchaseText("YOU ALREADY OWN THIS ITEM!");
                    this.leftPurchaseButton.myText.text = "-";
                    this.rightPurchaseButton.myText.text = "-";
                    this.leftPurchaseButton.buttonRenderer.material = this.leftPurchaseButton.pressedMaterial;
                    this.rightPurchaseButton.buttonRenderer.material = this.rightPurchaseButton.pressedMaterial;
                    this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.ItemOwned;
                    return;
                }
                if (this.itemToBuy.cost <= this.currencyBalance)
                {
                    this.FormattedPurchaseText("DO YOU WANT TO BUY THIS ITEM?");
                    this.leftPurchaseButton.myText.text = "NO!";
                    this.rightPurchaseButton.myText.text = "YES!";
                    this.leftPurchaseButton.buttonRenderer.material = this.leftPurchaseButton.unpressedMaterial;
                    this.rightPurchaseButton.buttonRenderer.material = this.rightPurchaseButton.unpressedMaterial;
                    this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.ItemSelected;
                    return;
                }
                this.FormattedPurchaseText("INSUFFICIENT SHINY ROCKS FOR THIS ITEM!");
                this.leftPurchaseButton.myText.text = "-";
                this.rightPurchaseButton.myText.text = "-";
                this.leftPurchaseButton.buttonRenderer.material = this.leftPurchaseButton.pressedMaterial;
                this.rightPurchaseButton.buttonRenderer.material = this.rightPurchaseButton.pressedMaterial;
                this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Start;
                return;
            case CosmeticsController.PurchaseItemStages.ItemSelected:
                if (buttonSide == "right")
                {
                    this.FormattedPurchaseText("ARE YOU REALLY SURE?");
                    this.leftPurchaseButton.myText.text = "YES! I NEED IT!";
                    this.rightPurchaseButton.myText.text = "LET ME THINK ABOUT IT";
                    this.leftPurchaseButton.buttonRenderer.material = this.leftPurchaseButton.unpressedMaterial;
                    this.rightPurchaseButton.buttonRenderer.material = this.rightPurchaseButton.unpressedMaterial;
                    this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.FinalPurchaseAcknowledgement;
                    return;
                }
                this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.CheckoutButtonPressed;
                this.ProcessPurchaseItemState(null, isLeftHand);
                return;
            case CosmeticsController.PurchaseItemStages.ItemOwned:
            case CosmeticsController.PurchaseItemStages.Buying:
                break;
            case CosmeticsController.PurchaseItemStages.FinalPurchaseAcknowledgement:
                if (buttonSide == "left")
                {
                    this.FormattedPurchaseText("PURCHASING ITEM...");
                    this.leftPurchaseButton.myText.text = "-";
                    this.rightPurchaseButton.myText.text = "-";
                    this.leftPurchaseButton.buttonRenderer.material = this.leftPurchaseButton.pressedMaterial;
                    this.rightPurchaseButton.buttonRenderer.material = this.rightPurchaseButton.pressedMaterial;
                    this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Buying;
                    this.isLastHandTouchedLeft = isLeftHand;
                    this.PurchaseItem();
                    return;
                }
                this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.CheckoutButtonPressed;
                this.ProcessPurchaseItemState(null, isLeftHand);
                return;
            case CosmeticsController.PurchaseItemStages.Success:
            {
                this.FormattedPurchaseText("SUCCESS! ENJOY YOUR NEW ITEM!");
                VRRig offlineVRRig = GorillaTagger.Instance.offlineVRRig;
                offlineVRRig.concatStringOfCosmeticsAllowed += this.itemToBuy.itemName;
                CosmeticsController.CosmeticItem itemFromDict = this.GetItemFromDict(this.itemToBuy.itemName);
                if (itemFromDict.bundledItems != null)
                {
                    foreach (string str in itemFromDict.bundledItems)
                    {
                        VRRig offlineVRRig2 = GorillaTagger.Instance.offlineVRRig;
                        offlineVRRig2.concatStringOfCosmeticsAllowed += str;
                    }
                }
                this.tryOnSet.ClearSet(this.nullItem);
                this.UpdateShoppingCart();
                this.ApplyCosmeticItemToSet(this.currentWornSet, itemFromDict, isLeftHand, true);
                this.UpdateShoppingCart();
                this.UpdateWornCosmetics(false);
                this.leftPurchaseButton.myText.text = "-";
                this.rightPurchaseButton.myText.text = "-";
                this.leftPurchaseButton.buttonRenderer.material = this.leftPurchaseButton.pressedMaterial;
                this.rightPurchaseButton.buttonRenderer.material = this.rightPurchaseButton.pressedMaterial;
                break;
            }
            case CosmeticsController.PurchaseItemStages.Failure:
                this.FormattedPurchaseText("ERROR IN PURCHASING ITEM! NO MONEY WAS SPENT. SELECT ANOTHER ITEM.");
                this.leftPurchaseButton.myText.text = "-";
                this.rightPurchaseButton.myText.text = "-";
                this.leftPurchaseButton.buttonRenderer.material = this.leftPurchaseButton.pressedMaterial;
                this.rightPurchaseButton.buttonRenderer.material = this.rightPurchaseButton.pressedMaterial;
                return;
            default:
                return;
            }
        }



        // Token: 0x060020E9 RID: 8425 RVA: 0x000BA588 File Offset: 0x000B8788
        public void FormattedPurchaseText(string finalLineVar)
        {
            this.finalLine = finalLineVar;
            this.purchaseText.text = string.Concat(new string[]
            {
                "SELECTION: ",
                this.GetItemDisplayName(this.itemToBuy),
                "\nITEM COST: ",
                this.itemToBuy.cost.ToString(),
                "\nYOU HAVE: ",
                this.currencyBalance.ToString(),
                "\n\n",
                this.finalLine
            });
        }



        // Token: 0x060020EA RID: 8426 RVA: 0x000BA60C File Offset: 0x000B880C
        public void PurchaseItem()
        {
            PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest
            {
                ItemId = this.itemToBuy.itemName,
                Price = this.itemToBuy.cost,
                VirtualCurrency = this.currencyName,
                CatalogVersion = this.catalog
            }, delegate(PurchaseItemResult result)
            {
                if (result.Items.Count > 0)
                {
                    foreach (ItemInstance itemInstance in result.Items)
                    {
                        CosmeticsController.CosmeticItem itemFromDict = this.GetItemFromDict(this.itemToBuy.itemName);
                        if (itemFromDict.itemCategory == CosmeticsController.CosmeticCategory.Set)
                        {
                            this.UnlockItem(itemInstance.ItemId);
                            foreach (string itemIdToUnlock in itemFromDict.bundledItems)
                            {
                                this.UnlockItem(itemIdToUnlock);
                            }
                        }
                        else
                        {
                            this.UnlockItem(itemInstance.ItemId);
                        }
                    }
                    if (PhotonNetwork.InRoom)
                    {
                        RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
                        WebFlags flags = new WebFlags(1);
                        raiseEventOptions.Flags = flags;
                        object[] eventContent = new object[0];
                        PhotonNetwork.RaiseEvent(9, eventContent, raiseEventOptions, SendOptions.SendReliable);
                        base.StartCoroutine(this.CheckIfMyCosmeticsUpdated(this.itemToBuy.itemName));
                    }
                    this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Success;
                    this.currencyBalance -= this.itemToBuy.cost;
                    this.UpdateShoppingCart();
                    this.ProcessPurchaseItemState(null, this.isLastHandTouchedLeft);
                    return;
                }
                this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Failure;
                this.ProcessPurchaseItemState(null, false);
            }, delegate(PlayFabError error)
            {
                this.currentPurchaseItemStage = CosmeticsController.PurchaseItemStages.Failure;
                this.ProcessPurchaseItemState(null, false);
            }, null, null);
        }



        // Token: 0x060020EB RID: 8427 RVA: 0x000BA678 File Offset: 0x000B8878
        private void UnlockItem(string itemIdToUnlock)
        {
            int num = this.allCosmetics.FindIndex((CosmeticsController.CosmeticItem x) => itemIdToUnlock == x.itemName);
            if (num > -1)
            {
                if (!this.unlockedCosmetics.Contains(this.allCosmetics[num]))
                {
                    this.unlockedCosmetics.Add(this.allCosmetics[num]);
                }
                this.concatStringCosmeticsAllowed += this.allCosmetics[num].itemName;
                switch (this.allCosmetics[num].itemCategory)
                {
                case CosmeticsController.CosmeticCategory.Hat:
                    if (!this.unlockedHats.Contains(this.allCosmetics[num]))
                    {
                        this.unlockedHats.Add(this.allCosmetics[num]);
                        return;
                    }
                    break;
                case CosmeticsController.CosmeticCategory.Badge:
                case CosmeticsController.CosmeticCategory.Skin:
                    if (!this.unlockedBadges.Contains(this.allCosmetics[num]))
                    {
                        this.unlockedBadges.Add(this.allCosmetics[num]);
                        return;
                    }
                    break;
                case CosmeticsController.CosmeticCategory.Face:
                    if (!this.unlockedFaces.Contains(this.allCosmetics[num]))
                    {
                        this.unlockedFaces.Add(this.allCosmetics[num]);
                        return;
                    }
                    break;
                case CosmeticsController.CosmeticCategory.Holdable:
                case CosmeticsController.CosmeticCategory.Gloves:
                case CosmeticsController.CosmeticCategory.Slingshot:
                    if (!this.unlockedHoldable.Contains(this.allCosmetics[num]))
                    {
                        this.unlockedHoldable.Add(this.allCosmetics[num]);
                    }
                    break;
                case CosmeticsController.CosmeticCategory.Count:
                case CosmeticsController.CosmeticCategory.Set:
                    break;
                default:
                    return;
                }
            }
        }



        // Token: 0x060020EC RID: 8428 RVA: 0x000303F8 File Offset: 0x0002E5F8
        private IEnumerator CheckIfMyCosmeticsUpdated(string itemToBuyID)
        {
            yield return new WaitForSeconds(1f);
            this.foundCosmetic = false;
            this.attempts = 0;
            while (!this.foundCosmetic && this.attempts < 10 && PhotonNetwork.InRoom)
            {
                this.playerIDList.Clear();
                this.playerIDList.Add(PhotonNetwork.LocalPlayer.ActorNumber.ToString());
                PlayFabClientAPI.GetSharedGroupData(new GetSharedGroupDataRequest
                {
                    Keys = this.playerIDList,
                    SharedGroupId = PhotonNetwork.CurrentRoom.Name + Regex.Replace(PhotonNetwork.CloudRegion, "[^a-zA-Z0-9]", "").ToUpper()
                }, delegate(GetSharedGroupDataResult result)
                {
                    this.attempts++;
                    foreach (KeyValuePair<string, SharedGroupDataRecord> keyValuePair in result.Data)
                    {
                        if (keyValuePair.Value.Value.Contains(itemToBuyID))
                        {
                            PhotonNetwork.RaiseEvent(199, null, new RaiseEventOptions
                            {
                                Receivers = ReceiverGroup.Others
                            }, SendOptions.SendReliable);
                            this.foundCosmetic = true;
                        }
                    }
                    if (this.foundCosmetic)
                    {
                        this.UpdateWornCosmetics(true);
                    }
                }, delegate(PlayFabError error)
                {
                    this.attempts++;
                    if (error.Error == PlayFabErrorCode.NotAuthenticated)
                    {
                        PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
                        return;
                    }
                    if (error.Error == PlayFabErrorCode.AccountBanned)
                    {
                        Application.Quit();
                        PhotonNetwork.Disconnect();
                        UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
                        UnityEngine.Object.DestroyImmediate(GorillaLocomotion.Player.Instance);
                        GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
                        for (int i = 0; i < array.Length; i++)
                        {
                            UnityEngine.Object.Destroy(array[i]);
                        }
                    }
                }, null, null);
                yield return new WaitForSeconds(1f);
            }
            yield break;
        }



        // Token: 0x060020ED RID: 8429 RVA: 0x000BA808 File Offset: 0x000B8A08
        public void UpdateWardrobeModelsAndButtons()
        {
            foreach (WardrobeInstance wardrobeInstance in this.wardrobes)
            {
                wardrobeInstance.wardrobeItemButtons[0].currentCosmeticItem = ((this.cosmeticsPages[this.wardrobeType] * 3 < this.itemLists[this.wardrobeType].Count) ? this.itemLists[this.wardrobeType][this.cosmeticsPages[this.wardrobeType] * 3] : this.nullItem);
                wardrobeInstance.wardrobeItemButtons[1].currentCosmeticItem = ((this.cosmeticsPages[this.wardrobeType] * 3 + 1 < this.itemLists[this.wardrobeType].Count) ? this.itemLists[this.wardrobeType][this.cosmeticsPages[this.wardrobeType] * 3 + 1] : this.nullItem);
                wardrobeInstance.wardrobeItemButtons[2].currentCosmeticItem = ((this.cosmeticsPages[this.wardrobeType] * 3 + 2 < this.itemLists[this.wardrobeType].Count) ? this.itemLists[this.wardrobeType][this.cosmeticsPages[this.wardrobeType] * 3 + 2] : this.nullItem);
                this.iterator = 0;
                while (this.iterator < wardrobeInstance.wardrobeItemButtons.Length)
                {
                    CosmeticsController.CosmeticItem currentCosmeticItem = wardrobeInstance.wardrobeItemButtons[this.iterator].currentCosmeticItem;
                    wardrobeInstance.wardrobeItemButtons[this.iterator].isOn = (!currentCosmeticItem.isNullItem && this.AnyMatch(this.currentWornSet, currentCosmeticItem));
                    wardrobeInstance.wardrobeItemButtons[this.iterator].UpdateColor();
                    this.iterator++;
                }
                wardrobeInstance.wardrobeItemButtons[0].controlledModel.SetCosmeticActive(wardrobeInstance.wardrobeItemButtons[0].currentCosmeticItem.displayName, false);
                wardrobeInstance.wardrobeItemButtons[1].controlledModel.SetCosmeticActive(wardrobeInstance.wardrobeItemButtons[1].currentCosmeticItem.displayName, false);
                wardrobeInstance.wardrobeItemButtons[2].controlledModel.SetCosmeticActive(wardrobeInstance.wardrobeItemButtons[2].currentCosmeticItem.displayName, false);
                wardrobeInstance.selfDoll.SetCosmeticActiveArray(this.currentWornSet.ToDisplayNameArray(), this.currentWornSet.ToOnRightSideArray());
            }
        }



        // Token: 0x060020EE RID: 8430 RVA: 0x000BAA80 File Offset: 0x000B8C80
        public void UpdateShoppingCart()
        {
            this.iterator = 0;
            while (this.iterator < this.fittingRoomButtons.Length)
            {
                if (this.iterator < this.currentCart.Count)
                {
                    this.fittingRoomButtons[this.iterator].currentCosmeticItem = this.currentCart[this.iterator];
                    this.checkoutCartButtons[this.iterator].currentCosmeticItem = this.currentCart[this.iterator];
                    this.checkoutCartButtons[this.iterator].isOn = (this.checkoutCartButtons[this.iterator].currentCosmeticItem.itemName == this.itemToBuy.itemName);
                    this.fittingRoomButtons[this.iterator].isOn = this.AnyMatch(this.tryOnSet, this.fittingRoomButtons[this.iterator].currentCosmeticItem);
                }
                else
                {
                    this.checkoutCartButtons[this.iterator].currentCosmeticItem = this.nullItem;
                    this.fittingRoomButtons[this.iterator].currentCosmeticItem = this.nullItem;
                    this.checkoutCartButtons[this.iterator].isOn = false;
                    this.fittingRoomButtons[this.iterator].isOn = false;
                }
                this.checkoutCartButtons[this.iterator].currentImage.sprite = this.checkoutCartButtons[this.iterator].currentCosmeticItem.itemPicture;
                this.fittingRoomButtons[this.iterator].currentImage.sprite = this.fittingRoomButtons[this.iterator].currentCosmeticItem.itemPicture;
                this.checkoutCartButtons[this.iterator].UpdateColor();
                this.fittingRoomButtons[this.iterator].UpdateColor();
                this.iterator++;
            }
            this.UpdateWardrobeModelsAndButtons();
        }



        // Token: 0x060020EF RID: 8431 RVA: 0x000BAC60 File Offset: 0x000B8E60
        public void UpdateWornCosmetics(bool sync = false)
        {
            GorillaTagger.Instance.offlineVRRig.LocalUpdateCosmeticsWithTryon(this.currentWornSet, this.tryOnSet);
            if (sync && GorillaTagger.Instance.myVRRig != null)
            {
                string[] array = this.currentWornSet.ToDisplayNameArray();
                string[] array2 = this.tryOnSet.ToDisplayNameArray();
                GorillaTagger.Instance.myVRRig.RPC("UpdateCosmeticsWithTryon", RpcTarget.All, new object[]
                {
                    array,
                    array2
                });
            }
        }



        // Token: 0x060020F0 RID: 8432 RVA: 0x0003040E File Offset: 0x0002E60E
        public CosmeticsController.CosmeticItem GetItemFromDict(string itemID)
        {
            if (!this.allCosmeticsDict.TryGetValue(itemID, out this.cosmeticItemVar))
            {
                return this.nullItem;
            }
            return this.cosmeticItemVar;
        }



        // Token: 0x060020F1 RID: 8433 RVA: 0x00030431 File Offset: 0x0002E631
        public string GetItemNameFromDisplayName(string displayName)
        {
            if (!this.allCosmeticsItemIDsfromDisplayNamesDict.TryGetValue(displayName, out this.returnString))
            {
                return "null";
            }
            return this.returnString;
        }



        // Token: 0x060020F2 RID: 8434 RVA: 0x000BACD8 File Offset: 0x000B8ED8
        public bool AnyMatch(CosmeticsController.CosmeticSet set, CosmeticsController.CosmeticItem item)
        {
            if (item.itemCategory != CosmeticsController.CosmeticCategory.Set)
            {
                return set.IsActive(item.displayName);
            }
            if (item.bundledItems.Length == 1)
            {
                return this.AnyMatch(set, this.GetItemFromDict(item.bundledItems[0]));
            }
            if (item.bundledItems.Length == 2)
            {
                return this.AnyMatch(set, this.GetItemFromDict(item.bundledItems[0])) || this.AnyMatch(set, this.GetItemFromDict(item.bundledItems[1]));
            }
            return item.bundledItems.Length >= 3 && (this.AnyMatch(set, this.GetItemFromDict(item.bundledItems[0])) || this.AnyMatch(set, this.GetItemFromDict(item.bundledItems[1])) || this.AnyMatch(set, this.GetItemFromDict(item.bundledItems[2])));
        }



        // Token: 0x060020F3 RID: 8435 RVA: 0x00030453 File Offset: 0x0002E653
        public void Initialize()
        {
            if (base.gameObject.activeSelf)
            {
                this.GetUserCosmeticsAllowed();
            }
        }



        // Token: 0x060020F4 RID: 8436 RVA: 0x00030468 File Offset: 0x0002E668
        public void GetLastDailyLogin()
        {
            PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest(), delegate(GetUserDataResult result)
            {
                if (result.Data.TryGetValue("DailyLogin", out this.userDataRecord))
                {
                    this.lastDailyLogin = this.userDataRecord.Value;
                    return;
                }
                this.lastDailyLogin = "NONE";
                base.StartCoroutine(this.GetMyDaily());
            }, delegate(PlayFabError error)
            {
                this.lastDailyLogin = "FAILED";
                if (error.Error == PlayFabErrorCode.NotAuthenticated)
                {
                    PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
                    return;
                }
                if (error.Error == PlayFabErrorCode.AccountBanned)
                {
                    Application.Quit();
                    PhotonNetwork.Disconnect();
                    UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
                    UnityEngine.Object.DestroyImmediate(GorillaLocomotion.Player.Instance);
                    GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    for (int i = 0; i < array.Length; i++)
                    {
                        UnityEngine.Object.Destroy(array[i]);
                    }
                }
            }, null, null);
        }



        // Token: 0x060020F5 RID: 8437 RVA: 0x0003048E File Offset: 0x0002E68E
        private IEnumerator CheckCanGetDaily()
        {
            for (;;)
            {
                if (GorillaComputer.instance != null && GorillaComputer.instance.startupMillis != 0L)
                {
                    this.currentTime = new DateTime((GorillaComputer.instance.startupMillis + (long)(Time.realtimeSinceStartup * 1000f)) * 10000L);
                    this.secondsUntilTomorrow = (int)(this.currentTime.AddDays(1.0).Date - this.currentTime).TotalSeconds;
                    if (this.lastDailyLogin == null || this.lastDailyLogin == "")
                    {
                        this.GetLastDailyLogin();
                    }
                    else if (this.currentTime.ToString("o").Substring(0, 10) == this.lastDailyLogin)
                    {
                        this.checkedDaily = true;
                        this.gotMyDaily = true;
                    }
                    else if (this.currentTime.ToString("o").Substring(0, 10) != this.lastDailyLogin)
                    {
                        this.checkedDaily = true;
                        this.gotMyDaily = false;
                        base.StartCoroutine(this.GetMyDaily());
                    }
                    else if (this.lastDailyLogin == "FAILED")
                    {
                        this.GetLastDailyLogin();
                    }
                    this.secondsToWaitToCheckDaily = (this.checkedDaily ? 60f : 10f);
                    this.UpdateCurrencyBoard();
                    yield return new WaitForSeconds(this.secondsToWaitToCheckDaily);
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
            yield break;
        }



        // Token: 0x060020F6 RID: 8438 RVA: 0x0003049D File Offset: 0x0002E69D
        private IEnumerator GetMyDaily()
        {
            yield return new WaitForSeconds(10f);
            ExecuteCloudScriptRequest executeCloudScriptRequest = new ExecuteCloudScriptRequest();
            executeCloudScriptRequest.FunctionName = "TryDistributeCurrency";
            executeCloudScriptRequest.FunctionParameter = new
            {



            };
            PlayFabClientAPI.ExecuteCloudScript(executeCloudScriptRequest, delegate(ExecuteCloudScriptResult result)
            {
                this.GetCurrencyBalance();
                this.GetLastDailyLogin();
            }, delegate(PlayFabError error)
            {
                if (error.Error == PlayFabErrorCode.NotAuthenticated)
                {
                    PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
                    return;
                }
                if (error.Error == PlayFabErrorCode.AccountBanned)
                {
                    Application.Quit();
                    PhotonNetwork.Disconnect();
                    UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
                    UnityEngine.Object.DestroyImmediate(GorillaLocomotion.Player.Instance);
                    GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    for (int i = 0; i < array.Length; i++)
                    {
                        UnityEngine.Object.Destroy(array[i]);
                    }
                }
            }, null, null);
            yield break;
        }



        // Token: 0x060020F7 RID: 8439 RVA: 0x000304AC File Offset: 0x0002E6AC
        public void GetUserCosmeticsAllowed()
        {
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), delegate(GetUserInventoryResult result)
            {
                PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest
                {
                    CatalogVersion = this.catalog
                }, delegate(GetCatalogItemsResult result2)
                {
                    this.unlockedCosmetics.Clear();
                    this.unlockedHats.Clear();
                    this.unlockedBadges.Clear();
                    this.unlockedFaces.Clear();
                    this.unlockedHoldable.Clear();
                    this.catalogItems = result2.Catalog;
                    using (List<CatalogItem>.Enumerator enumerator = this.catalogItems.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            CatalogItem catalogItem = enumerator.Current;
                            this.searchIndex = this.allCosmetics.FindIndex((CosmeticsController.CosmeticItem x) => catalogItem.DisplayName == x.displayName);
                            if (this.searchIndex > -1)
                            {
                                this.tempStringArray = null;
                                this.hasPrice = false;
                                if (catalogItem.Bundle != null)
                                {
                                    this.tempStringArray = catalogItem.Bundle.BundledItems.ToArray();
                                }
                                uint cost;
                                if (catalogItem.VirtualCurrencyPrices.TryGetValue(this.currencyName, out cost))
                                {
                                    this.hasPrice = true;
                                }
                                this.allCosmetics[this.searchIndex] = new CosmeticsController.CosmeticItem
                                {
                                    itemName = catalogItem.ItemId,
                                    displayName = catalogItem.DisplayName,
                                    cost = (int)cost,
                                    itemPicture = this.allCosmetics[this.searchIndex].itemPicture,
                                    itemPictureResourceString = this.allCosmetics[this.searchIndex].itemPictureResourceString,
                                    itemCategory = this.allCosmetics[this.searchIndex].itemCategory,
                                    bundledItems = this.tempStringArray,
                                    canTryOn = this.hasPrice,
                                    bothHandsHoldable = this.allCosmetics[this.searchIndex].bothHandsHoldable,
                                    overrideDisplayName = this.allCosmetics[this.searchIndex].overrideDisplayName,
                                    bLoadsFromResources = this.allCosmetics[this.searchIndex].bLoadsFromResources,
                                    bUsesMeshAtlas = this.allCosmetics[this.searchIndex].bUsesMeshAtlas,
                                    rotationOffset = this.allCosmetics[this.searchIndex].rotationOffset,
                                    positionOffset = this.allCosmetics[this.searchIndex].positionOffset,
                                    meshAtlasResourceString = this.allCosmetics[this.searchIndex].meshAtlasResourceString,
                                    meshResourceString = this.allCosmetics[this.searchIndex].meshResourceString,
                                    materialResourceString = this.allCosmetics[this.searchIndex].materialResourceString
                                };
                                this.allCosmeticsDict[this.allCosmetics[this.searchIndex].itemName] = this.allCosmetics[this.searchIndex];
                                this.allCosmeticsItemIDsfromDisplayNamesDict[this.allCosmetics[this.searchIndex].displayName] = this.allCosmetics[this.searchIndex].itemName;
                            }
                        }
                    }
                    for (int i = this.allCosmetics.Count - 1; i > -1; i--)
                    {
                        this.tempItem = this.allCosmetics[i];
                        if (this.tempItem.itemCategory == CosmeticsController.CosmeticCategory.Set && this.tempItem.canTryOn)
                        {
                            string[] bundledItems = this.tempItem.bundledItems;
                            for (int j = 0; j < bundledItems.Length; j++)
                            {
                                string setItemName = bundledItems[j];
                                this.searchIndex = this.allCosmetics.FindIndex((CosmeticsController.CosmeticItem x) => setItemName == x.itemName);
                                if (this.searchIndex > -1)
                                {
                                    this.tempItem = new CosmeticsController.CosmeticItem
                                    {
                                        itemName = this.allCosmetics[this.searchIndex].itemName,
                                        displayName = this.allCosmetics[this.searchIndex].displayName,
                                        cost = this.allCosmetics[this.searchIndex].cost,
                                        itemPicture = this.allCosmetics[this.searchIndex].itemPicture,
                                        itemCategory = this.allCosmetics[this.searchIndex].itemCategory,
                                        overrideDisplayName = this.allCosmetics[this.searchIndex].overrideDisplayName,
                                        bothHandsHoldable = this.allCosmetics[this.searchIndex].bothHandsHoldable,
                                        canTryOn = true
                                    };
                                    this.allCosmetics[this.searchIndex] = this.tempItem;
                                    this.allCosmeticsDict[this.allCosmetics[this.searchIndex].itemName] = this.allCosmetics[this.searchIndex];
                                    this.allCosmeticsItemIDsfromDisplayNamesDict[this.allCosmetics[this.searchIndex].displayName] = this.allCosmetics[this.searchIndex].itemName;
                                }
                            }
                        }
                    }
                    using (List<ItemInstance>.Enumerator enumerator2 = result.Inventory.GetEnumerator())
                    {
                        while (enumerator2.MoveNext())
                        {
                            ItemInstance item = enumerator2.Current;
                            if (item.ItemId == "Early Access Supporter Pack")
                            {
                                this.unlockedCosmetics.Add(this.allCosmetics[1]);
                                this.unlockedCosmetics.Add(this.allCosmetics[10]);
                                this.unlockedCosmetics.Add(this.allCosmetics[11]);
                                this.unlockedCosmetics.Add(this.allCosmetics[12]);
                                this.unlockedCosmetics.Add(this.allCosmetics[13]);
                                this.unlockedCosmetics.Add(this.allCosmetics[14]);
                                this.unlockedCosmetics.Add(this.allCosmetics[15]);
                                this.unlockedCosmetics.Add(this.allCosmetics[31]);
                                this.unlockedCosmetics.Add(this.allCosmetics[32]);
                                this.unlockedCosmetics.Add(this.allCosmetics[38]);
                                this.unlockedCosmetics.Add(this.allCosmetics[67]);
                                this.unlockedCosmetics.Add(this.allCosmetics[68]);
                            }
                            else
                            {
                                if (item.ItemId == this.BundlePlayfabItemName)
                                {
                                    foreach (EarlyAccessButton earlyAccessButton in this.earlyAccessButtons)
                                    {
                                        this.AlreadyOwnAllBundleButtons();
                                    }
                                }
                                this.searchIndex = this.allCosmetics.FindIndex((CosmeticsController.CosmeticItem x) => item.ItemId == x.itemName);
                                if (this.searchIndex > -1)
                                {
                                    this.unlockedCosmetics.Add(this.allCosmetics[this.searchIndex]);
                                }
                            }
                        }
                    }
                    this.searchIndex = this.allCosmetics.FindIndex((CosmeticsController.CosmeticItem x) => "Slingshot" == x.itemName);
                    this.allCosmeticsDict["Slingshot"] = this.allCosmetics[this.searchIndex];
                    this.allCosmeticsItemIDsfromDisplayNamesDict[this.allCosmetics[this.searchIndex].displayName] = this.allCosmetics[this.searchIndex].itemName;
                    foreach (CosmeticsController.CosmeticItem cosmeticItem in this.unlockedCosmetics)
                    {
                        if (cosmeticItem.itemCategory == CosmeticsController.CosmeticCategory.Hat && !this.unlockedHats.Contains(cosmeticItem))
                        {
                            this.unlockedHats.Add(cosmeticItem);
                        }
                        else if (cosmeticItem.itemCategory == CosmeticsController.CosmeticCategory.Face && !this.unlockedFaces.Contains(cosmeticItem))
                        {
                            this.unlockedFaces.Add(cosmeticItem);
                        }
                        else if (cosmeticItem.itemCategory == CosmeticsController.CosmeticCategory.Badge && !this.unlockedBadges.Contains(cosmeticItem))
                        {
                            this.unlockedBadges.Add(cosmeticItem);
                        }
                        else if (cosmeticItem.itemCategory == CosmeticsController.CosmeticCategory.Skin && !this.unlockedBadges.Contains(cosmeticItem))
                        {
                            this.unlockedBadges.Add(cosmeticItem);
                        }
                        else if (cosmeticItem.itemCategory == CosmeticsController.CosmeticCategory.Holdable && !this.unlockedHoldable.Contains(cosmeticItem))
                        {
                            this.unlockedHoldable.Add(cosmeticItem);
                        }
                        else if (cosmeticItem.itemCategory == CosmeticsController.CosmeticCategory.Gloves && !this.unlockedHoldable.Contains(cosmeticItem))
                        {
                            this.unlockedHoldable.Add(cosmeticItem);
                        }
                        else if (cosmeticItem.itemCategory == CosmeticsController.CosmeticCategory.Slingshot && !this.unlockedHoldable.Contains(cosmeticItem))
                        {
                            this.unlockedHoldable.Add(cosmeticItem);
                        }
                        this.concatStringCosmeticsAllowed += cosmeticItem.itemName;
                    }
                    foreach (CosmeticStand cosmeticStand in this.cosmeticStands)
                    {
                        if (cosmeticStand != null)
                        {
                            cosmeticStand.InitializeCosmetic();
                        }
                    }
                    this.currencyBalance = result.VirtualCurrency[this.currencyName];
                    int num;
                    this.playedInBeta = (result.VirtualCurrency.TryGetValue("TC", out num) && num > 0);
                    this.currentWornSet.LoadFromPlayerPreferences(this);
                    this.SwitchToStage(CosmeticsController.ATMStages.Begin);
                    this.ProcessPurchaseItemState(null, false);
                    this.UpdateShoppingCart();
                    this.UpdateWornCosmetics(false);
                    this.UpdateCurrencyBoard();
                }, delegate(PlayFabError error)
                {
                    if (error.Error == PlayFabErrorCode.NotAuthenticated)
                    {
                        PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
                    }
                    else if (error.Error == PlayFabErrorCode.AccountBanned)
                    {
                        Application.Quit();
                        PhotonNetwork.Disconnect();
                        UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
                        UnityEngine.Object.DestroyImmediate(GorillaLocomotion.Player.Instance);
                        GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
                        for (int i = 0; i < array.Length; i++)
                        {
                            UnityEngine.Object.Destroy(array[i]);
                        }
                    }
                    if (!this.tryTwice)
                    {
                        this.tryTwice = true;
                        this.GetUserCosmeticsAllowed();
                    }
                }, null, null);
            }, delegate(PlayFabError error)
            {
                if (error.Error == PlayFabErrorCode.NotAuthenticated)
                {
                    PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
                }
                else if (error.Error == PlayFabErrorCode.AccountBanned)
                {
                    Application.Quit();
                    PhotonNetwork.Disconnect();
                    UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
                    UnityEngine.Object.DestroyImmediate(GorillaLocomotion.Player.Instance);
                    GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    for (int i = 0; i < array.Length; i++)
                    {
                        UnityEngine.Object.Destroy(array[i]);
                    }
                }
                if (!this.tryTwice)
                {
                    this.tryTwice = true;
                    this.GetUserCosmeticsAllowed();
                }
            }, null, null);
        }



        // Token: 0x060020F8 RID: 8440 RVA: 0x000BADAC File Offset: 0x000B8FAC
        private void SteamPurchase()
        {
            Debug.Log("attempting to purchase item through steam");
            StartPurchaseRequest startPurchaseRequest = new StartPurchaseRequest();
            startPurchaseRequest.CatalogVersion = this.catalog;
            startPurchaseRequest.Items = new List<ItemPurchaseRequest>
            {
                new ItemPurchaseRequest
                {
                    ItemId = this.itemToPurchase,
                    Quantity = 1U,
                    Annotation = "Purchased via in-game store"
                }
            };
            PlayFabClientAPI.StartPurchase(startPurchaseRequest, delegate(StartPurchaseResult result)
            {
                Debug.Log("successfully started purchase. attempted to pay for purchase through steam");
                this.currentPurchaseID = result.OrderId;
                PlayFabClientAPI.PayForPurchase(new PayForPurchaseRequest
                {
                    OrderId = this.currentPurchaseID,
                    ProviderName = "Steam",
                    Currency = "RM"
                }, delegate(PayForPurchaseResult result2)
                {
                    Debug.Log("succeeded on sending request for paying with steam! waiting for response");
                    this.buyingBundle = true;
                    this.m_MicroTxnAuthorizationResponse = Callback<MicroTxnAuthorizationResponse_t>.Create(new Callback<MicroTxnAuthorizationResponse_t>.DispatchDelegate(this.OnMicroTxnAuthorizationResponse));
                }, delegate(PlayFabError error)
                {
                    if (error.Error == PlayFabErrorCode.NotAuthenticated)
                    {
                        PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
                    }
                    else if (error.Error == PlayFabErrorCode.AccountBanned)
                    {
                        Application.Quit();
                        PhotonNetwork.Disconnect();
                        UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
                        UnityEngine.Object.DestroyImmediate(GorillaLocomotion.Player.Instance);
                        GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
                        for (int i = 0; i < array.Length; i++)
                        {
                            UnityEngine.Object.Destroy(array[i]);
                        }
                    }
                    Debug.Log("failed to send request to purchase with steam!");
                    Debug.Log(error.ToString());
                    this.SwitchToStage(CosmeticsController.ATMStages.Failure);
                }, null, null);
            }, delegate(PlayFabError error)
            {
                if (error.Error == PlayFabErrorCode.NotAuthenticated)
                {
                    PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
                }
                else if (error.Error == PlayFabErrorCode.AccountBanned)
                {
                    Application.Quit();
                    PhotonNetwork.Disconnect();
                    UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
                    UnityEngine.Object.DestroyImmediate(GorillaLocomotion.Player.Instance);
                    GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    for (int i = 0; i < array.Length; i++)
                    {
                        UnityEngine.Object.Destroy(array[i]);
                    }
                }
                Debug.Log("error in starting purchase!");
            }, null, null);
        }



        // Token: 0x060020F9 RID: 8441 RVA: 0x000BAE3C File Offset: 0x000B903C
        public void ProcessATMState(string currencyButton)
        {
            switch (this.currentATMStage)
            {
            case CosmeticsController.ATMStages.Unavailable:
            case CosmeticsController.ATMStages.Purchasing:
                break;
            case CosmeticsController.ATMStages.Begin:
                this.SwitchToStage(CosmeticsController.ATMStages.Menu);
                return;
            case CosmeticsController.ATMStages.Menu:
                if (currencyButton == "one")
                {
                    this.SwitchToStage(CosmeticsController.ATMStages.Balance);
                    return;
                }
                if (currencyButton == "two")
                {
                    this.SwitchToStage(CosmeticsController.ATMStages.Choose);
                    return;
                }
                if (!(currencyButton == "four"))
                {
                    return;
                }
                this.SwitchToStage(CosmeticsController.ATMStages.Begin);
                return;
            case CosmeticsController.ATMStages.Balance:
                if (currencyButton == "four")
                {
                    this.SwitchToStage(CosmeticsController.ATMStages.Menu);
                    return;
                }
                break;
            case CosmeticsController.ATMStages.Choose:
                if (currencyButton == "one")
                {
                    this.numShinyRocksToBuy = 1000;
                    this.shinyRocksCost = 4.99f;
                    this.itemToPurchase = "1000SHINYROCKS";
                    this.SwitchToStage(CosmeticsController.ATMStages.Confirm);
                    return;
                }
                if (currencyButton == "two")
                {
                    this.numShinyRocksToBuy = 2200;
                    this.shinyRocksCost = 9.99f;
                    this.itemToPurchase = "2200SHINYROCKS";
                    this.SwitchToStage(CosmeticsController.ATMStages.Confirm);
                    return;
                }
                if (currencyButton == "three")
                {
                    this.numShinyRocksToBuy = 5000;
                    this.shinyRocksCost = 19.99f;
                    this.itemToPurchase = "5000SHINYROCKS";
                    this.SwitchToStage(CosmeticsController.ATMStages.Confirm);
                    return;
                }
                if (!(currencyButton == "four"))
                {
                    return;
                }
                this.SwitchToStage(CosmeticsController.ATMStages.Menu);
                return;
            case CosmeticsController.ATMStages.Confirm:
                if (currencyButton == "one")
                {
                    this.SteamPurchase();
                    this.SwitchToStage(CosmeticsController.ATMStages.Purchasing);
                    return;
                }
                if (!(currencyButton == "four"))
                {
                    return;
                }
                this.SwitchToStage(CosmeticsController.ATMStages.Choose);
                return;
            default:
                this.SwitchToStage(CosmeticsController.ATMStages.Menu);
                break;
            }
        }



        // Token: 0x060020FA RID: 8442 RVA: 0x000BAFC8 File Offset: 0x000B91C8
        public void SwitchToStage(CosmeticsController.ATMStages newStage)
        {
            this.currentATMStage = newStage;
            switch (newStage)
            {
            case CosmeticsController.ATMStages.Unavailable:
                this.atmText.text = "ATM NOT AVAILABLE! PLEASE TRY AGAIN LATER!";
                this.atmButtonsText.text = "";
                return;
            case CosmeticsController.ATMStages.Begin:
                this.atmText.text = "WELCOME! PRESS ANY BUTTON TO BEGIN.";
                this.atmButtonsText.text = "\n\n\n\n\n\n\n\n\nBEGIN   -->";
                return;
            case CosmeticsController.ATMStages.Menu:
                this.atmText.text = "CHECK YOUR BALANCE OR PURCHASE MORE SHINY ROCKS.";
                this.atmButtonsText.text = "BALANCE-- >\n\n\nPURCHASE-->\n\n\n\n\n\nBACK    -->";
                return;
            case CosmeticsController.ATMStages.Balance:
                this.atmText.text = "CURRENT BALANCE:\n\n" + this.currencyBalance.ToString();
                this.atmButtonsText.text = "\n\n\n\n\n\n\n\n\nBACK    -->";
                return;
            case CosmeticsController.ATMStages.Choose:
                this.atmText.text = "CHOOSE AN AMOUNT OF SHINY ROCKS TO PURCHASE.";
                this.atmButtonsText.text = "$4.99 FOR -->\n1000\n\n$9.99 FOR -->\n2200\n\n$19.99 FOR-->\n5000\n\nBACK -->";
                return;
            case CosmeticsController.ATMStages.Confirm:
                this.atmText.text = string.Concat(new string[]
                {
                    "YOU HAVE CHOSEN TO PURCHASE ",
                    this.numShinyRocksToBuy.ToString(),
                    " SHINY ROCKS FOR $",
                    this.shinyRocksCost.ToString(),
                    ". CONFIRM TO LAUNCH A STEAM WINDOW TO COMPLETE YOUR PURCHASE."
                });
                this.atmButtonsText.text = "CONFIRM -->\n\n\n\n\n\n\n\n\nBACK    -->";
                return;
            case CosmeticsController.ATMStages.Purchasing:
                this.atmText.text = "PURCHASING IN STEAM...";
                this.atmButtonsText.text = "";
                return;
            case CosmeticsController.ATMStages.Success:
                this.atmText.text = "SUCCESS! NEW SHINY ROCKS BALANCE: " + (this.currencyBalance + this.numShinyRocksToBuy).ToString();
                this.atmButtonsText.text = "\n\n\n\n\n\n\n\n\nRETURN  -->";
                return;
            case CosmeticsController.ATMStages.Failure:
                this.atmText.text = "PURCHASE CANCELED. NO FUNDS WERE SPENT.";
                this.atmButtonsText.text = "\n\n\n\n\n\n\n\n\nRETURN  -->";
                return;
            case CosmeticsController.ATMStages.Locked:
                this.atmText.text = "UNABLE TO PURCHASE AT THIS TIME. PLEASE RESTART THE GAME OR TRY AGAIN LATER.";
                this.atmButtonsText.text = "\n\n\n\n\n\n\n\n\nRETURN  -->";
                return;
            default:
                return;
            }
        }



        // Token: 0x060020FB RID: 8443 RVA: 0x000304D2 File Offset: 0x0002E6D2
        public void PressCurrencyPurchaseButton(string currencyPurchaseSize)
        {
            this.ProcessATMState(currencyPurchaseSize);
        }



        // Token: 0x060020FC RID: 8444 RVA: 0x000304DB File Offset: 0x0002E6DB
        private void OnMicroTxnAuthorizationResponse(MicroTxnAuthorizationResponse_t pCallback)
        {
            PlayFabClientAPI.ConfirmPurchase(new ConfirmPurchaseRequest
            {
                OrderId = this.currentPurchaseID
            }, delegate(ConfirmPurchaseResult result)
            {
                if (this.buyingBundle)
                {
                    this.buyingBundle = false;
                    if (PhotonNetwork.InRoom)
                    {
                        RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
                        WebFlags flags = new WebFlags(1);
                        raiseEventOptions.Flags = flags;
                        object[] eventContent = new object[0];
                        PhotonNetwork.RaiseEvent(9, eventContent, raiseEventOptions, SendOptions.SendReliable);
                    }
                    base.StartCoroutine(this.CheckIfMyCosmeticsUpdated(this.BundlePlayfabItemName));
                }
                this.SwitchToStage(CosmeticsController.ATMStages.Success);
                this.GetCurrencyBalance();
                this.UpdateCurrencyBoard();
                this.GetUserCosmeticsAllowed();
                GorillaTagger.Instance.offlineVRRig.GetUserCosmeticsAllowed();
            }, delegate(PlayFabError error)
            {
                this.atmText.text = "PURCHASE CANCELLED!\n\nCURRENT BALANCE IS: ";
                this.UpdateCurrencyBoard();
                this.SwitchToStage(CosmeticsController.ATMStages.Failure);
            }, null, null);
        }



        // Token: 0x060020FD RID: 8445 RVA: 0x000BB1B8 File Offset: 0x000B93B8
        public void UpdateCurrencyBoard()
        {
            this.FormattedPurchaseText(this.finalLine);
            this.dailyText.text = (this.checkedDaily ? (this.gotMyDaily ? "SUCCESSFULLY GOT DAILY ROCKS!" : "WAITING TO GET DAILY ROCKS...") : "CHECKING DAILY ROCKS...");
            this.currencyBoardText.text = string.Concat(new string[]
            {
                this.currencyBalance.ToString(),
                "\n\n",
                (this.secondsUntilTomorrow / 3600).ToString(),
                " HR, ",
                (this.secondsUntilTomorrow % 3600 / 60).ToString(),
                "MIN"
            });
        }



        // Token: 0x060020FE RID: 8446 RVA: 0x0003050D File Offset: 0x0002E70D
        public void GetCurrencyBalance()
        {
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), delegate(GetUserInventoryResult result)
            {
                this.currencyBalance = result.VirtualCurrency[this.currencyName];
                this.UpdateCurrencyBoard();
            }, delegate(PlayFabError error)
            {
                if (error.Error == PlayFabErrorCode.NotAuthenticated)
                {
                    PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
                    return;
                }
                if (error.Error == PlayFabErrorCode.AccountBanned)
                {
                    Application.Quit();
                    PhotonNetwork.Disconnect();
                    UnityEngine.Object.DestroyImmediate(PhotonNetworkController.Instance);
                    UnityEngine.Object.DestroyImmediate(GorillaLocomotion.Player.Instance);
                    GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    for (int i = 0; i < array.Length; i++)
                    {
                        UnityEngine.Object.Destroy(array[i]);
                    }
                }
            }, null, null);
        }



        // Token: 0x060020FF RID: 8447 RVA: 0x00030546 File Offset: 0x0002E746
        public string GetItemDisplayName(CosmeticsController.CosmeticItem item)
        {
            if (item.overrideDisplayName != null && item.overrideDisplayName != "")
            {
                return item.overrideDisplayName;
            }
            return item.displayName;
        }



        // Token: 0x06002100 RID: 8448 RVA: 0x0001B2AB File Offset: 0x000194AB
        public void LeaveSystemMenu()
        {
        }



        // Token: 0x06002101 RID: 8449 RVA: 0x000BB26C File Offset: 0x000B946C
        private void AlreadyOwnAllBundleButtons()
        {
            EarlyAccessButton[] array = this.earlyAccessButtons;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].AlreadyOwn();
            }
        }



        // Token: 0x04002325 RID: 8997
        public static int maximumTransferrableItems = 5;



        // Token: 0x04002326 RID: 8998
        [OnEnterPlay_SetNull]
        public static volatile CosmeticsController instance;



        // Token: 0x04002327 RID: 8999
        public List<CosmeticsController.CosmeticItem> allCosmetics;



        // Token: 0x04002328 RID: 9000
        public Dictionary<string, CosmeticsController.CosmeticItem> allCosmeticsDict = new Dictionary<string, CosmeticsController.CosmeticItem>();



        // Token: 0x04002329 RID: 9001
        public Dictionary<string, string> allCosmeticsItemIDsfromDisplayNamesDict = new Dictionary<string, string>();



        // Token: 0x0400232A RID: 9002
        public CosmeticsController.CosmeticItem nullItem;



        // Token: 0x0400232B RID: 9003
        public string catalog;



        // Token: 0x0400232C RID: 9004
        private string[] tempStringArray;



        // Token: 0x0400232D RID: 9005
        private CosmeticsController.CosmeticItem tempItem;



        // Token: 0x0400232E RID: 9006
        public List<CatalogItem> catalogItems;



        // Token: 0x0400232F RID: 9007
        public bool tryTwice;



        // Token: 0x04002330 RID: 9008
        [NonSerialized]
        public CosmeticsController.CosmeticSet tryOnSet = new CosmeticsController.CosmeticSet();



        // Token: 0x04002331 RID: 9009
        public FittingRoomButton[] fittingRoomButtons;



        // Token: 0x04002332 RID: 9010
        public CosmeticStand[] cosmeticStands;



        // Token: 0x04002333 RID: 9011
        public List<CosmeticsController.CosmeticItem> currentCart = new List<CosmeticsController.CosmeticItem>();



        // Token: 0x04002334 RID: 9012
        public CosmeticsController.PurchaseItemStages currentPurchaseItemStage;



        // Token: 0x04002335 RID: 9013
        public CheckoutCartButton[] checkoutCartButtons;



        // Token: 0x04002336 RID: 9014
        public PurchaseItemButton leftPurchaseButton;



        // Token: 0x04002337 RID: 9015
        public PurchaseItemButton rightPurchaseButton;



        // Token: 0x04002338 RID: 9016
        public Text purchaseText;



        // Token: 0x04002339 RID: 9017
        public CosmeticsController.CosmeticItem itemToBuy;



        // Token: 0x0400233A RID: 9018
        public HeadModel checkoutHeadModel;



        // Token: 0x0400233B RID: 9019
        private List<string> playerIDList = new List<string>();



        // Token: 0x0400233C RID: 9020
        private bool foundCosmetic;



        // Token: 0x0400233D RID: 9021
        private int attempts;



        // Token: 0x0400233E RID: 9022
        private string finalLine;



        // Token: 0x0400233F RID: 9023
        private bool purchaseLocked;



        // Token: 0x04002340 RID: 9024
        private bool isLastHandTouchedLeft;



        // Token: 0x04002341 RID: 9025
        private CosmeticsController.CosmeticSet cachedSet = new CosmeticsController.CosmeticSet();



        // Token: 0x04002342 RID: 9026
        private List<WardrobeInstance> wardrobes = new List<WardrobeInstance>();



        // Token: 0x04002343 RID: 9027
        public List<CosmeticsController.CosmeticItem> unlockedCosmetics = new List<CosmeticsController.CosmeticItem>();



        // Token: 0x04002344 RID: 9028
        public List<CosmeticsController.CosmeticItem> unlockedHats = new List<CosmeticsController.CosmeticItem>();



        // Token: 0x04002345 RID: 9029
        public List<CosmeticsController.CosmeticItem> unlockedFaces = new List<CosmeticsController.CosmeticItem>();



        // Token: 0x04002346 RID: 9030
        public List<CosmeticsController.CosmeticItem> unlockedBadges = new List<CosmeticsController.CosmeticItem>();



        // Token: 0x04002347 RID: 9031
        public List<CosmeticsController.CosmeticItem> unlockedHoldable = new List<CosmeticsController.CosmeticItem>();



        // Token: 0x04002348 RID: 9032
        public int[] cosmeticsPages = new int[4];



        // Token: 0x04002349 RID: 9033
        private List<CosmeticsController.CosmeticItem>[] itemLists = new List<CosmeticsController.CosmeticItem>[4];



        // Token: 0x0400234A RID: 9034
        private int wardrobeType;



        // Token: 0x0400234B RID: 9035
        [NonSerialized]
        public CosmeticsController.CosmeticSet currentWornSet = new CosmeticsController.CosmeticSet();



        // Token: 0x0400234C RID: 9036
        public string concatStringCosmeticsAllowed = "";



        // Token: 0x0400234D RID: 9037
        public Text atmText;



        // Token: 0x0400234E RID: 9038
        public string currentAtmString;



        // Token: 0x0400234F RID: 9039
        public Text infoText;



        // Token: 0x04002350 RID: 9040
        public Text earlyAccessText;



        // Token: 0x04002351 RID: 9041
        public Text[] purchaseButtonText;



        // Token: 0x04002352 RID: 9042
        public Text dailyText;



        // Token: 0x04002353 RID: 9043
        public CosmeticsController.ATMStages currentATMStage;



        // Token: 0x04002354 RID: 9044
        public Text atmButtonsText;



        // Token: 0x04002355 RID: 9045
        public int currencyBalance;



        // Token: 0x04002356 RID: 9046
        public string currencyName;



        // Token: 0x04002357 RID: 9047
        public PurchaseCurrencyButton[] purchaseCurrencyButtons;



        // Token: 0x04002358 RID: 9048
        public Text currencyBoardText;



        // Token: 0x04002359 RID: 9049
        public Text currencyBoxText;



        // Token: 0x0400235A RID: 9050
        public string startingCurrencyBoxTextString;



        // Token: 0x0400235B RID: 9051
        public string successfulCurrencyPurchaseTextString;



        // Token: 0x0400235C RID: 9052
        public int numShinyRocksToBuy;



        // Token: 0x0400235D RID: 9053
        public float shinyRocksCost;



        // Token: 0x0400235E RID: 9054
        public string itemToPurchase;



        // Token: 0x0400235F RID: 9055
        public bool confirmedDidntPlayInBeta;



        // Token: 0x04002360 RID: 9056
        public bool playedInBeta;



        // Token: 0x04002361 RID: 9057
        public bool gotMyDaily;



        // Token: 0x04002362 RID: 9058
        public bool checkedDaily;



        // Token: 0x04002363 RID: 9059
        public string currentPurchaseID;



        // Token: 0x04002364 RID: 9060
        public bool hasPrice;



        // Token: 0x04002365 RID: 9061
        private int searchIndex;



        // Token: 0x04002366 RID: 9062
        private int iterator;



        // Token: 0x04002367 RID: 9063
        private CosmeticsController.CosmeticItem cosmeticItemVar;



        // Token: 0x04002368 RID: 9064
        public EarlyAccessButton[] earlyAccessButtons;



        // Token: 0x04002369 RID: 9065
        private BundleList bundleList = new BundleList();



        // Token: 0x0400236A RID: 9066
        public string BundleSkuName = "2024_i_lava_you_pack";



        // Token: 0x0400236B RID: 9067
        public string BundlePlayfabItemName = "LSABG.";



        // Token: 0x0400236C RID: 9068
        public int BundleShinyRocks = 10000;



        // Token: 0x0400236D RID: 9069
        public bool buyingBundle;



        // Token: 0x0400236E RID: 9070
        public DateTime currentTime;



        // Token: 0x0400236F RID: 9071
        public string lastDailyLogin;



        // Token: 0x04002370 RID: 9072
        public UserDataRecord userDataRecord;



        // Token: 0x04002371 RID: 9073
        public int secondsUntilTomorrow;



        // Token: 0x04002372 RID: 9074
        public float secondsToWaitToCheckDaily = 10f;



        // Token: 0x04002373 RID: 9075
        private string returnString;



        // Token: 0x04002374 RID: 9076
        protected Callback<MicroTxnAuthorizationResponse_t> m_MicroTxnAuthorizationResponse;



        // Token: 0x0200059A RID: 1434
        public enum PurchaseItemStages
        {
            // Token: 0x04002376 RID: 9078
            Start,
            // Token: 0x04002377 RID: 9079
            CheckoutButtonPressed,
            // Token: 0x04002378 RID: 9080
            ItemSelected,
            // Token: 0x04002379 RID: 9081
            ItemOwned,
            // Token: 0x0400237A RID: 9082
            FinalPurchaseAcknowledgement,
            // Token: 0x0400237B RID: 9083
            Buying,
            // Token: 0x0400237C RID: 9084
            Success,
            // Token: 0x0400237D RID: 9085
            Failure
        }



        // Token: 0x0200059B RID: 1435
        public enum ATMStages
        {
            // Token: 0x0400237F RID: 9087
            Unavailable,
            // Token: 0x04002380 RID: 9088
            Begin,
            // Token: 0x04002381 RID: 9089
            Menu,
            // Token: 0x04002382 RID: 9090
            Balance,
            // Token: 0x04002383 RID: 9091
            Choose,
            // Token: 0x04002384 RID: 9092
            Confirm,
            // Token: 0x04002385 RID: 9093
            Purchasing,
            // Token: 0x04002386 RID: 9094
            Success,
            // Token: 0x04002387 RID: 9095
            Failure,
            // Token: 0x04002388 RID: 9096
            Locked
        }



        // Token: 0x0200059C RID: 1436
        public enum CosmeticCategory
        {
            // Token: 0x0400238A RID: 9098
            None,
            // Token: 0x0400238B RID: 9099
            Hat,
            // Token: 0x0400238C RID: 9100
            Badge,
            // Token: 0x0400238D RID: 9101
            Face,
            // Token: 0x0400238E RID: 9102
            Holdable,
            // Token: 0x0400238F RID: 9103
            Gloves,
            // Token: 0x04002390 RID: 9104
            Slingshot,
            // Token: 0x04002391 RID: 9105
            Skin,
            // Token: 0x04002392 RID: 9106
            Count,
            // Token: 0x04002393 RID: 9107
            Set
        }



        // Token: 0x0200059D RID: 1437
        public enum CosmeticSlots
        {
            // Token: 0x04002395 RID: 9109
            Hat,
            // Token: 0x04002396 RID: 9110
            Badge,
            // Token: 0x04002397 RID: 9111
            Face,
            // Token: 0x04002398 RID: 9112
            ArmLeft,
            // Token: 0x04002399 RID: 9113
            ArmRight,
            // Token: 0x0400239A RID: 9114
            BackLeft,
            // Token: 0x0400239B RID: 9115
            BackRight,
            // Token: 0x0400239C RID: 9116
            HandLeft,
            // Token: 0x0400239D RID: 9117
            HandRight,
            // Token: 0x0400239E RID: 9118
            Chest,
            // Token: 0x0400239F RID: 9119
            Skin,
            // Token: 0x040023A0 RID: 9120
            Count
        }



        // Token: 0x0200059E RID: 1438
        [Serializable]
        public class CosmeticSet
        {
            // Token: 0x14000039 RID: 57
            // (add) Token: 0x06002114 RID: 8468 RVA: 0x000BB80C File Offset: 0x000B9A0C
            // (remove) Token: 0x06002115 RID: 8469 RVA: 0x000BB844 File Offset: 0x000B9A44
            public event CosmeticsController.CosmeticSet.OnSetActivatedHandler onSetActivatedEvent;



            // Token: 0x06002116 RID: 8470 RVA: 0x00030624 File Offset: 0x0002E824
            protected void OnSetActivated(CosmeticsController.CosmeticSet prevSet, CosmeticsController.CosmeticSet currentSet, NetPlayer netPlayer)
            {
                if (this.onSetActivatedEvent != null)
                {
                    this.onSetActivatedEvent(prevSet, currentSet, netPlayer);
                }
            }



            // Token: 0x06002117 RID: 8471 RVA: 0x0003063C File Offset: 0x0002E83C
            public CosmeticSet()
            {
                this.items = new CosmeticsController.CosmeticItem[11];
            }



            // Token: 0x06002118 RID: 8472 RVA: 0x000BB87C File Offset: 0x000B9A7C
            public CosmeticSet(string[] itemNames, CosmeticsController controller)
            {
                this.items = new CosmeticsController.CosmeticItem[11];
                for (int i = 0; i < itemNames.Length; i++)
                {
                    string displayName = itemNames[i];
                    string itemNameFromDisplayName = controller.GetItemNameFromDisplayName(displayName);
                    this.items[i] = controller.GetItemFromDict(itemNameFromDisplayName);
                }
            }



            // Token: 0x06002119 RID: 8473 RVA: 0x000BB8D8 File Offset: 0x000B9AD8
            public void CopyItems(CosmeticsController.CosmeticSet other)
            {
                for (int i = 0; i < this.items.Length; i++)
                {
                    this.items[i] = other.items[i];
                }
            }



            // Token: 0x0600211A RID: 8474 RVA: 0x000BB910 File Offset: 0x000B9B10
            public void MergeSets(CosmeticsController.CosmeticSet tryOn, CosmeticsController.CosmeticSet current)
            {
                for (int i = 0; i < 11; i++)
                {
                    if (tryOn == null)
                    {
                        this.items[i] = current.items[i];
                    }
                    else
                    {
                        this.items[i] = (tryOn.items[i].isNullItem ? current.items[i] : tryOn.items[i]);
                    }
                }
            }



            // Token: 0x0600211B RID: 8475 RVA: 0x000BB980 File Offset: 0x000B9B80
            public void ClearSet(CosmeticsController.CosmeticItem nullItem)
            {
                for (int i = 0; i < 11; i++)
                {
                    this.items[i] = nullItem;
                }
            }



            // Token: 0x0600211C RID: 8476 RVA: 0x000BB9A8 File Offset: 0x000B9BA8
            public bool IsActive(string name)
            {
                int num = 11;
                for (int i = 0; i < num; i++)
                {
                    if (this.items[i].displayName == name)
                    {
                        return true;
                    }
                }
                return false;
            }



            // Token: 0x0600211D RID: 8477 RVA: 0x000BB9E0 File Offset: 0x000B9BE0
            public bool HasItemOfCategory(CosmeticsController.CosmeticCategory category)
            {
                int num = 11;
                for (int i = 0; i < num; i++)
                {
                    if (!this.items[i].isNullItem && this.items[i].itemCategory == category)
                    {
                        return true;
                    }
                }
                return false;
            }



            // Token: 0x0600211E RID: 8478 RVA: 0x000BBA28 File Offset: 0x000B9C28
            public bool HasItem(string name)
            {
                int num = 11;
                for (int i = 0; i < num; i++)
                {
                    if (!this.items[i].isNullItem && this.items[i].displayName == name)
                    {
                        return true;
                    }
                }
                return false;
            }



            // Token: 0x0600211F RID: 8479 RVA: 0x0003065E File Offset: 0x0002E85E
            public static bool IsSlotLeftHanded(CosmeticsController.CosmeticSlots slot)
            {
                return slot == CosmeticsController.CosmeticSlots.ArmLeft || slot == CosmeticsController.CosmeticSlots.BackLeft || slot == CosmeticsController.CosmeticSlots.HandLeft;
            }



            // Token: 0x06002120 RID: 8480 RVA: 0x0003066E File Offset: 0x0002E86E
            public static bool IsSlotRightHanded(CosmeticsController.CosmeticSlots slot)
            {
                return slot == CosmeticsController.CosmeticSlots.ArmRight || slot == CosmeticsController.CosmeticSlots.BackRight || slot == CosmeticsController.CosmeticSlots.HandRight;
            }



            // Token: 0x06002121 RID: 8481 RVA: 0x0003067E File Offset: 0x0002E87E
            public static bool IsHoldable(CosmeticsController.CosmeticItem item)
            {
                return item.itemCategory == CosmeticsController.CosmeticCategory.Holdable || item.itemCategory == CosmeticsController.CosmeticCategory.Slingshot;
            }



            // Token: 0x06002122 RID: 8482 RVA: 0x00030694 File Offset: 0x0002E894
            public static bool IsSlotHoldable(CosmeticsController.CosmeticSlots slot)
            {
                return slot == CosmeticsController.CosmeticSlots.ArmLeft || slot == CosmeticsController.CosmeticSlots.ArmRight || slot == CosmeticsController.CosmeticSlots.BackLeft || slot == CosmeticsController.CosmeticSlots.BackRight || slot == CosmeticsController.CosmeticSlots.Chest;
            }



            // Token: 0x06002123 RID: 8483 RVA: 0x000BBA74 File Offset: 0x000B9C74
            public static CosmeticsController.CosmeticSlots OppositeSlot(CosmeticsController.CosmeticSlots slot)
            {
                switch (slot)
                {
                case CosmeticsController.CosmeticSlots.Hat:
                    return CosmeticsController.CosmeticSlots.Hat;
                case CosmeticsController.CosmeticSlots.Badge:
                    return CosmeticsController.CosmeticSlots.Badge;
                case CosmeticsController.CosmeticSlots.Face:
                    return CosmeticsController.CosmeticSlots.Face;
                case CosmeticsController.CosmeticSlots.ArmLeft:
                    return CosmeticsController.CosmeticSlots.ArmRight;
                case CosmeticsController.CosmeticSlots.ArmRight:
                    return CosmeticsController.CosmeticSlots.ArmLeft;
                case CosmeticsController.CosmeticSlots.BackLeft:
                    return CosmeticsController.CosmeticSlots.BackRight;
                case CosmeticsController.CosmeticSlots.BackRight:
                    return CosmeticsController.CosmeticSlots.BackLeft;
                case CosmeticsController.CosmeticSlots.HandLeft:
                    return CosmeticsController.CosmeticSlots.HandRight;
                case CosmeticsController.CosmeticSlots.HandRight:
                    return CosmeticsController.CosmeticSlots.HandLeft;
                case CosmeticsController.CosmeticSlots.Chest:
                    return CosmeticsController.CosmeticSlots.Chest;
                case CosmeticsController.CosmeticSlots.Skin:
                    return CosmeticsController.CosmeticSlots.Skin;
                default:
                    return CosmeticsController.CosmeticSlots.Count;
                }
            }



            // Token: 0x06002124 RID: 8484 RVA: 0x000306AD File Offset: 0x0002E8AD
            public static string SlotPlayerPreferenceName(CosmeticsController.CosmeticSlots slot)
            {
                return "slot_" + slot.ToString();
            }



            // Token: 0x06002125 RID: 8485 RVA: 0x000BBAD0 File Offset: 0x000B9CD0
            private void ActivateHoldable(int cosmeticIdx, BodyDockPositions bDock, CosmeticsController.CosmeticItem nullItem)
            {
                BodyDockPositions.DropPositions dropPositions = CosmeticsController.CosmeticSlotToDropPosition((CosmeticsController.CosmeticSlots)cosmeticIdx);
                CosmeticsController.CosmeticItem cosmeticItem = this.items[cosmeticIdx];
                if (cosmeticItem.isNullItem)
                {
                    bDock.TransferrableItemDisableAtPosition(dropPositions);
                    return;
                }
                if (bDock.ItemPositionInUse(dropPositions) == null)
                {
                    bDock.TransferrableItemEnableAtPosition(cosmeticItem.displayName, dropPositions);
                    return;
                }
                if (!bDock.TransferrableItemActiveAtPos(cosmeticItem.displayName, dropPositions))
                {
                    bDock.TransferrableItemDisableAtPosition(dropPositions);
                    bDock.TransferrableItemEnableAtPosition(cosmeticItem.displayName, dropPositions);
                }
            }



            // Token: 0x06002126 RID: 8486 RVA: 0x000BBB44 File Offset: 0x000B9D44
            private void ActivateCosmeticItem(CosmeticsController.CosmeticSet prevSet, VRRig rig, int cosmeticIdx, CosmeticItemRegistry cosmeticsObjectRegistry, CosmeticsController.CosmeticItem nullItem)
            {
                CosmeticsController.CosmeticItem cosmeticItem = prevSet.items[cosmeticIdx];
                CosmeticsController.CosmeticItem cosmeticItem2 = this.items[cosmeticIdx];
                CosmeticItemInstance cosmeticItemInstance = cosmeticsObjectRegistry.Cosmetic(cosmeticItem.displayName);
                CosmeticItemInstance cosmeticItemInstance2 = cosmeticsObjectRegistry.Cosmetic(cosmeticItem2.displayName);
                string itemNameFromDisplayName = CosmeticsController.instance.GetItemNameFromDisplayName(cosmeticItem2.displayName);
                string itemNameFromDisplayName2 = CosmeticsController.instance.GetItemNameFromDisplayName(cosmeticItem.displayName);
                if (itemNameFromDisplayName == itemNameFromDisplayName2)
                {
                    if (cosmeticItem2.isNullItem)
                    {
                        return;
                    }
                    if (cosmeticItemInstance2 != null)
                    {
                        if (!rig.IsItemAllowed(itemNameFromDisplayName))
                        {
                            cosmeticItemInstance2.DisableItem((CosmeticsController.CosmeticSlots)cosmeticIdx);
                            return;
                        }
                        cosmeticItemInstance2.EnableItem((CosmeticsController.CosmeticSlots)cosmeticIdx);
                    }
                    return;
                }
                else
                {
                    if (cosmeticItem2.isNullItem)
                    {
                        if (!cosmeticItem.isNullItem && cosmeticItemInstance != null)
                        {
                            cosmeticItemInstance.DisableItem((CosmeticsController.CosmeticSlots)cosmeticIdx);
                        }
                        return;
                    }
                    if (!cosmeticItem.isNullItem && cosmeticItemInstance != null)
                    {
                        cosmeticItemInstance.DisableItem((CosmeticsController.CosmeticSlots)cosmeticIdx);
                    }
                    if (rig.IsItemAllowed(itemNameFromDisplayName) && cosmeticItemInstance2 != null)
                    {
                        cosmeticItemInstance2.EnableItem((CosmeticsController.CosmeticSlots)cosmeticIdx);
                    }
                    return;
                }
            }



            // Token: 0x06002127 RID: 8487 RVA: 0x000BBC2C File Offset: 0x000B9E2C
            public void ActivateCosmetics(CosmeticsController.CosmeticSet prevSet, VRRig rig, BodyDockPositions bDock, CosmeticsController.CosmeticItem nullItem, CosmeticItemRegistry cosmeticsObjectRegistry)
            {
                int num = 11;
                for (int i = 0; i < num; i++)
                {
                    if (CosmeticsController.CosmeticSet.IsSlotHoldable((CosmeticsController.CosmeticSlots)i))
                    {
                        this.ActivateHoldable(i, bDock, nullItem);
                    }
                    else
                    {
                        this.ActivateCosmeticItem(prevSet, rig, i, cosmeticsObjectRegistry, nullItem);
                    }
                }
                this.OnSetActivated(prevSet, this, rig.creatorWrapped);
            }



            // Token: 0x06002128 RID: 8488 RVA: 0x000BBC78 File Offset: 0x000B9E78
            public void DeactivateAllCosmetcs(BodyDockPositions bDock, CosmeticsController.CosmeticItem nullItem, CosmeticItemRegistry cosmeticObjectRegistry)
            {
                bDock.DisableAllTransferableItems();
                int num = 11;
                for (int i = 0; i < num; i++)
                {
                    CosmeticsController.CosmeticItem cosmeticItem = this.items[i];
                    if (!cosmeticItem.isNullItem)
                    {
                        CosmeticsController.CosmeticSlots cosmeticSlots = (CosmeticsController.CosmeticSlots)i;
                        if (!CosmeticsController.CosmeticSet.IsSlotHoldable(cosmeticSlots))
                        {
                            CosmeticItemInstance cosmeticItemInstance = cosmeticObjectRegistry.Cosmetic(cosmeticItem.displayName);
                            if (cosmeticItemInstance != null)
                            {
                                cosmeticItemInstance.DisableItem(cosmeticSlots);
                            }
                        }
                        this.items[i] = nullItem;
                    }
                }
            }



            // Token: 0x06002129 RID: 8489 RVA: 0x000BBCE0 File Offset: 0x000B9EE0
            public void LoadFromPlayerPreferences(CosmeticsController controller)
            {
                int num = 11;
                for (int i = 0; i < num; i++)
                {
                    CosmeticsController.CosmeticSlots slot = (CosmeticsController.CosmeticSlots)i;
                    CosmeticsController.CosmeticItem item = controller.GetItemFromDict(PlayerPrefs.GetString(CosmeticsController.CosmeticSet.SlotPlayerPreferenceName(slot), "NOTHING"));
                    if (controller.unlockedCosmetics.FindIndex((CosmeticsController.CosmeticItem x) => item.itemName == x.itemName) >= 0)
                    {
                        this.items[i] = item;
                    }
                    else
                    {
                        this.items[i] = controller.nullItem;
                    }
                }
            }



            // Token: 0x0600212A RID: 8490 RVA: 0x000BBD60 File Offset: 0x000B9F60
            public string[] ToDisplayNameArray()
            {
                int num = 11;
                for (int i = 0; i < num; i++)
                {
                    this.returnArray[i] = this.items[i].displayName;
                }
                return this.returnArray;
            }



            // Token: 0x0600212B RID: 8491 RVA: 0x000BBD9C File Offset: 0x000B9F9C
            public string[] HoldableDisplayNames(bool leftHoldables)
            {
                int num = 11;
                int num2 = 0;
                for (int i = 0; i < num; i++)
                {
                    if (this.items[i].itemCategory == CosmeticsController.CosmeticCategory.Holdable && this.items[i].itemCategory == CosmeticsController.CosmeticCategory.Holdable)
                    {
                        if (leftHoldables && BodyDockPositions.IsPositionLeft(CosmeticsController.CosmeticSlotToDropPosition((CosmeticsController.CosmeticSlots)i)))
                        {
                            num2++;
                        }
                        else if (!leftHoldables && !BodyDockPositions.IsPositionLeft(CosmeticsController.CosmeticSlotToDropPosition((CosmeticsController.CosmeticSlots)i)))
                        {
                            num2++;
                        }
                    }
                }
                if (num2 == 0)
                {
                    return null;
                }
                int num3 = 0;
                string[] array = new string[num2];
                for (int j = 0; j < num; j++)
                {
                    if (this.items[j].itemCategory == CosmeticsController.CosmeticCategory.Holdable)
                    {
                        if (leftHoldables && BodyDockPositions.IsPositionLeft(CosmeticsController.CosmeticSlotToDropPosition((CosmeticsController.CosmeticSlots)j)))
                        {
                            array[num3] = this.items[j].displayName;
                            num3++;
                        }
                        else if (!leftHoldables && !BodyDockPositions.IsPositionLeft(CosmeticsController.CosmeticSlotToDropPosition((CosmeticsController.CosmeticSlots)j)))
                        {
                            array[num3] = this.items[j].displayName;
                            num3++;
                        }
                    }
                }
                return array;
            }



            // Token: 0x0600212C RID: 8492 RVA: 0x000BBEA0 File Offset: 0x000BA0A0
            public bool[] ToOnRightSideArray()
            {
                int num = 11;
                bool[] array = new bool[num];
                for (int i = 0; i < num; i++)
                {
                    if (this.items[i].itemCategory == CosmeticsController.CosmeticCategory.Holdable)
                    {
                        array[i] = !BodyDockPositions.IsPositionLeft(CosmeticsController.CosmeticSlotToDropPosition((CosmeticsController.CosmeticSlots)i));
                    }
                    else
                    {
                        array[i] = false;
                    }
                }
                return array;
            }



            // Token: 0x040023A1 RID: 9121
            public CosmeticsController.CosmeticItem[] items;



            // Token: 0x040023A3 RID: 9123
            public string[] returnArray = new string[11];



            // Token: 0x0200059F RID: 1439
            // (Invoke) Token: 0x0600212E RID: 8494
            public delegate void OnSetActivatedHandler(CosmeticsController.CosmeticSet prevSet, CosmeticsController.CosmeticSet currentSet, NetPlayer netPlayer);
        }



        // Token: 0x020005A1 RID: 1441
        [Serializable]
        public struct CosmeticItem
        {
            // Token: 0x040023A5 RID: 9125
            [Tooltip("Should match the spreadsheet item name.")]
            public string itemName;



            // Token: 0x040023A6 RID: 9126
            [Tooltip("Determines what wardrobe section the item will show up in.")]
            public CosmeticsController.CosmeticCategory itemCategory;



            // Token: 0x040023A7 RID: 9127
            [Tooltip("Icon shown in the store menus & hunt watch.")]
            public Sprite itemPicture;



            // Token: 0x040023A8 RID: 9128
            public string displayName;



            // Token: 0x040023A9 RID: 9129
            public string itemPictureResourceString;



            // Token: 0x040023AA RID: 9130
            [Tooltip("The name shown on the store checkout screen.")]
            public string overrideDisplayName;



            // Token: 0x040023AB RID: 9131
            [DebugReadout]
            [NonSerialized]
            public int cost;



            // Token: 0x040023AC RID: 9132
            [DebugReadout]
            [NonSerialized]
            public string[] bundledItems;



            // Token: 0x040023AD RID: 9133
            [DebugReadout]
            [NonSerialized]
            public bool canTryOn;



            // Token: 0x040023AE RID: 9134
            [Tooltip("Set to true if the item takes up both left and right wearable hand slots at the same time. Used for things like mittens/gloves.")]
            public bool bothHandsHoldable;



            // Token: 0x040023AF RID: 9135
            public bool bLoadsFromResources;



            // Token: 0x040023B0 RID: 9136
            public bool bUsesMeshAtlas;



            // Token: 0x040023B1 RID: 9137
            public Vector3 rotationOffset;



            // Token: 0x040023B2 RID: 9138
            public Vector3 positionOffset;



            // Token: 0x040023B3 RID: 9139
            public string meshAtlasResourceString;



            // Token: 0x040023B4 RID: 9140
            public string meshResourceString;



            // Token: 0x040023B5 RID: 9141
            public string materialResourceString;



            // Token: 0x040023B6 RID: 9142
            [HideInInspector]
            public bool isNullItem;
        }



        // Token: 0x020005A2 RID: 1442
        [Serializable]
        public class IAPRequestBody
        {
            // Token: 0x040023B7 RID: 9143
            public string accessToken;



            // Token: 0x040023B8 RID: 9144
            public string userID;



            // Token: 0x040023B9 RID: 9145
            public string nonce;



            // Token: 0x040023BA RID: 9146
            public string platform;



            // Token: 0x040023BB RID: 9147
            public string sku;



            // Token: 0x040023BC RID: 9148
            public string playFabId;



            // Token: 0x040023BD RID: 9149
            public bool[] debugParameters;
        }
    }
}

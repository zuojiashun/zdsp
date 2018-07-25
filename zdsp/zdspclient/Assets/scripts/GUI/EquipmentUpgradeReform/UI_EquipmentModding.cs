﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Zealot.Common;
using Zealot.Client.Entities;
using Zealot.Repository;
using Kopio.JsonContracts;

public class ModdingEquipment
{
    public int mSlotID;
    public Equipment mEquip;

    public ModdingEquipment(int slotId, Equipment equipment)
    {
        mSlotID = slotId;
        mEquip = equipment;
    }
}

public class UI_EquipmentModding : MonoBehaviour
{
    public List<ModdingEquipment> GetModdingEquipmentList(List<Equipment> equippedEquipList, List<IInventoryItem> invEquipList)
    {
        List<ModdingEquipment> equipUpgList = new List<ModdingEquipment>();
        for (int i = 0; i < equippedEquipList.Count; ++i)
        {
            Equipment equipment = equippedEquipList[i];
            if (equipment != null)
            {
                equipUpgList.Add(new ModdingEquipment(i, equipment));
            }
        }

        for (int i = 0; i < invEquipList.Count; ++i)
        {
            Equipment invEquip = invEquipList[i] as Equipment;
            if (invEquip != null)
            {
                equipUpgList.Add(new ModdingEquipment(i, invEquip));
            }
        }

        return equipUpgList;
    }
}
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using RPGMods.Commands;
using RPGMods.Hooks;
using RPGMods.Systems;
using RPGMods.Utils;
using System.IO;
using System.Reflection;
using UnhollowerRuntimeLib;
using Unity.Entities;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;

#if WETSTONE
using Wetstone.API;
#endif

namespace RPGMods
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]

#if WETSTONE
    [BepInDependency("xyz.molenzwiebel.wetstone")]
    [Reloadable]
    public class Plugin : BasePlugin, IRunOnInitialized
#else
    public class Plugin : BasePlugin
#endif
    {
        public static Harmony harmony;

        private static ConfigEntry<string> Prefix;
        private static ConfigEntry<string> DisabledCommands;
        private static ConfigEntry<float> DelayedCommands;
        private static ConfigEntry<int> WaypointLimit;

        private static ConfigEntry<bool> EnableVIPSystem;
        private static ConfigEntry<bool> EnableVIPWhitelist;
        private static ConfigEntry<int> VIP_Permission;

        private static ConfigEntry<double> VIP_InCombat_ResYield;
        private static ConfigEntry<double> VIP_InCombat_DurabilityLoss;
        private static ConfigEntry<double> VIP_InCombat_MoveSpeed;
        private static ConfigEntry<double> VIP_InCombat_GarlicResistance;
        private static ConfigEntry<double> VIP_InCombat_SilverResistance;

        private static ConfigEntry<double> VIP_OutCombat_ResYield;
        private static ConfigEntry<double> VIP_OutCombat_DurabilityLoss;
        private static ConfigEntry<double> VIP_OutCombat_MoveSpeed;
        private static ConfigEntry<double> VIP_OutCombat_GarlicResistance;
        private static ConfigEntry<double> VIP_OutCombat_SilverResistance;

        private static ConfigEntry<bool> AnnouncePvPKills;
        private static ConfigEntry<bool> EnablePvPToggle;

        private static ConfigEntry<bool> EnablePvPLadder;
        private static ConfigEntry<int> PvPLadderLength;
        private static ConfigEntry<bool> HonorSortLadder;
        
        private static ConfigEntry<bool> EnablePvPPunish;
        private static ConfigEntry<bool> EnablePvPPunishAnnounce;
        private static ConfigEntry<bool> ExcludeOfflineKills;
        private static ConfigEntry<int> PunishLevelDiff;
        private static ConfigEntry<float> PunishDuration;
        private static ConfigEntry<int> PunishOffenseLimit;
        private static ConfigEntry<float> PunishOffenseCooldown;

        private static ConfigEntry<bool> EnableHonorSystem;
        private static ConfigEntry<bool> EnableHonorTitle;
        private static ConfigEntry<int> MaxHonorGainPerSpan;
        private static ConfigEntry<bool> EnableHonorBenefit;
        private static ConfigEntry<int> HonorSiegeDuration;
        private static ConfigEntry<int> PvPDuration;
        private static ConfigEntry<bool> EnableHostileGlow;
        private static ConfigEntry<bool> UseProximityGlow;

        private static ConfigEntry<bool> BuffSiegeGolem;
        private static ConfigEntry<float> GolemPhysicalReduction;
        private static ConfigEntry<float> GolemSpellReduction;

        private static ConfigEntry<bool> HunterHuntedEnabled;
        private static ConfigEntry<int> HeatCooldown;
        private static ConfigEntry<int> BanditHeatCooldown;
        private static ConfigEntry<int> CoolDown_Interval;
        private static ConfigEntry<int> Ambush_Interval;
        private static ConfigEntry<int> Ambush_Chance;
        private static ConfigEntry<float> Ambush_Despawn_Unit_Timer;

        private static ConfigEntry<bool> EnableExperienceSystem;
        private static ConfigEntry<int> MaxLevel;
        private static ConfigEntry<float> EXPMultiplier;
        private static ConfigEntry<float> VBloodEXPMultiplier;
        private static ConfigEntry<double> EXPLostOnDeath;
        private static ConfigEntry<float> EXPFormula_1;
        private static ConfigEntry<double> EXPGroupModifier;
        private static ConfigEntry<float> EXPGroupMaxDistance;

        private static ConfigEntry<bool> EnableWeaponMaster;
        private static ConfigEntry<bool> EnableWeaponMasterDecay;
        private static ConfigEntry<float> WeaponMasterMultiplier;
        private static ConfigEntry<int> WeaponDecayInterval;
        private static ConfigEntry<int> WeaponMaxMastery;
        private static ConfigEntry<float> WeaponMastery_VBloodMultiplier;
        private static ConfigEntry<int> Offline_Weapon_MasteryDecayValue;
        private static ConfigEntry<int> MasteryCombatTick;
        private static ConfigEntry<int> MasteryMaxCombatTicks;
        private static ConfigEntry<bool> WeaponMasterySpellMasteryNeedsNoneToUse;
        private static ConfigEntry<bool> WeaponMasterySpellMasteryNeedsNoneToLearn;
        private static ConfigEntry<bool> WeaponLinearSpellMastery;
        private static ConfigEntry<bool> WeaponSpellMasteryCDRStacks;
        private static ConfigEntry<bool> DetailedMasteryInfo;
        private static ConfigEntry<string> UnarmedStats;
        private static ConfigEntry<string> UnarmedRates;
        private static ConfigEntry<string> SpearStats;
        private static ConfigEntry<string> SpearRates;
        private static ConfigEntry<string> SwordStats;
        private static ConfigEntry<string> SwordRates;
        private static ConfigEntry<string> ScytheStats;
        private static ConfigEntry<string> ScytheRates;
        private static ConfigEntry<string> CrossbowStats;
        private static ConfigEntry<string> CrossbowRates;
        private static ConfigEntry<string> MaceStats;
        private static ConfigEntry<string> MaceRates;
        private static ConfigEntry<string> SlasherStats;
        private static ConfigEntry<string> SlasherRates;
        private static ConfigEntry<string> AxeStats;
        private static ConfigEntry<string> AxeRates;
        private static ConfigEntry<string> FishingPoleStats;
        private static ConfigEntry<string> FishingPoleRates;
        private static ConfigEntry<string> SpellStats;
        private static ConfigEntry<string> SpellRates;


        private static ConfigEntry<bool> effectivenessSubSystemEnabled;
        private static ConfigEntry<bool> growthSubSystemEnabled;
        private static ConfigEntry<float> maxEffectiveness;
        private static ConfigEntry<float> minGrowth;
        private static ConfigEntry<float> maxGrowth;
        private static ConfigEntry<float> growthPerEfficency;

        private static ConfigEntry<bool> EnableWorldDynamics;
        private static ConfigEntry<bool> WDGrowOnKill;


        public static bool isInitialized = false;

        public static ManualLogSource Logger;
        private static World _serverWorld;
        public static World Server
        {
            get
            {
                if (_serverWorld != null) return _serverWorld;

                _serverWorld = GetWorld("Server")
                    ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");
                return _serverWorld;
            }
        }

        public static bool IsServer => Application.productName == "VRisingServer";

        private static World GetWorld(string name)
        {
            foreach (var world in World.s_AllWorlds)
            {
                if (world.Name == name)
                {
                    return world;
                }
            }

            return null;
        }

        public void InitConfig()
        {
            Prefix = Config.Bind("Config", "Prefix", ".", "The prefix used for chat commands.");
            DelayedCommands = Config.Bind("Config", "Command Delay", 5f, "The number of seconds user need to wait out before sending another command.\n" +
                "Admin will always bypass this.");
            DisabledCommands = Config.Bind("Config", "Disabled Commands", "", "Enter command names to disable them, abbreviation are included automatically. Seperated by commas.\n" +
                "Ex.: save,godmode");
            WaypointLimit = Config.Bind("Config", "Waypoint Limit", 3, "Set a waypoint limit per user.");

            EnableVIPSystem = Config.Bind("VIP", "Enable VIP System", false, "Enable the VIP System.");
            EnableVIPWhitelist = Config.Bind("VIP", "Enable VIP Whitelist", false, "Enable the VIP user to ignore server capacity limit.");
            VIP_Permission = Config.Bind("VIP", "Minimum VIP Permission", 10, "The minimum permission level required for the user to be considered as VIP.");

            VIP_InCombat_DurabilityLoss = Config.Bind("VIP.InCombat", "Durability Loss Multiplier", 0.5, "Multiply durability loss when user is in combat. -1.0 to disable.\n" +
                "Does not affect durability loss on death.");
            VIP_InCombat_GarlicResistance = Config.Bind("VIP.InCombat", "Garlic Resistance Multiplier", -1.0, "Multiply garlic resistance when user is in combat. -1.0 to disable.");
            VIP_InCombat_SilverResistance = Config.Bind("VIP.InCombat", "Silver Resistance Multiplier", -1.0, "Multiply silver resistance when user is in combat. -1.0 to disable.");
            VIP_InCombat_MoveSpeed = Config.Bind("VIP.InCombat", "Move Speed Multiplier", -1.0, "Multiply move speed when user is in combat. -1.0 to disable.");
            VIP_InCombat_ResYield = Config.Bind("VIP.InCombat", "Resource Yield Multiplier", 2.0, "Multiply resource yield (not item drop) when user is in combat. -1.0 to disable.");

            VIP_OutCombat_DurabilityLoss = Config.Bind("VIP.OutCombat", "Durability Loss Multiplier", 0.5, "Multiply durability loss when user is out of combat. -1.0 to disable.\n" +
                "Does not affect durability loss on death.");
            VIP_OutCombat_GarlicResistance = Config.Bind("VIP.OutCombat", "Garlic Resistance Multiplier", 2.0, "Multiply garlic resistance when user is out of combat. -1.0 to disable.");
            VIP_OutCombat_SilverResistance = Config.Bind("VIP.OutCombat", "Silver Resistance Multiplier", 2.0, "Multiply silver resistance when user is out of combat. -1.0 to disable.");
            VIP_OutCombat_MoveSpeed = Config.Bind("VIP.OutCombat", "Move Speed Multiplier", 1.25, "Multiply move speed when user is out of combat. -1.0 to disable.");
            VIP_OutCombat_ResYield = Config.Bind("VIP.OutCombat", "Resource Yield Multiplier", 2.0, "Multiply resource yield (not item drop) when user is out of combat. -1.0 to disable.");

            AnnouncePvPKills = Config.Bind("PvP", "Announce PvP Kills", true, "Make a server wide announcement for all PvP kills.");
            EnableHonorSystem = Config.Bind("PvP", "Enable Honor System", false, "Enable the honor system.");
            EnableHonorTitle = Config.Bind("PvP", "Enable Honor Title", true, "When enabled, the system will append the title to their name.\nHonor system will leave the player name untouched if disabled.");
            MaxHonorGainPerSpan = Config.Bind("PvP", "Max Honor Gain/Hour", 250, "Maximum amount of honor points the player can gain per hour.");
            EnableHonorBenefit = Config.Bind("PvP", "Enable Honor Benefit & Penalties", true, "If disabled, the hostility state and custom siege system will be disabled.\n" +
                "All other bonus is also not applied.");
            HonorSiegeDuration = Config.Bind("PvP", "Custom Siege Duration", 180, "In minutes. Player will automatically exit siege mode after this many minutes has passed.\n" +
                "Siege mode cannot be exited while duration has not passed.");
            PvPDuration = Config.Bind("PvP", "Custom PVP Duration", 5, "In minutes, Player will be unable to exit PVP Mode until these many minutes has passed.\n");
            EnableHostileGlow = Config.Bind("PvP", "Enable Hostile Glow", true, "When set to true, hostile players will glow red.");
            UseProximityGlow = Config.Bind("PvP", "Enable Proximity Hostile Glow", true, "If enabled, hostile players will only glow when they are close to other online player.\n" +
                "If disabled, hostile players will always glow red.");
            EnablePvPLadder = Config.Bind("PvP", "Enable PvP Ladder", true, "Enables the PvP Ladder in the PvP command.");
            PvPLadderLength = Config.Bind("PvP", "Ladder Length", 10, "How many players should be displayed in the PvP Ladders.");
            HonorSortLadder = Config.Bind("PvP", "Sort PvP Ladder by Honor", true, "This will automatically be false if honor system is not enabled.");
            EnablePvPToggle = Config.Bind("PvP", "Enable PvP Toggle", false, "Enable/disable the pvp toggle feature in the pvp command.");

            EnablePvPPunish = Config.Bind("PvP", "Enable PvP Punishment", false, "Enables the punishment system for killing lower level player.");
            EnablePvPPunishAnnounce = Config.Bind("PvP", "Enable PvP Punish Announcement", true, "Announce all grief-kills that occured.");
            ExcludeOfflineKills = Config.Bind("PvP", "Exclude Offline Grief", true, "Do not punish the killer if the victim is offline.");
            PunishLevelDiff = Config.Bind("PvP", "Punish Level Difference", -10, "Only punish the killer if the victim level is this much lower.");
            PunishOffenseLimit = Config.Bind("PvP", "Offense Limit", 3, "Killer must make this many offense before the punishment debuff is applied.");
            PunishOffenseCooldown = Config.Bind("PvP", "Offense Cooldown", 300f, "Reset the offense counter after this many seconds has passed since last offense.");
            PunishDuration = Config.Bind("PvP", "Debuff Duration", 1800f, "Apply the punishment debuff for this amount of time.");

            BuffSiegeGolem = Config.Bind("Siege", "Buff Siege Golem", false, "Enabling this will reduce all incoming physical and spell damage according to config.");
            GolemPhysicalReduction = Config.Bind("Siege", "Physical Damage Reduction", 0.5f, "Reduce incoming damage by this much. Ex.: 0.25 -> 25%");
            GolemSpellReduction = Config.Bind("Siege", "Spell Damage Reduction", 0.5f, "Reduce incoming spell damage by this much. Ex.: 0.75 -> 75%");

            HunterHuntedEnabled = Config.Bind("HunterHunted", "Enable", true, "Enable/disable the HunterHunted system.");
            HeatCooldown = Config.Bind("HunterHunted", "Heat Cooldown", 25, "Set the reduction value for player heat for every cooldown interval.");
            BanditHeatCooldown = Config.Bind("HunterHunted", "Bandit Heat Cooldown", 5, "Set the reduction value for player heat from the bandits faction for every cooldown interval.");
            CoolDown_Interval = Config.Bind("HunterHunted", "Cooldown Interval", 60, "Set every how many seconds should the cooldown interval trigger.");
            Ambush_Interval = Config.Bind("HunterHunted", "Ambush Interval", 300, "Set how many seconds player can be ambushed again since last ambush.");
            Ambush_Chance = Config.Bind("HunterHunted", "Ambush Chance", 50, "Set the percentage that an ambush may occur for every cooldown interval.");
            Ambush_Despawn_Unit_Timer = Config.Bind("HunterHunted", "Ambush Despawn Timer", 300f, "Despawn the ambush squad after this many second if they are still alive.\n" +
                "Must be higher than 1.");


            EnableExperienceSystem = Config.Bind("Experience", "Enable", true, "Enable/disable the the Experience System.");
            MaxLevel = Config.Bind("Experience", "Max Level", 80, "Configure the experience system max level.");
            EXPMultiplier = Config.Bind("Experience", "Multiplier", 1.0f, "Multiply the EXP gained by player.\n" +
                "Ex.: 0.7f -> Will reduce the EXP gained by 30%\nFormula: UnitKilledLevel * EXPMultiplier");
            VBloodEXPMultiplier = Config.Bind("Experience", "VBlood Multiplier", 15f, "Multiply EXP gained from VBlood kill.\n" +
                "Formula: EXPGained * VBloodMultiplier * EXPMultiplier");
            EXPLostOnDeath = Config.Bind("Experience", "EXP Lost / Death", 0.10, "Percentage of experience the player lost for every death by NPC, no EXP is lost for PvP.\nFormula: TotalPlayerEXP - (EXPNeeded * EXPLost)");
            EXPFormula_1 = Config.Bind("Experience", "Constant", 0.2f, "Increase or decrease the required EXP to level up.\n" +
                "Formula: (level/constant)^2\n" +
                "EXP Table & Formula: https://bit.ly/3npqdJw");
            EXPGroupModifier = Config.Bind("Experience", "Group Modifier", 0.75, "Set the modifier for EXP gained for each ally(player) in vicinity.\n" +
                "Example if you have 2 ally nearby, EXPGained = ((EXPGained * Modifier)*Modifier)");
            EXPGroupMaxDistance = Config.Bind("Experience", "Ally Max Distance", 50f, "Set the maximum distance an ally(player) has to be from the player for them to share EXP with the player");

            EnableWeaponMaster = Config.Bind("Mastery", "Enable Weapon Mastery", true, "Enable/disable the weapon mastery system.");
            EnableWeaponMasterDecay = Config.Bind("Mastery", "Enable Mastery Decay", true, "Enable/disable the decay of weapon mastery when the user is offline.");
            WeaponMaxMastery = Config.Bind("Mastery", "Max Mastery Value", 100000, "Configure the maximum mastery the user can atain. (100000 is 100%)");
            MasteryCombatTick = Config.Bind("Mastery", "Mastery Value/Combat Ticks", 5, "Configure the amount of mastery gained per combat ticks. (5 -> 0.005%)");
            MasteryMaxCombatTicks = Config.Bind("Mastery", "Max Combat Ticks", 12, "Mastery will no longer increase after this many ticks is reached in combat. (1 tick = 5 seconds)");
            WeaponMasterMultiplier = Config.Bind("Mastery", "Mastery Multiplier", 1f, "Multiply the gained mastery value by this amount.");
            WeaponMastery_VBloodMultiplier = Config.Bind("Mastery", "VBlood Mastery Multiplier", 15f, "Multiply Mastery gained from VBlood kill.");
            WeaponDecayInterval = Config.Bind("Mastery", "Decay Interval", 60, "Every amount of seconds the user is offline by the configured value will translate as 1 decay tick.");
            Offline_Weapon_MasteryDecayValue = Config.Bind("Mastery", "Decay Value", 1, "Mastery will decay by this amount for every decay tick.(1 -> 0.001%)");
            WeaponMasterySpellMasteryNeedsNoneToUse = Config.Bind("Mastery", "Unarmed Only Spell Mastery Use", true, "Gain the benefits of spell mastery only when you have no weapon equipped.");
            WeaponMasterySpellMasteryNeedsNoneToLearn = Config.Bind("Mastery", "Unarmed Only Spell Mastery Learning", true, "Progress spell mastery only when you have no weapon equipped."); 
            WeaponLinearSpellMastery = Config.Bind("Mastery", "Linear Mastery CDR", false, "Changes CDR from mastery to provide a linear increase to spells able to be cast in a given time by making the cdr diminishing.");
            WeaponSpellMasteryCDRStacks = Config.Bind("Mastery", "Mastery CDR stacks", false, "Allows mastery cdr to stack with that from other sources, the reduction is multiplicative. E.G. Mist signet (10% cdr) and 100% mastery (50% cdr) will result in 55% total cdr, or 120%ish faster cooldowns.");
            DetailedMasteryInfo = Config.Bind("Mastery", "Detailed Mastery Info", false, "Shows all mastery benefits when you use the .mastery command.");

            UnarmedStats = Config.Bind("Mastery", "Unarmed Stats", " 0, 5 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            UnarmedRates = Config.Bind("Mastery", "Unarmed Rates", " 0.25, 0.01 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            SpearStats = Config.Bind("Mastery", "Spear Stats", " 0 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            SpearRates = Config.Bind("Mastery", "Spear Rates", " 0.25", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            SwordStats = Config.Bind("Mastery", "Sword Stats", " 0, 25 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            SwordRates = Config.Bind("Mastery", "Sword Rates", " 0.125, 0.125 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            ScytheStats = Config.Bind("Mastery", "Scythe Stats", " 0, 29 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            ScytheRates = Config.Bind("Mastery", "Scythe Rates", " 0.125, 0.00125 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            CrossbowStats = Config.Bind("Mastery", "Crossbow Stats", " 29 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            CrossbowRates = Config.Bind("Mastery", "Crossbow Rates", " 0.0025", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            MaceStats = Config.Bind("Mastery", "Mace Stats", " 4 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            MaceRates = Config.Bind("Mastery", "Mace Rates", " 1 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            SlasherStats = Config.Bind("Mastery", "Slasher Stats", " 29, 5 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            SlasherRates = Config.Bind("Mastery", "Slasher Rates", " 0.00125, 0.005 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            AxeStats = Config.Bind("Mastery", "Axe Stats", " 0, 4 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            AxeRates = Config.Bind("Mastery", "Axe Rates", " 0.125, 0.5 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            FishingPoleStats = Config.Bind("Mastery", "Fishing Pole Stats", " ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            FishingPoleRates = Config.Bind("Mastery", "Fishing Pole Rates", " ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");
            SpellStats = Config.Bind("Mastery", "Spell Stats", " 7 ", "The stat IDs for what this weapon should boost, should be able to handle any number of stats. See the readme for a list of stat IDs.");
            SpellRates = Config.Bind("Mastery", "Spell Rates", " 100 ", "The amount per point of mastery the stat should be boosted by. Some stats, like crit, have 1 as 100%, and CDR is % mastery to reach 50% cdr, so configure appropriately.");

            effectivenessSubSystemEnabled = Config.Bind("Mastery", "Enable Effectiveness Subsystem", false, "Enables the Effectiveness mastery subsystem, which lets you reset your mastery to gain a multiplier to the effects of the matching mastery.");
            maxEffectiveness = Config.Bind("Mastery", "Maximum Effectiveness", 10f, "The maximum mastery effectiveness where 1 is 100%.");
            growthSubSystemEnabled = Config.Bind("Mastery", "Enable Growth Subsystem", false, "Enables the growth subsystem, when you reset mastery either increases or decreases your matching mastery growth rate, depending on config.");
            minGrowth = Config.Bind("Mastery", "Minimum Growth Rate", 0.1f, "The minimum growth rate, where 1 is 100%");
            maxGrowth = Config.Bind("Mastery", "Maximum Growth Rate", 10f, "the maximum growth rate where 1 is 100%");
            growthPerEfficency = Config.Bind("Mastery", "Growth per efficency", 10f, "The amount of growth gained per point of efficency gained, if negative will reduce accordingly (gaining 100% efficency with -1 here will halve your current growth)");

            WeaponDecayInterval = Config.Bind("Mastery", "Decay Interval", 60, "Every amount of seconds the user is offline by the configured value will translate as 1 decay tick.");
            Offline_Weapon_MasteryDecayValue = Config.Bind("Mastery", "Decay Value", 1, "Mastery will decay by this amount for every decay tick.(1 -> 0.001%)");
            WeaponMasterySpellMasteryNeedsNoneToUse = Config.Bind("Mastery", "Unarmed Only Spell Mastery Use", true, "Gain the benefits of spell mastery only when you have no weapon equipped.");
            WeaponMasterySpellMasteryNeedsNoneToLearn = Config.Bind("Mastery", "Unarmed Only Spell Mastery Learning", true, "Progress spell mastery only when you have no weapon equipped.");
            WeaponLinearSpellMastery = Config.Bind("Mastery", "Linear Mastery CDR", false, "Changes CDR from mastery to provide a linear increase to spells able to be cast in a given time by making the cdr diminishing.");
            WeaponSpellMasteryCDRStacks = Config.Bind("Mastery", "Mastery CDR stacks", false, "Allows mastery cdr to stack with that from other sources, the reduction is multiplicative. E.G. Mist signet (10% cdr) and 100% mastery (50% cdr) will result in 55% total cdr, or 120%ish faster cooldowns.");
            

            EnableWorldDynamics = Config.Bind("World Dynamics", "Enable Faction Dynamics", true, "All other faction dynamics data & config is withing /RPGMods/Saves/factionstats.json file.");
            WDGrowOnKill = Config.Bind("World Dynamics", "Factions grow on kill", false, "Inverts the faction dynamic system, so that they grow stronger when killed and weaker over time.");

            if (!Directory.Exists("BepInEx/config/RPGMods")) Directory.CreateDirectory("BepInEx/config/RPGMods");
            if (!Directory.Exists("BepInEx/config/RPGMods/Saves")) Directory.CreateDirectory("BepInEx/config/RPGMods/Saves");

            if (!File.Exists("BepInEx/config/RPGMods/kits.json"))
            {
                var stream = File.Create("BepInEx/config/RPGMods/kits.json");
                stream.Dispose();
            }
        }

        public override void Load()
        {
            InitConfig();
            Logger = Log;
            harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            TaskRunner.Initialize();

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public override bool Unload()
        {
            AutoSaveSystem.SaveDatabase();
            Config.Clear();
            harmony.UnpatchSelf();

            TaskRunner.Destroy();

            return true;
        }

        public void OnGameInitialized()
        {
            Initialize();
        }

        public static void Initialize()
        {
            //-- Initialize System
            Helper.CreatePlayerCache();
            Helper.GetServerGameSettings(out Helper.SGS);
            Helper.GetServerGameManager(out Helper.SGM);
            Helper.GetUserActivityGridSystem(out Helper.UAGS);
            ProximityLoop.UpdateCache();
            PvPSystem.Interlocked.isSiegeOn = false;

            if (isInitialized) return;

            //-- Commands Related
            AutoSaveSystem.LoadDatabase();

            //-- Apply configs
            CommandHandler.Prefix = Prefix.Value;
            CommandHandler.DisabledCommands = DisabledCommands.Value;
            CommandHandler.delay_Cooldown = DelayedCommands.Value;
            Waypoint.WaypointLimit = WaypointLimit.Value;

            PermissionSystem.isVIPSystem = EnableVIPSystem.Value;
            PermissionSystem.isVIPWhitelist = EnableVIPWhitelist.Value;
            PermissionSystem.VIP_Permission = VIP_Permission.Value;

            PermissionSystem.VIP_InCombat_ResYield = VIP_InCombat_ResYield.Value;
            PermissionSystem.VIP_InCombat_DurabilityLoss = VIP_InCombat_DurabilityLoss.Value;
            PermissionSystem.VIP_InCombat_MoveSpeed = VIP_InCombat_MoveSpeed.Value;
            PermissionSystem.VIP_InCombat_GarlicResistance = VIP_InCombat_GarlicResistance.Value;
            PermissionSystem.VIP_InCombat_SilverResistance = VIP_InCombat_SilverResistance.Value;

            PermissionSystem.VIP_OutCombat_ResYield = VIP_OutCombat_ResYield.Value;
            PermissionSystem.VIP_OutCombat_DurabilityLoss = VIP_OutCombat_DurabilityLoss.Value;
            PermissionSystem.VIP_OutCombat_MoveSpeed = VIP_OutCombat_MoveSpeed.Value;
            PermissionSystem.VIP_OutCombat_GarlicResistance = VIP_OutCombat_GarlicResistance.Value;
            PermissionSystem.VIP_OutCombat_SilverResistance = VIP_OutCombat_SilverResistance.Value;

            HunterHuntedSystem.isActive = HunterHuntedEnabled.Value;
            HunterHuntedSystem.heat_cooldown = HeatCooldown.Value;
            HunterHuntedSystem.bandit_heat_cooldown = BanditHeatCooldown.Value;
            HunterHuntedSystem.cooldown_timer = CoolDown_Interval.Value;
            HunterHuntedSystem.ambush_interval = Ambush_Interval.Value;
            HunterHuntedSystem.ambush_chance = Ambush_Chance.Value;

            if (Ambush_Despawn_Unit_Timer.Value < 1) Ambush_Despawn_Unit_Timer.Value = 300f;
            HunterHuntedSystem.ambush_despawn_timer = Ambush_Despawn_Unit_Timer.Value + 0.44444f;

            PvPSystem.isPvPToggleEnabled = EnablePvPToggle.Value;
            PvPSystem.isAnnounceKills = AnnouncePvPKills.Value;

            PvPSystem.isHonorSystemEnabled = EnableHonorSystem.Value;
            PvPSystem.isHonorTitleEnabled = EnableHonorTitle.Value;
            PvPSystem.MaxHonorGainPerSpan = MaxHonorGainPerSpan.Value;
            PvPSystem.SiegeDuration = HonorSiegeDuration.Value;
            PvPSystem.PvPDuration = PvPDuration.Value;
            PvPSystem.isHonorBenefitEnabled = EnableHonorBenefit.Value;
            PvPSystem.isEnableHostileGlow = EnableHostileGlow.Value;
            PvPSystem.isUseProximityGlow = UseProximityGlow.Value;

            PvPSystem.isLadderEnabled = EnablePvPLadder.Value;
            PvPSystem.LadderLength = PvPLadderLength.Value;
            PvPSystem.isSortByHonor = HonorSortLadder.Value;
            
            PvPSystem.isPunishEnabled = EnablePvPPunish.Value;
            PvPSystem.isAnnounceGrief = EnablePvPPunishAnnounce.Value;
            PvPSystem.isExcludeOffline = ExcludeOfflineKills.Value;
            PvPSystem.PunishLevelDiff = PunishLevelDiff.Value;
            PvPSystem.PunishDuration = PunishDuration.Value;
            PvPSystem.OffenseLimit = PunishOffenseLimit.Value;
            PvPSystem.Offense_Cooldown = PunishOffenseCooldown.Value;

            SiegeSystem.isSiegeBuff = BuffSiegeGolem.Value;
            SiegeSystem.GolemPDef.Value = GolemPhysicalReduction.Value;
            SiegeSystem.GolemSDef.Value = GolemSpellReduction.Value;

            ExperienceSystem.isEXPActive = EnableExperienceSystem.Value;
            ExperienceSystem.MaxLevel = MaxLevel.Value;
            ExperienceSystem.EXPMultiplier = EXPMultiplier.Value;
            ExperienceSystem.VBloodMultiplier = VBloodEXPMultiplier.Value;
            ExperienceSystem.EXPLostOnDeath = EXPLostOnDeath.Value;
            ExperienceSystem.EXPConstant = EXPFormula_1.Value;
            ExperienceSystem.GroupModifier = EXPGroupModifier.Value;
            ExperienceSystem.GroupMaxDistance = EXPGroupMaxDistance.Value;

            WeaponMasterSystem.isMasteryEnabled = EnableWeaponMaster.Value;
            WeaponMasterSystem.isDecaySystemEnabled = EnableWeaponMasterDecay.Value;
            WeaponMasterSystem.Offline_DecayValue = Offline_Weapon_MasteryDecayValue.Value;
            WeaponMasterSystem.DecayInterval = WeaponDecayInterval.Value;
            WeaponMasterSystem.VBloodMultiplier = WeaponMastery_VBloodMultiplier.Value;
            WeaponMasterSystem.MasteryMultiplier = WeaponMasterMultiplier.Value;
            WeaponMasterSystem.MaxMastery = WeaponMaxMastery.Value;
            WeaponMasterSystem.MasteryCombatTick = MasteryCombatTick.Value;
            WeaponMasterSystem.MaxCombatTick = MasteryMaxCombatTicks.Value;
            WeaponMasterSystem.spellMasteryNeedsNoneToUse = WeaponMasterySpellMasteryNeedsNoneToUse.Value;
            WeaponMasterSystem.spellMasteryNeedsNoneToLearn = WeaponMasterySpellMasteryNeedsNoneToLearn.Value;
            WeaponMasterSystem.linearCDR = WeaponLinearSpellMastery.Value;
            WeaponMasterSystem.CDRStacks = WeaponSpellMasteryCDRStacks.Value;
            Mastery.detailedStatements = DetailedMasteryInfo.Value;


            WeaponMasterSystem.UnarmedStats = parseIntArrayConifg(UnarmedStats.Value);
            WeaponMasterSystem.UnarmedRates = parseFloatArrayConifg(UnarmedRates.Value);
            WeaponMasterSystem.SpearStats = parseIntArrayConifg(SpearStats.Value);
            WeaponMasterSystem.SpearRates = parseFloatArrayConifg(SpearRates.Value);
            WeaponMasterSystem.SwordStats = parseIntArrayConifg(SwordStats.Value);
            WeaponMasterSystem.SwordRates = parseFloatArrayConifg(SwordRates.Value);
            WeaponMasterSystem.ScytheStats = parseIntArrayConifg(ScytheStats.Value);
            WeaponMasterSystem.ScytheRates = parseFloatArrayConifg(ScytheRates.Value);
            WeaponMasterSystem.CrossbowStats = parseIntArrayConifg(CrossbowStats.Value);
            WeaponMasterSystem.CrossbowRates = parseFloatArrayConifg(CrossbowRates.Value);
            WeaponMasterSystem.MaceStats = parseIntArrayConifg(MaceStats.Value);
            WeaponMasterSystem.MaceRates = parseFloatArrayConifg(MaceRates.Value);
            WeaponMasterSystem.AxeStats = parseIntArrayConifg(AxeStats.Value);
            WeaponMasterSystem.AxeRates = parseFloatArrayConifg(AxeRates.Value);
            WeaponMasterSystem.FishingPoleStats = parseIntArrayConifg(FishingPoleStats.Value);
            WeaponMasterSystem.FishingPoleRates = parseFloatArrayConifg(FishingPoleRates.Value);
            WeaponMasterSystem.SpellStats = parseIntArrayConifg(SpellStats.Value);
            WeaponMasterSystem.SpellRates = parseFloatArrayConifg(SpellRates.Value);

            WeaponMasterSystem.masteryStats = new int[][] { WeaponMasterSystem.SpellStats, WeaponMasterSystem.UnarmedStats, WeaponMasterSystem.SpearStats, WeaponMasterSystem.SwordStats, WeaponMasterSystem.ScytheStats, WeaponMasterSystem.CrossbowStats, WeaponMasterSystem.MaceStats, WeaponMasterSystem.SlasherStats, WeaponMasterSystem.AxeStats, WeaponMasterSystem.FishingPoleStats };
            WeaponMasterSystem.masteryRates = new float[][] { WeaponMasterSystem.SpellRates, WeaponMasterSystem.UnarmedRates, WeaponMasterSystem.SpearRates, WeaponMasterSystem.SwordRates, WeaponMasterSystem.ScytheRates, WeaponMasterSystem.CrossbowRates, WeaponMasterSystem.MaceRates, WeaponMasterSystem.SlasherRates, WeaponMasterSystem.AxeRates, WeaponMasterSystem.FishingPoleRates };

        WeaponMasterSystem.effectivenessSubSystemEnabled = effectivenessSubSystemEnabled.Value;
            WeaponMasterSystem.maxEffectiveness = maxEffectiveness.Value;
            WeaponMasterSystem.growthSubSystemEnabled = effectivenessSubSystemEnabled.Value;
            WeaponMasterSystem.minGrowth = minGrowth.Value;
            WeaponMasterSystem.maxGrowth = maxGrowth.Value;
            WeaponMasterSystem.growthPerEfficency = growthPerEfficency.Value;


            WorldDynamicsSystem.isFactionDynamic = EnableWorldDynamics.Value;
            WorldDynamicsSystem.growOnKill = WDGrowOnKill.Value;

            isInitialized = true;
        }

        public static int[] parseIntArrayConifg(string data) {
            var match = Regex.Match(data, "([0-9]+)");
            List<int> list = new List<int>();
            while (match.Success) {
                try {
                    list.Add(int.Parse(match.Value));
                }
                catch {
                    Plugin.Logger.LogWarning("Error interperting integer value: " + match.ToString());
                }
                match = match.NextMatch();
            }
            int[] result = list.ToArray();
            return result;
        }
        public static float[] parseFloatArrayConifg(string data) {
            var match = Regex.Match(data, "[-+]?[0-9]*\\.?[0-9]+");
            List<float> list = new List<float>();
            while (match.Success) {
                try {
                    list.Add(float.Parse(match.Value));
                }
                catch {
                    Plugin.Logger.LogWarning("Error interperting float value: " + match.ToString());
                }
                
                match = match.NextMatch();
            }
            float[] result = list.ToArray();
            return result;
        }
    }
}

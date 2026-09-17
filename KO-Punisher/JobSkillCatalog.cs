using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Linq;

namespace KOPunisher;

public sealed record JobSkillDef(string Id, string Name, ClassType[] Jobs, string Category, string IconFile, string SourceUrl);

/// <summary>Reviewed third-party active candidates, not a complete/version-certified game database.
/// Data and icons are shipped offline; see docs/skill-catalog-sources.md for source pointers and hashes.</summary>
public static class JobSkillCatalog
{
    private static readonly JobSkillDef[] Entries =
    [
        // MAGE /data/0/nodes/2
        new("Mage_Flash", "Flash", [ClassType.Mage], "Attack", "catalog/mage-flash.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/0/nodes/3
        new("Mage_Stroke", "Stroke", [ClassType.Mage], "Attack", "catalog/warrior-stroke.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/0/nodes/4
        new("Mage_Shiver", "Shiver", [ClassType.Mage], "Attack", "catalog/mage-shiver.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/0/nodes/5
        new("Mage_SummonFriend", "Summon Friend", [ClassType.Mage], "Utility", "catalog/mage-summon_friend.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/0/nodes/6
        new("Mage_Flame", "Flame", [ClassType.Mage], "Attack", "catalog/mage-flame.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/0/nodes/7
        new("Mage_ColdWave", "Cold Wave", [ClassType.Mage], "Attack", "catalog/mage-cold_wave.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/0/nodes/8
        new("Mage_Spark", "Spark", [ClassType.Mage], "Attack", "catalog/mage-spark.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/0/nodes/9
        new("Mage_MagicBlade", "Magic Blade", [ClassType.Mage], "Attack", "catalog/mage-magic_blade.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/0/nodes/10
        new("Mage_Gate", "Gate", [ClassType.Mage], "Utility", "catalog/mage-gate.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/0/nodes/11
        new("Mage_Escape", "Escape", [ClassType.Mage], "Utility", "catalog/mage-escape.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/0
        new("Mage_Burn", "Burn", [ClassType.Mage], "Attack", "catalog/mage-burn.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/1
        new("Mage_ResistFire", "Resist Fire", [ClassType.Mage], "Buff", "catalog/mage-resist_fire.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/2
        new("Mage_Blaze", "Blaze", [ClassType.Mage], "Attack", "catalog/warrior-blaze.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/3
        new("Mage_FireBall", "Fire Ball", [ClassType.Mage], "Attack", "catalog/mage-fire_ball.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/4
        new("Mage_Ignition", "Ignition", [ClassType.Mage], "Attack", "catalog/mage-ignition.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/5
        new("Mage_EndureFire", "Endure Fire", [ClassType.Mage], "Buff", "catalog/mage-endure_fire.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/6
        new("Mage_FireSpear", "Fire Spear", [ClassType.Mage], "Attack", "catalog/mage-fire_spear.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/7
        new("Mage_FireBurst", "Fire Burst", [ClassType.Mage], "Attack", "catalog/mage-fire_burst.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/8
        new("Mage_FireBlast", "Fire Blast", [ClassType.Mage], "Attack", "catalog/mage-fire_blast.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/9
        new("Mage_HellFire", "Hell Fire", [ClassType.Mage], "Attack", "catalog/mage-hell_fire.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/10
        new("Mage_FireBlade", "Fire Blade", [ClassType.Mage], "Attack", "catalog/mage-fire_blade.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/11
        new("Mage_SpecterOfFire", "Specter of Fire", [ClassType.Mage], "Attack", "catalog/mage-specter_of_fire.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/12
        new("Mage_Inferno", "Inferno", [ClassType.Mage], "Attack", "catalog/mage-inferno.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/13
        new("Mage_ImmunityFire", "Immunity Fire", [ClassType.Mage], "Buff", "catalog/mage-immunity_fire.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/14
        new("Mage_PillarOfFire", "Pillar of Fire", [ClassType.Mage], "Attack", "catalog/mage-pillar_of_fire.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/15
        new("Mage_FireThorn", "Fire Thorn", [ClassType.Mage], "Attack", "catalog/mage-fire_thorn.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/16
        new("Mage_ManesOfFire", "Manes of Fire", [ClassType.Mage], "Attack", "catalog/mage-manes_of_fire.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/17
        new("Mage_FireImpact", "Fire Impact", [ClassType.Mage], "Attack", "catalog/mage-fire_impact.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/18
        new("Mage_Supernova", "Supernova", [ClassType.Mage], "Attack", "catalog/mage-supernova.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/19
        new("Mage_Incineration", "Incineration", [ClassType.Mage], "Attack", "catalog/mage-incineration.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/20
        new("Mage_MeteorFall", "Meteor Fall", [ClassType.Mage], "Attack", "catalog/mage-meteor_fall.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/21
        new("Mage_FireStaff", "Fire Staff", [ClassType.Mage], "Attack", "catalog/mage-fire_staff.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/22
        new("Mage_FireArmor", "Fire Armor", [ClassType.Mage], "Buff", "catalog/mage-fire_armor.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/23
        new("Mage_VampiricFire", "Vampiric Fire", [ClassType.Mage], "Attack", "catalog/mage-vampiric_fire.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/1/nodes/24
        new("Mage_Igzination", "Igzination", [ClassType.Mage], "Attack", "catalog/mage-igzination.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/0
        new("Mage_Freeze", "Freeze", [ClassType.Mage], "Attack", "catalog/mage-freeze.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/1
        new("Mage_ResistCold", "Resist Cold", [ClassType.Mage], "Buff", "catalog/mage-resist_cold.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/2
        new("Mage_Chill", "Chill", [ClassType.Mage], "Attack", "catalog/mage-chill.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/3
        new("Mage_FrozenArmor", "Frozen Armor", [ClassType.Mage], "Buff", "catalog/mage-frozen_armor.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/4
        new("Mage_IceArrow", "Ice Arrow", [ClassType.Mage], "Attack", "catalog/mage-ice_arrow.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/5
        new("Mage_Solid", "Solid", [ClassType.Mage], "Attack", "catalog/mage-solid.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/6
        new("Mage_EndureCold", "Endure Cold", [ClassType.Mage], "Buff", "catalog/mage-endure_cold.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/7
        new("Mage_IceOrb", "Ice Orb", [ClassType.Mage], "Attack", "catalog/mage-ice_orb.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/8
        new("Mage_FrozenShell", "Frozen Shell", [ClassType.Mage], "Buff", "catalog/mage-frozen_shell.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/9
        new("Mage_IceBurst", "Ice Burst", [ClassType.Mage], "Attack", "catalog/mage-ice_burst.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/10
        new("Mage_IceBlast", "Ice Blast", [ClassType.Mage], "Attack", "catalog/mage-ice_blast.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/11
        new("Mage_Frostbite", "Frostbite", [ClassType.Mage], "Attack", "catalog/mage-frostbite.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/12
        new("Mage_FrozenBlade", "Frozen Blade", [ClassType.Mage], "Attack", "catalog/mage-frozen_blade.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/13
        new("Mage_SpecterOfIce", "Specter of Ice", [ClassType.Mage], "Attack", "catalog/mage-specter_of_ice.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/14
        new("Mage_Blizzard", "Blizzard", [ClassType.Mage], "Attack", "catalog/mage-blizzard.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/15
        new("Mage_ImmunityCold", "Immunity Cold", [ClassType.Mage], "Buff", "catalog/mage-immunity_cold.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/16
        new("Mage_IceComet", "Ice Comet", [ClassType.Mage], "Attack", "catalog/mage-ice_comet.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/17
        new("Mage_IceBarrier", "Ice Barrier", [ClassType.Mage], "Buff", "catalog/mage-ice_barrier.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/18
        new("Mage_ManesOfIce", "Manes of Ice", [ClassType.Mage], "Attack", "catalog/mage-manes_of_ice.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/19
        new("Mage_IceImpact", "Ice Impact", [ClassType.Mage], "Attack", "catalog/mage-ice_impact.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/20
        new("Mage_FrostNova", "Frost Nova", [ClassType.Mage], "Attack", "catalog/mage-frost_nova.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/21
        new("Mage_Prismatic", "Prismatic", [ClassType.Mage], "Attack", "catalog/mage-prismatic.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/22
        new("Mage_IceStorm", "Ice Storm", [ClassType.Mage], "Attack", "catalog/mage-ice_storm.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/23
        new("Mage_IceStaff", "Ice Staff", [ClassType.Mage], "Attack", "catalog/mage-ice_staff.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/24
        new("Mage_IceArmor", "Ice Armor", [ClassType.Mage], "Buff", "catalog/mage-ice_armor.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/2/nodes/25
        new("Mage_FrezingDistance", "Frezing Distance", [ClassType.Mage], "Utility", "", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/0
        new("Mage_StaticHemisphere", "Static Hemisphere", [ClassType.Mage], "Attack", "catalog/mage-static_hemisphere.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/1
        new("Mage_Charge", "Charge", [ClassType.Mage], "Attack", "catalog/mage-charge.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/2
        new("Mage_ResistLightning", "Resist Lightning", [ClassType.Mage], "Buff", "catalog/mage-resist_lightning.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/3
        new("Mage_CounterSpell", "Counter Spell", [ClassType.Mage], "Attack", "catalog/mage-counter_spell.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/4
        new("Mage_Lightning", "Lightning", [ClassType.Mage], "Attack", "catalog/mage-lightning.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/5
        new("Mage_EndureLightning", "Endure Lightning", [ClassType.Mage], "Buff", "catalog/mage-endure_lightning.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/6
        new("Mage_Thunder", "Thunder", [ClassType.Mage], "Attack", "catalog/mage-thunder.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/7
        new("Mage_ThunderBurst", "Thunder Burst", [ClassType.Mage], "Attack", "catalog/mage-thunder_burst.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/8
        new("Mage_ThunderBlast", "Thunder Blast", [ClassType.Mage], "Attack", "catalog/mage-thunder_blast.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/9
        new("Mage_Discharge", "Discharge", [ClassType.Mage], "Attack", "catalog/mage-discharge.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/10
        new("Mage_ChargedBlade", "Charged Blade", [ClassType.Mage], "Attack", "catalog/mage-charged_blade.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/11
        new("Mage_SpecterOfThunder", "Specter of Thunder", [ClassType.Mage], "Attack", "catalog/mage-specter_of_thunder.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/12
        new("Mage_Thundercloud", "Thundercloud", [ClassType.Mage], "Attack", "catalog/mage-thundercloud.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/13
        new("Mage_ImmunityLightning", "Immunity Lightning", [ClassType.Mage], "Buff", "catalog/mage-immunity_lightning.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/14
        new("Mage_StaticOrb", "Static Orb", [ClassType.Mage], "Attack", "catalog/mage-static_orb.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/15
        new("Mage_StaticThorn", "Static Thorn", [ClassType.Mage], "Attack", "catalog/mage-static_thorn.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/16
        new("Mage_ManesOfThunder", "Manes of Thunder", [ClassType.Mage], "Attack", "catalog/mage-manes_of_thunder.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/17
        new("Mage_ThunderImpact", "Thunder Impact", [ClassType.Mage], "Attack", "catalog/mage-thunder_impact.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/18
        new("Mage_StaticNova", "Static Nova", [ClassType.Mage], "Attack", "catalog/mage-static_nova.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/19
        new("Mage_LightShock", "Light Shock", [ClassType.Mage], "Utility", "catalog/mage-light_shock.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/20
        new("Mage_StunCloud", "Stun Cloud", [ClassType.Mage], "Attack", "catalog/mage-stun_cloud.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/21
        new("Mage_ChainLightning", "Chain Lightning", [ClassType.Mage], "Attack", "catalog/mage-chain_lightning.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/22
        new("Mage_LightStaff", "Light Staff", [ClassType.Mage], "Attack", "catalog/mage-light_staff.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/23
        new("Mage_LightningArmor", "Lightning Armor", [ClassType.Mage], "Buff", "catalog/mage-lightning_armor.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/3/nodes/24
        new("Mage_Blink", "Blink", [ClassType.Mage], "Utility", "catalog/mage-blink.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/4/nodes/1
        new("Mage_AbsolutePower", "Absolute Power", [ClassType.Mage], "Buff", "catalog/mage-absolute_power.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/4/nodes/4
        new("Mage_ManaShield", "Mana Shield", [ClassType.Mage], "Buff", "catalog/mage-mana_shield.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/4/nodes/5
        new("Mage_InstantlyMagic", "Instantly Magic", [ClassType.Mage], "Buff", "catalog/mage-instantly_magic.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/4/nodes/6
        new("Mage_MinorResist", "Minor Resist", [ClassType.Mage], "Utility", "catalog/mage-minor_resist.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // MAGE /data/4/nodes/7
        new("Mage_GuardSummon", "Guard Summon", [ClassType.Mage], "Attack", "catalog/mage-guard_summon.png", "https://kobugda.com/api/skills?characterClass=MAGE"),
        // PRIEST /data/0/nodes/2
        new("Priest_TinyHealing", "Tiny Healing", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-tiny_healing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/3
        new("Priest_Stroke", "Stroke", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/mage-stroke.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/4
        new("Priest_LightStrike", "Light Strike", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-light_strike.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/5
        new("Priest_Strength", "Strength", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-strength.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/6
        new("Priest_LightHealing", "Light Healing", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-light_healing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/7
        new("Priest_HolyAttack", "Holy Attack", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-holy_attack.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/8
        new("Priest_ResistPoison", "Resist Poison", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-resist_poison.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/9
        new("Priest_Brightness", "Brightness", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-brightness.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/10
        new("Priest_TinyRestore", "Tiny Restore", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-tiny_restore.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/11
        new("Priest_PrayerOfCronos", "Prayer of Cronos", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-prayer_of_cronos.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/12
        new("Priest_LightMagicAttack", "Light Magic Attack", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-light_magic_attack.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/13
        new("Priest_PrayerOfGodsPower", "Prayer of God's Power", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-prayer_of_gods_power.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/14
        new("Priest_LightCounter", "Light Counter", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-light_counter.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/0/nodes/15
        new("Priest_CriticalLight", "Critical Light", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-critical_light.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/0
        new("Priest_MinorHealing", "Minor Healing", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/kurian-el_morad-minor_healing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/1
        new("Priest_LightRestore", "Light Restore", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-light_restore.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/2
        new("Priest_Healing", "Healing", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-healing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/3
        new("Priest_Collision", "Collision", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-collision.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/4
        new("Priest_Restore", "Restore", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-restore.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/5
        new("Priest_MajorHealing", "Major Healing", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/kurian-el_morad-major_healing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/6
        new("Priest_Shuddering", "Shuddering", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-shuddering.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/7
        new("Priest_MajorRestore", "Major Restore", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-major_restore.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/8
        new("Priest_CureCurse", "Cure Curse", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-cure_curse.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/9
        new("Priest_GreatHealing", "Great Healing", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-great_healing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/10
        new("Priest_Blasting", "Blasting", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-blasting.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/11
        new("Priest_GreatRestore", "Great Restore", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-great_restore.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/12
        new("Priest_CureDisease", "Cure Disease", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-cure_disease.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/13
        new("Priest_MassiveHealing", "Massive Healing", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-massive_healing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/14
        new("Priest_MassiveRestore", "Massive Restore", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-massive_restore.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/15
        new("Priest_Ruin", "Ruin", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-ruin.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/16
        new("Priest_SuperiorHealing", "Superior Healing", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-superior_healing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/17
        new("Priest_SuperiorRestore", "Superior Restore", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-superior_restore.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/18
        new("Priest_Hellish", "Hellish", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-hellish.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/19
        new("Priest_CompleteHealing", "Complete Healing", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-complete_healing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/20
        new("Priest_GroupMassiveHealing", "Group Massive Healing", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-group_massive_healing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/21
        new("Priest_GroupCompleteHealing", "Group Complete Healing", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-group_complete_healing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/22
        new("Priest_CriticalRestore", "Critical Restore", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-critical_restore.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/23
        new("Priest_PastRecovery", "Past Recovery", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-past_recovery.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/1/nodes/24
        new("Priest_PastRestore", "Past Restore", [ClassType.Priest, ClassType.BattlePriest], "Heal", "catalog/priest-past_restore.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/0
        new("Priest_InsensibilitySkin", "Insensibility Skin", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-insensibility_skin.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/1
        new("Priest_Grace", "Grace", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-grace.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/2
        new("Priest_ResistAll", "Resist All", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-resist_all.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/3
        new("Priest_Wrath", "Wrath", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-wrath.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/4
        new("Priest_InsensibilityShell", "Insensibility Shell", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-insensibility_shell.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/5
        new("Priest_Brave", "Brave", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-brave.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/6
        new("Priest_Wield", "Wield", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-wield.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/7
        new("Priest_InsensibilityArmor", "Insensibility Armor", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-insensibility_armor.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/8
        new("Priest_Strong", "Strong", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-strong.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/9
        new("Priest_BrightMind", "Bright Mind", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-bright_mind.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/10
        new("Priest_Wildness", "Wildness", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-wildness.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/11
        new("Priest_InsensibilityShield", "Insensibility Shield", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-insensibility_shield.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/12
        new("Priest_Hardness", "Hardness", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-hardness.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/13
        new("Priest_CalmMind", "Calm Mind", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-calm_mind.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/14
        new("Priest_InsensibilityBarrier", "Insensibility Barrier", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-insensibility_barrier.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/15
        new("Priest_Harsh", "Harsh", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-harsh.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/16
        new("Priest_Mightness", "Mightness", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-mightness.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/17
        new("Priest_FreshMind", "Fresh Mind", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-fresh_mind.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/18
        new("Priest_Collapse", "Collapse", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-collapse.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/19
        new("Priest_InsensibilityProtector", "Insensibility Protector", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-insensibility_protector.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/20
        new("Priest_Undying", "Undying", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-undying.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/21
        new("Priest_Heapness", "Heapness", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-heapness.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/22
        new("Priest_Greatness", "Greatness", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-greatness.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/23
        new("Priest_Massiveness", "Massiveness", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-massiveness.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/24
        new("Priest_InsensibilityPeel", "Insensibility Peel", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-insensibility_peel.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/25
        new("Priest_Imposingness", "Imposingness", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-imposingness.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/26
        new("Priest_BlessOfGod", "Bless of God", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-bless_of_god.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/27
        new("Priest_MassiveBinder", "Massive Binder", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-massive_binder.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/28
        new("Priest_RoundInsensibility", "Round Insensibility", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-round_insensibility.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/29
        new("Priest_InsensibilityGuard", "Insensibility Guard", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-insensibility_guard.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/30
        new("Priest_Superioris", "Superioris", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-superioris.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/2/nodes/31
        new("Priest_CounterCurse", "Counter Curse", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-counter_curse.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/0
        new("Priest_Gate", "Gate", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/mage-gate.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/1
        new("Priest_Malice", "Malice", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-malice.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/2
        new("Priest_ClearMana", "Clear Mana", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-clear_mana.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/3
        new("Priest_Tilt", "Tilt", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-tilt.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/4
        new("Priest_Confusion", "Confusion", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-confusion.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/5
        new("Priest_Bloody", "Bloody", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-bloody.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/6
        new("Priest_Slow", "Slow", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-slow.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/7
        new("Priest_ReverseLife", "Reverse Life", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-reverse_life.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/8
        new("Priest_Eruption", "Eruption", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-eruption.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/9
        new("Priest_SleepWing", "Sleep Wing", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-sleep_wing.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/10
        new("Priest_ResurrectionOfLove", "Resurrection of Love", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-resurrection_of_love.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/11
        new("Priest_SweepMana", "Sweep Mana", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-sweep_mana.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/12
        new("Priest_RavingEdge", "Raving Edge", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-raving_edge.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/13
        new("Priest_ResurrectionOfGrace", "Resurrection of Grace", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-resurrection_of_grace.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/14
        new("Priest_Parasite", "Parasite", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-parasite.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/15
        new("Priest_Hades", "Hades", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-hades.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/16
        new("Priest_SleepCarpet", "Sleep Carpet", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-sleep_carpet.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/17
        new("Priest_ResurrectionOfFavors", "Resurrection of Favors", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-resurrection_of_favors.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/18
        new("Priest_Torment", "Torment", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-torment.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/19
        new("Priest_Massive", "Massive", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-massive.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/20
        new("Priest_Subside", "Subside", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-subside.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/21
        new("Priest_SuperiorParasite", "Superior Parasite", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-superior_parasite.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/3/nodes/22
        new("Priest_Discountis", "Discountis", [ClassType.Priest, ClassType.BattlePriest], "Utility", "catalog/priest-discountis.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/4/nodes/1
        new("Priest_Judgment", "Judgment", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-judgment.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/4/nodes/4
        new("Priest_Helis", "Helis", [ClassType.Priest, ClassType.BattlePriest], "Attack", "catalog/priest-helis.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/4/nodes/5
        new("Priest_CurseRefraction", "Curse Refraction", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-curse_refraction.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/4/nodes/6
        new("Priest_ElysianWeb", "Elysian Web", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-elysian_web.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // PRIEST /data/4/nodes/7
        new("Priest_MinaksThorn", "Minak's Thorn", [ClassType.Priest, ClassType.BattlePriest], "Buff", "catalog/priest-minaks_thorn.png", "https://kobugda.com/api/skills?characterClass=PRIEST"),
        // ROGUE /data/0/nodes/2
        new("Stroke", "Stroke", [ClassType.Archer, ClassType.Assassin], "Attack", "catalog/rogue-stroke.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/0/nodes/3
        new("Sprint", "Sprint", [ClassType.Archer, ClassType.Assassin], "Buff", "catalog/rogue-sprint.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/0/nodes/4
        new("Archery", "Archery", [ClassType.Archer], "Attack", "catalog/rogue-archery.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/0/nodes/5
        new("Stab", "Stab", [ClassType.Assassin], "Attack", "catalog/rogue-stab.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/0/nodes/6
        new("Stab2", "Stab2", [ClassType.Assassin], "Attack", "catalog/rogue-stab2.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/0/nodes/7
        new("Archery2", "Archery2", [ClassType.Archer], "Attack", "catalog/rogue-archery2.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/0/nodes/8
        new("Swift", "Swift", [ClassType.Archer, ClassType.Assassin], "Buff", "catalog/rogue-swift.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/0/nodes/9
        new("StrengthOfWolf", "Strength of wolf", [ClassType.Archer, ClassType.Assassin], "Buff", "catalog/rogue-strength_of_wolf.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/0
        new("ThroughShot", "Through shot", [ClassType.Archer], "Attack", "catalog/rogue-through_shot.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/1
        new("FireArrow", "Fire arrow", [ClassType.Archer], "Attack", "catalog/rogue-fire_arrow.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/2
        new("PoisonArrow", "Poison arrow", [ClassType.Archer], "Attack", "catalog/rogue-poison_arrow.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/3
        new("MultipleShot", "Multiple shot", [ClassType.Archer], "Attack", "catalog/rogue-multiple_shot.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/4
        new("GuidedArrow", "Guided arrow", [ClassType.Archer], "Attack", "catalog/rogue-guided_arrow.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/5
        new("PerfectShot", "Perfect shot", [ClassType.Archer], "Attack", "catalog/rogue-perfect_shot.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/6
        new("FireShot", "Fire shot", [ClassType.Archer], "Attack", "catalog/rogue-fire_shot.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/7
        new("PoisonShot", "Poison shot", [ClassType.Archer], "Attack", "catalog/rogue-poison_shot.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/8
        new("ArcShot", "Arc shot", [ClassType.Archer], "Attack", "catalog/rogue-arc_shot.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/9
        new("ExplosiveShot", "Explosive shot", [ClassType.Archer], "Attack", "catalog/rogue-explosive_shot.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/10
        new("Viper", "Viper", [ClassType.Archer], "Attack", "catalog/rogue-viper.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/11
        new("CounterStrike", "Counter Strike", [ClassType.Archer], "Attack", "catalog/rogue-counter_strike.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/12
        new("ArrowShower", "Arrow shower", [ClassType.Archer], "Attack", "catalog/rogue-arrow_shower.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/13
        new("ShadowShot", "Shadow shot", [ClassType.Archer], "Attack", "catalog/rogue-shadow_shot.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/14
        new("ShadowHunter", "Shadow hunter", [ClassType.Archer], "Attack", "catalog/rogue-shadow_hunter.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/15
        new("IceShot", "Ice shot", [ClassType.Archer], "Attack", "catalog/rogue-ice_shot.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/16
        new("LightingShot", "Lighting shot", [ClassType.Archer], "Attack", "catalog/rogue-lighting_shot.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/17
        new("DarkPursuer", "Dark pursuer", [ClassType.Archer], "Attack", "catalog/rogue-dark_pursuer.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/18
        new("BlowArrow", "Blow Arrow", [ClassType.Archer], "Attack", "catalog/rogue-blow_arrow.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/19
        new("BlindingStrafe", "Blinding Strafe", [ClassType.Archer], "Attack", "catalog/rogue-blinding_strafe.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/1/nodes/20
        new("PowerShot", "Power Shot", [ClassType.Archer], "Attack", "catalog/rogue-power_shot.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/0
        new("Jab", "Jab", [ClassType.Assassin], "Attack", "catalog/rogue-jab.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/1
        new("BloodDrain", "Blood drain", [ClassType.Assassin], "Attack", "catalog/rogue-blood_drain.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/2
        new("Pierce", "Pierce", [ClassType.Assassin], "Attack", "catalog/rogue-pierce.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/3
        new("Shock", "Shock", [ClassType.Assassin], "Attack", "catalog/rogue-shock.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/4
        new("Illusion", "Illusion", [ClassType.Assassin], "Utility", "catalog/rogue-illusion.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/5
        new("Thrust", "Thrust", [ClassType.Assassin], "Attack", "catalog/rogue-thrust.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/6
        new("Cut", "Cut", [ClassType.Assassin], "Attack", "catalog/rogue-cut.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/7
        new("Stealth", "Stealth", [ClassType.Assassin], "Utility", "catalog/rogue-stealth.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/8
        new("VampiricTouch", "Vampiric touch", [ClassType.Assassin], "Attack", "catalog/rogue-vampiric_touch.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/9
        new("Spike", "Spike", [ClassType.Assassin], "Attack", "catalog/rogue-spike.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/10
        new("ThrowingKnife", "Throwing Knife", [ClassType.Assassin], "Attack", "catalog/rogue-throwing_knife.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/11
        new("BloodyBeast", "Bloody Beast", [ClassType.Assassin], "Attack", "catalog/rogue-bloody_beast.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/12
        new("Blinding", "Blinding", [ClassType.Assassin], "Attack", "catalog/rogue-blinding.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/13
        new("BeastHiding", "Beast Hiding", [ClassType.Assassin], "Attack", "catalog/rogue-beast_hiding.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/2/nodes/14
        new("CriticalPoint", "Critical Point", [ClassType.Assassin], "Buff", "catalog/rogue-critical_point.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/0
        new("Hide", "Hide", [ClassType.Archer, ClassType.Assassin], "Utility", "catalog/rogue-hide.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/1
        new("MinorHealing", "Minor healing", [ClassType.Archer, ClassType.Assassin], "Heal", "catalog/rogue-minor_healing.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/2
        new("Evade", "Evade", [ClassType.Archer, ClassType.Assassin], "Buff", "catalog/rogue-evade.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/3
        new("CatsEyes", "Cat's eyes", [ClassType.Archer, ClassType.Assassin], "Utility", "catalog/rogue-cats_eyes.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/4
        new("LightFeet", "Light feet", [ClassType.Archer, ClassType.Assassin], "Buff", "catalog/rogue-light_feet.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/5
        new("Safety", "Safety", [ClassType.Archer, ClassType.Assassin], "Buff", "catalog/rogue-safety.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/6
        new("LupineEyes", "Lupine Eyes", [ClassType.Archer, ClassType.Assassin], "Utility", "catalog/rogue-lupine_eyes.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/7
        new("CureCurse", "Cure curse", [ClassType.Archer, ClassType.Assassin], "Utility", "catalog/rogue-cure_curse.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/8
        new("CureDisease", "Cure disease", [ClassType.Archer, ClassType.Assassin], "Utility", "catalog/rogue-cure_disease.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/9
        new("ScaledSkin", "Scaled skin", [ClassType.Archer, ClassType.Assassin], "Buff", "catalog/rogue-scaled_skin.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/10
        new("WildAdvent", "Wild advent", [ClassType.Archer, ClassType.Assassin], "Utility", "catalog/rogue-wild_advent.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/11
        new("Concentration", "Concentration", [ClassType.Archer, ClassType.Assassin], "Buff", "catalog/rogue-concentration.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/3/nodes/12
        new("SmokeScreen", "Smoke Screen", [ClassType.Archer, ClassType.Assassin], "Utility", "catalog/rogue-smoke_screen.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/4/nodes/1
        new("MagicShield", "Magic Shield", [ClassType.Archer, ClassType.Assassin], "Buff", "catalog/rogue-magic_shield.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/4/nodes/4
        new("SourceMarking", "Source Marking", [ClassType.Archer, ClassType.Assassin], "Utility", "catalog/rogue-source_marking.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/4/nodes/5
        new("WeaponCancelation", "Weapon Cancelation", [ClassType.Archer, ClassType.Assassin], "Utility", "catalog/rogue-weapon_cancelation.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // ROGUE /data/4/nodes/6
        new("Eskrima", "Eskrima", [ClassType.Archer, ClassType.Assassin], "Attack", "catalog/rogue-eskrima.png", "https://kobugda.com/api/skills?characterClass=ROGUE"),
        // WARRIOR /data/0/nodes/2
        new("Warrior_Sprint", "Sprint", [ClassType.Warrior], "Buff", "catalog/rogue-sprint.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/0/nodes/3
        new("Warrior_Stroke", "Stroke", [ClassType.Warrior], "Attack", "catalog/priest-stroke.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/0/nodes/4
        new("Warrior_Slash", "Slash", [ClassType.Warrior], "Attack", "catalog/warrior-slash.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/0/nodes/5
        new("Warrior_Crash", "Crash", [ClassType.Warrior], "Buff", "catalog/warrior-crash.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/0/nodes/6
        new("Warrior_Defense", "Defense", [ClassType.Warrior], "Buff", "catalog/kurian-el_morad-defense.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/0/nodes/7
        new("Warrior_Piercing", "Piercing", [ClassType.Warrior], "Attack", "catalog/warrior-piercing.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/0
        new("Warrior_Hash", "Hash", [ClassType.Warrior], "Attack", "catalog/warrior-hash.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/1
        new("Warrior_Hoodwink", "Hoodwink", [ClassType.Warrior], "Attack", "catalog/warrior-hoodwink.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/2
        new("Warrior_Shear", "Shear", [ClassType.Warrior], "Attack", "catalog/warrior-shear.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/3
        new("Warrior_Pierce", "Pierce", [ClassType.Warrior], "Attack", "catalog/rogue-pierce.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/4
        new("Warrior_LegCutting", "Leg Cutting", [ClassType.Warrior], "Attack", "catalog/warrior-leg_cutting.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/5
        new("Warrior_Carving", "Carving", [ClassType.Warrior], "Attack", "catalog/warrior-carving.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/6
        new("Warrior_Sever", "Sever", [ClassType.Warrior], "Attack", "catalog/warrior-sever.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/7
        new("Warrior_Prick", "Prick", [ClassType.Warrior], "Attack", "catalog/warrior-prick.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/8
        new("Warrior_MultipleShock", "Multiple shock", [ClassType.Warrior], "Attack", "catalog/warrior-multiple_shock.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/9
        new("Warrior_Cleave", "Cleave", [ClassType.Warrior], "Attack", "catalog/warrior-cleave.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/10
        new("Warrior_Mangling", "Mangling", [ClassType.Warrior], "Attack", "catalog/warrior-mangling.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/11
        new("Warrior_Thrust", "Thrust", [ClassType.Warrior], "Attack", "catalog/rogue-thrust.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/12
        new("Warrior_SwordAura", "Sword Aura", [ClassType.Warrior], "Attack", "catalog/warrior-sword_aura.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/13
        new("Warrior_SwordDancing", "Sword Dancing", [ClassType.Warrior], "Attack", "catalog/warrior-sword_dancing.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/14
        new("Warrior_HowlingSword", "Howling Sword", [ClassType.Warrior], "Attack", "catalog/warrior-howling_sword.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/15
        new("Warrior_Blooding", "Blooding", [ClassType.Warrior], "Attack", "catalog/warrior-blooding.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/1/nodes/16
        new("Warrior_HellBlade", "Hell Blade", [ClassType.Warrior], "Attack", "catalog/warrior-hell_blade.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/2/nodes/4
        new("Warrior_Binding", "Binding", [ClassType.Warrior], "Utility", "catalog/kurian-binding.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/2/nodes/7
        new("Warrior_Provoke", "Provoke", [ClassType.Warrior], "Utility", "catalog/kurian-provoke.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/2/nodes/8
        new("Warrior_Descent", "Descent", [ClassType.Warrior], "Utility", "catalog/kurian-descent.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/2/nodes/10
        new("Warrior_Sacrifice", "Sacrifice", [ClassType.Warrior], "Heal", "catalog/kurian-sacrifice.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/2/nodes/12
        new("Warrior_WallOfIron", "Wall of Iron", [ClassType.Warrior], "Buff", "catalog/kurian-wall_of_iron.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/0
        new("Warrior_Gain", "Gain", [ClassType.Warrior], "Buff", "catalog/warrior-gain.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/1
        new("Warrior_Wink", "Wink", [ClassType.Warrior], "Attack", "catalog/warrior-wink.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/2
        new("Warrior_Rise", "Rise", [ClassType.Warrior], "Buff", "catalog/warrior-rise.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/3
        new("Warrior_PainKiller", "Pain Killer", [ClassType.Warrior], "Utility", "catalog/warrior-pain_killer.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/4
        new("Warrior_Cutting", "Cutting", [ClassType.Warrior], "Attack", "catalog/kurian-el_morad-cutting.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/5
        new("Warrior_Outrage", "Outrage", [ClassType.Warrior], "Buff", "catalog/warrior-outrage.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/6
        new("Warrior_BladeOfHate", "Blade of Hate", [ClassType.Warrior], "Attack", "catalog/warrior-blade_of_hate.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/7
        new("Warrior_Shock", "Shock", [ClassType.Warrior], "Attack", "catalog/rogue-shock.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/8
        new("Warrior_Restoration", "Restoration", [ClassType.Warrior], "Heal", "catalog/warrior-restoration.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/9
        new("Warrior_BlazeKiller", "Blaze Killer", [ClassType.Warrior], "Utility", "catalog/warrior-blaze_killer.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/10
        new("Warrior_Wind", "Wind", [ClassType.Warrior], "Attack", "catalog/warrior-wind.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/11
        new("Warrior_BladeOfHate2", "Blade of Hate 2", [ClassType.Warrior], "Attack", "catalog/warrior-blade_of_hate_2.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/12
        new("Warrior_Rage", "Rage", [ClassType.Warrior], "Attack", "catalog/warrior-rage.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/13
        new("Warrior_ReturnToLife", "Return to Life", [ClassType.Warrior], "Heal", "catalog/warrior-return_to_life.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/14
        new("Warrior_Blaze", "Blaze", [ClassType.Warrior], "Attack", "catalog/mage-blaze.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/15
        new("Warrior_Regeneration", "Regeneration", [ClassType.Warrior], "Heal", "catalog/warrior-regeneration.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/16
        new("Warrior_Hate", "Hate", [ClassType.Warrior], "Attack", "catalog/warrior-hate.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/17
        new("Warrior_Frenzy", "Frenzy", [ClassType.Warrior], "Buff", "catalog/warrior-frenzy.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/18
        new("Warrior_Killer", "Killer", [ClassType.Warrior], "Attack", "catalog/warrior-killer.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/19
        new("Warrior_Echo", "Echo", [ClassType.Warrior], "Attack", "catalog/warrior-echo.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/20
        new("Warrior_BladeOfHell", "Blade of Hell", [ClassType.Warrior], "Attack", "catalog/warrior-blade_of_hell.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/21
        new("Warrior_Berserker", "Berserker", [ClassType.Warrior], "Buff", "catalog/warrior-berserker.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/22
        new("Warrior_BerserkEcho", "Berserk Echo", [ClassType.Warrior], "Buff", "catalog/warrior-berserk_echo.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/23
        new("Warrior_HPBooster", "HP Booster", [ClassType.Warrior], "Heal", "catalog/warrior-hp_booster.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/24
        new("Warrior_BattleCry", "Battle Cry", [ClassType.Warrior], "Buff", "catalog/warrior-battle_cry.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/3/nodes/25
        new("Warrior_CryEcho", "Cry Echo", [ClassType.Warrior], "Attack", "catalog/warrior-cry_echo.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/4/nodes/1
        new("Warrior_Scream", "Scream", [ClassType.Warrior], "Attack", "catalog/warrior-scream.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/4/nodes/4
        new("Warrior_ExceedBreak", "Exceed Break", [ClassType.Warrior], "Attack", "catalog/warrior-exceed_break.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/4/nodes/5
        new("Warrior_ShockStun", "Shock Stun", [ClassType.Warrior], "Attack", "catalog/warrior-shock_stun.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
        // WARRIOR /data/4/nodes/6
        new("Warrior_InevitableMuderus", "Inevitable Muderus", [ClassType.Warrior], "Buff", "catalog/warrior-inevitable_muderus.png", "https://kobugda.com/api/skills?characterClass=WARRIOR"),
    ];

    private static readonly FrozenDictionary<string, JobSkillDef> ById =
        Entries.ToFrozenDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
    private static readonly FrozenDictionary<ClassType, JobSkillDef[]> ByJob =
        Enum.GetValues<ClassType>().ToFrozenDictionary(job => job,
            job => Entries.Where(s => s.Jobs.Contains(job)).ToArray());
    private static readonly FrozenDictionary<ClassType, IReadOnlySet<string>> JobIds =
        ByJob.ToFrozenDictionary(pair => pair.Key,
            pair => (IReadOnlySet<string>)pair.Value.Select(s => s.Id).ToFrozenSet(StringComparer.OrdinalIgnoreCase));
    private static readonly IReadOnlySet<string> EmptyIds =
        Array.Empty<string>().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    // The required record exposes an array. Return copies so callers cannot alter the source table.
    private static JobSkillDef Copy(JobSkillDef skill) => skill with { Jobs = (ClassType[])skill.Jobs.Clone() };

    public static IReadOnlyList<JobSkillDef> ForJob(ClassType job) =>
        ByJob.TryGetValue(job, out var skills)
            ? Array.AsReadOnly(skills.Select(Copy).ToArray())
            : Array.Empty<JobSkillDef>();

    public static JobSkillDef? Find(string id) =>
        id is not null && ById.TryGetValue(id, out var skill) ? Copy(skill) : null;

    public static IReadOnlySet<string> IdsForJob(ClassType job) =>
        JobIds.TryGetValue(job, out var ids) ? ids : EmptyIds;
}

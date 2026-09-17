# Job skill catalog: sources and limits

Retrieved over live HTTPS: 2026-09-12T23:02:52.204745+00:00. This is a reviewed third-party snapshot, **not all current skills**, not official NTTGame/Mgame data and not certified against any client/server version. Active status is inferred from effect descriptions (no explicit active flag); hotbar placement and weapon/level/skill-point eligibility were not tested. Entries are picker candidates, not a promise that a character can cast them. No runtime network/JSON/dependency is required.

## Coverage and selection

309 distinct records: Warrior 58, Priest 99, BattlePriest 99, Mage 91, Archer 44, Assassin 38. Priest and BattlePriest deliberately share one Priest skill pool. Rogue Basic/Search/Master are shared except dagger-only Stab/Stab2 and bow-only Archery/Archery2; Archery and Assassinate trees are separated. Shared records explain why per-job counts sum above 309.

The calculator API has 338 source rows. Exclude 21 descriptions explicitly marked `[Passive]` and 8 football event rows named Dribble/Shoot; include the remaining 309 regular active candidates. In particular Warrior Defense passives are NOT selectable. No Commander/event extras or search-excerpt-only Mage records were promoted. Category is an application classification: Attack = damage/HP absorption including damaging crowd control; Buff = positive stats/protection (including reflective armor); Heal = HP recovery/regeneration; Utility = debuff, cleanse, resurrection, teleport, non-damaging control/detection and mana conversion. Categories are not literal skill trees.

## Authoritative snapshot endpoints

These public community calculator API endpoints are the source of the static table and same-node name/icon associations. SourceUrl stores the endpoint; adjacent C# comments and the manifest below store exact JSON pointers. The JSON body hashes below identify the reviewed snapshot (a later API response may differ).

| Class | HTTPS source | SHA-256 of response body |
|---|---|---|
| MAGE | https://kobugda.com/api/skills?characterClass=MAGE | `5ea6381369ba6d915bbb64538bc965d61c383e3f4c17c97223d8ae089a02f35b` |
| PRIEST | https://kobugda.com/api/skills?characterClass=PRIEST | `3911d3f08b154594a1aa5cea668226c926f1c5dc909e86b774b8bdc0f2aa4442` |
| ROGUE | https://kobugda.com/api/skills?characterClass=ROGUE | `dada2a11089f980c427759f763b1cf5f88712830e09df53c5542cb8c5ecf4bf9` |
| WARRIOR | https://kobugda.com/api/skills?characterClass=WARRIOR | `739f0401d7a67c1d02acdc67b248f9e013233214a7181e5fadd3a6c28d0204c6` |

Calculator discovery: https://kobugda.com/skill-calculator . Earlier research also consulted https://www.kopazar.com/en/blog/knight-online-priest-skills-guide , https://www.kopazar.com/en/blog/knight-online-warrior-guide and https://www.kopazar.com/en/blog/knight-online-rogue-guide . API coverage is wider (notably Priest Basic, Warrior Berserk and Rogue Basic). Kopazar-only Nimble Wind/Quake and conflicting names Pound Insensibility, Judgement, Inevitable Murderus, Weapon Cancellation are not merged or added as duplicate variants; the consistent API snapshot is used instead. This is an explicit source selection, not proof those alternatives do not exist. API spellings such as Frezing Distance, Igzination, Inevitable Muderus and Weapon Cancelation are retained verbatim.

## IDs, risky mappings and missing data

Existing Rogue IDs are retained on exact case-insensitive name matches only (MultipleShot, ArrowShower, MinorHealing, Spike, LightingShot, etc.). Names preserve API casing. Priest IDs always use `Priest_`; Warrior/Mage use corresponding prefixes. No fuzzy or semantic migration was performed: old `BloodRain` is not equated with API `Blood drain` (`BloodDrain`), and `LupinEyes` is not silently equated with `Lupine Eyes` (`LupineEyes`). Integration should preserve/reject unsupported saved IDs explicitly rather than infer an alias. Beast Hiding is Attack because its source explicitly deals damage as well as invisibility.

These risky bindings were checked directly in fresh live response nodes:

| Endpoint class | JSON pointer | Exact source name | Literal iconPath |
|---|---|---|---|
| MAGE | `/data/0/nodes/3` | Stroke | `/assets/skills/warrior/stroke.png` |
| MAGE | `/data/2/nodes/25` | Frezing Distance | `None` |
| PRIEST | `/data/1/nodes/0` | Minor Healing | `/assets/skills/kurian/el morad/minor healing.png` |
| ROGUE | `/data/1/nodes/3` | Multiple shot | `/assets/skills/rogue/multiple shot.png` |
| ROGUE | `/data/1/nodes/12` | Arrow shower | `/assets/skills/rogue/arrow shower.png` |
| ROGUE | `/data/2/nodes/1` | Blood drain | `/assets/skills/rogue/blood drain.png` |
| ROGUE | `/data/3/nodes/1` | Minor healing | `/assets/skills/rogue/minor healing.png` |
| ROGUE | `/data/3/nodes/6` | Lupine Eyes | `/assets/skills/rogue/lupine eyes.png` |

Cross-class icon directories are literal source evidence, not guessed: Mage Stroke points at warrior/stroke.png; Priest Minor Healing points at kurian/el morad/minor healing.png. Reuse/authenticity against the official game client is unverified. `Mage_FrezingDistance` is the sole missing icon (`IconFile=""`); no stock or unrelated fallback was substituted.

## Download verification and packaging

303 unique PNG URLs back 308 records; shared URLs are downloaded once. All downloads used HTTPS GET with curl fail-on-HTTP-error; all succeeded. Literal path spaces were URL-percent-encoded (not renamed on the server). Local filenames flatten the literal source path, replace spaces with underscores, and are relative to the `images` root. Every downloaded file passed PNG magic bytes, IHDR dimensions (34×34), complete chunk CRC checks, and SHA-256 computation; the on-disk bytes were rechecked against this manifest. Python urllib initially returned 403; curl to the same live public API succeeded. An initial curl attempt rejected unencoded spaces locally; the successful downloads used correct URL encoding.

The icons remain copyrighted assets of their respective owners. Source availability is not a redistribution license; rights/permission for external distribution must be reviewed by the publisher. This catalog does not assert such permission.

Build packaging must copy `images/catalog/*` to the deployed images root; no download occurs when running the application. This task does not modify csproj/build integration. Windows runtime/image rendering remains an integration acceptance check.

## Record provenance manifest

Each pointer resolves relative to the matching endpoint above. Description is retained here to audit active-status and category decisions without making runtime JSON a dependency.

| ID | Jobs | Category | Endpoint | JSON pointer | Name | Source effect |
|---|---|---|---|---|---|---|
| `Mage_Flash` | Mage | Attack | MAGE | `/data/0/nodes/2` | Flash | Use magic shocks to attack the enemy |
| `Mage_Stroke` | Mage | Attack | MAGE | `/data/0/nodes/3` | Stroke | Inflict 70% damage |
| `Mage_Shiver` | Mage | Attack | MAGE | `/data/0/nodes/4` | Shiver | Continuous attack done for a set period of time |
| `Mage_SummonFriend` | Mage | Utility | MAGE | `/data/0/nodes/5` | Summon Friend | Retrieve a party member |
| `Mage_Flame` | Mage | Attack | MAGE | `/data/0/nodes/6` | Flame | Use magic shocks to attack the enemy |
| `Mage_ColdWave` | Mage | Attack | MAGE | `/data/0/nodes/7` | Cold Wave | An Glacier attack that slows down your enemy for a set period of time |
| `Mage_Spark` | Mage | Attack | MAGE | `/data/0/nodes/8` | Spark | Spark Lightningity on your enemy. It also has an added effect of neutralizing magic attacks |
| `Mage_MagicBlade` | Mage | Attack | MAGE | `/data/0/nodes/9` | Magic Blade | Inflict 90% damage |
| `Mage_Gate` | Mage | Utility | MAGE | `/data/0/nodes/10` | Gate | Teleport to the chosen resurrection spot |
| `Mage_Escape` | Mage | Utility | MAGE | `/data/0/nodes/11` | Escape | Teleport all party members to your resurrection spot |
| `Mage_Burn` | Mage | Attack | MAGE | `/data/1/nodes/0` | Burn | A fail-safe flame attack |
| `Mage_ResistFire` | Mage | Buff | MAGE | `/data/1/nodes/1` | Resist Fire | Increase resistance to fire by 20 |
| `Mage_Blaze` | Mage | Attack | MAGE | `/data/1/nodes/2` | Blaze | Burn your enemy with flames for a set period of time |
| `Mage_FireBall` | Mage | Attack | MAGE | `/data/1/nodes/3` | Fire Ball | Launch fireball at your enemy from far away |
| `Mage_Ignition` | Mage | Attack | MAGE | `/data/1/nodes/4` | Ignition | Spark flames on your enemy.  Additional damage is done to the enemy for a set period of time |
| `Mage_EndureFire` | Mage | Buff | MAGE | `/data/1/nodes/5` | Endure Fire | Increase resistance to fire by 50 |
| `Mage_FireSpear` | Mage | Attack | MAGE | `/data/1/nodes/6` | Fire Spear | Launch fire spears at your enemy from far away |
| `Mage_FireBurst` | Mage | Attack | MAGE | `/data/1/nodes/7` | Fire Burst | Launch an explosive burst of fire at your enemy from far away |
| `Mage_FireBlast` | Mage | Attack | MAGE | `/data/1/nodes/8` | Fire Blast | Launch a fire blast at your enemy from far away |
| `Mage_HellFire` | Mage | Attack | MAGE | `/data/1/nodes/9` | Hell Fire | Burn your enemy with the flames of hell for a set period of time |
| `Mage_FireBlade` | Mage | Attack | MAGE | `/data/1/nodes/10` | Fire Blade | Hit your enemy with a staff.  It has additional fire damage |
| `Mage_SpecterOfFire` | Mage | Attack | MAGE | `/data/1/nodes/11` | Specter of Fire | Casts a fail-safe fire spell |
| `Mage_Inferno` | Mage | Attack | MAGE | `/data/1/nodes/12` | Inferno | Retrieve fires of hell onto a specific location |
| `Mage_ImmunityFire` | Mage | Buff | MAGE | `/data/1/nodes/13` | Immunity Fire | Increase resistance to fire by 80 |
| `Mage_PillarOfFire` | Mage | Attack | MAGE | `/data/1/nodes/14` | Pillar of Fire | Burn your enemy with a great flame |
| `Mage_FireThorn` | Mage | Attack | MAGE | `/data/1/nodes/15` | Fire Thorn | Inflict flame damage and absorb HP. HP absorption applies to other players only |
| `Mage_ManesOfFire` | Mage | Attack | MAGE | `/data/1/nodes/16` | Manes of Fire | Casts a fail-safe fire spell |
| `Mage_FireImpact` | Mage | Attack | MAGE | `/data/1/nodes/17` | Fire Impact | Inflict powerful flame damage with continuous damage lasting for a short period of time |
| `Mage_Supernova` | Mage | Attack | MAGE | `/data/1/nodes/18` | Supernova | Summons a supernova that explodes with great power |
| `Mage_Incineration` | Mage | Attack | MAGE | `/data/1/nodes/19` | Incineration | Summons a flaming meteor to strike an enemy |
| `Mage_MeteorFall` | Mage | Attack | MAGE | `/data/1/nodes/20` | Meteor Fall | Summons flaming meteors to attack enemies within a certain area |
| `Mage_FireStaff` | Mage | Attack | MAGE | `/data/1/nodes/21` | Fire Staff | Hit your enemy with a staff. Additional fire damage will be given |
| `Mage_FireArmor` | Mage | Buff | MAGE | `/data/1/nodes/22` | Fire Armor | Inflicts powerful fire damage back to enemy who attacks you and continuously damage for short period of time |
| `Mage_VampiricFire` | Mage | Attack | MAGE | `/data/1/nodes/23` | Vampiric Fire | Shoot fireballs to inflict fire damage on enemy |
| `Mage_Igzination` | Mage | Attack | MAGE | `/data/1/nodes/24` | Igzination | Summon a flaming meteor to strike an enemy |
| `Mage_Freeze` | Mage | Attack | MAGE | `/data/2/nodes/0` | Freeze | A fail-safe Glacier attack |
| `Mage_ResistCold` | Mage | Buff | MAGE | `/data/2/nodes/1` | Resist Cold | Increase resistance to Glacier by 20 |
| `Mage_Chill` | Mage | Attack | MAGE | `/data/2/nodes/2` | Chill | Continuous ice damage attack for certain period of time and has a chance to slow down the enemy |
| `Mage_FrozenArmor` | Mage | Buff | MAGE | `/data/2/nodes/3` | Frozen Armor | Incrases defense ability |
| `Mage_IceArrow` | Mage | Attack | MAGE | `/data/2/nodes/4` | Ice Arrow | A Glacier magic attack that allows you to attack an opponent from far away.  It also slows down your enemy for a set period of time |
| `Mage_Solid` | Mage | Attack | MAGE | `/data/2/nodes/5` | Solid | An Glacier magic which allows you to attack an opponent from far away.  It also slows down your enemy for a set period of time |
| `Mage_EndureCold` | Mage | Buff | MAGE | `/data/2/nodes/6` | Endure Cold | Increase resistance to Glacier by 50 |
| `Mage_IceOrb` | Mage | Attack | MAGE | `/data/2/nodes/7` | Ice Orb | A Glacier magic attack that allows you to attack an opponent from far away.  It also slows down your enemy for a set period of time |
| `Mage_FrozenShell` | Mage | Buff | MAGE | `/data/2/nodes/8` | Frozen Shell | Incrases defense ability |
| `Mage_IceBurst` | Mage | Attack | MAGE | `/data/2/nodes/9` | Ice Burst | Launch an explosive ball of ice at your enemy from far away |
| `Mage_IceBlast` | Mage | Attack | MAGE | `/data/2/nodes/10` | Ice Blast | Launch Ice Blast at an an enemy far away |
| `Mage_Frostbite` | Mage | Attack | MAGE | `/data/2/nodes/11` | Frostbite | Causes frostbite to an enemy.  It has an added effect of slowing down your enemy |
| `Mage_FrozenBlade` | Mage | Attack | MAGE | `/data/2/nodes/12` | Frozen Blade | Hit your enemy with a staff. It has additional ice damage |
| `Mage_SpecterOfIce` | Mage | Attack | MAGE | `/data/2/nodes/13` | Specter of Ice | Casts a fail-safe glacier spell |
| `Mage_Blizzard` | Mage | Attack | MAGE | `/data/2/nodes/14` | Blizzard | Snowstorm attack that inflicts damage to all your enemies around you. Also temporarily slows down the enemies |
| `Mage_ImmunityCold` | Mage | Buff | MAGE | `/data/2/nodes/15` | Immunity Cold | Increase resistance to Glacier by 80 |
| `Mage_IceComet` | Mage | Attack | MAGE | `/data/2/nodes/16` | Ice Comet | Summons an ice comet to attack your enemy |
| `Mage_IceBarrier` | Mage | Buff | MAGE | `/data/2/nodes/17` | Ice Barrier | Incrases defense ability |
| `Mage_ManesOfIce` | Mage | Attack | MAGE | `/data/2/nodes/18` | Manes of Ice | Casts a fail-safe glacier spell |
| `Mage_IceImpact` | Mage | Attack | MAGE | `/data/2/nodes/19` | Ice Impact | Inflicts powerful Glacier damage with continuous damage lasting for a short period of time |
| `Mage_FrostNova` | Mage | Attack | MAGE | `/data/2/nodes/20` | Frost Nova | Explosion of ice glacier. Inflicts damage to enemies in a certain area and slows them down |
| `Mage_Prismatic` | Mage | Attack | MAGE | `/data/2/nodes/21` | Prismatic | Summon a frozen meteor to strike an enemy |
| `Mage_IceStorm` | Mage | Attack | MAGE | `/data/2/nodes/22` | Ice Storm | Summon frozen meteors to attack enemies within a certain area |
| `Mage_IceStaff` | Mage | Attack | MAGE | `/data/2/nodes/23` | Ice Staff | Inflicts on enemy powerful ice damage by staff. It's a snigle shot damage and slows down your enemy for a set period of time |
| `Mage_IceArmor` | Mage | Buff | MAGE | `/data/2/nodes/24` | Ice Armor | Inflicts powerful ice damage back to enemy who attacks you and has a chance to slows down enemy momently |
| `Mage_FrezingDistance` | Mage | Utility | MAGE | `/data/2/nodes/25` | Frezing Distance | Freeze your opponent with ice for a short period of time. The freezing success rate is proportional to the opponent's current HP level. Does not apply to monsters |
| `Mage_StaticHemisphere` | Mage | Attack | MAGE | `/data/3/nodes/0` | Static Hemisphere | Form an Lightning sphere around your enemy |
| `Mage_Charge` | Mage | Attack | MAGE | `/data/3/nodes/1` | Charge | A fail-safe Lightning attack |
| `Mage_ResistLightning` | Mage | Buff | MAGE | `/data/3/nodes/2` | Resist Lightning | Increase resistance to Lightning by 20 |
| `Mage_CounterSpell` | Mage | Attack | MAGE | `/data/3/nodes/3` | Counter Spell | A Lightning magic attak that neutralizes enemy's magic attack |
| `Mage_Lightning` | Mage | Attack | MAGE | `/data/3/nodes/4` | Lightning | An Lightning attack that allows you to attack an enemy from far away. It has an added effect of neutralizing enemy's magic attack |
| `Mage_EndureLightning` | Mage | Buff | MAGE | `/data/3/nodes/5` | Endure Lightning | Increase resistance to Lightning by 50 |
| `Mage_Thunder` | Mage | Attack | MAGE | `/data/3/nodes/6` | Thunder | A Lightning magic that allows you to attack an enemy from far away |
| `Mage_ThunderBurst` | Mage | Attack | MAGE | `/data/3/nodes/7` | Thunder Burst | Launch a explosive lightning sphere to attack an enemy from far away |
| `Mage_ThunderBlast` | Mage | Attack | MAGE | `/data/3/nodes/8` | Thunder Blast | Launch a thunder blast to attack an enemy from far away |
| `Mage_Discharge` | Mage | Attack | MAGE | `/data/3/nodes/9` | Discharge | Shock your enemy with Lightningity for a certain period of time. It has an added effect of neutralizing enemy's magic attack |
| `Mage_ChargedBlade` | Mage | Attack | MAGE | `/data/3/nodes/10` | Charged Blade | Hit your enemy with a staff. Additional lightning damage will be given |
| `Mage_SpecterOfThunder` | Mage | Attack | MAGE | `/data/3/nodes/11` | Specter of Thunder | Casts a fail-safe lightning spell |
| `Mage_Thundercloud` | Mage | Attack | MAGE | `/data/3/nodes/12` | Thundercloud | Summon a thundercloud |
| `Mage_ImmunityLightning` | Mage | Buff | MAGE | `/data/3/nodes/13` | Immunity Lightning | Increase resistance to Lightning by 80 |
| `Mage_StaticOrb` | Mage | Attack | MAGE | `/data/3/nodes/14` | Static Orb | Launch a charged Lightning ball |
| `Mage_StaticThorn` | Mage | Attack | MAGE | `/data/3/nodes/15` | Static Thorn | Inflict lightning damage and absorb HP. HP absorption applies to other players only |
| `Mage_ManesOfThunder` | Mage | Attack | MAGE | `/data/3/nodes/16` | Manes of Thunder | Casts a fail-safe lightning spell |
| `Mage_ThunderImpact` | Mage | Attack | MAGE | `/data/3/nodes/17` | Thunder Impact | Attack enemy with strong elecrtic shock and Continuous attack done for a set period of time |
| `Mage_StaticNova` | Mage | Attack | MAGE | `/data/3/nodes/18` | Static Nova | A great Lightning explosion |
| `Mage_LightShock` | Mage | Utility | MAGE | `/data/3/nodes/19` | Light Shock | Temporarily blind an enemy with a bolt of light. Does not apply to monsters |
| `Mage_StunCloud` | Mage | Attack | MAGE | `/data/3/nodes/20` | Stun Cloud | Summon an electrically charged meteor to strike an enemy |
| `Mage_ChainLightning` | Mage | Attack | MAGE | `/data/3/nodes/21` | Chain Lightning | Retrieve electrically charged meteors to attack enemies standing within a certain area |
| `Mage_LightStaff` | Mage | Attack | MAGE | `/data/3/nodes/22` | Light Staff | A powerful lightning attack with a staff. Also stuns enemy momently |
| `Mage_LightningArmor` | Mage | Buff | MAGE | `/data/3/nodes/23` | Lightning Armor | Inflicts powerful lightning damage back to enemy who attacks you and has a chance to stuns enemy |
| `Mage_Blink` | Mage | Utility | MAGE | `/data/3/nodes/24` | Blink | Able to teleport forward 20 meters |
| `Mage_AbsolutePower` | Mage | Buff | MAGE | `/data/4/nodes/1` | Absolute Power | Temporarily increase your magic attack power by 30% |
| `Mage_ManaShield` | Mage | Buff | MAGE | `/data/4/nodes/4` | Mana Shield | Absorbs 15% of damage received through mana. However, the damage absorbed from mana will be 4 times greater |
| `Mage_InstantlyMagic` | Mage | Buff | MAGE | `/data/4/nodes/5` | Instantly Magic | Able to cast a spell once without any refresh time |
| `Mage_MinorResist` | Mage | Utility | MAGE | `/data/4/nodes/6` | Minor Resist | Decrease all enemy's resistance by 20% in a certain area. Does not apply to monsters |
| `Mage_GuardSummon` | Mage | Attack | MAGE | `/data/4/nodes/7` | Guard Summon | A guard (dragon) summons from the earth and stages continuous attacks for a certain period of time |
| `Priest_TinyHealing` | Priest, BattlePriest | Heal | PRIEST | `/data/0/nodes/2` | Tiny Healing | Heal 15 HP |
| `Priest_Stroke` | Priest, BattlePriest | Attack | PRIEST | `/data/0/nodes/3` | Stroke | Inflict 70% damage |
| `Priest_LightStrike` | Priest, BattlePriest | Attack | PRIEST | `/data/0/nodes/4` | Light Strike | Light magic attack |
| `Priest_Strength` | Priest, BattlePriest | Buff | PRIEST | `/data/0/nodes/5` | Strength | Increase strength by 15 |
| `Priest_LightHealing` | Priest, BattlePriest | Heal | PRIEST | `/data/0/nodes/6` | Light Healing | Heal 30 HP |
| `Priest_HolyAttack` | Priest, BattlePriest | Attack | PRIEST | `/data/0/nodes/7` | Holy Attack | Inflict 90% damage |
| `Priest_ResistPoison` | Priest, BattlePriest | Buff | PRIEST | `/data/0/nodes/8` | Resist Poison | Increase resistance to poison by 20 |
| `Priest_Brightness` | Priest, BattlePriest | Attack | PRIEST | `/data/0/nodes/9` | Brightness | Light magic attack |
| `Priest_TinyRestore` | Priest, BattlePriest | Heal | PRIEST | `/data/0/nodes/10` | Tiny Restore | Heal 50 HP for 20 seconds |
| `Priest_PrayerOfCronos` | Priest, BattlePriest | Buff | PRIEST | `/data/0/nodes/11` | Prayer of Cronos | Supernatural power increases your attack power by 20% |
| `Priest_LightMagicAttack` | Priest, BattlePriest | Attack | PRIEST | `/data/0/nodes/12` | Light Magic Attack | Light magic attack |
| `Priest_PrayerOfGodsPower` | Priest, BattlePriest | Buff | PRIEST | `/data/0/nodes/13` | Prayer of God's Power | Supernatural power increases your attack power by 50% |
| `Priest_LightCounter` | Priest, BattlePriest | Attack | PRIEST | `/data/0/nodes/14` | Light Counter | Light magic attack |
| `Priest_CriticalLight` | Priest, BattlePriest | Attack | PRIEST | `/data/0/nodes/15` | Critical Light | Light magic attack |
| `Priest_MinorHealing` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/0` | Minor Healing | Heal 60 HP |
| `Priest_LightRestore` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/1` | Light Restore | Heal 100 HP over 20 seconds |
| `Priest_Healing` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/2` | Healing | Heal 240 HP |
| `Priest_Collision` | Priest, BattlePriest | Attack | PRIEST | `/data/1/nodes/3` | Collision | A fail-safe attack that inflicts 120% damage |
| `Priest_Restore` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/4` | Restore | Heal 400 HP over 20 seconds |
| `Priest_MajorHealing` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/5` | Major Healing | Heal 360 HP |
| `Priest_Shuddering` | Priest, BattlePriest | Attack | PRIEST | `/data/1/nodes/6` | Shuddering | A fail-safe attack that inflicts 150% damage |
| `Priest_MajorRestore` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/7` | Major Restore | Heal 600 HP over 20 seconds |
| `Priest_CureCurse` | Priest, BattlePriest | Utility | PRIEST | `/data/1/nodes/8` | Cure Curse | Neutralizes any resistance decreasing spells |
| `Priest_GreatHealing` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/9` | Great Healing | Heal 720 HP |
| `Priest_Blasting` | Priest, BattlePriest | Buff | PRIEST | `/data/1/nodes/10` | Blasting | Increase strength by 30 |
| `Priest_GreatRestore` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/11` | Great Restore | Heal 800 HP for 20 seconds |
| `Priest_CureDisease` | Priest, BattlePriest | Utility | PRIEST | `/data/1/nodes/12` | Cure Disease | Neutralizes any spells that decrease your HP |
| `Priest_MassiveHealing` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/13` | Massive Healing | Heal 960 HP |
| `Priest_MassiveRestore` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/14` | Massive Restore | Heal 1500 HP over 20 seconds |
| `Priest_Ruin` | Priest, BattlePriest | Attack | PRIEST | `/data/1/nodes/15` | Ruin | A fail-safe attack that inflicts 150% damage and an additional 100 damage |
| `Priest_SuperiorHealing` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/16` | Superior Healing | Heal 1920 HP |
| `Priest_SuperiorRestore` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/17` | Superior Restore | Heal 2500 HP over 20 seconds |
| `Priest_Hellish` | Priest, BattlePriest | Attack | PRIEST | `/data/1/nodes/18` | Hellish | A fail-safe attack that inflicts 200% damage and an additional 50 damage |
| `Priest_CompleteHealing` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/19` | Complete Healing | Commpletely heal the HP of a friend |
| `Priest_GroupMassiveHealing` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/20` | Group Massive Healing | Heal all the members of your party with 960 HP |
| `Priest_GroupCompleteHealing` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/21` | Group Complete Healing | Completely heal all the members of your party |
| `Priest_CriticalRestore` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/22` | Critical Restore | Heals 3000HP over 20 seconds for party members in a certain area |
| `Priest_PastRecovery` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/23` | Past Recovery | Heals a friend for 2500 HP and heals for an additional 3000 HP over 20 seconds |
| `Priest_PastRestore` | Priest, BattlePriest | Heal | PRIEST | `/data/1/nodes/24` | Past Restore | Heals 6000 HP over 20 seconds for all party members in a certain area |
| `Priest_InsensibilitySkin` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/0` | Insensibility Skin | Increase AC by 20 |
| `Priest_Grace` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/1` | Grace | Increase a party member's HP by 60 |
| `Priest_ResistAll` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/2` | Resist All | Increase magic, curse, and poison resistance by 20 |
| `Priest_Wrath` | Priest, BattlePriest | Attack | PRIEST | `/data/2/nodes/3` | Wrath | A fail-safe attack that inflicts 120% damage |
| `Priest_InsensibilityShell` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/4` | Insensibility Shell | Increase AC by 40 |
| `Priest_Brave` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/5` | Brave | Increase a party member's HP by 240 |
| `Priest_Wield` | Priest, BattlePriest | Attack | PRIEST | `/data/2/nodes/6` | Wield | A fail-safe attack that inflicts 150% damage |
| `Priest_InsensibilityArmor` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/7` | Insensibility Armor | Increase AC by 80 |
| `Priest_Strong` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/8` | Strong | Increase a party member's HP by 360 |
| `Priest_BrightMind` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/9` | Bright Mind | Increase magic, curse, and poison resistance by 40 |
| `Priest_Wildness` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/10` | Wildness | Increase strength by 30 |
| `Priest_InsensibilityShield` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/11` | Insensibility Shield | Increase AC by 120 |
| `Priest_Hardness` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/12` | Hardness | Increase a party member's HP by 720 |
| `Priest_CalmMind` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/13` | Calm Mind | Increase magic, curse, and poison resistance by 60 |
| `Priest_InsensibilityBarrier` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/14` | Insensibility Barrier | Increase AC by 160 |
| `Priest_Harsh` | Priest, BattlePriest | Attack | PRIEST | `/data/2/nodes/15` | Harsh | A fail-safe attack that inflicts 150% damage and an additional 100 damage |
| `Priest_Mightness` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/16` | Mightness | Increase a party member's HP by 960 |
| `Priest_FreshMind` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/17` | Fresh Mind | Increase magic, curse, and poison resistance by 80 |
| `Priest_Collapse` | Priest, BattlePriest | Attack | PRIEST | `/data/2/nodes/18` | Collapse | A fail-safe attack that inflicts 200% damage and an additional 50 damage |
| `Priest_InsensibilityProtector` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/19` | Insensibility Protector | Increase AC by 200 |
| `Priest_Undying` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/20` | Undying | Increase the max HP limit of a party member by 60% |
| `Priest_Heapness` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/21` | Heapness | Increase the max HP limit of a party member by 1200 |
| `Priest_Greatness` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/22` | Greatness | Increase the max HP limit of all the party member by 1200 |
| `Priest_Massiveness` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/23` | Massiveness | Increase the max HP limit of a party member by 1500 |
| `Priest_InsensibilityPeel` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/24` | Insensibility Peel | Increase the AC by 300 |
| `Priest_Imposingness` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/25` | Imposingness | Increase the max HP limit of a party member by 2000 |
| `Priest_BlessOfGod` | Priest, BattlePriest | Utility | PRIEST | `/data/2/nodes/26` | Bless of God | Recovers user ability that was lost for all the party members |
| `Priest_MassiveBinder` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/27` | Massive Binder | Increase the max HP limit of all the party member by 2000 |
| `Priest_RoundInsensibility` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/28` | Round Insensibility | Increase the defense of all party members by 300 |
| `Priest_InsensibilityGuard` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/29` | Insensibility Guard | Increase the defense of a party member by 350 |
| `Priest_Superioris` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/30` | Superioris | Increase the max HP limit of the party member by 2500 |
| `Priest_CounterCurse` | Priest, BattlePriest | Buff | PRIEST | `/data/2/nodes/31` | Counter Curse | Blocks all curses for 10 seconds |
| `Priest_Gate` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/0` | Gate | Teleport to the chosen resurrection spot |
| `Priest_Malice` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/1` | Malice | Decrease your enemy's defense ability by 25% |
| `Priest_ClearMana` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/2` | Clear Mana | Decrease your enemy's Mana by 480 |
| `Priest_Tilt` | Priest, BattlePriest | Attack | PRIEST | `/data/3/nodes/3` | Tilt | A fail-safe attack that inflicts 120% damage |
| `Priest_Confusion` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/4` | Confusion | Decrease your enemy's Magic Power by 30 |
| `Priest_Bloody` | Priest, BattlePriest | Attack | PRIEST | `/data/3/nodes/5` | Bloody | A fail-safe attack that inflicts 150% damage |
| `Priest_Slow` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/6` | Slow | Decrease your enemy's attack speed by 20% |
| `Priest_ReverseLife` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/7` | Reverse Life | Neutralizes enemy's HP bonus spells |
| `Priest_Eruption` | Priest, BattlePriest | Buff | PRIEST | `/data/3/nodes/8` | Eruption | Increase strength by 30 |
| `Priest_SleepWing` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/9` | Sleep Wing | Puts a monster to sleep for 20 seconds |
| `Priest_ResurrectionOfLove` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/10` | Resurrection of Love | Resurrect regaining 60% of the original experience lost. Requires 4 Stones of life at dead person's inventory. |
| `Priest_SweepMana` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/11` | Sweep Mana | Decrease your enemy's Mana by 960 |
| `Priest_RavingEdge` | Priest, BattlePriest | Attack | PRIEST | `/data/3/nodes/12` | Raving Edge | A fail-safe attack that inflicts 150% damage and an additional 100 damage |
| `Priest_ResurrectionOfGrace` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/13` | Resurrection of Grace | Resurrect regaining 70% of the original experience lost. Requires 10 Stones of life at dead person's inventory. |
| `Priest_Parasite` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/14` | Parasite | Decrease your enemy's max HP by 20% |
| `Priest_Hades` | Priest, BattlePriest | Attack | PRIEST | `/data/3/nodes/15` | Hades | A fail-safe attack that inflicts 200% damage and an additional 50 damage |
| `Priest_SleepCarpet` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/16` | Sleep Carpet | Puts all the monsters in an area to sleep for 20 seconds |
| `Priest_ResurrectionOfFavors` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/17` | Resurrection of Favors | Resurrect regaining 80% of the original experience lost. Requires 30 Stones of life at dead person's inventory. |
| `Priest_Torment` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/18` | Torment | Decrease all enemy's defense by 30% in a certain area |
| `Priest_Massive` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/19` | Massive | Decrease your enemy's attack power by 20% |
| `Priest_Subside` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/20` | Subside | Decrease 20% of attack ability of enemies if they're standing within a certain area |
| `Priest_SuperiorParasite` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/21` | Superior Parasite | Decrease an enemy's HP by 30%. Does not apply to unique monsters |
| `Priest_Discountis` | Priest, BattlePriest | Utility | PRIEST | `/data/3/nodes/22` | Discountis | Decrease an enemy's Mana by 3840 in a certain area |
| `Priest_Judgment` | Priest, BattlePriest | Attack | PRIEST | `/data/4/nodes/1` | Judgment | A fail-safe attack that inflicts 200% damage and an additional 150 damage |
| `Priest_Helis` | Priest, BattlePriest | Attack | PRIEST | `/data/4/nodes/4` | Helis | A fail-safe attack that inflicts 250% damage and an additional 400 damage that disregards defense |
| `Priest_CurseRefraction` | Priest, BattlePriest | Buff | PRIEST | `/data/4/nodes/5` | Curse Refraction | Blocks all curses for 10 seconds and has a chance to reflect the curse back onto the caster |
| `Priest_ElysianWeb` | Priest, BattlePriest | Buff | PRIEST | `/data/4/nodes/6` | Elysian Web | Increases magic resistance for 20 seconds to reduce any damage received from magic spells by 30% |
| `Priest_MinaksThorn` | Priest, BattlePriest | Buff | PRIEST | `/data/4/nodes/7` | Minak's Thorn | Applies an aurora that mirrors 20% of direct attack damages to all party members. The Thorn effect continues for the cast time, mirroring 20% of damage out of 100% damage inflicted on you |
| `Stroke` | Archer, Assassin | Attack | ROGUE | `/data/0/nodes/2` | Stroke | Inflict 70% damage |
| `Sprint` | Archer, Assassin | Buff | ROGUE | `/data/0/nodes/3` | Sprint | Temporarily increase your running speed |
| `Archery` | Archer | Attack | ROGUE | `/data/0/nodes/4` | Archery | Use a bow to shoot arrows |
| `Stab` | Assassin | Attack | ROGUE | `/data/0/nodes/5` | Stab | Use a dagger to inflict 150% damage |
| `Stab2` | Assassin | Attack | ROGUE | `/data/0/nodes/6` | Stab2 | Use a dagger to inflict 150% damage and 50 additional damage with no chance of failure |
| `Archery2` | Archer | Attack | ROGUE | `/data/0/nodes/7` | Archery2 | Shoot arrows. Inflict 120% damage |
| `Swift` | Archer, Assassin | Buff | ROGUE | `/data/0/nodes/8` | Swift | Increase the running speed of a friend |
| `StrengthOfWolf` | Archer, Assassin | Buff | ROGUE | `/data/0/nodes/9` | Strength of wolf | Increase the attack powers of your party members |
| `ThroughShot` | Archer | Attack | ROGUE | `/data/1/nodes/0` | Through shot | Inflict 150% damage |
| `FireArrow` | Archer | Attack | ROGUE | `/data/1/nodes/1` | Fire arrow | Shoot flame arrows |
| `PoisonArrow` | Archer | Attack | ROGUE | `/data/1/nodes/2` | Poison arrow | Shoot poison arrows |
| `MultipleShot` | Archer | Attack | ROGUE | `/data/1/nodes/3` | Multiple shot | Shoot 3 arrows simultaneously |
| `GuidedArrow` | Archer | Attack | ROGUE | `/data/1/nodes/4` | Guided arrow | 100% accurate arrow |
| `PerfectShot` | Archer | Attack | ROGUE | `/data/1/nodes/5` | Perfect shot | Inflict 200% damage |
| `FireShot` | Archer | Attack | ROGUE | `/data/1/nodes/6` | Fire shot | Arrow attack that inflicts additional flame damage |
| `PoisonShot` | Archer | Attack | ROGUE | `/data/1/nodes/7` | Poison shot | Arrow attack that inflicts additional poison damage |
| `ArcShot` | Archer | Attack | ROGUE | `/data/1/nodes/8` | Arc shot | Inflict 250% damage |
| `ExplosiveShot` | Archer | Attack | ROGUE | `/data/1/nodes/9` | Explosive shot | The strongest flame arrow |
| `Viper` | Archer | Attack | ROGUE | `/data/1/nodes/10` | Viper | The strongest poison arrow |
| `CounterStrike` | Archer | Attack | ROGUE | `/data/1/nodes/11` | Counter Strike | Shoots arrow inflicts critical damage |
| `ArrowShower` | Archer | Attack | ROGUE | `/data/1/nodes/12` | Arrow shower | Shoot 5 arrows simulataneously |
| `ShadowShot` | Archer | Attack | ROGUE | `/data/1/nodes/13` | Shadow shot | 100% accurate arrow with 200% damage |
| `ShadowHunter` | Archer | Attack | ROGUE | `/data/1/nodes/14` | Shadow hunter | 100% accurate arrow with 300% damage |
| `IceShot` | Archer | Attack | ROGUE | `/data/1/nodes/15` | Ice shot | Inflicts 300% damage and has a chance to slow down the enemy |
| `LightingShot` | Archer | Attack | ROGUE | `/data/1/nodes/16` | Lighting shot | Inflicts 300% damage and stuns for 3 seconds |
| `DarkPursuer` | Archer | Attack | ROGUE | `/data/1/nodes/17` | Dark pursuer | 100% accurate arrow that inflicts 350% damage |
| `BlowArrow` | Archer | Attack | ROGUE | `/data/1/nodes/18` | Blow Arrow | While moving, stab an enemy with an arrow to inflict 200% damage |
| `BlindingStrafe` | Archer | Attack | ROGUE | `/data/1/nodes/19` | Blinding Strafe | Shoots an arrow that inflicts 400% damage and blinds the enemy. Does not apply to monsters |
| `PowerShot` | Archer | Attack | ROGUE | `/data/1/nodes/20` | Power Shot | Shoots an arrow that inflicts a great amount of damage |
| `Jab` | Assassin | Attack | ROGUE | `/data/2/nodes/0` | Jab | A stabbing technique that disregards opponent's defense |
| `BloodDrain` | Assassin | Attack | ROGUE | `/data/2/nodes/1` | Blood drain | Absorbs 5% of the enemy's HP. Can be used only once every minute |
| `Pierce` | Assassin | Attack | ROGUE | `/data/2/nodes/2` | Pierce | Inflict 100% damage with no chance of failure |
| `Shock` | Assassin | Attack | ROGUE | `/data/2/nodes/3` | Shock | A stabbing technique that disregards opponent's defense |
| `Illusion` | Assassin | Utility | ROGUE | `/data/2/nodes/4` | Illusion | Temporarily decreases enemy's attack accuracy |
| `Thrust` | Assassin | Attack | ROGUE | `/data/2/nodes/5` | Thrust | Inflict 200% damage with no chance of failure |
| `Cut` | Assassin | Attack | ROGUE | `/data/2/nodes/6` | Cut | A stabbing technique that disregards opponent's defense |
| `Stealth` | Assassin | Utility | ROGUE | `/data/2/nodes/7` | Stealth | Stay invisible for 80 seconds without attacking |
| `VampiricTouch` | Assassin | Attack | ROGUE | `/data/2/nodes/8` | Vampiric touch | Absorb 10% of the enemy's HP. Can be used only once every minute |
| `Spike` | Assassin | Attack | ROGUE | `/data/2/nodes/9` | Spike | Inflict 600% damage |
| `ThrowingKnife` | Assassin | Attack | ROGUE | `/data/2/nodes/10` | Throwing Knife | Throws a knife with 300% damage at enemies within 20 meter range |
| `BloodyBeast` | Assassin | Attack | ROGUE | `/data/2/nodes/11` | Bloody Beast | A stabbing technique that disregards opponent's defense |
| `Blinding` | Assassin | Attack | ROGUE | `/data/2/nodes/12` | Blinding | Inflicts 500% damage and blinds the enemy for 2 seconds. Does not apply to monsters |
| `BeastHiding` | Assassin | Attack | ROGUE | `/data/2/nodes/13` | Beast Hiding | Inflicts 300% damage and turns you invisible for 3 seconds. Does not apply to monsters |
| `CriticalPoint` | Assassin | Buff | ROGUE | `/data/2/nodes/14` | Critical Point | Execute critical attacks. Does not apply to monsters |
| `Hide` | Archer, Assassin | Utility | ROGUE | `/data/3/nodes/0` | Hide | Stay invisible for 40 seconds without moving |
| `MinorHealing` | Archer, Assassin | Heal | ROGUE | `/data/3/nodes/1` | Minor healing | Heal 60 HP |
| `Evade` | Archer, Assassin | Buff | ROGUE | `/data/3/nodes/2` | Evade | Increases Defense by 200 |
| `CatsEyes` | Archer, Assassin | Utility | ROGUE | `/data/3/nodes/3` | Cat's eyes | Allows you to see an invisible enemy for 50 seconds |
| `LightFeet` | Archer, Assassin | Buff | ROGUE | `/data/3/nodes/4` | Light feet | Temporarily increases your running speed by 2 |
| `Safety` | Archer, Assassin | Buff | ROGUE | `/data/3/nodes/5` | Safety | Increases Defense by 400 |
| `LupineEyes` | Archer, Assassin | Utility | ROGUE | `/data/3/nodes/6` | Lupine Eyes | Allows you and your party members to see an invisible enemy for 50 seconds |
| `CureCurse` | Archer, Assassin | Utility | ROGUE | `/data/3/nodes/7` | Cure curse | Neutralizes any resistance decreasing spells |
| `CureDisease` | Archer, Assassin | Utility | ROGUE | `/data/3/nodes/8` | Cure disease | Neutralizes any spells that decrease your HP |
| `ScaledSkin` | Archer, Assassin | Buff | ROGUE | `/data/3/nodes/9` | Scaled skin | Increases Defense by 800 |
| `WildAdvent` | Archer, Assassin | Utility | ROGUE | `/data/3/nodes/10` | Wild advent | Teleports to an enemy that is within a certain distance from you |
| `Concentration` | Archer, Assassin | Buff | ROGUE | `/data/3/nodes/11` | Concentration | Will not fail an attack for 15 seconds |
| `SmokeScreen` | Archer, Assassin | Utility | ROGUE | `/data/3/nodes/12` | Smoke Screen | Detonates a smoke screen in an area, disable targeting ability for everyone around. Does not apply to monsters |
| `MagicShield` | Archer, Assassin | Buff | ROGUE | `/data/4/nodes/1` | Magic Shield | Temporarily increases all your resistant point |
| `SourceMarking` | Archer, Assassin | Utility | ROGUE | `/data/4/nodes/4` | Source Marking | Marks an enemy, disabling them from becoming invisible. Does not apply to monsters |
| `WeaponCancelation` | Archer, Assassin | Utility | ROGUE | `/data/4/nodes/5` | Weapon Cancelation | Chance to cancel out the Weapon held by an enemy. Does not apply to monsters |
| `Eskrima` | Archer, Assassin | Attack | ROGUE | `/data/4/nodes/6` | Eskrima | Inflicts attacks as well as a bleeding curse on the enemy. Decreases 10% Dagger and Bow Defense of the enemy under the bleeding curse buff |
| `Warrior_Sprint` | Warrior | Buff | WARRIOR | `/data/0/nodes/2` | Sprint | Temporarily increase your running speed |
| `Warrior_Stroke` | Warrior | Attack | WARRIOR | `/data/0/nodes/3` | Stroke | Inflict 70% damage |
| `Warrior_Slash` | Warrior | Attack | WARRIOR | `/data/0/nodes/4` | Slash | Inflict 120% damage |
| `Warrior_Crash` | Warrior | Buff | WARRIOR | `/data/0/nodes/5` | Crash | Increases your chances of succeeding an attack by 1.5 times |
| `Warrior_Defense` | Warrior | Buff | WARRIOR | `/data/0/nodes/6` | Defense | Temporarily increases your defense |
| `Warrior_Piercing` | Warrior | Attack | WARRIOR | `/data/0/nodes/7` | Piercing | Inflict 120% damage and 50 additional damage |
| `Warrior_Hash` | Warrior | Attack | WARRIOR | `/data/1/nodes/0` | Hash | Adds an additional 30 damage regardless of defense |
| `Warrior_Hoodwink` | Warrior | Attack | WARRIOR | `/data/1/nodes/1` | Hoodwink | Inflict 150% damage |
| `Warrior_Shear` | Warrior | Attack | WARRIOR | `/data/1/nodes/2` | Shear | Adds an additional 50 damage regardless of defense |
| `Warrior_Pierce` | Warrior | Attack | WARRIOR | `/data/1/nodes/3` | Pierce | Inflict 100% damage with no chance of failure |
| `Warrior_LegCutting` | Warrior | Attack | WARRIOR | `/data/1/nodes/4` | Leg Cutting | An attack that slows down your enemy |
| `Warrior_Carving` | Warrior | Attack | WARRIOR | `/data/1/nodes/5` | Carving | Inflict 200% damage |
| `Warrior_Sever` | Warrior | Attack | WARRIOR | `/data/1/nodes/6` | Sever | Adds an additional 100 damage regardless of defense |
| `Warrior_Prick` | Warrior | Attack | WARRIOR | `/data/1/nodes/7` | Prick | Inflict 150% damage with no chance of failure |
| `Warrior_MultipleShock` | Warrior | Attack | WARRIOR | `/data/1/nodes/8` | Multiple shock | Inflict 150% damage and 50 additional damage |
| `Warrior_Cleave` | Warrior | Attack | WARRIOR | `/data/1/nodes/9` | Cleave | Inflict 250% damage |
| `Warrior_Mangling` | Warrior | Attack | WARRIOR | `/data/1/nodes/10` | Mangling | Adds an additional 150 damage regardless of defense |
| `Warrior_Thrust` | Warrior | Attack | WARRIOR | `/data/1/nodes/11` | Thrust | Inflict 200% damage with no chance of failure |
| `Warrior_SwordAura` | Warrior | Attack | WARRIOR | `/data/1/nodes/12` | Sword Aura | Inflict 250% damage and 100 additional damage with no chance of failure |
| `Warrior_SwordDancing` | Warrior | Attack | WARRIOR | `/data/1/nodes/13` | Sword Dancing | Inflict 250% damage and 150 additional damage with no chance of failure |
| `Warrior_HowlingSword` | Warrior | Attack | WARRIOR | `/data/1/nodes/14` | Howling Sword | Inflict 300% damage with no chance of failure and 200 additional damage |
| `Warrior_Blooding` | Warrior | Attack | WARRIOR | `/data/1/nodes/15` | Blooding | Inflict 200% damage, 150 additional damage, and deals 1000 damage over 20 seconds with no chance of failure |
| `Warrior_HellBlade` | Warrior | Attack | WARRIOR | `/data/1/nodes/16` | Hell Blade | Inflict 300% damage and 350 additional damage with no change of failure |
| `Warrior_Binding` | Warrior | Utility | WARRIOR | `/data/2/nodes/4` | Binding | Trick monster into attacking itself |
| `Warrior_Provoke` | Warrior | Utility | WARRIOR | `/data/2/nodes/7` | Provoke | Provoke monsters in a certain area to concentrate their attacks on you |
| `Warrior_Descent` | Warrior | Utility | WARRIOR | `/data/2/nodes/8` | Descent | Teleport to the location of a chosen party member |
| `Warrior_Sacrifice` | Warrior | Heal | WARRIOR | `/data/2/nodes/10` | Sacrifice | Sacrifice your own self to fill the HP one party member |
| `Warrior_WallOfIron` | Warrior | Buff | WARRIOR | `/data/2/nodes/12` | Wall of Iron | Defense is tripled for 10 seconds. However, reduces running speed by 50% during this time |
| `Warrior_Gain` | Warrior | Buff | WARRIOR | `/data/3/nodes/0` | Gain | Increase Strength by 15 |
| `Warrior_Wink` | Warrior | Attack | WARRIOR | `/data/3/nodes/1` | Wink | Gives 150% damage to target |
| `Warrior_Rise` | Warrior | Buff | WARRIOR | `/data/3/nodes/2` | Rise | Increase HP by 10 |
| `Warrior_PainKiller` | Warrior | Utility | WARRIOR | `/data/3/nodes/3` | Pain Killer | Exchange 100 HP for 200 MP |
| `Warrior_Cutting` | Warrior | Attack | WARRIOR | `/data/3/nodes/4` | Cutting | Gives 100% damage to target and it will not fail |
| `Warrior_Outrage` | Warrior | Buff | WARRIOR | `/data/3/nodes/5` | Outrage | Absorbs damage from MP for 10 seconds |
| `Warrior_BladeOfHate` | Warrior | Attack | WARRIOR | `/data/3/nodes/6` | Blade of Hate | Use of a sword to attack an enemy that is far away |
| `Warrior_Shock` | Warrior | Attack | WARRIOR | `/data/3/nodes/7` | Shock | A stabbing technique that disregards opponent's defense |
| `Warrior_Restoration` | Warrior | Heal | WARRIOR | `/data/3/nodes/8` | Restoration | Temporarily increase rate of HP regeneration |
| `Warrior_BlazeKiller` | Warrior | Utility | WARRIOR | `/data/3/nodes/9` | Blaze Killer | Exchanges 200 HP for 400 Stamina |
| `Warrior_Wind` | Warrior | Attack | WARRIOR | `/data/3/nodes/10` | Wind | Gives 100 additional damage that is not affected by defense |
| `Warrior_BladeOfHate2` | Warrior | Attack | WARRIOR | `/data/3/nodes/11` | Blade of Hate 2 | Use of a sword to attack an enemy that is far away |
| `Warrior_Rage` | Warrior | Attack | WARRIOR | `/data/3/nodes/12` | Rage | Gives 150% damage to target, and it does not fail |
| `Warrior_ReturnToLife` | Warrior | Heal | WARRIOR | `/data/3/nodes/13` | Return to Life | Exchange 500 MP for 250 HP |
| `Warrior_Blaze` | Warrior | Attack | WARRIOR | `/data/3/nodes/14` | Blaze | Burn your enemy with flames for a set period of time |
| `Warrior_Regeneration` | Warrior | Heal | WARRIOR | `/data/3/nodes/15` | Regeneration | Temporarily increase rate of HP regeneration. Rate of HP regeneration is faster than Restoration |
| `Warrior_Hate` | Warrior | Attack | WARRIOR | `/data/3/nodes/16` | Hate | Gives additional 150 damage that is not affected by defense |
| `Warrior_Frenzy` | Warrior | Buff | WARRIOR | `/data/3/nodes/17` | Frenzy | Absorbs damage from MP for 20 seconds |
| `Warrior_Killer` | Warrior | Attack | WARRIOR | `/data/3/nodes/18` | Killer | Gives 200% damage to target and it will not fail |
| `Warrior_Echo` | Warrior | Attack | WARRIOR | `/data/3/nodes/19` | Echo | Gives 200% damage and additional 100 damage. This skill does not fail |
| `Warrior_BladeOfHell` | Warrior | Attack | WARRIOR | `/data/3/nodes/20` | Blade of Hell | A sword type skill that damages nearby monsters by 250% |
| `Warrior_Berserker` | Warrior | Buff | WARRIOR | `/data/3/nodes/21` | Berserker | Increase attack speed by 20% for 20 seconds. However, defense is decreased by 300 during this time |
| `Warrior_BerserkEcho` | Warrior | Buff | WARRIOR | `/data/3/nodes/22` | Berserk Echo | Temporarily increase attack speed by 40% |
| `Warrior_HPBooster` | Warrior | Heal | WARRIOR | `/data/3/nodes/23` | HP Booster | Recovers HP while standing as if you were sitting for a set period of time |
| `Warrior_BattleCry` | Warrior | Buff | WARRIOR | `/data/3/nodes/24` | Battle Cry | Encourages party members located within a range of 30 of the Warrior during the casting, and increases all stats (except for STR) by 15 points for all party members including yourself |
| `Warrior_CryEcho` | Warrior | Attack | WARRIOR | `/data/3/nodes/25` | Cry Echo | Applicable only when used with Battle Cry. An unfailing attack skill that inflicts 300% damage and 200 additional damage |
| `Warrior_Scream` | Warrior | Attack | WARRIOR | `/data/4/nodes/1` | Scream | A fail-safe attack that inflicts 250% Damage and 150 Damage. It also freezes the enemy temporarily |
| `Warrior_ExceedBreak` | Warrior | Attack | WARRIOR | `/data/4/nodes/4` | Exceed Break | Inflict 200% damage with no chance of failure and a chance to reduce the oppoenent's armor and weapon's durability by 1000 |
| `Warrior_ShockStun` | Warrior | Attack | WARRIOR | `/data/4/nodes/5` | Shock Stun | Inflicts 200% damage and stuns the enemy for 3 seconds |
| `Warrior_InevitableMuderus` | Warrior | Buff | WARRIOR | `/data/4/nodes/6` | Inevitable Muderus | Increases range by 1 meter for 10 seconds |

## Explicit exclusions

| Endpoint | JSON pointer | Name |
|---|---|---|
| MAGE | `/data/0/nodes/0` | Dribble |
| MAGE | `/data/0/nodes/1` | Shoot |
| MAGE | `/data/4/nodes/0` | Bright Dew |
| MAGE | `/data/4/nodes/2` | Absoluteness |
| MAGE | `/data/4/nodes/3` | Matchless |
| PRIEST | `/data/0/nodes/0` | Dribble |
| PRIEST | `/data/0/nodes/1` | Shoot |
| PRIEST | `/data/4/nodes/0` | Daring |
| PRIEST | `/data/4/nodes/2` | Absoluteness |
| PRIEST | `/data/4/nodes/3` | Matchless |
| ROGUE | `/data/0/nodes/0` | Dribble |
| ROGUE | `/data/0/nodes/1` | Shoot |
| ROGUE | `/data/4/nodes/0` | Valor |
| ROGUE | `/data/4/nodes/2` | Absoluteness |
| ROGUE | `/data/4/nodes/3` | Matchless |
| WARRIOR | `/data/0/nodes/0` | Dribble |
| WARRIOR | `/data/0/nodes/1` | Shoot |
| WARRIOR | `/data/2/nodes/0` | Hinder |
| WARRIOR | `/data/2/nodes/1` | Resist |
| WARRIOR | `/data/2/nodes/2` | Arrest |
| WARRIOR | `/data/2/nodes/3` | Endure |
| WARRIOR | `/data/2/nodes/5` | Bulwark |
| WARRIOR | `/data/2/nodes/6` | Immunity |
| WARRIOR | `/data/2/nodes/9` | Evading |
| WARRIOR | `/data/2/nodes/11` | Iron Skin |
| WARRIOR | `/data/2/nodes/13` | Iron Body |
| WARRIOR | `/data/4/nodes/0` | Boldness |
| WARRIOR | `/data/4/nodes/2` | Absoluteness |
| WARRIOR | `/data/4/nodes/3` | Matchless |

## Icon manifest

| File relative to images | HTTPS URL | Bytes | Dimensions | SHA-256 |
|---|---|---|---|---|
| `catalog/kurian-binding.png` | https://kobugda.com/assets/skills/kurian/binding.png | 3165 | 34×34 | `d0434eb615af41e389ec622231f9721d5dbe58cb7d3303f7294f28108d3e6d11` |
| `catalog/kurian-descent.png` | https://kobugda.com/assets/skills/kurian/descent.png | 3549 | 34×34 | `ee473c6c4225692fb419f3d65677e3185b3fd19f061fecc3a1d6570e25e01433` |
| `catalog/kurian-el_morad-cutting.png` | https://kobugda.com/assets/skills/kurian/el%20morad/cutting.png | 3440 | 34×34 | `80203bca66c762a8ab2ddd775ff14139a02017115adfb472f594e4fe2f337a80` |
| `catalog/kurian-el_morad-defense.png` | https://kobugda.com/assets/skills/kurian/el%20morad/defense.png | 3361 | 34×34 | `105fb3f120b104dba4d9cbd64df4eb7b55cfbeb21b6a7d799db29861f977004d` |
| `catalog/kurian-el_morad-major_healing.png` | https://kobugda.com/assets/skills/kurian/el%20morad/major%20healing.png | 3423 | 34×34 | `38cebd0677e6a7524202648efe64eadcad67c267cd8ac5e0918e287207b4d693` |
| `catalog/kurian-el_morad-minor_healing.png` | https://kobugda.com/assets/skills/kurian/el%20morad/minor%20healing.png | 3366 | 34×34 | `2b1c773630f47c16b46c1a994fbbf3162ef9781ad872514cc34182206773f19d` |
| `catalog/kurian-provoke.png` | https://kobugda.com/assets/skills/kurian/provoke.png | 3185 | 34×34 | `0b0790648a4c164355921951d6f2b896e7edf342b3ffc0a323ce9a2aaf4fbfee` |
| `catalog/kurian-sacrifice.png` | https://kobugda.com/assets/skills/kurian/sacrifice.png | 3217 | 34×34 | `99d5b19c609829a00f75c38ec88e32b9f97bc328743981c87f4eb7be9a720107` |
| `catalog/kurian-wall_of_iron.png` | https://kobugda.com/assets/skills/kurian/wall%20of%20iron.png | 3321 | 34×34 | `a2072e1fd84db693018d7bb3b87207ba25d0856c8b86651abbae799750e3c75f` |
| `catalog/mage-absolute_power.png` | https://kobugda.com/assets/skills/mage/absolute%20power.png | 3387 | 34×34 | `357713538f72fc7b6be624842c8243c1d05ad404db17c8423c64bb3fab63689a` |
| `catalog/mage-blaze.png` | https://kobugda.com/assets/skills/mage/blaze.png | 2118 | 34×34 | `69dfe7b1773a739e89731911a5d07fe0504579c1e77a8de85f4be848f26d8d82` |
| `catalog/mage-blink.png` | https://kobugda.com/assets/skills/mage/blink.png | 1384 | 34×34 | `ce6a41e6f86f3ccb3fdb10dc3e38aeb2c786ce10974ce2de8fea9e820ff2456c` |
| `catalog/mage-blizzard.png` | https://kobugda.com/assets/skills/mage/blizzard.png | 2306 | 34×34 | `89b60f32662e8f0e292e6cdbe995ec025f71789812968e34a1407b7dc77af842` |
| `catalog/mage-burn.png` | https://kobugda.com/assets/skills/mage/burn.png | 1811 | 34×34 | `1872a0c5d87e36176114bd934e510aaaf89580904f973e5e9a07ba85269f02a2` |
| `catalog/mage-chain_lightning.png` | https://kobugda.com/assets/skills/mage/chain%20lightning.png | 1577 | 34×34 | `a7829b4d8ee34a5b3bd3e54579b2ce9e437918d06c643401f3c5675a8fd8f878` |
| `catalog/mage-charge.png` | https://kobugda.com/assets/skills/mage/charge.png | 1859 | 34×34 | `6a4b5199b038652318c26b6a226a13dce435d1a50a41771e858ce5565e29e1ef` |
| `catalog/mage-charged_blade.png` | https://kobugda.com/assets/skills/mage/charged%20blade.png | 1627 | 34×34 | `5f8ae092c765add98285b22089919c68036d32b97b0a448beb1d23d3b020ea0c` |
| `catalog/mage-chill.png` | https://kobugda.com/assets/skills/mage/chill.png | 2374 | 34×34 | `e476afb19678ce3178e8fec44251ae099982e58a305647d2a2558d8ec428f27b` |
| `catalog/mage-cold_wave.png` | https://kobugda.com/assets/skills/mage/cold%20wave.png | 1973 | 34×34 | `751191ceb84e9287922a06346291f1057dc73a70a6aa6d6d40f4362bc970bbad` |
| `catalog/mage-counter_spell.png` | https://kobugda.com/assets/skills/mage/counter%20spell.png | 2364 | 34×34 | `3a591dc4402b288e5bcd1274b9a0532a751091a83d47d21761c2787ade546cf7` |
| `catalog/mage-discharge.png` | https://kobugda.com/assets/skills/mage/discharge.png | 2105 | 34×34 | `d4c18574e35d8d32d6b671c1dbd523a8961cafd20cdfe18b1d6a1d44ca3e0e9d` |
| `catalog/mage-endure_cold.png` | https://kobugda.com/assets/skills/mage/endure%20cold.png | 1901 | 34×34 | `5f4a002ff4b8be24b8e35d8b8f13ba53dcacf61aa3ce6c6d6b3a4eb31e7e57dd` |
| `catalog/mage-endure_fire.png` | https://kobugda.com/assets/skills/mage/endure%20fire.png | 1795 | 34×34 | `f9141b524539727e38fadfe2c7912d189a7998f7c2fbf722aec598b7104c5369` |
| `catalog/mage-endure_lightning.png` | https://kobugda.com/assets/skills/mage/endure%20lightning.png | 1789 | 34×34 | `74b30a44b6e512bfa1c90137e79735cfa33f07db7318cb0dcf6ce295454a258c` |
| `catalog/mage-escape.png` | https://kobugda.com/assets/skills/mage/escape.png | 2399 | 34×34 | `325102859381baed943df38543dca5eacabd7af987f2b07ade7e1bd34d6cceb4` |
| `catalog/mage-fire_armor.png` | https://kobugda.com/assets/skills/mage/fire%20armor.png | 1665 | 34×34 | `2009d1afefe45fbfc906cb83addc44118c67d56dd9157e0de6aba043bbe711a8` |
| `catalog/mage-fire_ball.png` | https://kobugda.com/assets/skills/mage/fire%20ball.png | 1887 | 34×34 | `e2ac87c9a99745a202857ebae2b1c58d80a55f406a5ce928bcba017a1b96c7d3` |
| `catalog/mage-fire_blade.png` | https://kobugda.com/assets/skills/mage/fire%20blade.png | 1757 | 34×34 | `7ab9c6d01de036fc8d3dcc91435097c1e96d06d0e0905a70b9b0851d771c337b` |
| `catalog/mage-fire_blast.png` | https://kobugda.com/assets/skills/mage/fire%20blast.png | 2275 | 34×34 | `d67e32c811afacf55d495ec8672ca6ae3183e5d0ba3de8d567f780dc18ecd6ee` |
| `catalog/mage-fire_burst.png` | https://kobugda.com/assets/skills/mage/fire%20burst.png | 2137 | 34×34 | `0aab38485c0892721c63020670866e51b49a753c4bafbe35f3dab32eaf897c1a` |
| `catalog/mage-fire_impact.png` | https://kobugda.com/assets/skills/mage/fire%20impact.png | 1566 | 34×34 | `a1c0ebb7b83e96aa98f67e14fb83d932d4a6aff62a10f9ef3c632b77c50eb3ce` |
| `catalog/mage-fire_spear.png` | https://kobugda.com/assets/skills/mage/fire%20spear.png | 1655 | 34×34 | `bdf4546e4901cf62eccba823ada11d750eba406d6569adde8cc75b54f10752e2` |
| `catalog/mage-fire_staff.png` | https://kobugda.com/assets/skills/mage/fire%20staff.png | 1317 | 34×34 | `5170c881e875bab278f3d434e7ec4c77fa755f4703116c2eb6d1dc9fd2be97cf` |
| `catalog/mage-fire_thorn.png` | https://kobugda.com/assets/skills/mage/fire%20thorn.png | 1430 | 34×34 | `f3fcac42bc9237646e6bf18f93fe5e7268d774b48c65c1d214474825985d162e` |
| `catalog/mage-flame.png` | https://kobugda.com/assets/skills/mage/flame.png | 1939 | 34×34 | `67e41bb8cfa7012e96b8492804b0488d0c5a0a138bd315009f1c8bf1503606ed` |
| `catalog/mage-flash.png` | https://kobugda.com/assets/skills/mage/flash.png | 2044 | 34×34 | `31a82c48cde61161baf23829318888ac7610febd5e25200c862cac34fe0e7776` |
| `catalog/mage-freeze.png` | https://kobugda.com/assets/skills/mage/freeze.png | 2395 | 34×34 | `cae6ad030b963682a4a9f36d7bcf5ab0b95401bed193d75a5b23582a1fe9724e` |
| `catalog/mage-frost_nova.png` | https://kobugda.com/assets/skills/mage/frost%20nova.png | 2397 | 34×34 | `e5e1b03d5357db21cb8706274d71a82b2a62d019c8916d9c0305fd700f9a8965` |
| `catalog/mage-frostbite.png` | https://kobugda.com/assets/skills/mage/frostbite.png | 2363 | 34×34 | `06a8bc1f2d6f70e08a285aed43c3ce86e291d9897d7dbb6d820b1891d71faebc` |
| `catalog/mage-frozen_armor.png` | https://kobugda.com/assets/skills/mage/frozen%20armor.png | 2090 | 34×34 | `d19998c15e42902d1802d1c0611ad673d765b622d2ae31a129970ab1d270b40b` |
| `catalog/mage-frozen_blade.png` | https://kobugda.com/assets/skills/mage/frozen%20blade.png | 2300 | 34×34 | `284e2dbf6465b811d7c33de0d0fa23d3dcd74b1bdf1ebaf0552a75cbfeda1fd4` |
| `catalog/mage-frozen_shell.png` | https://kobugda.com/assets/skills/mage/frozen%20shell.png | 2135 | 34×34 | `e1011e977375729ed54c1d2f9da8ad871412b103f59b12648c238bb5373574ac` |
| `catalog/mage-gate.png` | https://kobugda.com/assets/skills/mage/gate.png | 2326 | 34×34 | `9fc1d5995a043566e6525121755146e22add0bc21bd7d2f00a476453907e408d` |
| `catalog/mage-guard_summon.png` | https://kobugda.com/assets/skills/mage/guard%20summon.png | 3479 | 34×34 | `3f80832a3e04d9d5904a4120c1f9ac703b628367eeb52906fb761a21ecb1a570` |
| `catalog/mage-hell_fire.png` | https://kobugda.com/assets/skills/mage/hell%20fire.png | 2234 | 34×34 | `9c06d9c6af5d726ff526bfee6fdc78c80d9815cd27b7bb98723982a0c359bfc0` |
| `catalog/mage-ice_armor.png` | https://kobugda.com/assets/skills/mage/ice%20armor.png | 1729 | 34×34 | `b8a41aa4087432085a28586e49271e93f3f6ce5dde7b8c1a7641d559475771e7` |
| `catalog/mage-ice_arrow.png` | https://kobugda.com/assets/skills/mage/ice%20arrow.png | 1964 | 34×34 | `9a9ba300fee94c879536e12e6d598ad0abc3cb312202a7cfb72677d8b0b04688` |
| `catalog/mage-ice_barrier.png` | https://kobugda.com/assets/skills/mage/ice%20barrier.png | 2139 | 34×34 | `8661acc3d8f452836473a8804272d564a99e78108a4e645f6bf9761e2df1b831` |
| `catalog/mage-ice_blast.png` | https://kobugda.com/assets/skills/mage/ice%20blast.png | 2367 | 34×34 | `cac4bd5d61fcff9a524734aac4ab40ee456679bfa42293266948766f3b42698a` |
| `catalog/mage-ice_burst.png` | https://kobugda.com/assets/skills/mage/ice%20burst.png | 2358 | 34×34 | `9b6406c928bad2fdd4130e07a2e487b3ccf1baff34c0ca52a801ae493cdf6f61` |
| `catalog/mage-ice_comet.png` | https://kobugda.com/assets/skills/mage/ice%20comet.png | 2129 | 34×34 | `069c0bbc96dbce97dce711b706931122706609def4c0ed4cbeceb948693c3ff7` |
| `catalog/mage-ice_impact.png` | https://kobugda.com/assets/skills/mage/ice%20impact.png | 1844 | 34×34 | `8352a1501a7466b63d472efa37c3840b187a76e90bb9c4d8484ddb392f497d1e` |
| `catalog/mage-ice_orb.png` | https://kobugda.com/assets/skills/mage/ice%20orb.png | 2441 | 34×34 | `36c4fe99f1936320094c3f68bc2c7fa1d1a40913dc2882d967942cdd12fec358` |
| `catalog/mage-ice_staff.png` | https://kobugda.com/assets/skills/mage/ice%20staff.png | 1682 | 34×34 | `edf3b036206fa8e83bba375aad4764ffb6c28059e52fb7bd3119dd61781a6549` |
| `catalog/mage-ice_storm.png` | https://kobugda.com/assets/skills/mage/ice%20storm.png | 1740 | 34×34 | `994ac4f9c64ccb1aabd5423145e953577552f5dd532028bc24eb8c2576dcd51d` |
| `catalog/mage-ignition.png` | https://kobugda.com/assets/skills/mage/ignition.png | 1988 | 34×34 | `7ad38bb90ba7b998c5115e0451131073a23b94ef4dc3436edac807dc8cd98b8a` |
| `catalog/mage-igzination.png` | https://kobugda.com/assets/skills/mage/igzination.png | 1419 | 34×34 | `6b63efe6eed56e833ccb09c21e0dead81af80b42bdbe82b6b72cef983bf1f3e3` |
| `catalog/mage-immunity_cold.png` | https://kobugda.com/assets/skills/mage/immunity%20cold.png | 1702 | 34×34 | `734f5e5b9e9dbc434457ba49bb5d74714b7873e6c3740d6f0b441c87c3dbc650` |
| `catalog/mage-immunity_fire.png` | https://kobugda.com/assets/skills/mage/immunity%20fire.png | 1872 | 34×34 | `4e004364c3efd1a1e7296c1f3566211a5f7f34fd908091cc4cd655265992a600` |
| `catalog/mage-immunity_lightning.png` | https://kobugda.com/assets/skills/mage/immunity%20lightning.png | 1863 | 34×34 | `0a3359e3af0da8bc3c8071228751ebc5e31342e76edb8cc1fdb656bd14c8e926` |
| `catalog/mage-incineration.png` | https://kobugda.com/assets/skills/mage/incineration.png | 1442 | 34×34 | `1d9f937d7a7eb158272d8b12e4323fedc73c90f954c05d635963250963af80cd` |
| `catalog/mage-inferno.png` | https://kobugda.com/assets/skills/mage/inferno.png | 2135 | 34×34 | `373d09ccd2ea4d6365c22b554d9ece2c24f26522658654055e02430a59dac564` |
| `catalog/mage-instantly_magic.png` | https://kobugda.com/assets/skills/mage/instantly%20magic.png | 3470 | 34×34 | `58d995c229e1c273ce592578dc32a7ea6d6b104d5c3673b90c47703eab3654da` |
| `catalog/mage-light_shock.png` | https://kobugda.com/assets/skills/mage/light%20shock.png | 1500 | 34×34 | `854f64ed14059dc150320f50e642eb9b8ccfc6aa9f8a697c06c9652e7927b445` |
| `catalog/mage-light_staff.png` | https://kobugda.com/assets/skills/mage/light%20staff.png | 1773 | 34×34 | `1fe7fcfe008369972efa7a2d8e4d69c052d9dfa41ec7a2a41a8b8ebdb2138c6b` |
| `catalog/mage-lightning_armor.png` | https://kobugda.com/assets/skills/mage/lightning%20armor.png | 1597 | 34×34 | `e4d6a02b3bd42acc6612d3be11c2d19b5e6bce4ca5f25bb45de039deb030ad18` |
| `catalog/mage-lightning.png` | https://kobugda.com/assets/skills/mage/lightning.png | 2064 | 34×34 | `da85c8b5e7a94ae4f42ed4b3a793e93424489a567ebcafeaae29c194e67e4c72` |
| `catalog/mage-magic_blade.png` | https://kobugda.com/assets/skills/mage/magic%20blade.png | 2186 | 34×34 | `ef5fad1579d3daeb8d2d8b91867e2ed470d5c67d38ce1f2363469a24b751c33e` |
| `catalog/mage-mana_shield.png` | https://kobugda.com/assets/skills/mage/mana%20shield.png | 3226 | 34×34 | `1b6d84c8b4fde586de3505c56da807ed753b8dfafa79c56f001ac7246c35b5df` |
| `catalog/mage-manes_of_fire.png` | https://kobugda.com/assets/skills/mage/manes%20of%20fire.png | 1880 | 34×34 | `fb53eeb68ff17775625926504bbcb773a0bbdac281f512017b1ef451e749dfee` |
| `catalog/mage-manes_of_ice.png` | https://kobugda.com/assets/skills/mage/manes%20of%20ice.png | 2013 | 34×34 | `8d57ef96a81ada55b8e87b51b64e36ecf97950d4ba5ab4ebdae1993fd046008d` |
| `catalog/mage-manes_of_thunder.png` | https://kobugda.com/assets/skills/mage/manes%20of%20thunder.png | 1806 | 34×34 | `b46c3fe628f313ef14d6741f0d6ed34c1bdd7db52a661e3aaa60deb2f2c03568` |
| `catalog/mage-meteor_fall.png` | https://kobugda.com/assets/skills/mage/meteor%20fall.png | 1624 | 34×34 | `6eb5eabaf41a2510b46a20ba9b177ff2116d57cbe4de4655d3083a161d1b179a` |
| `catalog/mage-minor_resist.png` | https://kobugda.com/assets/skills/mage/minor%20resist.png | 3004 | 34×34 | `1e5da511cc26475ac2589c2c68d28ae0fcaabf5cac1b46fc30f29bd3bc71e3c5` |
| `catalog/mage-pillar_of_fire.png` | https://kobugda.com/assets/skills/mage/pillar%20of%20fire.png | 2090 | 34×34 | `6d5ddf8b762ad4d84ad122d5fbd121b3bdbc03fc3ca7e2a91d98872e7a2296d2` |
| `catalog/mage-prismatic.png` | https://kobugda.com/assets/skills/mage/prismatic.png | 1612 | 34×34 | `879e63749490600b2445a6c8309fa8887d2960d3bcce5c070d3279d3009b45dc` |
| `catalog/mage-resist_cold.png` | https://kobugda.com/assets/skills/mage/resist%20cold.png | 1788 | 34×34 | `e297956736d00261890e4b2b51a072beb6448dbfff81c13d7022f8d42d912bc4` |
| `catalog/mage-resist_fire.png` | https://kobugda.com/assets/skills/mage/resist%20fire.png | 1340 | 34×34 | `6590e6f5ce82a41e3a044823bbb2fbfd46db42b04f0256cb46caa7d89b5641af` |
| `catalog/mage-resist_lightning.png` | https://kobugda.com/assets/skills/mage/resist%20lightning.png | 1666 | 34×34 | `b79b8a18050cd8dad218821065e14acf89bd9bda4cef9734eef05f1eb7d4bc65` |
| `catalog/mage-shiver.png` | https://kobugda.com/assets/skills/mage/shiver.png | 1850 | 34×34 | `87a2782b77ac60b7a9415e9fe944aef1b1eb329e1b415bdbab5fa0cc23926f08` |
| `catalog/mage-solid.png` | https://kobugda.com/assets/skills/mage/solid.png | 1838 | 34×34 | `7283b3d33faefbd5d45cce8c3b9d0fd205f5850b17888b3a3bec64f5df551c57` |
| `catalog/mage-spark.png` | https://kobugda.com/assets/skills/mage/spark.png | 2080 | 34×34 | `306999a6ab15f7a4fc9fb3d1b25a6bebe1863680e0b08486656b10b896214bf0` |
| `catalog/mage-specter_of_fire.png` | https://kobugda.com/assets/skills/mage/specter%20of%20fire.png | 1608 | 34×34 | `a474efd44c35d41bff024691ce7b417f56daf8a6a0a5727e2f3211b43593fd7b` |
| `catalog/mage-specter_of_ice.png` | https://kobugda.com/assets/skills/mage/specter%20of%20ice.png | 1796 | 34×34 | `1351ac3f09479915471002bb0bf6c58be94f6a257fce2f578f587915e80699ea` |
| `catalog/mage-specter_of_thunder.png` | https://kobugda.com/assets/skills/mage/specter%20of%20thunder.png | 1823 | 34×34 | `754a6c547d57da8be8887702fdbde287c82bdd636304607474033f149bd8de62` |
| `catalog/mage-static_hemisphere.png` | https://kobugda.com/assets/skills/mage/static%20hemisphere.png | 2042 | 34×34 | `c7a651e2991d9e48212cf63981260922a62079904ca076d140758a0cc6b6e72c` |
| `catalog/mage-static_nova.png` | https://kobugda.com/assets/skills/mage/static%20nova.png | 1926 | 34×34 | `39e4e88de98447a5391ee595d4a6626ef7edfca67cba96a28dc639ca09101da9` |
| `catalog/mage-static_orb.png` | https://kobugda.com/assets/skills/mage/static%20orb.png | 1746 | 34×34 | `c456a821ccf24d8798fb1e347d515aa2d893ff4ccefdd75ac17d7be1a0d7bf23` |
| `catalog/mage-static_thorn.png` | https://kobugda.com/assets/skills/mage/static%20thorn.png | 1581 | 34×34 | `885e35fa2ef88f5a486d93509cddca563c867134d1c44d52fd2ae7eaa4a0a19b` |
| `catalog/mage-stroke.png` | https://kobugda.com/assets/skills/mage/stroke.png | 2283 | 34×34 | `8ae290ea3a16d75193d8c7c98c484dfe5fd091e684b0054c89fcf802f5cdf445` |
| `catalog/mage-stun_cloud.png` | https://kobugda.com/assets/skills/mage/stun%20cloud.png | 1557 | 34×34 | `2b76b624617dd34b2d38ab415a3449ea0e48dfb5b908096d25adb67039a27fdd` |
| `catalog/mage-summon_friend.png` | https://kobugda.com/assets/skills/mage/summon%20friend.png | 2120 | 34×34 | `5d1ce678403eba94e18adf413cc01757046d606c296886ae6e39ca93633dc586` |
| `catalog/mage-supernova.png` | https://kobugda.com/assets/skills/mage/supernova.png | 2330 | 34×34 | `54d03b1eef115c8ac425dd9259d9b094592d463b584818dc5fa33537c51e0cf3` |
| `catalog/mage-thunder_blast.png` | https://kobugda.com/assets/skills/mage/thunder%20blast.png | 2198 | 34×34 | `56ca0ffec7a00270c688deb901f1d63b65dcbb7d3cd0d80d9321b33069fd221a` |
| `catalog/mage-thunder_burst.png` | https://kobugda.com/assets/skills/mage/thunder%20burst.png | 2180 | 34×34 | `fdf85e9cf0bcc9d43e0e51b695f6dc958ebf62692b29eba9969c29aeb0611b8d` |
| `catalog/mage-thunder_impact.png` | https://kobugda.com/assets/skills/mage/thunder%20impact.png | 1753 | 34×34 | `5abf0a496cfeef3a8d63af9e4f660164610fbc452864f78bff59844afd28c240` |
| `catalog/mage-thunder.png` | https://kobugda.com/assets/skills/mage/thunder.png | 2150 | 34×34 | `acb1c73e0809c2007e1749525624ba491fb6c8ce033111e737d937e3fa2779f4` |
| `catalog/mage-thundercloud.png` | https://kobugda.com/assets/skills/mage/thundercloud.png | 1563 | 34×34 | `a2cba2e518900b2a70951d1f9c15c13a1376c970c8d88b1dce22d373cb7af0a8` |
| `catalog/mage-vampiric_fire.png` | https://kobugda.com/assets/skills/mage/vampiric%20fire.png | 1643 | 34×34 | `d8c0ee651ed58eb929295898dc4465de2919e928ba47eb9d28984f0ed1e57ac3` |
| `catalog/priest-blasting.png` | https://kobugda.com/assets/skills/priest/blasting.png | 1803 | 34×34 | `b1aa377f7c97c6a8d1a646f10615abe9fc51b50900a4acf6752669e3b03e4ac0` |
| `catalog/priest-bless_of_god.png` | https://kobugda.com/assets/skills/priest/bless%20of%20god.png | 1325 | 34×34 | `27cca12d78f4c09941a7e83e4a1f14d09f1b2fb385d80d78c72cc044cffb6cd9` |
| `catalog/priest-bloody.png` | https://kobugda.com/assets/skills/priest/bloody.png | 1119 | 34×34 | `302cb795dba52d35b01886c6fbfc2c03bc89a85070ed9437b15f40ad6021f9c1` |
| `catalog/priest-brave.png` | https://kobugda.com/assets/skills/priest/brave.png | 1821 | 34×34 | `4986eff9108f658e5e0cbbb2ef833d516e27c58c0747b1b68b48b0b1e8140491` |
| `catalog/priest-bright_mind.png` | https://kobugda.com/assets/skills/priest/bright%20mind.png | 2489 | 34×34 | `27aaa82b24b6861faf85cafb39e7e2c878155794c991c3aad669195023435343` |
| `catalog/priest-brightness.png` | https://kobugda.com/assets/skills/priest/brightness.png | 2395 | 34×34 | `de8532a5117a97351c258d397d574fd4a76bcdb0ddb05102455bd4f1c4da3d57` |
| `catalog/priest-calm_mind.png` | https://kobugda.com/assets/skills/priest/calm%20mind.png | 2614 | 34×34 | `dd1571e245a78437ce48931a413c5bb2ddd47c67cd404372d136231ee87bd6c9` |
| `catalog/priest-clear_mana.png` | https://kobugda.com/assets/skills/priest/clear%20mana.png | 1847 | 34×34 | `e4a450d9ac651dc29d9ecdfab16c015871973c549c529602d8c05828b9e7cfb8` |
| `catalog/priest-collapse.png` | https://kobugda.com/assets/skills/priest/collapse.png | 1344 | 34×34 | `54d1598bed258a46aad6ed61bdfa4c9abba57e7c91ddeb36cc0c372e8a73b032` |
| `catalog/priest-collision.png` | https://kobugda.com/assets/skills/priest/collision.png | 998 | 34×34 | `705413117912fabd390bdbfe4b2952b2a4ac8bd071eb6343e7e875e92b7af37b` |
| `catalog/priest-complete_healing.png` | https://kobugda.com/assets/skills/priest/complete%20healing.png | 2048 | 34×34 | `cabb48e034913b51b5207a5abeecf12504c723232e62bbe535053802327afcd6` |
| `catalog/priest-confusion.png` | https://kobugda.com/assets/skills/priest/confusion.png | 2070 | 34×34 | `204757cbcc8e7059bc377f44495decd05340dff19b6b67323b8b016ff74520a7` |
| `catalog/priest-counter_curse.png` | https://kobugda.com/assets/skills/priest/counter%20curse.png | 1377 | 34×34 | `f81104f8b7ba3eb9815ec3bc6ade8047e256e0aa70c8ca258efea01293e68384` |
| `catalog/priest-critical_light.png` | https://kobugda.com/assets/skills/priest/critical%20light.png | 1722 | 34×34 | `fc4961f2a19f244447d3d2b58c3b3ab5c25064201fefddcc07d925727b97ea2d` |
| `catalog/priest-critical_restore.png` | https://kobugda.com/assets/skills/priest/critical%20restore.png | 2069 | 34×34 | `b4bbfde43a9d67a36b235c470a1dd6bfb5420486e46cc677870dfaafd05039a5` |
| `catalog/priest-cure_curse.png` | https://kobugda.com/assets/skills/priest/cure%20curse.png | 2108 | 34×34 | `ece0bfd0a75d894308c006e81fd1957606366a5848f9109b9cfc15b0bae29353` |
| `catalog/priest-cure_disease.png` | https://kobugda.com/assets/skills/priest/cure%20disease.png | 1705 | 34×34 | `056b68e4dc54d8ffec7641dd2c74e4f181d4ddce892a5e81608e3d03808fb0d2` |
| `catalog/priest-curse_refraction.png` | https://kobugda.com/assets/skills/priest/curse%20refraction.png | 3399 | 34×34 | `b4579dfc1e9e56ced398847f5ff4632c845a7903f54741d7ff082fe6d80bfd4f` |
| `catalog/priest-discountis.png` | https://kobugda.com/assets/skills/priest/discountis.png | 1503 | 34×34 | `429422551a0dfc88d850cc341d04dc6051699c82200d9c4e9c3c10af3ef924b9` |
| `catalog/priest-elysian_web.png` | https://kobugda.com/assets/skills/priest/elysian%20web.png | 3153 | 34×34 | `b01e229929c7f525c60d072cc1205cdb145bd6c14b00ac9ada38bcbe89b56429` |
| `catalog/priest-eruption.png` | https://kobugda.com/assets/skills/priest/eruption.png | 1803 | 34×34 | `b1aa377f7c97c6a8d1a646f10615abe9fc51b50900a4acf6752669e3b03e4ac0` |
| `catalog/priest-fresh_mind.png` | https://kobugda.com/assets/skills/priest/fresh%20mind.png | 2693 | 34×34 | `e1bf0ce19718f114834d88cdbf70c59c1df0904b67a753b3c16589d9d7baa30f` |
| `catalog/priest-grace.png` | https://kobugda.com/assets/skills/priest/grace.png | 1775 | 34×34 | `f1da4fb820ca4f0c33106b7bb99696bfa8d3ee54e48bc16400690ccf76f569cb` |
| `catalog/priest-great_healing.png` | https://kobugda.com/assets/skills/priest/great%20healing.png | 2240 | 34×34 | `e552492aeac7929c0aa08b8236fdbd7c2dbded9deb38a2b9855b75f34b37806f` |
| `catalog/priest-great_restore.png` | https://kobugda.com/assets/skills/priest/great%20restore.png | 2343 | 34×34 | `c3cf31ea418783840abd34f0bc67e7cf88d3fc4c83b4a169dddf3c128a1de859` |
| `catalog/priest-greatness.png` | https://kobugda.com/assets/skills/priest/greatness.png | 1983 | 34×34 | `95e09408f6bcf8c0af485e5df338e3936b8ad7c74c0ece7e8ab1feca1c07bb83` |
| `catalog/priest-group_complete_healing.png` | https://kobugda.com/assets/skills/priest/group%20complete%20healing.png | 2594 | 34×34 | `11d635771521b244e344ae1250675ef42c9a46c199d55ec0ea360e01b55f8283` |
| `catalog/priest-group_massive_healing.png` | https://kobugda.com/assets/skills/priest/group%20massive%20healing.png | 2445 | 34×34 | `62668a90d3108f6afa6bc1f1f441370c912670872a885414c73465ae8dc4017d` |
| `catalog/priest-hades.png` | https://kobugda.com/assets/skills/priest/hades.png | 1337 | 34×34 | `c16f7bf657276a215d1a50808fb8ed2612297cb037a8fbf0adeff504225e1949` |
| `catalog/priest-hardness.png` | https://kobugda.com/assets/skills/priest/hardness.png | 1908 | 34×34 | `7068465a92947f538810ac248d610147e4c580b30636277717be45ea630b30e4` |
| `catalog/priest-harsh.png` | https://kobugda.com/assets/skills/priest/harsh.png | 1281 | 34×34 | `0205811088538a48ee4635dc0ff07aea3a1158d537f3ac7ecc9ae5be8edf935f` |
| `catalog/priest-healing.png` | https://kobugda.com/assets/skills/priest/healing.png | 2154 | 34×34 | `7470be08098937a1f5fb746858a812d29f8a093c40a95fbdca174ccd326f81f5` |
| `catalog/priest-heapness.png` | https://kobugda.com/assets/skills/priest/heapness.png | 1977 | 34×34 | `3577b43b358b6e4f2c4da5d4ce2a2aa87676e5cba70dd1503239e8397df4da83` |
| `catalog/priest-helis.png` | https://kobugda.com/assets/skills/priest/helis.png | 3175 | 34×34 | `b3710cd6ec65c62fd5a5271126a306c879e74baf99a10861baf671a3efd86e8c` |
| `catalog/priest-hellish.png` | https://kobugda.com/assets/skills/priest/hellish.png | 1344 | 34×34 | `54d1598bed258a46aad6ed61bdfa4c9abba57e7c91ddeb36cc0c372e8a73b032` |
| `catalog/priest-holy_attack.png` | https://kobugda.com/assets/skills/priest/holy%20attack.png | 1758 | 34×34 | `458f2f7933f9cec128c0a6bc0be2006c1fbbfb4c9bd566e4e359c5b04bd0a673` |
| `catalog/priest-imposingness.png` | https://kobugda.com/assets/skills/priest/imposingness.png | 1988 | 34×34 | `0bc7eb7a0dd525f79bb3833d74bb80a8b4fd1bdc11e30343bd1bd3bd216640d4` |
| `catalog/priest-insensibility_armor.png` | https://kobugda.com/assets/skills/priest/insensibility%20armor.png | 2376 | 34×34 | `1e61baf44416e2670a2bf9873f91833bb6ae6c01031b23cbc763487d273612d1` |
| `catalog/priest-insensibility_barrier.png` | https://kobugda.com/assets/skills/priest/insensibility%20barrier.png | 2048 | 34×34 | `695253363f5ec5e459481767ab79e85749107260b6cf3ad7619190d1bf0fd28a` |
| `catalog/priest-insensibility_guard.png` | https://kobugda.com/assets/skills/priest/insensibility%20guard.png | 2006 | 34×34 | `56b664043b85eb1c6f66d9ade6ee7fe573297a2ef0b6e8446307c24c95155817` |
| `catalog/priest-insensibility_peel.png` | https://kobugda.com/assets/skills/priest/insensibility%20peel.png | 1748 | 34×34 | `59e1e5f87ac5ba717bb7fbb61f0485940f96d561ff78acd3204c7dc246b48262` |
| `catalog/priest-insensibility_protector.png` | https://kobugda.com/assets/skills/priest/insensibility%20protector.png | 2222 | 34×34 | `d686e00acafc2eaf091f6d0cd1eccf02f11680240cd78da6b8cd1d60d2f3f177` |
| `catalog/priest-insensibility_shell.png` | https://kobugda.com/assets/skills/priest/insensibility%20shell.png | 2150 | 34×34 | `e6bacdf26bc0700871e05df0a28e9c370b0ac760211e976654c80549b7c169fe` |
| `catalog/priest-insensibility_shield.png` | https://kobugda.com/assets/skills/priest/insensibility%20shield.png | 2431 | 34×34 | `9d3815c8d46c345ae52d12de44c58c56bc9fb50e426f48db6a263fcc2b507d76` |
| `catalog/priest-insensibility_skin.png` | https://kobugda.com/assets/skills/priest/insensibility%20skin.png | 1949 | 34×34 | `0026b1c2297fdc1123efecd5d54ba483193493615a5c24e2154cf9bf6ad31bc6` |
| `catalog/priest-judgment.png` | https://kobugda.com/assets/skills/priest/judgment.png | 3266 | 34×34 | `f8ea33765e1c2f12f43309e69dffce8b18c293e4beded590a93c66b82f0a02b9` |
| `catalog/priest-light_counter.png` | https://kobugda.com/assets/skills/priest/light%20counter.png | 1553 | 34×34 | `d5a2f10e6c648922f3d1dacc3be6a514686376bdc9c45f3cc68dae24d1083bf5` |
| `catalog/priest-light_healing.png` | https://kobugda.com/assets/skills/priest/light%20healing.png | 2369 | 34×34 | `c7c6dc2d3fd525e9c60e8944c528d74f3fb6c7e3d0fa55ec7415f09db7a72a66` |
| `catalog/priest-light_magic_attack.png` | https://kobugda.com/assets/skills/priest/light%20magic%20attack.png | 1274 | 34×34 | `87726dfc70a6adf5c6a21d50647be1349cc61ca8457b30458500a55a07111d83` |
| `catalog/priest-light_restore.png` | https://kobugda.com/assets/skills/priest/light%20restore.png | 1997 | 34×34 | `55aa7adaeab1c20c663c57f3aa49091fc2464793453c5d9b9034778a7d73294e` |
| `catalog/priest-light_strike.png` | https://kobugda.com/assets/skills/priest/light%20strike.png | 1864 | 34×34 | `e9f06d9b5e008084064e64f087d1dad6a5a5e74435077ccd2213bef84b2be8fa` |
| `catalog/priest-major_restore.png` | https://kobugda.com/assets/skills/priest/major%20restore.png | 2231 | 34×34 | `0ba1029736ae32a92b2bdcd990373e5050d37386ea39842a602be26b772850cb` |
| `catalog/priest-malice.png` | https://kobugda.com/assets/skills/priest/malice.png | 1891 | 34×34 | `c8bb084885a07aefe81f7c14e32f94c232db1a3a4b9c5d8cd5e555699f58c18a` |
| `catalog/priest-massive_binder.png` | https://kobugda.com/assets/skills/priest/massive%20binder.png | 1667 | 34×34 | `9aac9af8aac01a9b2d4dc1784ff9ef5132bad0857136053a92f3c852c106174a` |
| `catalog/priest-massive_healing.png` | https://kobugda.com/assets/skills/priest/massive%20healing.png | 2375 | 34×34 | `89184bf94ce711d8916a029cc7faea87479d6105888b7d36da56dd353f430359` |
| `catalog/priest-massive_restore.png` | https://kobugda.com/assets/skills/priest/massive%20restore.png | 2352 | 34×34 | `4bf3473e6c9384bbc80953be8752ac10179ac8e4024bfcb07491de653bc6f3b3` |
| `catalog/priest-massive.png` | https://kobugda.com/assets/skills/priest/massive.png | 2147 | 34×34 | `a7ff6839a05596d3182edbc45985e5886f9a7fa0f7f39490e6b6f9b62766edeb` |
| `catalog/priest-massiveness.png` | https://kobugda.com/assets/skills/priest/massiveness.png | 1810 | 34×34 | `3da2dd9add3704acb40a6f1fcc58e2dbda3c8e155cceb63ca0c26330b9d093a3` |
| `catalog/priest-mightness.png` | https://kobugda.com/assets/skills/priest/mightness.png | 1947 | 34×34 | `179c560b94ce3968128174da14e4b0733c52647bc8320300590bd4ae1a8ef42d` |
| `catalog/priest-minaks_thorn.png` | https://kobugda.com/assets/skills/priest/minaks%20thorn.png | 3327 | 34×34 | `efc81fa940451e4753ef45ae1f52e9e8e6d573e3375b9cd9d5b672fd9301c26e` |
| `catalog/priest-parasite.png` | https://kobugda.com/assets/skills/priest/parasite.png | 1677 | 34×34 | `e19b38fe8134c72d01f7654d9ee97a37e2456cf208d609ce897aa50dfdc4df4a` |
| `catalog/priest-past_recovery.png` | https://kobugda.com/assets/skills/priest/past%20recovery.png | 1826 | 34×34 | `95b8b4f02cf7d11ee9165b709458fdcc9433b2a9fc93ee9e5b824c7bdfdf7399` |
| `catalog/priest-past_restore.png` | https://kobugda.com/assets/skills/priest/past%20restore.png | 2022 | 34×34 | `1b8c181980f6342eb04cf501c45e8b03e111702c304064a9c154a4af652419b5` |
| `catalog/priest-prayer_of_cronos.png` | https://kobugda.com/assets/skills/priest/prayer%20of%20cronos.png | 2241 | 34×34 | `d6f87984ffcae8f35d3ff4dd95ef8f12a99dee266ade5389c40ccd07540bc223` |
| `catalog/priest-prayer_of_gods_power.png` | https://kobugda.com/assets/skills/priest/prayer%20of%20gods%20power.png | 1496 | 34×34 | `aaa71781e4a9b0d31b4b8fe848db76582bb1c7774f4bc71df8d23678b4775c60` |
| `catalog/priest-raving_edge.png` | https://kobugda.com/assets/skills/priest/raving%20edge.png | 1288 | 34×34 | `0832d141083b56e6dc38e2dc47eca7740457a317cb7ffc622ce08188db7d8ec8` |
| `catalog/priest-resist_all.png` | https://kobugda.com/assets/skills/priest/resist%20all.png | 2350 | 34×34 | `f45af13b2ce16c4530f2944c7f1eee0e1bb3011df7d25267016505a8f82b656e` |
| `catalog/priest-resist_poison.png` | https://kobugda.com/assets/skills/priest/resist%20poison.png | 1696 | 34×34 | `0da782d7b68d635c89e944d4091f63ce25df992a240186c7ffcc07c0149db587` |
| `catalog/priest-restore.png` | https://kobugda.com/assets/skills/priest/restore.png | 2192 | 34×34 | `57d4b344b05cde8a19b70e9ab664874b24e59d0da8929ebc8db33490765543db` |
| `catalog/priest-resurrection_of_favors.png` | https://kobugda.com/assets/skills/priest/resurrection%20of%20favors.png | 1627 | 34×34 | `0cb7322f4612070fd2df0f7f512bcc5083ac6b93f0a623880bf29596b76ec08e` |
| `catalog/priest-resurrection_of_grace.png` | https://kobugda.com/assets/skills/priest/resurrection%20of%20grace.png | 1587 | 34×34 | `ec5dbb00765aab5cc2916b1d749f9ac137b424e537abb692ac11ce3dd3357ef0` |
| `catalog/priest-resurrection_of_love.png` | https://kobugda.com/assets/skills/priest/resurrection%20of%20love.png | 1479 | 34×34 | `f44b2858baaeed62e34d1d50bec38b7aadf6ac4f2f912aa21aa3c37a6a039408` |
| `catalog/priest-reverse_life.png` | https://kobugda.com/assets/skills/priest/reverse%20life.png | 1511 | 34×34 | `d8312cd5bc69705d0f43d1336ab247eb6122741ee20131a3044ed426dfa10ebe` |
| `catalog/priest-round_insensibility.png` | https://kobugda.com/assets/skills/priest/round%20insensibility.png | 1756 | 34×34 | `faed57d7665bb47d137c66528c152c035a61b983448de222b654e09dae81ee30` |
| `catalog/priest-ruin.png` | https://kobugda.com/assets/skills/priest/ruin.png | 1281 | 34×34 | `0205811088538a48ee4635dc0ff07aea3a1158d537f3ac7ecc9ae5be8edf935f` |
| `catalog/priest-shuddering.png` | https://kobugda.com/assets/skills/priest/shuddering.png | 1125 | 34×34 | `af98bb7a06dcfd482dfd7f99b913893e4432d640677b67aefe6cb14541c1bc52` |
| `catalog/priest-sleep_carpet.png` | https://kobugda.com/assets/skills/priest/sleep%20carpet.png | 1642 | 34×34 | `d2f457959de48c7de2c2bf9a2da736c60061e273f168e21a487be0745b4ea98d` |
| `catalog/priest-sleep_wing.png` | https://kobugda.com/assets/skills/priest/sleep%20wing.png | 1595 | 34×34 | `1d7bb6c9312c45585f5c541ed5f8b3e399bbc483522873bc744e10a8c8249ad6` |
| `catalog/priest-slow.png` | https://kobugda.com/assets/skills/priest/slow.png | 1690 | 34×34 | `48bc0362abec346299a259db225227eec1be801c1a536d1f5835f6a59f4e60b3` |
| `catalog/priest-strength.png` | https://kobugda.com/assets/skills/priest/strength.png | 2007 | 34×34 | `7d56287e9785e665efda94ef5f9b0dcc1871dc1aeff6f2dd923e3dd50b8134ab` |
| `catalog/priest-stroke.png` | https://kobugda.com/assets/skills/priest/stroke.png | 2283 | 34×34 | `8ae290ea3a16d75193d8c7c98c484dfe5fd091e684b0054c89fcf802f5cdf445` |
| `catalog/priest-strong.png` | https://kobugda.com/assets/skills/priest/strong.png | 1852 | 34×34 | `eacbc4dd1658ea5dc92f08b13fb94511598ba3bc1fad3b797886dce11f6af6a7` |
| `catalog/priest-subside.png` | https://kobugda.com/assets/skills/priest/subside.png | 1667 | 34×34 | `d6a88755d72b62a7deede918ac200ba6f264d8d7272a24bc8050544f428edff0` |
| `catalog/priest-superior_healing.png` | https://kobugda.com/assets/skills/priest/superior%20healing.png | 2464 | 34×34 | `72d608ddd27cbb291c762e93acb10adb58902dc5d2e41abdaeaba64fabc97cba` |
| `catalog/priest-superior_parasite.png` | https://kobugda.com/assets/skills/priest/superior%20parasite.png | 1531 | 34×34 | `4df5842998c24e007befc3f1cbf49c9bc96ec2e36e7af918aab62880c99adc2b` |
| `catalog/priest-superior_restore.png` | https://kobugda.com/assets/skills/priest/superior%20restore.png | 2444 | 34×34 | `f06d53f775f74bf799482b7fd336101c94d5b71c72f2507325683219833e3fc8` |
| `catalog/priest-superioris.png` | https://kobugda.com/assets/skills/priest/superioris.png | 1874 | 34×34 | `9f1037e00490d5efaa13e62855044b6340521100586397797380794f22e1b312` |
| `catalog/priest-sweep_mana.png` | https://kobugda.com/assets/skills/priest/sweep%20mana.png | 1843 | 34×34 | `3189ef7854883e3a08e7e988cf75ec94a0ed670f1508460152e19d46e73fd42d` |
| `catalog/priest-tilt.png` | https://kobugda.com/assets/skills/priest/tilt.png | 998 | 34×34 | `705413117912fabd390bdbfe4b2952b2a4ac8bd071eb6343e7e875e92b7af37b` |
| `catalog/priest-tiny_healing.png` | https://kobugda.com/assets/skills/priest/tiny%20healing.png | 2075 | 34×34 | `f7b43725b2314b6607b7cd8676f6df446780857d06d2ad5961eebd8c4a4ab837` |
| `catalog/priest-tiny_restore.png` | https://kobugda.com/assets/skills/priest/tiny%20restore.png | 2100 | 34×34 | `5de50b689052c822c0627b165a10304d7c6813bc86f0ac0e8940f88e1fcbd738` |
| `catalog/priest-torment.png` | https://kobugda.com/assets/skills/priest/torment.png | 1872 | 34×34 | `01d31e00b44b14197b3ca2989a89458311d2567b700bfb77069c70cbc10407fc` |
| `catalog/priest-undying.png` | https://kobugda.com/assets/skills/priest/undying.png | 1587 | 34×34 | `763be34e0b5ea1fe24beeabc000c007f030d8bb718cd5726673c3fdbb8399bcd` |
| `catalog/priest-wield.png` | https://kobugda.com/assets/skills/priest/wield.png | 1125 | 34×34 | `af98bb7a06dcfd482dfd7f99b913893e4432d640677b67aefe6cb14541c1bc52` |
| `catalog/priest-wildness.png` | https://kobugda.com/assets/skills/priest/wildness.png | 1803 | 34×34 | `b1aa377f7c97c6a8d1a646f10615abe9fc51b50900a4acf6752669e3b03e4ac0` |
| `catalog/priest-wrath.png` | https://kobugda.com/assets/skills/priest/wrath.png | 998 | 34×34 | `705413117912fabd390bdbfe4b2952b2a4ac8bd071eb6343e7e875e92b7af37b` |
| `catalog/rogue-arc_shot.png` | https://kobugda.com/assets/skills/rogue/arc%20shot.png | 2154 | 34×34 | `869f0d9158ff540c52b60b83c209a5895bb258fb68b0c04c9ab60516166d502c` |
| `catalog/rogue-archery.png` | https://kobugda.com/assets/skills/rogue/archery.png | 1863 | 34×34 | `aed27eda4eece53b91b23bcee6d92159b8b2f23356d6da3e4868b1428a00ec7a` |
| `catalog/rogue-archery2.png` | https://kobugda.com/assets/skills/rogue/archery2.png | 1882 | 34×34 | `48dbd8493906a005cb7c9c1e997a444b978df9801695df1d514fcd8aabf0eee1` |
| `catalog/rogue-arrow_shower.png` | https://kobugda.com/assets/skills/rogue/arrow%20shower.png | 1936 | 34×34 | `b60539dde15fdecb02c4553b111618d186b625348e7813a32af297e3fc13e411` |
| `catalog/rogue-beast_hiding.png` | https://kobugda.com/assets/skills/rogue/beast%20hiding.png | 1067 | 34×34 | `baf3b296ba5637a4278afa016b240162f252ef758fa247f5a4adb8d8eb837d6a` |
| `catalog/rogue-blinding_strafe.png` | https://kobugda.com/assets/skills/rogue/blinding%20strafe.png | 1639 | 34×34 | `7287fb91327697a5458bee6fd909b56a12cffe63a6be0a82ebe6c0876737fa05` |
| `catalog/rogue-blinding.png` | https://kobugda.com/assets/skills/rogue/blinding.png | 1087 | 34×34 | `db98365a4b9bb3c05ddb4113761cad3d3bc5865a86f31fbda1ba62df6f26aae7` |
| `catalog/rogue-blood_drain.png` | https://kobugda.com/assets/skills/rogue/blood%20drain.png | 1364 | 34×34 | `57deb542d62fd24ee6a80c922145de53db549852bcf93cb80816acc2d8bd6a84` |
| `catalog/rogue-bloody_beast.png` | https://kobugda.com/assets/skills/rogue/bloody%20beast.png | 2068 | 34×34 | `0f4a205c205d1027646af199c310dd42afacabb1e0e9f670fdba38782ded99ef` |
| `catalog/rogue-blow_arrow.png` | https://kobugda.com/assets/skills/rogue/blow%20arrow.png | 1397 | 34×34 | `797ff98af0920b92bff75ec4fe08d9a40f55f831e24237ef015f127e01a774d2` |
| `catalog/rogue-cats_eyes.png` | https://kobugda.com/assets/skills/rogue/cats%20eyes.png | 1582 | 34×34 | `bdaacc42d3cea84aa90e1e2379b80fca8371a7f05b7e564269e026604edc93e9` |
| `catalog/rogue-concentration.png` | https://kobugda.com/assets/skills/rogue/concentration.png | 1019 | 34×34 | `af91ec705f19176d8a0bb2e32d11337e654a0a31a3ed72582262c2ac522fa80f` |
| `catalog/rogue-counter_strike.png` | https://kobugda.com/assets/skills/rogue/counter%20strike.png | 1489 | 34×34 | `67fdc6b047ad20293bffb0a5d67046e068797c21364bc7e39072331faeeeb490` |
| `catalog/rogue-critical_point.png` | https://kobugda.com/assets/skills/rogue/critical%20point.png | 1541 | 34×34 | `66e6420ffccfe006f67cc5190267997aa60a34049896541e5af11b2bcc684c13` |
| `catalog/rogue-cure_curse.png` | https://kobugda.com/assets/skills/rogue/cure%20curse.png | 2108 | 34×34 | `ece0bfd0a75d894308c006e81fd1957606366a5848f9109b9cfc15b0bae29353` |
| `catalog/rogue-cure_disease.png` | https://kobugda.com/assets/skills/rogue/cure%20disease.png | 1705 | 34×34 | `056b68e4dc54d8ffec7641dd2c74e4f181d4ddce892a5e81608e3d03808fb0d2` |
| `catalog/rogue-cut.png` | https://kobugda.com/assets/skills/rogue/cut.png | 1940 | 34×34 | `297c4a6955c8063c5a6bc13a4c317d4ae0a4af8837087a11a23a2498e5a8f79f` |
| `catalog/rogue-dark_pursuer.png` | https://kobugda.com/assets/skills/rogue/dark%20pursuer.png | 1436 | 34×34 | `7243f34b0cc4ce478652ef72d8034267b486daf1e5bafdc722d2e87a2be7c18c` |
| `catalog/rogue-eskrima.png` | https://kobugda.com/assets/skills/rogue/eskrima.png | 3291 | 34×34 | `d17f3f7b81b4138a06ee107c085e04a16275fc27584662d3b908fd409a5a6302` |
| `catalog/rogue-evade.png` | https://kobugda.com/assets/skills/rogue/evade.png | 2071 | 34×34 | `5909f4e0b04d269d5f0e82d27576a8bca52d29f13d121159cb03facf4cdf3dcc` |
| `catalog/rogue-explosive_shot.png` | https://kobugda.com/assets/skills/rogue/explosive%20shot.png | 2182 | 34×34 | `f153e155a9b549b0fcf310339424cabb98a521f790b0088e0db83515209e3fc9` |
| `catalog/rogue-fire_arrow.png` | https://kobugda.com/assets/skills/rogue/fire%20arrow.png | 1887 | 34×34 | `03577613e1c41486d480aea996162ac9c5952ec9b4dbb4784287ec5583d2d783` |
| `catalog/rogue-fire_shot.png` | https://kobugda.com/assets/skills/rogue/fire%20shot.png | 2252 | 34×34 | `655588902a8b0a2efadddb229219cf6f1bf736533071858cf2f54ffd7302d1b0` |
| `catalog/rogue-guided_arrow.png` | https://kobugda.com/assets/skills/rogue/guided%20arrow.png | 1952 | 34×34 | `333ab9a69404461a07f5f123e74002ee42abeae61211904ae5eb89ee248a4d2b` |
| `catalog/rogue-hide.png` | https://kobugda.com/assets/skills/rogue/hide.png | 1514 | 34×34 | `788dd77d2b6f82dba690dbd7c5eaef45d095649fbaf5f615352c33573bc23772` |
| `catalog/rogue-ice_shot.png` | https://kobugda.com/assets/skills/rogue/ice%20shot.png | 1435 | 34×34 | `9f5765385ec231d95eff2a0f6c09f99d0497f663bb3b5b61e77f431296323282` |
| `catalog/rogue-illusion.png` | https://kobugda.com/assets/skills/rogue/illusion.png | 2079 | 34×34 | `c58a2cd1e849d168f1a3b4546ca3a38dba3ee06c21664376cb479e7b128af09a` |
| `catalog/rogue-jab.png` | https://kobugda.com/assets/skills/rogue/jab.png | 1939 | 34×34 | `754038362f934a1bf4cb14cc12ad8b0670e7f3beafd9ef407537f73a86fc262b` |
| `catalog/rogue-light_feet.png` | https://kobugda.com/assets/skills/rogue/light%20feet.png | 2160 | 34×34 | `7020f97e409e2ed48267832f6fae6666a3f3eedc1e9a4b41cbc7c0ba1c2409c7` |
| `catalog/rogue-lighting_shot.png` | https://kobugda.com/assets/skills/rogue/lighting%20shot.png | 1370 | 34×34 | `83571ad14a30a0b214f50bf5fd17fa70074a90f8afc7b488ebd8843987d3bf8b` |
| `catalog/rogue-lupine_eyes.png` | https://kobugda.com/assets/skills/rogue/lupine%20eyes.png | 1839 | 34×34 | `bcb59d505f58366f07e569433dde2e01ad8c3b64bee22eaacaeaa4d16711da01` |
| `catalog/rogue-magic_shield.png` | https://kobugda.com/assets/skills/rogue/magic%20shield.png | 3450 | 34×34 | `593958d47ce24538bbe228df3da505f46cbd2826bb1ecc703c995edd832e7bc7` |
| `catalog/rogue-minor_healing.png` | https://kobugda.com/assets/skills/rogue/minor%20healing.png | 2147 | 34×34 | `8f1a28442f375ad6d7d0e4c4fb9a5d76c9cd99ff0d03091ea4a29af8b9338d78` |
| `catalog/rogue-multiple_shot.png` | https://kobugda.com/assets/skills/rogue/multiple%20shot.png | 1684 | 34×34 | `62dfed6a0e22780942bfed8c2fe9aa5533c2dcb63bee6868cb813c1397545c10` |
| `catalog/rogue-perfect_shot.png` | https://kobugda.com/assets/skills/rogue/perfect%20shot.png | 2103 | 34×34 | `eaf3a68a349cbbde635f48a24f081b05cb5a5ca246850db55d07044ff20871bd` |
| `catalog/rogue-pierce.png` | https://kobugda.com/assets/skills/rogue/pierce.png | 1700 | 34×34 | `1d0a097c6aac5385b97260e6a47d326adedb2bb7e1e498499df3a50d65eff6a7` |
| `catalog/rogue-poison_arrow.png` | https://kobugda.com/assets/skills/rogue/poison%20arrow.png | 1448 | 34×34 | `9c2e694c1390e8ccf7864657b53df947e1c4e4b8d3a043ccc5fdb56631c85ba8` |
| `catalog/rogue-poison_shot.png` | https://kobugda.com/assets/skills/rogue/poison%20shot.png | 1691 | 34×34 | `40e4bc19c373bf0f3779f0430090b114f77641fafd22c91fc059dfe24ffae872` |
| `catalog/rogue-power_shot.png` | https://kobugda.com/assets/skills/rogue/power%20shot.png | 1504 | 34×34 | `9ef00b4973bbb610de355b7a34dca23cb1c25654408d394a7d9310f7ddf11aa7` |
| `catalog/rogue-safety.png` | https://kobugda.com/assets/skills/rogue/safety.png | 1605 | 34×34 | `2399ac79481bfa3e849ef01647999d32983cb11b713ff30a6a9a26c67e998786` |
| `catalog/rogue-scaled_skin.png` | https://kobugda.com/assets/skills/rogue/scaled%20skin.png | 2496 | 34×34 | `3366c4eef52f49363b5a547b00aa12ead71cfc73328e38b646516ff9faf56ced` |
| `catalog/rogue-shadow_hunter.png` | https://kobugda.com/assets/skills/rogue/shadow%20hunter.png | 1972 | 34×34 | `e34a7e177dab854ed128e799c68a4146c7e7e41dafffec1e804904bcadc84d52` |
| `catalog/rogue-shadow_shot.png` | https://kobugda.com/assets/skills/rogue/shadow%20shot.png | 1621 | 34×34 | `bfc14acfee1904218443b725fdb43210c4b6d871b2b82480609cf428bf93a5a2` |
| `catalog/rogue-shock.png` | https://kobugda.com/assets/skills/rogue/shock.png | 1594 | 34×34 | `7ab131887cd17e92f9b719275d0b8ca846c1efa62a34a049506b3a9277e7d71e` |
| `catalog/rogue-smoke_screen.png` | https://kobugda.com/assets/skills/rogue/smoke%20screen.png | 795 | 34×34 | `58000c59cf55a3adb90c33bfa248be5f39686954aa25ca5917ea0d20f7c0ac89` |
| `catalog/rogue-source_marking.png` | https://kobugda.com/assets/skills/rogue/source%20marking.png | 3112 | 34×34 | `450ec157669d1ac2c0c41e8ef7a5495bd8be984d7e9dd9d20f1f4bc3ec591a83` |
| `catalog/rogue-spike.png` | https://kobugda.com/assets/skills/rogue/spike.png | 2071 | 34×34 | `7f1199ccf39d56e59fee1c613ea8c6ee7bfb7adc3cf0582f16a1b6b1ec5a889b` |
| `catalog/rogue-sprint.png` | https://kobugda.com/assets/skills/rogue/sprint.png | 2371 | 34×34 | `8fc770849d6b629311dd5018d84faf5abcc9002a1b7253f973cce9c60969a030` |
| `catalog/rogue-stab.png` | https://kobugda.com/assets/skills/rogue/stab.png | 1675 | 34×34 | `e08207c90672803f685ae61ff0765c4d0d44b3c619dad545fd174a2089484731` |
| `catalog/rogue-stab2.png` | https://kobugda.com/assets/skills/rogue/stab2.png | 1976 | 34×34 | `92b8152dc3043cbccea92a97d9444ac69f27e0a83fd99a49373f10022dcad277` |
| `catalog/rogue-stealth.png` | https://kobugda.com/assets/skills/rogue/stealth.png | 1525 | 34×34 | `0edc6439a2ad887aff61751ec91388aecd833afa48a6f748100657319b062fcd` |
| `catalog/rogue-strength_of_wolf.png` | https://kobugda.com/assets/skills/rogue/strength%20of%20wolf.png | 1658 | 34×34 | `2a742077deb72bbdb835fe2d0982eece9442a8c8ab167d20d6b6cbab55f3aca6` |
| `catalog/rogue-stroke.png` | https://kobugda.com/assets/skills/rogue/stroke.png | 2283 | 34×34 | `8ae290ea3a16d75193d8c7c98c484dfe5fd091e684b0054c89fcf802f5cdf445` |
| `catalog/rogue-swift.png` | https://kobugda.com/assets/skills/rogue/swift.png | 2106 | 34×34 | `d33ec38427ca860fb79a807e0237eb679905a57e6c89d34788628d621d894ddc` |
| `catalog/rogue-through_shot.png` | https://kobugda.com/assets/skills/rogue/through%20shot.png | 1495 | 34×34 | `e00c9b132679661d7eeaa3c4bc63926ba309024f6a3a01b60853767e74b49563` |
| `catalog/rogue-throwing_knife.png` | https://kobugda.com/assets/skills/rogue/throwing%20knife.png | 1861 | 34×34 | `6b1d3c863113c20b4ee51e775100571d9908d20be6817c9bc39a3f1ab15d0c02` |
| `catalog/rogue-thrust.png` | https://kobugda.com/assets/skills/rogue/thrust.png | 1855 | 34×34 | `e97bb3408f54c1066c04faac4431e48ac4577271578c980a116db880c2e8df3d` |
| `catalog/rogue-vampiric_touch.png` | https://kobugda.com/assets/skills/rogue/vampiric%20touch.png | 2003 | 34×34 | `2629d145f14dd2c9b88f928623f7d0544d3c6b1d715028d54360219fe72bcdfd` |
| `catalog/rogue-viper.png` | https://kobugda.com/assets/skills/rogue/viper.png | 1842 | 34×34 | `87a93ff65264323864ec04d168bee41ffce30191938146fe3f34bb1797fbd4a4` |
| `catalog/rogue-weapon_cancelation.png` | https://kobugda.com/assets/skills/rogue/weapon%20cancelation.png | 3350 | 34×34 | `242cba7148aa2ca8267a144b70fd6ebd92d8b151b812bedd078abad6ba89ac32` |
| `catalog/rogue-wild_advent.png` | https://kobugda.com/assets/skills/rogue/wild%20advent.png | 1292 | 34×34 | `bbdb44010d65ef1a4e2abd8d2d1961a3fcf8c5e0635dc35183f6baba639c40e6` |
| `catalog/warrior-battle_cry.png` | https://kobugda.com/assets/skills/warrior/battle%20cry.png | 2450 | 34×34 | `b257e121fa77af5a2f5b76279bc09964b8d233cfdf0c274c16a688cd9b04dfa0` |
| `catalog/warrior-berserk_echo.png` | https://kobugda.com/assets/skills/warrior/berserk%20echo.png | 2251 | 34×34 | `62f1b315f24e0039ceaa2539f4c2f106f4a3e3a07ff9d662f9ae567e9d2a62fc` |
| `catalog/warrior-berserker.png` | https://kobugda.com/assets/skills/warrior/berserker.png | 1660 | 34×34 | `6e14602279e0c2eb1b184c06d2aaa5f4ee190a50c911da5690c8dd3e52912301` |
| `catalog/warrior-blade_of_hate_2.png` | https://kobugda.com/assets/skills/warrior/blade%20of%20hate%202.png | 2191 | 34×34 | `e73a3173e3b6d1c5fced0de25d8ae5f0b4db709581993e2a3cb7c01a2295007b` |
| `catalog/warrior-blade_of_hate.png` | https://kobugda.com/assets/skills/warrior/blade%20of%20hate.png | 2069 | 34×34 | `b040b392bb3d53bd891f80e6a9c4b247e6d258bde9cf85af4178f595f9fc583f` |
| `catalog/warrior-blade_of_hell.png` | https://kobugda.com/assets/skills/warrior/blade%20of%20hell.png | 2247 | 34×34 | `180f585e355e8bcc28fb8e70ec68b0c8f36c10dcdc9c950333887170217bd00b` |
| `catalog/warrior-blaze_killer.png` | https://kobugda.com/assets/skills/warrior/blaze%20killer.png | 1761 | 34×34 | `e31c83cb7aeb3af0bd14fb4952d439b27dc03b23a3393eed97505058635ef36e` |
| `catalog/warrior-blaze.png` | https://kobugda.com/assets/skills/warrior/blaze.png | 2183 | 34×34 | `90d17e35a9e6dba45ad22001cde36b52c7ace7971dbd2a62d8216d99946bce62` |
| `catalog/warrior-blooding.png` | https://kobugda.com/assets/skills/warrior/blooding.png | 876 | 34×34 | `df69c7a92e0ed1843913b0db11cdef69c89784bf8faed7a4dbfcb76e0ea27964` |
| `catalog/warrior-carving.png` | https://kobugda.com/assets/skills/warrior/carving.png | 2214 | 34×34 | `797065959aee462835afe090122a3d0664827eae1d77aa830eb79c8e612479f9` |
| `catalog/warrior-cleave.png` | https://kobugda.com/assets/skills/warrior/cleave.png | 2225 | 34×34 | `d1adef8c4edfde49aa8493c9c616ae915dd9c98c65b58a1b982a1e09b1b6264d` |
| `catalog/warrior-crash.png` | https://kobugda.com/assets/skills/warrior/crash.png | 1834 | 34×34 | `b34665f90bc0b41f329030a1bc88d957c012bcb025ff306fc49289528741474f` |
| `catalog/warrior-cry_echo.png` | https://kobugda.com/assets/skills/warrior/cry%20echo.png | 2474 | 34×34 | `da6d3f5b9bb70d407077b10cb0a291eea7eb1f5fa6da2baeb168c27661f10f02` |
| `catalog/warrior-echo.png` | https://kobugda.com/assets/skills/warrior/echo.png | 1533 | 34×34 | `ea2aafb33a34edf9f24d22dd6b80f6b16fa911d552851d7cea599229d3b8d73b` |
| `catalog/warrior-exceed_break.png` | https://kobugda.com/assets/skills/warrior/exceed%20break.png | 3050 | 34×34 | `e5501ef32e1f1ec198c4df946f1bf7b64470a2ca6463c1bb3995f6b8266ec0f7` |
| `catalog/warrior-frenzy.png` | https://kobugda.com/assets/skills/warrior/frenzy.png | 1984 | 34×34 | `780885e37341ee529681207c9d769c4940800706404007ff37876d66f136386a` |
| `catalog/warrior-gain.png` | https://kobugda.com/assets/skills/warrior/gain.png | 2015 | 34×34 | `2da2d015900779d84e5c2554075c289bfd75ef9ef18b9a4d793e93b71406ac36` |
| `catalog/warrior-hash.png` | https://kobugda.com/assets/skills/warrior/hash.png | 1967 | 34×34 | `3b9354459671f578a423a3a9c08238411f07d781bbfca33d2bb2e9ba0239b90c` |
| `catalog/warrior-hate.png` | https://kobugda.com/assets/skills/warrior/hate.png | 2377 | 34×34 | `256fd6e43a1e664051f057a2b572ec2eafdd1bed33636e207744e31367afbdae` |
| `catalog/warrior-hell_blade.png` | https://kobugda.com/assets/skills/warrior/hell%20blade.png | 1313 | 34×34 | `6c7c6b52c98aeffd58163a5876b97b412bec7198b86c9aa73e4dbe9cd61768cd` |
| `catalog/warrior-hoodwink.png` | https://kobugda.com/assets/skills/warrior/hoodwink.png | 1866 | 34×34 | `68d920b7b21cfec2955df53e57919acbf6ff022aaefc1e673c6e468515cad205` |
| `catalog/warrior-howling_sword.png` | https://kobugda.com/assets/skills/warrior/howling%20sword.png | 1773 | 34×34 | `7178be950d9e071a3ac438861ccecc49864f27eff22678af55a6b498a3313cac` |
| `catalog/warrior-hp_booster.png` | https://kobugda.com/assets/skills/warrior/hp%20booster.png | 1253 | 34×34 | `4f0389b8aa4dd9680c84fca3437731a426e5caf8260e31bcb662155e3831d206` |
| `catalog/warrior-inevitable_muderus.png` | https://kobugda.com/assets/skills/warrior/inevitable%20muderus.png | 3569 | 34×34 | `29654bb437b8b805da9f219370f19b11c5e988e1ed0b99a1c838cf238592aee4` |
| `catalog/warrior-killer.png` | https://kobugda.com/assets/skills/warrior/killer.png | 2073 | 34×34 | `5f956f4b8db93a02f3fdf463adf1538f68c2104eae0f213f249179628a8aaa29` |
| `catalog/warrior-leg_cutting.png` | https://kobugda.com/assets/skills/warrior/leg%20cutting.png | 2146 | 34×34 | `9f15d7e7f00eee5da21b8b277be38aaddac25457270fda3607f31709381d0c1d` |
| `catalog/warrior-mangling.png` | https://kobugda.com/assets/skills/warrior/mangling.png | 2377 | 34×34 | `256fd6e43a1e664051f057a2b572ec2eafdd1bed33636e207744e31367afbdae` |
| `catalog/warrior-multiple_shock.png` | https://kobugda.com/assets/skills/warrior/multiple%20shock.png | 2183 | 34×34 | `90d17e35a9e6dba45ad22001cde36b52c7ace7971dbd2a62d8216d99946bce62` |
| `catalog/warrior-outrage.png` | https://kobugda.com/assets/skills/warrior/outrage.png | 2008 | 34×34 | `80b8febebe1520c185462ed2e2b27635e7bc7bf8c6bf0bab3ab6049af0e0fca4` |
| `catalog/warrior-pain_killer.png` | https://kobugda.com/assets/skills/warrior/pain%20killer.png | 2201 | 34×34 | `12a20ab16f6911492f71c20929fcc4dd6b05949423b634ec4ff0d10a942c08f3` |
| `catalog/warrior-piercing.png` | https://kobugda.com/assets/skills/warrior/piercing.png | 2010 | 34×34 | `67a6bfda11fb69c4022a1f029e18fa7874688b17633f1375c2c6b842bc3e7f51` |
| `catalog/warrior-prick.png` | https://kobugda.com/assets/skills/warrior/prick.png | 1705 | 34×34 | `d35e2b3cc22e581fa2055248726f65dfb88305fcfc6f6169a50327e9afe6dddd` |
| `catalog/warrior-rage.png` | https://kobugda.com/assets/skills/warrior/rage.png | 1715 | 34×34 | `7fc9b880f4793f332460dc3940a37b1bdb4766411504ef2bde131836671274e4` |
| `catalog/warrior-regeneration.png` | https://kobugda.com/assets/skills/warrior/regeneration.png | 2367 | 34×34 | `0ddda69b8fb451b1066ed3691b890cdcfbcfba8cfc5be9598832ab88e4247ecd` |
| `catalog/warrior-restoration.png` | https://kobugda.com/assets/skills/warrior/restoration.png | 1603 | 34×34 | `2d8fdd3d9b8ddb980f5c812dd4181b23f1a8cc2fd18a18d0f0222fec70e8a546` |
| `catalog/warrior-return_to_life.png` | https://kobugda.com/assets/skills/warrior/return%20to%20life.png | 2344 | 34×34 | `2c9f3488f21b30be5844f661ebdf6412a699b15403b31b5a64eba3f08dcdb064` |
| `catalog/warrior-rise.png` | https://kobugda.com/assets/skills/warrior/rise.png | 2328 | 34×34 | `852a80fd67eecf988c0875027ab6c474e367c7a32a43200d548b1499d142ed31` |
| `catalog/warrior-scream.png` | https://kobugda.com/assets/skills/warrior/scream.png | 3254 | 34×34 | `15e0d392e9a674b3acb9040f3f712251c5f26b937959c43bfef42d0b51560cbe` |
| `catalog/warrior-sever.png` | https://kobugda.com/assets/skills/warrior/sever.png | 2284 | 34×34 | `d4134c48c54b256e9ca02e0bfc53ea6e12f96c85b80f7aebd050d486af61bc2e` |
| `catalog/warrior-shear.png` | https://kobugda.com/assets/skills/warrior/shear.png | 2141 | 34×34 | `90b013ee70cfd5c4c9674232f63be33cc366423f5802f710b965c5975bc62024` |
| `catalog/warrior-shock_stun.png` | https://kobugda.com/assets/skills/warrior/shock%20stun.png | 2298 | 34×34 | `9b164d0df0d9b31d476656f7454c40fac6757b36dc02cf7bb8719feebd6438f5` |
| `catalog/warrior-slash.png` | https://kobugda.com/assets/skills/warrior/slash.png | 1908 | 34×34 | `fda8ebbce150749b83e1bb3c9f43c87e8902f81f022d8066472ca768c63e6d96` |
| `catalog/warrior-stroke.png` | https://kobugda.com/assets/skills/warrior/stroke.png | 2283 | 34×34 | `8ae290ea3a16d75193d8c7c98c484dfe5fd091e684b0054c89fcf802f5cdf445` |
| `catalog/warrior-sword_aura.png` | https://kobugda.com/assets/skills/warrior/sword%20aura.png | 1511 | 34×34 | `156dbd0d35234e04a0e619402f363af3e3f63090ead22d6842e06a7ab5da112a` |
| `catalog/warrior-sword_dancing.png` | https://kobugda.com/assets/skills/warrior/sword%20dancing.png | 1913 | 34×34 | `4abe628cf0d3e384c4e98ce0f1df867dcd6fdc659be76a2f256c8890ecd985a5` |
| `catalog/warrior-wind.png` | https://kobugda.com/assets/skills/warrior/wind.png | 2279 | 34×34 | `fac58a0edb8c99cebcc0dc3ca533def34606a8d58b150d035799b01bdc360974` |
| `catalog/warrior-wink.png` | https://kobugda.com/assets/skills/warrior/wink.png | 1866 | 34×34 | `68d920b7b21cfec2955df53e57919acbf6ff022aaefc1e673c6e468515cad205` |

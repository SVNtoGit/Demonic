using System.Linq;
using CommonBehaviors.Actions;
using Demonic.Core;
using Demonic.Managers;
using Demonic.Settings;
using JetBrains.Annotations;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Demonic.Specialisation
{
    [UsedImplicitly]
    internal class Affliction : RotationBase
    {
        private static DemonicSettings Settings { get { return DemonicSettings.Instance; } }

        #region RotationBase Overrides
        public override WoWSpec KeySpec { get { return WoWSpec.WarlockAffliction;} }
        public override string Name { get { return "Affliction"; } }
        public override string Revision { get { return "$Rev: 42 $"; } }
        #endregion
        
        #region Rotation Handles
        public override Composite Rotation
        {
            get
            {
                return new PrioritySelector(
                    new Decorator(ret => SpellManager.GlobalCooldown, new ActionAlwaysSucceed()),
                    CachedUnits.Pulse,
                    Common.SetCounts, // Set counts for our AoE trigger
                    Common.Combat(), // Handles all of our utility / talent abilities.

                    HandleDarkSoulMisery(), 

                    // AoE selector
                    new Decorator(ret => Common._nearbyAoEUnitCount < Settings.Affliction_AoELowUnitCount || !Settings.Affliction_EnableAoEAbilities, SingleTarget()),
                    
                    new Decorator(ret => Settings.Affliction_EnableAoEAbilities &&
                        (Common._nearbyAoEUnitCount >= Settings.Affliction_AoEHighUnitCount && Me.Level >= 62), HandleHighUnitAoECombat()),
                    
                    new Decorator(ret => Settings.Affliction_EnableAoEAbilities &&
                        (Common._nearbyAoEUnitCount >= Settings.Affliction_AoELowUnitCount
                        && Common._nearbyAoEUnitCount < Settings.Affliction_AoEHighUnitCount
                        || (Common._nearbyAoEUnitCount >= Settings.Affliction_AoEHighUnitCount && Me.Level <= 61)), HandleLowUnitAoECombat())
                   
                    );
            }
        }

        private static Composite HandleStopChannelToCurse()
        {
            return
                new Decorator(
                    a =>
                        // Drain Soul, Boss, Need DoTs...
                    (Me.ChanneledCastingSpellId == SpellId.DrainSoul &&
                    ((NeedaCurse && Me.CurrentSoulShards >= 1) || !NeedDrainSoul))
                    ||
                        // Malefic Grasp and target < 20% HP
                    (Me.ChanneledCastingSpellId == SpellId.MaleficGrasp &&
                    Me.CurrentTarget.HealthPercent < 20),
                    new Action(delegate
                    {
                        SpellManager.StopCasting();
                        return RunStatus.Failure;
                    }));
        }

        private static Composite SingleTarget()
        {
            return new PrioritySelector(
                HandleStopChannelToCurse(),
                new Decorator(ret => Me.HasAura("Soul Swap"), new Action(delegate { Me.CancelAura("Soul Swap"); return RunStatus.Failure; })), // If we accidently get Soul Swap Exhale
                Spell.PreventDoubleCast("Soulburn", 2, ret => NeedSoulBurn),
                Spell.ForceCast(SpellId.SoulburnSoulswap, on => Me.CurrentTarget, ret => Me.ActiveAuras.ContainsKey("Soulburn")),
                new Decorator(ret => Me.HasAura("Soulburn") || Me.ActiveAuras.ContainsKey("Soulburn") || Me.IsChanneling, new ActionAlwaysSucceed()),

                //Spell.PreventDoubleCast("Curse of the Elements", 2.5, ret => Common.NeedCurseOfElements),
                // UA needs to be cast by string as SpellManager.CanCast() does not contain overload to ignore moving.
                Spell.PreventDoubleCast("Unstable Affliction", 2, on => Me.CurrentTarget, ret => NeedUnstableAffliction, Common.UseKilJaedensCunning),
                Spell.PreventDoubleCast(SpellId.Corruption, 2, ret => NeedCorruption),
                Spell.PreventDoubleCast(SpellId.Agony, 2, ret => NeedAgony),
                Spell.PreventDoubleCast("Haunt", 3, ret => NeedHaunt),

                Spell.PreventDoubleChannel("Drain Soul", 0.5, true, on => Me.CurrentTarget,
                                           ret => Me.Level >= 19 && NeedDrainSoul && !NeedaCurse, Common.UseKilJaedensCunning),

                Spell.PreventDoubleChannel("Malefic Grasp", 0.5, true, on => Me.CurrentTarget, ret => Me.Level >= 42 && !NeedaCurse,
                                           Common.UseKilJaedensCunning),

                Spell.Cast("Shadow Bolt", ret => Me.Level <= 41 && !NeedaCurse, "Low Level Filler"),

                Spell.Cast("Life Tap",
                           ret => !Me.IsChanneling && (Me.IsMoving() && Common.CanLifeTap || Me.ManaPercent < 10)),

                Spell.Cast("Fel Flame",
                           ret => Me.IsMoving() && !Common.UseKilJaedensCunning && Me.IsSafelyFacing(Me.CurrentTarget))
                           );
        }

        private static Composite HandleLowUnitAoECombat()
        {
            WoWUnit soulSwapTarget = null;
            return new PrioritySelector(ctx => soulSwapTarget = GetSoulSwapTarget(),

                Spell.ForceCast(SpellId.SoulburnSoulswap, on => soulSwapTarget, ret => Me.ActiveAuras.ContainsKey("Soul Swap")),
                new Decorator(ret => Me.HasAura("Soul Swap"), new Action(delegate { Me.CancelAura("Soul Swap"); return RunStatus.Failure; })),

                // If target isn't boss, and less than 5% hp, and we have 3 or less shards, drain soul to get shards.
                Spell.PreventDoubleChannel("Drain Soul", 0.5, true, on => Me.CurrentTarget,
                                           ret =>
                                           !Me.CurrentTarget.IsBoss() && Me.CurrentTarget.HealthPercent < 5 &&
                                           Me.CurrentSoulShards <= 3),

                // Handle canceling drain soul to perform other actions
                new Decorator(
                    a => Me.Level >= 79 && Me.ChanneledCastingSpellId == 1120 && soulSwapTarget != null && Me.CurrentSoulShards >= 1,
                    new Action(delegate
                    {
                        SpellManager.StopCasting();
                        return RunStatus.Failure;
                    })),

                // Soulburn
                Spell.PreventDoubleCast("Soulburn", 1,
                                        ret => Me.Level >= 79 &&
                                        soulSwapTarget != null && !Me.ActiveAuras.ContainsKey("Soulburn") &&
                                        Me.CurrentSoulShards >= 1),

                // AoE Soul Swap
                new Decorator(ret => Me.Level >= 79 && Me.HasAura(SpellId.Soulburn) && soulSwapTarget != null,
                              Spell.ForceCast(SpellId.SoulburnSoulswap, on => soulSwapTarget,
                                              ret => Me.ActiveAuras.ContainsKey("Soulburn"))
                    ),

                MultiDoTSelector(),

                SingleTarget()
                );
        }

        private static Composite HandleHighUnitAoECombat()
        {
            WoWUnit seedTarget = null;
            return new PrioritySelector(ctx => seedTarget = GetSoCTarget(),
                new Decorator(ret => Me.HasAura("Soul Swap"), new Action(delegate { Me.CancelAura("Soul Swap"); return RunStatus.Failure; })),

                Spell.PreventDoubleCast("Soulburn", 1, ret => !Me.HasAura(SpellId.Soulburn) && Me.CurrentSoulShards >= 1
                                        && (NeedSoulburnSoC || GetSoulSwapTarget() != null)),

                // SB:Seed of Corruption - Not in SpellManager so cast by ID.
                Spell.PreventDoubleCast(SpellId.SoulburnSeedOfCorruption, 0.5, on => seedTarget, ret => seedTarget != null && Me.HasAura(SpellId.Soulburn) && !seedTarget.HasAura(SpellId.SoulburnSeedOfCorruption)),

                // Soul Swap current target
                Spell.ForceCast(SpellId.SoulburnSoulswap, on => Me.CurrentTarget, ret => Me.CurrentTarget != null && Me.CurrentTarget.HasAllAuras("Agony", "Corruption") && Me.ActiveAuras.ContainsKey("Soulburn")),

                // AoE Soul Swap
                Spell.ForceCast(SpellId.SoulburnSoulswap, on => GetSoulSwapTarget(), ret => GetSoulSwapTarget() != null && Me.ActiveAuras.ContainsKey("Soulburn")),

                // Spam SoC
                Spell.PreventDoubleCast(SpellId.SeedofCorruption, 0.5, on => seedTarget, ret => seedTarget != null),

                Spell.Cast("Life Tap", ret => !Me.IsChanneling && Common.CanLifeTap),

                // Multi DoT Selector when nothing else to do.
                new Decorator(ret => Me.CurrentSoulShards == 0 || !Me.HasAura(SpellId.Soulburn) || seedTarget == null,
                    MultiDoTSelector())
                
                );
        }

        private static Composite MultiDoTSelector()
        {
            return new Decorator(ret => !Me.ActiveAuras.ContainsKey("Soulburn"),
                new PrioritySelector(
                Spell.PreventDoubleMultiDoT("Corruption", 1, Me, 40, 3, ret => true),
                Spell.PreventDoubleMultiDoT("Agony", 1, Me, 40, 3, ret => Me.Level >= 36),
                Spell.PreventDoubleMultiDoT("Unstable Affliction", 1, Me, 40, 3, ret => true)
                        ));

        }

        private static Composite HandleDarkSoulMisery()
        {
            return new Decorator(ret => Settings.Affliction_UseDarkSoulMisery && !Common.Hasted,
                new PrioritySelector(
                /*
                    0,On Cooldown
                    1,On Boss Or Player
                    2,On Target Low HP
                 */
                    Spell.Cast("Dark Soul: Misery", ret => Settings.Affliction_DarkSoulMiseryCondition == 0 && !Me.HasAura("Dark Soul: Misery")),
                    Spell.Cast("Dark Soul: Misery", ret => Settings.Affliction_DarkSoulMiseryCondition == 1 && Me.CurrentTarget.IsBossOrPlayer() && !Me.HasAura("Dark Soul: Misery")),
                    Spell.Cast("Dark Soul: Misery", ret => Settings.Affliction_DarkSoulMiseryCondition == 2 && Me.CurrentTarget != null && Me.CurrentTarget.HealthPercent <= Settings.Affliction_DarkSoulMiseryLowHPValue && !Me.HasAura("Dark Soul: Misery"))
                    ));
        }

        public override Composite PreCombat
        {
            get
            {
                return new PrioritySelector(
                    Common.PreCombat()
                    );
            }
        }

        #endregion

        #region Conditions

        private static bool NeedHaunt
        {
            get
            {
                return Me.Level >= 62 && !Me.IsMoving() &&
                    // Check we don't already have haunt, or haunt is about to expire.
                       (!Me.CurrentTarget.HasMyAura("Haunt") || (Me.CurrentTarget.HasMyAura("Haunt") && Spell.GetAuraTimeLeft("Haunt", Me.CurrentTarget) < Spell.GetSpellCastTime("Haunt")))

                       // We have 3 shards (so close to cap)
                       && (Me.CurrentSoulShards >= 3

                       // We have 1 or more shards and dark soul is on cooldown for more than 35 seconds
                       || Me.CurrentSoulShards >= 1 && Spell.GetSpellCooldown(SpellId.DarkSoulMisery).TotalSeconds > 35

                       // We have 2 or more shards, and the time left on dark soul misery is longer than the time it'll take to cast haunt
                       || Me.CurrentSoulShards >= 2 && Spell.GetAuraTimeLeft(SpellId.DarkSoulMisery) > Spell.GetSpellCastTime(SpellId.Haunt)

                       // We have a shard, and target HP is less than, or equal to 20%
                       || Me.CurrentSoulShards >= 1 && Me.CurrentTarget.HealthPercent <= 20)
                       ;
            }
        }
        private static bool NeedUnstableAffliction
        {
            get { return Spell.GetAuraTimeLeftMs("Unstable Affliction", Me.CurrentTarget) <= Settings.UnstableAfflictionRefresh; }
        }
        private static bool NeedAgony
        {
            get { return Me.Level >= 36 && Spell.GetAuraTimeLeftMs("Agony", Me.CurrentTarget) <= Settings.AgonyRefresh /*|| CasterStatBuffActiveAgony*/; }
        }
        private static bool NeedCorruption
        {
            get { return Spell.GetAuraTimeLeftMs("Corruption", Me.CurrentTarget) <= Settings.CorruptionRefresh /*|| CasterStatBuffActiveCorruption*/; }
        }
        private static bool NeedDrainSoul { get { return Me.CurrentTarget != null && Me.CurrentTarget.HealthPercent <= 20; } }
        private static bool NeedaCurse
        {
            get { return NeedHaunt || NeedUnstableAffliction || NeedAgony || NeedCorruption; }
        }
        
        private static bool NeedSoulSwap
        {
            get
            {
                if (Me.Level < 79) return false;

                if (Me.CurrentTarget != null && !Me.IsChanneling && Me.CurrentTarget.HealthPercent <= 20 && Me.CurrentTarget.IsBoss() && (NeedAgony || NeedCorruption || NeedUnstableAffliction))
                {
                    return true;
                }

                return Me.CurrentTarget != null &&
                    (
                    !Me.CurrentTarget.HasAura("Unstable Affliction") &&
                    !Me.CurrentTarget.HasAura("Corruption") &&
                    !Me.CurrentTarget.HasAura("Agony")
                    );
            }
        }
        private static bool NeedSoulBurn
        {
            get
            {
                return Me.CurrentTarget != null 
                    && !Me.CurrentTarget.IsDead 
                    && Me.CurrentSoulShards >= 1 
                    && !Me.ActiveAuras.ContainsKey("Soulburn")
                    && NeedSoulSwap;
            }
        }
        private static bool NeedSoulburnSoC
        {
            get
            {
                return
                        !Unit.CachedNearbyAttackableUnits(Me.CurrentTarget.Location, 20)
                            .Any(x => x.HasAura(SpellId.SoulburnSeedOfCorruption))

                        && Unit.CachedNearbyAttackableUnits(Me.CurrentTarget.Location, 15)
                                .Count(x => x.HasAura("Corruption")) >= 2;
            }
        }

        #endregion

        #region Targets

        private static WoWUnit GetSoulSwapTarget()
        {
            return Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 40)
                            .OrderByDescending(x => x.CurrentHealth)
                            .FirstOrDefault(x => !x.HasAllAuras("Corruption", "Agony", "Unstable Affliction"))
                   ??
                   Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 40)
                             .OrderByDescending(x => x.CurrentHealth)
                             .FirstOrDefault(x => !x.HasAura("Agony"));
        }

        private static WoWUnit GetSoCTarget()
        {
            return Unit.CachedNearbyAttackableUnits(Me.CurrentTarget.Location, 15)
                            .OrderByDescending(x => x.CurrentHealth)
                            .FirstOrDefault(x => !x.HasAura("Seed of Corruption") && x.Distance < 40 && x.InLineOfSpellSight);
        }

        #endregion Targets
    }
}

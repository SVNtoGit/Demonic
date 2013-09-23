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
    internal class Destruction : RotationBase
    {

        #region RotationBase Overrides
        public override WoWSpec KeySpec { get { return WoWSpec.WarlockDestruction; } }
        public override string Name { get { return "Destruction"; } }
        public override string Revision { get { return "$Rev: 42 $"; } }
        #endregion
        private static DemonicSettings Settings { get { return DemonicSettings.Instance; } }

        #region Honorbuddy's Rotation Overrides
        public override Composite Rotation
        {
            get
            {
                return new PrioritySelector(
                    new Decorator(ret => SpellManager.GlobalCooldown || Me.IsCasting || Me.IsChanneling || !Me.IsValid, new ActionAlwaysSucceed()),
                    CachedUnits.Pulse,
                    Common.SetCounts, // Set counts for our AoE trigger
                    Common.Combat(), // Handles all of our utility / talent abilities.

                    // Ember Tap
                    Spell.Cast("Ember Tap", on => Me, ret => Settings.Destruction_UseEmberTap &&  Me.HealthPercent <= Settings.Destruction_EmberTapHPValue && CurrentBurningEmbers >= 1),

                    // Dark Soul
                    HandleDarkSoul(),

                    // AoE selector
                    new Decorator(ret =>
                        Common._nearbyAoEUnitCount <= Settings.Destruction_AoEUnitCount || !Settings.Destruction_UseAoEAbilities || Me.Level <= 53, HandleSingleTarget()),
                    new Decorator(ret => Settings.Destruction_UseAoEAbilities &&
                        (Common._nearbyAoEUnitCount >= Settings.Destruction_AoEUnitCount && Me.Level >= 54), HandleAoECombat())
                    );
            }

        }

        private static Composite HandleDarkSoul()
        {
            /*
           0,On Cooldown
           1,On Boss Or Player
           2,On Target Low HP
            */
            return new Decorator(ret => Settings.Destruction_UseDarkSoulInstability,
                new PrioritySelector(
                    
                new Decorator(ret => Settings.Destruction_DarkSoulCondition == 0,
                    Spell.Cast("Dark Soul: Instability",
                        ret => CurrentBurningEmbers >= Settings.Destruction_MinimumEmbersForDarkSoul
                        && Me.CurrentTarget != null)
                    ),
                new Decorator(ret => Settings.Destruction_DarkSoulCondition == 1,
                    Spell.Cast("Dark Soul: Instability",
                        ret => CurrentBurningEmbers >= Settings.Destruction_MinimumEmbersForDarkSoul
                                && Me.CurrentTarget != null 
                                && Me.CurrentTarget.IsBossOrPlayer())
                    ),
                new Decorator(ret => Settings.Destruction_DarkSoulCondition == 2,
                    Spell.Cast("Dark Soul: Instability",
                        ret => CurrentBurningEmbers >= Settings.Destruction_MinimumEmbersForDarkSoul
                                && Me.CurrentTarget != null
                                && Me.CurrentTarget.HealthPercent <= Settings.Destruction_DarkSoulLowHPValue)
                    )));
                       

                
        }

        private static Composite HandleSingleTarget()
        {
            return new PrioritySelector(
                new Decorator(ret => Me.HasAura(SpellId.FireandBrimstone),
                    new Action(delegate { Me.CancelAura("Fire and Brimstone"); return RunStatus.Failure; })),

                Spell.PreventDoubleCast("Immolate", 2, on => Me.CurrentTarget, ret => NeedImmolate, UseKilJaedensCunningPassive),

                new Decorator(ret => Settings.Destruction_UseAoEAbilities, new PrioritySelector(
                    HavocAoEHandler(),
                    RainOfFireAllUnits()
                    )),

                Spell.CastOnGround(SpellId.RainofFire, ret => Me.CurrentTarget.Location, ret => Settings.Destruction_RoFSingleTarget && RainOfFireConditions),
                Spell.PreventDoubleCast("Shadowburn", 0.5, on => Me.CurrentTarget, ret => NeedShadowburn, UseKilJaedensCunningPassive),
                Spell.Cast("Conflagrate", ret => !ChaosBoltAura && CurrentBurningEmbers < 3.5),
                Spell.PreventDoubleCast("Chaos Bolt", 0.5, on => Me.CurrentTarget, ret => NeedChaosBolt, UseKilJaedensCunningPassive),
                Spell.Cast("Conflagrate"),
                Spell.PreventDoubleCast("Incinerate", 0.5, on => Me.CurrentTarget, ret => !NeedImmolate, UseKilJaedensCunningPassive)
                );
        }

        private static Composite HandleAoECombat()
        {
            return new PrioritySelector(

                new Decorator(ret => CurrentBurningEmbers < 1 && Me.HasAura(SpellId.FireandBrimstone),
                    new Action(delegate { Me.CancelAura("Fire and Brimstone"); return RunStatus.Failure; })),

                Spell.CastOnGround(SpellId.RainofFire, ret => Me.CurrentTarget.Location, ret => RainOfFireConditions),

                HavocAoEHandler(),
                RainOfFireAllUnits(),

                Spell.PreventDoubleCast("Fire and Brimstone", 0.5, on => Me, ret => !Me.HasAura(SpellId.FireandBrimstone) && CurrentBurningEmbers > 1, true),
                Spell.PreventDoubleCast("Immolate", 3, on => Me.CurrentTarget, ret => Me.HasAura(SpellId.FireandBrimstone) && (NeedImmolate || NeedAoEImmolate), UseKilJaedensCunningPassive),
                Spell.PreventDoubleCast("Conflagrate", 0.5, on => Me.CurrentTarget, ret => Me.HasAura(SpellId.FireandBrimstone) && (!NeedAoEImmolate && !NeedImmolate), true),

                // Multi DoT Immolate (for low embers)
                Spell.MultiDoT("Immolate", Me.CurrentTarget, 38, 3, ret => !Me.HasAura(SpellId.FireandBrimstone) && CurrentBurningEmbers < 1),

                // Filler
                Spell.PreventDoubleCast("Incinerate", 0.5, on => Me.CurrentTarget, ret => true, UseKilJaedensCunningPassive)
                );
        }

        private static Composite HavocAoEHandler()
        {
            return new PrioritySelector(
                new Decorator(ret => Settings.Destruction_UseHavoc && (!Settings.Destruction_OnlyHavocWithCB || Settings.Destruction_OnlyHavocWithCB && CurrentBurningEmbers >= 1) && SpellManager.CanCast(SpellId.Havoc),
                        Spell.PreventDoubleCast("Havoc", 0.5, on => HavocTarget(), ret => HavocTarget() != null, true)),

                new Decorator(ret => Me.HasAura("Havoc") && Spell.GetAuraStackCount(SpellId.Havoc, Me.CurrentTarget) <= 2 && CurrentBurningEmbers >= 1,
                    Spell.PreventDoubleCast("Shadowburn", 0.5, on => BestShadowburnUnit(), ret => BestShadowburnUnit() != null, true)),

                new Decorator(ret => Settings.Destruction_OnlyHavocWithCB && Spell.GetAuraStackCount(SpellId.Havoc, Me.CurrentTarget) == 3 && CurrentBurningEmbers >= 1,
                    Spell.PreventDoubleCast("Chaos Bolt", 0.5, ret => !Me.CurrentTarget.HasAura("Havoc"), UseKilJaedensCunningPassive))
                );

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

        private static double CurrentBurningEmbers { get { return (double)Me.GetPowerInfo(WoWPowerType.BurningEmbers).Current / 10; /*Styx.WoWInternals.Lua.PlayerUnitPower("SPELL_POWER_BURNING_EMBERS");*/ } }
        private static bool NeedShadowburn { get { return CurrentBurningEmbers >= 1 && Me.CurrentTarget.HealthPercent <= 20; } }
        private static bool NeedImmolate { get { return !Me.CurrentTarget.HasMyAura("Immolate") || (Me.CurrentTarget.HasMyAura("Immolate") && Spell.GetMyAuraTimeLeft("Immolate", Me.CurrentTarget) <= 7.5); } }
        private static bool NeedAoEImmolate
        {
            get
            {
                return
                    Unit.CachedNearbyAttackableUnits(Me.CurrentTarget.Location, 15).FirstOrDefault(x => !x.HasMyAura("Immolate")) != null && Me.HasAura(SpellId.RainofFire);
            }
        }
        private static bool NeedChaosBolt
        {
            get
            {
                return Me.CurrentTarget.HealthPercent > 20 &&
                    (CurrentBurningEmbers >= 1 && (!Me.HasAura(SpellId.Backdraft) || ChaosBoltAura)) || CurrentBurningEmbers >= Settings.Destruction_ChaosBoltValue;
            }
        }
        private static bool ChaosBoltAura { get { return Me.HasAnyAuras(SpellId.DarkSoulInstability, SpellId.SkullBanner, 138963 /*Perfect Aim*/); } }
        private static bool UseKilJaedensCunningPassive { get { return TalentManager.HasTalent(17); } }
        private static bool RainOfFireConditions { get { return Me.CurrentTarget != null && Me.ManaPercent >= 50 && !Me.CurrentTarget.IsMoving && Me.CurrentTarget.Distance <= 35 && !Me.CurrentTarget.HasAura("Rain of Fire") && !Me.CurrentTarget.IsFriendly && !Me.CurrentTarget.IsDead; } }

        #endregion

        #region Units
        private static Composite RainOfFireAllUnits()
        {
            return new Decorator(ret => Settings.Destruction_RoFEverythingInRange && Me.ManaPercent >= 30,
                new PrioritySelector(ctx => Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 34).FirstOrDefault(x => x.IsValid && !x.HasAura("Rain of Fire") && !x.IsMoving && (x.HealthPercent >= 15 || x.IsBoss()) && !x.IsPet),
                    Spell.CastOnGround(SpellId.RainofFire, on => ((WoWUnit)on).Location, ret => ((WoWUnit)ret) != null)
                ));
        }

        private static WoWUnit HavocTarget()
        {
            return Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 35)
                    .Where(t => t.IsValid && t != Me.CurrentTarget)
                    .OrderByDescending(t => t.CurrentHealth) // We want highest HP value, not percent
                    .FirstOrDefault(x => !x.IsPet); // don't cast on our current target.
        }

        private static WoWUnit BestShadowburnUnit()
        {
            // See if we can use our current target first
            if (Me.CurrentTarget.HealthPercent < 20 && !Me.CurrentTarget.HasAura("Havoc"))
            {
                return Me.CurrentTarget;
            }

            // Can't use current target, scan for one.
            var bestHostileEnemy = Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 40)
                                       .Where(t => t.IsValid && t.HealthPercent < 20)
                                       .OrderBy(t => t.HealthPercent)
                                       .FirstOrDefault(t => !t.HasAura("Havoc") && !t.IsPet) ??
                                   Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 40)
                                       .Where(t => t.IsValid && t.HealthPercent < 20)
                                       .OrderBy(t => t.HealthPercent)
                                       .FirstOrDefault();

            return bestHostileEnemy;
        }
        #endregion

    }
}

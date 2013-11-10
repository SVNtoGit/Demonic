using System;
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
using Demonic.Helpers;
using Action = Styx.TreeSharp.Action;

namespace Demonic.Specialisation
{
    [UsedImplicitly]
    internal class Demonology : RotationBase
    {

        #region RotationBase Overrides
        public override WoWSpec KeySpec { get { return WoWSpec.WarlockDemonology; } }
        public override string Name { get { return "Demonology"; } }
        public override string Revision { get { return "$Rev: 42 $"; } }
        #endregion
        private static DemonicSettings Settings { get { return DemonicSettings.Instance; } }
        private delegate T Selection<out T>(object context);

        #region Honorbuddy's Rotation Overrides
        public override Composite Rotation
        {
            get
            {
                return new PrioritySelector(
                    new Decorator(ret => SpellManager.GlobalCooldown || Me.IsCasting || Me.IsChanneling || !Me.IsValid, new ActionAlwaysSucceed()),
                    CachedUnits.Pulse,
                    SetCounts, // Set counts for our AoE trigger
                    Common.Combat(), // Handles all of our utility / talent abilities.

                    HandleDarkSoulKnowledge(),

                    // AoE selector
                    new Decorator(ret => !NeedAoEMode, HandleSingleTarget()),
                    new Decorator(ret => NeedAoEMode && 
                        (_nearbyAoEUnitsNearMeCount >= Settings.Demonology_AoEHighUnitCount 
                            || _nearbyAoEUnitCount >= Settings.Demonology_AoEHighUnitCount), 
                        HandleAoEHighUnitCount()),
                    new Decorator(ret => NeedAoEMode && 
                        (_nearbyAoEUnitsNearMeCount >= Settings.Demonology_AoELowUnitCount 
                            || _nearbyAoEUnitCount >= Settings.Demonology_AoELowUnitCount), 
                        HandleAoELowUnitCount())
                    );
            }
        }

        private static Composite HandleSingleTarget()
        {

            return new PrioritySelector(
                HandleShapeShifting(),

            #region Shape shifted
                Spell.PreventDoubleCast("Shadow Bolt", 0.5, on => Me.CurrentTarget, ret => Me.HasAura("Metamorphosis") && Me.CurrentTarget.HasAura("Corruption") && Spell.GetAuraTimeLeft("Corruption", Me.CurrentTarget) <= 5, true),
                Spell.CastHack("Metamorphosis: Doom", "Doom", on => Me.CurrentTarget, ret => NeedDoom),
                Spell.PreventDoubleCast("Soul Fire", 1.3, on => Me.CurrentTarget, ret => Me.HasAura("Metamorphosis") && (_haveMoltenCoreBuff || Me.CurrentTarget.HealthPercent < 25) && (Me.HasAnyAuras(SpellId.DarkSoulKnowledge, SpellId.SkullBanner) || Me.HasAura("Perfect Aim")), UseKilJaedensCunningPassive),
                Spell.PreventDoubleCast("Shadow Bolt", 0.1, on => Me.CurrentTarget, ret => Me.HasAura("Metamorphosis"), true), //Touch of Chaos
            #endregion

            #region Not-Shapeshifted
                Spell.Cast("Corruption", ret => NeedCorruption),
                Spell.PreventDoubleCast("Hand of Gul'dan", 2, ret => !TalentManager.HasGlyph("Hand of Gul'dan") && NeedHandofGuldan),
                Spell.CastOnGround("Hand of Gul'dan", on => Me.CurrentTarget.Location, ret => TalentManager.HasGlyph("Hand of Gul'dan") && NeedHandofGuldan, true),
                Spell.PreventDoubleCast("Soul Fire", 1.3, on => Me.CurrentTarget, ret => !Me.HasAura("Metamorphosis") && (_haveMoltenCoreBuff || Me.CurrentTarget.HealthPercent < 25), UseKilJaedensCunningPassive),
                Spell.PreventDoubleCast("Shadow Bolt", 0.5, on => Me.CurrentTarget, ret => !Me.HasAura("Metamorphosis") && Me.CurrentTarget.HasMyAura("Corruption") && !DemonicFuryDump, UseKilJaedensCunningPassive),
            #endregion

                Spell.Cast("Life Tap", ret => Me.HealthPercent > 50 && (Me.ManaPercent < 20 || Common.CanLifeTap && Me.IsMoving())),
                Spell.Cast("Fel Flame", ret => Me.IsMoving() && !UseKilJaedensCunningPassive));
        }

        private static Composite HandleAoELowUnitCount()
        {
            return new PrioritySelector(
                new Decorator(ret => Me.HasAura("Hellfire"), CancelHellfireAura()),
                HandleShapeShifting(false),

                Spell.PreventDoubleCast("Shadow Bolt", 0.5, on => Me.CurrentTarget, ret => Me.HasAura("Metamorphosis") && Me.CurrentTarget.HasAura("Corruption") && Spell.GetAuraTimeLeft("Corruption", Me.CurrentTarget) <= 5, true),
                Spell.MultiDoTCastHack("Metamorphosis: Doom", "Doom", Me, 40, 41, ret => Me.HasAura("Perfect Aim")),
                Spell.MultiDoTCastHack("Metamorphosis: Doom", "Doom", Me, 40, 3, ret => Me.HasAura("Metamorphosis")),
                Spell.PreventDoubleMultiDoT("Corruption", 1, Me, 40, 3, ret => !Me.HasAura("Metamorphosis")),

                HandleSingleTarget()

                );
        }

        private static Composite HandleAoEHighUnitCount()
        {
            return new PrioritySelector(
            new Decorator(ret => Me.HasAura("Hellfire") && ((NeedMetamorphosis && CanMetamorphosis) || NeedMultiCorruption || NeedCorruption), CancelHellfireAura()),
            new Decorator(ret => Me.HasAura("Hellfire"), new ActionAlwaysSucceed()),
            HandleShapeShifting(false),

           new Decorator(ret => !Me.HasAura("Metamorphosis"),
                new PrioritySelector(
                        Spell.Cast("Corruption", ret => NeedCorruption),
                        Spell.PreventDoubleCast("Hand of Gul'dan", 2, ret => !TalentManager.HasGlyph("Hand of Gul'dan") && NeedHandofGuldan),
                        Spell.CastOnGround("Hand of Gul'dan", on => Me.CurrentTarget.Location, ret => TalentManager.HasGlyph("Hand of Gul'dan") && NeedHandofGuldan, true),
                        Spell.PreventDoubleMultiDoT("Corruption", 1, Me, 40, 3, ret => true),
                        Spell.PreventDoubleChannel("Hellfire", 1, true, ret => !Me.IsChanneling && Me.HealthPercent >= 60 && (!NeedMetamorphosis || !CanMetamorphosis) && !NeedMultiCorruption),
                        Spell.PreventDoubleChannel("Drain Life", 0.5, true, on => Me.CurrentTarget, ret => !Me.IsChanneling && TalentManager.HasTalent(3) && !NeedMetamorphosis && !CanMetamorphosis && !NeedMultiCorruption)
                        )),

            new Decorator(ret => Me.HasAura("Metamorphosis"),
                new PrioritySelector(
                        Spell.CastHack("Metamorphosis: Doom", "Doom", on => Me.CurrentTarget, ret => NeedDoom),
                        Spell.PreventDoubleCast("Hellfire", 2, ret => !Me.HasAura("Immolation Aura") && CurrentDemonicFury >= 500),
                        Spell.FaceAndCast("Fel Flame"/*Void Ray - Refresh corruption*/, on => BestVoidRayUnit(), ret => BestVoidRayUnit() != null),
                        Spell.MultiDoTCastHack("Metamorphosis: Doom", "Doom", Me, 40, 20, ret => true),
                        Spell.FaceAndCast("Fel Flame"/*Void Ray - Any unit in range - Filler*/, on => BestVoidRayUnit(false), ret => BestVoidRayUnit(false) != null && !NeedMultiDoom),
                        Spell.PreventDoubleChannel("Drain Life", 0.5, true, on => Me.CurrentTarget, ret => !Me.IsChanneling && TalentManager.HasTalent(3) && !NeedMultiDoom)
                    )),


            Spell.Cast("Life Tap", ret => Me.HealthPercent > 50 && (Me.ManaPercent < 20 || Common.CanLifeTap && Me.IsMoving()))
            );

        }
        private static Composite HandleShapeShifting(bool singleTarget = true)
        {
            return new PrioritySelector(
                 Spell.PreventDoubleCast("Metamorphosis", 2, ret => singleTarget && NeedMetamorphosis && CanMetamorphosis),
                 Spell.PreventDoubleCast("Metamorphosis", 2, ret => !singleTarget && NeedMetamorphosis && CanMetamorphosis && (!NeedMultiCorruption || DemonicFuryDump)),
                 CancelMetamorphosisAura(ret => ((singleTarget && CancelMetamorphosis) || (!singleTarget && CancelMetamorphosisAoE))));
        }

        private static Composite HandleDarkSoulKnowledge()
        {
            /*
                0,On Metamorphosis Form
                1,On Metamorphosis Form AND Boss Or Player
                2,On Metamorphosis Form AND Target Low HP
                3,On Cooldown
                4,On Boss Or Player
                5,On Target Low HP
             */
            return new Decorator(ret => Settings.Demonology_UseDarkSoulKnowledge && Me.CurrentTarget != null,
                new PrioritySelector(
                    new Decorator(ret => Settings.Demonology_DarkSoulKnowledgeCondition == 0,
                        Spell.Cast("Dark Soul: Knowledge", ret => Me.HasAura("Metamorphosis") && !Me.HasAura("Dark Soul: Knowledge"))),

                    new Decorator(ret => Settings.Demonology_DarkSoulKnowledgeCondition == 1,
                        Spell.Cast("Dark Soul: Knowledge", ret => Me.HasAura("Metamorphosis") && Me.CurrentTarget.IsBossOrPlayer() && CurrentDemonicFury > Settings.Demonology_FuryCancelValue && !Me.HasAura("Dark Soul: Knowledge"))),

                    new Decorator(ret => Settings.Demonology_DarkSoulKnowledgeCondition == 2,
                        Spell.Cast("Dark Soul: Knowledge", ret => Me.HasAura("Metamorphosis") && Me.CurrentTarget.HealthPercent <= Settings.Demonology_DarkSoulKnowledgeLowHPValue && !Me.HasAura("Dark Soul: Knowledge"))),

                    new Decorator(ret => Settings.Demonology_DarkSoulKnowledgeCondition == 3,
                        Spell.Cast("Dark Soul: Knowledge", ret => Me.HasAura("Dark Soul: Knowledge"))),

                    new Decorator(ret => Settings.Demonology_DarkSoulKnowledgeCondition == 4,
                        Spell.Cast("Dark Soul: Knowledge", ret => Me.CurrentTarget.IsBossOrPlayer() && CurrentDemonicFury > Settings.Demonology_FuryCancelValue && !Me.HasAura("Dark Soul: Knowledge"))),

                    new Decorator(ret => Settings.Demonology_DarkSoulKnowledgeCondition == 5,
                        Spell.Cast("Dark Soul: Knowledge", ret => Me.CurrentTarget.HealthPercent <= Settings.Demonology_DarkSoulKnowledgeLowHPValue && !Me.HasAura("Dark Soul: Knowledge")))

                    
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
        
        private static double CurrentDemonicFury { get { return Me.GetPowerInfo(WoWPowerType.DemonicFury).CurrentI; } }

        private static bool NeedDoom 
        {
            get
            {
                return Me.Level >= 32 && Me.HasAura("Metamorphosis") && CurrentDemonicFury >= 70
                    && (!Me.CurrentTarget.HasAura("Doom") ||
                        Me.CurrentTarget.HasAura("Doom") && Spell.GetAuraTimeLeft("Doom", Me.CurrentTarget) < 29 ||
                        Me.HasAnyAura("Dark Soul: Knowledge", "Perfect Aim") && Me.CurrentTarget.HasAura("Doom") && Spell.GetAuraTimeLeft("Doom", Me.CurrentTarget) < 42);
            } 
        }

        private static bool DemonicFuryDump { get { return CurrentDemonicFury >= Settings.Demonology_FuryCastValue; } }

        private static bool CanMetamorphosis { get { return !Me.HasAura("Metamorphosis") && !Spell.SpellOnCooldown("Metamorphosis") && Me.CurrentTarget.HasAura("Corruption"); } }

        private static bool NeedMetamorphosis
        {
            get
            {
                return Me.HasAnyAura("Dark Soul: Knowledge", "Perfect Aim") ||
                        (!Me.CurrentTarget.HasMyAura("Doom") && Me.Level >= 32 && CurrentDemonicFury >= 70) ||
                         DemonicFuryDump ||
                         Me.CurrentTarget.HasAura("Corruption") && Spell.GetAuraTimeLeft("Corruption", Me.CurrentTarget) <= 4.5;
            }
        }
        private static bool CancelMetamorphosis { get { return Me.HasAura("Metamorphosis") && ((!Me.HasAnyAura("Dark Soul: Knowledge", "Perfect Aim") && CurrentDemonicFury <= Settings.Demonology_FuryCancelValue && Me.CurrentTarget.HasMyAura("Doom")) || !Me.CurrentTarget.HasAura("Corruption")); } }
        private static bool CancelMetamorphosisAoE { get { return CancelMetamorphosis && !Me.HasAura("Immolation Aura") && !NeedMultiDoom; } }

        private static bool NeedCorruption
        {
            get
            {
                return Me.CurrentTarget != null &&
                    (!Me.CurrentTarget.HasAura("Corruption") ||
                     Me.CurrentTarget.HasAura("Corruption") && Spell.GetAuraTimeLeft("Corruption", Me.CurrentTarget) <= 3);
            }
        }
        private static bool NeedHandofGuldan { get { return !Me.HasAura("Metamorphosis") && !Me.CurrentTarget.MovementInfo.IsMoving && !Me.CurrentTarget.HasAura("Shadowflame"); } }
        private static bool UseKilJaedensCunningPassive { get { return TalentManager.HasTalent(17); } }
        private static bool NeedMultiCorruption { get { return Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 40).Any(x => !x.HasAura("Corruption")); } }
        private static bool NeedMultiDoom { get { return Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 40).Any(x => !x.HasAura("Doom")); } }
        private static int _nearbyAoEUnitCount;
        private static int _nearbyAoEUnitsNearMeCount;
        private static bool _haveMoltenCoreBuff;
        private static Composite SetCounts
        {
            get
            {
                return new Action(delegate
                {
                    try
                    {
                        if (Me.CurrentTarget != null)
                        {
                            var countList = Unit.CachedNearbyAttackableUnits(Me.CurrentTarget.Location, 15);
                            var nearMeList = Unit.CachedNearbyAttackableUnits(Me.Location, 20);

                            _nearbyAoEUnitCount = countList != null ? countList.Count() : 0;
                            _nearbyAoEUnitsNearMeCount = nearMeList != null ? nearMeList.Count() : 0;
                            _haveMoltenCoreBuff = Lua.PlayerCountBuff("Molten Core") > 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("Unit detection failed with: {0}", ex);
                        _nearbyAoEUnitCount = 0;
                    }
                    return RunStatus.Failure;
                });
            }
        }
        private static bool NeedAoEMode
        {
            get
            {
                return Settings.Demonology_EnableAoEAbilities && (
                    _nearbyAoEUnitsNearMeCount >= Settings.Demonology_AoELowUnitCount ||
                       _nearbyAoEUnitCount >= Settings.Demonology_AoELowUnitCount);
            }
        }

        #endregion

        #region Units

        private static WoWUnit BestVoidRayUnit(bool onlyForRefresh = true)
        {
            // Get a unit that needs corruption refreshing
            var bestUnit = Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 20)
                        .Where(x => x != null && x.HasAura("Corruption") && Spell.GetAuraTimeLeft("Corruption", x) <= 15000)
                        .OrderBy(x => Spell.GetAuraTimeLeft("Corruption", x))
                        .FirstOrDefault();

            // Can't refresh corruption, pick our current target if it's close enough.
            if (bestUnit == null && !onlyForRefresh)
            {
                if (Me.CurrentTarget != null && Me.CurrentTarget.Distance <= 20)
                {
                    return Me.CurrentTarget;
                }

                // If current target is no good, pick one thats close enough.
                bestUnit = Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 20)
                    .FirstOrDefault(x => x != null);
            }

            return bestUnit;

        }

        #endregion

        #region Cancel Auras

        private static Composite CancelMetamorphosisAura(Selection<bool> reqs = null)
        {
            return new Decorator(ret => ((reqs != null && reqs(ret)) || (reqs == null)),
            new Action(delegate
            {
                Log.Info("Cancelling Metamorphosis Form");
                Me.CancelAura("Metamorphosis"); return RunStatus.Failure;
            }));
        }

        private static Composite CancelHellfireAura()
        {
            return new Action(delegate
            {
                Log.Info("Cancelling Hellfire");
                Me.CancelAura("Hellfire"); return RunStatus.Failure;
            });
        }

        #endregion
    }
}

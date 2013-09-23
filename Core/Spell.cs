/*
 * 
 *  Spell.cs
 *  This file is mostly taken from PureRotation with minor edits, so credits to the PureRotation team.
 *  
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using CommonBehaviors.Actions;
using Styx;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Demonic.Helpers;
using Action = Styx.TreeSharp.Action;

namespace Demonic.Core
{

    internal static class Spell
    {      

        public delegate WoWUnit UnitSelectionDelegate(object context);
        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        public static bool IsChanneling { get { return StyxWoW.Me.IsChanneling && StyxWoW.Me.ChanneledCastingSpellId != 0; } }
        public static string Lastspellcast;
        internal delegate T Selection<out T>(object context);
        public static readonly HashSet<int> HeroismBuff = new HashSet<int>
        {
            32182, //Bloodlust - Shaman (Horde)
            2825, // Heroism - Shaman (Alliance)
            80353, // Time Warp - Mages
            90355 // Ancient Hysteria - Core Hound - Hunter Pet
        };


        #region Double Cast

        static readonly Dictionary<string, DoubleCastSpell> DoubleCastEntries = new Dictionary<string, DoubleCastSpell>();
        
        private static void UpdateDoubleCastEntries(string spellName, double expiryTime)
        {
            if (DoubleCastEntries.ContainsKey(spellName)) DoubleCastEntries[spellName] = new DoubleCastSpell(spellName, expiryTime, DateTime.UtcNow);
            if (!DoubleCastEntries.ContainsKey(spellName)) DoubleCastEntries.Add(spellName, new DoubleCastSpell(spellName, expiryTime, DateTime.UtcNow));
        }

        internal static void PulseDoubleCastEntries()
        {
            DoubleCastEntries.RemoveAll(t => DateTime.UtcNow.Subtract(t.DoubleCastCurrentTime).TotalSeconds >= t.DoubleCastExpiryTime);
        }

        public static Composite PreventDoubleCast(string spell, double expiryTime, Selection<bool> reqs = null, bool ignoreMoving = false, string reason = null)
        {
            return PreventDoubleCast(spell, expiryTime, ret => StyxWoW.Me.CurrentTarget, reqs, ignoreMoving, reason);
        }

        public static Composite PreventDoubleCast(string spell, double expiryTime, UnitSelectionDelegate onUnit, Selection<bool> reqs = null, bool ignoreMoving = false, string reason = null)
        {
            return
                new Decorator(
                    ret => ((reqs != null && reqs(ret)) || (reqs == null)) && onUnit != null && onUnit(ret) != null 
                            && SpellManager.CanCast(spell, onUnit(ret), true, !ignoreMoving)
                            && !DoubleCastEntries.ContainsKey(spell + onUnit(ret).Guid),
                    new Sequence(
                        new Action(a => SpellManager.Cast(spell, onUnit(a))),
                        new Action(a => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture) + onUnit(a).Guid, expiryTime)),
                        new Action(ret => Log.Info(String.Format("[Casting: {0}] [On: {1}]", spell, onUnit(ret).IsPlayer ? onUnit(ret).Class.ToString() : onUnit(ret).Name)))
                        ));
        }

        public static Composite GlobalPreventDoubleCast(string spell, double expiryTime, UnitSelectionDelegate onUnit, Selection<bool> reqs = null, bool ignoreMoving = false, string reason = null)
        {
            return
                new Decorator(
                    ret => ((reqs != null && reqs(ret)) || (reqs == null)) && onUnit != null && onUnit(ret) != null
                            && SpellManager.CanCast(spell, onUnit(ret), true, !ignoreMoving)
                            && !DoubleCastEntries.ContainsKey(spell),
                    new Sequence(
                        new Action(a => SpellManager.Cast(spell, onUnit(a))),
                        new Action(a => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture), expiryTime)),
                        new Action(ret => Log.Info(String.Format("[Casting: {0}] [On: {1}]", spell, onUnit(ret).IsPlayer ? onUnit(ret).Class.ToString() : onUnit(ret).Name)))
                        ));
        }

        public static Composite PreventDoubleCast(int spell, double expiryTime, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return
                new Decorator(
                    ret => ((reqs != null && reqs(ret)) || (reqs == null)) && onUnit != null && onUnit(ret) != null
                           && !DoubleCastEntries.ContainsKey(spell.ToString(CultureInfo.InvariantCulture) + onUnit(ret).Guid),
                    new Sequence(
                        new Action(a => SpellManager.Cast(spell, onUnit(a))),
                        new Action(a => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture) + onUnit(a).Guid, expiryTime))));
        }

        public static Composite PreventDoubleCastNoCanCast(string spell, double expiryTime, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return
                new Decorator(
                    ret => ((reqs != null && reqs(ret)) || (reqs == null)) && onUnit != null && onUnit(ret) != null && !DoubleCastEntries.ContainsKey(spell + onUnit(ret).Guid),
                    new Sequence(
                        new Action(a => SpellManager.Cast(spell, onUnit(a))),
                        new Action(a => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture) + onUnit(a).Guid, expiryTime))));
        }

        public static Composite PreventDoubleCast(int spell, double expiryTime, Selection<bool> reqs = null )
        {
            return PreventDoubleCast(spell, expiryTime, target => Me.CurrentTarget, reqs);
        }

        public static Composite PreventDoubleChannel(string spell, double expiryTime, bool checkCancast, Selection<bool> reqs = null)
        {
            return PreventDoubleChannel(spell, expiryTime, checkCancast, onUnit => StyxWoW.Me.CurrentTarget, reqs);
        }

        public static Composite PreventDoubleChannel(string spell, double expiryTime, bool checkCancast, UnitSelectionDelegate onUnit, Selection<bool> reqs, bool ignoreMoving = false, string reason = null)
        {
            return new Decorator(
                delegate(object a)
                    {
                        if (IsChanneling)
                            return false;
                    
                        if (!reqs(a))
                            return false;

                        if (onUnit != null && DoubleCastEntries.ContainsKey(spell + onUnit(a).Guid))
                            return false;

                        if (onUnit != null && (checkCancast && !SpellManager.CanCast(spell, onUnit(a), true, !ignoreMoving)))
                            return false;

                        return true;
                    },
                new Sequence(
                    new Action(a => SpellManager.Cast(spell, onUnit(a))),
                    new Action(a => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture) + onUnit(a).Guid, expiryTime)),
                    new Action(ret => Log.Info(String.Format("[Casting: {0}] [On: {1}]", spell, onUnit(ret).IsPlayer ? onUnit(ret).Class.ToString() : onUnit(ret).Name)))
                    ));
        }


        public static Composite PreventDoubleCastOnGround(string spell, double expiryTime, LocationRetriever onLocation)
        {
            return PreventDoubleCastOnGround(spell, expiryTime, onLocation, ret => true);
        }

        public static Composite PreventDoubleCastOnGround(string spell, double expiryTime, LocationRetriever onLocation, CanRunDecoratorDelegate requirements, bool waitForSpell = false)
        {
            return new Decorator(
                    ret =>
                    onLocation != null && requirements(ret) && SpellManager.CanCast(spell) &&
                    !BossList.IgnoreAoE.Contains(StyxWoW.Me.CurrentTarget.Entry) &&
                    (StyxWoW.Me.Location.Distance(onLocation(ret)) <= SpellManager.Spells[spell].MaxRange ||
                     SpellManager.Spells[spell].MaxRange == 0) && !DoubleCastEntries.ContainsKey(spell.ToString(CultureInfo.InvariantCulture) + onLocation(ret)), 
                    new Sequence(
                        new Action(ret => SpellManager.Cast(spell)),

                        new DecoratorContinue(ctx => waitForSpell,
                            new WaitContinue(1,
                                ret =>
                                StyxWoW.Me.CurrentPendingCursorSpell != null &&
                                StyxWoW.Me.CurrentPendingCursorSpell.Name == spell, new ActionAlwaysSucceed())),

                        new Action(ret => SpellManager.ClickRemoteLocation(onLocation(ret))),
                        new Action(ret => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture) + onLocation(ret), expiryTime))));
        }

        public static Composite GlobalPreventDoubleCastOnGround(string spell, double expiryTime, LocationRetriever onLocation, CanRunDecoratorDelegate requirements)
        {
            return new Decorator(
                    ret =>
                    onLocation != null && requirements(ret) && SpellManager.CanCast(spell) 
                    && !DoubleCastEntries.ContainsKey(spell.ToString(CultureInfo.InvariantCulture)),
                    new Sequence(
                        new Action(ret => SpellManager.Cast(spell)),
                        new Action(ret => SpellManager.ClickRemoteLocation(onLocation(ret))),
                        new Action(ret => UpdateDoubleCastEntries(spell.ToString(CultureInfo.InvariantCulture), expiryTime))));
        }

        struct DoubleCastSpell
        {
            public DoubleCastSpell(string spellName, double expiryTime, DateTime currentTime)
                : this()
            {

                DoubleCastSpellName = spellName;
                DoubleCastExpiryTime = expiryTime;
                DoubleCastCurrentTime = currentTime;
            }

            private string DoubleCastSpellName { get; set; }
            public double DoubleCastExpiryTime { get; set; }
            public DateTime DoubleCastCurrentTime { get; set; }
        }

        #endregion

        #region Cast - by name

        public static Composite Cast(string spell, Selection<bool> reqs = null, string reason = null)
        {
            return Cast(spell, ret => StyxWoW.Me.CurrentTarget, reqs);
        }

        public static Composite Cast(string spell, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return
                new Decorator(
                    ret => (onUnit != null &&
                        onUnit(ret) != null &&
                        (reqs == null || reqs(ret)) &&
                        SpellManager.CanCast(spell, onUnit(ret))),
                    new Sequence(
                        new Action(ret => SpellManager.Cast(spell, onUnit(ret))),
                        new Action(ret => Log.Info(String.Format("[Casting: {0}] [On: {1}]", spell, onUnit(ret).IsPlayer ? onUnit(ret).Class.ToString() : onUnit(ret).Name))),
                        new Action(ret => Lastspellcast = spell))
                    );
        }

        /// <summary>
        /// used for warlocks and shit like this Spell.CastHack("Metamorphosis: Doom", "Doom", on => Me.CurrentTarget, ret =>  NeedDoom));
        /// </summary>
        /// <param name="canCastName">1st is the spell name SpellManager expects</param>
        /// <param name="castName">2nd is the spell the game expects</param>
        /// <returns></returns>
        public static Composite CastHack(string canCastName, string castName, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return
                new Decorator(ret => castName != null && canCastName != null && ((reqs != null && reqs(ret)) || (reqs == null)) && onUnit != null && onUnit(ret) != null
                    && SpellManager.CanCast(canCastName, onUnit(ret), true, false),
                new Sequence(
                    new Action(ret => SpellManager.Cast(castName, onUnit(ret))),
                    new Action(ret => Log.Info(String.Format("[Casting: {0}] [On: {1}]", castName, onUnit(ret).IsPlayer ? onUnit(ret).Class.ToString() : onUnit(ret).Name))),
                    new Action(ret => Lastspellcast = castName)));
        }

        /// <summary>
        /// Face the target before casting spell
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="onUnit"></param>
        /// <param name="reqs"></param>
        /// <returns></returns>
        public static Composite FaceAndCast(string spell, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return
                new Decorator(
                    ret => (onUnit != null && onUnit(ret) != null && Me.CurrentTarget != null &&
                        ((reqs != null && reqs(ret)) || (reqs == null)) &&
                        SpellManager.CanCast(spell, onUnit(ret))),
                    new Sequence(
                // Face Target
                        new Action(ret => onUnit(ret).Face()),
                // Wait until we're facing.
                        new WaitContinue(TimeSpan.FromMilliseconds(500), ret => Me.IsSafelyFacing(onUnit(ret)), new ActionAlwaysSucceed()),
                // Cast Spell
                        new Action(ret => SpellManager.Cast(spell, onUnit(ret))),
                        new Action(ret => Log.Info(String.Format("[Casting: {0}] [On: {1}]", spell, onUnit(ret).IsPlayer ? onUnit(ret).Class.ToString() : onUnit(ret).Name))),
                        new Action(ret => Lastspellcast = spell)
                        ));
        }

        #endregion

        #region Cast - by ID

        public static Composite Cast(int spell, Selection<bool> reqs = null)
        {
            return Cast(spell, ret => StyxWoW.Me.CurrentTarget, reqs);
        }

        public static Composite Cast(int spell, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return
                new Decorator(
                    ret => ((reqs != null && reqs(ret)) || (reqs == null)) && onUnit != null && onUnit(ret) != null && SpellManager.CanCast(spell, onUnit(ret)),
                    new Action(ret => SpellManager.Cast(spell, onUnit(ret))));
        }

        /// <summary>
        /// Holy Word: Serenity just would not cast any other way.
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="onUnit"></param>
        /// <param name="reqs"></param>
        /// <returns></returns>
        public static Composite ForceCast(int spell, UnitSelectionDelegate onUnit, Selection<bool> reqs = null, string reason = null)
        {
            return
                new Decorator(
                    ret => ((reqs != null && reqs(ret)) || (reqs == null)) && onUnit != null && onUnit(ret) != null && !WoWSpell.FromId(spell).Cooldown 
                           && onUnit(ret).Distance < 40 && onUnit(ret).InLineOfSpellSight,
                           new Sequence(
                               new Action(ret => SpellManager.Cast(spell, onUnit(ret))),
                               new Action(ret => Log.Info(String.Format("[Casting: {0}] [On: {1}]", WoWSpell.FromId(spell).Name, onUnit(ret).IsPlayer ? onUnit(ret).Class.ToString() : onUnit(ret).Name)))
                            ));
        }


        #endregion

        #region Cast - Multi DoT

        /// <summary> Multi-DoT targets within range of target</summary>
        public static Composite MultiDoT(string spellName, WoWUnit unit, Selection<bool> reqs = null)
        {
            return MultiDoT(spellName, unit, 15, 1, reqs);
        }

        /// <summary> Multi-DoT targets within range of target</summary>
        public static Composite MultiDoT(string spellName, WoWUnit unit, double radius, double refreshDurationRemaining, Selection<bool> reqs = null)
        {
            WoWUnit dotTarget = null;
            return new Decorator(ret => unit != null && ((reqs != null && reqs(ret)) || reqs == null),
                              new PrioritySelector(ctx => dotTarget = Unit.GetMultiDoTTarget(unit, spellName, radius, refreshDurationRemaining),
                                  PreventDoubleCast(spellName, GetSpellCastTime(spellName) + 0.5, on => dotTarget, ret => dotTarget != null)));
        }

        /// <summary> Multi-DoT targets within range of target</summary>
        public static Composite PreventDoubleMultiDoT(string spellName, double expiryTime, WoWUnit unit, double radius, double refreshDurationRemaining, Selection<bool> reqs = null)
        {
            WoWUnit dotTarget = null;
            return new Decorator(ret => unit != null && ((reqs != null && reqs(ret)) || reqs == null),
                              new PrioritySelector(ctx => dotTarget = Unit.GetMultiDoTTarget(unit, spellName, radius, refreshDurationRemaining),
                                  PreventDoubleCast(spellName, expiryTime, on => dotTarget, ret => dotTarget != null)));
        }

        /// <summary>
        /// Multi-DoT targets within range of unit - allow different "can cast name" to "cast name"
        /// </summary>
        /// <param name="canCastName">The spell value HB expects</param>
        /// <param name="spellName">The spell value the game expects</param>
        /// <param name="unit">DoT units around which unit?</param>
        /// <param name="radius">Radius</param>
        /// <param name="refreshDurationRemaining">Duration remaining to refresh dot</param>
        /// <param name="reqs">requirements</param>
        /// <returns></returns>
        public static Composite MultiDoTCastHack(string canCastName, string spellName, WoWUnit unit, double radius, double refreshDurationRemaining, Selection<bool> reqs = null)
        {
            WoWUnit dotTarget = null;
            return new Decorator(ret => unit != null && ((reqs != null && reqs(ret)) || reqs == null),
                              new PrioritySelector(ctx => dotTarget = Unit.GetMultiDoTTarget(unit, spellName, radius, refreshDurationRemaining),
                                  CastHack(canCastName, spellName, on => dotTarget, ret => dotTarget != null)));
        }

        #endregion Cast - Multi DoT

        #region ChanneledSpell - by name

        public static Composite ChannelSpell(string spell, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return
                new Decorator(
                    ret => ((reqs != null && reqs(ret)) || (reqs == null)) && onUnit != null && onUnit(ret) != null && SpellManager.CanCast(spell, onUnit(ret)) && !IsChanneling,
                    new Action(ret => SpellManager.Cast(spell, onUnit(ret))));
        }

        #endregion

        #region ChanneledSpell - by ID

        public static Composite ChannelSpell(int spell, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return
                new Decorator(
                    ret => ((reqs != null && reqs(ret)) || (reqs == null)) && onUnit != null && onUnit(ret) != null && SpellManager.CanCast(spell, onUnit(ret)) && !IsChanneling,
                    new Action(ret => SpellManager.Cast(spell, onUnit(ret))));
        }

        #endregion

        #region CastOnGround - placeable spell casting

        #region Delegates

        public delegate WoWPoint LocationRetriever(object context);

        #endregion

        /// <summary>
        ///   Creates a behavior to cast a spell by name, on the ground at the specified location.
        ///   Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        public static Composite CastOnGround(string spell, LocationRetriever onLocation)
        {
            return CastOnGround(spell, onLocation, ret => true);
        }

        public static Composite CastOnGround(string spell, LocationRetriever onLocation, CanRunDecoratorDelegate requirements, bool waitForSpell = false)
        {
            return
                new Decorator(
                    ret =>
                    onLocation != null && requirements(ret) && SpellManager.CanCast(spell) &&
                    !BossList.IgnoreAoE.Contains(StyxWoW.Me.CurrentTarget.Entry) &&
                    (StyxWoW.Me.Location.Distance(onLocation(ret)) <= SpellManager.Spells[spell].MaxRange ||
                     SpellManager.Spells[spell].MaxRange == 0),  //&& PRSettings.Instance.UseAoEAbilities  && GameWorld.IsInLineOfSpellSight(StyxWoW.Me.Location, onLocation(ret))
                    new Sequence(
                       new Action(ret => SpellManager.Cast(spell)),

                        new DecoratorContinue(ctx => waitForSpell,
                            new WaitContinue(1,
                                ret =>
                                StyxWoW.Me.CurrentPendingCursorSpell != null &&
                                StyxWoW.Me.CurrentPendingCursorSpell.Name == spell, new ActionAlwaysSucceed())),

                        new Action(ret => SpellManager.ClickRemoteLocation(onLocation(ret))),
                        new Action(ret => Log.Info(String.Format("[Casting: {0}] [Loc: {1}]", spell, onLocation(ret))))
                        ));
        }


        /// <summary>
        ///   Creates a behavior to cast a spell by Id, on the ground at the specified location. 
        ///   Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        public static Composite CastOnGround(int spellid, LocationRetriever onLocation)
        {
            return CastOnGround(spellid, onLocation, ret => true);
        }

        public static Composite CastOnGround(int spellid, LocationRetriever onLocation, CanRunDecoratorDelegate requirements, bool waitForSpell = false)
        {
            return
                new Decorator(
                    ret => onLocation != null && requirements(ret), //  && PRSettings.Instance.UseAoEAbilities
                    new Sequence(
                        new Action(ret => SpellManager.Cast(spellid)),

                        new DecoratorContinue(ctx => waitForSpell,
                            new WaitContinue(1,
                                ret =>
                                StyxWoW.Me.CurrentPendingCursorSpell != null &&
                                StyxWoW.Me.CurrentPendingCursorSpell.Id == spellid, new ActionAlwaysSucceed())),

                        new Action(ret => SpellManager.ClickRemoteLocation(onLocation(ret))),
                        new Action(ret => Log.Info(String.Format("[Casting: {0}] [Loc: {1}]", WoWSpell.FromId(spellid).Name, onLocation(ret))))
                        ));
        }

        public static Composite CastOnUnitLocation(string spell, UnitSelectionDelegate onUnit, Selection<bool> reqs = null)
        {
            return CastOnGround(spell, loc=> onUnit(loc).Location, req => reqs(req));
        }

        public static Composite CastOnUnitLocation(string spell, UnitSelectionDelegate onUnit, Selection<bool> reqs = null, bool waitForSpell = false)
        {
            return CastOnGround(spell, loc => onUnit(loc).Location, req => reqs(req), waitForSpell);
        }


        #endregion

        #region Spells - methods to handle Spells such as cooldowns

        public static TimeSpan GetSpellCooldown(string spell)
        {
            SpellFindResults results;
            if (SpellManager.FindSpell(spell, out results))
            {
                return results.Override != null ? results.Override.CooldownTimeLeft : results.Original.CooldownTimeLeft;
            }

            return TimeSpan.MaxValue;
        }

        public static TimeSpan GetSpellCooldown(int spell)
        {
            SpellFindResults results;
            if (SpellManager.FindSpell(spell, out results))
            {
                return results.Override != null ? results.Override.CooldownTimeLeft : results.Original.CooldownTimeLeft;
            }

            return TimeSpan.MaxValue;
        }

        public static bool SpellOnCooldown(string spell)
        {

            SpellFindResults results;
            if (SpellManager.FindSpell(spell, out results))
            {
                return results.Override != null ? results.Override.Cooldown : results.Original.Cooldown;
            }

            return false;
        }

        public static bool SpellOnCooldown(int spell)
        {

            SpellFindResults results;
            if (SpellManager.FindSpell(spell, out results))
            {
                return results.Override != null ? results.Override.Cooldown : results.Original.Cooldown;
            }

            return false;
        }

        public static double GetSpellCastTime(string spell)
        {
            SpellFindResults results;
            if (SpellManager.FindSpell(spell, out results))
            {
                return results.Override != null ? results.Override.CastTime / 1000.0 : results.Original.CastTime / 1000.0;
            }

            return 99999.9;
        }

        public static double GetSpellCastTime(int spell)
        {
            SpellFindResults results;
            if (SpellManager.FindSpell(spell, out results))
            {
                return results.Override != null ? results.Override.CastTime / 1000.0 : results.Original.CastTime / 1000.0;
            }

            return 99999.9;
        }

        #endregion

        #region Auras - methods to handle Auras/Buffs/Debuffs

        public static uint GetAuraStackCount(int spellId, WoWUnit unit)
        {
            var result = unit.GetAuraById(spellId);
            if (result != null)
            {
                if (result.StackCount > 0)
                    return result.StackCount;
            }

            return 0;
        }

        public static double GetAuraTimeLeft(string aura, WoWUnit onUnit)
        {
            if (onUnit != null)
            {
                var result = onUnit.GetAuraByName(aura);
                if (result != null)
                {
                    if (result.TimeLeft.TotalSeconds > 0)
                        return result.TimeLeft.TotalSeconds;
                }
            }
            return 0;
        }

        public static double GetAuraTimeLeftMs(string aura, WoWUnit onUnit)
        {
            if (onUnit != null)
            {
                var result = onUnit.GetAuraByName(aura);
                if (result != null)
                {
                        return result.TimeLeft.TotalMilliseconds;
                }
            }
            return 0;
        }

        public static double GetAuraTimeLeft(int aura)
        {
            return GetAuraTimeLeft(aura, StyxWoW.Me);
        }

        public static double GetAuraTimeLeft(int aura, WoWUnit onUnit)
        {
            if (onUnit != null)
            {
                var result = onUnit.GetAuraById(aura);
                if (result != null)
                {
                    if (result.TimeLeft.TotalSeconds > 0)
                        return result.TimeLeft.TotalSeconds;
                }
            }
            return 0;
        }

        public static double GetMyAuraTimeLeft(string aura, WoWUnit onUnit)
        {
            if (onUnit != null)
            {
                var result = onUnit.GetAllAuras().FirstOrDefault(a => a.Name == aura && a.CreatorGuid == Me.Guid);
                if (result != null && result.TimeLeft.TotalSeconds > 0)
                    return result.TimeLeft.TotalSeconds;
            }
            return 0;
        }

      
        #endregion

        #region HasAura - Internal Extenstions


        public static bool HasMyAura(this WoWUnit unit, string aura)
        {
            return unit.GetAllAuras().Any(a => a.Name == aura && a.CreatorGuid == Me.Guid);
        }

        public static bool HasMyAura(this WoWUnit unit, int spellId) 
        {
            return unit.GetAllAuras().Any(a => a.SpellId == spellId && a.CreatorGuid == Me.Guid);

        }

        public static bool HasAnyAura(this WoWUnit unit, params string[] auraNames)
        {
            var auras = unit.GetAllAuras();
            var hashes = new HashSet<string>(auraNames);
            return auras.Any(a => hashes.Contains(a.Name));
        }

        public static bool HasAnyAura(this WoWUnit unit, HashSet<int> auraIDs)
        {
            var auras = unit.GetAllAuras();
            return auras.Any(a => auraIDs.Contains(a.SpellId));
        }

        public static bool HasAllAuras(this WoWUnit unit, params string[] auraNames)
        {
            return auraNames.All(unit.HasAura);
        }

        public static bool HasAnyAuras(this WoWUnit unit, params int[] auraIDs)
        {
            return auraIDs.Any(unit.HasAura);
        }

        public static bool HasAuraWithMechanic(this WoWUnit unit, params WoWSpellMechanic[] mechanics)
        {
            var auras = unit.GetAllAuras();
            return auras.Any(a => mechanics.Contains(a.Spell.Mechanic));
        }

        #endregion

        #region Moving - Internal Extentions

        /// <summary>
        /// Internal IsMoving check which ignores turning on the spot.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static bool IsMoving(this WoWUnit unit)
        {
            return unit.MovementInfo.MovingBackward || unit.MovementInfo.MovingForward || unit.MovementInfo.MovingStrafeLeft || unit.MovementInfo.MovingStrafeRight;
        }

        /// <summary>
        /// Internal IsMoving check which ignores turning on the spot, and allows specifying how long you've been moving for before accepting it as actually moving. 
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="movingDuration">Duration in MS how long the unit has been moving before accepting it as a moving unit</param>
        /// <returns></returns>
        public static bool IsMoving(this WoWUnit unit, int movingDuration)
        {
            return unit.IsMoving() && unit.MovementInfo.TimeMoved >= movingDuration;
        }

        #endregion

        #region Extentions
        public static bool IsMelee(this WoWUnit unit)
        {
            {
                if (unit != null)
                {
                    switch (StyxWoW.Me.Class)
                    {
                        case WoWClass.Warrior:
                            return true;
                        case WoWClass.Paladin:
                            return StyxWoW.Me.Specialization != WoWSpec.PaladinHoly;
                        case WoWClass.Hunter:
                            return false;
                        case WoWClass.Rogue:
                            return true;
                        case WoWClass.Priest:
                            return false;
                        case WoWClass.DeathKnight:
                            return true;
                        case WoWClass.Shaman:
                            return StyxWoW.Me.Specialization == WoWSpec.ShamanEnhancement;
                        case WoWClass.Mage:
                            return false;
                        case WoWClass.Warlock:
                            return false;
                        case WoWClass.Druid:
                            return StyxWoW.Me.Specialization != WoWSpec.DruidRestoration &&
                                   StyxWoW.Me.Specialization != WoWSpec.DruidBalance;
                        case WoWClass.Monk:
                            return StyxWoW.Me.Specialization != WoWSpec.MonkMistweaver;
                    }
                }
                return false;
            }
        }
        #endregion

       
    }
}

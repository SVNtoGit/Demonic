using System.Collections.Generic;
using System.Linq;
using CommonBehaviors.Actions;
using Demonic.Helpers;
using Demonic.Managers;
using Demonic.Settings;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Demonic.Core
{
    internal static class Unit
    {
        #region Core Unit Checks - these should all be incorperated into the individual class checks below.

        // This is the core check..filter everthing else related to hostiles of UnfriendlyUnits
        internal static IEnumerable<WoWUnit> AttackableUnits
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>(true, false).Where(u => u.Attackable && u.CanSelect && !u.IsFriendly && !u.IsDead && !u.IsNonCombatPet && !u.IsCritter && u.Distance <= 60); }
        }

        internal static IEnumerable<WoWUnit> AttackableMeleeUnits
        {
            get { return AttackableUnits.Where(u => u.DistanceSqr < 5); }
        }

        internal static IEnumerable<WoWPlayer> GroupMembers
        {
            get { return ObjectManager.GetObjectsOfType<WoWPlayer>(true, true).Where(u => !u.IsDead && u.CanSelect && u.IsInMyPartyOrRaid); }
        }

        internal static IEnumerable<WoWUnit> NearbyAttackableUnits(WoWPoint fromLocation, double radius)
        {
            var hostile = AttackableUnits;
            var maxDistance = radius * radius;
            hostile = hostile.Where(x => x.Location.DistanceSqr(fromLocation) < maxDistance); // We need real distance, not only 2D-Distances
            return hostile.Any(x => x.IsCrowdControlled()) ? new List<WoWUnit>() : hostile;
        }

        internal static IEnumerable<WoWUnit> AttackableUnitsAll
        {
            get { return ObjectManager.GetObjectsOfType<WoWUnit>(true, false).Where(u => !u.IsDead && u.CanSelect && u.Attackable && !u.IsFriendly && !u.IsNonCombatPet && !u.IsCritter); }
        }

        internal static IEnumerable<WoWUnit> NearbyAttackableUnitsIgnoreCc(WoWPoint fromLocation, double radius)
        {
            var hostile = AttackableUnitsAll;
            var maxDistance = radius * radius;
            return hostile.Where(x => x.Location.DistanceSqr(fromLocation) < maxDistance); // We need real distance, not only 2D-Distances
            //return hostile.Any(x => x.IsCrowdControlled()) ? new List<WoWUnit>() : hostile;
        }

        internal static IEnumerable<WoWUnit> NearbyAttackableUnitsAttackingUs(WoWPoint fromLocation, double radius)
        {
            var hostile = AttackableUnits;
            var maxDistance = radius * radius;
            return hostile.Where(x => x.Location.DistanceSqr(fromLocation) < maxDistance && (x.IsTargetingMyPartyMember || x.IsTargetingMeOrPet || x.IsTargetingAnyMinion || x.IsTargetingMyRaidMember || x.IsTargetingPet || x.IsMechanical));
        }

        internal static IEnumerable<WoWUnit> NearbyAttackableUnitsAttackingMe(WoWPoint fromLocation, double radius)
        {
            var hostile = AttackableUnits;
            var maxDistance = radius * radius;
            return hostile.Where(x => x.Location.DistanceSqr(fromLocation) < maxDistance && x.IsTargetingMeOrPet);
        }

        /// <summary>returns true if the unit is crowd controlled.</summary>
        /// <param name="unit">unit to check</param>
        /// <param name="breakOnDamageOnly">true for break on damage</param>
        /// <returns>The unit is controlled.</returns>
        public static bool UnitIsControlled(WoWUnit unit, bool breakOnDamageOnly)
        {
            return unit != null && unit.GetAllAuras().Any(x => x.IsHarmful && (ControlDebuffs.Contains(x.Name) || (!breakOnDamageOnly && ControlUnbreakableDebuffs.Contains(x.Name))));
        }

        private static readonly string[] ControlDebuffs = new[] {
            "Bind Elemental", "Hex", "Polymorph", "Hibernate", "Entangling Roots", "Freezing Trap", "Wyvern Sting",
            "Repentance", "Psychic Scream", "Sap", "Blind", "Fear", "Seduction", "Howl of Terror"
        };

        private static readonly string[] ControlUnbreakableDebuffs = new[] { "Cyclone", "Mind Control", "Banish" };

        #endregion

        #region Cached methods

        internal static IEnumerable<WoWUnit> CachedNearbyAttackableUnits(WoWPoint fromLocation, double radius)
        {
            var hostile = CachedUnits.CachedAttackableUnits;
            var maxDistance = radius * radius;

            if (hostile == null || hostile.Count == 0)
            {
                //Log.Debug("Cache Empty! Fresh ObjectManager Call");
                var freshHostile = NearbyAttackableUnits(fromLocation, radius);
                return freshHostile.Where(x => x != null && x.IsValid && x.Location.DistanceSqr(fromLocation) < maxDistance);
            }
            return hostile.Where(x => x != null && x.IsValid && x.Location.DistanceSqr(fromLocation) < maxDistance);
        }

        internal static IEnumerable<WoWUnit> CachedNearbyAttackableUnitsAttackingUs(WoWPoint fromLocation, double radius)
        {
            var hostile = CachedUnits.CachedAttackableUnits;
            var maxDistance = radius * radius;

            if (hostile == null || hostile.Count == 0)
            {
                //Log.Debug("Cache Empty! Fresh ObjectManager Call");
                var freshHostile = NearbyAttackableUnits(fromLocation, radius);
                return freshHostile.Where(x => x != null && x.IsValid && x.Location.DistanceSqr(fromLocation) < maxDistance);
            }

            return hostile.Where(x => x.IsValid && x.Location.DistanceSqr(fromLocation) < maxDistance && (x.IsTargetingMyPartyMember || x.IsTargetingMeOrPet || x.IsTargetingAnyMinion || x.IsTargetingMyRaidMember || x.IsTargetingPet || x.IsMechanical));
        }

        #endregion Cached methods

        #region MultiDotting

        internal static WoWUnit GetMultiDoTTarget(WoWUnit unit, string debuff, double radius, double refreshDurationRemaining)
        {
            // find unit without our debuff
            var dotTarget = CachedNearbyAttackableUnitsAttackingUs(unit.Location, radius)
                .Where(x => x != null)
                .OrderByDescending(x => x.HealthPercent)
                .FirstOrDefault(x => !x.HasMyAura(debuff) && x.InLineOfSpellSight);

            if (dotTarget == null)
            {
                // If we couldn't find one without our debuff, then find ones where debuff is about to expire.
                dotTarget = CachedNearbyAttackableUnitsAttackingUs(unit.Location, radius)
                            .Where(x => x != null)
                            .OrderByDescending(x => x.HealthPercent)
                            .FirstOrDefault(x => (x.HasMyAura(debuff) && Spell.GetMyAuraTimeLeft(debuff, x) < refreshDurationRemaining) && x.InLineOfSpellSight);
            }
            return dotTarget;
        }

        #endregion MultiDotting

        #region Extensions

        internal static bool IsBoss(this WoWUnit thisUnit)
        {
            return thisUnit != null && (thisUnit.IsBoss || BossList.BossIds.Contains(thisUnit.Entry));
        }

        internal static bool IsCrowdControlled(this WoWUnit unit)
        {
            if (unit != null)
            {
                Dictionary<string, WoWAura>.ValueCollection auras = unit.Auras.Values;

                return auras.Any(a =>
                                 a.IsHarmful && (
                                                    a.Spell.Mechanic == WoWSpellMechanic.Banished ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Disoriented ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Charmed ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Horrified ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Incapacitated ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Polymorphed ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Sapped ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Shackled ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Asleep ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Frozen ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Invulnerable ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Invulnerable2 ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Turned ||
                                                    a.Spell.Mechanic == WoWSpellMechanic.Fleeing ||

                                                    // Really want to ignore hexed mobs.
                                                    a.Spell.Name == "Hex"));
            }
            return false;
        }

        private static bool IsAerialTarget(this WoWUnit unit)
        {
            if (unit.MaxHealth == 1) return false; // training dummy..:/
            float height = HeightOffTheGround(unit);
            if (height > 5f && height < float.MaxValue)
                return true;
            return false;
        }

        private static float HeightOffTheGround(this WoWUnit unit)
        {
            float minDiff = float.MaxValue;
            var pt = new WoWPoint(unit.Location.X, unit.Location.Y, unit.Location.Z);

            List<float> listMeshZ = Navigator.FindHeights(pt.X, pt.Y);
            foreach (var meshZ in listMeshZ)
            {
                var diff = pt.Z - meshZ;
                if (diff >= 0 && diff < minDiff)
                {
                    minDiff = diff;
                    pt.Z = meshZ;
                }
            }

            return minDiff;
        }

        /*
         
        * Example Code for integrating a Hotkey for cooldown usage!
         
        */
        internal static bool IsBoss(this WoWUnit thisUnit, bool hotkey)
        {
            return thisUnit != null && (hotkey || thisUnit.IsBoss || BossList.BossIds.Contains(thisUnit.Entry));
        }

        internal static bool IsBossOrPlayer(this WoWUnit thisUnit)
        {
            return thisUnit != null && ((thisUnit.IsPlayer && !thisUnit.IsFriendly) || thisUnit.IsBoss || BossList.BossIds.Contains(thisUnit.Entry));
        }

        /*
         
         * Example usage: WoWUnit.IsBoss(HotKeyManager.IsCooldown)
         
        */
        public static void CancelAura(this WoWUnit unit, string aura)
        {
            WoWAura a = unit.GetAuraByName(aura);
            if (a != null && a.Cancellable)
                a.TryCancelAura();
        }
        #endregion

    }
}

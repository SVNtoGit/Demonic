﻿using System.Linq;
using Demonic.Settings;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Demonic.Core
{
    static class Racials
    {
        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static WoWRace CurrentRace { get { return StyxWoW.Me.Race; }}
        private static DemonicSettings Settings { get { return DemonicSettings.Instance; } }

        public static Composite UseRacials()
        {

            return new Decorator(ret => ((Me.CurrentTarget.IsBoss || (Me.CurrentTarget.IsPlayer && Me.CurrentTarget.IsHostile)) && Settings.RacialUsage == 1)
                                        || Settings.RacialUsage == 2,

                    Spell.Cast(CurrentRacialSpell(), ret => CurrentRacialSpell() != null && RacialUsageSatisfied(CurrentRacialSpell())));
        }

        private static string CurrentRacialSpell()
        {
            switch (CurrentRace)
            {
                case WoWRace.BloodElf:
                    return "Arcane Torrent";
                case WoWRace.Draenei:
                    return "Gift of the Naaru";
                case WoWRace.Dwarf:
                    return "Stoneform";
                case WoWRace.Gnome:
                    return "Escape Artist";
                case WoWRace.Goblin:
                    return "Rocket Barrage";
                case WoWRace.Human:
                    return "Every Man for Himself";
                case WoWRace.NightElf:
                    return "Shadowmeld";
                case WoWRace.Orc:
                    return "Blood Fury";
                case WoWRace.Pandaren:
                    return null;
                case WoWRace.Tauren:
                    return "War Stomp";
                case WoWRace.Troll:
                    return "Berserking";
                case WoWRace.Undead:
                    return "Will of the Forsaken";
                case WoWRace.Worgen:
                    return "Darkflight";
                default:
                    return null;
            }
        }

        private static bool RacialUsageSatisfied(string racial)
        {
            if (racial != null)
            {
                switch (racial)
                {
                    case "Stoneform":
                        return StyxWoW.Me.GetAllAuras().Any(a => a.Spell.Mechanic == WoWSpellMechanic.Bleeding || a.Spell.DispelType == WoWDispelType.Disease || a.Spell.DispelType == WoWDispelType.Poison);
                    case "Escape Artist":
                        return StyxWoW.Me.Rooted;
                    case "Every Man for Himself":
                        return StyxWoW.Me.GetAllAuras().Any(a => a.Spell.Mechanic == WoWSpellMechanic.Fleeing || a.Spell.Mechanic == WoWSpellMechanic.Asleep || a.Spell.Mechanic == WoWSpellMechanic.Banished || a.Spell.Mechanic == WoWSpellMechanic.Charmed || a.Spell.Mechanic == WoWSpellMechanic.Frozen || a.Spell.Mechanic == WoWSpellMechanic.Horrified || a.Spell.Mechanic == WoWSpellMechanic.Incapacitated || a.Spell.Mechanic == WoWSpellMechanic.Polymorphed || a.Spell.Mechanic == WoWSpellMechanic.Rooted || a.Spell.Mechanic == WoWSpellMechanic.Sapped || a.Spell.Mechanic == WoWSpellMechanic.Stunned);
                    case "Shadowmeld":
                        return Targeting.GetAggroOnMeWithin(StyxWoW.Me.Location, 15) >= 1 && StyxWoW.Me.HealthPercent < 80 && !StyxWoW.Me.IsMoving;
                    case "Gift of the Naaru":
                        return StyxWoW.Me.HealthPercent <= 75;
                    case "Darkflight":
                        return StyxWoW.Me.IsMoving;
                    case "Blood Fury":
                        return StyxWoW.Me.CurrentTarget != null && ((StyxWoW.Me.IsMelee() && StyxWoW.Me.CurrentTarget.IsWithinMeleeRange) || !StyxWoW.Me.IsMelee());
                    case "War Stomp":
                        return Targeting.GetAggroOnMeWithin(StyxWoW.Me.Location, 8) >= 1;
                    case "Berserking":
                        return !StyxWoW.Me.HasAnyAura(Spell.HeroismBuff) && ((Me.CurrentTarget != null && Me.IsMelee() && Me.CurrentTarget.IsWithinMeleeRange) || !Me.IsMelee());
                    case "Will of the Forsaken":
                        return StyxWoW.Me.GetAllAuras().Any(a => a.Spell.Mechanic == WoWSpellMechanic.Fleeing || a.Spell.Mechanic == WoWSpellMechanic.Asleep || a.Spell.Mechanic == WoWSpellMechanic.Charmed);
                    case "Arcane Torrent":
                        return StyxWoW.Me.ManaPercent < 91 && StyxWoW.Me.Class != WoWClass.DeathKnight;
                    case "Rocket Barrage":
                        return true;

                    default:
                        return false;
                }
            }

            return false;
        }

    }
}

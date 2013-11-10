using System;
using System.Linq;
using Demonic.Core;
using Demonic.Helpers;
using Demonic.Managers;
using Demonic.Settings;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Demonic.Specialisation
{
    static class Common
    {
        private delegate WoWUnit UnitSelectionDelegate(object context);
        private static DemonicSettings Settings { get { return DemonicSettings.Instance; } }
        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static string _sacrificeAbility;

        public static Composite PreCombat()
        {
            return new Decorator(ret => !Me.IsMoving() && !Me.Mounted && !Me.IsDead && !Me.Combat && !Me.IsFlying && !Me.IsOnTransport && !Me.HasAnyAura("Food", "Drink"),
                new PrioritySelector(
                SummonPet(),
                HandleHealthstones(),
                HandleSoulLink(),
                Spell.PreventDoubleCast("Dark Intent", 5, ret => !Me.HasAura("Dark Intent")),
                Spell.Cast("Life Tap", ret => CanLifeTap && (StyxWoW.Me.Specialization == WoWSpec.WarlockAffliction || StyxWoW.Me.Specialization == WoWSpec.WarlockDemonology)),
                HandleBloodHorror(),
                HandleTwilightWard(),
                HandleGrimoireOfSacrifice()
                ));
        }

        public static Composite Combat()
        {
            return new PrioritySelector(
                // Pet
                SummonPet(),
                
                // Racials
                Racials.UseRacials(),

                // Items
                Item.UseTrinkets(),
                HandleHealthstones(),
                HandleEngineerGloves(),

                // General Abilities
                HealthFunnel(),
                DrainLife(),
                HandleLifeblood(),
                HandleTwilightWard(),
                HandleCurses(),
                HandleUnendingResolve(),
                HandleDemonicCircle(),
                HandleDoomguardOrInfernal(),
                HandleSoulshatter(),
                new Decorator(ret => Me.CurrentTarget != null && Me.CurrentTarget.IsValid,
                                    HandlePetAbility(ret => Me.CurrentTarget)),

                // Talents
                HandleDarkRegeneration(),
                //HandleSoulLink(),
                HandleMortalCoil(),
                HandleHowlOfTerror(),
                HandleShadowfury(),
                HandleBloodHorror(),
                HandleSacrificialPact(),
                HandleDarkBargain(),
                HandleBurningRush(),
                HandleUnboundWill(),
                HandleGrimoireOfService(),
                HandleGrimoireOfSacrifice()
                //HandleArchimondesVengeance()

                );
        }

        public static Composite Pull()
        {
            return new PrioritySelector(
                //SummonPet(), // Removed from Pull. HB blacklists mobs after 3 seconds of attempting to engage.
                HandleUnendingBreath(),
                HandleGrimoireOfSacrifice()
                );
        }

        #region Ability Composites
        private static Composite DrainLife()
        {
            return new PrioritySelector(
                Spell.Cast("Drain Life", ret => Settings.UseDrainLife && Me.HealthPercent <= Settings.DrainLifeHP, "HP Below Setting Threshold")
                );
        }

        public static Composite HandleHealthstones()
        {
            return new PrioritySelector(
                Item.UseBagItem("Healthstone", ret => Settings.UseHealthstones && Me.Combat && Me.HealthPercent < Settings.HealthstoneHPPercent && !Settings.OnlyHealthstoneWithDarkRegen, "HP Low"),
                Item.UseBagItem("Healthstone", ret => Settings.UseHealthstones && Me.Combat && Settings.OnlyHealthstoneWithDarkRegen && Me.HasAura("Dark Regeneration"), "Dark Regeneration Active"),
                Spell.PreventDoubleCast("Create Healthstone", 3.5, ret => Settings.CreateHealthstones && !Me.IsActuallyInCombat && Me.Level >= 9 && Me.BagItems.FirstOrDefault(x => x.Entry == 5512) == null)
                );
        }

        private static Composite HandleEngineerGloves()
        {
            return new PrioritySelector(
                /* Synapse Springs
                    0 = Never
                    1 = On Boss Or Player
                    2 = On Cooldown
                */
                new Decorator(
                    ret => Settings.UseSynapseSprings
                        && (Settings.SynapseSpringsCond == 1 && (Me.CurrentTarget.IsBoss || Me.CurrentTarget.IsPlayer && Me.CurrentTarget.IsHostile))
                        || (Settings.SynapseSpringsCond == 2),
                Item.UseEngineerGloves())

                );
        }

        private static Composite HandleLifeblood()
        {
            return new Decorator(ret => Settings.UseLifeblood,
                new PrioritySelector(
                     /* 
                     0 = On Low HP
                     1 = On Boss Or Player
                     2 = On Cooldown
                     */
                    Spell.Cast("Lifeblood", ret => Settings.LifebloodCond == 0 && Me.HealthPercent <= Settings.LifebloodLowHPValue),
                    Spell.Cast("Lifeblood", ret => Settings.LifebloodCond == 1 && Me.CurrentTarget.IsBossOrPlayer()),
                    Spell.Cast("Lifeblood", ret => Settings.LifebloodCond == 2)
                ));
        }

        private static Composite HandleTwilightWard()
        {
            /*
         Never
         In Combat
         Always
         Target Casting Holy/Shadow Spell At Me
         */
            return new Decorator(ret => Settings.TwilightWardCond != 0,
                new PrioritySelector(
                Spell.Cast("Twilight Ward", ret => Settings.TwilightWardCond == 1 && Me.Combat),
                Spell.Cast("Twilight Ward", ret => Settings.TwilightWardCond == 2),
                Spell.Cast("Twilight Ward", ret => Settings.TwilightWardCond == 3 && Me.CurrentTarget != null && Me.CurrentTarget.CastingSpell != null && 
                    (Me.CurrentTarget.CastingSpell.School == WoWSpellSchool.Shadow || Me.CurrentTarget.CastingSpell.School == WoWSpellSchool.Holy))
                ));
        }

        private static Composite HandleCurses()
        {
            /*Curses
            0-Curse of the Elements
            1-Curse of Enfeeblement
            2-Curse of Exhaustion
            3-None
         
             * When
                Always
                On Boss or Player
         
             */

            return new Decorator(ret => Settings.CurseSelection != 3,
                new PrioritySelector(
                Spell.PreventDoubleCast("Soulburn", 1, ret => Settings.Affliction_UseSoulburnWithCurse && Me.Specialization == WoWSpec.WarlockAffliction && !Me.ActiveAuras.ContainsKey("Soulburn")),
                new Decorator(ret => Settings.CurseSelection == 0, new PrioritySelector(
                    Spell.PreventDoubleCast("Curse of the Elements", 5, ret => Settings.CurseSelectionCond == 0 && NeedCurseOfElements),
                    Spell.PreventDoubleCast("Curse of the Elements", 5, ret => Settings.CurseSelectionCond == 1 && NeedCurseOfElements && Me.CurrentTarget.IsBossOrPlayer())
                    )),
                new Decorator(ret => Settings.CurseSelection == 1, new PrioritySelector(
                    Spell.PreventDoubleCast("Curse of Enfeeblement", 5, ret => Settings.CurseSelectionCond == 0 && !Me.CurrentTarget.HasAnyAura("Curse of Enfeeblement")),
                    Spell.PreventDoubleCast("Curse of Enfeeblement", 5, ret => Settings.CurseSelectionCond == 1 && !Me.CurrentTarget.HasAnyAura("Curse of Enfeeblement") && Me.CurrentTarget.IsBossOrPlayer())
                    )),
                new Decorator(ret => Settings.CurseSelection == 2, new PrioritySelector(
                    Spell.PreventDoubleCast("Curse of Exhaustion", 5, ret => Settings.CurseSelectionCond == 0 && !Me.CurrentTarget.HasAnyAura("Curse of Exhaustion")),
                    Spell.PreventDoubleCast("Curse of Exhaustion", 5, ret => Settings.CurseSelectionCond == 1 && !Me.CurrentTarget.HasAnyAura("Curse of Exhaustion") && Me.CurrentTarget.IsBossOrPlayer())
                    ))
                ));
        }

        private static Composite HandleUnendingResolve()
        {
            return new Decorator(ret => Settings.UseUnendingResolve,
                new PrioritySelector(
                Spell.Cast("Unending Resolve", ret => Me.HealthPercent <= Settings.UnendingResolveHPValue)
                ));
        }

        private static Composite HandleUnendingBreath()
        {
            return new PrioritySelector(
                Spell.Cast("Unending Breath", ret => Settings.UnendingBreath && Me.IsSwimming && !Me.HasAura("Unending Breath"))
                );
        }

        private static Composite HandleDemonicCircle()
        {
            return new PrioritySelector(
                Spell.PreventDoubleCast("Soulburn", 1, ret => Settings.Affliction_DemonicCircle_Soulburn && Me.Specialization == WoWSpec.WarlockAffliction && !Me.ActiveAuras.ContainsKey("Soulburn")),
                Spell.Cast("Demonic Circle: Teleport", ret => Settings.DemonicCircle_RootedSnared && (Me.Rooted || Me.HasAuraWithMechanic(WoWSpellMechanic.Snared))),
                Spell.Cast("Demonic Circle: Teleport", ret => Settings.DemonicCircle_LowHP && Me.HealthPercent <= Settings.DemonicCircle_LowHPValue)
                );

        }

        private static Composite HandleDoomguardOrInfernal()
        {
            return new PrioritySelector(
               // 0 = SimulationCraft Rules, 1 = Boss Or Player, 2 = Always
               new Decorator(ret => Settings.AutoSummonDoomguard, new PrioritySelector(
                   Spell.Cast("Summon Doomguard", ret => Settings.DoomguardCondition == 0 && NeedToSummonDoomguard && Me.CurrentTarget.IsBossOrPlayer()),
                   Spell.Cast("Summon Doomguard", ret => Settings.DoomguardCondition == 1 && Me.CurrentTarget.IsBossOrPlayer()),
                   Spell.Cast("Summon Doomguard", ret => Settings.DoomguardCondition == 2)
                   )),
               new Decorator(ret => Settings.AutoSummonInfernal, new PrioritySelector(
                   Spell.Cast("Summon Infernal", ret => Settings.InfernalCondition == 0 && NeedToSummonInfernal && Me.CurrentTarget.IsBossOrPlayer()),
                   Spell.Cast("Summon Infernal", ret => Settings.InfernalCondition == 1 && Me.CurrentTarget.IsBossOrPlayer()),
                   Spell.Cast("Summon Infernal", ret => Settings.InfernalCondition == 2)
                   ))

                );
        }

        private static Composite HandleSoulshatter()
        {
            return new PrioritySelector(
                Spell.Cast("Soulshatter", on => Me, ret => Settings.UseSoulshatter && Me.HealthPercent < 70 && (Me.CurrentMap.IsRaid || Me.CurrentMap.IsInstance) && (Me.CurrentTarget.ThreatInfo.RawPercent > 95 || Targeting.GetAggroOnMeWithin(StyxWoW.Me.Location, 10) >= 1) && !Spell.IsChanneling)
                );
        }

        //private static Composite HandlePetAbility(UnitSelectionDelegate unit)
        //{
        //    return new Decorator(ret => Settings.UseCommandDemon && (Me.GotAlivePet && Me.Pet.Distance <= 40 || Me.HasAura("Grimoire of Sacrifice")) && unit != null,
        //        new PrioritySelector(
        //            new Decorator(ret => Me.GotAlivePet,
        //                new PrioritySelector(
        //                    //Succubus/Shivarra: Fellash (25s CD) - Knock back all units within 5 yards.
        //                    Spell.PreventDoubleCastOnGround("Command Demon", 25, on => Me.Location, ret =>
        //                        (Me.Pet.CreatureFamilyInfo.Id == SpellId.PetId_Shivarra || Me.Pet.CreatureFamilyInfo.Id == SpellId.PetId_Succubus)
        //                         && unit.DistanceSqr <= 5),

        //                    //Felhunter/Observer:  (24s CD) - Interupt/Silence.
        //                    Spell.PreventDoubleCast("Command Demon", 24, on => unit, ret =>
        //                        (Me.Pet.CreatureFamilyInfo.Id == SpellId.PetId_Observer || Me.Pet.CreatureFamilyInfo.Id == SpellId.PetId_Felhunter)
        //                        && (unit.IsCasting || unit.IsChanneling) && unit.CanInterruptCurrentSpellCast),

        //                    //Voidlord: Disarm (1min CD) - Disarm target for 8 seconds.
        //                    Spell.PreventDoubleCast("Command Demon", 60, on => unit, ret =>
        //                         (Me.Pet.CreatureFamilyInfo.Id == SpellId.PetId_Voidlord || Me.Pet.CreatureFamilyInfo.Id == SpellId.PetId_Voidwalker)
        //                         && !unit.Disarmed
        //                         && unit.DistanceSqr <= 5
        //                         && (!unit.IsPlayer || unit.IsPlayer && (unit.Class == WoWClass.DeathKnight
        //                            || unit.Class == WoWClass.Hunter
        //                            || unit.Class == WoWClass.Monk
        //                            || unit.Class == WoWClass.Paladin
        //                            || unit.Class == WoWClass.Warrior
        //                            || unit.Class == WoWClass.Shaman
        //                            || unit.Class == WoWClass.Rogue))),

        //                    //Felguard/Wrathguard: Wrathstorm (45s CD) - Attack all units in 8 yards.
        //                    Spell.PreventDoubleCast("Command Demon", 45, ret =>
        //                        (Me.Pet.CreatureFamilyInfo.Id == SpellId.PetId_Felguard || Me.Pet.CreatureFamilyInfo.Id == SpellId.PetId_Wrathguard)
        //                        && Unit.CachedNearbyAttackableUnits(Me.Pet.Location, 8).Any()),

        //                    //Fel Imp: Cauterize Master (30s CD) - Small damage then heals 12% of HP over 12 seconds.
        //                    Spell.PreventDoubleCast("Command Demon", 30, ret =>
        //                        Me.Pet.CreatureFamilyInfo.Id == SpellId.PetId_FelImp
        //                        && Me.HealthPercent <= 88)
        //                    )),

        //            new Decorator(ret => Me.HasAura("Grimoire of Sacrifice") /*&& !Spell.SpellOnCooldown(SpellBook.CommandDemon)*/,
        //                new PrioritySelector(

        //                    new Action(delegate
        //                    {
        //                        _sacrificeAbility = GetSacrificeAbility();
        //                        /*
        //                        Logger.FailLog("Ability:[{0}] - Icon:[{1}] - MeleeRange[{2}] - Facing[{3}] - Cond[{4}]", 
        //                            _sacrificeAbility, 
        //                            Helpers.Lua.GetSpellIconText(SpellBook.CommandDemon), 
        //                            unit.IsWithinMeleeRange, 
        //                            Me.IsSafelyFacing(unit, 120f),
        //                            Unit.CachedNearbyAttackableUnits(Me.Location, 5).Any()); 
        //                        */
        //                        return RunStatus.Failure;
        //                    }),

        //                    //Imp: Singe Magic (10s CD) - Removes 1 harmful effect from target.
        //                    new Decorator(ret => _sacrificeAbility == "Singe Magic",
        //                        Spell.PreventDoubleCast("Command Demon", 10, on => Me, ret => Me.GetAllAuras().FirstOrDefault(a => a.IsHarmful && a.Duration > 0 && a.Spell.DispelType == WoWDispelType.Magic) != null)),

        //                    //Voidwalker: Shadow Bulwark (2min CD) - 30% HP for 20 seconds.
        //                    new Decorator(ret => _sacrificeAbility == "Shadow Bulwark",
        //                        Spell.PreventDoubleCast("Command Demon", 120, ret => Me.HealthPercent <= 25)),

        //                    //Whiplash (25s CD) - Knock back all enemies in 5 yards.
        //                    new Decorator(ret => _sacrificeAbility == "Whiplash",
        //                        Spell.PreventDoubleCastOnGround("Command Demon", 25, loc => Me.Location, ret => Unit.CachedNearbyAttackableUnits(Me.Location, 5).Any())),

        //                    //Spell Lock (24s CD) - Interupt/Silence
        //                    new Decorator(ret => _sacrificeAbility == "Spell Lock",
        //                        Spell.PreventDoubleCast("Command Demon", 24, on => unit, ret =>
        //                            unit.CanInterruptCurrentSpellCast && (unit.IsCasting || unit.IsChanneling))),

        //                    //Pursuit (15s CD - 8-25yd Range) - Charge enemy 
        //                    new Decorator(ret => _sacrificeAbility == "Pursuit", Spell.PreventDoubleCast("Command Demon", 15, on => unit,
        //                        ret => unit.Distance >= 15 && unit.Distance <= 25 && Me.MovementInfo.MovingForward && Me.IsSafelyFacing(unit, 120f))))

        //                    ))

        //        );
        //}

        private static Composite HandlePetAbility(UnitSelectionDelegate unit)
        {
            try
            {
                return new Decorator(ret => unit != null && unit(ret) != null && unit(ret).IsValid,
                                     new PrioritySelector(
                                         new Decorator(
                                             ret =>
                                             Me.GotAlivePet && Me.Pet != null && Me.Pet.Distance < 40 && unit(ret).InLineOfSpellSight,
                                             new PrioritySelector(

                                                 //Felhunter/Observer:  (24s CD) - Interupt/Silence.
                                                 Spell.PreventDoubleCast("Spell Lock", 3, on => unit(on),
                                                            ret => unit != null && unit(ret) != null && unit(ret).IsValid &&
                                                                   Spell.Lastspellcast != "Fear" &&
                                                                   !unit(ret).IsPet &&
                                                                   (unit(ret).IsCasting || unit(ret).IsChanneling) &&
                                                                   unit(ret).CanInterruptCurrentSpellCast),
                                                 Spell.PreventDoubleCast("Optical Blast", 3, on => unit(on),
                                                            ret => unit != null && unit(ret) != null && unit(ret).IsValid &&
                                                                   Spell.Lastspellcast != "Fear" &&
                                                                   !unit(ret).IsPet &&
                                                                   (unit(ret).IsCasting || unit(ret).IsChanneling) &&
                                                                   unit(ret).CanInterruptCurrentSpellCast),

                                                 //Voidlord: Disarm (1min CD) - Disarm target for 8 seconds.
                                                 new Decorator(ret => Me.Pet.CreatureFamilyInfo.Id == SpellId.PetId_Voidlord || Me.Pet.CreatureFamilyInfo.Id == SpellId.PetId_Voidwalker,
                                                 Spell.PreventDoubleCast("Disarm", 3, on => unit(on), ret =>
                                                                    unit != null &&
                                                                    unit(ret) != null &&
                                                                    unit(ret).IsValid &&
                                                                   !unit(ret).Disarmed

                                                     )),

                                                 //Felguard Felstorm (45s CD) - Attack all units in 8 yards.
                                                 Spell.Cast("Felstorm", ret => unit != null && unit(ret) != null && unit(ret).IsValid &&
                                                                               Me.Pet.GotTarget &&
                                                                               Me.Pet.CurrentTarget.RelativeLocation
                                                                                 .Distance(Me.Pet.Location) < 8),

                                                 //Wrathguard - Wrathstorm (45s CD) - all units in 8 yds
                                                 Spell.Cast("Wrathstorm",
                                                            ret => unit != null && unit(ret) != null && unit(ret).IsValid &&
                                                                   Me.Pet.GotTarget &&
                                                                   Me.Pet.CurrentTarget.RelativeLocation.Distance(
                                                                       Me.Pet.Location) < 8),

                                                 //Fel Imp: Cauterize Master (30s CD) - Small damage then heals 12% of HP over 12 seconds.
                                                 Spell.Cast("Cauterize Master",
                                                            ret => unit != null && unit(ret) != null && unit(ret).IsValid && Me.HealthPercent <= 88)
                                                 )),

                                         new Decorator(ret => Me.HasAura("Grimoire of Sacrifice"),
                                                       new PrioritySelector(

                                                           new Action(delegate
                                                           {
                                                               _sacrificeAbility = GetSacrificeAbility().Trim();
                                                               //Log.PrintNoDuplicate("Sacrifice Ability: {0}", _sacrificeAbility);
                                                               return RunStatus.Failure;
                                                           }),

                                                           //Imp: Singe Magic (10s CD) - Removes 1 harmful effect from target.
                                                           new Decorator(ret => _sacrificeAbility == "Singe Magic",
                                                                         Spell.PreventDoubleCast(132411, 10, on => Me,
                                                                                         ret =>
                                                                                         Me.GetAllAuras()
                                                                                           .FirstOrDefault(
                                                                                               a =>
                                                                                               a.IsHarmful &&
                                                                                               a.Duration > 0 &&
                                                                                               a.Spell.DispelType ==
                                                                                               WoWDispelType.Magic) !=
                                                                                         null)),

                                                           //Voidwalker: Shadow Bulwark (2min CD) - 30% HP for 20 seconds.
                                                           new Decorator(ret => _sacrificeAbility == "Shadow Bulwark",
                                                                         Spell.PreventDoubleCast(132413, 120, on => Me,
                                                                                         ret => Me.HealthPercent <= 25)),

                                                           //Spell Lock (24s CD) - Interupt/Silence
                                                           new Decorator(ret => _sacrificeAbility == "Spell Lock",
                                                                         Spell.PreventDoubleCast(132409, 24, on => unit(on), ret =>
                                                                                                                Spell.Lastspellcast != "Fear" &&
                                                                                                                 !unit(ret).IsPet &&
                                                                                                                 (unit(ret).IsCasting || unit(ret).IsChanneling) &&
                                                                                                                 unit(ret).CanInterruptCurrentSpellCast
                                                                             )),

                                                           //Pursuit (15s CD - 8-25yd Range) - Charge enemy 
                                                           new Decorator(ret => _sacrificeAbility == "Pursuit",
                                                                         Spell.PreventDoubleCast(132410, 15, on => unit(on),
                                                                                         ret =>
                                                                                         unit(ret).Distance >= 15 &&
                                                                                         unit(ret).Distance <= 25 &&
                                                                                         Me.MovementInfo.MovingForward &&
                                                                                         Me.IsSafelyFacing(unit(ret),
                                                                                                           120f))))

                                             ))

                    );
            }
            catch (Exception ex)
            {
                Log.Debug("Exception thrown at HandlePet: {0}", ex.ToString());
                return new PrioritySelector();
            }
        }

        #endregion

        #region Talent Composites

        private static Composite HandleBloodHorror()
        {
            // 0 = Always, 1 = Only In Combat, 2 = Never
            return new Decorator(ret => Settings.BloodHorror != 2,
                new PrioritySelector(
                    Spell.Cast(111397, ret => Settings.BloodHorror != 2 && Me.Combat && !Me.HasAura("Blood Horror")),
                    Spell.Cast(111397, ret => Settings.BloodHorror == 0 && !Me.HasAura("Blood Horror"))
                    ));
        }

        private static Composite HandleDarkRegeneration()
        {
            return new Decorator(ret => Settings.UseDarkRegeneration,
                new PrioritySelector(
                    Spell.Cast("Dark Regeneration", ret => Me.HealthPercent <= Settings.DarkRegenerationPercent)
                    ));
        }

        private static Composite HandleSoulLink()
        {
            return new Decorator(ret => Settings.UseSoulLink && Me.GotAlivePet,

                Spell.PreventDoubleCast("Soul Link", 2, on => Me, ret => !Me.HasAura("Soul Link")));
        }

        private static Composite HandleMortalCoil()
        {
            return new Decorator(ret => Settings.UseMortalCoil, Spell.Cast("Mortal Coil", ret => Me.HealthPercent <= Settings.MortalCoilHPValue));
        }

        private static Composite HandleHowlOfTerror()
        {
            
            return new PrioritySelector(
                Spell.Cast("Howl of Terror", ret => Settings.UseHowlOfTerrorHPLessThan && Me.HealthPercent <= Settings.HowlOfTerrorHPLessThanValue && Unit.CachedNearbyAttackableUnits(Me.Location, 10).Any()),
                Spell.Cast("Howl of Terror", ret => Settings.UseHowlOfTerrorUnitsInRange && Unit.CachedNearbyAttackableUnits(Me.Location, 10).Count() >= Settings.HowlOfTerrorUnitsInRangeValue)
                );
        }

        private static Composite HandleShadowfury()
        {
            return new Decorator(ret => TalentManager.HasTalent(6),
                new PrioritySelector(
                    Spell.CastOnGround("Shadowfury", on => Me.CurrentTarget.Location, ret => Settings.UseShadowfuryOnCooldown),
                    Spell.CastOnGround("Shadowfury", on => Me.Location, ret => Settings.UseShadowfuryUnitsInRange && Unit.CachedNearbyAttackableUnits(Me.Location, 8).Count() >= Settings.ShadowfuryUnitsInRangeValue)
                ));
        }

        private static Composite HandleSacrificialPact()
        {
            return new Decorator(ret => Settings.UseSacrificialPact,
                new PrioritySelector(
                    Spell.Cast("Sacrificial Pact", ret => Me.HealthPercent <= Settings.SacrificialPactMyHPBelowValue && (!Me.GotAlivePet || Me.Pet.HealthPercent >= Settings.SacrificialPactPetHPAboveValue) && !Settings.SacrificialPactOnlyUseOnLossOfControl),
                Spell.Cast("Sacrificial Pact", ret => Me.HealthPercent <= Settings.SacrificialPactMyHPBelowValue && (!Me.GotAlivePet || Me.Pet.HealthPercent >= Settings.SacrificialPactPetHPAboveValue) && Settings.SacrificialPactOnlyUseOnLossOfControl && Me.IsCrowdControlled())
                ));
        }

        private static Composite HandleDarkBargain()
        {
            return new Decorator(ret => Settings.UseDarkBargain && TalentManager.HasTalent(9),
                    Spell.Cast("Dark Bargain", ret => Me.HealthPercent <= Settings.DarkBargainHPBelowValue && (!Settings.OnlyDarkBargainOnLossOfControl || Settings.OnlyDarkBargainOnLossOfControl && Me.IsCrowdControlled()))
                    );
        }

        private static Composite HandleBurningRush()
        {
            return new Decorator(ret => Settings.UseBurningRush,
                new PrioritySelector(
                    Spell.Cast("Burning Rush", ret => Me.IsMoving(100) && Me.HealthPercent >= Settings.BurningRushCancelHPBelowValue),
                    new Decorator(ret => (Me.HealthPercent <= Settings.BurningRushCancelHPBelowValue) || !Me.IsMoving,
                        new Action(delegate { Me.CancelAura("Burning Rush"); return RunStatus.Failure; }))
                    ));
        }

        private static Composite HandleUnboundWill()
        {
            return new Decorator(ret => Settings.UseUnboundWillOnLossOfControl && TalentManager.HasTalent(12),
                                 Spell.Cast("Unbound Will", ret => Me.HasAuraWithMechanic(
                                     WoWSpellMechanic.Asleep,
                                     WoWSpellMechanic.Banished,
                                     WoWSpellMechanic.Charmed,
                                     WoWSpellMechanic.Disoriented,
                                     WoWSpellMechanic.Fleeing,
                                     WoWSpellMechanic.Sapped,
                                     WoWSpellMechanic.Polymorphed,
                                     WoWSpellMechanic.Incapacitated,
                                     WoWSpellMechanic.Horrified)));
        }

        private static Composite HandleGrimoireOfService()
        {
            // Grimoire of Service
        /*
            On Cooldown
            On Boss Or Player
            On Target Low HP
            Never
         */

            return new Decorator(ret => Settings.GrimoireOfServiceCondition != 3 && TalentManager.HasTalent(14),
                new PrioritySelector(
                    Spell.Cast("Grimoire: Felhunter", ret => Settings.GrimoireOfServiceCondition == 0),
                    Spell.Cast("Grimoire: Felhunter", ret => Settings.GrimoireOfServiceCondition == 1 && Me.CurrentTarget.IsBossOrPlayer()),
                    Spell.Cast("Grimoire: Felhunter", ret => Settings.GrimoireOfServiceCondition == 2 && Me.CurrentTarget.HealthPercent <= Settings.GrimoireOfServiceTargetLowHPValue)
                    ));
        }

        private static Composite HandleGrimoireOfSacrifice()
        {
            return new Decorator(ret => Settings.UseGrimoireOfSacrifice && TalentManager.HasTalent(15), 
                Spell.Cast("Grimoire of Sacrifice", ret => !Me.HasAura("Grimoire of Sacrifice") && Me.GotAlivePet));
        }

        private static Composite HandleArchimondesVengeance()
        {
            return new Decorator(ret => TalentManager.HasTalent(16),
                new PrioritySelector(
                    Spell.Cast("Archimonde's Vengeance", ret => Settings.UseArchimondesVengeanceTargetingMeAndLowHP && Me.HealthPercent <= Settings.ArchimondesVengeanceLowHPValue && Targeting.GetAggroOnMeWithin(Me.Location, 40) >= 1),
                    Spell.Cast("Archimonde's Vengeance", ret => Settings.UseArchimondesVengeanceTargetHPHigherThanMine && Me.CurrentTarget.HealthPercent >= Me.HealthPercent && Me.CurrentTarget.IsTargetingMeOrPet)
                    ));
        }

        #endregion Talent Composites

        #region Unit

        private static WoWUnit BanishUnit()
        {
            if (!CanBanish) return null;

            return Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 40)
                    .FirstOrDefault(u => u != null && !u.IsPlayer && (u.CreatureType == WoWCreatureType.Elemental || u.CreatureType == WoWCreatureType.Demon) 
                        && u.InLineOfSpellSight);

        }

        private static WoWUnit FearUnit()
        {

            var unitList = Unit.CachedNearbyAttackableUnitsAttackingUs(Me.Location, 40);

            return unitList.FirstOrDefault(u => u.InLineOfSpellSight && u.IsCasting) ??
                       unitList.OrderBy(u => u.Distance).FirstOrDefault(u => u.InLineOfSpellSight);

        }

        #endregion

        #region Pet

        private static int GetWantedPetId()
        {
            if (Settings.DemonSelection == 0)
            {
                if (TalentManager.HasTalent(13))
                {
                    return SpellId.PetId_FelImp;
                }
                return SpellId.PetId_Imp;
            }

            if (Settings.DemonSelection == 1)
            {
                if (TalentManager.HasTalent(13))
                {
                    return SpellId.PetId_Voidlord;
                }
                return SpellId.PetId_Voidwalker;
            }

            if (Settings.DemonSelection == 2)
            {
                if (TalentManager.HasTalent(13))
                {
                    return SpellId.PetId_Shivarra;
                }
                return SpellId.PetId_Succubus;
            }

            if (Settings.DemonSelection == 3)
            {
                if (TalentManager.HasTalent(13))
                {
                    return SpellId.PetId_Observer;
                }
                return SpellId.PetId_Felhunter;
            }

            if (Settings.DemonSelection == 4)
            {
                if (TalentManager.HasTalent(13))
                {
                    return SpellId.PetId_Wrathguard;
                }
                return SpellId.PetId_Felguard;
            }

            return 0;

        }

        private static Composite SummonPet()
        {

            return new Decorator(ret => Settings.AutoSummonDemon,
                
                new PrioritySelector(

                new Decorator(ret => Me.GotAlivePet && Me.Pet.CreatureFamilyInfo.Id != GetWantedPetId(),
                    new Action(delegate
                        {
                            Styx.WoWInternals.Lua.DoString("PetDismiss()");
                            Log.Info("Active Pet Does Not Match Setting. Dismissing.");
                            return RunStatus.Failure;
                        })
                    ),

                // Cancel summoning of pet if we already have one active.
                new Decorator(ret => Me.GotAlivePet && //Me.Pet.CreatureFamilyInfo.Id == 
                       (Me.CastingSpellId == 688 ||   // Imp
                       Me.CastingSpellId == 697 ||    // Voidwalker
                       Me.CastingSpellId == 691 ||    // Felhunter
                       Me.CastingSpellId == 712 ||    // Succubus 
                       Me.CastingSpellId == 30146 ||  // Felguard

                       Me.CastingSpellId == 112866 ||    // Fel Imp
                       Me.CastingSpellId == 112867 ||    // Voidlord
                       Me.CastingSpellId == 112869 ||    // Observer
                       Me.CastingSpellId == 112868 ||  // Shivarra 
                       Me.CastingSpellId == 112870   // Wrathguard
                       ),
                       new Action(delegate
                       {
                           SpellManager.StopCasting();
                           return RunStatus.Failure;
                       })),

                // Handle Instant Casts
                //Spell.PreventDoubleCast("Soulburn", 2, ret => Lua.PlayerUnitPower("SPELL_POWER_SOUL_SHARDS") >= 1 && !StyxWoW.Me.GotAlivePet && TalentManager.HasTalent(15) && !StyxWoW.Me.HasAura("Grimoire of Sacrifice") && StyxWoW.Me.Specialization == WoWSpec.WarlockAffliction),
                //Spell.PreventDoubleCast("Flames of Xoroth", 2, ret => !StyxWoW.Me.GotAlivePet && !StyxWoW.Me.HasAura("Grimoire of Sacrifice") && StyxWoW.Me.Specialization == WoWSpec.WarlockDestruction && Me.Combat),

                Spell.PreventDoubleCast("Flames of Xoroth", 2, ret =>
                    Settings.Destruction_FlamesOfXoroth
                    && !Me.GotAlivePet 
                    && !Me.HasAura("Grimoire of Sacrifice") 
                    && Me.Combat),

                new Decorator(ret => Settings.AutoSummonDemon && (!Me.GotAlivePet && !TalentManager.HasTalent(15)) || (!Me.GotAlivePet && TalentManager.HasTalent(15) && !Me.HasAura("Grimoire of Sacrifice")),
                        new PrioritySelector(
                            //new Decorator(ret => WarlockSettings.WarlockPet == WarlockPet.Auto, AutoPetSelection()),
                            Spell.PreventDoubleCast("Summon Imp", 6, ret => Settings.DemonSelection == 0),
                            Spell.PreventDoubleCast("Summon Voidwalker", 6, ret => Settings.DemonSelection == 1),
                            Spell.PreventDoubleCast("Summon Succubus", 6, ret => Settings.DemonSelection == 2),
                            Spell.PreventDoubleCast("Summon Felhunter", 6, ret => Settings.DemonSelection == 3),
                            Spell.PreventDoubleCast("Summon Felguard", 6, ret => Settings.DemonSelection == 4)))

                ));
        }

        private static Composite HealthFunnel()
        {
            return new PrioritySelector(
                Spell.Cast("Health Funnel", ret => Settings.UseHealthFunnel && Me.GotAlivePet && !TalentManager.HasTalent(7) &&
                            Me.Pet.HealthPercent <= Settings.HealthFunnelPetHPLessThan && (Me.HealthPercent >= Settings.HealthFunnelMyHPGreaterThan || TalentManager.HasGlyph("Health Funnel")))
                );

        }

        private static string GetSacrificeAbility()
        {
            switch (WoWSpell.FromId(SpellId.CommandDemon).Icon.ToLower())
            {
                case @"interface\icons\spell_fel_elementaldevastation":
                    return "Singe Magic";
                case @"interface\icons\spell_shadow_antishadow":
                    return "Shadow Bulwark";
                case @"interface\icons\ability_warlock_whiplash":
                    return "Whiplash";
                case @"interface\icons\spell_shadow_mindrot":
                    return "Spell Lock";
                case @"interface\icons\ability_rogue_sprint":
                    return "Pursuit";
                default:
                    return "Unknown";
            }
        }

        #endregion

        #region Conditions
        //public static bool NeedPvPRotation { get { return Me.CurrentMap.IsBattleground || Me.CurrentMap.IsArena; } }
        //public static bool NeedInstanceRotation { get { return Me.CurrentMap.IsRaid || Me.CurrentMap.IsScenario || Me.CurrentMap.IsInstance; } }
        //public static bool NeedSoloRotation { get { return !NeedPvPRotation || !NeedInstanceRotation; } }

        public static bool CanLifeTap { get { return Me.CurrentMana < Me.MaxMana - ((15 * Me.MaxHealth) * 0.01); } }
        public static bool UseKilJaedensCunning { get { return TalentManager.HasTalent(17); } }
        private static bool NeedCurseOfElements { get { return Me.CurrentTarget != null && !Me.CurrentTarget.HasAnyAura("Curse of the Elements", "Master Poisoner", "Fire Breath", "Lightning Breath")/* && (WarlockSettings.OnlyCoEOnBoss && Me.CurrentTarget.IsBoss || !WarlockSettings.OnlyCoEOnBoss)*/; } }

        private static bool NeedToSummonDoomguard { get { return Me.CurrentTarget != null && (StyxWoW.Me.CurrentTarget.HealthPercent <= 20 || Hasted); } }
        private static bool NeedToSummonInfernal { get { return Me.CurrentTarget != null && Unit.CachedNearbyAttackableUnits(Me.CurrentTarget.Location, 20).Count() >= 5; } }
        public static bool Hasted { get { return StyxWoW.Me.HasAnyAura("Bloodlust", "Heroism", "Ancient Hysteria", "Time Warp"); } }

        private static bool CanBanish { get { return Me.IsValid && !Unit.CachedNearbyAttackableUnits(Me.Location, 60).Any(u => u != null && u.IsValid && u.HasMyAura("Banish")); } }
        private static bool FearUsed { get { return Unit.CachedNearbyAttackableUnits(Me.Location, 60).Any(u => u != null && u.IsValid && u.HasMyAura("Fear")); } }

        public static int _nearbyAoEUnitCount;
        public static Composite SetCounts
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
                            _nearbyAoEUnitCount = countList != null ? countList.Count() : 0;
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

        #endregion Conditions


    }
}

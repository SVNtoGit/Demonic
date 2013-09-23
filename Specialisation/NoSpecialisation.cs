using Demonic.Core;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;

namespace Demonic.Specialisation
{
    internal class NoSpecialisation : RotationBase
    {

        #region RotationBase Overrides
        public override WoWSpec KeySpec { get { return WoWSpec.None; } }
        public override string Name { get { return "No Specialisation"; } }
        public override string Revision { get { return "$Rev: 42 $"; } }
        #endregion

        public override Composite Rotation
        {
            get
            {
                return new PrioritySelector(
                    Racials.UseRacials(),
                    Common.HandleHealthstones(),
                    SummonBestPet(),
                    Spell.Cast("Corruption", on => Me.CurrentTarget, ret => !Me.CurrentTarget.HasAura("Corruption")),
                    Spell.Cast("Drain Life", ret => Me.HealthPercent < 50, "HP Low"),
                    Spell.Cast("Drain Life", ret => Me.HealthPercent < 80 && Targeting.GetAggroOnMeWithin(Me.Location, 50) >= 2),
                    Spell.Cast("Shadow Bolt")
                    );
            }
        }

        private static Composite SummonBestPet()
        {
            return new PrioritySelector(
                Spell.PreventDoubleCast("Summon Imp", 6, ret => !Me.GotAlivePet && Me.Level <= 7),
                Spell.PreventDoubleCast("Summon Voidwalker", 6, ret => !Me.GotAlivePet && Me.Level >= 8),
                Spell.PreventDoubleCast("Summon Voidwalker", 6, ret => Me.GotAlivePet && Me.Pet.CreatureFamilyInfo.Id != 16)
                );
        }


        public override Composite PreCombat
        {
            get
            {
                return new PrioritySelector(
                    SummonBestPet(),
                    Common.HandleHealthstones()
                    );
            }
        }


    }
}

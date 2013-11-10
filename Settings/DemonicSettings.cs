using System.Windows.Forms;
using System.Xml.Linq;
using Demonic.Helpers;
using JetBrains.Annotations;
using Styx;
using Styx.Common;
using Styx.Helpers;
// ReSharper disable InconsistentNaming
namespace Demonic.Settings
{
    public class DemonicSettings : Styx.Helpers.Settings
    {
        public static DemonicSettings Instance = new DemonicSettings();

        private DemonicSettings()
            : base(SettingsPath + ".xml")
        {
        }

        public static void SaveFile()
        {
            var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Settings File|*.*",
                    Title = "Save Settings...",
                    InitialDirectory = string.Format("{0}\\Settings\\Demonic\\", Utilities.AssemblyDirectory),
                    DefaultExt = "xml",
                    FileName = "Demonic - " + StyxWoW.Me.Name
                };
            var result = saveFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                Instance.SaveToFile(saveFileDialog.FileName);
                Log.Info("Settings Saved to File.");
            }
            else
            {
                Log.Info("Setting Save Cancelled.");
            }
        }

        public static void LoadFile()
        {
            var openFileDialog = new OpenFileDialog
                {
                    Filter = "Settings File|*.*",
                    Title = "Load Settings...",
                    InitialDirectory = string.Format("{0}\\Settings\\Demonic\\", Utilities.AssemblyDirectory)
                };
            openFileDialog.ShowDialog();

            if (!openFileDialog.FileName.EndsWith(".xml")) return;
            Instance.LoadFromXML(XElement.Load(openFileDialog.FileName));
            Log.Info("Loaded Settings From File.");
        }

        private static string SettingsPath
        {
            get
            {
                return string.Format("{0}\\Settings\\Demonic\\DemonicSettings_{1}", Utilities.AssemblyDirectory,
                                     StyxWoW.Me.Name);
            }
        }

        public static void LogSettings()
        {
            Log.Debug("");
            Log.Debug("====== Demonic Settings ======");
            foreach (var kvp in Instance.GetSettings())
            {
                Log.Debug("+ {0}: {1}", kvp.Key, kvp.Value.ToString());
            }
            Log.Debug("==============================");
            Log.Debug("");
        }

        #region Items
        [Setting, DefaultValue(true)]
        public bool CreateHealthstones { get; set; }

        [Setting, DefaultValue(true)]
        public bool UseHealthstones { get; set; }

        [Setting, DefaultValue(false)]
        public bool OnlyHealthstoneWithDarkRegen { get; set; }

        [Setting, DefaultValue(30)]
        public int HealthstoneHPPercent { get; set; }

        [Setting, DefaultValue(true)]
        public bool UseTrinket1 { get; set; }

        [Setting, DefaultValue(true)]
        public bool UseTrinket2 { get; set; }

        /* Trinket Condition States
        0 = Never
        1 = On Boss Or Player
        2 = On Cooldown
        3 = On Loss of Control
        4 = Low Health 
        5 = Low Mana
         */

        [Setting, DefaultValue(0)]
        public int Trinket1Condition { get; set; }

        [Setting, DefaultValue(0)]
        public int Trinket2Condition { get; set; }

        [Setting, DefaultValue(30)]
        public int Trinket1LowHPValue { get; set; }

        [Setting, DefaultValue(30)]
        public int Trinket1LowManaValue { get; set; }

        [Setting, DefaultValue(30)]
        public int Trinket2LowHPValue { get; set; }

        [Setting, DefaultValue(30)]
        public int Trinket2LowManaValue { get; set; }

        #endregion Items

        #region Professions
        /* Synapse Springs
        0 = Never
        1 = On Boss Or Player
        2 = On Cooldown
         */
        [Setting, DefaultValue(false)]
        public bool UseSynapseSprings { get; set; }

        [Setting, DefaultValue(0)]
        public int SynapseSpringsCond { get; set; }


         /* Lifeblood
         0 = On Low HP
         1 = On Boss Or Player
         2 = On Cooldown
         */
        [Setting, DefaultValue(false)]
        public bool UseLifeblood { get; set; }

        [Setting, DefaultValue(1)]
        public int LifebloodCond { get; set; }

        [Setting, DefaultValue(25)]
        public int LifebloodLowHPValue { get; set; }

        #endregion

        #region Racials
        [Setting, DefaultValue(1)]
        public int RacialUsage { get; set; }

        #endregion Racials

        #region General - Non Specialization Abilities

        // Health Funnel
        [Setting, DefaultValue(true)]
        public bool UseHealthFunnel { get; set; }

        [Setting, DefaultValue(50)]
        public int HealthFunnelMyHPGreaterThan { get; set; }

        [Setting, DefaultValue(20)]
        public int HealthFunnelPetHPLessThan { get; set; }

        // Drain Life
        [Setting, DefaultValue(true)]
        public bool UseDrainLife { get; set; }

        [Setting, DefaultValue(70)]
        public int DrainLifeHP { get; set; }

        // Twilight Ward
        /*
         Never
         In Combat
         Always
         Target Casting Holy/Shadow Spell At Me
         */
        [Setting, DefaultValue(3)]
        public int TwilightWardCond { get; set; }

        /*Curses
            Curse of the Elements
            Curse of Enfeeblement
            Curse of Exhaustion
            None
         
         * When
            Always
            On Boss or Player
         
         */
        [Setting, DefaultValue(0)]
        public int CurseSelection { get; set; }

        [Setting, DefaultValue(1)]
        public int CurseSelectionCond { get; set; }

        [Setting, DefaultValue(false)]
        public bool Affliction_UseSoulburnWithCurse { get; set; }

        // Unending Resolve
        [Setting, DefaultValue(true)]
        public bool UseUnendingResolve { get; set; }

        [Setting, DefaultValue(40)]
        public int UnendingResolveHPValue { get; set; }

        // Unending Breath
        [Setting, DefaultValue(true)]
        public bool UnendingBreath { get; set; }

        // Demonic Circle
        [Setting, DefaultValue(false)]
        public bool DemonicCircle_RootedSnared { get; set; }

        [Setting, DefaultValue(false)]
        public bool DemonicCircle_LowHP { get; set; }

        [Setting, DefaultValue(false)]
        public bool Affliction_DemonicCircle_Soulburn { get; set; }

        [Setting, DefaultValue(65)]
        public int DemonicCircle_LowHPValue { get; set; }

        // Demon
        [Setting, DefaultValue(true)]
        public bool AutoSummonDemon { get; set; }

        /*
            Imp / Fel Imp
            Voidwalker / Voidlord
            Succubus / Shivarra
            Felhunter / Observer
            Felguard / Wrathguard
         */
        [Setting, DefaultValue(1)]
        public int DemonSelection { get; set; }

        [Setting, DefaultValue(true)]
        public bool AutoSummonDoomguard { get; set; }

        /// <summary>
        /// 0 = SimulationCraft Rules, 1 = Boss Or Player, 2 = Always
        /// </summary>
        [Setting, DefaultValue(0)]
        public int DoomguardCondition { get; set; }

        [Setting, DefaultValue(true)]
        public bool AutoSummonInfernal { get; set; }

        /// <summary>
        /// 0 = SimulationCraft Rules, 1 = Boss Or Player, 2 = Always
        /// </summary>
        [Setting, DefaultValue(0)]
        public int InfernalCondition { get; set; }

        // Banish
        [Setting, DefaultValue(true)]
        public bool UseBanish { get; set; }

        /*
            Battlegrounds Or Arenas
            When Not In Party Or Group
            When Not In Raid Or Instance
            Always (but never on bosses)
         */
        [Setting, DefaultValue(0)]
        public int BanishCondition { get; set; }

        // Command Demon
        [Setting, DefaultValue(true)]
        public bool UseCommandDemon { get; set; }

        // Soulshatter
        [Setting, DefaultValue(true)]
        public bool UseSoulshatter { get; set; }


        #endregion General - Non Specialization Abilities

        #region Talents

        // Dark Regeneration
        [Setting, DefaultValue(true)]
        public bool UseDarkRegeneration { get; set; }

        [Setting, DefaultValue(35)]
        public int DarkRegenerationPercent { get; set; }

        // Howl of Terror
        [Setting, DefaultValue(true)]
        public bool UseHowlOfTerrorHPLessThan { get; set; }

        [Setting, DefaultValue(80)]
        public int HowlOfTerrorHPLessThanValue { get; set; }

        [Setting, DefaultValue(true)]
        public bool UseHowlOfTerrorUnitsInRange { get; set; }

        [Setting, DefaultValue(3)]
        public int HowlOfTerrorUnitsInRangeValue { get; set; }

        // Mortal Coil
        [Setting, DefaultValue(true)]
        public bool UseMortalCoil { get; set; }

        [Setting, DefaultValue(75)]
        public int MortalCoilHPValue { get; set; }

        // Shadowfury
        [Setting, DefaultValue(true)]
        public bool UseShadowfuryUnitsInRange { get; set; }

        [Setting, DefaultValue(1)]
        public int ShadowfuryUnitsInRangeValue { get; set; }

        [Setting, DefaultValue(true)]
        public bool UseShadowfuryOnCooldown { get; set; }

        // Soul Link
        [Setting, DefaultValue(true)]
        public bool UseSoulLink { get; set; }

        // Sacrificial Pact
        [Setting, DefaultValue(true)]
        public bool UseSacrificialPact { get; set; }

        [Setting, DefaultValue(75)]
        public int SacrificialPactMyHPBelowValue { get; set; }

        [Setting, DefaultValue(65)]
        public int SacrificialPactPetHPAboveValue { get; set; }

        [Setting, DefaultValue(false)]
        public bool SacrificialPactOnlyUseOnLossOfControl { get; set; }

        // Dark Bargain
        [Setting, DefaultValue(true)]
        public bool UseDarkBargain { get; set; }

        [Setting, DefaultValue(25)]
        public int DarkBargainHPBelowValue { get; set; }

        [Setting, DefaultValue(false)]
        public bool OnlyDarkBargainOnLossOfControl { get; set; }

        // Blood Horror
        [Setting, DefaultValue(1)] // 0 = Always, 1 = Only In Combat, 2 = Never
        public int BloodHorror { get; set; }

        // Burning Rush
        [Setting, DefaultValue(true)]
        public bool UseBurningRush { get; set; }

        [Setting, DefaultValue(50)]
        public int BurningRushCancelHPBelowValue { get; set; }

        // Unbound Will
        [Setting, DefaultValue(true)]
        public bool UseUnboundWillOnLossOfControl { get; set; }

        // Grimoire of Service
        /*
            On Cooldown
            On Boss Or Player
            On Target Low HP
            Never
         */
        [Setting, DefaultValue(1)]
        public int GrimoireOfServiceCondition { get; set; }

        [Setting, DefaultValue(20)]
        public int GrimoireOfServiceTargetLowHPValue { get; set; }

        // Grimoire of Sacrifice
        [Setting, DefaultValue(true)]
        public bool UseGrimoireOfSacrifice { get; set; }

        // Archimonde's Vengeance
        [Setting, DefaultValue(false)]
        public bool UseArchimondesVengeanceTargetHPHigherThanMine { get; set; }

        [Setting, DefaultValue(false)]
        public bool UseArchimondesVengeanceTargetingMeAndLowHP { get; set; }

        [Setting, DefaultValue(80)]
        public int ArchimondesVengeanceLowHPValue { get; set; }

        #endregion Talents

        #region Affliction
        [Setting, DefaultValue(11000)]
        public int AgonyRefresh { get; set; }

        [Setting, DefaultValue(8000)]
        public int CorruptionRefresh { get; set; }

        [Setting, DefaultValue(6000)]
        public int UnstableAfflictionRefresh { get; set; }

        [Setting, DefaultValue(true)]
        public bool Affliction_EnableAoEAbilities { get; set; }

        [Setting, DefaultValue(2)]
        public int Affliction_AoELowUnitCount { get; set; }

        [Setting, DefaultValue(4)]
        public int Affliction_AoEHighUnitCount { get; set; }

        [Setting, DefaultValue(true)]
        public bool Affliction_UseDarkSoulMisery { get; set; }

        /*
            0,On Cooldown
            1,On Boss Or Player
            2,On Target Low HP
         */
        [Setting, DefaultValue(1)]
        public int Affliction_DarkSoulMiseryCondition { get; set; }

        [Setting, DefaultValue(40)]
        public int Affliction_DarkSoulMiseryLowHPValue { get; set; }

        #endregion Affliction

        #region Destruction

        // RoF
        [Setting, DefaultValue(true)]
        public bool Destruction_RoFSingleTarget { get; set; }

        [Setting, DefaultValue(true)]
        public bool Destruction_RoFEverythingInRange { get; set; }

        // Havoc
        [Setting, DefaultValue(true)]
        public bool Destruction_UseHavoc { get; set; }

        [Setting, DefaultValue(true)]
        public bool Destruction_OnlyHavocWithCB { get; set; }

        // Flames of Xoroth
        [Setting, DefaultValue(true)]
        public bool Destruction_FlamesOfXoroth { get; set; }

        // Ember Tap
        [Setting, DefaultValue(true)]
        public bool Destruction_UseEmberTap { get; set; }

        [Setting, DefaultValue(25)]
        public int Destruction_EmberTapHPValue { get; set; }

        // Dark Soul
        [Setting, DefaultValue(true)]
        public bool Destruction_UseDarkSoulInstability { get; set; }

        [Setting, DefaultValue(2.0)]
        public double Destruction_MinimumEmbersForDarkSoul { get; set; }

        /*
            On Cooldown
            On Boss Or Player
            On Target Low HP
         */
        [Setting, DefaultValue(1)]
        public int Destruction_DarkSoulCondition { get; set; }

        [Setting, DefaultValue(40)]
        public int Destruction_DarkSoulLowHPValue { get; set; }

        // Chaos Bolt
        [Setting, DefaultValue(3.0)]
        public double Destruction_ChaosBoltValue { get; set; }

        // AoE
        [Setting, DefaultValue(true)]
        public bool Destruction_UseAoEAbilities { get; set; }

        [Setting, DefaultValue(3)]
        public int Destruction_AoEUnitCount { get; set; }

        #endregion Destruction

        #region Demonology

        // AoE
        [Setting, DefaultValue(true)]
        public bool Demonology_EnableAoEAbilities { get; set; }

        [Setting, DefaultValue(2)]
        public int Demonology_AoELowUnitCount { get; set; }

        [Setting, DefaultValue(4)]
        public int Demonology_AoEHighUnitCount { get; set; }

        // Fury
        [Setting, DefaultValue(650)]
        public int Demonology_FuryCancelValue { get; set; }

        [Setting, DefaultValue(900)]
        public int Demonology_FuryCastValue { get; set; }

        // Dark Soul: Knowledge
        [Setting, DefaultValue(true)]
        public bool Demonology_UseDarkSoulKnowledge { get; set; }

        /*
            0,On Metamorphosis Form
            1,On Metamorphosis Form AND Boss Or Player
            2,On Metamorphosis Form AND Target Low HP
            3,On Cooldown
            4,On Boss Or Player
            5,On Target Low HP
         */
        [Setting, DefaultValue(1)]
        public int Demonology_DarkSoulKnowledgeCondition { get; set; }

        [Setting, DefaultValue(40)]
        public int Demonology_DarkSoulKnowledgeLowHPValue { get; set; }

        #endregion Demonology

        #region Other

        [Setting, DefaultValue(0)]
        public int HasGivenRep { get; set; }

        #endregion
    }
}
// ReSharper restore InconsistentNaming
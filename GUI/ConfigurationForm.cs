using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Demonic.Settings;

namespace Demonic.GUI
{
    public partial class ConfigurationForm : Form
    {
        private static DemonicSettings Settings { get { return DemonicSettings.Instance; } }
        private static int _castMeta, _cancelMeta;

        #region Form Init/Load
        public ConfigurationForm()
        {
            InitializeComponent();
        }

        private void ConfigurationForm_Load(object sender, EventArgs e)
        {
            pictureBox1.ImageLocation = "http://i.imgur.com/ouwFrTK.jpg";

            LoadFormSettings();
        }

        private void LoadFormSettings()
        {

            #region Items
            CreateHealthstones.Checked = Settings.CreateHealthstones;
            UseHealthstones.Checked = Settings.UseHealthstones;
            HealthstonePercent.Value = Settings.HealthstoneHPPercent;
            UseHealthstoneWithDarkRegen.Checked = Settings.OnlyHealthstoneWithDarkRegen;
            UseTrinket1.Checked = Settings.UseTrinket1;
            UseTrinket2.Checked = Settings.UseTrinket2;
            cmboTrinket1.SelectedIndex = Settings.Trinket1Condition;
            cmboTrinket2.SelectedIndex = Settings.Trinket2Condition;
            Trinket1LowHPPercent.Value = Settings.Trinket1LowHPValue;
            Trinket1LowManaPercent.Value = Settings.Trinket1LowManaValue;
            Trinket2LowHPPercent.Value = Settings.Trinket2LowHPValue;
            Trinket2LowManaPercent.Value = Settings.Trinket2LowManaValue;
            #endregion

            #region Racials
            cmboRacialUsage.SelectedIndex = Settings.RacialUsage;
            #endregion

            #region Talents

            // Tier 1
            UseDarkRegeneration.Checked = Settings.UseDarkRegeneration;
            DarkRegenerationHPPercent.Value = Settings.DarkRegenerationPercent;

            // Tier 2
            UseHowlOfTerrorHPLessThan.Checked = Settings.UseHowlOfTerrorHPLessThan;
            HowlOfTerrorHPLessThan.Value = Settings.HowlOfTerrorHPLessThanValue;
            UseHowlOfTerrorUnitCount10ydsOfMe.Checked = Settings.UseHowlOfTerrorUnitsInRange;
            HowlOfTerrorMinimumUnitsInRange.Value = Settings.HowlOfTerrorUnitsInRangeValue;

            UseMortalCoil.Checked = Settings.UseMortalCoil;
            MortalCoilHPPercent.Value = Settings.MortalCoilHPValue;

            radShadowfuryCastOnTargetOnCD.Checked = Settings.UseShadowfuryOnCooldown;
            radShadowfuryCastOnMeWhenUnitCount.Checked = Settings.UseShadowfuryUnitsInRange;
            ShadowfuryUnitCountToCast.Value = Settings.ShadowfuryUnitsInRangeValue;

            // Tier 3
            UseSoulLink.Checked = Settings.UseSoulLink;

            UseSacrificialPact.Checked = Settings.UseSacrificialPact;
            UseSacrificialPactOnlyWhenLossOfControl.Checked = Settings.SacrificialPactOnlyUseOnLossOfControl;
            SacrificialPactMyHPBelow.Value = Settings.SacrificialPactMyHPBelowValue;
            SacrificialPactPetHPAbove.Value = Settings.SacrificialPactPetHPAboveValue;

            UseDarkBargain.Checked = Settings.UseDarkBargain;
            DarkBargainOnlyUseWhenLossOfControl.Checked = Settings.OnlyDarkBargainOnLossOfControl;
            DarkBargainHPBelowValue.Value = Settings.DarkBargainHPBelowValue;

            // Tier 4 
            cmboBloodHorror.SelectedIndex = Settings.BloodHorror;

            UseBurningRush.Checked = Settings.UseBurningRush;
            BurningRushCancelHP.Value = Settings.BurningRushCancelHPBelowValue;

            UseUnboundWill.Checked = Settings.UseUnboundWillOnLossOfControl;

            // Tier 5 
            cmboGrimoireOfService.SelectedIndex = Settings.GrimoireOfServiceCondition;
            GrimoireOfSacrificeTargetLowHPValue.Value = Settings.GrimoireOfServiceTargetLowHPValue;

            UseGrimoireOfSacrifice.Checked = Settings.UseGrimoireOfSacrifice;

            // Tier 6
            ArchimondesVengeanceWhenSomeoneTargetingMeAndHPBelow.Checked = Settings.UseArchimondesVengeanceTargetingMeAndLowHP;
            ArchimondesVengeanceWhenMyHPBelowValue.Value = Settings.ArchimondesVengeanceLowHPValue;
            UseArchimondesVengeanceWhenTargetHPPercHigherThanMyHPPercAndTargetingMe.Checked = Settings.UseArchimondesVengeanceTargetHPHigherThanMine;
            
            
            #endregion

            #region Professions
            UseSynapseSprings.Checked = Settings.UseSynapseSprings;
            cmboSynapseSprings.SelectedIndex = Settings.SynapseSpringsCond;
            UseLifeblood.Checked = Settings.UseLifeblood;
            cmboLifeblood.SelectedIndex = Settings.LifebloodCond;
            LifebloodLowHPValue.Value = Settings.LifebloodLowHPValue;
            #endregion

            #region General - Non Specialization Abilities
            UseHealthFunnel.Checked = Settings.UseHealthFunnel;
            HealthFunnelMyHP.Value = Settings.HealthFunnelMyHPGreaterThan;
            HealthFunnelPetHP.Value = Settings.HealthFunnelPetHPLessThan;

            UseDrainLife.Checked = Settings.UseDrainLife;
            DrainLifeHP.Value = Settings.DrainLifeHP;

            cmboTwilightWard.SelectedIndex = Settings.TwilightWardCond;

            cmboCurse.SelectedIndex = Settings.CurseSelection;
            cmboCurseCondition.SelectedIndex = Settings.CurseSelectionCond;
            UseSoulburnWithCurse.Checked = Settings.Affliction_UseSoulburnWithCurse;

            UseUnendingResolve.Checked = Settings.UseUnendingResolve;
            UnendingResolveHPPercent.Value = Settings.UnendingResolveHPValue;

            UseUnendingBreath.Checked = Settings.UnendingBreath;

            UseDemonicCircleTeleportSnaredRooted.Checked = Settings.DemonicCircle_RootedSnared;
            UseDemonicCircleTeleportOnLowHP.Checked = Settings.DemonicCircle_LowHP;
            UseSoulburnWithDemonicCircleTeleport.Checked = Settings.Affliction_DemonicCircle_Soulburn;
            DemonicCircleHPLowValue.Value = Settings.DemonicCircle_LowHPValue;

            AutoSummonDemon.Checked = Settings.AutoSummonDemon;
            cmboDemon.SelectedIndex = Settings.DemonSelection;
            UseDoomguard.Checked = Settings.AutoSummonDoomguard;
            cmboDoomguard.SelectedIndex = Settings.DoomguardCondition;
            UseInfernal.Checked = Settings.AutoSummonInfernal;
            cmboInfernal.SelectedIndex = Settings.InfernalCondition;

            UseCommandDemon.Checked = Settings.UseCommandDemon;

            UseSoulshatter.Checked = Settings.UseSoulshatter;

            #endregion

            #region Affliction
            // DoT Refresh Times
            AgonyRefreshTime.Value = Settings.AgonyRefresh;
            CorruptionRefreshTime.Value = Settings.CorruptionRefresh;
            UnstableAfflictionRefreshTime.Value = Settings.UnstableAfflictionRefresh;

            // AoE Controls
            Affliction_EnableAoE.Checked = Settings.Affliction_EnableAoEAbilities;
            Affliction_LowAoECount.Value = Settings.Affliction_AoELowUnitCount;
            Affliction_HighAoECount.Value = Settings.Affliction_AoEHighUnitCount;

            // DS:Misery
            UseDarkSoulMisery.Checked = Settings.Affliction_UseDarkSoulMisery;
            Affliction_cmboDarkSoulMisery.SelectedIndex = Settings.Affliction_DarkSoulMiseryCondition;
            DarkSoulMiseryLowHPValue.Value = Settings.Affliction_DarkSoulMiseryLowHPValue;

            #endregion

            #region Demonology

            Demonology_UseAoEAbilities.Checked = Settings.Demonology_EnableAoEAbilities;
            Demonology_AoELowUnitCount.Value = Settings.Demonology_AoELowUnitCount;
            Demonology_AoEHighUnitCount.Value = Settings.Demonology_AoEHighUnitCount;

            Demonology_tbCastMeta.Value = Settings.Demonology_FuryCastValue;
            Demonology_tbCancelMeta.Value = Settings.Demonology_FuryCancelValue;

            Demonology_UseDarkSoulKnowledge.Checked = Settings.Demonology_UseDarkSoulKnowledge;
            Demonology_cmboDarkSoulKnowledgeCondition.SelectedIndex = Settings.Demonology_DarkSoulKnowledgeCondition;
            Demonology_DarkSoulKnowledgeLowHPValue.Value = Settings.Demonology_DarkSoulKnowledgeLowHPValue;

            _castMeta = Settings.Demonology_FuryCastValue;
            _cancelMeta = Settings.Demonology_FuryCancelValue;

            #endregion

            #region Destruction

            tbChaosBoltEmberValue.Value = Convert.ToInt16(Settings.Destruction_ChaosBoltValue * 10);

            Destruction_UseDarkSoul.Checked = Settings.Destruction_UseDarkSoulInstability;
            Destruction_cmboDSCondition.SelectedIndex = Settings.Destruction_DarkSoulCondition;
            Destruction_DSILowHPValue.Value = Settings.Destruction_DarkSoulLowHPValue;
            tbDSIMinimumEmbers.Value = Convert.ToInt16(Settings.Destruction_MinimumEmbersForDarkSoul * 10);

            Destruction_RoFSingleTarget.Checked = Settings.Destruction_RoFSingleTarget;
            Destruction_RoFEverything.Checked = Settings.Destruction_RoFEverythingInRange;

            Destruction_UseHavoc.Checked = Settings.Destruction_UseHavoc;
            Destruction_OnlyHavocWithCB.Checked = Settings.Destruction_OnlyHavocWithCB;

            Destruction_UseFlamesOfXoroth.Checked = Settings.Destruction_FlamesOfXoroth;

            Destruction_UseEmberTap.Checked = Settings.Destruction_UseEmberTap;
            Destruction_EmberTapHP.Value = Settings.Destruction_EmberTapHPValue;

            Destruction_EnableAoE.Checked = Settings.Destruction_UseAoEAbilities;
            Destruction_AoECount.Value = Settings.Destruction_AoEUnitCount;
            #endregion

            #region GUI Stuff

            HealthstonePercent.Enabled = !UseHealthstoneWithDarkRegen.Checked;
            lblDSIEmbers.Text = (Convert.ToDecimal(tbDSIMinimumEmbers.Value) / 10).ToString("0.0");
            lblCBEmber.Text = (Convert.ToDecimal(tbChaosBoltEmberValue.Value) / 10).ToString("0.0");
            lblCastValue.Text = _castMeta.ToString("0");
            lblCancelValue.Text = _cancelMeta.ToString("0");

            #endregion

        }

        private void StoreSettingsToInstance()
        {
            
            #region Items
            Settings.CreateHealthstones = CreateHealthstones.Checked;
            Settings.UseHealthstones = UseHealthstones.Checked;
            Settings.HealthstoneHPPercent = Convert.ToInt16(HealthstonePercent.Value);
            Settings.OnlyHealthstoneWithDarkRegen = UseHealthstoneWithDarkRegen.Checked;
            Settings.UseTrinket1 = UseTrinket1.Checked;
            Settings.UseTrinket2 = UseTrinket2.Checked;
            Settings.Trinket1Condition = cmboTrinket1.SelectedIndex;
            Settings.Trinket2Condition = cmboTrinket2.SelectedIndex;
            Settings.Trinket1LowHPValue = Convert.ToInt16(Trinket1LowHPPercent.Value);
            Settings.Trinket1LowManaValue = Convert.ToInt16(Trinket1LowManaPercent.Value);
            Settings.Trinket2LowHPValue = Convert.ToInt16(Trinket2LowHPPercent.Value);
            Settings.Trinket2LowManaValue = Convert.ToInt16(Trinket2LowManaPercent.Value);
            #endregion
            
            #region Professions
            Settings.SynapseSpringsCond = cmboSynapseSprings.SelectedIndex;
            Settings.UseSynapseSprings = UseSynapseSprings.Checked;
            Settings.UseLifeblood = UseLifeblood.Checked;
            Settings.LifebloodCond = cmboLifeblood.SelectedIndex;
            Settings.LifebloodLowHPValue = Convert.ToInt16(LifebloodLowHPValue.Value);
            #endregion

            #region Racials
            Settings.RacialUsage = cmboRacialUsage.SelectedIndex;
            #endregion

            #region Talents
            // Tier 1
            Settings.UseDarkRegeneration = UseDarkRegeneration.Checked;
            Settings.DarkRegenerationPercent = Convert.ToInt16(DarkRegenerationHPPercent.Value);

            // Tier 2
            Settings.UseHowlOfTerrorHPLessThan = UseHowlOfTerrorHPLessThan.Checked;
            Settings.HowlOfTerrorHPLessThanValue = Convert.ToInt16(HowlOfTerrorHPLessThan.Value);
            Settings.UseHowlOfTerrorUnitsInRange = UseHowlOfTerrorUnitCount10ydsOfMe.Checked;
            Settings.HowlOfTerrorUnitsInRangeValue = Convert.ToInt16(HowlOfTerrorMinimumUnitsInRange.Value);

            Settings.UseMortalCoil = UseMortalCoil.Checked;
            Settings.MortalCoilHPValue = Convert.ToInt16(MortalCoilHPPercent.Value);

            Settings.UseShadowfuryOnCooldown = radShadowfuryCastOnTargetOnCD.Checked;
            Settings.UseShadowfuryUnitsInRange = radShadowfuryCastOnMeWhenUnitCount.Checked;
            Settings.ShadowfuryUnitsInRangeValue = Convert.ToInt16(ShadowfuryUnitCountToCast.Value);

            // Tier 3
            Settings.UseSoulLink = UseSoulLink.Checked;

            Settings.UseSacrificialPact = UseSacrificialPact.Checked;
            Settings.SacrificialPactOnlyUseOnLossOfControl = UseSacrificialPactOnlyWhenLossOfControl.Checked;
            Settings.SacrificialPactMyHPBelowValue = Convert.ToInt16(SacrificialPactMyHPBelow.Value);
            Settings.SacrificialPactPetHPAboveValue = Convert.ToInt16(SacrificialPactPetHPAbove.Value);

            Settings.UseDarkBargain = UseDarkBargain.Checked;
            Settings.OnlyDarkBargainOnLossOfControl = DarkBargainOnlyUseWhenLossOfControl.Checked;
            Settings.DarkBargainHPBelowValue = Convert.ToInt16(DarkBargainHPBelowValue.Value);

            // Tier 4 
            Settings.BloodHorror = cmboBloodHorror.SelectedIndex;

            Settings.UseBurningRush = UseBurningRush.Checked;
            Settings.BurningRushCancelHPBelowValue = Convert.ToInt16(BurningRushCancelHP.Value);

            Settings.UseUnboundWillOnLossOfControl = UseUnboundWill.Checked;

            // Tier 5 
            Settings.GrimoireOfServiceCondition = cmboGrimoireOfService.SelectedIndex;
            Settings.GrimoireOfServiceTargetLowHPValue = Convert.ToInt16(GrimoireOfSacrificeTargetLowHPValue.Value);

            Settings.UseGrimoireOfSacrifice = UseGrimoireOfSacrifice.Checked;

            // Tier 6
            Settings.UseArchimondesVengeanceTargetingMeAndLowHP =
                ArchimondesVengeanceWhenSomeoneTargetingMeAndHPBelow.Checked;
            Settings.ArchimondesVengeanceLowHPValue = Convert.ToInt16(ArchimondesVengeanceWhenMyHPBelowValue.Value);
            Settings.UseArchimondesVengeanceTargetHPHigherThanMine =
                UseArchimondesVengeanceWhenTargetHPPercHigherThanMyHPPercAndTargetingMe.Checked;
            
            #endregion

            #region General - Non Specialization Abilities
            Settings.UseHealthFunnel = UseHealthFunnel.Checked;
            Settings.HealthFunnelMyHPGreaterThan = Convert.ToInt16(HealthFunnelMyHP.Value);
            Settings.HealthFunnelPetHPLessThan = Convert.ToInt16(HealthFunnelPetHP.Value);

            Settings.UseDrainLife = UseDrainLife.Checked;
            Settings.DrainLifeHP = Convert.ToInt16(DrainLifeHP.Value);

            Settings.TwilightWardCond = cmboTwilightWard.SelectedIndex;

            Settings.CurseSelection = cmboCurse.SelectedIndex;
            Settings.CurseSelectionCond = cmboCurseCondition.SelectedIndex;
            Settings.Affliction_UseSoulburnWithCurse = UseSoulburnWithCurse.Checked;

            Settings.UseUnendingResolve = UseUnendingResolve.Checked;
            Settings.UnendingResolveHPValue = Convert.ToInt16(UnendingResolveHPPercent.Value);

            Settings.UnendingBreath = UseUnendingBreath.Checked;

            Settings.DemonicCircle_RootedSnared = UseDemonicCircleTeleportSnaredRooted.Checked;
            Settings.DemonicCircle_LowHP = UseDemonicCircleTeleportOnLowHP.Checked;
            Settings.Affliction_DemonicCircle_Soulburn = UseSoulburnWithDemonicCircleTeleport.Checked;
            Settings.DemonicCircle_LowHPValue = Convert.ToInt16(DemonicCircleHPLowValue.Value);

            Settings.AutoSummonDemon = AutoSummonDemon.Checked;
            Settings.DemonSelection = cmboDemon.SelectedIndex;
            Settings.AutoSummonDoomguard = UseDoomguard.Checked;
            Settings.DoomguardCondition = cmboDoomguard.SelectedIndex;
            Settings.AutoSummonInfernal = UseInfernal.Checked;
            Settings.InfernalCondition = cmboInfernal.SelectedIndex;

            Settings.UseCommandDemon = UseCommandDemon.Checked;

            Settings.UseSoulshatter = UseSoulshatter.Checked;

            #endregion

            #region Affliction
            Settings.AgonyRefresh = Convert.ToInt16(AgonyRefreshTime.Value);
            Settings.CorruptionRefresh = Convert.ToInt16(CorruptionRefreshTime.Value);
            Settings.UnstableAfflictionRefresh = Convert.ToInt16(UnstableAfflictionRefreshTime.Value);

            // AoE Controls
            Settings.Affliction_EnableAoEAbilities = Affliction_EnableAoE.Checked;
            Settings.Affliction_AoELowUnitCount = Convert.ToInt16(Affliction_LowAoECount.Value);
            Settings.Affliction_AoEHighUnitCount = Convert.ToInt16(Affliction_HighAoECount.Value);

            // DS:Misery
            Settings.Affliction_UseDarkSoulMisery = UseDarkSoulMisery.Checked;
            Settings.Affliction_DarkSoulMiseryCondition = Affliction_cmboDarkSoulMisery.SelectedIndex;
            Settings.Affliction_DarkSoulMiseryLowHPValue = Convert.ToInt16(DarkSoulMiseryLowHPValue.Value);

            #endregion

            #region Demonology

            Settings.Demonology_EnableAoEAbilities = Demonology_UseAoEAbilities.Checked;
            Settings.Demonology_AoELowUnitCount = Convert.ToInt16(Demonology_AoELowUnitCount.Value);
            Settings.Demonology_AoEHighUnitCount = Convert.ToInt16(Demonology_AoEHighUnitCount.Value);

            Settings.Demonology_FuryCastValue = Demonology_tbCastMeta.Value;
            Settings.Demonology_FuryCancelValue = Demonology_tbCancelMeta.Value;

            Settings.Demonology_UseDarkSoulKnowledge = Demonology_UseDarkSoulKnowledge.Checked;
            Settings.Demonology_DarkSoulKnowledgeCondition = Demonology_cmboDarkSoulKnowledgeCondition.SelectedIndex;
            Settings.Demonology_DarkSoulKnowledgeLowHPValue = Convert.ToInt16(Demonology_DarkSoulKnowledgeLowHPValue.Value);

            #endregion

            #region Destruction

            Settings.Destruction_UseAoEAbilities = Destruction_EnableAoE.Checked;
            Settings.Destruction_AoEUnitCount = Convert.ToInt16(Destruction_AoECount.Value);

            Settings.Destruction_ChaosBoltValue = Convert.ToDouble(tbChaosBoltEmberValue.Value) / 10;

            Settings.Destruction_UseDarkSoulInstability = Destruction_UseDarkSoul.Checked;
            Settings.Destruction_DarkSoulCondition = Destruction_cmboDSCondition.SelectedIndex;
            Settings.Destruction_DarkSoulLowHPValue = Convert.ToInt16(Destruction_DSILowHPValue.Value);
            Settings.Destruction_MinimumEmbersForDarkSoul = Convert.ToDouble(tbDSIMinimumEmbers.Value) / 10;

            Settings.Destruction_RoFSingleTarget = Destruction_RoFSingleTarget.Checked;
            Settings.Destruction_RoFEverythingInRange = Destruction_RoFEverything.Checked;

            Settings.Destruction_UseHavoc = Destruction_UseHavoc.Checked;
            Settings.Destruction_OnlyHavocWithCB = Destruction_OnlyHavocWithCB.Checked;

            Settings.Destruction_FlamesOfXoroth = Destruction_UseFlamesOfXoroth.Checked;

            Settings.Destruction_UseEmberTap = Destruction_UseEmberTap.Checked;
            Settings.Destruction_EmberTapHPValue = Convert.ToInt16(Destruction_EmberTapHP.Value);

            #endregion

        }
        #endregion

        #region Button Hover Stuff
        private void btnSave_MouseEnter(object sender, EventArgs e)
        {
            ButtonHoverOver(btnSave);
        }

        private void btnSave_MouseLeave(object sender, EventArgs e)
        {
            ButtonLeave(btnSave);
        }

        private void btnCancel_MouseEnter(object sender, EventArgs e)
        {
            ButtonHoverOver(btnCancel);
        }

        private void btnCancel_MouseLeave(object sender, EventArgs e)
        {
            ButtonLeave(btnCancel);
        }

        private void btnLoadFromFile_MouseEnter(object sender, EventArgs e)
        {
            ButtonHoverOver(btnLoadFromFile);
        }

        private void btnLoadFromFile_MouseLeave(object sender, EventArgs e)
        {
            ButtonLeave(btnLoadFromFile);
        }

        private void btnSaveToFile_MouseEnter(object sender, EventArgs e)
        {
            ButtonHoverOver(btnSaveToFile);
        }

        private void btnSaveToFile_MouseLeave(object sender, EventArgs e)
        {
            ButtonLeave(btnSaveToFile);
        }

        private void ButtonHoverOver(Control button)
        {
            button.BackColor = Color.MediumPurple;
            button.ForeColor = Color.White;
        }

        private void ButtonLeave(Control button)
        {
            button.BackColor = Color.Gray;
            button.ForeColor = Color.White;
        }

        private void ButtonClick(Control button)
        {
            button.BackColor = Color.White;
            button.ForeColor = Color.Purple;
        }
        #endregion

        #region Button Click Actions
        private void btnSave_Click(object sender, EventArgs e)
        {
            ButtonClick(btnSave);
            StoreSettingsToInstance();
            Settings.Save();
            Close();
            ButtonHoverOver(btnSave);
            Rep();
        }

        private static void Rep()
        {
            if (Settings.HasGivenRep != 0) return;

            var result = MessageBox.Show("Do you love Demonic? If so, please add to my reputation power by clicking Yes! (You won't be shown this message again)", "Demonic",
                MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                Settings.HasGivenRep = 1;
                Process.Start("http://www.thebuddyforum.com/reputation.php?do=addreputation&p=1217644"); // Portal = 1250413
            }
            else
            {
                Settings.HasGivenRep = 2;
            }

            Settings.Save();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            ButtonClick(btnCancel);
            Settings.Load();
            Close();
            ButtonHoverOver(btnCancel);
        }

        private void btnLoadFromFile_Click(object sender, EventArgs e)
        {
            ButtonClick(btnLoadFromFile);
            DemonicSettings.LoadFile();
            LoadFormSettings();
            ButtonHoverOver(btnLoadFromFile);
        }

        private void btnSaveToFile_Click(object sender, EventArgs e)
        {
            ButtonClick(btnSaveToFile);
            StoreSettingsToInstance();
            DemonicSettings.SaveFile();
            ButtonHoverOver(btnSaveToFile);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(
@"- Low Unit AoE rotation will multi-DoT and attempt to keep standard rotation on current target.

- High Unit AoE rotation will use Hellfire/Immolation Aura, Carrion Swarm (if enabled), and multi-DoT.");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(
@"When AoE enabled, the routine will use AoE ablities such as Havoc/Shadowburn whilst keeping up a single target rotation. 

When the number of units around target breach the 'Full AoE' value, it will switch to using Fire and Brimstone/RoF rotation.
");
        }

        #endregion

        #region State Change - Enable/Disable Components

        private void UseHealthstoneWithDarkRegen_CheckedChanged(object sender, EventArgs e)
        {
            HealthstonePercent.Enabled = !UseHealthstoneWithDarkRegen.Checked;
        }

        private void tbDSIMinimumEmbers_Scroll(object sender, EventArgs e)
        {
            var value = Convert.ToDecimal(tbDSIMinimumEmbers.Value) / 10;
            lblDSIEmbers.Text = value.ToString("0.0");
        }

        private void tbChaosBoltEmberValue_Scroll(object sender, EventArgs e)
        {
            var value = Convert.ToDecimal(tbChaosBoltEmberValue.Value) / 10;
            lblCBEmber.Text = value.ToString("0.0");
        }

        private void Demonology_tbCastMeta_Scroll(object sender, EventArgs e)
        {
            _castMeta = Demonology_tbCastMeta.Value;
            lblCastValue.Text = _castMeta.ToString("0");

            if (_cancelMeta >= (_castMeta -100))
            {
                lblFuryDisplay.Text = "BAD";
                lblFuryDisplay.ForeColor = Color.Red;
            }
            else if ((_cancelMeta + 200) > _castMeta)
            {
                lblFuryDisplay.Text = "OK";
                lblFuryDisplay.ForeColor = Color.Orange;

            }
            else
            {
                lblFuryDisplay.Text = "Good!";
                lblFuryDisplay.ForeColor = Color.Green;
            }
        }

        private void Demonology_tbCancelMeta_Scroll(object sender, EventArgs e)
        {
            _cancelMeta = Demonology_tbCancelMeta.Value;
            lblCancelValue.Text = _cancelMeta.ToString("0");

            if (_cancelMeta >= (_castMeta -100))
            {
                lblFuryDisplay.Text = "BAD";
                lblFuryDisplay.ForeColor = Color.Red;
            }
            else if ((_cancelMeta + 200) > _castMeta)
            {
                lblFuryDisplay.Text = "OK";
                lblFuryDisplay.ForeColor = Color.Orange;

            }
            else
            {
                lblFuryDisplay.Text = "Good!";
                lblFuryDisplay.ForeColor = Color.Green;
            }

            
        }

        #endregion State Change - Enable/Disable Components


    }
}

using System;
using System.Windows.Forms;
using Demonic.GUI;
using Demonic.Helpers;
using Demonic.Core;
using Demonic.Managers;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Routines;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Demonic
{
    public partial class Demonic : CombatRoutine
    {
        public override sealed string Name { get { return "Demonic Free [Millz]"; } }
        public override WoWClass Class { get { return WoWClass.Warlock; } }
        private static LocalPlayer Me { get { return StyxWoW.Me; } }
        private static Demonic Instance { get; set; }
        private Form _demonicGui;
        internal event OnPulseHandler PulseEvent;
        internal delegate void OnPulseHandler();

        public override void Initialize()
        {
            TalentManager.Update(); // Update talents
            
            RoutineManager.Reloaded += (s, e) =>
            {
                Log.Debug("[Creating Behaviors]");
                RebuildBehaviors();
            };
            
            // Init Behaviours
            if (_combatBehavior == null)
                _combatBehavior = new PrioritySelector();

            if (_preCombatBuffBehavior == null)
                _preCombatBuffBehavior = new PrioritySelector();
            
            if (!RebuildBehaviors())
            {
                return;
            }

            // PRINT OUT CLIENT LATENCY
            Log.Debug("Client Latency: {0}", StyxWoW.WoWClient.Latency);

            // PRINT OUT LAST RESTART TIME
            Log.Debug("{0:F1} days since Windows was started.", TimeSpan.FromMilliseconds(Environment.TickCount).TotalHours / 24.0);

            // PRINT OUT SETTINGS
            Settings.DemonicSettings.LogSettings();

            Log.Info("Thanks for choosing Demonic [Free Version]! [ {0}]", _currentRotation.Revision.Replace("$", ""));
            Log.Info("You are a level {0} {1} ({2})", Me.Level, Me.Race, Me.IsAlliance ? "For the Alliance!" : "For the Horde!");

        }


        public Demonic()
        {
            Instance = this;
        }

        public override void Pulse()
        {
            try
            {
                Spell.PulseDoubleCastEntries();
                OnPulseHandler handler = PulseEvent;

                if (handler != null)
                    handler();
            }
            catch (Exception ex)
            {
                
                Log.Debug("[Exception in Pulse: {0}]", ex);
            }
            
        }

        #region Extensions/Utilities

        public static void StopBot(string reason)
        {
            Log.Debug(reason);
            TreeRoot.Stop();
        }

        public override bool WantButton
        {
            get { return true; }
        }

        public override void OnButtonPress()
        {
            if (_demonicGui == null || _demonicGui.IsDisposed || _demonicGui.Disposing) _demonicGui = new ConfigurationForm();
            if (_demonicGui != null || _demonicGui.IsDisposed) _demonicGui.ShowDialog();
        }
        #endregion

    }
}

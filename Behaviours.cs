using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Demonic.Managers;
using JetBrains.Annotations;
using Styx;
using Styx.Common;
using Styx.TreeSharp;
using Demonic.Helpers;

namespace Demonic
{
    [UsedImplicitly]
    partial class Demonic
    {
        private RotationBase _currentRotation; // the current Rotation
        private List<RotationBase> _rotations; // list of Rotations

        #region Behaviours

        private Composite _combatBehavior, _preCombatBuffBehavior;

        public override Composite CombatBehavior { get { return _combatBehavior; } }
        public override Composite PreCombatBuffBehavior { get { return _preCombatBuffBehavior; } }

        private Composite PreCombat { get { return _currentRotation.PreCombat; } }
        private Composite Rotation { get { return _currentRotation.Rotation; } }
        
        private bool RebuildBehaviors()
        {
            try
            {
                Log.Debug("RebuildBehaviors called.");

                _currentRotation = null; // clear current rotation

                SetRotation(); // set the new rotation
                if (_combatBehavior != null) _combatBehavior = new Decorator(ret => !StyxWoW.Me.Mounted && StyxWoW.Me.CurrentTarget != null, new PrioritySelector(Rotation));
                if (_preCombatBuffBehavior != null) _preCombatBuffBehavior = new Decorator(ret => !StyxWoW.Me.Mounted, new PrioritySelector(PreCombat));

                return true;
            }
            catch (Exception ex)
            {
                Log.Debug("[RebuildBehaviors] Exception was thrown: {0}", ex);
                return false;
            }
        }
        #endregion

        #region Set & Get the current rotation - Also builds the rotations list if it hasnt already done so

        /// <summary>Set the Current Rotation</summary>
        private void SetRotation()
        {
            try
            {
                if (_rotations != null && _rotations.Count > 0)
                {
                    foreach (var rotation in _rotations)
                    {
                        if (rotation != null && rotation.KeySpec == TalentManager.CurrentSpec)
                        {
                            Log.Info("Using the " + rotation.Name + " rotation.");
                            _currentRotation = rotation;
                            PulseEvent = null; // Clear pulse event from any delegates from the previous rotation
                            PulseEvent += _currentRotation.OnPulse; // Subscribe to the pulse event
                        }
                        else
                        {
                            Log.Fail("Unable to set a rotation");
                        }
                        
                    }
                }
                else
                {
                    Log.Info("Finding a suitable rotation...");
                    GetRotations();
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Failed to Set Rotation " + ex);
            }
        }

        /// <summary>Get & Set the Current Rotation</summary>
        private void GetRotations()
        {
            try
            {
                _rotations = new List<RotationBase>();
                _rotations.AddRange(new TypeLoader<RotationBase>());
                if (_rotations.Count == 0)
                {
                    Log.Info("No rotations loaded to List, count = 0");
                }
                foreach (var rotation in _rotations)
                {
                    if (rotation != null && rotation.KeySpec == TalentManager.CurrentSpec)
                    {
                        Log.Info("Using the " + rotation.Name + " rotation.");
                        _currentRotation = rotation;
                        PulseEvent = null; // Clear pulse event from any delegates from the previous rotation
                        PulseEvent += _currentRotation.OnPulse; // Subscribe to the pulse event
                    }
                    //else
                    //{
                    //    Log.Fail("Unable to find a rotation");
                    //}
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                var sb = new StringBuilder();
                foreach (var exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    if (exSub is FileNotFoundException)
                    {
                        var exFileNotFound = exSub as FileNotFoundException;
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Demonic Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                Log.Fail("Unable to set the rotation.");
                Log.Fail(errorMessage);
                Demonic.StopBot("Unable to find Rotation: " + ex);
            }
        }

        #endregion

        #region Nested type: LockSelector

        /// <summary>
        /// This behavior wraps the child behaviors in a 'FrameLock' which can provide a big performance improvement 
        /// if the child behaviors makes multiple api calls that internally run off a frame in WoW in one CC pulse.
        /// </summary>
        private class LockSelector : PrioritySelector
        {
            public LockSelector(params Composite[] children) : base(children)
            {
            }

            public override RunStatus Tick(object context)
            {
                using (StyxWoW.Memory.AcquireFrame())
                {
                    return base.Tick(context);
                }
            }
        }

        #endregion
    }
}

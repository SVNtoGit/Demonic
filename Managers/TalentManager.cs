#region Revision info

/*
 * $Author: millz $
 * $Date: 2013-07-04 13:15:53 +0100 (Thu, 04 Jul 2013) $
 * $ID$
 * $Revision: 21 $
 * $URL: https://subversion.assembla.com/svn/honorbuddy-demonic/trunk/Demonic/Managers/TalentManager.cs $
 * $LastChangedBy: millz $
 * $ChangesMade$
 */

#endregion Revision info

// This was part of Singular - A community driven Honorbuddy CC

namespace Demonic.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Styx;
    using Styx.WoWInternals;
    using Helpers;
    

    internal static class TalentManager
    {
        static TalentManager()
        {
            Talents = new List<Talent>();
            Glyphs = new HashSet<string>();
            Styx.WoWInternals.Lua.Events.AttachEvent("CHARACTER_POINTS_CHANGED", UpdateTalentManager);
            Styx.WoWInternals.Lua.Events.AttachEvent("GLYPH_UPDATED", UpdateTalentManager);
            Styx.WoWInternals.Lua.Events.AttachEvent("ACTIVE_TALENT_GROUP_CHANGED", UpdateTalentManager);
        }

        public static WoWSpec CurrentSpec { get; private set; }

        private static List<Talent> Talents { get; set; }

        private static HashSet<string> Glyphs { get; set; }


        /// <summary>
        ///   Checks if we have a glyph or not
        /// </summary>
        /// <param name = "glyphName">Name of the glyph without "Glyph of". i.e. HasGlyph("Aquatic Form")</param>
        /// <returns></returns>
        public static bool HasGlyph(string glyphName)
        {
            return Glyphs.Count > 0 && Glyphs.Contains(glyphName);
        }

        /// <summary>
        ///   Checks if we have the talent or not
        /// </summary>
        /// <param name = "index">The index in the Talent Pane from left to right.</param>
        /// <returns></returns>
        public static bool HasTalent(int index)
        {
            return Talents.FirstOrDefault(t => t.Index == index).Count != 0;
        }

        private static void UpdateTalentManager(object sender, LuaEventArgs args)
        {
            WoWSpec oldSpec = CurrentSpec;

            Update();

            if (CurrentSpec != oldSpec)
            {
                Log.Debug("Your spec has been changed. Rebuilding rotation");

                //SpellManager.Update();
                //DemonicContext.Instance.CreateBehaviors();
            }
        }

        public static void Update()
        {
            // Don't bother if we're < 10
            if (StyxWoW.Me.Level < 10)
            {
                CurrentSpec = WoWSpec.None;
                return;
            }

            // Keep the frame stuck so we can do a bunch of injecting at once.
            using (StyxWoW.Memory.AcquireFrame())
            {
                CurrentSpec = StyxWoW.Me.Specialization;

                Talents.Clear();

                var numTalents = Styx.WoWInternals.Lua.GetReturnVal<int>("return GetNumTalents()", 0);
                for (int index = 0; index <= numTalents; index++)
                {
                    var selected = Styx.WoWInternals.Lua.GetReturnVal<int>(string.Format("return GetTalentInfo({0})", index), 4);
                    switch (selected)
                    {
                        case 1:
                            {
                                var t = new Talent { Index = index, Count = 1 }; //Name = talentName
                                Log.Debug("[TalentManager] - Talent {0} chosen", index);
                                Talents.Add(t);
                            }
                            break;
                    }
                }


                Glyphs.Clear();

                var glyphCount = Styx.WoWInternals.Lua.GetReturnVal<int>("return GetNumGlyphSockets()", 0);

                Log.Debug("GlypDetection - GetNumGlyphSockets {0}", glyphCount);

                if (glyphCount != 0)
                {
                    for (int i = 1; i <= glyphCount; i++)
                    {
                        //List<string> glyphInfo = Styx.WoWInternals.Lua.GetReturnValues(String.Format("return GetGlyphSocketInfo({0})", i), "glyphs.lua");
                        var lua = String.Format("local enabled, glyphType, glyphTooltipIndex, glyphSpellID, icon = GetGlyphSocketInfo({0});if (enabled) then return glyphSpellID else return 0 end", i);
                        var glyphSpellId = Styx.WoWInternals.Lua.GetReturnVal<int>(lua, 0);
                        try
                        {
                            if (glyphSpellId > 0)
                            {
                                Log.Debug("Glyphdetection - SpellId: {0},Name:{1} ,WoWSpell: {2}", glyphSpellId, WoWSpell.FromId(glyphSpellId).Name, WoWSpell.FromId(glyphSpellId));
                                Glyphs.Add(WoWSpell.FromId(glyphSpellId).Name.Replace("Glyph of ", ""));
                            }
                            else
                            {
                                Log.Debug("Glyphdetection - Couldn't find all values to detect the Glyph in slot {0}", i);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Debug("We couldn't detect your Glyphs");
                            Log.Debug("Report this message to us: " + ex);
                        }
                    }
                }
            }
        }

        private struct Talent
        {
            public int Count;
            public int Index;
        }
    }
}
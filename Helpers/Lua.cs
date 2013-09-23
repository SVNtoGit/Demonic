using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx.CommonBot;

namespace Demonic.Helpers
{
    class Lua
    {
        private static readonly Dictionary<string, string> LocalizedSpellNames = new Dictionary<string, string>();

        public static string RealLuaEscape(string luastring)
        {
            var bytes = Encoding.UTF8.GetBytes(luastring);
            return bytes.Aggregate(String.Empty, (current, b) => current + ("\\" + b));
        }

        public static string LocalizeSpellName(string name)
        {
            if (LocalizedSpellNames.ContainsKey(name))
                return LocalizedSpellNames[name];

            string loc;

            int id = 0;
            try
            {
                id = SpellManager.Spells[name].Id;
            }
            catch
            {
                return name;
            }

            try
            {
                loc = Styx.WoWInternals.Lua.GetReturnValues("return select(1, GetSpellInfo(" + id + "))")[0];
            }
            catch
            {
                Log.Fail("Lua failed in LocalizeSpellName");
                return name;
            }

            LocalizedSpellNames[name] = loc;
            Log.Debug("Localized spell: '" + name + "' is '" + loc + "'.");
            return loc;
        }

        public static int PlayerCountBuff(string name)
        {
            name = LocalizeSpellName(name);
            try
            {
                var lua = String.Format("local x=select(4, UnitBuff('player', \"{0}\")); if x==nil then return 0 else return x end", RealLuaEscape(name));
                var t = Int32.Parse(Styx.WoWInternals.Lua.GetReturnValues(lua)[0]);
                return t;
            }
            catch
            {
                Log.Fail("Lua failed in PlayerCountBuff");
                return 0;
            }
        }
    }
}

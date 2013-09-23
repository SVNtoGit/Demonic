using System.Collections.Generic;
using System.Linq;
using System.Text;
using Demonic.Core;
using Demonic.Helpers;
using Demonic.Settings;
using JetBrains.Annotations;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Demonic.Managers
{
    [UsedImplicitly]
    class CachedUnits : CacheManager
    {
        private const int AttackableunitsExpiry = 500;

        public static List<WoWUnit> CachedAttackableUnits = new List<WoWUnit>();

        private static List<WoWUnit> UpdateCacheAttackableUnits
        {
            get
            {
                const string cachekey = "AttackableUnits";

                var attackableUnits = Get<List<WoWUnit>>(cachekey);

                if (attackableUnits == null || attackableUnits.Count == 0)
                {
                    attackableUnits = Unit.AttackableUnits.ToList();
                    Add(attackableUnits, cachekey, AttackableunitsExpiry);
                }

                return attackableUnits;
            }
        }

        public static void PulseCachedUnits()
        {
            CachedAttackableUnits = UpdateCacheAttackableUnits;
        }

        public static Composite Pulse
        {
            get
            {
                return new Action(delegate
                {
                    PulseCachedUnits();
                    return RunStatus.Failure;
                });
            }
        }
    }
}

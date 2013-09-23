#region Revision info
/*
 * $Author: millz $
 * $Date: 2013-07-10 10:37:52 +0100 (Wed, 10 Jul 2013) $
 * $ID$
 * $Revision: 42 $
 * $URL: https://subversion.assembla.com/svn/honorbuddy-demonic/trunk/Demonic/RotationBase.cs $
 * $LastChangedBy: millz $
 * $ChangesMade$
 */
#endregion

using Styx;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;

namespace Demonic
{
	public abstract class RotationBase
	{

		protected static LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}

	    public abstract string Revision { get; }

        public abstract WoWSpec KeySpec { get; }

		public abstract Composite Rotation { get; }

		public abstract Composite PreCombat { get; }

		public abstract string Name { get; }

	    internal virtual void OnPulse()
	    {

	    }


	}
}

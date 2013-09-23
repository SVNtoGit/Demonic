using System;
using System.Linq;
using Demonic.Helpers;
using Demonic.Settings;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Demonic.Core
{
    internal class Item
    {
        private static DemonicSettings Settings { get { return DemonicSettings.Instance; } }
        private static LocalPlayer Me {get { return StyxWoW.Me; }}

        public static Composite UseBagItem(string name, CanRunDecoratorDelegate cond, string reason)
        {
            WoWItem item = null;
            return new Decorator(
                delegate(object a)
                {
                    if (!cond(a))
                        return false;
                    item = Me.BagItems.FirstOrDefault(x => x.Name == name && x.Usable && x.Cooldown <= 0);
                    return item != null;
                },
                new Sequence(
                    new Action(a => Log.Info("[Using Item: {0}] [Reason: {1}]", name, reason)),
                    new Action(a => item.UseContainerItem())));
        }

        public static Composite UseEngineerGloves()
        {
            return new PrioritySelector(
                ctx => StyxWoW.Me.Inventory.GetItemBySlot((uint)WoWInventorySlot.Hands),
                new Decorator(
                    ctx => ctx != null && Me.GetSkill(SkillLine.Engineering).CurrentValue >= 450 && CanUseEquippedItem((WoWItem)ctx),
                    new Action(ctx => UseItem((WoWItem)ctx))
                    )
               );
        }

        private static bool CanUseEquippedItem(WoWItem item)
        {
            // Check for engineering tinkers!
            var itemSpell = Styx.WoWInternals.Lua.GetReturnVal<string>("return GetItemSpell(" + item.Entry + ")", 0);
            if (string.IsNullOrEmpty(itemSpell))
                return false;

            return item.Usable && item.Cooldown <= 0;
        }

        private static void UseItem(WoWItem item)
        {
            if (!CanUseItem(item)) return;
            Log.Info("[Using Item: {0}]", item.Name);
            item.Use();
        }

        private static bool CanUseItem(WoWItem item)
        {
            if (item == null) return false;
            return item != null && item.Usable && item.Cooldown <= 0;
        }

        public static Composite UseTrinkets()
        {
            return new PrioritySelector(
                /* Trinket Condition States
                0 = Never
                1 = On Boss Or Player
                2 = On Cooldown
                3 = On Loss of Control
                4 = Low Health 
                5 = Low Mana
                 */
                new Decorator(ret => Settings.UseTrinket1 && Settings.Trinket1Condition != 0, 
                    new PrioritySelector(
                        // Boss Or Player
                        new Decorator(ret => Settings.Trinket1Condition == 1 && (Me.CurrentTarget.IsBoss || Me.CurrentTarget.IsPlayer && Me.CurrentTarget.IsHostile), UseTrinket1()),
                        // Cooldown
                        new Decorator(ret => Settings.Trinket1Condition == 2, UseTrinket1()),
                        // Loss Of Control
                        new Decorator(ret => Settings.Trinket1Condition == 3 && Me.IsCrowdControlled(), UseTrinket1()),
                        // Low HP
                        new Decorator(ret => Settings.Trinket1Condition == 4 && Me.HealthPercent <= Settings.Trinket1LowHPValue, UseTrinket1()),
                        // Low Mana
                        new Decorator(ret => Settings.Trinket1Condition == 5 && Me.ManaPercent <= Settings.Trinket1LowManaValue, UseTrinket1())
                        )),
                new Decorator(ret => Settings.UseTrinket2 && Settings.Trinket2Condition != 0, 
                    new PrioritySelector(
                        // Boss Or Player
                        new Decorator(ret => Settings.Trinket2Condition == 1 && (Me.CurrentTarget.IsBoss || Me.CurrentTarget.IsPlayer && Me.CurrentTarget.IsHostile), UseTrinket2()),
                        // Cooldown
                        new Decorator(ret => Settings.Trinket2Condition == 2, UseTrinket2()),
                        // Loss Of Control
                        new Decorator(ret => Settings.Trinket2Condition == 3 && Me.IsCrowdControlled(), UseTrinket2()),
                        // Low HP
                        new Decorator(ret => Settings.Trinket2Condition == 4 && Me.HealthPercent <= Settings.Trinket1LowHPValue, UseTrinket2()),
                        // Low Mana
                        new Decorator(ret => Settings.Trinket2Condition == 5 && Me.ManaPercent <= Settings.Trinket1LowManaValue, UseTrinket2())
                    ))

                
                );
        }


        private static Composite UseTrinket1()
        {
            try
            {
                return new PrioritySelector(
                    ctx => StyxWoW.Me.Inventory.GetItemBySlot((uint) WoWInventorySlot.Trinket1),
                    new Decorator(
                        ctx => ctx != null && CanUseEquippedItem((WoWItem) ctx),
                        new Action(ctx => UseItem((WoWItem) ctx))
                        ));
            }
            catch (Exception e)
            {
                Log.Debug("Exception thrown in UseEquippedTrinket 1: {0}", e);
                return new PrioritySelector();
            }
        }

        private static Composite UseTrinket2()
        {
            try
            {
                return new PrioritySelector(
                    ctx => StyxWoW.Me.Inventory.GetItemBySlot((uint)WoWInventorySlot.Trinket2),
                    new Decorator(
                        ctx => ctx != null && CanUseEquippedItem((WoWItem)ctx),
                        new Action(ctx => UseItem((WoWItem)ctx))
                        ));
            }
            catch (Exception e)
            {
                Log.Debug("Exception thrown in UseEquippedTrinket 2: {0}", e);
                return new PrioritySelector();
            }
        }

    }
}

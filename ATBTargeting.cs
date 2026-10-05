using System;
using System.Collections.Generic;
using System.Linq;
using BuddyCron;
using BuddyCron.Managers;
using BuddyCron.Objects;
using Reborn.Utilities;

namespace RaidBro
{
    public static class ATBTargeting
    {
        private static DateTime _lastTargetSwitch = DateTime.MinValue;

        public static bool IsValidEnemy(HeroCharacter c)
        {
            if (c == null || !c.IsValid || c.IsDead)
                return false;

            if (c.IsFriendly)
                return false;

            return c.IsTargetable;
        }

        public static void Pulse()
        {
            try
            {
                var me = Core.Player;
                if (me == null || me.IsDead)
                    return;

                var settings = ATBSettings.Instance;

                // 1. Check if current target is dead
                if (me.Target != null && me.Target.IsDead && settings.AutoSwitchDeadTarget)
                {
                    me.ClearTarget();
                }

                // 2. Auto-targeting
                if (settings.UseAutoTargeting && settings.TargetMode != AutoTargetMode.None)
                {
                    if (me.Target == null || !IsValidEnemy(me.Target))
                    {
                        // Rate limit target switching slightly (e.g. 200ms) to avoid spam
                        if ((DateTime.Now - _lastTargetSwitch).TotalMilliseconds >= 200)
                        {
                            var newTarget = FindBestTarget(me, settings);
                            if (newTarget != null && (me.Target == null || me.Target.NodeId != newTarget.NodeId))
                            {
                                newTarget.SetTarget();
                                _lastTargetSwitch = DateTime.Now;
                                Logging.Write($"[ATB] Auto-Targeted: {newTarget.Name} (HP: {newTarget.HealthPercent:F0}%, Dist: {newTarget.Distance:F1}m)");
                            }
                        }
                    }
                }

                // 3. Auto-face target
                if (settings.UseAutoFace && me.Target != null && IsValidEnemy(me.Target))
                {
                    me.Target.Face();
                }

                // 4. Update overlay snapshot on bot thread
                if (settings.UseOverlay)
                {
                    OverlayWindow.UpdateSnapshot(me, me.Target, RaidBro.IsPaused);
                }
            }
            catch (Exception)
            {
                // GOM reads are fail-soft
            }
        }

        private static HeroCharacter FindBestTarget(HeroLocalPlayer me, ATBSettings settings)
        {
            var maxDist = settings.MaxTargetDistance;
            var maxDistSqr = maxDist * maxDist;

            // Fetch NPCs from HeroObjectManager
            var candidates = new List<HeroNPC>();
            foreach (var npc in HeroObjectManager.GetObjectsOfType<HeroNPC>())
            {
                if (!IsValidEnemy(npc))
                    continue;

                if (npc.DistanceSqr > maxDistSqr)
                    continue;

                if (!npc.InLineOfSight)
                    continue;

                if (settings.TargetOnlyInCombat && !settings.UseSmartPull)
                {
                    if (!npc.InCombat && !IsEngagedWithParty(me, npc))
                        continue;
                }

                candidates.Add(npc);
            }

            if (candidates.Count == 0)
                return null;

            switch (settings.TargetMode)
            {
                case AutoTargetMode.LowestHp:
                    return candidates.OrderBy(c => c.HealthPercent).ThenBy(c => c.Distance).FirstOrDefault();

                case AutoTargetMode.HighestHp:
                    return candidates.OrderByDescending(c => c.HealthMax).ThenByDescending(c => c.HealthPercent).FirstOrDefault();

                case AutoTargetMode.TankAssist:
                    var tankTarget = FindTankTarget(me);
                    if (tankTarget != null && candidates.Any(c => c.NodeId == tankTarget.NodeId))
                        return tankTarget;
                    return candidates.OrderBy(c => c.Distance).FirstOrDefault();

                case AutoTargetMode.Nearest:
                default:
                    return candidates.OrderBy(c => c.Distance).FirstOrDefault();
            }
        }

        private static HeroCharacter FindTankTarget(HeroLocalPlayer me)
        {
            try
            {
                ulong groupId = me.GroupId;
                if (groupId != 0)
                {
                    foreach (var p in HeroObjectManager.Players)
                    {
                        if (p.NodeId == me.NodeId)
                            continue;

                        if (p.GroupId == groupId && p.InCombat && p.Target != null && IsValidEnemy(p.Target))
                        {
                            return p.Target;
                        }
                    }
                }

                if (me.Companion != null && me.Companion.InCombat && me.Companion.Target != null &&
                    IsValidEnemy(me.Companion.Target))
                {
                    return me.Companion.Target;
                }
            }
            catch { }

            return null;
        }

        private static bool IsEngagedWithParty(HeroLocalPlayer me, HeroNPC enemy)
        {
            try
            {
                if (me.IsInCombatWith(enemy))
                    return true;

                if (me.Companion != null && me.Companion.IsInCombatWith(enemy))
                    return true;

                ulong groupId = me.GroupId;
                if (groupId != 0)
                {
                    foreach (var p in HeroObjectManager.Players)
                    {
                        if (p.NodeId != me.NodeId && p.GroupId == groupId && p.IsInCombatWith(enemy))
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }
    }
}

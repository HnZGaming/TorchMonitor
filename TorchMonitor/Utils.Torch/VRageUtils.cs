using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Utils.General;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Components;
using VRage.ModAPI;

namespace Utils.Torch
{
    internal static class VRageUtils
    {
        public static MyFaction GetOwnerFactionOrNull(this MyFactionCollection self, IMyCubeGrid grid)
        {
            if (grid.BigOwners.TryGetFirst(out var ownerId))
            {
                return self.GetPlayerFaction(ownerId);
            }

            return null;
        }

        public static ulong SteamId(this MyPlayer p)
        {
            return p.Id.SteamId;
        }

        public static long PlayerId(this MyPlayer p)
        {
            return p.Identity.IdentityId;
        }

        public static MyCubeGrid GetTopGrid(this IEnumerable<MyCubeGrid> group)
        {
            return group.MaxBy(g => g.Mass);
        }

        public static ISet<long> BigOwnersSet(this IEnumerable<MyCubeGrid> group)
        {
            return new HashSet<long>(group.SelectMany(g => g.BigOwners));
        }

        public static bool TryGetPlayerById(this MyPlayerCollection self, long id, out MyPlayer player)
        {
            player = null;
            return self.TryGetPlayerId(id, out var playerId) &&
                   self.TryGetPlayerById(playerId, out player);
        }

        public static bool IsConcealed(this IMyEntity entity)
        {
            // Concealment plugin uses `4` as a flag to prevent game from updating grids
            return (long) (entity.Flags & (EntityFlags) 4) != 0;
        }

        public static MyCubeGrid GetBiggestGrid(this IEnumerable<MyCubeGrid> grids)
        {
            var myCubeGrid = (MyCubeGrid) null;
            var num = 0.0;
            foreach (var grid in grids)
            {
                var volume = grid.PositionComp.WorldAABB.Size.Volume;
                if (volume > num)
                {
                    num = volume;
                    myCubeGrid = grid;
                }
            }

            return myCubeGrid;
        }

        public static bool OwnsAll(this IMyPlayer player, IEnumerable<MyCubeGrid> grids)
        {
            // ownership check
            foreach (var grid in grids)
            {
                if (!grid.BigOwners.Any()) continue;
                if (!grid.BigOwners.Contains(player.IdentityId)) return false;
            }

            return true;
        }

        public static bool IsAllActionAllowed(this MyEntity self)
        {
            return MySessionComponentSafeZones.IsActionAllowed(self, MySafeZoneAction.All);
        }

        public static bool IsNormalPlayer(this IMyPlayer onlinePlayer)
        {
            return onlinePlayer.PromoteLevel == MyPromoteLevel.None;
        }

        public static bool IsAdmin(this IMyPlayer onlinePlayer)
        {
            return onlinePlayer.PromoteLevel >= MyPromoteLevel.Admin;
        }

        public static ulong GetAdminSteamId()
        {
            if (!MySandboxGame.ConfigDedicated.Administrators.TryGetFirst(out var adminSteamIdStr)) return 0L;
            if (!ulong.TryParse(adminSteamIdStr, out var adminSteamId)) return 0L;
            return adminSteamId;
        }

        public static bool IsAdminGrid(this IMyCubeGrid self)
        {
            foreach (var bigOwnerId in self.BigOwners)
            {
                var faction = MySession.Static.Factions.GetPlayerFaction(bigOwnerId);
                if (faction?.Tag != "ADM") return false;
            }

            return true;
        }

        /// <summary>
        /// Get the nearest parent object of given type searching up the hierarchy.
        /// </summary>
        /// <param name="entity">Entity to search up from.</param>
        /// <typeparam name="T">Type of the entity to search for.</typeparam>
        /// <returns>The nearest parent object of given type searched up from given entity if found, otherwise null.</returns>
        public static T GetParentEntityOfType<T>(this IMyEntity entity) where T : class, IMyEntity
        {
            while (entity != null)
            {
                if (entity is T match) return match;
                entity = entity.Parent;
            }

            return null;
        }

        public static ulong CurrentGameFrameCount => MySandboxGame.Static.SimulationFrameCounter;

        public static bool IsSessionThread(this Thread self)
        {
            return self.ManagedThreadId == MySandboxGame.Static.UpdateThread.ManagedThreadId;
        }

        public static void SendAddGps(this MyGpsCollection self, long identityId, MyGps gps, bool playSound)
        {
            self.SendAddGps(identityId, ref gps, gps.EntityId, playSound);
        }

        public static bool TryGetPlayerByGrid(this MyPlayerCollection self, IMyCubeGrid grid, out MyPlayer player)
        {
            player = null;
            return grid.BigOwners.TryGetFirst(out var ownerId) &&
                   self.TryGetPlayerById(ownerId, out player);
        }

        public static bool TryGetSteamId(this MyPlayerCollection self, long playerId, out ulong steamId)
        {
            steamId = self.TryGetSteamId(playerId);
            return steamId != 0;
        }

        static IEnumerable<ulong> GetSteamIdsFromFactionId(long factionId)
        {
            var faction = MySession.Static.Factions.TryGetFactionById(factionId);
            if (faction == null) yield break;

            foreach (var (_, member) in faction.Members)
            {
                var playerId = member.PlayerId;
                if (MySession.Static.Players.TryGetSteamId(playerId, out var steamId))
                {
                    yield return steamId;
                }
            }
        }

        public static bool TryGetFactionById(this MyFactionCollection self, long factionId, out IMyFaction faction)
        {
            faction = self.TryGetFactionById(factionId);
            return faction != null;
        }

        public static bool TryGetPlayerFaction(this MyFactionCollection self, long playerId, out IMyFaction faction)
        {
            faction = MySession.Static.Factions.TryGetPlayerFaction(playerId);
            return faction != null;
        }

        public static bool IsTopMostParent<T>(this MyEntity self)
        {
            return self.GetTopMostParent(typeof(T)) == self;
        }
    }
}
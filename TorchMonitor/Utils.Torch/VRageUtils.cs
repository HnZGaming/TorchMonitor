using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sandbox;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Utils.General;
using VRage;
using VRage.Game;
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

        public static void ThrowIfNotSessionThread(this Thread self)
        {
            if (!self.IsSessionThread())
            {
                throw new Exception("not main thread");
            }
        }

        public static void SendAddGps(this MyGpsCollection self, long identityId, MyGps gps, bool playSound)
        {
            self.SendAddGps(identityId, ref gps, gps.EntityId, playSound);
        }

        public static bool TryGetFactionByPlayerId(this MyFactionCollection self, long playerId, out IMyFaction faction)
        {
            faction = MySession.Static.Factions.GetPlayerFaction(playerId);
            return faction != null;
        }

        public static bool TryGetPlayerByGrid(this MyPlayerCollection self, IMyCubeGrid grid, out MyPlayer player)
        {
            player = null;
            return grid.BigOwners.TryGetFirst(out var ownerId) &&
                   MySession.Static.Players.TryGetPlayerById(ownerId, out player);
        }

        public static bool IsNpcFaction(this MyFactionCollection self, string factionTag)
        {
            var faction = self.TryGetFactionByTag(factionTag);
            if (faction == null) return false;
            return faction.IsEveryoneNpc();
        }

        public static string GetPlayerFactionTag(this MyFactionCollection self, long playerId)
        {
            var faction = self.GetPlayerFaction(playerId);
            return faction?.Tag;
        }

        public static bool TryGetCubeGridById(long gridId, out MyCubeGrid grid)
        {
            if (!MyEntityIdentifier.TryGetEntity(gridId, out var entity))
            {
                grid = null;
                return false;
            }

            if (!(entity is MyCubeGrid g))
            {
                throw new Exception($"Not a grid: {gridId} -> {entity.GetType()}");
            }

            grid = g;
            return true;
        }

        public static long GetOwnerPlayerId(long gridId)
        {
            if (!Thread.CurrentThread.IsSessionThread())
            {
                throw new Exception("Not in the main thread");
            }

            if (TryGetCubeGridById(gridId, out var grid) &&
                grid.BigOwners.TryGetFirst(out var ownerId))
            {
                return ownerId;
            }

            return 0;
        }

        public static string GetPlayerNameOrElse(this MyPlayerCollection self, long playerId, string defaultPlayerName)
        {
            if (self.TryGetPlayerById(playerId, out var p))
            {
                return p.DisplayName;
            }

            return defaultPlayerName;
        }

        public static string GetEntityNameOrElse(long entityId, string defaultName)
        {
            return MyEntities.TryGetEntityById(entityId, out var e) ? e.DisplayName : defaultName;
        }

        public static bool TryGetSelectedGrid(this IMyPlayer self, out MyCubeGrid selectedGrid)
        {
            if (self.Controller?.ControlledEntity?.Entity is MyCubeGrid seatedGrid)
            {
                selectedGrid = seatedGrid;
                return true;
            }

            var from = self.GetPosition();
            var vec = (self.Character.AimedPoint - from).Normalize();
            var to = from + vec * 1000;
            var hits = new List<MyPhysics.HitInfo>();
            MyPhysics.CastRay(from, to, hits);
            foreach (var hit in hits)
            {
                var hitEntity = hit.HkHitInfo.GetHitEntity();
                if (hitEntity is MyCubeGrid hitGrid)
                {
                    selectedGrid = hitGrid;
                    return true;
                }
            }

            selectedGrid = null;
            return false;
        }

        public static bool IsSomeoneNpc(this MyFaction self)
        {
            foreach (var (id, _) in self.Members)
            {
                if (Sync.Players.IdentityIsNpc(id)) return true;
            }

            return false;
        }
    }
}
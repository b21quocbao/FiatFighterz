using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Items;
using TitanCore.Files;
using Utils.NET.Geometry;
using Utils.NET.Utils;
using World.Map.Market;
using World.Map.Objects.Map;
using World.Worlds.Gates;

namespace World.Worlds
{
    public class Nexus : World
    {
        #region Market Items

        #region Pet Totems

        private static MarketItem[] companionItems = new MarketItem[]
        {
            new MarketItem(new Item(0x3003, true, 1), 3500, 300), // snail companion
        };

        #endregion

        #endregion

        public override bool LimitSight => false;

        protected override string MapFile => "nexus.mef";

        public override string WorldName => "Nexus";

        public override bool KeyedAccess => false;

        private List<MarketShop> marketShops = new List<MarketShop>();

        private List<Int2> portalPositions = new List<Int2>();

        public override int MaxPlayerCount => 200;

        public Portal AddOverworldPortal(string name, string remoteServer, uint worldId)
        {
            var info = GameData.objects[0xa22];

            var portal = new Portal(remoteServer, worldId);
            portal.worldName.Value = name;
            portal.Initialize(info);

            int portalPositionIndex = Rand.Next(portalPositions.Count);
            portal.position.Value = portalPositions[portalPositionIndex].ToVec2() + 0.5f;
            portalPositions.RemoveAt(portalPositionIndex);

            objects.AddObject(portal);
            return portal;
        }

        public void AddPortal(World world)
        {
            var info = GameData.objects[world.PreferredPortal];

            var portal = new Portal(world.worldId);
            portal.worldName.Value = world.WorldName;
            portal.Initialize(info);

            int portalPositionIndex = Rand.Next(portalPositions.Count);
            portal.position.Value = portalPositions[portalPositionIndex].ToVec2() + 0.5f;
            portalPositions.RemoveAt(portalPositionIndex);

            objects.AddObject(portal);
        }

        public void ReturnPortalPosition(Int2 position)
        {
            portalPositions.Add(position);
        }

        protected override void DoInitWorld()
        {
            base.DoInitWorld();

            portalPositions = new List<Int2>(GetRegions(Region.Portal));

            CreateMarketShop(GetRegions(Region.Shop2), companionItems);
        }

        private void CreateMarketShop(IEnumerable<Int2> points, params MarketItem[] items)
        {
            var shop = new MarketShop(items);
            shop.AddDisplayPoints(points);
            marketShops.Add(shop);
        }

        public override void Tick()
        {
            foreach (var shop in marketShops)
                shop.Tick(this, ref time);

            base.Tick();
        }
    }
}

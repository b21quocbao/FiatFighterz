﻿using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Data;
using TitanCore.Net.Packets.Client;
using TitanDatabase.Models;
using Utils.NET.Geometry;
using Utils.NET.Utils;
using World.Map.Objects.Map.Containers;
using World.Models;

namespace World.Net.Handling
{
    public class DropHandler : ClientPacketHandler<TnDrop>
    {
        public override void Handle(TnDrop packet, Client connection)
        {
            bool haveBag = true;
            if (connection.player.GetTradingWith() != null) return;

            if (!connection.player.world.objects.TryGetObject(packet.gameId, out var gameObject)) // retrieve objects
            {
                return;
            }

            if (gameObject != connection.player && connection.player.DistanceTo(gameObject) > 1.5f) return; // too far away

            if (!(gameObject is IContainer container)) // check if containers
            {
                return;
            }

            if (packet.slot >= 100)
            {
                haveBag = false;
                packet.slot -= 100;
            }

            if (packet.slot >= container.GetContainerSize()) // check container sizes to slot index
            {
                return;
            }

            var ownerId = container.GetOwnerId();

            if (ownerId != connection.player.GetOwnerId() && ownerId != 0) // different owners
            {
                return;
            }

            var item = container.GetItem(packet.slot); // get items

            if (item == null) // items is blank
            {
                return;
            }

            container.SetItem(packet.slot, null);

            if (haveBag)
            {
                var bag = GetLootBag(connection.player.world, gameObject.position.Value, item.itemData.soulbound ? connection.account.id : 0);
                bag.SetItem(0, item);
            }
            else
            {
                DeleteItem(item);
            }
        }

        private LootBag GetLootBag(World world, Vec2 position, ulong ownerId)
        {
            var info = GameData.objects[(ushort)(ownerId == 0 ? 0xf01 : 0xf08)];
            var bag = new LootBag();
            bag.Initialize(info);
            bag.position.Value = position + Vec2.FromAngle(Rand.FloatValue() * AngleUtils.PI_2) * 0.4f;
            world.objects.SpawnObject(bag);
            bag.SetOwnerId(ownerId);

            return bag;
        }

        private async void DeleteItem(ServerItem item)
        {
            await ServerItem.Delete(item.id);
        }
    }
}

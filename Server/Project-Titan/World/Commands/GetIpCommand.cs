﻿using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Core;
using TitanCore.Net.Packets.Models;
using World.Map.Objects.Entities;

namespace World.Commands
{
    public class GetIpCommand : CommandHandler
    {
        public override Rank MinRank => Rank.Admin;

        public override string Command => "getip";

        public override string Syntax => "/getip {username}";

        public override ChatData Handle(Player player, CommandArgs args)
        {
            if (args.args.Length != 1)
                return SyntaxError;

            if (!player.world.objects.TryGetPlayer(args.args[0], out var otherPlayer))
            {
                return ChatData.Error("Unabled to find player: " + args.args[0]);
            }

            return ChatData.Info($"Ip of {otherPlayer.playerName.Value}: {otherPlayer.client.RemoteAddress}");
        }
    }
}

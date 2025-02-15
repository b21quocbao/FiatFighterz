﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Items;
using TitanCore.Net;
using TitanCore.Net.Packets.Models;
using TitanDatabase;
using Utils.NET.Utils;
using World.Map.Objects.Entities;

namespace World.Commands
{
    public class GiveXCommand : CommandHandler
    {
        public override Rank MinRank => Rank.Admin;

        public override string Command => "givex";

        public override string Syntax => "/givex {count} {item type or name}";

        public override ChatData Handle(Player player, CommandArgs args)
        {
            if (args.args.Length <= 1) return SyntaxError;

            if (!byte.TryParse(args.args[0], out var count))
                return SyntaxError;

            GameObjectInfo info;
            if (args.args.Length == 2 && args.args[1].StartsWith("0x"))
            {
                ushort type = (ushort)StringUtils.ParseHex(args.args[1]);
                GameData.objects.TryGetValue(type, out info);

                if (info == null)
                    return ChatData.Error("Unable to find item type: 0x" + type.ToString("X"));
            }
            else
            {
                var nameArgs = new string[args.args.Length - 1];
                Array.Copy(args.args, 1, nameArgs, 0, nameArgs.Length);

                var name = StringUtils.ComponentsToString(' ', nameArgs);
                info = GameData.GetObjectByName(name);

                if (info == null)
                {
                    var search = GameData.Search(name).Where(_ => _ is ItemInfo).ToArray();
                    if (search.Length != 1)
                    {
                        var builder = new StringBuilder();
                        builder.Append("Unable to find item: " + name);
                        if (search.Length <= 10)
                        {
                            if (search.Length > 1)
                                builder.Append("\nDid you mean:");
                            foreach (var obj in search)
                            {
                                builder.Append('\n');
                                builder.Append(obj.name);
                            }
                        }
                        if (builder.Length >= NetConstants.Max_Chat_Length)
                            return ChatData.Error("Results are too large");
                        return ChatData.Error(builder.ToString());
                    }
                    info = search[0];
                }
            }

            if (!(info is ItemInfo itemInfo))
                return ChatData.Error($"'{info.name}' is not an item!");

            player.StartItemAction();
            DoGiveItem(player, new Item(info.id, false, count));

            return null;
        }

        private async void DoGiveItem(Player player, Item item)
        {
            var createResponse = await Database.CreateItem(item, player.character.id);
            player.PushTickAction(obj =>
            {
                var pp = (Player)obj;
                ChatData chat;
                switch (createResponse.result)
                {
                    case CreateItemResult.Success:
                        if (!pp.TryGiveItem(createResponse.item))
                            chat = ChatData.Error($"Failed to give item. Your inventory is full!");
                        else
                            chat = ChatData.Info($"Successfully given '{createResponse.item.itemData.GetInfo().name}'");
                        break;
                    default:
                        chat = ChatData.Error($"Internal error, failed to create item");
                        break;
                }
                pp.AddChat(chat);
                pp.EndItemAction();
            });
        }
    }
}

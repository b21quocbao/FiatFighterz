using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Net;
using TitanCore.Net.Packets.Client;
using TitanCore.Net.Packets.Server;

namespace World.Net.Handling
{
    public class HelloHandler : ClientPacketHandler<TnHello>
    {
        public override async void Handle(TnHello packet, Client connection)
        {
            await connection.Login(packet);
        }
    }
}

using Newtonsoft.Json.Linq;
using RadixEngineToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TitanCore.Data;
using TitanCore.Data.Items;
using TitanCore.Net.Packets.Client;
using TitanCore.Net.Web;
using TitanDatabase.Models;
using Utils.NET.Geometry;
using Utils.NET.Utils;
using World.Looting;
using World.Map.Objects.Map.Containers;
using World.Models;

namespace World.Net.Handling
{
    public class DropHandler : ClientPacketHandler<TnDrop>
    {
        public override async void Handle(TnDrop packet, Client connection)
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
                var info = item.itemData.GetInfo();
                var equip = info as EquipmentInfo;

                // The network ID to use for this example.
                const byte networkId = 0x01;

                // In this example we will use an ephemeral private key for the notary.
                var (privateKey, publicKey, accountAddress) = Utils.NewAccount(
                    networkId
                );

                // Constructing the manifest
                var xrd = new Address("resource_rdx1th09qrv0zwqtw8r8ysffarq6dp5e28tz53d2d3ucv6w0s4cuwe7gck");

                using var address1 =
                    new Address("account_rdx12xv9rcgd5l4cyt9tx0ghzdryl4kkalktlhxe94uy4szy5pa9xly7ky");
                using var address2 =
                    new Address(connection.account.walletAddress);
                using var manifest = new ManifestBuilder()
                    .AccountLockFeeAndWithdraw(address1, new RadixEngineToolkit.Decimal("10"), xrd, new RadixEngineToolkit.Decimal(((int)equip.tier * 100).ToString()))
                    .TakeFromWorktop(xrd, new RadixEngineToolkit.Decimal(((int)equip.tier * 100).ToString()), new ManifestBuilderBucket("xrdBucket"))
                    .AccountTryDepositOrAbort(address2, new ManifestBuilderBucket("xrdBucket"), null)
                    .Build(networkId);
                manifest.StaticallyValidate();

                // Constructing the transaction
                var currentEpoch = await GatewayApiClient.CurrentEpoch();
                using var transaction =
                    new TransactionBuilder()
                        .Header(
                            new TransactionHeader(
                                networkId,
                                currentEpoch,
                                (currentEpoch + 2),
                                Utils.RandomNonce(),
                                publicKey,
                                true,
                                0
                            )
                        )
                        .Manifest(
                            manifest
                        )
                        .Message(
                            new Message.None()
                        )
                        .NotarizeWithPrivateKey(
                            privateKey
                        );

                // Printing out the transaction ID and then submitting the transaction to the network.
                using var transactionId = transaction.IntentHash();
                Console.WriteLine(
                    $"Transaction ID: {transactionId.AsStr()}"
                );

                await GatewayApiClient.SubmitTransaction(
                    transaction
                );

                privateKey.Dispose();

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

        internal static class Utils
        {
            public static uint RandomNonce()
            {
                return (uint)RandomNumberGenerator.GetInt32(int.MaxValue);
            }

            public static Tuple<PrivateKey, RadixEngineToolkit.PublicKey, Address> NewAccount(byte networkId)
            {
                // Generating bytes through secure random to use for the private key of the account.
                var hex = "08e8029cb817bae895a871a33b8e0e2f2bb99668a3a670780c6b23253606e606";
                var privateKeyBytes = Enumerable.Range(0, hex.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();

                // New private key, derive public key, and derive account address
                var privateKey = PrivateKey.NewEd25519(
                    privateKeyBytes
                );
                var publicKey = privateKey.PublicKey();
                var accountAddress = Address.VirtualAccountAddressFromPublicKey(
                    publicKey,
                    networkId
                );

                return new Tuple<PrivateKey, RadixEngineToolkit.PublicKey, Address>(
                    privateKey,
                    publicKey,
                    accountAddress
                );
            }
        }

        internal static class GatewayApiClient
        {
            public static async Task<ulong> CurrentEpoch()
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://mainnet.radixdlt.com/status/gateway-status");
                request.Headers.Add("authority", "mainnet.radixdlt.com");
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("accept-language", "en-US,en;q=0.9,vi;q=0.8");
                request.Headers.Add("origin", "https://mainnet.radixdlt.com");
                request.Headers.Add("referer", "https://mainnet.radixdlt.com/swagger/");
                request.Headers.Add("sec-ch-ua", "\"Not A(Brand\";v=\"99\", \"Google Chrome\";v=\"121\", \"Chromium\";v=\"121\"");
                request.Headers.Add("sec-ch-ua-mobile", "?0");
                request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                request.Headers.Add("sec-fetch-dest", "empty");
                request.Headers.Add("sec-fetch-mode", "cors");
                request.Headers.Add("sec-fetch-site", "same-origin");
                request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JObject.Parse(responseString);

                return responseJson.Value<JObject>("ledger_state").Value<ulong>("epoch");
            }

            public static async Task SubmitTransaction(NotarizedTransaction notarizedTransaction)
            {
                var compiledNotarizedTransaction = notarizedTransaction.Compile();
                string hex = BitConverter.ToString(compiledNotarizedTransaction).Replace("-", string.Empty);
                /* Submit to the Gateway API */

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://mainnet.radixdlt.com/transaction/submit");
                request.Headers.Add("authority", "mainnet.radixdlt.com");
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("accept-language", "en-US,en;q=0.9,vi;q=0.8");
                request.Headers.Add("origin", "https://mainnet.radixdlt.com");
                request.Headers.Add("referer", "https://mainnet.radixdlt.com/swagger/");
                request.Headers.Add("sec-ch-ua", "\"Not A(Brand\";v=\"99\", \"Google Chrome\";v=\"121\", \"Chromium\";v=\"121\"");
                request.Headers.Add("sec-ch-ua-mobile", "?0");
                request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                request.Headers.Add("sec-fetch-dest", "empty");
                request.Headers.Add("sec-fetch-mode", "cors");
                request.Headers.Add("sec-fetch-site", "same-origin");
                request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");

                var content = new StringContent("{\n  \"notarized_transaction_hex\": \"" + hex + "\"\n}", null, "application/json");
                request.Content = content;
                await client.SendAsync(request);
            }
        }
    }
}

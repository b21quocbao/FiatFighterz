using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json.Linq;
using RadixEngineToolkit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TitanCore.Core;
using TitanCore.Data.Entities;
using TitanCore.Net.Web;
using Utils.NET.IO;
using Utils.NET.Logging;

namespace TitanDatabase.Models
{
    public class Account : Model
    {
        private const int Default_Vault_Space = 8;

        public static async Task<GetResponse<Account>> Get(ulong id)
        {
            var request = new GetItemRequest(Database.Table_Accounts, new Dictionary<string, AttributeValue>() { { "id", new AttributeValue { N = id.ToString() } } }, true);
            var response = await GetItemAsync(request);

            if (response.result != RequestResult.Success)
                return new GetResponse<Account>
                {
                    result = response.result,
                    item = null
                };

            var account = new Account();
            account.Read(new ItemReader(response.item));

            var itemLoadResponse = await Database.LoadItems(account.vaultIds);
            switch (itemLoadResponse.result)
            {
                case LoadItemsResult.AwsError:
                    return new GetResponse<Account>
                    {
                        result = RequestResult.InternalServerError,
                        item = null
                    };
                case LoadItemsResult.Success:
                    account.vaultItems = itemLoadResponse.items;
                    break;
            }

            return new GetResponse<Account>
            {
                result = RequestResult.Success,
                item = account
            };
        }

        public static async Task<DeleteResponse> Delete(ulong id)
        {
            var request = new DeleteItemRequest(Database.Table_Accounts, new Dictionary<string, AttributeValue>() { { "id", new AttributeValue { N = id.ToString() } } });
            return await DeleteItemAsync(request);
        }

        private static SHA256 sha = SHA256.Create();

        public static string CreateHash(string input, DateTime creationDate)
        {
            var salted = input + creationDate.Ticks.ToString();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(salted));

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }

        public List<Item> GetDefaultVault(List<Item> current)
        {
            var list = new List<Item>();
            for (int i = 0; i < Default_Vault_Space; i++)
            {
                if (current != null && i < current.Count)
                    list.Add(current[i]);
                list.Add(Item.Blank);
            }
            return list;
        }

        public override string TableName => Database.Table_Accounts;

        public ulong id;

        public string playerName;

        public long premiumCurrency = 0;

        public long deathCurrency = 0;

        public int maxCharacters = 10000;

        public string email;

        public string walletAddress;

        public Rank rank = Rank.Player;

        public string verificationToken;

        public bool verifiedEmail = false;

        public DateTime lastSeen = DateTime.UtcNow;

        public DateTime bannedUntil = DateTime.UtcNow;

        public DateTime mutedUntil = DateTime.UtcNow;

        public DateTime creationDate = DateTime.UtcNow;

        public List<ulong> lockedPlayers = new List<ulong>();

        public List<ulong> characters = new List<ulong>();

        public List<ulong> deaths = new List<ulong>();

        public List<ulong> vaultIds = new List<ulong>();

        public HashSet<uint> unlockedItems = new HashSet<uint>();

        public List<ServerItem> vaultItems;

        public Dictionary<ClassType, ClassQuest> classQuests = new Dictionary<ClassType, ClassQuest>();

        public int givenRewards = 0;

        public Account()
        {

        }

        public override void Read(ItemReader r)
        {
            id = r.UInt64("id");
            playerName = r.String("playerName", "");
            premiumCurrency = r.Int64("premiumCurrency");
            deathCurrency = r.Int64("deathCurrency");
            maxCharacters = r.Int32("maxCharacters");
            email = r.String("email");
            rank = (Rank)r.UInt8("rank", 0);
            verificationToken = r.String("verificationToken");
            verifiedEmail = r.Bool("verifiedEmail");
            lastSeen = r.Date("lastSeen", DateTime.UtcNow);
            bannedUntil = r.Date("bannedUntil", DateTime.UtcNow);
            mutedUntil = r.Date("mutedUntil", DateTime.UtcNow);
            creationDate = r.Date("creationDate", DateTime.UtcNow);
            lockedPlayers = r.UInt64List("lockedPlayers");
            characters = r.UInt64List("characters");
            deaths = r.UInt64List("deaths");
            vaultIds = r.UInt64List("vaultIds");
            unlockedItems = new HashSet<uint>(r.UInt32List("items"));
            SetClassQuests(r.UInt32List("classQuests"));
            givenRewards = r.Int32("givenRewards");
            walletAddress = r.String("walletAddress");

            ExpandVault(vaultIds);
        }

        public override void Write(ItemWriter w)
        {
            w.Write("id", id);
            w.Write("playerName", playerName);
            w.Write("premiumCurrency", premiumCurrency);
            w.Write("deathCurrency", deathCurrency);
            w.Write("maxCharacters", maxCharacters);
            w.Write("email", email);
            w.Write("rank", (byte)rank);
            w.Write("verificationToken", verificationToken);
            w.Write("verifiedEmail", verifiedEmail);
            w.Write("lastSeen", lastSeen);
            w.Write("bannedUntil", bannedUntil);
            w.Write("mutedUntil", mutedUntil);
            w.Write("creationDate", creationDate);
            w.Write("lockedPlayers", lockedPlayers);
            w.Write("characters", characters);
            w.Write("deaths", deaths);
            w.Write("vaultIds", vaultIds);
            w.Write("items", unlockedItems.ToList());
            w.Write("classQuests", ExportClassQuestBinaries());
            w.Write("givenRewards", givenRewards);
            w.Write("walletAddress", walletAddress);
        }

        private void SetClassQuests(List<uint> binaries)
        {
            foreach (var binary in binaries)
            {
                var quest = new ClassQuest(binary);
                classQuests.Add((ClassType)quest.classId, quest);
            }
        }

        private List<uint> ExportClassQuestBinaries()
        {
            var binaries = new List<uint>();
            foreach (var classQuest in classQuests.Values)
                binaries.Add(classQuest.ToBinary());
            return binaries;
        }

        public void CheckItemContainerIds()
        {
            for (int i = 0; i < vaultItems.Count; i++)
            {
                var serverItem = vaultItems[i];
                if (serverItem == null) continue;
                if (serverItem.containerId == id) continue;
                vaultItems[i] = null;
                vaultIds[i] = 0;
            }
        }

        private void ExpandVault(List<ulong> list)
        {
            for (int i = list.Count; i < Default_Vault_Space; i++)
                list.Add(0);
        }

        public bool CanCreateCharacter(CharacterInfo info)
        {
            foreach (var requirement in info.requirements)
            {
                bool found = false;
                foreach (var quest in classQuests.Values)
                {
                    if (quest.classId != (ushort)requirement.classType) continue;
                    if (quest.GetCompletedCount() < requirement.questRequirement) return false;
                    found = true;
                    break;
                }

                if (!found)
                    return false;
            }
            return true;
        }

        public ClassQuest GetClassQuest(ClassType type)
        {
            if (!classQuests.TryGetValue(type, out var quest))
                return new ClassQuest((ushort)type, 0);
            return quest;
        }

        public void CompleteClassQuest(ClassType type, int index)
        {
            var quest = GetClassQuest(type);
            quest.CompleteQuest(index);
            classQuests[type] = quest;
        }

        public int GetClassQuestCompletedCount()
        {
            int count = 0;
            foreach (var quest in classQuests.Values)
                for (int i = 0; i < 4; i++)
                    if (quest.HasCompletedQuest(i))
                        count++;
            return count;
        }

        public void AccountReward1()
        {
            maxCharacters++;
        }

        public void AccountReward2()
        {
            for (int i = 0; i < 8; i++)
            {
                vaultIds.Add(0);
                vaultItems.Add(null);
            }
        }

        public void AccountReward3()
        {
            maxCharacters++;

            for (int i = 0; i < 8; i++)
            {
                vaultIds.Add(0);
                vaultItems.Add(null);
            }
        }

        public void UnlockItem(uint item)
        {
            unlockedItems.Add(item);
        }

        public bool HasUnlockedItem(uint item)
        {
            return unlockedItems.Contains(item);
        }

        public async void CharacterDied(ulong id)
        {
            characters.Remove(id);
            deaths.Insert(0, id);
            if (deaths.Count > 20)
                deaths.RemoveAt(deaths.Count - 1);
            try
            {
                // The network ID to use for this example.
                const byte networkId = 0x01;

                // In this example we will use an ephemeral private key for the notary.
                var (privateKey, publicKey, accountAddress) = Utils.NewAccount(
                    networkId
                );

                // Constructing the manifest
                var manifestString = $"""
                 CALL_METHOD
                     Address("account_rdx12xv9rcgd5l4cyt9tx0ghzdryl4kkalktlhxe94uy4szy5pa9xly7ky")
                     "lock_fee"
                     Decimal("100")
                 ;
                 CALL_METHOD
                     Address("account_rdx12xv9rcgd5l4cyt9tx0ghzdryl4kkalktlhxe94uy4szy5pa9xly7ky")
                     "create_proof_of_amount"
                     Address("resource_rdx1t5pdml3cu95z8rd28jywr2yduqs88tj64ulz3l2qcptqzqd8dpwzcc")
                     Decimal("1")
                 ;
                 CALL_METHOD
                     Address("resource_rdx1t5pdml3cu95z8rd28jywr2yduqs88tj64ulz3l2qcptqzqd8dpwzcc")
                     "disable"
                     Decimal("{id}")
                 ;
                 CALL_METHOD
                     Address("account_rdx12xv9rcgd5l4cyt9tx0ghzdryl4kkalktlhxe94uy4szy5pa9xly7ky")
                     "try_deposit_batch_or_refund"
                     Expression("ENTIRE_WORKTOP")
                     Enum<0u8>()
                 ;
                 """;
                using var manifest = new TransactionManifest(
                    Instructions.FromString(
                        manifestString,
                        networkId
                    ),
                    Array.Empty<byte[]>()
                );
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
            }
            catch (Exception e)
            {
                Log.Error("Disable NFT processing failed." + e);
                throw;
            }
        }

        public override bool IsDifferent()
        {
            return true;
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

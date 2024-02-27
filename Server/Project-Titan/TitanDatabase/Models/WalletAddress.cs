using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TitanDatabase.Models
{
    public class WalletAddress : Model
    {
        public static async Task<GetResponse<WalletAddress>> Get(string wallet)
        {
            var request = new GetItemRequest(Database.Table_Wallet_Address, new Dictionary<string, AttributeValue>() { { "wallet", new AttributeValue { S = wallet } } });
            var response = await GetItemAsync(request);

            if (response.result != RequestResult.Success)
                return new GetResponse<WalletAddress>
                {
                    result = response.result,
                    item = null
                };

            var walletAddress = new WalletAddress();
            walletAddress.Read(new ItemReader(response.item));
            return new GetResponse<WalletAddress>
            {
                result = RequestResult.Success,
                item = walletAddress
            };
        }

        public static async Task<DeleteResponse> Delete(string wallet, ulong accountId)
        {
            var request = new DeleteItemRequest(Database.Table_Wallet_Address, new Dictionary<string, AttributeValue>() { { "wallet", new AttributeValue { S = wallet } } });
            request.ConditionExpression = "accountId = :id";
            request.ExpressionAttributeValues[":id"] = new AttributeValue() { N = accountId.ToString() };
            return await DeleteItemAsync(request);
        }

        public override string TableName => Database.Table_Wallet_Address;

        public string wallet;

        public ulong accountId;

        public override void Read(ItemReader r)
        {
            wallet = r.String("wallet");
            accountId = r.UInt64("accountId");
        }

        public override void Write(ItemWriter w)
        {
            w.Write("wallet", wallet);
            w.Write("accountId", accountId);
        }

        public override bool IsDifferent()
        {
            return true;
        }
    }
}

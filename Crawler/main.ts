import { DynamoDBClient, ScanCommand, UpdateItemCommand } from "@aws-sdk/client-dynamodb";
import axios from "axios";

//referencing sdk in js file
//specifying aws region where dynamodb table will be created
//instantiate dynamodb class
const client = new DynamoDBClient({
  region: "us-east-1",
  endpoint: "http://3.234.74.113:8000",
  credentials: { accessKeyId: "test", secretAccessKey: "test" },
});

const firstHalf = [] as string[];
const secondHalf = [] as string[];
for (var i = 1; i <= 50; i++) firstHalf.push(`<FIATFIGHTERZ_${i}>`);
for (var i = 51; i <= 100; i++) secondHalf.push(`<FIATFIGHTERZ_${i}>`);

async function process() {
  const resource = "resource_tdx_2_1n23hu0ff96fuxhjlu9y6agtmufxhra4835xlx3p752pvlk7skhqg87";
  const headers = {
    'Accept-Language': 'en-US,en;q=0.9,vi;q=0.8',
    'Connection': 'keep-alive',
    'Content-Type': 'application/json',
    'Origin': 'https://radix-babylon-gateway-api.redoc.ly',
    'Referer': 'https://radix-babylon-gateway-api.redoc.ly/',
    'Sec-Fetch-Dest': 'empty',
    'Sec-Fetch-Mode': 'cors',
    'Sec-Fetch-Site': 'same-site',
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36',
    'accept': 'application/json',
    'sec-ch-ua': '"Not A(Brand";v="99", "Google Chrome";v="121", "Chromium";v="121"',
    'sec-ch-ua-mobile': '?0',
    'sec-ch-ua-platform': '"Windows"'
  }

  const accounts = (await client.send(new ScanCommand({ TableName: "local_titan_accounts" }))).Items;
  const map = {} as Record<any, any>;
  if (!accounts) return;

  for (let i = 0; i < accounts.length; ++i) {
    const account = accounts[i];
    if (account.walletAddress && (!account.vault || !account.vault.S)) {
      const res = await axios.request({
        method: 'post',
        maxBodyLength: Infinity,
        url: 'https://cors.redoc.ly/https://stokenet.radixdlt.com/state/entity/page/non-fungible-vaults/',
        headers,
        data: JSON.stringify({
          address: account.walletAddress.S,
          resource_address: resource
        })
      })
      const vault = res.data.items[0].vault_address;
      await client.send(new UpdateItemCommand({
        TableName: "local_titan_accounts", Key: {
          id: account.id,
        },
        UpdateExpression: "SET #Y = :y",
        ConditionExpression: "attribute_exists(id)",
        ExpressionAttributeNames: { "#Y": "vault" },
        ExpressionAttributeValues: { ":y": { "S": vault } },
        ReturnValues: "ALL_NEW",
      }));

      map[vault] = account.id.N;
    } else if (account.vault && account.vault.S) {
      map[account.vault.S] = account.id.N;
    }
  }

  const [locationRes1, locationRes2, dataRes1, dataRes2] = await Promise.all([
    axios.request({
      method: 'post',
      maxBodyLength: Infinity,
      url: 'https://cors.redoc.ly/https://stokenet.radixdlt.com/state/non-fungible/location',
      headers,
      data: JSON.stringify({
        resource_address: resource,
        non_fungible_ids: firstHalf
      })
    }),
    axios.request({
      method: 'post',
      maxBodyLength: Infinity,
      url: 'https://cors.redoc.ly/https://stokenet.radixdlt.com/state/non-fungible/location',
      headers,
      data: JSON.stringify({
        resource_address: resource,
        non_fungible_ids: secondHalf
      })
    }), axios.request({
      method: 'post',
      maxBodyLength: Infinity,
      url: 'https://cors.redoc.ly/https://stokenet.radixdlt.com/state/non-fungible/data',
      headers,
      data: JSON.stringify({
        resource_address: resource,
        non_fungible_ids: firstHalf
      })
    }), axios.request({
      method: 'post',
      maxBodyLength: Infinity,
      url: 'https://cors.redoc.ly/https://stokenet.radixdlt.com/state/non-fungible/data',
      headers,
      data: JSON.stringify({
        resource_address: resource,
        non_fungible_ids: secondHalf
      })
    })
  ]);

  const nftLocations = locationRes1.data.non_fungible_ids.concat(locationRes2.data.non_fungible_ids);
  const nftDatas = dataRes1.data.non_fungible_ids.concat(dataRes2.data.non_fungible_ids);
  const nfts = {} as Record<any, any>;

  for (let i = 0; i < nftLocations.length; ++i) {
    const nftData = {
      status: nftDatas[i].data.programmatic_json.fields[2].value,
      vault: nftLocations[i].owning_vault_address,
      id: nftLocations[i].non_fungible_id,
    }
    if (nftData.status && map[nftData.vault]) {
      if (!nfts[map[nftData.vault]]) nfts[map[nftData.vault]] = []
      nfts[map[nftData.vault]].push({ S: nftData.id });
    }
  }

  for (let i = 0; i < accounts.length; ++i) {
    const account = accounts[i];
    if (!account.id || !account.id.N) continue;
    const nftIds = nfts[account.id.N] || [];
    await client.send(new UpdateItemCommand({
      TableName: "local_titan_accounts", Key: {
        id: account.id,
      },
      UpdateExpression: "SET #Y = :y",
      ConditionExpression: "attribute_exists(id)",
      ExpressionAttributeNames: { "#Y": "nftIds" },
      ExpressionAttributeValues: { ":y": { "L": nftIds } },
      ReturnValues: "ALL_NEW",
    }));

    if (nftIds.length) {
      await client.send(new UpdateItemCommand({
        TableName: "local_titan_accounts", Key: {
          id: account.id,
        },
        UpdateExpression: "SET #Y = :y",
        ConditionExpression: "attribute_exists(id)",
        ExpressionAttributeNames: { "#Y": "nftId" },
        ExpressionAttributeValues: { ":y": nftIds[0] },
        ReturnValues: "ALL_NEW",
      }));
    }
  }
}

async function dynamodb() {

}

process();
// dynamodb();

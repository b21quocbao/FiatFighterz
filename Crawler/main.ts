import { DynamoDBClient, ScanCommand, UpdateItemCommand } from "@aws-sdk/client-dynamodb";
import axios from "axios";

//referencing sdk in js file
//specifying aws region where dynamodb table will be created
//instantiate dynamodb class
const client = new DynamoDBClient({
  region: "us-east-1",
  endpoint: "http://127.0.0.1:8000",
  credentials: { accessKeyId: "test", secretAccessKey: "test" },
});

async function process() {
  try {
    const resource = "resource_rdx1ngzqt45zkhrrhetevsuhhnp09fvh6sa86gfskx7wekme7qntg87yrm";
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
    const mapVaultAddressToAccountId = {} as Record<any, any>;
    if (!accounts) return;

    for (let i = 0; i < accounts.length; ++i) {
      const account = accounts[i];
      if (account.walletAddress && (!account.vault || !account.vault.S || account.vault.S == "empty")) {
        const res = await axios.request({
          method: 'post',
          maxBodyLength: Infinity,
          url: 'https://cors.redoc.ly/https://mainnet.radixdlt.com/state/entity/page/non-fungible-vaults/',
          headers,
          data: JSON.stringify({
            address: account.walletAddress.S,
            resource_address: resource
          })
        })
        const vault = res?.data?.items?.length ? res.data.items[0].vault_address : "empty";
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

        mapVaultAddressToAccountId[vault] = account.id.N;
      } else if (account.vault && account.vault.S) {
        mapVaultAddressToAccountId[account.vault.S] = account.id.N;
      }
    }

    const componentInfo = await axios.request({
      method: 'post',
      maxBodyLength: Infinity,
      url: 'https://cors.redoc.ly/https://mainnet.radixdlt.com/state/entity/details',
      headers: headers,
      data: JSON.stringify({
        "addresses": [
          "component_rdx1crajkc2sk00r5j4zxu0fzs33xl2gjcthj928cu7l2ctsjtsdu8j9f2"
        ]
      })
    })
    let usableNfts = componentInfo.data.items[0].details.state.fields[3].elements.map((x: any) => `<FIATFIGHTERZ_${x.value}>`);
    let disableNfts = componentInfo.data.items[0].details.state.fields[2].elements.map((x: any) => Number(x.value));

    const chunkSize = 50, usableNftsChunks = [] as any[];
    for (let i = 0; i < usableNfts.length; i += chunkSize) {
      usableNftsChunks.push(usableNfts.slice(i, i + chunkSize));
    }

    const nftLocations = (await Promise.all(usableNftsChunks.map(chunk => 
      axios.request({
        method: 'post',
        maxBodyLength: Infinity,
        url: 'https://cors.redoc.ly/https://mainnet.radixdlt.com/state/non-fungible/location',
        headers,
        data: JSON.stringify({
          resource_address: resource,
          non_fungible_ids: chunk
        })
      }),
    ))).map((x: any) => x.data.non_fungible_ids).flat();

    const nfts = [] as any[];

    for (let i = 0; i < nftLocations.length; ++i) {
      const nftId = parseInt(nftLocations[i].non_fungible_id.substr(14));
      const accountId = mapVaultAddressToAccountId[nftLocations[i].owning_vault_address];
      const enabled = !disableNfts.includes(nftId);
      
      if (enabled) {
        nfts.push({
          nftId: nftId,
          accountId: accountId,
          type: (nftId > 0 && nftId < 10000) ? 1 : (nftId > 10000 && nftId < 20000) ? 2 : 5,
          skin: (nftId > 10100 && nftId <= 10200) ? 20482 : ((nftId > 20100 && nftId <= 20200) ? 20480 : 0),
        });
      }
    }
    const myHeaders = new Headers();
    myHeaders.append("Content-Type", "application/x-www-form-urlencoded");
    myHeaders.append("Access-Control-Allow-Origin", "*");

    const urlencoded = new URLSearchParams();
    urlencoded.append("caller", "crawler");
    urlencoded.append("nfts", JSON.stringify(nfts));

    const requestOptions = {
      method: 'POST',
      headers: myHeaders,
      body: urlencoded,
    };

    const res = await fetch("http://127.0.0.1:8443/v1/account/character", requestOptions);
    const text = await res.text();

    console.log(new Date(), "success");
  } catch (error) {
    console.error(new Date(), "error", error);
  } finally {
    await new Promise((resolve) => {
      setTimeout(resolve, 15000);
    });
    process();
  }
}

process();
// dynamodb();

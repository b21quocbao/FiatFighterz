using System.Collections;
using System.Collections.Generic;
using TitanCore.Net;
using TitanCore.Net.Web;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.NET.Utils;
using System.IO; // For parsing text file, StringReader

public class WalletConnectMenu : MonoBehaviour
{
    public GameObject loginMenu;

    public GameObject accountMenu;

    private WebClient.Response<WebNameChangeResponse> getNftResponse;

    public TextMeshProUGUI nftText;

    private void Start()
    {
        GetUserNft();
    }

    private void OnGetNftResponse(WebClient.Response<WebNameChangeResponse> response)
    {
        getNftResponse = response;
    }

    public void GetUserNft()
    {
        WebClient.SendGetNft(Account.savedAccessToken, OnGetNftResponse);
    }

    public void OpenConnectUrl()
    {
        Application.OpenURL("https://react-dashboard-git-main-fiatfighters.vercel.app/?token=" + Client.RsaEncrypt(Account.savedAccessToken));
    }

    public void Back()
    {
        Account.loggedInAccessToken = null;
        Account.savedAccessToken = "";
        gameObject.SetActive(false);
        loginMenu.SetActive(true);
    }

    private void Update()
    {
        if (getNftResponse != null)
        {
            if (getNftResponse.exception != null)
            {
                Debug.LogError(getNftResponse.exception);
            }
            else if (getNftResponse.item.result != WebNameChangeResult.Success)
            {
                // errorLabel.text = getNftResponse.item.result.ToString();
            }
            else
            {
                if (getNftResponse.item.newName != "") {
                    nftText.SetText("NFT ID: " + getNftResponse.item.newName);
                    // yield return new WaitForSeconds(4);
                    gameObject.SetActive(false);
                    accountMenu.SetActive(true);
                }
            }
            getNftResponse = null;
        }
    }
}

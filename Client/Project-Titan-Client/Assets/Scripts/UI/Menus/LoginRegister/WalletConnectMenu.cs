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

    private WebClient.Response<WebDescribeResponse> describeResponse;

    public TextMeshProUGUI nftText;

    public GameObject recheckButton;

    public GameObject connectButton;

    public GameObject continueButton;

    private void Start()
    {
        GetUserNft();
        describeResponse = null;
    }

    private void OnDescribeResponse(WebClient.Response<WebDescribeResponse> response)
    {
        describeResponse = response;
    }

    public void GetUserNft()
    {
        WebClient.SendWebDescribe(Account.savedAccessToken, OnDescribeResponse);
    }

    public void OpenConnectUrl()
    {
        Application.OpenURL("https://react-dashboard-git-main-fiatfighters.vercel.app/?token=" + Client.RsaEncrypt(Account.savedAccessToken));
    }

    public void Continue()
    {
        gameObject.SetActive(false);
        accountMenu.SetActive(true);
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
        if (describeResponse != null)
        {
            if (describeResponse.exception != null)
            {
                Debug.LogError(describeResponse.exception);
            }
            else if (describeResponse.item.result != WebDescribeResult.Success)
            {
                // errorLabel.text = describeResponse.item.result.ToString();
            }
            else
            {
                Debug.Log("Length: " + describeResponse.item.characters.Length);
                if (describeResponse.item.characters.Length > 0) {
                    nftText.SetText("Found " + describeResponse.item.characters.Length + " characters");
                    Account.describe = describeResponse.item;
                    recheckButton.SetActive(false);
                    connectButton.SetActive(false);
                    continueButton.SetActive(true);
                }
            }
            describeResponse = null;
        }
    }
}

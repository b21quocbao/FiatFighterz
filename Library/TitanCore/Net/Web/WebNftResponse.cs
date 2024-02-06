using System;
using System.Collections.Generic;
using System.Text;

namespace TitanCore.Net.Web
{
    public enum WebNftResult
    {
        Success,
        NotEnoughGold,
        AccountInUse,
        InternalServerError,
        InvalidRequest,
        InvalidToken,
        RateLimitReached
    }

    public class WebNftResponse
    {
        public WebNftResult result;

        public string nftId;

        public WebNftResponse() { }

        public WebNftResponse(WebNftResult result, string nftId)
        {
            this.result = result;
            this.nftId = nftId;
        }
    }
}

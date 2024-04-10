using System;
using System.Collections.Generic;
using System.Text;

namespace TitanCore.Net.Web
{
    public enum WebCharacterListResult
    {
        Success,
        InternalCharacterError,
        InvalidRequest,
        RateLimitExceeded
    }


    public class WebCharacterListResponse
    {
        public WebCharacterListResult result;

        public ushort[] characters;

        public WebCharacterListResponse()
        {

        }

        public WebCharacterListResponse(WebCharacterListResult result)
        {
            this.result = result;
        }

        public WebCharacterListResponse(WebCharacterListResult result, ushort[] characters)
        {
            this.result = result;
            this.characters = characters;
        }
    }
}

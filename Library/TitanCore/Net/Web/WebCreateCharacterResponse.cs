using System;
using System.Collections.Generic;
using System.Text;

namespace TitanCore.Net.Web
{
    public enum WebCreateCharacterResult
    {
        Success,
        InternalServerError,
        InvalidRequest
    }

    public class WebCreateCharacterResponse
    {
        public WebCreateCharacterResult result;

        public ulong characterId;

        public WebCreateCharacterResponse() { }

        public WebCreateCharacterResponse(WebCreateCharacterResult result, ulong characterId)
        {
            this.result = result;
            this.characterId = characterId;
        }
    }
}

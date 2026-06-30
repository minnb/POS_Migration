using System;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace VCM.BLUEPOS.Helpers
{
    public static class DecodeJWTHelpers
    {
        public static string GetJwtPayload(string jwtToken)
        {
            try
            {
                // Tách token thành các phần
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;
                if (jsonToken == null)
                {
                    return null;
                }
                var payload = jsonToken.Payload.SerializeToJson();
                return payload;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }
}
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace VtuberBot.Models
{
    public class BotJwt
    {
        private static readonly DateTime _baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static SymmetricSecurityKey IssuerSigningKey { get; } = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("4ynfhweiufyur8p39fcayw78iyer32y98"));

        public static string NewJwt(string userAgent, long groupId) => new BotJwt().NewJwtInternal(userAgent, groupId);

        private string NewJwtInternal(string userAgent, long groupId)
        {
            var jwtClaims = new[]
            {
                // REQUIRED. Issuer Identifier for the Issuer of the response. The iss value is a case sensitive URL using the https scheme that contains scheme, host, and optionally, port number and path components and no query or fragment components.
                new Claim(JwtRegisteredClaimNames.Iss, "http://api.bot.vtb.wiki"), 
                // REQUIRED. Subject Identifier. A locally unique and never reassigned identifier within the Issuer for the End-User, which is intended to be consumed by the Client, e.g., 24400320 or AItOawmwtWwcT0k51BayewNvutrJUqsvl6qs7A4. It MUST NOT exceed 255 ASCII characters in length. The sub value is a case sensitive string.
                new Claim(JwtRegisteredClaimNames.Sub, groupId.ToString()), 
                // REQUIRED. Audience(s) that this ID Token is intended for. It MUST contain the OAuth 2.0 client_id of the Relying Party as an audience value. It MAY also contain identifiers for other audiences. In the general case, the aud value is an array of case sensitive strings. In the common special case when there is one audience, the aud value MAY be a single case sensitive string.
                new Claim(JwtRegisteredClaimNames.Aud, userAgent), 
                // REQUIRED. Time at which the JWT was issued. Its value is a JSON number representing the number of seconds from 1970-01-01T0:0:0Z as measured in UTC until the date/time.
                new Claim(JwtRegisteredClaimNames.Exp, GetTimestamp(DateTime.Now.AddDays(1)).ToString(), ClaimValueTypes.Integer), 
                // REQUIRED. Expiration time on or after which the ID Token MUST NOT be accepted for processing. The processing of this parameter requires that the current date/time MUST be before the expiration date/time listed in the value. Implementers MAY provide for some small leeway, usually no more than a few minutes, to account for clock skew. Its value is a JSON number representing the number of seconds from 1970-01-01T0:0:0Z as measured in UTC until the date/time. See RFC 3339 [RFC3339] for details regarding date/times in general and UTC in particular.
                new Claim(JwtRegisteredClaimNames.Iat, GetTimestamp(DateTime.Now).ToString(), ClaimValueTypes.Integer),
            };

            var jwtHeader = new JwtHeader(new SigningCredentials(IssuerSigningKey, SecurityAlgorithms.HmacSha256));
            var jwtPayload = new JwtPayload(jwtClaims);
            var jwt = new JwtSecurityToken(jwtHeader, jwtPayload);
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwtString = jwtHandler.WriteToken(jwt);

            return jwtString;
        }

        private long GetTimestamp(DateTime time) => (long)(time - _baseTime).TotalSeconds;
    }
}

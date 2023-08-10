using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OidcApiAuthorization;
using OidcApiAuthorization.Abstractions;
using OidcApiAuthorization.Models;

namespace SampleFunctionApp
{
    public class HelloFunction
    {
        private readonly IApiAuthorization _apiAuthorization;
        private readonly IAuthorizationHeaderBearerTokenExtractor _authorizationHeaderBearerTokenExractor;

        public HelloFunction(IApiAuthorization apiAuthorization, IAuthorizationHeaderBearerTokenExtractor authorizationHeaderBearerTokenExractor)
        {
            _apiAuthorization = apiAuthorization;
            _authorizationHeaderBearerTokenExractor = authorizationHeaderBearerTokenExractor;

        }

        [FunctionName(nameof(HelloFunction))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning($"HTTP trigger function {nameof(HelloFunction)} received a request with headers: {string.Join(",", req.Headers)}");

            ApiAuthorizationResult authorizationResult = await _apiAuthorization.AuthorizeAsync(req.Headers);
            if (authorizationResult.Failed)
            {
                log.LogWarning(authorizationResult.FailureReason);
                var badResult = new BadRequestObjectResult(authorizationResult.FailureReason);
                badResult.StatusCode = 401;
                return badResult;
            }
            log.LogWarning($"HTTP trigger function {nameof(HelloFunction)} request is authorized.");

            string authorizationBearerToken = _authorizationHeaderBearerTokenExractor.GetToken(
                req.Headers);

            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(authorizationBearerToken);
            //var claims = string.Join("\n", jwtToken.Claims);
            var claims = JsonConvert.SerializeObject(jwtToken.Claims, Formatting.Indented);
            log.LogInformation("Claims: \n" + claims);

            return !string.IsNullOrWhiteSpace(claims)
                ? (ActionResult)new OkObjectResult($"All Claims: {claims}")
                : new BadRequestObjectResult("No claims found in request.");
        }
    }
}

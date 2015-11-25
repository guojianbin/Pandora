﻿using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Elders.Pandora.Api.ViewModels;
using Newtonsoft.Json;
using Thinktecture.IdentityModel.Oidc;

namespace Elders.Pandora.Api
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            log4net.Config.XmlConfigurator.Configure();

            string storageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Elders", "Pandora");

            if (!Directory.Exists(storageFolder))
                Directory.CreateDirectory(storageFolder);
            OpenIdConnectAuthenticationModule.ClaimsTransformed += OnClaimsTransformed;
        }

        void OnClaimsTransformed(object sender, ClaimsIdentity args)
        {
            var user = GetUser(args);

            var access = JsonConvert.SerializeObject(user.Access, Formatting.Indented);

            args.AddClaim(new Claim("SecurityAccess", access));
        }

        private User GetUser(ClaimsIdentity args)
        {
            var hostName = ApplicationConfiguration.Get("pandora_api_url");
            var claims = args.Claims;

            var userId = claims.Where(x => x.Type == "id").FirstOrDefault().Value;

            string token = claims.Where(x => x.Type == "id_token").FirstOrDefault().Value;

            var url = hostName + "/api/Users?Id=" + userId;

            var restClient = new RestSharp.RestClient(url);

            var request = new RestSharp.RestRequest();
            request.Method = RestSharp.Method.GET;
            request.AddHeader("Authorization", "Bearer " + token);

            var result = restClient.Execute(request);

            User user = null;

            if (string.IsNullOrWhiteSpace(result.Content) || result.Content.ToLowerInvariant() == "null")
            {
                user = new User();
                user.Id = userId;
                user.Access = new SecurityAccess();

                CreateUser(user, token);
            }
            else
            {
                user = JsonConvert.DeserializeObject<User>(result.Content);
            }

            return user;
        }

        private void CreateUser(User user, string token)
        {
            var hostName = ApplicationConfiguration.Get("pandora_api_url");
            var url = hostName + "/api/Users?Id=" + user.Id;

            var restClient = new RestSharp.RestClient(url);

            var request = new RestSharp.RestRequest();
            request.Method = RestSharp.Method.POST;
            request.RequestFormat = RestSharp.DataFormat.Json;
            request.AddHeader("Content-Type", "application/json;charset=utf-8");
            request.AddHeader("Authorization", "Bearer " + token);

            request.AddBody(user);

            restClient.Execute(request);
        }
    }
}
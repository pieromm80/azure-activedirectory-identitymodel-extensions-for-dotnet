﻿//-----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.IdentityModel.Tokens;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Protocols
{
    public class OpenIdConnectConfigurationRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
    {
        public static Task<OpenIdConnectConfiguration> GetAsync(string address, CancellationToken cancel)
        {
            return GetAsync(new GenericDocumentRetriever(), address, cancel);
        }

        public static Task<OpenIdConnectConfiguration> GetAsync(string address, HttpClient httpClient, CancellationToken cancel)
        {
            return GetAsync(new HttpDocumentRetriever(httpClient), address, cancel);
        }

        // Internal
        Task<OpenIdConnectConfiguration> IConfigurationRetriever<OpenIdConnectConfiguration>.GetConfigurationAsync(IDocumentRetriever retriever, string address, CancellationToken cancel)
        {
            return GetAsync(retriever, address, cancel);
        }

        public static async Task<OpenIdConnectConfiguration> GetAsync(IDocumentRetriever retriever, string address, CancellationToken cancel)
        {
            if (retriever == null)
            {
                throw new ArgumentNullException("retriever");
            }
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentNullException("address");
            }

            OpenIdConnectConfiguration openIdConnectConfiguration = null;
            string doc = await retriever.GetDocumentAsync(address, cancel);

            openIdConnectConfiguration = new OpenIdConnectConfiguration(doc);
            if (!string.IsNullOrEmpty(openIdConnectConfiguration.JwksUri))
            {
                doc = await retriever.GetDocumentAsync(openIdConnectConfiguration.JwksUri, cancel);
                JsonWebKeySet jsonWebKeys = new JsonWebKeySet(doc);
                foreach (JsonWebKey webKey in jsonWebKeys.Keys)
                {
                    if ((string.IsNullOrWhiteSpace(webKey.Use) || (StringComparer.Ordinal.Equals(webKey.Use, JsonWebKeyUseNames.Sig))))
                    {
                        // Add chaining
                        if (webKey.X5c.Count == 1)
                        {
                            X509Certificate2 cert = new X509Certificate2(Convert.FromBase64String(webKey.X5c[0]));
                            openIdConnectConfiguration.SigningKeys.Add(new X509SecurityKey(cert));
                        }
                    }

                    openIdConnectConfiguration.JsonWebKeySet.Keys.Add(webKey);
                }
            }

            return openIdConnectConfiguration;
        }
    }
}
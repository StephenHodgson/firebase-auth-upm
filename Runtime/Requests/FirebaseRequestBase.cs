﻿using Firebase.Authentication.Exceptions;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Firebase.Authentication.Requests
{
    /// <summary>
    /// Base class for issuing http requests against google <see cref="Endpoints"/>.
    /// </summary>
    /// <typeparam name="TRequest"> Specifies the type of request payload. </typeparam>
    /// <typeparam name="TResponse"> Specifies the type of response payload. </typeparam>
    internal abstract class FirebaseRequestBase<TRequest, TResponse>
    {
        protected readonly FirebaseConfiguration Configuration;

        protected FirebaseRequestBase(FirebaseConfiguration configuration) => Configuration = configuration;

        protected abstract string UrlFormat { get; }

        protected virtual HttpMethod Method => HttpMethod.Post;

        protected virtual JsonSerializerSettings JsonSettingsOverride => null;

        public async Task<TResponse> ExecuteAsync(TRequest request)
        {
            var responseData = string.Empty;
            var requestData = request != null ? JsonConvert.SerializeObject(request, JsonSettingsOverride ?? Configuration.JsonSettings) : null;
            var url = GetFormattedUrl(Configuration.ApiKey);

            try
            {
                var content = request != null ? new StringContent(requestData, Encoding.UTF8, "application/json") : null;
                var message = new HttpRequestMessage(Method, url)
                {
                    Content = content
                };

                var httpResponse = await Configuration.HttpClient.SendAsync(message).ConfigureAwait(false);
                responseData = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                var response = JsonConvert.DeserializeObject<TResponse>(responseData, JsonSettingsOverride ?? Configuration.JsonSettings);

                httpResponse.EnsureSuccessStatusCode();

                return response;
            }
            catch (Exception e)
            {
                var errorReason = FirebaseFailureParser.GetFailureReason(responseData);
                throw new FirebaseAuthHttpException(e, url, requestData, responseData, errorReason);
            }
        }

        private string GetFormattedUrl(string apiKey) => string.Format(UrlFormat, apiKey);
    }
}
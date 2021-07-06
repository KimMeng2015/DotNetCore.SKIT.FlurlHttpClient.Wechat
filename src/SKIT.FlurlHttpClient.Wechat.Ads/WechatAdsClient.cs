﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;

namespace SKIT.FlurlHttpClient.Wechat.Ads
{
    /// <summary>
    /// 一个微信广告平台 API HTTP 客户端。
    /// </summary>
    public class WechatAdsClient : WechatClientBase
    {
        /// <summary>
        /// 获取当前客户端使用的微信广告平台服务商 ID。
        /// </summary>
        public string WechatAgencyId { get; }

        /// <summary>
        /// 获取当前客户端使用的微信广告平台服务商 AppId。
        /// </summary>
        public string WechatAgencyAppId { get; }

        /// <summary>
        /// 获取当前客户端使用的微信广告平台服务商 ApiKey。
        /// </summary>
        internal string WechatAgencyApiKey { get; }

        /// <summary>
        /// 获取当前客户端使用的 JSON 序列化器。
        /// </summary>
        internal ISerializer FlurlJsonSerializer
        {
            get { return FlurlClient.Settings?.JsonSerializer ?? new FlurlNewtonsoftJsonSerializer(); }
        }

        /// <summary>
        /// 用指定的配置项初始化 <see cref="WechatAdsClient"/> 类的新实例。
        /// </summary>
        /// <param name="options">配置项。</param>
        public WechatAdsClient(WechatAdsClientOptions options)
            : base()
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            WechatAgencyId = options.AgencyId;
            WechatAgencyAppId = options.AgencyAppId;
            WechatAgencyApiKey = options.AgencyApiKey;

            FlurlClient.BaseUrl = options.Endpoints ?? WechatAdsEndpoints.DEFAULT;
            FlurlClient.WithTimeout(TimeSpan.FromMilliseconds(options.Timeout));

            Interceptors.Add(new Interceptors.WechatAdsAgencyTokenInterceptor(
                agencyId: options.AgencyId,
                agencyApiKey: options.AgencyApiKey
            ));
        }

        /// <summary>
        /// 用指定的微信广告平台服务商 ID、微信广告平台服务商 AppId、微信广告平台服务商 ApiKey 初始化 <see cref="WechatAdsClient"/> 类的新实例。
        /// </summary>
        /// <param name="agencyId">微信广告平台服务商 ID。</param>
        /// <param name="agencyAppId">微信广告平台服务商 AppId。</param>
        /// <param name="agencyApiKey">微信广告平台服务商 ApiKey。</param>
        public WechatAdsClient(string agencyId, string agencyAppId, string agencyApiKey)
            : this(new WechatAdsClientOptions() { AgencyId = agencyId, AgencyAppId = agencyAppId, AgencyApiKey = agencyApiKey })
        {
        }

        /// <summary>
        /// 异步发起请求。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flurlRequest"></param>
        /// <param name="content"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T> SendRequestAsync<T>(IFlurlRequest flurlRequest, HttpContent? content = null, CancellationToken cancellationToken = default)
            where T : WechatAdsResponse, new()
        {
            try
            {
                using IFlurlResponse flurlResponse = await base.SendRequestAsync(flurlRequest, content, cancellationToken).ConfigureAwait(false);
                return await GetResposneAsync<T>(flurlResponse).ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                throw new WechatAdsException(ex.Message, ex);
            }
        }

        /// <summary>
        /// 异步发起请求。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flurlRequest"></param>
        /// <param name="data"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T> SendRequestWithJsonAsync<T>(IFlurlRequest flurlRequest, object? data = null, CancellationToken cancellationToken = default)
            where T : WechatAdsResponse, new()
        {
            try
            {
                using IFlurlResponse flurlResponse = await base.SendRequestWithJsonAsync(flurlRequest, data, cancellationToken).ConfigureAwait(false);
                return await GetResposneAsync<T>(flurlResponse).ConfigureAwait(false);
            }
            catch (FlurlHttpException ex)
            {
                throw new WechatAdsException(ex.Message, ex);
            }
        }

        private async Task<T> GetResposneAsync<T>(IFlurlResponse flurlResponse)
            where T : WechatAdsResponse, new()
        {
            string contentType = flurlResponse.Headers.GetAll("Content-Type").FirstOrDefault() ?? string.Empty;
            string contentDisposition = flurlResponse.Headers.GetAll("Content-Disposition").FirstOrDefault() ?? string.Empty;
            bool contentTypeIsNotJson =
                (flurlResponse.StatusCode != (int)HttpStatusCode.OK) ||
                (!contentType.StartsWith("application/json") && !contentType.StartsWith("text/json")) ||
                (contentDisposition.StartsWith("attachment"));

            T result = contentTypeIsNotJson ? new T() : await flurlResponse.GetJsonAsync<T>().ConfigureAwait(false);
            result.RawStatus = flurlResponse.StatusCode;
            result.RawHeaders = new ReadOnlyDictionary<string, string>(
                flurlResponse.Headers
                    .GroupBy(e => e.Name)
                    .ToDictionary(
                        k => k.Key,
                        v => string.Join(", ", v.Select(e => e.Value))
                    )
            );
            result.RawBytes = await flurlResponse.ResponseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            return result;
        }
    }
}
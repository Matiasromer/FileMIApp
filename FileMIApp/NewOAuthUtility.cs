using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nemiro.OAuth;

namespace FileMIApp
{
    class NewOAuthUtility
    {
        public static void DeleteAsync(string endpoint = null, HttpParameterCollection parameters = null, 
            HttpAuthorization authorization = null, NameValueCollection headers = null, string contentType = null, 
            AccessToken accessToken = null, ExecuteRequestAsyncCallback callback = null, bool allowWriteStreamBuffering = false, 
            bool allowSendChunked = true, long contentLength = -1, HttpWriteRequestStream streamWriteCallback = null, 
            int writeBufferSize = 4096, int readBufferSize = 4096, bool donotEncodeKeys = false)
        {
            NewOAuthUtility.ExecuteRequestAsync("DELETE", endpoint, parameters, authorization, headers, contentType, accessToken, 
                callback, allowWriteStreamBuffering, allowSendChunked, contentLength, streamWriteCallback, writeBufferSize, 
                readBufferSize, donotEncodeKeys);
        }

        public static void ExecuteRequestAsync(string method = "POST", string endpoint = null, HttpParameterCollection parameters = null, HttpAuthorization authorization = null, NameValueCollection headers = null, string contentType = null, AccessToken accessToken = null, ExecuteRequestAsyncCallback callback = null, bool allowWriteStreamBuffering = false, bool allowSendChunked = true, long contentLength = -1, HttpWriteRequestStream streamWriteCallback = null, int writeBufferSize = 4096, int readBufferSize = 4096, bool donotEncodeKeys = false)
        {
            new Thread((ThreadStart)(() =>
            {
                RequestResult result;
                try
                {
                    result = OAuthUtility.ExecuteRequest(method, endpoint, parameters, authorization, headers, contentType, accessToken, allowWriteStreamBuffering, allowSendChunked, contentLength, streamWriteCallback, writeBufferSize, readBufferSize, donotEncodeKeys);
                }
                catch (RequestException ex)
                {
                    result = ex.RequestResult;
                }
                if (callback == null)
                    return;
                callback(result);
            }))
            {
                IsBackground = true
            }.Start();
        }
    }
}

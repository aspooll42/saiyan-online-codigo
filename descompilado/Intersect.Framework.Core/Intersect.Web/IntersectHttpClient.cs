using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Intersect.Core;
using Intersect.Framework.Net;
using Intersect.Framework.SystemInformation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect.Web;

public class IntersectHttpClient : HttpClient
{
	public const string HeaderIntersectRuntimeIdentifier = "X-Intersect-RuntimeIdentifier";

	private readonly Uri _baseUri;

	private readonly TokenResponse? _tokenResponse;

	private static HttpClientHandler GetHttpClientHandler()
	{
		return new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = delegate(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors policyErrors)
			{
				string text = request.RequestUri?.Host;
				if (string.IsNullOrWhiteSpace(text))
				{
					return false;
				}
				if (IPAddress.TryParse(text, out IPAddress address))
				{
					if (!address.IsLoopback())
					{
						return address.IsPrivate();
					}
					return true;
				}
				if (OperatingSystem.IsMacOS())
				{
					if (!string.Equals("localhost", text, StringComparison.Ordinal))
					{
						return policyErrors == SslPolicyErrors.None;
					}
					return true;
				}
				return string.Equals("localhost", text, StringComparison.Ordinal) || (certificate?.Verify() ?? false);
			}
		};
	}

	public IntersectHttpClient(string serverUri, TokenResponse? tokenResponse = null)
		: base(GetHttpClientHandler())
	{
		_baseUri = new Uri(new Uri(serverUri), "/");
		_tokenResponse = tokenResponse;
		IApplicationContext currentContext = ApplicationContext.CurrentContext;
		string productName = currentContext.Name.Replace(" ", "_");
		string version = currentContext.Version;
		string runtimeIdentifier = PlatformInformation.RuntimeIdentifier;
		ProductInfoHeaderValue item = new ProductInfoHeaderValue("(" + runtimeIdentifier + ")");
		ProductInfoHeaderValue item2 = new ProductInfoHeaderValue(productName, version);
		base.DefaultRequestHeaders.UserAgent.Clear();
		base.DefaultRequestHeaders.UserAgent.Add(item2);
		base.DefaultRequestHeaders.UserAgent.Add(item);
		base.DefaultRequestHeaders.Add("X-Intersect-RuntimeIdentifier", runtimeIdentifier);
	}

	public TokenResultType TryRequestToken(string username, string password, out TokenResponse? tokenResponse, bool hashed = false)
	{
		tokenResponse = null;
		if (string.IsNullOrWhiteSpace(username))
		{
			return TokenResultType.InvalidUsername;
		}
		if (string.IsNullOrWhiteSpace(password))
		{
			return TokenResultType.InvalidPassword;
		}
		string value = (hashed ? password : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))));
		if (string.IsNullOrWhiteSpace(value))
		{
			return TokenResultType.ClientSideFailure;
		}
		try
		{
			using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, "/api/oauth/token"));
			Dictionary<string, string> value2 = new Dictionary<string, string>
			{
				{ "grant_type", "password" },
				{ "username", username },
				{ "password", value }
			};
			string content = JsonConvert.SerializeObject(value2);
			httpRequestMessage.Content = new StringContent(content, MediaTypeHeaderValue.Parse("application/json"));
			HttpResponseMessage httpResponseMessage = Send(httpRequestMessage);
			if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
			{
				ApplicationContext.Context.Value?.Logger.LogError("Authentication failed, received {StatusCode} from server", httpResponseMessage.StatusCode);
				return TokenResultType.InvalidCredentials;
			}
			try
			{
				if (httpResponseMessage.Content.Headers.ContentType?.MediaType != "application/json")
				{
					return TokenResultType.InvalidResponse;
				}
				long? contentLength = httpResponseMessage.Content.Headers.ContentLength;
				if (contentLength.HasValue && contentLength.Value < 3)
				{
					return TokenResultType.InvalidResponse;
				}
				using Stream stream = httpResponseMessage.Content.ReadAsStream();
				using StreamReader streamReader = new StreamReader(stream);
				string value3 = streamReader.ReadToEnd();
				if (string.IsNullOrWhiteSpace(value3))
				{
					return TokenResultType.InvalidResponse;
				}
				tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(value3);
				return TokenResultType.TokenReceived;
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to parse token response");
				return TokenResultType.InvalidResponse;
			}
		}
		catch (Exception exception2)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception2, "Failed to authenticate");
			return TokenResultType.Failed;
		}
	}

	public TokenResultType TryRequestToken(string refreshToken, out TokenResponse? tokenResponse)
	{
		tokenResponse = null;
		if (string.IsNullOrWhiteSpace(refreshToken))
		{
			return TokenResultType.InvalidRefreshToken;
		}
		try
		{
			using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseUri, "/api/oauth/token"));
			Dictionary<string, string> value = new Dictionary<string, string>
			{
				{ "grant_type", "refresh_token" },
				{ "refresh_token", refreshToken }
			};
			string content = JsonConvert.SerializeObject(value);
			httpRequestMessage.Content = new StringContent(content, MediaTypeHeaderValue.Parse("application/json"));
			HttpResponseMessage httpResponseMessage = base.Send(httpRequestMessage);
			if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
			{
				ApplicationContext.Context.Value?.Logger.LogError("Authentication failed, received {StatusCode} from server", httpResponseMessage.StatusCode);
				return TokenResultType.InvalidCredentials;
			}
			try
			{
				if (httpResponseMessage.Content.Headers.ContentType?.MediaType != "application/json")
				{
					return TokenResultType.InvalidResponse;
				}
				long? contentLength = httpResponseMessage.Content.Headers.ContentLength;
				if (contentLength.HasValue && contentLength.Value < 3)
				{
					return TokenResultType.InvalidResponse;
				}
				using Stream stream = httpResponseMessage.Content.ReadAsStream();
				using StreamReader streamReader = new StreamReader(stream);
				string value2 = streamReader.ReadToEnd();
				if (string.IsNullOrWhiteSpace(value2))
				{
					return TokenResultType.InvalidResponse;
				}
				tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(value2);
				return TokenResultType.TokenReceived;
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to parse token response");
				return TokenResultType.InvalidResponse;
			}
		}
		catch (Exception exception2)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception2, "Failed to authenticate");
			return TokenResultType.Failed;
		}
	}

	public new HttpResponseMessage Send(HttpRequestMessage request)
	{
		return Send(request, default(CancellationToken));
	}

	public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		TokenResponse tokenResponse = _tokenResponse;
		if (tokenResponse != null)
		{
			if (request.Headers.Authorization == null)
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
			}
		}
		else
		{
			tokenResponse = null;
		}
		HttpResponseMessage httpResponseMessage = base.Send(request, cancellationToken);
		if (httpResponseMessage.IsSuccessStatusCode || tokenResponse == null)
		{
			return httpResponseMessage;
		}
		if (TryRequestToken(tokenResponse.RefreshToken, out tokenResponse) != TokenResultType.TokenReceived)
		{
			return httpResponseMessage;
		}
		using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(request.Method, request.RequestUri)
		{
			Version = request.Version
		};
		if (request.Content != null)
		{
			MemoryStream memoryStream = new MemoryStream();
			request.Content.CopyTo(memoryStream, null, cancellationToken);
			memoryStream.Position = 0L;
			httpRequestMessage.Content = new StreamContent(memoryStream);
			foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
			{
				httpRequestMessage.Content.Headers.Add(header.Key, header.Value);
			}
		}
		foreach (KeyValuePair<string, object> item in (IEnumerable<KeyValuePair<string, object>>)request.Options)
		{
			httpRequestMessage.Options.Set(new HttpRequestOptionsKey<object>(item.Key), item.Value);
		}
		foreach (KeyValuePair<string, IEnumerable<string>> header2 in request.Headers)
		{
			httpRequestMessage.Headers.TryAddWithoutValidation(header2.Key, header2.Value);
		}
		return base.Send(httpRequestMessage, cancellationToken);
	}

	public new HttpResponseMessage Send(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
	{
		TokenResponse tokenResponse = _tokenResponse;
		if (tokenResponse != null)
		{
			if (request.Headers.Authorization == null)
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
			}
		}
		else
		{
			tokenResponse = null;
		}
		HttpResponseMessage httpResponseMessage = base.Send(request, completionOption, cancellationToken);
		if (httpResponseMessage.IsSuccessStatusCode || tokenResponse == null)
		{
			return httpResponseMessage;
		}
		if (TryRequestToken(tokenResponse.RefreshToken, out tokenResponse) != TokenResultType.TokenReceived)
		{
			return httpResponseMessage;
		}
		using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(request.Method, request.RequestUri)
		{
			Version = request.Version
		};
		if (request.Content != null)
		{
			MemoryStream memoryStream = new MemoryStream();
			request.Content.CopyTo(memoryStream, null, cancellationToken);
			memoryStream.Position = 0L;
			httpRequestMessage.Content = new StreamContent(memoryStream);
			foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
			{
				httpRequestMessage.Content.Headers.Add(header.Key, header.Value);
			}
		}
		foreach (KeyValuePair<string, object> item in (IEnumerable<KeyValuePair<string, object>>)request.Options)
		{
			httpRequestMessage.Options.Set(new HttpRequestOptionsKey<object>(item.Key), item.Value);
		}
		foreach (KeyValuePair<string, IEnumerable<string>> header2 in request.Headers)
		{
			httpRequestMessage.Headers.TryAddWithoutValidation(header2.Key, header2.Value);
		}
		return base.Send(httpRequestMessage, completionOption, cancellationToken);
	}
}

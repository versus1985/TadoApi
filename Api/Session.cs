﻿using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace KoenZomers.Tado.Api
{
    public class Session : IDisposable
    {
        #region Properties

        /// <summary>
        /// Username to use to connect to the Tado API. Set by providing it in the constructor.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Password to use to connect to the Tado API. Set by providing it in the constructor.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Base Uri with which all Tado API requests start
        /// </summary>
        public Uri TadoApiBaseUrl => new Uri("https://my.tado.com/api/v2/");

        /// <summary>
        /// Tado API Uri to authenticate against
        /// </summary>
        public Uri TadoApiAuthUrl => new Uri("https://auth.tado.com/oauth/token");

        /// <summary>
        /// Tado API Client Id to use for the OAuth token
        /// </summary>
        public string ClientId => "public-api-preview";

        /// <summary>
        /// Tado API Client Secret to use for the OAuth token
        /// </summary>
        public string ClientSecret => "4HJGRffVR8xb3XdEUQpjgZ1VplJi6Xgw";

        /// <summary>
        /// Allows setting an User Agent which will be provided to the Tado API
        /// </summary>
        public string UserAgent => "";

        private IWebProxy proxyConfiguration;
        /// <summary>
        /// If provided, this proxy will be used for communication with the Tado API. If not provided, no proxy will be used.
        /// </summary>
        public IWebProxy ProxyConfiguration
        {
            get { return proxyConfiguration; }
            set { proxyConfiguration = value; _httpClient?.Dispose(); _httpClient = CreateHttpClient(); }
        }

        private NetworkCredential proxyCredential;
        /// <summary>
        /// If provided along with a proxy configuration, these credentials will be used to authenticate to the proxy. If omitted, the default system credentials will be used.
        /// </summary>
        public NetworkCredential ProxyCredential
        {
            get { return proxyCredential; }
            set { proxyCredential = value; _httpClient?.Dispose(); _httpClient = CreateHttpClient(); }
        }

        /// <summary>
        /// Boolean indicating if the current session is authenticated
        /// </summary>
        public bool IsAuthenticated => AuthenticatedSession != null;

        /// <summary>
        /// Authenticated Session that will be used to communicate with the Tado API
        /// </summary>
        public Entities.Session AuthenticatedSession { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// HttpClient to use for network communications towards the Tado API
        /// </summary>
        private HttpClient _httpClient;

        #endregion

        #region Constructors \ Destructors

        /// <summary>
        /// Initiates a new session to the Tado API
        /// </summary>
        public Session(string username, string password)
        {
            Username = username;
            Password = password;

            // Add a HttpClient to the session to allow for network communication
            _httpClient = CreateHttpClient();
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #endregion

        #region Authentication Methods

        /// <summary>
        /// Validates if the session has authenticated already and if so, ensures the AccessToken coming forward from the authentication is still valid and assigns it to the HttpClient in this session
        /// </summary>
        private async Task EnsureAccessToken()
        {
            // Check if we have an authenticated session
            if (AuthenticatedSession == null || !AuthenticatedSession.Expires.HasValue)
            {
                // Session is not yet authenticated, nothing we can do at this point
                return;
            }

            // We have an authenticated session, check if its still valid
            if (AuthenticatedSession.Expires.Value < DateTime.Now)
            {
                // Access token is no longer valid, request a new one
                if (!string.IsNullOrEmpty(AuthenticatedSession.RefreshToken))
                {
                    // We have a refresh token, use that to get a new access token
                    AuthenticatedSession = await GetRefreshedSession();
                }
                else
                {
                    // We don't have a refresh token, just get a new access token
                    AuthenticatedSession = await GetNewSession();
                }
            }

            // Set the access token on the HttpClient
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", AuthenticatedSession.AccessToken);
        }

        /// <summary>
        /// Ensures the current session is authenticated or throws an exception if it's not
        /// </summary>
        private void EnsureAuthenticatedSession()
        {
            if (!IsAuthenticated)
            {
                throw new Exceptions.SessionNotAuthenticatedException();
            }
        }

        /// <summary>
        /// Sets up a new session with the Tado API
        /// </summary>
        /// <returns>Session instance</returns>
        private async Task<Entities.Session> GetNewSession()
        {
            // Build the POST body with the authentication arguments
            var queryBuilder = new Helpers.QueryStringBuilder();
            queryBuilder.Add("client_id", ClientId);
            queryBuilder.Add("grant_type", "password");
            queryBuilder.Add("client_secret", ClientSecret);
            queryBuilder.Add("password", Password);
            queryBuilder.Add("scope", "home.user");
            queryBuilder.Add("username", Username);

            return await PostMessageGetResponse<Entities.Session>(TadoApiAuthUrl, queryBuilder, false);
        }

        /// <summary>
        /// Sets up a session with the Tado API based on the refresh token
        /// </summary>
        /// <returns>Session instance</returns>
        private async Task<Entities.Session> GetRefreshedSession()
        {
            // Build the POST body with the authentication arguments
            var queryBuilder = new Helpers.QueryStringBuilder();
            queryBuilder.Add("client_id", ClientId);
            queryBuilder.Add("grant_type", "refresh_token");
            queryBuilder.Add("client_secret", ClientSecret);
            queryBuilder.Add("refresh_token", AuthenticatedSession.RefreshToken);
            queryBuilder.Add("scope", "home.user");

            return await PostMessageGetResponse<Entities.Session>(TadoApiAuthUrl, queryBuilder, false);
        }

        /// <summary>
        /// Authenticates this session with the Tado API
        /// </summary>
        public async Task Authenticate()
        {
            try
            {
                // Request the OAuth token
                AuthenticatedSession = await GetNewSession();
            }
            catch (Exception ex)
            {
                throw new Exceptions.SessionAuthenticationFailedException(ex);
            }
        }

        #endregion

        #region Network Traffic Methods

        /// <summary>
        /// Instantiates a new HttpClient preconfigured for use. Note that the caller is responsible for disposing this object.
        /// </summary>
        /// <returns>HttpClient instance</returns>
        private HttpClient CreateHttpClient()
        {
            // Define the HttpClient settings
            var httpClientHandler = new HttpClientHandler
            {
                UseDefaultCredentials = ProxyCredential == null,
                Proxy = ProxyConfiguration,
            };
            if(ProxyConfiguration != null)
            {
                httpClientHandler.UseProxy = true;
            }

            // Check if we need specific credentials for the proxy
            if (ProxyCredential != null && httpClientHandler.Proxy != null)
            {
                httpClientHandler.Proxy.Credentials = ProxyCredential;
            }

            // Create the new HTTP Client
            var httpClient = new HttpClient(httpClientHandler);
            
            if (!string.IsNullOrEmpty(UserAgent))
            {
                // Identify to the server with a specific User Agent
                httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            }
            
            return httpClient;
        }

        /// <summary>
        /// Sends a HTTP POST to the provided uri
        /// </summary>
        /// <param name="queryBuilder">The querystring parameters to send in the POST body</param>
        /// <typeparam name="T">Type of object to try to parse the response JSON into</typeparam>
        /// <param name="uri">Uri of the webservice to send the message to</param>
        /// <param name="requiresAuthenticatedSession">True to indicate that this request must have a valid oAuth token</param>
        /// <returns>Object of type T with the parsed response</returns>
        private async Task<T> PostMessageGetResponse<T>(Uri uri, Helpers.QueryStringBuilder queryBuilder, bool requiresAuthenticatedSession)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("Uri has not been provided");
            }

            if (requiresAuthenticatedSession)
            {
                // Ensure a valid OAuth token is set on the HttpClient if possible
                await EnsureAccessToken();
            }

            // Prepare the content to POST
            using (var content = new StringContent(queryBuilder.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded"))
            {
                // Construct the message towards the webservice
                using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
                {
                    // Set the content to send along in the message body with the request
                    request.Content = content;

                    try
                    {
                        // Request the response from the webservice
                        var response = await _httpClient.SendAsync(request);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        // Verify if the request was successful (response status 200-299)
                        if (response.IsSuccessStatusCode)
                        {
                            // Request was successful
                            var responseEntity = JsonConvert.DeserializeObject<T>(responseBody);
                            return responseEntity;
                        }
                    }
                    catch(Exception ex)
                    {
                        throw new Exceptions.RequestFailedException(uri, ex);
                    }

                    // Request was not successful. throw an exception
                    throw new Exceptions.RequestFailedException(uri);
                }
            }
        }

        /// <summary>
        /// Sends a message to the Tado API and returns the provided object of type T with the response
        /// </summary>
        /// <typeparam name="T">Object type of the expected response</typeparam>
        /// <param name="uri">Uri of the webservice to send the message to</param>
        /// <param name="expectedHttpStatusCode">The expected Http result status code. Optional. If provided and the webservice returns a different response, the return type will be NULL to indicate failure.</param>
        /// <returns>Typed entity with the result from the webservice</returns>
        protected virtual async Task<T> GetMessageReturnResponse<T>(Uri uri, HttpStatusCode? expectedHttpStatusCode = null)
        {
            // Ensure a valid OAuth token is set on the HttpClient if possible
            await EnsureAccessToken();

            // Construct the request towards the webservice
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                try
                {
                    // Request the response from the webservice
                    using (var response = await _httpClient.SendAsync(request))
                    {
                        if (!expectedHttpStatusCode.HasValue || (expectedHttpStatusCode.HasValue && response != null && response.StatusCode == expectedHttpStatusCode.Value))
                        {
                            var responseString = await response.Content.ReadAsStringAsync();
                            var responseEntity = JsonConvert.DeserializeObject<T>(responseString);
                            return responseEntity;
                        }
                        return default(T);
                    }
                }
                catch (Exception ex)
                {
                    // Request was not successful. throw an exception
                    throw new Exceptions.RequestFailedException(uri, ex);
                }
            }
        }

        /// <summary>
        /// Sends a message to the Tado API and returns the provided object of type T with the response
        /// </summary>
        /// <typeparam name="T">Object type of the expected response</typeparam>
        /// <param name="uri">Uri of the webservice to send the message to</param>
        /// <param name="bodyText">Text to send to the webservice in the body</param>
        /// <param name="httpMethod">Http Method to use to connect to the webservice</param>
        /// <param name="expectedHttpStatusCode">The expected Http result status code. Optional. If provided and the webservice returns a different response, the return type will be NULL to indicate failure.</param>
        /// <returns>Typed entity with the result from the webservice</returns>
        protected virtual async Task<T> SendMessageReturnResponse<T>(string bodyText, HttpMethod httpMethod, Uri uri, HttpStatusCode? expectedHttpStatusCode = null)
        {
            // Ensure a valid OAuth token is set on the HttpClient if possible
            await EnsureAccessToken();

            // Load the content to send in the body
            using (var content = new StringContent(bodyText ?? "", Encoding.UTF8, "application/json"))
            {
                // Construct the message towards the webservice
                using (var request = new HttpRequestMessage(httpMethod, uri))
                {
                    // Check if a body to send along with the request has been provided
                    if (!string.IsNullOrEmpty(bodyText) && httpMethod != HttpMethod.Get)
                    {
                        // Set the content to send along in the message body with the request
                        request.Content = content;
                    }

                    try
                    {
                        // Request the response from the webservice
                        using (var response = await _httpClient.SendAsync(request))
                        {
                            if (!expectedHttpStatusCode.HasValue || (expectedHttpStatusCode.HasValue && response != null && response.StatusCode == expectedHttpStatusCode.Value))
                            {
                                var responseString = await response.Content.ReadAsStringAsync();
                                var responseEntity = JsonConvert.DeserializeObject<T>(responseString);
                                return responseEntity;
                            }
                            return default(T);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Request was not successful. throw an exception
                        throw new Exceptions.RequestFailedException(uri, ex);
                    }
                }
            }
        }

        /// <summary>
        /// Sends a message to the Tado API without looking at the response
        /// </summary>
        /// <param name="uri">Uri of the webservice to send the message to</param>
        /// <param name="bodyText">Text to send to the webservice in the body</param>
        /// <param name="httpMethod">Http Method to use to connect to the webservice</param>
        /// <param name="expectedHttpStatusCode">The expected Http result status code. Optional. If provided and the webservice returns a different response, the return type will be false to indicate failure.</param>
        /// <returns>Boolean indicating if the request was successful</returns>
        protected virtual async Task<bool> SendMessage(string bodyText, HttpMethod httpMethod, Uri uri, HttpStatusCode? expectedHttpStatusCode = null)
        {
            // Ensure a valid OAuth token is set on the HttpClient if possible
            await EnsureAccessToken();

            // Load the content to send in the body
            using (var content = new StringContent(bodyText ?? "", Encoding.UTF8, "application/json"))
            {
                // Construct the message towards the webservice
                using (var request = new HttpRequestMessage(httpMethod, uri))
                {
                    // Check if a body to send along with the request has been provided
                    if (!string.IsNullOrEmpty(bodyText) && httpMethod != HttpMethod.Get)
                    {
                        // Set the content to send along in the message body with the request
                        request.Content = content;
                    }

                    try
                    {
                        // Request the response from the webservice
                        using (var response = await _httpClient.SendAsync(request))
                        {
                            if (!expectedHttpStatusCode.HasValue || (expectedHttpStatusCode.HasValue && response != null && response.StatusCode == expectedHttpStatusCode.Value))
                            {
                                return true;
                            }
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Request was not successful. throw an exception
                        throw new Exceptions.RequestFailedException(uri, ex);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns information about the user currently connected through the Tado API
        /// </summary>
        /// <returns>Information about the current user</returns>
        public async Task<Entities.User> GetMe()
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.User>(new Uri(TadoApiBaseUrl, "me"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the zones configured in the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <returns>The configured zones</returns>
        public async Task<Entities.Zone[]> GetZones(long homeId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.Zone[]>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/zones"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the devices configured in the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <returns>The configured devices</returns>
        public async Task<Entities.Device[]> GetDevices(long homeId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.Device[]>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/devices"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the mobile devices connected to the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <returns>The connected mobile devices</returns>
        public async Task<Entities.MobileDevice.Item[]> GetMobileDevices(long homeId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.MobileDevice.Item[]>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/mobileDevices"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the settings of a mobile device connected to the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <param name="mobileDeviceId">Id of the mobile device to query</param>
        /// <returns>The settings of the connected mobile device</returns>
        public async Task<Entities.MobileDevice.Settings> GetMobileDeviceSettings(long homeId, int mobileDeviceId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.MobileDevice.Settings>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/mobileDevices/{mobileDeviceId}/settings"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the installations in the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <returns>The installations</returns>
        public async Task<Entities.Installation[]> GetInstallations(long homeId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.Installation[]>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/installations"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the state of the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <returns>The state of the home</returns>
        public async Task<Entities.HomeState> GetHomeState(long homeId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.HomeState>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/state"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the state of a zone in the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <param name="zoneId">Id of the zone to query</param>
        /// <returns>The state of the zone</returns>
        public async Task<Entities.State> GetZoneState(long homeId, long zoneId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.State>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/zones/{zoneId}/state"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the summarized state of a zone in the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <param name="zoneId">Id of the zone to query</param>
        /// <returns>The summarized state of the zone</returns>
        public async Task<Entities.ZoneSummary> GetSummarizedZoneState(long homeId, long zoneId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.ZoneSummary>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/zones/{zoneId}/overlay"), HttpStatusCode.OK);
            return response;
        }
        public async Task<Entities.DayReport> GetZoneDayReport(long homeId, long zoneId, DateTime date)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.DayReport>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/zones/{zoneId}/dayReport?date={date.ToString("yyyy-MM-dd")}"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the current weather at the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <returns>The current weater at the home</returns>
        public async Task<Entities.Weather> GetWeather(long homeId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.Weather>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/weather"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <returns>The home details</returns>
        public async Task<Entities.House> GetHome(long homeId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.House>(new Uri(TadoApiBaseUrl, $"homes/{homeId}"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the users with access to the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <returns>The users with access</returns>
        public async Task<Entities.User[]> GetUsers(long homeId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.User[]>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/users"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the capabilities of a zone in the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <param name="zoneId">Id of the zone to query</param>
        /// <returns>The capabilities of the zone</returns>
        public async Task<Entities.Capability> GetZoneCapabilities(long homeId, long zoneId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.Capability>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/zones/{zoneId}/capabilities"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Returns the early start of a zone in the home with the provided Id from the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to query</param>
        /// <param name="zoneId">Id of the zone to query</param>
        /// <returns>The early start setting of the zone</returns>
        public async Task<Entities.EarlyStart> GetEarlyStart(long homeId, long zoneId)
        {
            EnsureAuthenticatedSession();

            var response = await GetMessageReturnResponse<Entities.EarlyStart>(new Uri(TadoApiBaseUrl, $"homes/{homeId}/zones/{zoneId}/earlyStart"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Sets the temperature in a zone in the home with the provided Id through the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to set the temperature of</param>
        /// <param name="zoneId">Id of the zone to set the temperature of</param>
        /// <param name="temperature">Temperature to set the zone to</param>
        /// <returns>The summarized new state of the zone</returns>
        [Obsolete("SetTemperatureCelcius is deprecated, please rewrite your code to use SetHeatingTemperatureCelcius instead")]
        public async Task<Entities.ZoneSummary> SetTemperatureCelsius(long homeId, long zoneId, double temperature)
        {
            return await SetHeatingTemperatureCelsius(homeId, zoneId, temperature);
        }

        /// <summary>
        /// Sets the temperature in a zone in the home with the provided Id through the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to set the temperature of</param>
        /// <param name="zoneId">Id of the zone to set the temperature of</param>
        /// <param name="temperature">Temperature to set the zone to</param>
        /// <returns>The summarized new state of the zone</returns>
        public async Task<Entities.ZoneSummary> SetHeatingTemperatureCelsius(long homeId, long zoneId, double temperature)
        {
            return await SetHeatingTemperatureCelcius(homeId, zoneId, temperature, Enums.DurationModes.UntilNextManualChange);
        }

        /// <summary>
        /// Sets the temperature in a zone in the home with the provided Id through the Tado API for the duration as specified
        /// </summary>
        /// <param name="homeId">Id of the home to set the temperature of</param>
        /// <param name="zoneId">Id of the zone to set the temperature of</param>
        /// <param name="temperature">Temperature to set the zone to</param>
        /// <param name="durationMode">Defines the duration for which the heating will be switched to the provided temperature</param>
        /// <param name="timer">Only applicapble if for durationMode Timer has been chosen. In that case it allows providing for how long the duration should be.</param>
        /// <returns>The summarized new state of the zone</returns>
        [Obsolete("SetTemperatureCelcius is deprecated, please rewrite your code to use SetHeatingTemperatureCelcius instead")]
        public async Task<Entities.ZoneSummary> SetTemperatureCelcius(long homeId, long zoneId, double temperature, Enums.DurationModes durationMode, TimeSpan? timer = null)
        {
            return await SetHeatingTemperatureCelcius(homeId, zoneId, temperature, durationMode, timer);
        }

        /// <summary>
        /// Sets the temperature in a zone in the home with the provided Id through the Tado API for the duration as specified
        /// </summary>
        /// <param name="homeId">Id of the home to set the temperature of</param>
        /// <param name="zoneId">Id of the zone to set the temperature of</param>
        /// <param name="temperature">Temperature to set the zone to</param>
        /// <param name="durationMode">Defines the duration for which the heating will be switched to the provided temperature</param>
        /// <param name="timer">Only applicapble if for durationMode Timer has been chosen. In that case it allows providing for how long the duration should be.</param>
        /// <returns>The summarized new state of the zone</returns>
        public async Task<Entities.ZoneSummary> SetHeatingTemperatureCelcius(long homeId, long zoneId, double temperature, Enums.DurationModes durationMode, TimeSpan? timer = null)
        {
            return await SetTemperature(homeId, zoneId, temperature, null, Enums.DeviceTypes.Heating, durationMode, timer);
        }

        /// <summary>
        /// Sets the hot water temperature in the home with the provided Id through the Tado API for the duration as specified
        /// </summary>
        /// <param name="homeId">Id of the home to set the temperature of</param>
        /// <param name="temperatureCelcius">Temperature in Celcius to set the zone to</param>
        /// <param name="durationMode">Defines the duration for which the heating will be switched to the provided temperature</param>
        /// <param name="timer">Only applicapble if for durationMode Timer has been chosen. In that case it allows providing for how long the duration should be.</param>
        /// <returns>The summarized new state of the zone</returns>
        public async Task<Entities.ZoneSummary> SetHotWaterTemperatureCelcius(long homeId, double temperatureCelcius, Enums.DurationModes durationMode, TimeSpan? timer = null)
        {
            // Tado Hot Water is zone 0
            return await SetTemperature(homeId, 0, temperatureCelcius, null, Enums.DeviceTypes.HotWater, durationMode, timer);
        }

        /// <summary>
        /// Sets the hot water temperature in the home with the provided Id through the Tado API for the duration as specified
        /// </summary>
        /// <param name="homeId">Id of the home to set the temperature of</param>
        /// <param name="temperatureFahrenheit">Temperature in Fahrenheit to set the zone to</param>
        /// <param name="durationMode">Defines the duration for which the heating will be switched to the provided temperature</param>
        /// <param name="timer">Only applicapble if for durationMode Timer has been chosen. In that case it allows providing for how long the duration should be.</param>
        /// <returns>The summarized new state of the zone</returns>
        public async Task<Entities.ZoneSummary> SetHotWaterTemperatureFahrenheit(long homeId, double temperatureFahrenheit, Enums.DurationModes durationMode, TimeSpan? timer = null)
        {
            // Tado Hot Water is zone 0
            return await SetTemperature(homeId, 0, null, temperatureFahrenheit, Enums.DeviceTypes.HotWater, durationMode, timer);
        }

        /// <summary>
        /// Sets the temperature in a zone in the home with the provided Id through the Tado API for the duration as specified
        /// </summary>
        /// <param name="homeId">Id of the home to set the temperature of</param>
        /// <param name="zoneId">Id of the zone to set the temperature of</param>
        /// <param name="temperatureCelcius">Temperature in Celcius to set the zone to. Provide NULL for both temperatureCelcius and temperatureFahrenheit to switch the device off.</param>
        /// <param name="temperatureFahrenheit">Temperature in Fahrenheit to set the zone to. Provide NULL for both temperatureCelcius and temperatureFahrenheit to switch the device off.</param>
        /// <param name="durationMode">Defines the duration for which the heating will be switched to the provided temperature</param>
        /// <param name="timer">Only applicapble if for durationMode Timer has been chosen. In that case it allows providing for how long the duration should be.</param>
        /// <param name="deviceType">Type of Tado device to switch on</param>
        /// <returns>The summarized new state of the zone</returns>
        public async Task<Entities.ZoneSummary> SetTemperature(long homeId, long zoneId, double? temperatureCelcius, double? temperatureFahrenheit, Enums.DeviceTypes deviceType, Enums.DurationModes durationMode, TimeSpan? timer = null)
        {
            // If using Timer mode but not providing a timer duration, switch it to manual
            if (durationMode == Enums.DurationModes.Timer && timer == null)
            {
                durationMode = Enums.DurationModes.UntilNextManualChange;
            }

            EnsureAuthenticatedSession();

            // Define the proper command for the provided duration mode
            var overlay = new Entities.Overlay
            {
                Setting = new Entities.Setting
                {
                    DeviceType = deviceType
                },
                Termination = new Entities.Termination
                {
                    CurrentType = durationMode
                }
            };

            // If no temperature in Celcius and Fahrenheit has been provided, instruct to switch the device off, otherwise instruct to switch it on
            overlay.Setting.Power = !temperatureCelcius.HasValue && !temperatureFahrenheit.HasValue ? Enums.PowerStates.Off : Enums.PowerStates.On;

            // If the device is about to be switched on, provide the temperature it should be switched to
            if(overlay.Setting.Power == Enums.PowerStates.On)
            {
                overlay.Setting.Temperature = new Entities.Temperature
                {
                    Celsius = temperatureCelcius,
                    Fahrenheit = temperatureFahrenheit
                };
            }

            if (durationMode == Enums.DurationModes.Timer)
            {
                overlay.Termination.DurationInSeconds = (int)timer.Value.TotalSeconds;
            }

            var request = JsonConvert.SerializeObject(overlay);

            var response = await SendMessageReturnResponse<Entities.ZoneSummary>(request, HttpMethod.Put, new Uri(TadoApiBaseUrl, $"homes/{homeId}/zones/{zoneId}/overlay"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Sets the temperature in a zone in the home with the provided Id through the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to set the temperature of</param>
        /// <param name="zoneId">Id of the zone to set the temperature of</param>
        /// <param name="temperature">Temperature to set the zone to</param>
        /// <returns>The summarized new state of the zone</returns>
        [Obsolete("SetTemperatureFahrenheit is deprecated, please rewrite your code to use SetHeatingTemperatureFahrenheit instead")]
        public async Task<Entities.ZoneSummary> SetTemperatureFahrenheit(long homeId, long zoneId, double temperature)
        {
            return await SetHeatingTemperatureFahrenheit(homeId, zoneId, temperature);
        }

        /// <summary>
        /// Sets the temperature in a zone in the home with the provided Id through the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to set the temperature of</param>
        /// <param name="zoneId">Id of the zone to set the temperature of</param>
        /// <param name="temperature">Temperature to set the zone to</param>
        /// <returns>The summarized new state of the zone</returns>
        public async Task<Entities.ZoneSummary> SetHeatingTemperatureFahrenheit(long homeId, long zoneId, double temperature)
        {
            return await SetHeatingTemperatureFahrenheit(homeId, zoneId, temperature, Enums.DurationModes.UntilNextManualChange);
        }

        /// <summary>
        /// Sets the temperature in a zone in the home with the provided Id through the Tado API for the duration as specified
        /// </summary>
        /// <param name="homeId">Id of the home to set the temperature of</param>
        /// <param name="zoneId">Id of the zone to set the temperature of</param>
        /// <param name="temperature">Temperature to set the zone to</param>
        /// <param name="durationMode">Defines the duration for which the heating will be switched to the provided temperature</param>
        /// <param name="timer">Only applicapble if for durationMode Timer has been chosen. In that case it allows providing for how long the duration should be.</param>
        /// <returns>The summarized new state of the zone</returns>
        [Obsolete("SetTemperatureFahrenheit is deprecated, please rewrite your code to use SetHeatingTemperatureFahrenheit instead")]
        public async Task<Entities.ZoneSummary> SetTemperatureFahrenheit(long homeId, long zoneId, double temperature, Enums.DurationModes durationMode, TimeSpan? timer = null)
        {
            return await SetHeatingTemperatureFahrenheit(homeId, zoneId, temperature, durationMode, timer);
        }

        /// <summary>
        /// Sets the temperature in a zone in the home with the provided Id through the Tado API for the duration as specified
        /// </summary>
        /// <param name="homeId">Id of the home to set the temperature of</param>
        /// <param name="zoneId">Id of the zone to set the temperature of</param>
        /// <param name="temperature">Temperature to set the zone to</param>
        /// <param name="durationMode">Defines the duration for which the heating will be switched to the provided temperature</param>
        /// <param name="timer">Only applicapble if for durationMode Timer has been chosen. In that case it allows providing for how long the duration should be.</param>
        /// <returns>The summarized new state of the zone</returns>
        public async Task<Entities.ZoneSummary> SetHeatingTemperatureFahrenheit(long homeId, long zoneId, double temperature, Enums.DurationModes durationMode, TimeSpan? timer = null)
        {
            return await SetTemperature(homeId, zoneId, null, temperature, Enums.DeviceTypes.Heating, durationMode, timer);
        }

        /// <summary>
        /// Switches the heating off in a zone in the home with the provided Id through the Tado API. Use SetTemperatureCelsius or SetTemperatureFahrenheit to switch the heating on again.
        /// </summary>
        /// <param name="homeId">Id of the home to switch the heating off in</param>
        /// <param name="zoneId">Id of the zone to switch the heating off in</param>
        /// <returns>The summarized new state of the zone</returns>
        public async Task<Entities.ZoneSummary> SwitchHeatingOff(long homeId, long zoneId)
        {
            return await SwitchHeatingOff(homeId, zoneId, Enums.DurationModes.UntilNextManualChange);
        }

        /// <summary>
        /// Switches the heating off in a zone in the home with the provided Id through the Tado API for the duration as specified
        /// </summary>
        /// <param name="homeId">Id of the home to switch the heating off in</param>
        /// <param name="zoneId">Id of the zone to switch the heating off in</param>
        /// <param name="durationMode">Defines the duration for which the temperature will remain switched off</param>
        /// <param name="timer">Only applicapble if for durationMode Timer has been chosen. In that case it allows providing for how long the duration should be.</param>
        /// <returns>The summarized new state of the zone</returns>
        public async Task<Entities.ZoneSummary> SwitchHeatingOff(long homeId, long zoneId, Enums.DurationModes durationMode, TimeSpan? timer = null)
        {
            return await SetTemperature(homeId, zoneId, null, null, Enums.DeviceTypes.Heating, durationMode, timer);
        }

        /// <summary>
        /// Switches the hot water off in the home with the provided Id through the Tado API for the duration as specified
        /// </summary>
        /// <param name="homeId">Id of the home to switch the heating off in</param>
        /// <param name="durationMode">Defines the duration for which the temperature will remain switched off</param>
        /// <param name="timer">Only applicapble if for durationMode Timer has been chosen. In that case it allows providing for how long the duration should be.</param>
        /// <returns>The summarized new state of the zone</returns>
        public async Task<Entities.ZoneSummary> SwitchHotWaterOff(long homeId, Enums.DurationModes durationMode, TimeSpan? timer = null)
        {
            return await SetTemperature(homeId, 0, null, null, Enums.DeviceTypes.HotWater, durationMode, timer);
        }

        /// <summary>
        /// Sets the EarlyStart mode of a zone in the home with the provided Id through the Tado API
        /// </summary>
        /// <param name="homeId">Id of the home to switch the heating off in</param>
        /// <param name="zoneId">Id of the zone to switch the heating off in</param>
        /// <param name="enabled">True to enable EarlyStart or False to disable it</param>
        /// <returns>The new EarlyStart mode of the zone</returns>
        public async Task<Entities.EarlyStart> SetEarlyStart(long homeId, long zoneId, bool enabled)
        {
            EnsureAuthenticatedSession();

            var earlyStart = new Entities.EarlyStart
            {
                Enabled = enabled
            };
            var request = JsonConvert.SerializeObject(earlyStart);

            var response = await SendMessageReturnResponse<Entities.EarlyStart>(request, HttpMethod.Put, new Uri(TadoApiBaseUrl, $"homes/{homeId}/zones/{zoneId}/earlyStart"), HttpStatusCode.OK);
            return response;
        }

        /// <summary>
        /// Shows Hi on the Tado device to identify it
        /// </summary>
        /// <param name="deviceId">Id / serial number of the Tado device</param>
        /// <returns>Boolean indicating if the request was successful</returns>
        public async Task<bool> SayHi(string deviceId)
        {
            EnsureAuthenticatedSession();

            var success = await SendMessage(null, HttpMethod.Post, new Uri(TadoApiBaseUrl, $"devices/{deviceId}/identify"), HttpStatusCode.OK);
            return success;
        }

        #endregion
    }
}
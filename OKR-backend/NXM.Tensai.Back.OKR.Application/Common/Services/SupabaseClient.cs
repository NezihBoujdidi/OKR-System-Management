using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;

namespace NXM.Tensai.Back.OKR.Application;

public class SupabaseClient : ISupabaseClient
{
    private readonly Client _supabaseClient;
    private readonly ILogger<SupabaseClient> _logger;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;
    private readonly string _serviceRoleKey;
    private readonly HttpClient _httpClient;

    public SupabaseClient(IConfiguration configuration, ILogger<SupabaseClient> logger)
    {
        _logger = logger;
        
        // Get Supabase configuration
        _supabaseUrl = configuration["SupabaseJwtSettings:supabaseUrl"] 
            ?? throw new ArgumentNullException("SupabaseJwtSettings:supabaseUrl is missing in configuration");
        
        _supabaseKey = configuration["SupabaseJwtSettings:supabaseKey"] 
            ?? throw new ArgumentNullException("SupabaseJwtSettings:supabaseKey is missing in configuration");
            
        _serviceRoleKey = configuration["SupabaseJwtSettings:ServiceRoleKey"] 
            ?? throw new ArgumentNullException("SupabaseJwtSettings:ServiceRoleKey is missing in configuration");

        try
        {
            // Initialize Supabase client
            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };
            
            _supabaseClient = new Client(_supabaseUrl, _supabaseKey, options);
            
            // Initialize HttpClient for direct Admin API calls
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // For Admin API endpoints like /invite, we need to use the service role key
            // Note that these are privileged operations that should be secured appropriately
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
            
            _logger.LogInformation("Supabase client initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Supabase client");
            throw;
        }
    }

    /// <summary>
    /// Invites a user by email using Supabase Auth Admin API
    /// </summary>
    public async Task<SupabaseInviteResult> InviteUserByEmailAsync(string email, string role, Guid organizationId, Guid? teamId = null)
    {
        try
        {
            _logger.LogInformation("Inviting user {Email} to organization {OrganizationId} with role {Role}", 
                email, organizationId, role);
            
            // Create metadata to include with the invitation
            var userMetadata = new Dictionary<string, object>
            {
                { "role", role },
                { "organization_id", organizationId.ToString() }
            };

            // Add team ID if provided
            if (teamId.HasValue)
            {
                userMetadata.Add("team_id", teamId.Value.ToString());
            }   

            // Full absolute redirect URL
            var redirectUrl = "http://localhost:4200/signup"; 
            /* var inviteBody = new
            {
                email = email,
                options = new
                {
                    redirectTo = redirectUrl,
                    data = userMetadata
                }
            }; */
            // Create the invite request body
           var inviteBody = new Dictionary<string, object>
           {
               ["email"] = email,
               ["options"] = new Dictionary<string, object>
               {
                    ["redirectTo"] = redirectUrl,
                    ["data"] = userMetadata
               }
        };

            //var json = JsonSerializer.Serialize(inviteBody);


            // Serialize with proper casing
             var json = JsonSerializer.Serialize(inviteBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }); 

            // Create explicit request message
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{_supabaseUrl}/auth/v1/invite"),
                Headers =
                {
                    { "apikey", _serviceRoleKey },
                    { "Authorization", $"Bearer {_serviceRoleKey}" }
                },
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _logger.LogDebug("Sending invitation request to {Url}", request.RequestUri);
            _logger.LogInformation("Invite JSON body: {Json}", json);
            // Send the request
            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent invitation to {Email}", email);
                
                // Parse the response to get the invite ID
                string inviteId;
                try
                {
                    var responseData = JsonSerializer.Deserialize<JsonDocument>(responseBody);
                    if (responseData.RootElement.TryGetProperty("id", out var idElement) && !string.IsNullOrEmpty(idElement.GetString()))
                    {
                        inviteId = idElement.GetString();
                    }
                    else
                    {
                        _logger.LogError("No invite ID returned in successful response for {Email}: {Response}", email, responseBody);
                        throw new Exception("No invite ID returned from Supabase despite successful API call.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse invite ID from Supabase response for {Email}: {Response}", email, responseBody);
                    throw new Exception("Failed to parse invite response from Supabase", ex);
                }
                
                return SupabaseInviteResult.Successful(inviteId);
            }
            else
            {
                _logger.LogError("Failed to invite user {Email}. Status: {Status}, Error: {Error}",
                    email, response.StatusCode, responseBody);
                
                return SupabaseInviteResult.Failed($"Failed to send invitation: {response.StatusCode} - {responseBody}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while inviting user {Email}", email);
            return SupabaseInviteResult.Failed($"Exception: {ex.Message}");
        }
    }
}

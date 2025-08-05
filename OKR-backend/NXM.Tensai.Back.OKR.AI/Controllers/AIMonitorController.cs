using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NXM.Tensai.Back.OKR.AI.Services;

namespace NXM.Tensai.Back.OKR.AI.Controllers
{
    [ApiController]
    [Route("api/ai/monitor")]
    public class AIMonitorController : ControllerBase
    {
        private readonly AzureOpenAIChatService _azureOpenAIChatService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIMonitorController> _logger;

        public AIMonitorController(
            AzureOpenAIChatService azureOpenAIChatService,
            IConfiguration configuration,
            ILogger<AIMonitorController> logger)
        {
            _azureOpenAIChatService = azureOpenAIChatService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Run diagnostics on Azure OpenAI configuration and connections
        /// </summary>
        [HttpGet("diagnostics")]
        public async Task<IActionResult> RunDiagnostics()
        {
            try
            {
                _logger.LogInformation("Running Azure OpenAI diagnostics");
                
                var results = await _azureOpenAIChatService.RunDiagnosticsAsync();
                
                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running diagnostics");
                return StatusCode(500, new { error = $"Error running diagnostics: {ex.Message}" });
            }
        }

        /// <summary>
        /// Test direct HTTP connectivity to the Azure OpenAI endpoint
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var endpoint = _configuration["AzureOpenAI:Endpoint"];
                if (string.IsNullOrEmpty(endpoint))
                {
                    return BadRequest(new { error = "Azure OpenAI endpoint is not configured" });
                }

                _logger.LogInformation("Testing connectivity to Azure OpenAI endpoint: {Endpoint}", endpoint);
                
                var diagnosticInfo = new Dictionary<string, object>();
                diagnosticInfo["Endpoint"] = endpoint;

                try
                {
                    // Basic domain connectivity test
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    
                    var uri = new Uri(endpoint);
                    var baseUrl = $"{uri.Scheme}://{uri.Host}";
                    
                    _logger.LogDebug("Testing connection to base URL: {BaseUrl}", baseUrl);
                    
                    var request = new HttpRequestMessage(HttpMethod.Head, baseUrl);
                    var response = await httpClient.SendAsync(request);
                    
                    diagnosticInfo["HttpStatus"] = (int)response.StatusCode;
                    diagnosticInfo["HttpStatusMessage"] = response.StatusCode.ToString();
                    diagnosticInfo["HttpSuccess"] = response.IsSuccessStatusCode;
                    
                    // DNS resolution test
                    try
                    {
                        var hostEntries = await System.Net.Dns.GetHostAddressesAsync(uri.Host);
                        var ipAddresses = new List<string>();
                        
                        foreach (var ip in hostEntries)
                        {
                            ipAddresses.Add(ip.ToString());
                        }
                        
                        diagnosticInfo["DnsResolution"] = "Success";
                        diagnosticInfo["IpAddresses"] = ipAddresses;
                    }
                    catch (Exception dnsEx)
                    {
                        diagnosticInfo["DnsResolution"] = "Failed";
                        diagnosticInfo["DnsError"] = dnsEx.Message;
                    }
                    
                    // TCP Socket connection test
                    try
                    {
                        using var tcpClient = new System.Net.Sockets.TcpClient();
                        var connectTask = tcpClient.ConnectAsync(uri.Host, uri.Port);
                        
                        // Set timeout for TCP connection
                        if (await Task.WhenAny(connectTask, Task.Delay(5000)) == connectTask)
                        {
                            diagnosticInfo["TcpConnection"] = "Success";
                        }
                        else
                        {
                            diagnosticInfo["TcpConnection"] = "Timeout";
                        }
                    }
                    catch (Exception tcpEx)
                    {
                        diagnosticInfo["TcpConnection"] = "Failed";
                        diagnosticInfo["TcpError"] = tcpEx.Message;
                        
                        if (tcpEx is System.Net.Sockets.SocketException socketEx)
                        {
                            diagnosticInfo["SocketErrorCode"] = socketEx.SocketErrorCode.ToString();
                            diagnosticInfo["NativeErrorCode"] = socketEx.NativeErrorCode;
                        }
                    }
                    
                    return Ok(new
                    {
                        timestamp = DateTime.UtcNow,
                        success = true,
                        diagnostics = diagnosticInfo
                    });
                }
                catch (Exception ex)
                {
                    diagnosticInfo["Error"] = ex.Message;
                    diagnosticInfo["ErrorType"] = ex.GetType().Name;
                    
                    if (ex.InnerException != null)
                    {
                        diagnosticInfo["InnerError"] = ex.InnerException.Message;
                        diagnosticInfo["InnerErrorType"] = ex.InnerException.GetType().Name;
                    }
                    
                    _logger.LogError(ex, "Error testing connection to Azure OpenAI endpoint");
                    
                    return Ok(new
                    {
                        timestamp = DateTime.UtcNow,
                        success = false,
                        diagnostics = diagnosticInfo
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in test-connection endpoint");
                return StatusCode(500, new { error = $"Unexpected error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Test if we can make a simple API call to Azure OpenAI
        /// </summary>
        [HttpGet("test-api")]
        public async Task<IActionResult> TestApi()
        {
            try
            {
                var endpoint = _configuration["AzureOpenAI:Endpoint"];
                var apiKey = _configuration["AzureOpenAI:ApiKey"];
                var deploymentName = _configuration["AzureOpenAI:DeploymentName"];
                var apiVersion = _configuration["AzureOpenAI:ApiVersion"] ?? "2025-01-01-preview";
                
                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(deploymentName))
                {
                    return BadRequest(new { error = "Missing Azure OpenAI configuration" });
                }

                _logger.LogInformation("Testing Azure OpenAI API with simple text completion");
                
                // Create a simple completion request
                var requestUrl = $"{endpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/chat/completions?api-version={apiVersion}";
                
                var requestData = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant." },
                        new { role = "user", content = "Say hello world" }
                    },
                    max_tokens = 50
                };
                
                var jsonRequestData = System.Text.Json.JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
                
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
                
                _logger.LogDebug("Sending request to: {Url}", requestUrl);
                
                var response = await httpClient.PostAsync(requestUrl, content);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                
                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    success = response.IsSuccessStatusCode,
                    statusCode = (int)response.StatusCode,
                    statusMessage = response.StatusCode.ToString(),
                    response = jsonResponse
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Azure OpenAI API");
                return StatusCode(500, new { error = $"Error testing API: {ex.Message}" });
            }
        }

        /// <summary>
        /// Test direct HTTP connectivity to the Azure OpenAI endpoint using the same parameters as your Postman test
        /// </summary>
        [HttpGet("test-direct-api")]
        public async Task<IActionResult> TestDirectApi()
        {
            try
            {
                // Get Azure OpenAI configuration from settings
                var apiKey = _configuration["AzureOpenAI:ApiKey"];
                var endpoint = _configuration["AzureOpenAI:Endpoint"]?.TrimEnd('/');
                var deploymentName = _configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
                var apiVersion = _configuration["AzureOpenAI:ApiVersion"] ?? "2025-01-01-preview";

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
                {
                    return BadRequest("Azure OpenAI configuration is incomplete");
                }

                // Construct the same request you used in Postman
                var requestUrl = $"{endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version={apiVersion}";
                
                var requestBodyObj = new
                {
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant." },
                        new { role = "user", content = "hello" }
                    }
                };

                var requestBody = JsonSerializer.Serialize(requestBodyObj);
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                // Create HttpClient with the same headers as your Postman request
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
                
                _logger.LogInformation("Sending test request to Azure OpenAI at: {Endpoint}", requestUrl);
                
                // Set a timeout to avoid long-running requests
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                // Send the request
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await httpClient.PostAsync(requestUrl, content);
                stopwatch.Stop();
                
                // Read the response
                var jsonResponse = await response.Content.ReadAsStringAsync();
                
                return Ok(new
                {
                    success = response.IsSuccessStatusCode,
                    statusCode = (int)response.StatusCode,
                    reasonPhrase = response.ReasonPhrase,
                    elapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    response = JsonDocument.Parse(jsonResponse)
                });
            }
            catch (HttpRequestException ex)
            {
                // Detailed network error information
                _logger.LogError(ex, "HTTP request error when testing direct API connection");
                return StatusCode(500, new 
                { 
                    error = "HTTP Request Error", 
                    message = ex.Message,
                    innerMessage = ex.InnerException?.Message,
                    statusCode = ex.StatusCode,
                    stackTrace = ex.StackTrace
                });
            }
            catch (TaskCanceledException ex)
            {
                // Timeout information
                _logger.LogError(ex, "Timeout when testing direct API connection");
                return StatusCode(500, new 
                { 
                    error = "Timeout Error", 
                    message = "The request to Azure OpenAI API timed out after 30 seconds"
                });
            }
            catch (Exception ex)
            {
                // General error information
                _logger.LogError(ex, "Error when testing direct API connection");
                return StatusCode(500, new 
                { 
                    error = "General Error", 
                    type = ex.GetType().Name,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Test DNS resolution of the Azure OpenAI endpoint
        /// </summary>
        [HttpGet("test-dns")]
        public async Task<IActionResult> TestDns()
        {
            try
            {
                var endpoint = _configuration["AzureOpenAI:Endpoint"];
                if (string.IsNullOrEmpty(endpoint))
                {
                    return BadRequest("Azure OpenAI endpoint is not configured");
                }

                var uri = new Uri(endpoint);
                var host = uri.Host;

                // Resolve DNS
                var ipAddresses = await System.Net.Dns.GetHostAddressesAsync(host);
                
                return Ok(new
                {
                    host,
                    ipAddresses = ipAddresses.Select(ip => ip.ToString()).ToArray(),
                    port = uri.Port,
                    scheme = uri.Scheme
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving DNS for Azure OpenAI endpoint");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Test TCP connectivity to the Azure OpenAI endpoint
        /// </summary>
        [HttpGet("test-tcp")]
        public async Task<IActionResult> TestTcp()
        {
            try
            {
                var endpoint = _configuration["AzureOpenAI:Endpoint"];
                if (string.IsNullOrEmpty(endpoint))
                {
                    return BadRequest("Azure OpenAI endpoint is not configured");
                }

                var uri = new Uri(endpoint);
                var host = uri.Host;
                var port = uri.Port;

                using var tcpClient = new System.Net.Sockets.TcpClient();
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await tcpClient.ConnectAsync(host, port);
                stopwatch.Stop();
                
                return Ok(new
                {
                    success = true,
                    host,
                    port,
                    connected = tcpClient.Connected,
                    elapsedMilliseconds = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing TCP connection to Azure OpenAI endpoint");
                return StatusCode(500, new 
                { 
                    error = "TCP Connection Error", 
                    type = ex.GetType().Name,
                    message = ex.Message,
                    socketErrorCode = (ex as System.Net.Sockets.SocketException)?.SocketErrorCode.ToString()
                });
            }
        }
    }
}

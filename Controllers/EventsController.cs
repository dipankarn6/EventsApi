using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace EventsApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EventsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents(string email)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                // Make the HTTP GET request to the external API
                var apiEndpoint = $"https://1uf1fhi7yk.execute-api.eu-west-2.amazonaws.com/default/events?email={email}";
                var response = await httpClient.GetAsync(apiEndpoint);

                // Handle different response status codes
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response body
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseBody);

                    var events = new List<Event>();

                    // Filter events by status ("Busy" or "OutOfOffice")
                    foreach (var eventData in apiResponse.Events)
                    {
                        if (eventData.Status == "Busy" || eventData.Status == "OutOfOffice")
                        {
                            events.Add(eventData);
                        }
                    }

                    return Ok(new
                    {
                        Email = apiResponse.Email,
                        Number_of_events = events.Count,
                        Events = events
                    });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    var error = JsonSerializer.Deserialize<ErrorResponse>(errorResponse);
                    return BadRequest(new { message = error.Message });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    var error = JsonSerializer.Deserialize<ErrorResponse>(errorResponse);
                    return StatusCode(429, new { message = error.Message });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    var error = JsonSerializer.Deserialize<ErrorResponse>(errorResponse);
                    return StatusCode(503, new { message = error.Message });
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    var error = JsonSerializer.Deserialize<ErrorResponse>(errorResponse);
                    return StatusCode(500, new { message = error.Message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }
    }

    public class ApiResponse
    {
        public string Email { get; set; }
        public int Number_of_events { get; set; }
        public List<Event> Events { get; set; }
    }

    public class Event
    {
        public string Subject { get; set; }
        public string Status { get; set; }
        public DateTime Start_time { get; set; }
        public DateTime End_time { get; set; }
        public string Id { get; set; }
    }

    public class ErrorResponse
    {
        public string Message { get; set; }
    }
}

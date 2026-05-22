using Hangfire;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Text;

namespace InactivateUsers
{
    public class Program
    {
        static int Main(string[] args)
        {
            try
            {
                ScheduleJob();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
                return 1;
            }
            finally
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        public static void ScheduleJob()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var connectionString = config["ConnectionString"]
                ?? throw new InvalidOperationException("ConnectionString is not configured.");
            var endpointEventsApi = config["EventsApiEndpoint"]
                ?? throw new InvalidOperationException("EventsApiEndpoint is not configured.");

            GlobalConfiguration.Configuration.UseSqlServerStorage(connectionString);

            var requestEvent = new
            {
                ApplicationId = 1,
                Version       = 3,
                Action        = 111,
                ActionName    = "CleanUsers",
                CreatedBy     = 65,
                CreatedByName = "Admin"
            };

            RecurringJob.AddOrUpdate<CallRestServiceJson>(
                "inactivate-users",
                x => x.Call(endpointEventsApi, "Queue", "SendMessage", requestEvent),
                Cron.Daily);

            Console.WriteLine($"Job 'inactivate-users' registered");
            Console.WriteLine($"  Schedule: Cron.Daily (00:00 UTC)");
            Console.WriteLine($"  Queue:    omnibees");
            Console.WriteLine($"  Endpoint: {endpointEventsApi}/api/Queue/SendMessage");
            Console.WriteLine($"  Action:   111 (CleanUsers)");
            Console.WriteLine($"  Payload:  ApplicationId=1, Version=3, CreatedBy=65 (Admin)");
            Console.WriteLine($"  Note:     NotificationGuid is generated fresh on each daily run");
        }

        public class CallRestServiceJson
        {
            private static readonly HttpClient _http = new HttpClient();

            [Queue("omnibees")]
            public void Call(string endpoint, string restService, string operation, object jsonRequest)
            {
                var jObject = JObject.FromObject(jsonRequest);
                jObject["NotificationGuid"] = Guid.NewGuid().ToString();
                var json = jObject.ToString();

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = _http.PostAsync(endpoint + "/api/" + restService + "/" + operation, content)
                                    .GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
            }
        }
    }
}

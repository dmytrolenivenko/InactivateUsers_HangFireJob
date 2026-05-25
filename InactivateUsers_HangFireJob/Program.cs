using Hangfire;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Text;
using Action = OB.Events.Contracts.Action;

namespace InactivateUsers_HangFireJob
{
    public class Program
    {
        private static readonly Action ActionEnum = Action.CleanUsers;
        private static readonly string ActionName = nameof(Action.CleanUsers);
        private const int    AdminUserId   = 65;
        private const string AdminUserName = "Admin";
        private const string JobName       = "inactivate-users";
        private const string QueueName     = "omnibees";
        private const int    ApplicationId = 1;

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
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
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
                ApplicationId = ApplicationId,
                Version       = 1,
                Action        = ActionEnum,
                ActionName    = ActionName,
                CreatedBy     = AdminUserId,
                CreatedByName = AdminUserName
            };

            RecurringJob.AddOrUpdate<CallRestServiceJson>(
                JobName,
                x => x.Call(endpointEventsApi, "Queue", "SendMessage", requestEvent),
                Cron.Daily);

            Console.WriteLine($"Job {JobName} registered");
            Console.WriteLine($"  Schedule: Cron.Daily (00:00 UTC)");
            Console.WriteLine($"  Queue:    {QueueName}");
            Console.WriteLine($"  Endpoint: {endpointEventsApi}/api/Queue/SendMessage");
            Console.WriteLine($"  Action:   {ActionEnum} ({ActionName})");
            Console.WriteLine($"  Payload:  ApplicationId={ApplicationId}, Version=1, CreatedBy={AdminUserId} ({AdminUserName})");
            Console.WriteLine($"  Note:     NotificationGuid is generated fresh on each daily run");
        }

        public class CallRestServiceJson
        {
            private static readonly HttpClient _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            [Queue(Program.QueueName)]
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

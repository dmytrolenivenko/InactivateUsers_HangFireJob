using Hangfire;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace InactivateUsers
{
    public class Program
    {
        static void Main(string[] args)
        {
            ScheduleJob();
           ///Run();
        }

        public static void ScheduleJob()
        {
            try
            {
                var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

                string connectionString = config.GetConnectionString("OmnibeesJobs") ?? throw new InvalidOperationException("Connection string is not configured."); ;
                GlobalConfiguration.Configuration.UseSqlServerStorage(connectionString);
                var endpointEventsApi = config["EventsApiEndpoint"] ?? throw new InvalidOperationException("EventsApiEndpoint is not configured.");

                var requestEvent = new
                {
                    ApplicationId = 1,
                    NotificationGuid = "8e9b1330-05cf-4bd8-85e4-6da4ee9f5384",
                    Version = 3,
                    Action = 111,
                    ActionName = "CleanUsers",
                    CreatedBy = 65,
                    CreatedByName = "Admin"
                };

                RecurringJob.AddOrUpdate<CallRestServiceJson>("inactivatesers-test", x => x.Call(endpointEventsApi, "Queue", "SendMessage", requestEvent), Cron.Daily);
                Console.WriteLine("Job registered successfully!");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        //public static void Run()
        //{
        //    using (var server = new BackgroundJobServer(new BackgroundJobServerOptions
        //    {
        //        Queues = new[] { "omnibees" }
        //    }))
        //    {
        //        Console.WriteLine("Hangfire Server running. Press Enter to exit...");
        //        Console.ReadLine();
        //    }
        //}

        public class CallRestServiceJson
        {
            [Queue("omnibees")]
            public void Call(string endpoint, string restService, string operation, object jsonRequest)
            {
                using (var httpClient = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(jsonRequest);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = httpClient.PostAsync(endpoint + "/api/" + restService + "/" + operation, content).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();
                }
            }
        }
    }
}

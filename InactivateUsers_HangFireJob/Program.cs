using Hangfire;
using Microsoft.Extensions.Configuration;
using OB.Services.Jobs.Operations;
using Action = OB.Events.Contracts.Action;

namespace InactivateUsers_HangFireJob
{
    public class Program
    {
        private static readonly int ActionEnum = (int)Action.CleanUsers;
        private static readonly string ActionName = nameof(Action.CleanUsers);
        private const string JobName       = "inactivate-users";
        private const string QueueName     = "omnibees";
        private const int    ApplicationId = 1;
        private const int    adminUserUid  = 65;
        private const string adminUserName = "Admin";

        static int Main(string[] args)
        {
            try
            {
                ScheduleJob();
                //Run(); // Uncomment this to run HangFire Server
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
                Version = 1,
                Action = ActionEnum,
                ActionName = ActionName,
                CreatedBy = adminUserUid,
                CreatedByName = adminUserName,
                TracingId = Guid.NewGuid().ToString(),
                NotificationGuid = Guid.NewGuid().ToString()
            };

            RecurringJob.AddOrUpdate<CallRestService>(JobName, x => x.Call(endpointEventsApi, "Queue", "SendMessage", requestEvent), Cron.Daily);

            Console.WriteLine($"Job {JobName} registered");
            Console.WriteLine($"  Schedule: {nameof(Cron.Daily)}");
            Console.WriteLine($"  Queue:    {QueueName}");
            Console.WriteLine($"  Endpoint: {endpointEventsApi}/api/Queue/SendMessage");
            Console.WriteLine($"  Action:   {ActionEnum} ({ActionName})");
            Console.WriteLine($"  Payload:  ApplicationId={ApplicationId}, Version=1, CreatedBy={adminUserUid} ({adminUserName})");
        }

        /// <summary>
        /// HangFireServer for testing
        /// </summary>
        //public static void Run()
        //{
        //    var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        //    var connectionString = config["ConnectionString"]
        //        ?? throw new InvalidOperationException("ConnectionString is not configured.");
        //    var endpointEventsApi = config["EventsApiEndpoint"]
        //        ?? throw new InvalidOperationException("EventsApiEndpoint is not configured.");
        //    GlobalConfiguration.Configuration.UseSqlServerStorage(connectionString);

        //    using (var server = new BackgroundJobServer(new BackgroundJobServerOptions
        //    {
        //        Queues = new[] { "omnibees" }
        //    }))
        //    {
        //        Console.WriteLine("Hangfire Server running. Press Enter to exit...");
        //        Console.ReadLine();
        //    }
        //}
    }
}

using System;
using System.Threading.Tasks;
using NLog.Extensions.Logging;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;
using Zeebe.Client.Impl.Builder;
using System.Net.Http;

namespace Client.Cloud.Example
{
    public class Program
    {
        HttpClient client = new HttpClient();
        public string? CallResult;

        public static async Task Main(string[] args)
        {
            //To setup client, create client in API page on Camunda Console
            // Change credientials to external file
            var zeebeClient =
                    CamundaCloudClientBuilder.Builder()
                        .UseClientId("Client Id")
                        .UseClientSecret("Client Secret") 
                        .UseContactPoint("Zeebe Address")// Zeebe Address
                    .UseLoggerFactory(new NLogLoggerFactory()) // optional
                    .Build();

            var topology = await zeebeClient.TopologyRequest().Send();

            Console.WriteLine("Connected to Zeebe Cluster : " + topology);

            // open job worker
            using (var signal = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                zeebeClient.NewWorker()
                      .JobType("orchestrate-something") //Service Task name here
                      .Handler(HandleJobAsync)
                      .MaxJobsActive(5)
                      .Name("KR-Worker")
                      .AutoCompletion()
                      .PollInterval(TimeSpan.FromSeconds(1))
                      .Timeout(TimeSpan.FromSeconds(10))
                      .Open();

                // blocks main thread, so that worker can run
                signal.WaitOne();
            }
        }

        private static async Task HandleJobAsync(IJobClient jobClient, IJob job)
        {
            // business logic
            var jobKey = job.Key;
            Console.WriteLine("Handling job: " + job);

            if (jobKey != 0)
            {
                Console.WriteLine("You have orchestrated something");
                Program program = new Program();
                string response = "";
                response = await program.GetHTTPRequest(response);

                jobClient.NewCompleteJobCommand(jobKey)
                    .Variables(response)
                    .Send()
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                // auto completion
            }
        }

        public async Task<string> GetHTTPRequest(string response) 
        {
            response = await client.GetStringAsync("http://catfact.ninja/fact");
            Console.WriteLine("Get Request Results : " + response);
            CallResult = response;
            return response;
        }

    }
}
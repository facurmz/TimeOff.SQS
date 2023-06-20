using System.Net;
using System.Text.Json;
using Amazon.Runtime.Internal;
using Amazon.SQS;
using Amazon.SQS.Model;
using TimeOff.SQS.Common;

AmazonSQSClient client = new();

string requestsQueueName = Constants.LeaveApplicationsRequestsQueueName;
string requestsQueueUrl = (await client.GetQueueUrlAsync(requestsQueueName)).QueueUrl;

string resultsQueueName = Constants.LeaveApplicationsResultsQueueName;
string resultsQueueUrl = (await client.GetQueueUrlAsync(resultsQueueName)).QueueUrl;

Console.WriteLine("Manager consumer started.");
Console.WriteLine("Waiting for leave application requests...");
Console.WriteLine("");

List<string> attributeNames = new() { "All" };
int maxNumberOfMessages = 5;
int visibilityTimeout = (int)TimeSpan.FromMinutes(10).TotalSeconds;
int waitTimeSeconds = (int)TimeSpan.FromSeconds(5).TotalSeconds;


while (true)
{
    ReceiveMessageRequest receiveMessageRequest = new()
    {
        QueueUrl = requestsQueueUrl,
        AttributeNames = attributeNames,
        MaxNumberOfMessages = maxNumberOfMessages,
        VisibilityTimeout = visibilityTimeout,
        WaitTimeSeconds = waitTimeSeconds
    };

    var response = await client.ReceiveMessageAsync(receiveMessageRequest);

    if (response.Messages.Count > 0)
    {
        var tasks = response.Messages.ToList().Select(m => ProcessRequest(m));

        await Task.WhenAll(tasks);
    }
    else
    {
        Thread.Sleep(TimeSpan.FromSeconds(1));
    }
}

async Task ProcessRequest(Message message)
{
    LeaveApplicationRequest? leaveApplicationRequest = JsonSerializer.Deserialize<LeaveApplicationRequest>(message.Body);

    if (leaveApplicationRequest != null)
    {
        Console.WriteLine($"Received message from queue {requestsQueueName} with value {JsonSerializer.Serialize(leaveApplicationRequest)}");

        Console.WriteLine("Approve request? (Y/N):");
        bool isApproved = (Console.ReadLine() ?? "N").Equals("Y", StringComparison.OrdinalIgnoreCase);

        LeaveApplicationResult leaveApplicationResult = new(
            leaveApplicationRequest.Email,
            leaveApplicationRequest.Department,
            leaveApplicationRequest.LeaveDurationInHours,
            leaveApplicationRequest.LeaveStartDateTicks,
            "Manager",
            isApproved
        );

        SendMessageRequest sendMessageRequest = new() {
            MessageBody = JsonSerializer.Serialize(leaveApplicationResult),
            QueueUrl = resultsQueueUrl
        };

        var sendResponse = await client.SendMessageAsync(sendMessageRequest);

        if (sendResponse.HttpStatusCode == HttpStatusCode.OK)
        {
            Console.WriteLine($"Leave request processed. Successfully sent message to queue {resultsQueueName}. Message ID: {sendResponse.MessageId}.");

            DeleteMessageRequest deleteMessageRequest = new()
            {
                QueueUrl = requestsQueueUrl,
                ReceiptHandle = message.ReceiptHandle
            };

            var deleteResponse = await client.DeleteMessageAsync(deleteMessageRequest);

            if (deleteResponse.HttpStatusCode == HttpStatusCode.OK)
                Console.WriteLine($"Successfully deleted message from queue {requestsQueueName}.");
            else
                Console.WriteLine("Could not delete message.");
        }
        else
            Console.WriteLine("Could not send message.");

        Console.WriteLine("");
    }
}
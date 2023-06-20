using System.Net;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using TimeOff.SQS.Common;

AmazonSQSClient client = new();

string resultsQueueName = Constants.LeaveApplicationsResultsQueueName;
string resultsQueueUrl = (await client.GetQueueUrlAsync(resultsQueueName)).QueueUrl;

Console.WriteLine("Reader consumer started.");
Console.WriteLine("Waiting for leave applications results...");
Console.WriteLine("");

List<string> attributeNames = new() { "All" };
int maxNumberOfMessages = 5;
int visibilityTimeout = (int)TimeSpan.FromMinutes(10).TotalSeconds;
int waitTimeSeconds = (int)TimeSpan.FromSeconds(5).TotalSeconds;

while (true)
{
    ReceiveMessageRequest receiveMessageRequest = new()
    {
        QueueUrl = resultsQueueUrl,
        AttributeNames = attributeNames,
        MaxNumberOfMessages = maxNumberOfMessages,
        VisibilityTimeout = visibilityTimeout,
        WaitTimeSeconds = waitTimeSeconds
    };

    var response = await client.ReceiveMessageAsync(receiveMessageRequest);

    if (response.Messages.Count > 0)
    {
        var tasks = response.Messages.ToList().Select(ProcessRequest);

        await Task.WhenAll(tasks);
    }
    else
    {
        Thread.Sleep(TimeSpan.FromSeconds(1));
    }
}

async Task ProcessRequest(Message message)
{
    LeaveApplicationResult? leaveApplicationResult = JsonSerializer.Deserialize<LeaveApplicationResult>(message.Body);

    if (leaveApplicationResult != null)
        Console.WriteLine($"Received message from queue {resultsQueueName} with value {JsonSerializer.Serialize(leaveApplicationResult)}");

    DeleteMessageRequest deleteMessageRequest = new()
    {
        QueueUrl = resultsQueueUrl,
        ReceiptHandle = message.ReceiptHandle
    };

    var deleteResponse = await client.DeleteMessageAsync(deleteMessageRequest);

    if (deleteResponse.HttpStatusCode == HttpStatusCode.OK)
        Console.WriteLine($"Successfully deleted message from queue {resultsQueueName}.");
    else
        Console.WriteLine("Could not delete message.");
}
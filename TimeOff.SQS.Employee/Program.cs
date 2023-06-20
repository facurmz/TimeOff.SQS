using System.Globalization;
using System.Net;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using TimeOff.SQS.Common;

AmazonSQSClient client = new();

string queueName = Constants.LeaveApplicationsRequestsQueueName;
string queueUrl = (await client.GetQueueUrlAsync(queueName)).QueueUrl;

Console.WriteLine("Employee producer started.");
Console.WriteLine("Preparing to submit application requests...");
Console.WriteLine("");

while (true)
{
    Console.WriteLine("Enter your employee email (e.g. example@your-company.com):");
    string? cEmail = Console.ReadLine();
    string email = (cEmail == null || cEmail == "") ? "example@your-company.com" : cEmail;

    Console.WriteLine("Enter your department code (HR, IT or OPS):");
    string? cDepartment = Console.ReadLine();
    string department = (cDepartment == null || cDepartment == "")  ? "IT" : cDepartment;

    Console.WriteLine("Enter the number of hours of leave requested (e.g. 8):");
    string? cLeaveDurationInHours = Console.ReadLine();
    int leaveDurationInHours = (cLeaveDurationInHours == null || cLeaveDurationInHours == "") ? 8 : int.Parse(cLeaveDurationInHours);

    Console.WriteLine("Enter your leave start date (DD-MM-YYYY):");
    string? cLeaveStartDate = Console.ReadLine();
    DateTime leaveStartDate = (cLeaveStartDate == null || cLeaveStartDate == "") ? DateTime.Now.AddDays(30) : DateTime.ParseExact(cLeaveStartDate, "dd-mm-yyyy", CultureInfo.InvariantCulture);

    LeaveApplicationRequest leaveApplicationRequest = new(email, department, leaveDurationInHours, leaveStartDate.Ticks);

    SendMessageRequest request = new() {
        MessageGroupId = "Employee",
        MessageBody = JsonSerializer.Serialize(leaveApplicationRequest),
        QueueUrl = queueUrl
    };

    var response = await client.SendMessageAsync(request);

    if (response.HttpStatusCode == HttpStatusCode.OK)
        Console.WriteLine($"Successfully sent message to queue {queueName}. Message ID: {response.MessageId}.");
    else
        Console.WriteLine("Could not send message.");

    Console.WriteLine("");
}

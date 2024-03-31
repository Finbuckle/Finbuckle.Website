using Amazon;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Options;

namespace Finbuckle.Website.Infrastructure;

public class MailService(IOptions<AmazonSesOptions> options)
{
    public async Task SendTextEmailAsync(string email, string subject, string body)
    {
        const string senderAddress = "noreply@finbuckle.com";
        using var client =
            new AmazonSimpleEmailServiceV2Client(options.Value.Key, options.Value.Secret, RegionEndpoint.USEast2);
        var sendRequest = new SendEmailRequest
        {
            FromEmailAddress = senderAddress,
            Destination = new Destination
            {
                ToAddresses = [email]
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content
                    {
                      Data = subject  
                    },
                    Body = new Body
                    {
                        Text = new Content
                        {
                            Charset = "UTF-8",
                            Data = body
                        }
                    }
                }
            }
        };

        try
        {
            Console.WriteLine("Sending email using Amazon SES...");
            await client.SendEmailAsync(sendRequest);
            Console.WriteLine("The email was sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("The email was not sent.");
            Console.WriteLine("Error message: " + ex.Message);
            throw;
        }
    }
}
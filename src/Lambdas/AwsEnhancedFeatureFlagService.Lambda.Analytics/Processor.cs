using Amazon.Lambda.KinesisFirehoseEvents;
using System.Text;
using static Amazon.Lambda.KinesisFirehoseEvents.KinesisFirehoseResponse;

namespace AwsEnhancedFeatureFlagService.Lambda.Analytics;

public class Processor
{
    public KinesisFirehoseResponse FunctionHandler(KinesisFirehoseEvent kinesisFirehoseEvent)
    {
        var records = new List<FirehoseRecord>();

        foreach (var record in kinesisFirehoseEvent.Records)
        {
            Console.WriteLine("Record with new line test {0}", record.DecodeData() + @"\n");

            var newLineData = Encoding.UTF8.GetBytes(record.DecodeData() + Environment.NewLine);

            var rowWithNewLine = Convert.ToBase64String(newLineData);

            var firehoseRecord = new FirehoseRecord
            {
                RecordId = record.RecordId,
                Result = "Ok",
                Base64EncodedData = rowWithNewLine
            };

            records.Add(firehoseRecord);
        }

        return new KinesisFirehoseResponse { Records = records };
    }
}
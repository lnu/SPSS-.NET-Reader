using System.IO;
using SpssLib.DataReader;
using Xunit;

namespace Test.SpssLibCore
{
    public class TestSpssCopy
    {
        [Fact]
        public void TestCopyFile()
        {
            var filename = @".\TestFiles\cakespss1000similarvars.sav";
            using (FileStream fileStream =
                new FileStream(filename, FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read, 2048 * 10, FileOptions.SequentialScan))
            {
                using (FileStream writeStream = new FileStream("ourcake1000similarvars.sav", FileMode.Create, FileAccess.Write))
                {
                    SpssReader spssDataset = new SpssReader(fileStream);

                    SpssWriter spssWriter = new SpssWriter(writeStream, spssDataset.Variables);

                    foreach (var record in spssDataset.Records)
                    {
                        var newRecord = spssWriter.CreateRecord(record);
                        spssWriter.WriteRecord(newRecord);
                    }

                    spssWriter.EndFile();
                }
            }
            Assert.True(true); // To check errors, set <DeleteDeploymentDirectoryAfterTestRunIsComplete> to False and open the file
        }
    }
}
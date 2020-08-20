using ETLBox.Connection;
using ETLBox.DataFlow;
using ETLBox.DataFlow.Connectors;
using ETLBox.Exceptions;
using ETLBoxTests.Fixtures;
using ETLBoxTests.Helper;
using System;
using System.Collections.Generic;
using Xunit;

namespace ETLBoxTests.DataFlowTests
{
    [Collection("DataFlow")]
    public class CustomSourceErrorLinkingTests
    {
        public SqlConnectionManager SqlConnection => Config.SqlConnection.ConnectionManager("DataFlow");
        public CustomSourceErrorLinkingTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public class MySimpleRow
        {
            public int Col1 { get; set; }
            public string Col2 { get; set; }
        }

        [Fact]
        public void RedirectOneErrorRow()
        {
            //Arrange
            TwoColumnsTableFixture dest2Columns = new TwoColumnsTableFixture("ErrorLinkingCustomSource");
            List<string> Data = new List<string>() { "Test1", "Test2", "Test3", "Test4" };
            int _readIndex = 0;
            Func<MySimpleRow> ReadData = () =>
            {
                var result = new MySimpleRow()
                {
                    Col1 = _readIndex + 1,
                    Col2 = Data[_readIndex]
                };
                _readIndex++;
                if (_readIndex == 4) throw new Exception("Error record!");
                return result;
            };

            Func<bool> EndOfData = () => _readIndex >= Data.Count;

            CustomSource<MySimpleRow> source = new CustomSource<MySimpleRow>(ReadData, EndOfData);
            DbDestination<MySimpleRow> dest = new DbDestination<MySimpleRow>(SqlConnection, "ErrorLinkingCustomSource");
            MemoryDestination<ETLBoxError> errorDest = new MemoryDestination<ETLBoxError>();

            //Act
            source.LinkTo(dest);
            source.LinkErrorTo(errorDest);
            source.Execute();
            dest.Wait();
            errorDest.Wait();

            //Assert
            dest2Columns.AssertTestData();
            Assert.Collection<ETLBoxError>(errorDest.Data,
                d => Assert.True(!string.IsNullOrEmpty(d.RecordAsJson) && !string.IsNullOrEmpty(d.ErrorText))
            );
        }

        [Fact]
        public void CompleteBufferBeforeEnd()
        {
            //Arrange
            TwoColumnsTableFixture dest2Columns = new TwoColumnsTableFixture("ErrorLinkingCustomSource2");
            var testData = new List<MySimpleRow>();
            for (int i = 0; i <= 10000; i++)
                testData.Add(new MySimpleRow() { Col1 = i, Col2 = $"Test{i}" });

            int _readIndex = 0;
            Func<MySimpleRow> ReadData = () =>
            {
                var result = testData[_readIndex];
                _readIndex++;
                return result;
            };

            Func<bool> EndOfData = () => _readIndex >= testData.Count;

            CustomSource<MySimpleRow> source = new CustomSource<MySimpleRow>(ReadData, EndOfData);
            DbDestination<MySimpleRow> dest = new DbDestination<MySimpleRow>(SqlConnection, "ErrorLinkingCustomSource2");

            //Act & Assert
            Assert.Throws<ETLBoxException>(() =>
            {
                try
                {

                    source.LinkTo(dest);
                    source.ExecuteAsync();
                    source.SourceBlock.Complete();
                    dest.Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;

                }
            });

        }
    }
}

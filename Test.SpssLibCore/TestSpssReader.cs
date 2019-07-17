using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SpssLib.DataReader;
using SpssLib.FileParser;
using SpssLib.SpssDataset;
using Xunit;

namespace Test.SpssLibCore
{
    public class TestSpssReader
    {
        [Fact]
        public void TestReadFile()
        {
            var filename = @".\TestFiles\test.sav";
            FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read,
                FileShare.Read, 2048 * 10, FileOptions.SequentialScan);

            int[] varenieValues = { 1, 2, 1 };
            string[] streetValues = { "Landsberger Straße", "Fröbelplatz", "Bayerstraße" };

            int varCount;
            int rowCount;
            try
            {
                ReadData(fileStream, out varCount, out rowCount,
                    new Dictionary<int, Action<int, Variable>>
                    {
                        {0, (i, variable) =>
                        {
                            Assert.True("varaible ñ"== variable.Label, "Label mismatch");
                            Assert.True(DataType.Numeric== variable.Type, "First file variable should be  a Number");
                        }},
                        {1, (i, variable) =>
                        {
                            Assert.True("straße"== variable.Label, "Label mismatch");
                            Assert.True(DataType.Text== variable.Type, "Second file variable should be  a text");
                        }}
                    },
                    new Dictionary<int, Action<int, int, Variable, object>>
                    {
                        {0, (r, c, variable, value) =>
                        {   // All numeric values are doubles
                            Assert.IsType<double>(value); //"First row variable should be a Number"
                            double v = (double) value;
                            Assert.True(varenieValues[r]== v, "Int value is different");
                        }},
                        {1, (r, c, variable, value) =>
                        {
                            Assert.IsType<string>(value); //, "Second row variable should be  a text");
                            string v = (string) value;
                            Assert.True(streetValues[r]== v, "String value is different");
                        }}
                    });
            }
            finally
            {
                fileStream.Close();
            }

            Assert.True(varCount == 3, "Variable count does not match");
            Assert.True(rowCount == 3, "Rows count does not match");
        }

        [Fact]
        public void TestEmptyStream()
        {
            int varCount;
            int rowCount;
            Assert.Throws<SpssFileFormatException>(() => ReadData(new MemoryStream(new byte[0]), out varCount, out rowCount));
        }

        [Fact]
        public void TestReadMissingValuesAsNull()
        {
            var filename = @".\TestFiles\MissingValues.sav";
            FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read,
                FileShare.Read, 2048 * 10, FileOptions.SequentialScan);

            double?[][] varValues =
            {
                new double?[]{ 0, 1, 2, 3, 4, 5, 6, 7 }, // No missing values
                new double?[]{ 0, null, 2, 3, 4, 5, 6, 7 }, // One mssing value
                new double?[]{ 0, null, null, 3, 4, 5, 6, 7 }, // Two missing values
                new double?[]{ 0, null, null, null, 4, 5, 6, 7 }, // Three missing values
                new double?[]{ 0, null, null, null, null, null, 6, 7 }, // Range
                new double?[]{ 0, null, null, null, null, null, 6, null }, // Range & one value
            };

            Action<int, int, Variable, object> rowCheck = (r, c, variable, value) =>
            {
            Assert.Equal(varValues[c][r], value);//, $"Wrong value: row {r}, variable {c}");
                //Assert.True(varValues[c][r] == value, $"Wrong value: row {r}, variable {c}");
            };


            try
            {
                int varCount, rowCount;
                ReadData(fileStream, out varCount, out rowCount, new Dictionary<int, Action<int, Variable>>
                    {
                        {0, (i, variable) => Assert.True(MissingValueType.NoMissingValues== variable.MissingValueType)},
                        {1, (i, variable) => Assert.True(MissingValueType.OneDiscreteMissingValue== variable.MissingValueType)},
                        {2, (i, variable) => Assert.True(MissingValueType.TwoDiscreteMissingValue== variable.MissingValueType)},
                        {3, (i, variable) => Assert.True(MissingValueType.ThreeDiscreteMissingValue== variable.MissingValueType)},
                        {4, (i, variable) => Assert.True(MissingValueType.Range== variable.MissingValueType)},
                        {5, (i, variable) => Assert.True(MissingValueType.RangeAndDiscrete== variable.MissingValueType)},
                    },
                    new Dictionary<int, Action<int, int, Variable, object>>
                    {
                        {0, rowCheck},
                        {1, rowCheck},
                        {2, rowCheck},
                        {3, rowCheck},
                        {4, rowCheck},
                        {5, rowCheck},
                        {6, rowCheck},
                    });
            }
            finally
            {
                fileStream.Close();
            }
        }

        internal static void ReadData(Stream fileStream, out int varCount, out int rowCount,
            IDictionary<int, Action<int, Variable>> variableValidators = null, IDictionary<int, Action<int, int, Variable, object>> valueValidators = null)
        {
            SpssReader spssDataset = new SpssReader(fileStream);

            varCount = 0;
            rowCount = 0;

            var variables = spssDataset.Variables;
            foreach (var variable in variables)
            {
                Debug.WriteLine("{0} - {1}", variable.Name, variable.Label);
                foreach (KeyValuePair<double, string> label in variable.ValueLabels)
                {
                    Debug.WriteLine(" {0} - {1}", label.Key, label.Value);
                }

                Action<int, Variable> checkVariable;
                if (variableValidators != null && variableValidators.TryGetValue(varCount, out checkVariable))
                {
                    checkVariable(varCount, variable);
                }

                varCount++;
            }

            foreach (var record in spssDataset.Records)
            {
                var varIndex = 0;
                foreach (var variable in variables)
                {
                    Debug.Write(variable.Name);
                    Debug.Write(':');
                    var value = record.GetValue(variable);
                    Debug.Write(value);
                    Debug.Write('\t');

                    Action<int, int, Variable, object> checkValue;
                    if (valueValidators != null && valueValidators.TryGetValue(varIndex, out checkValue))
                    {
                        checkValue(rowCount, varIndex, variable, value);
                    }

                    varIndex++;
                }
                Debug.WriteLine("");

                rowCount++;
            }
        }
    }
}

using Trino.Client.Types;

using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Trino.Data.ADO.Server;
using Trino.Data.ADO.Client;

namespace Trino.Client.Test
{
    [TestClass]
    public class TestTypes
    {
        [TestMethod]
        public void TestTimestampConversion()
        {
            using (TrinoTestServer server = TrinoTestServer.Create("trino_test_timestamp_conversion.txt"))
            {
                TrinoConnectionProperties properties = server.GetConnectionProperties();
                properties.Catalog = "tpch";
                using (TrinoConnection tc = new(properties))
                {
                    CancellationTokenSource cancellationTokenSource = new();
                    using (TrinoCommand trinoCommand = new(tc, "select timestamp '2024-01-02 01:02:03.456' as ts, \"timestamp with time zone\" '2024-01-02 01:02:03.456 +05:00' as tz, cast('2024-01-02 01:02:03.456789 +05:00' as timestamp(6) with time zone) as tz6"))
                    {
                        TrinoDataReader reader = (TrinoDataReader)trinoCommand.ExecuteReader(CommandBehavior.CloseConnection);
                        reader.Read();
                        DateTime col1DateTime = reader.GetDateTime(0);
                        Assert.AreEqual(DateTime.Parse("2024-01-02 01:02:03.456"), col1DateTime);
                        DateTime col2DateTime = reader.GetDateTime(1);
                        Assert.AreEqual(DateTime.Parse("2024-01-02 01:02:03.456"), col2DateTime);
                        DateTimeOffset col2DateTimeOffset = reader.GetDateTimeOffset(1);
                        Assert.AreEqual(DateTimeOffset.Parse("2024-01-02 01:02:03.456 +05:00"), col2DateTimeOffset);
                        DateTime col3DateTime = reader.GetDateTime(2);
                        Assert.AreEqual(DateTime.Parse("2024-01-02 01:02:03.456789"), col3DateTime);
                        DateTimeOffset col3DateTimeOffset = reader.GetDateTimeOffset(2);
                        Assert.AreEqual(DateTimeOffset.Parse("2024-01-02 01:02:03.456789 +05:00"), col3DateTimeOffset);
                        // note, test does not terminate connection
                    }
                }
            }
        }

        /// <summary>
        /// Test that after cancellation schema can still be retrieved.
        /// </summary>
        [TestMethod]
        public void TestReadZeroRows()
        {
            using (TrinoTestServer server = TrinoTestServer.Create("zero_rows.txt"))
            {
                TrinoConnectionProperties properties = server.GetConnectionProperties();
                properties.Catalog = "tpch";
                properties.SessionProperties = new Dictionary<string, string>() { { "query_cache_enabled", "true" }, { "query_cache_ttl", "1h" } };

                using (TrinoConnection tc = new(properties))
                {
                    CancellationTokenSource cancellationTokenSource = new();
                    using (TrinoCommand trinoCommand = new(tc, "select * from tpch.sf100000.customer limit 0"))
                    {
                        DbDataReader reader = trinoCommand.ExecuteReader(CommandBehavior.CloseConnection);
                        DataTable? schema = reader.GetSchemaTable();
                        Assert.AreEqual(reader.Read(), false);
                    }
                }
            }
        }

        /// <summary>
        /// Test NaN values handling in the response
        /// </summary>
        [TestMethod]
        public async Task TestNanProgress()
        {
            using (TrinoTestServer server = TrinoTestServer.Create("nan_progress.txt"))
            {
                var properties = server.GetConnectionProperties();
                properties.Catalog = "memory";

                using (TrinoConnection tc = new(properties))
                {
                    using (TrinoCommand trinoCommand = new(tc, "INSERT INSERT memory.default.target_table (Id) SELECT Id FROM memory.default.target_table"))
                    {
                        var result = await trinoCommand.ExecuteNonQueryAsync(CancellationToken.None);
                        Assert.AreEqual(result, 0);
                    }
                }
            }
        }

        /// <summary>
        /// Test that after cancellation schema can still be retrieved.
        /// </summary>
        [TestMethod]
        public void TestCancellationGetSchema()
        {
            using (TrinoTestServer server = TrinoTestServer.Create("trino_cancel.txt"))
            {
                TrinoConnectionProperties properties = server.GetConnectionProperties();
                properties.Catalog = "tpch";
                properties.SessionProperties = new Dictionary<string, string>() { { "query_cache_enabled", "true" }, { "query_cache_ttl", "1h" } };
                string all_types = $@"select * from tpch.sf100000.customer limit 2";

                using (TrinoConnection tc = new(properties))
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    using (IDbCommand trinoCommand = new TrinoCommand(tc, all_types, TimeSpan.MaxValue, null, null))
                    {
                        IDataReader idr = trinoCommand.ExecuteReader();
                        DataTable? tableWithSchema = idr.GetSchemaTable();
                        trinoCommand.Cancel();
                        // should be able to get table with schema after cancellation
                        DataTable? tableWithSchema2 = idr.GetSchemaTable();
                        Assert.IsNotNull(tableWithSchema);
                        Assert.IsNotNull(tableWithSchema2);
                        Assert.AreEqual(tableWithSchema.Rows.Count, tableWithSchema2.Rows.Count);
                        Assert.AreEqual(tableWithSchema.Columns.Count, tableWithSchema2.Columns.Count);
                        Assert.AreEqual(tableWithSchema.Columns[0].ColumnName, tableWithSchema2.Columns[0].ColumnName);
                    }
                    Console.WriteLine("Duration of cancel query: " + stopwatch.ElapsedMilliseconds + "ms");
                }
            }
        }

        /// <summary>
        /// Test that after cancellation schema can still be retrieved.
        /// </summary>
        [TestMethod]
        public void TestTimeout()
        {
            using (TrinoTestServer server = TrinoTestServer.Create("trino_client_timeout.txt", TimeSpan.FromSeconds(5)))
            {
                try
                {
                    TrinoConnectionProperties properties = server.GetConnectionProperties();
                    properties.Catalog = "tpch";
                    string all_types = $@"select * from tpch.sf100000.customer limit 2";

                    using (TrinoConnection tc = new(properties))
                    {
                        using (IDbCommand trinoCommand = new TrinoCommand(tc, all_types, TimeSpan.FromSeconds(10), null, null))
                        {
                            IDataReader idr = trinoCommand.ExecuteReader();
                            while (idr.Read()) {
                                Console.WriteLine("Read 1 row");
                            }
                        }
                    }

                    throw new TimeoutException("This test is expected to time out.");
                }
                catch (TrinoAggregateException e)
                {
                    bool foundTimeout = false;
                    foreach (Exception item in e.InnerExceptions)
                    {
                        if (item is TimeoutException)
                        {
                            foundTimeout = true;
                        }
                    }
                    Assert.IsTrue(foundTimeout);
                }
            }
        }

        /// <summary>
        /// Test that parameters are handled correctly.
        /// </summary>
        [TestMethod]
        public void TestParameters()
        {
            using (TrinoTestServer server = TrinoTestServer.Create("parameters.txt"))
            {
                TrinoConnectionProperties properties = server.GetConnectionProperties();
                properties.Catalog = "delta";
                string query = @"select * from (
                select timestamp '2024-01-01 00:00:00.000' as val
                union all
                    select ""timestamp with time zone"" '2024-01-01 00:00:00.000 UTC' as val
                union all
                    select timestamp '2024-01-01 00:00:00' as val
                union all
                    select date '2024-01-01' as val
                )
                where val = ? and val = ?";

                using (TrinoConnection tc = new(properties))
                {
                    CancellationTokenSource cancellationTokenSource = new();
                    using (IDbCommand trinoCommand = new TrinoCommand(tc, query, TimeSpan.MaxValue, null, null))
                    {
                        IDbDataParameter parameter = trinoCommand.CreateParameter();
                        parameter.Value = new DateTime(2024, 1, 1, 0, 0, 0);
                        IDbDataParameter parameter2 = trinoCommand.CreateParameter();
                        parameter2.Value = new DateTimeOffset(2024, 1, 1, 0, 0, 0, new TimeSpan(0));
                        IDataReader reader = trinoCommand.ExecuteReader(CommandBehavior.CloseConnection);
                        reader.Read();
                        Assert.AreEqual(reader.GetString(0), "2024-01-01 00:00:00.000 UTC");
                        reader.Read();
                        Assert.AreEqual(reader.GetString(0), "2024-01-01 00:00:00.000 UTC");
                        reader.Read();
                        Assert.AreEqual(reader.GetString(0), "2024-01-01 00:00:00.000 UTC");
                        reader.Read();
                        Assert.AreEqual(reader.GetString(0), "2024-01-01 00:00:00.000 UTC");
                        Assert.AreEqual(reader.Read(), false);
                    }
                }
            }
        }

        /// <summary>
        /// Test that exceptions are handled properly by the client.
        /// </summary>
        [TestMethod]
        public void TrinoExceptionTest()
        {
            using (TrinoTestServer server = TrinoTestServer.Create("trino_exception_test.txt"))
            {
                TrinoConnectionProperties properties = server.GetConnectionProperties();
                properties.SessionProperties = new Dictionary<string, string>() { { "query_cache_enabled", "false" } };
                properties.Catalog = "delta";
                string query = @"SELECT 'i' = 0";

                try
                {

                    while (true)
                    {
                        using (TrinoConnection tc = new(properties))
                        {
                            CancellationTokenSource cancellationTokenSource = new();
                            using (IDbCommand trinoCommand = new TrinoCommand(tc, query, TimeSpan.MaxValue, null, null))
                            {
                                IDataReader reader = trinoCommand.ExecuteReader(CommandBehavior.CloseConnection);
                                while (reader.Read()) ;
                            }
                        }
                    }
                }
                catch (Exception ae)
                {
                    Assert.AreEqual(ae.Message, "One or more errors occurred. (line 1:12: Cannot apply operator: varchar(1) = integer)");
                }
            }
        }

        private readonly string[] columnColumnNames = ["TABLE_CATALOG", "TABLE_SCHEMA", "TABLE_NAME", "COLUMN_NAME", "ORDINAL_POSITION", "COLUMN_DEFAULT", "IS_NULLABLE", "DATA_TYPE"];
        private readonly string[] availableSchemas = ["catalogs", "schemas", "schemata", "tables", "columns", "views", "functions", "sessions"];
        private readonly string[] firstRowColumns = ["delta", "nyc", "request_by_region", "region", "1", "", "YES", "varchar"];

        [TestMethod]
        public void TestGetSchema()
        {
            using (TrinoTestServer server = TrinoTestServer.Create("trino_schema_columns.txt"))
            {
                TrinoConnectionProperties properties = server.GetConnectionProperties();
                properties.Catalog = "delta";
                properties.Schema = "nyc";

                using (TrinoConnection tc = new(properties))
                {
                    DataTable schemas = tc.GetSchema();
                    Assert.AreEqual(schemas.Rows.Count, 8);
                    Assert.AreEqual(schemas.Columns[0].ColumnName, "CollectionName");
                    for (int i = 0; i < availableSchemas.Length; i++)
                    {
                        Assert.AreEqual(schemas.Rows[i][0]?.ToString()?.ToLower(), availableSchemas[i]);
                    }

                    DataTable columns = tc.GetSchema("columns");
                    for (int i = 0; i < columnColumnNames.Length; i++)
                    {
                        Assert.AreEqual(columns.Columns[i].ColumnName.ToLower(), columnColumnNames[i].ToLower());
                    }

                    // loop through the first row
                    for (int i = 0; i < columnColumnNames.Length; i++)
                    {
                        Assert.AreEqual(columns.Rows[0][i]?.ToString()?.ToLower(), firstRowColumns[i].ToLower());
                    }

                    Assert.AreEqual(columns.Rows.Count, 158);
                }
            }
        }

        /// <summary>
        /// Verifies that session properties are set correctly over multiple queries.
        /// </summary>
        [TestMethod]
        public void TrinoSessionTest()
        {
            using (TrinoTestServer server = TrinoTestServer.Create("trino_session_test.txt"))
            {
                TrinoConnectionProperties properties = server.GetConnectionProperties();
                properties.Catalog = "tpch";
                properties.Source = "Pythagoras";

                using (TrinoConnection connection = new(properties))
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    using (TrinoCommand trinoCommand = new(connection, "set session writer_min_size='64MB'"))
                    {
                        trinoCommand.ExecuteNonQuery();
                    }
                    Assert.AreEqual(connection.ConnectionSession.Properties.Source, properties.Source);
                    connection.ConnectionSession.Properties.Source = "Archimedes";
                    using (TrinoCommand trinoCommand = new(connection, "USE tpch.sf10"))
                    {
                        trinoCommand.ExecuteNonQuery();
                    }
                    using (TrinoCommand trinoCommand = new(connection, "SET SESSION hive.insert_existing_partitions_behavior = 'OVERWRITE'"))
                    {
                        trinoCommand.ExecuteNonQuery();
                    }

                    Assert.AreEqual(connection.ConnectionSession.Properties.Source, "Archimedes");
                    Assert.AreEqual(connection.ConnectionSession.Properties.Catalog, "tpch");
                    Assert.AreEqual(connection.ConnectionSession.Properties.Schema, "sf10");
                    Assert.IsTrue(connection.ConnectionSession.Properties.Properties.ContainsKey("hive.insert_existing_partitions_behavior"));
                    Assert.AreEqual(connection.ConnectionSession.Properties.Properties["hive.insert_existing_partitions_behavior"], "OVERWRITE");
                }
            }
        }

        /// <summary>
        /// Verifies type support
        /// </summary>
        [TestMethod]
        public void TestAllTypes()
        {
            using (TrinoTestServer server = TrinoTestServer.Create("trino_all_types.txt"))
            {
                string all_types = $@"SELECT
                CAST(NULL as varchar) AS null_varchar_column,
                9223372036854775806 AS big_int_column,
                2147483647 AS int_column,
                CAST(32767 AS smallint) AS small_int_column,
                CAST(-127 AS tinyint) AS tiny_int_column,
                Cast(3.402823466E+38 AS REAL) AS real_column,
                Cast(1.7976931348623158E+308 AS DOUBLE) AS double_column,
                Cast(678.12345 AS DECIMAL(8, 5)) AS decimal_column,
                Cast(123456789000.1234005 AS DECIMAL(24, 10)) AS big_decimal_column,
                true AS boolean_column,
                cast('0123456789abc' as char(10)) AS char_column,
                '0123456789abc' AS varchar_column,
                Cast('2022-02-22' AS DATE) AS date_column,
                cast('12:34:56.123' AS time) AS time_column,
                cast('01:02:03.004+05:00' AS time with time zone) AS timewithtimezone_column,
                cast('2022-02-22 12:34:56.004' as timestamp) as timestamp_column,
                cast('2023-04-04 01:02:03.004088+05:00' AS timestamp with time zone) AS timestamp_with_timezone_column,
                cast('2023-04-04 01:02:03.004088+05:00' AS timestamp(6) with time zone) AS timestamp_with_timezone_column_precision6,
                cast('2023-04-04 01:02:03.004+05:00' AS timestamp(3) with time zone) AS timestamp_with_timezone_column2,
                cast('2023-04-04 01:02:03.004 UTC' AS timestamp(3) with time zone) AS timestamp_with_timezone_column3,
                cast('2023-04-04 01:02:03.004567 UTC' AS timestamp(3) with time zone) AS timestamp_with_timezone_column4,
                cast('2023-04-04 01:02:03.2 UTC' AS timestamp(1) with time zone) AS timestamp_with_timezone_column5,
                cast('2023-04-04 01:02:03 UTC' AS timestamp(0) with time zone) AS timestamp_with_timezone_column6,
                cast(interval '3' year + interval '5' month AS ""INTERVAL YEAR TO MONTH"")
                AS interval_year_to_month_column,
                CAST(interval '2' day + interval '1' hour + interval '3' minute + interval '5' second AS ""INTERVAL DAY TO SECOND"") AS interval_day_to_second_column,
                From_base64('aGVsbG8=') AS varbinary_column,
                ARRAY['hello', 'world'] AS array_column,
                MAP(ARRAY['key1', 'key2'], ARRAY[1,2]) AS map_column,
                ARRAY[MAP(ARRAY['key1', 'key2'], ARRAY[1,2]), MAP(ARRAY['key3', 'key4'], ARRAY[5,6])] AS array_of_maps_column,
                CAST('431396d2-f305-4c88-b097-d0b5c9d4c0f6' AS uuid) AS uuid_column,
                CAST('2001:db8::8a2e:370:7334' AS IPADDRESS) ipaddress_column,
                approx_set(34983452343) AS hll_column,
                CAST(approx_set(34983452343) AS P4HyperLogLog) AS p4hll_column,
                make_set_digest(5) AS setdigest_column,
                tdigest_agg(6) AS tdigest_column
                ";

                TrinoConnectionProperties properties = server.GetConnectionProperties();

                using (TrinoConnection tc = new(properties))
                {
                    using (IDbCommand trinoCommand = new TrinoCommand(tc, all_types))
                    {
                        TrinoDataReader idr = (TrinoDataReader)trinoCommand.ExecuteReader();
                        Assert.AreEqual(idr.FieldCount, 35);
                        int rowCount = 0;
                        while (idr.Read())
                        {
                            Assert.AreEqual(0, rowCount); // should only be one row

                            DataTable? tableWithSchema = idr.GetSchemaTableTemplate();

                            Assert.IsNotNull(tableWithSchema);

                            Assert.AreEqual(35, idr.FieldCount);
                            Assert.AreEqual(null, idr.GetValue(0));
                            Assert.AreEqual("null_varchar_column", tableWithSchema.Columns[0].ToString());

                            Assert.AreEqual(typeof(Int64), idr.GetValue(1).GetType());
                            Assert.AreEqual(9223372036854775806, idr.GetValue(1));
                            Assert.AreEqual("big_int_column", tableWithSchema.Columns[1].ToString());

                            Assert.AreEqual(typeof(Int32), idr.GetValue(2).GetType());
                            Assert.AreEqual(2147483647, idr.GetValue(2));
                            Assert.AreEqual("int_column", tableWithSchema.Columns[2].ToString());

                            Assert.AreEqual(typeof(Int16), idr.GetValue(3).GetType());
                            Assert.AreEqual((short)32767, idr.GetValue(3));
                            Assert.AreEqual("small_int_column", tableWithSchema.Columns[3].ToString());

                            Assert.AreEqual(typeof(SByte), idr.GetValue(4).GetType());
                            Assert.AreEqual((sbyte)-127, idr.GetValue(4));
                            Assert.AreEqual("tiny_int_column", tableWithSchema.Columns[4].ToString());

                            Assert.AreEqual(typeof(float), idr.GetValue(5).GetType());
                            Assert.AreEqual((float)3.402823466E+38, idr.GetValue(5));
                            Assert.AreEqual("real_column", tableWithSchema.Columns[5].ToString());

                            Assert.AreEqual(typeof(double), idr.GetValue(6).GetType());
                            Assert.AreEqual(1.7976931348623158E+308, idr.GetValue(6));
                            Assert.AreEqual("double_column", tableWithSchema.Columns[6].ToString());

                            Assert.AreEqual(typeof(TrinoBigDecimal), idr.GetValue(7).GetType());
                            Assert.AreEqual(new TrinoBigDecimal("678.12345"), idr.GetValue(7));
                            Assert.AreEqual(new TrinoBigDecimal("678.12345").ToDecimal(), ((TrinoBigDecimal)idr.GetValue(7)).ToDecimal());
                            Assert.AreEqual("decimal_column", tableWithSchema.Columns[7].ToString());

                            Assert.AreEqual(typeof(TrinoBigDecimal), idr.GetValue(8).GetType());
                            Assert.AreEqual(new TrinoBigDecimal("123456789000000000.123400500099999999"), idr.GetValue(8));
                            Assert.AreEqual("big_decimal_column", tableWithSchema.Columns[8].ToString());

                            // Test big decimal extraction fails at this scale
                            Assert.ThrowsException<OverflowException>(() => ((TrinoBigDecimal)idr.GetValue(8)).ToDecimal());

                            Assert.AreEqual(typeof(Boolean), idr.GetValue(9).GetType());
                            Assert.AreEqual(true, idr.GetValue(9));
                            Assert.AreEqual("boolean_column", tableWithSchema.Columns[9].ToString());

                            Assert.AreEqual(typeof(Char[]), idr.GetValue(10).GetType());
                            Assert.AreEqual("0123456789", new string((Char[])idr.GetValue(10)));
                            Assert.AreEqual("char_column", tableWithSchema.Columns[10].ToString());

                            Assert.AreEqual(typeof(string), idr.GetValue(11).GetType());
                            Assert.AreEqual("0123456789abc", idr.GetValue(11));
                            Assert.AreEqual("varchar_column", tableWithSchema.Columns[11].ToString());

                            Assert.AreEqual(typeof(DateTime), idr.GetValue(12).GetType());
                            Assert.AreEqual(new DateTime(2022, 02, 22, 0, 0, 0), idr.GetValue(12));
                            Assert.AreEqual("date_column", tableWithSchema.Columns[12].ToString());

                            Assert.AreEqual(typeof(TimeSpan), idr.GetValue(13).GetType());
                            Assert.AreEqual(new TimeSpan(0, 12, 34, 56, 123), idr.GetValue(13));
                            Assert.AreEqual("time_column", tableWithSchema.Columns[13].ToString());

                            Assert.AreEqual(typeof(string), idr.GetValue(14).GetType());
                            Assert.AreEqual("01:02:03.004+05:00", idr.GetValue(14));
                            Assert.AreEqual("timewithtimezone_column", tableWithSchema.Columns[14].ToString());

                            Assert.AreEqual(typeof(DateTime), idr.GetValue(15).GetType());
                            Assert.AreEqual(new DateTime(2022, 02, 22, 12, 34, 56, 004), idr.GetValue(15));
                            Assert.AreEqual("timestamp_column", tableWithSchema.Columns[15].ToString());

                            Assert.AreEqual(typeof(DateTimeOffset), idr.GetValue(16).GetType());
                            Assert.AreEqual(DateTimeOffset.Parse("2023-04-04 01:02:03.004+05:00"), idr.GetValue(16));
                            Assert.AreEqual("timestamp_with_timezone_column", tableWithSchema.Columns[16].ToString());

                            Assert.AreEqual(typeof(DateTimeOffset), idr.GetValue(17).GetType());
                            Assert.AreEqual(DateTimeOffset.Parse("2023-04-04 01:02:03.004088+05:00"), idr.GetValue(17));
                            Assert.AreEqual("timestamp_with_timezone_column_precision6", tableWithSchema.Columns[17].ToString());

                            Assert.AreEqual(typeof(DateTimeOffset), idr.GetValue(18).GetType());
                            Assert.AreEqual(DateTimeOffset.Parse("2023-04-04 01:02:03.004+05:00"), idr.GetValue(18));
                            Assert.AreEqual("timestamp_with_timezone_column2", tableWithSchema.Columns[18].ToString());

                            Assert.AreEqual(typeof(DateTimeOffset), idr.GetValue(19).GetType());
                            Assert.AreEqual(DateTimeOffset.Parse("2023-04-04 01:02:03.004+00:00"), idr.GetValue(19));
                            Assert.AreEqual("timestamp_with_timezone_column3", tableWithSchema.Columns[19].ToString());

                            Assert.AreEqual(typeof(DateTimeOffset), idr.GetValue(20).GetType());
                            // expect 2023-04-04 01:02:03.004567 UTC to be rounded to .005
                            Assert.AreEqual(DateTimeOffset.Parse("2023-04-04 01:02:03.005+00:00"), idr.GetValue(20));
                            Assert.AreEqual("timestamp_with_timezone_column4", tableWithSchema.Columns[20].ToString());

                            Assert.AreEqual(typeof(DateTimeOffset), idr.GetValue(21).GetType());
                            Assert.AreEqual(DateTimeOffset.Parse("2023-04-04 01:02:03.2 +00:00"), idr.GetValue(21));
                            Assert.AreEqual("timestamp_with_timezone_column5", tableWithSchema.Columns[21].ToString());

                            Assert.AreEqual(typeof(DateTimeOffset), idr.GetValue(21).GetType());
                            Assert.AreEqual(DateTimeOffset.Parse("2023-04-04 01:02:03.2 +00:00"), idr.GetValue(21));
                            Assert.AreEqual("timestamp_with_timezone_column5", tableWithSchema.Columns[21].ToString());

                            Assert.AreEqual(typeof(DateTimeOffset), idr.GetValue(22).GetType());
                            Assert.AreEqual(DateTimeOffset.Parse("2023-04-04 01:02:03 +00:00"), idr.GetValue(22));
                            Assert.AreEqual("timestamp_with_timezone_column6", tableWithSchema.Columns[22].ToString());

                            Assert.AreEqual(typeof(DateTime), idr.GetValue(23).GetType());
                            Assert.AreEqual(new DateTime(3, 5, 1), idr.GetValue(23));
                            Assert.AreEqual("interval_year_to_month_column", tableWithSchema.Columns[23].ToString());

                            Assert.AreEqual(typeof(TimeSpan), idr.GetValue(24).GetType());
                            Assert.AreEqual(new TimeSpan(2, 1, 3, 5), idr.GetValue(24));
                            Assert.AreEqual("interval_day_to_second_column", tableWithSchema.Columns[24].ToString());

                            Assert.AreEqual(typeof(byte[]), idr.GetValue(25).GetType());
                            CompareBytes(Convert.FromBase64String("aGVsbG8="), (byte[])idr.GetValue(25));
                            Assert.AreEqual("varbinary_column", tableWithSchema.Columns[25].ToString());

                            Assert.AreEqual(typeof(List<System.Object>), idr.GetValue(26).GetType());
                            Assert.IsTrue(((List<object>)idr.GetValue(26)).SequenceEqual(["hello", "world"]));
                            Assert.AreEqual("array_column", tableWithSchema.Columns[26].ToString());

                            Assert.AreEqual(typeof(Dictionary<System.Object, System.Object>), idr.GetValue(27).GetType());
                            Dictionary<object, object> staticDictionary = new() { { "key1", 1 }, { "key2", 2 } };
                            Assert.IsTrue(((Dictionary<object, object>)idr.GetValue(27)).Keys.SequenceEqual(staticDictionary.Keys));
                            Assert.AreEqual("map_column", tableWithSchema.Columns[27].ToString());

                            rowCount++;
                        }
                    }
                }
            }
        }

        private static void CompareBytes(byte[] bytes, byte[] bytes_from_trino)
        {
            // compare byte arrays
            Assert.AreEqual(bytes.Length, bytes_from_trino.Length);
            for (int i = 0; i < bytes.Length; i++)
            {
                Assert.AreEqual(bytes[i], bytes_from_trino[i]);
            }
        }
    }
}

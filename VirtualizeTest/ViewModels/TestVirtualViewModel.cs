using Dapper;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.Data.SQLite;
using System.Diagnostics;
using System.Transactions;
using VirtualizeTest.Helpers;
using VirtualizeTest.Models;

namespace VirtualizeTest.ViewModels
{
    public class TestVirtualViewModel : BaseViewModel
    {
        private readonly ILogger<TestVirtualViewModel> _logger;

        #region Properties
        private bool loading = true;
        public bool Loading
        {
            get => loading;
            private set
            {
                loading = value;
                OnPropertyChanged(nameof(Loading));
            }
        }
        public int selectedId;

        public Virtualize<WeatherForecastTest>? TestContainer = new();

        private async Task ReloadData()
        {
            Trace.WriteLine("Call RefreshDataAsync()");

            await TestContainer?.RefreshDataAsync();
        }
        #endregion

        #region Constructor
        public TestVirtualViewModel(ILogger<TestVirtualViewModel> logger)
        {
            _logger = logger;

            Task.Run(() => Init());
        }
        #endregion

        #region Methods
        private async Task Init()
        {
            await Task.Delay(0);

            if (!File.Exists(Constant.sqlfile))
            {
                Miscellaneous.CreateDatabase();
                Miscellaneous.InsertTestData();
            }

            Loading = false;
        }

        public async ValueTask<ItemsProviderResult<WeatherForecastTest>> LoadTestData(ItemsProviderRequest request)
        {
            await Task.Delay(0);

            Trace.WriteLine("Calling LoadTestData()");

            int numDriverItems = Math.Min(request.Count, GetCountFromSql() - request.StartIndex);

            Trace.WriteLine(string.Format("Skip <{0}> Take <{1}>", request.StartIndex.ToString("N0"), numDriverItems.ToString("N0")));

            List<WeatherForecastTest> results = new();

            results = await GetWithYieldAsync();

            async Task<List<WeatherForecastTest>> GetWithYieldAsync()
            {
                await foreach (var item in GetWeatherDataAsync(request.StartIndex, numDriverItems))
                {
                    results.Add(item);
                    Trace.WriteLine("LoadTestData " + item.Id);
                }

                return results;
            }

            return new ItemsProviderResult<WeatherForecastTest>(results, GetCountFromSql());
        }

        private async IAsyncEnumerable<WeatherForecastTest> GetWeatherDataAsync(int startIndex, int numDriverItems)
        {
            if (File.Exists(Constant.sqlfile))
            {
                using (SQLiteConnection conn = new SQLiteConnection(Constant.sqlconnection))
                {
                    conn.Open();

                    string query = string.Format("SELECT Id, Date, TemperatureC, Summary FROM Test ORDER BY Id DESC LIMIT {0} OFFSET {1}", numDriverItems, startIndex);

                    Trace.WriteLine($"Sql query: {query}");

                    using (var reader = await conn.ExecuteReaderAsync(query))
                    {
                        var rowParser = reader.GetRowParser<WeatherForecastTest>();

                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            yield return rowParser(reader);
                        }
                    }
                }
            }
        }

        public int GetCountFromSql()
        {
            int count = 0;

            if (File.Exists(Constant.sqlfile))
            {
                try
                {
                    using (SQLiteConnection conn = new SQLiteConnection(Constant.sqlconnection))
                    {
                        conn.Open();

                        string query = "SELECT COUNT(*) as Count FROM Test";

                        count = conn.Query<int>(query).First();

                        Trace.WriteLine(string.Format("Total row count {0}", count));
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }

            return count;
        }

        public void OnSelectedId(int id)
        {
            Trace.WriteLine($"Id {id} has been selected");

            selectedId = id;
        }

        public void ActionStateDelete(bool state)
        {
            if (state)
            {
                Trace.WriteLine($"Delete id {selectedId}");

                DeleteIdInDatabase(selectedId);

                _ = ReloadData();
            }
        }

        private static void DeleteIdInDatabase(int selectedId)
        {
            if (File.Exists(Constant.sqlfile))
            {
                try
                {
                    using (SQLiteConnection conn = new SQLiteConnection(Constant.sqlconnection))
                    {
                        conn.Open();

                        string query = "DELETE FROM Test WHERE Id = @Id;";

                        Trace.WriteLine($"Sql query: DELETE FROM Test WHERE Id = {selectedId};");

                        var result = conn.Execute(query, new { Id = selectedId });

                        Trace.WriteLine($"Id {selectedId} deleted from database");
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }
        }
        #endregion
    }
}

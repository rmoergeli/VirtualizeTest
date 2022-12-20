using Dapper;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.Data.SQLite;
using System.Diagnostics;
using VirtualizeTest.Helpers;
using VirtualizeTest.Models;

namespace VirtualizeTest.ViewModels
{
    public class TestViewModel : BaseViewModel
    {
        private readonly ILogger<TestViewModel> _logger;

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

        private List<WeatherForecastTest> weatherData;
        public List<WeatherForecastTest> WeatherData
        {
            get => weatherData;
            private set
            {
                weatherData = value;
                OnPropertyChanged(nameof(WeatherData));
            }
        }

        public int selectedId;
        #endregion

        #region Constructor
        public TestViewModel(ILogger<TestViewModel> logger)
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

            WeatherData = GetWeatherData();

            Loading = false;
        }

        private List<WeatherForecastTest> GetWeatherData()
        {
            List<WeatherForecastTest> weatherData = new();

            if (File.Exists(Constant.sqlfile))
            {
                using (SQLiteConnection conn = new SQLiteConnection(Constant.sqlconnection))
                {
                    conn.Open();

                    string query = string.Format("SELECT Id, Date, TemperatureC, Summary FROM Test ORDER BY Id DESC;");

                    Trace.WriteLine($"Sql query: {query}");

                    weatherData = conn.Query<WeatherForecastTest>(query).ToList();
                }
            }

            return weatherData;
        }


        public void ActionStateDelete(bool state)
        {
            if (state)
            {
                Trace.WriteLine($"Delete id {selectedId}");

                WeatherData = WeatherData.Where(o => o.Id != selectedId).ToList();             
            }
        }

        public void OnSelectedId(int id)
        {
            Trace.WriteLine($"Id {id} has been selected");

            selectedId = id;
        }
        #endregion
    }
}

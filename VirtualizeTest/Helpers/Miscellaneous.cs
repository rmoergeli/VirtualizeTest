using Dapper;
using System.Data.SQLite;
using System.Diagnostics;
using VirtualizeTest.Models;

namespace VirtualizeTest.Helpers
{
    public class Miscellaneous
    {
        internal static void CreateDatabase()
        {
            if (!File.Exists(Constant.sqlfile))
            {
                try
                {
                    using (SQLiteConnection conn = new SQLiteConnection(Constant.sqlconnection + ";new=true;"))
                    {
                        try
                        {
                            conn.Open();

                            var command = conn.CreateCommand();

                            command.CommandText = "CREATE TABLE Test (Id integer PRIMARY KEY AUTOINCREMENT, Date text, TemperatureC integer, Summary text);";
                            command.ExecuteNonQuery();

                            conn.Close();
                        }
                        catch (SQLiteException ex)
                        {
                            Trace.WriteLine($"{ex}");
                        }
                    }
                }
                catch (SQLiteException ex)
                {
                    Trace.WriteLine($"SQLiteException {ex.Message}");
                }
            }
        }

        internal static void InsertTestData()
        {
            if (File.Exists(Constant.sqlfile))
            {
                try
                {
                    using (SQLiteConnection conn = new SQLiteConnection(Constant.sqlconnection))
                    {
                        conn.Open();

                        WeatherForecastTest[] weatherForecasts = GetForecast(DateOnly.FromDateTime(DateTime.Now));

                        var data = weatherForecasts.Select(o => new { Date = o.Date.ToString("yyyy-MM-dd"), TemperatureC = o.TemperatureC, Summary = o.Summary }).ToList();

                        string query = "INSERT INTO Test (Date, TemperatureC, Summary) VALUES (@Date, @TemperatureC, @Summary);";
                        using (var transaction = conn.BeginTransaction())
                        {
                            var result = conn.Execute(query, data, transaction: transaction);

                            transaction.Commit();
                        }

                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }
        }


        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private static WeatherForecastTest[] GetForecast(DateOnly startDate)
        {
            DateTime startDateTime = startDate.ToDateTime(TimeOnly.Parse("12:00 PM"));

            return Enumerable.Range(1, 1000).Select(index => new WeatherForecastTest
            {
                Date = startDateTime.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();
        }
    }
}

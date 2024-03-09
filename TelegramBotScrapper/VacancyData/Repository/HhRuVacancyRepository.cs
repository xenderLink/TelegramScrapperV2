using Dapper;
using TelegramBotScrapper.VacancyData.Model;
using System.Data;
using System.Data.SQLite;
using System.Collections.ObjectModel;

namespace TelegramBotScrapper.VacancyData.Repository;

public sealed class HhRuVacancyRepository : IHhRuVacancyRepository
{
    private const string TableName = "vacancies";
    private readonly string _connectionString;

    public HhRuVacancyRepository(string connectionString)
    {
        _connectionString = connectionString;
        InitTable();
    }

    public async Task AddVacancies(HashSet<Vacancy> vacancies, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = new SQLiteConnection(_connectionString);
        connection.Open();
        using IDbTransaction transaction = connection.BeginTransaction();

        #region queries

        var deleteSql = $@"
                          DELETE FROM {TableName}
                          WHERE service = 'hh.ru' 
                            AND date < strftime('%Y/%m/%d', datetime('now', '-1 month'));";
        
        var sqlIds = $@"SELECT vacancy_id FROM {TableName} WHERE service = 'hh.ru';";

        var sql = $@"
                    INSERT INTO {TableName} 
                    (vacancy_id, name, url, city, date, service)
                    VALUES(@vacancyId, @Name, @Url, @City, @Date, 'hh.ru');";
                    
        #endregion
        
        var deleteResult = connection.ExecuteAsync(deleteSql, cancellationToken);

        var selectResult = connection
            .QueryAsync<string>(sqlIds, cancellationToken);

        await deleteResult;
        var vacIds = await selectResult;

        var vacs = vacancies.Where(vac => !vacIds.Contains(vac.vacancyId)).ToList();
        
        try 
        {
            if (vacs.Any())
            {
                await connection.ExecuteAsync(sql, vacs, transaction);
                transaction.Commit();
            }
        }
        catch
        {
            transaction.Rollback();
        }
    }

    public async Task<IReadOnlyList<(string Name, string Url)>> GetVacancies(string city, CancellationToken cancellationToken = default)
    {
        try
        {
            using IDbConnection connection = new SQLiteConnection(_connectionString);

            var sql = $@"
                        SELECT name, url FROM {TableName}
                        WHERE city = '{city}' AND service = 'hh.ru'
                        ORDER BY date DESC";

            var result = await connection.QueryAsync<(string Name, string Url)>(sql, cancellationToken);

            if (!result.Any())
            {
                return ReadOnlyCollection<(string Name, string Url)>.Empty;
            }

            return result.ToList();
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            return ReadOnlyCollection<(string Name, string Url)>.Empty;
        }
    }

    private void InitTable()
    {
        using IDbConnection connection = new SQLiteConnection(_connectionString);

        string sql = @$"
                       CREATE TABLE IF NOT EXISTS  
                       {TableName} (
                           id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, 
                           vacancy_id varchar(35) NOT NULL,
                           name TEXT NOT NULL,
                           url TEXT NOT NULL,
                           city varchar(100) NOT NULL,
                           date TEXT NOT NULL,
                           service TEXT NOT NULL
                       );";

        connection.Execute(sql);
    }
}
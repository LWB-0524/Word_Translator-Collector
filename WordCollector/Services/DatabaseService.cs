using System.IO;
using Microsoft.Data.Sqlite;
using WordCollector.Models;
using WordCollector.Helpers;

namespace WordCollector.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService() : this(GetDefaultDatabasePath())
    {
    }

    internal DatabaseService(string dbPath)
    {
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        InitializeDatabase();
    }

    private static string GetDefaultDatabasePath()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WordCollector");
        Directory.CreateDirectory(appData);
        return Path.Combine(appData, "words.db");
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS vocabulary_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                text TEXT NOT NULL,
                normalized_text TEXT,
                item_type TEXT,
                meaning_zh TEXT NOT NULL,
                brief_explanation TEXT,
                detailed_explanation TEXT,
                example_en TEXT,
                example_zh TEXT,
                key_expressions_json TEXT,
                raw_ai_response TEXT,
                date_added TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT,
                lookup_count INTEGER DEFAULT 1,
                familiarity INTEGER DEFAULT 0,
                spoken_count INTEGER DEFAULT 0,
                last_spoken_at TEXT
            );
        ";

        using var command = new SqliteCommand(createTableSql, connection);
        command.ExecuteNonQuery();

        // Column migrations for databases created by earlier versions.
        TryAddColumn(connection, "phonetic TEXT");
        TryAddColumn(connection, "review_repetitions INTEGER DEFAULT 0");
        TryAddColumn(connection, "review_interval_days INTEGER DEFAULT 0");
        TryAddColumn(connection, "review_ease_factor REAL DEFAULT 2.5");
        TryAddColumn(connection, "next_review_date TEXT");
        TryAddColumn(connection, "last_reviewed_at TEXT");

        // Create index for dedup lookups
        const string createIndexSql = @"
            CREATE INDEX IF NOT EXISTS idx_normalized_date
            ON vocabulary_items(normalized_text, date_added);
        ";
        using var idxCommand = new SqliteCommand(createIndexSql, connection);
        idxCommand.ExecuteNonQuery();

        // Index to speed up the "due for review" query.
        using var reviewIdxCommand = new SqliteCommand(
            "CREATE INDEX IF NOT EXISTS idx_next_review ON vocabulary_items(next_review_date);",
            connection);
        reviewIdxCommand.ExecuteNonQuery();
    }

    private static void TryAddColumn(SqliteConnection connection, string columnDefinition)
    {
        try
        {
            using var command = new SqliteCommand(
                $"ALTER TABLE vocabulary_items ADD COLUMN {columnDefinition};", connection);
            command.ExecuteNonQuery();
        }
        catch
        {
            // Column already exists — ignore.
        }
    }

    public VocabularyItem? FindByNormalizedTextAndDate(string normalizedText, string date)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            const string sql = @"
                SELECT * FROM vocabulary_items
                WHERE normalized_text = @nt AND date_added = @date
                LIMIT 1;
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@nt", normalizedText);
            command.Parameters.AddWithValue("@date", date);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return ReadVocabularyItem(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB FindByNormalizedTextAndDate error: {ex.Message}");
            return null;
        }
    }

    public VocabularyItem? FindByNormalizedText(string normalizedText)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            const string sql = @"
                SELECT * FROM vocabulary_items
                WHERE normalized_text = @nt
                ORDER BY date_added DESC
                LIMIT 1;
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@nt", normalizedText);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return ReadVocabularyItem(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB FindByNormalizedText error: {ex.Message}");
            return null;
        }
    }

    public long Insert(VocabularyItem item)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string sql = @"
            INSERT INTO vocabulary_items
            (text, normalized_text, item_type, phonetic, meaning_zh, brief_explanation,
             detailed_explanation, example_en, example_zh, key_expressions_json,
             raw_ai_response, date_added, created_at, updated_at, lookup_count,
             familiarity, spoken_count, last_spoken_at,
             review_repetitions, review_interval_days, review_ease_factor,
             next_review_date, last_reviewed_at)
            VALUES
            (@text, @normalized_text, @item_type, @phonetic, @meaning_zh, @brief_explanation,
             @detailed_explanation, @example_en, @example_zh, @key_expressions_json,
             @raw_ai_response, @date_added, @created_at, @updated_at, @lookup_count,
             @familiarity, @spoken_count, @last_spoken_at,
             @review_repetitions, @review_interval_days, @review_ease_factor,
             @next_review_date, @last_reviewed_at);
            SELECT last_insert_rowid();
        ";

        using var command = new SqliteCommand(sql, connection);
        AddParameters(command, item);
        return (long)command.ExecuteScalar()!;
    }

    public void UpdateLookupCount(long id, int newCount)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string sql = @"
            UPDATE vocabulary_items
            SET lookup_count = @count, updated_at = @updated_at
            WHERE id = @id;
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@count", newCount);
        command.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    public void UpdateSpokenCount(long id, int newCount)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string sql = @"
            UPDATE vocabulary_items
            SET spoken_count = @count, last_spoken_at = @last_spoken
            WHERE id = @id;
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@count", newCount);
        command.Parameters.AddWithValue("@last_spoken", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    public void Update(VocabularyItem item)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string sql = @"
            UPDATE vocabulary_items
            SET text = @text, normalized_text = @normalized_text,
                meaning_zh = @meaning_zh, brief_explanation = @brief_explanation,
                detailed_explanation = @detailed_explanation, example_en = @example_en,
                example_zh = @example_zh, familiarity = @familiarity,
                updated_at = @updated_at
            WHERE id = @id;
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@text", item.Text);
        command.Parameters.AddWithValue("@normalized_text", item.NormalizedText ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@meaning_zh", item.MeaningZh);
        command.Parameters.AddWithValue("@brief_explanation", item.BriefExplanation ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@detailed_explanation", item.DetailedExplanation ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@example_en", item.ExampleEn ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@example_zh", item.ExampleZh ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@familiarity", item.Familiarity);
        command.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@id", item.Id);
        command.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string sql = "DELETE FROM vocabulary_items WHERE id = @id;";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    public List<VocabularyItem> GetByDate(string date)
    {
        var items = new List<VocabularyItem>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            const string sql = @"
                SELECT * FROM vocabulary_items
                WHERE date_added = @date
                ORDER BY created_at DESC;
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@date", date);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                items.Add(ReadVocabularyItem(reader));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB GetByDate error: {ex.Message}");
        }

        return items;
    }

    public List<VocabularyItem> Search(string? searchText, string? dateFrom, string? dateTo,
        string? itemType, int? familiarity, int offset = 0, int limit = 100)
    {
        var items = new List<VocabularyItem>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var conditions = new List<string>();
            var parameters = new List<SqliteParameter>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var pattern = $"%{EscapeLikePattern(searchText)}%";
                conditions.Add(@"(text LIKE @search ESCAPE '\' OR meaning_zh LIKE @search2 ESCAPE '\')");
                parameters.Add(new SqliteParameter("@search", pattern));
                parameters.Add(new SqliteParameter("@search2", pattern));
            }

            if (!string.IsNullOrWhiteSpace(dateFrom))
            {
                conditions.Add("date_added >= @dateFrom");
                parameters.Add(new SqliteParameter("@dateFrom", dateFrom));
            }

            if (!string.IsNullOrWhiteSpace(dateTo))
            {
                conditions.Add("date_added <= @dateTo");
                parameters.Add(new SqliteParameter("@dateTo", dateTo));
            }

            if (!string.IsNullOrWhiteSpace(itemType))
            {
                conditions.Add("item_type = @itemType");
                parameters.Add(new SqliteParameter("@itemType", itemType));
            }

            if (familiarity.HasValue)
            {
                conditions.Add("familiarity = @familiarity");
                parameters.Add(new SqliteParameter("@familiarity", familiarity.Value));
            }

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : "";

            var sql = $@"
                SELECT * FROM vocabulary_items
                {whereClause}
                ORDER BY date_added DESC, created_at DESC
                LIMIT @limit OFFSET @offset;
            ";

            using var command = new SqliteCommand(sql, connection);
            foreach (var p in parameters)
                command.Parameters.Add(p);
            command.Parameters.AddWithValue("@limit", limit);
            command.Parameters.AddWithValue("@offset", offset);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                items.Add(ReadVocabularyItem(reader));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB Search error: {ex.Message}");
        }

        return items;
    }

    public List<string> GetDistinctDates()
    {
        var dates = new List<string>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            const string sql = @"
                SELECT DISTINCT date_added FROM vocabulary_items
                ORDER BY date_added DESC
                LIMIT 365;
            ";

            using var command = new SqliteCommand(sql, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                dates.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB GetDistinctDates error: {ex.Message}");
        }

        return dates;
    }

    public VocabularyItem? GetById(long id)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            const string sql = "SELECT * FROM vocabulary_items WHERE id = @id;";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return ReadVocabularyItem(reader);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB GetById error: {ex.Message}");
        }

        return null;
    }

    /// <summary>返回到期（next_review_date 为空或不晚于 today）的词条，用于间隔重复复习。</summary>
    public List<VocabularyItem> GetDueForReview(string today, int limit = 100)
    {
        var items = new List<VocabularyItem>();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            const string sql = @"
                SELECT * FROM vocabulary_items
                WHERE next_review_date IS NULL OR next_review_date <= @today
                ORDER BY (next_review_date IS NULL) DESC, next_review_date ASC, date_added ASC
                LIMIT @limit;
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@today", today);
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
                items.Add(ReadVocabularyItem(reader));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB GetDueForReview error: {ex.Message}");
        }

        return items;
    }

    public int GetDueCount(string today)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            const string sql = @"
                SELECT COUNT(*) FROM vocabulary_items
                WHERE next_review_date IS NULL OR next_review_date <= @today;
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@today", today);
            return Convert.ToInt32(command.ExecuteScalar());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB GetDueCount error: {ex.Message}");
            return 0;
        }
    }

    public void UpdateReviewState(VocabularyItem item)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        const string sql = @"
            UPDATE vocabulary_items
            SET review_repetitions = @reps, review_interval_days = @interval,
                review_ease_factor = @ease, next_review_date = @next,
                last_reviewed_at = @last, familiarity = @familiarity
            WHERE id = @id;
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@reps", item.ReviewRepetitions);
        command.Parameters.AddWithValue("@interval", item.ReviewIntervalDays);
        command.Parameters.AddWithValue("@ease", item.ReviewEaseFactor);
        command.Parameters.AddWithValue("@next", item.NextReviewDate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@last", item.LastReviewedAt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@familiarity", item.Familiarity);
        command.Parameters.AddWithValue("@id", item.Id);
        command.ExecuteNonQuery();
    }

    /// <summary>汇总词库统计信息（用于统计面板）。</summary>
    public VocabularyStatistics GetStatistics(string today)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            const string aggregateSql = @"
                SELECT
                    COUNT(*),
                    COALESCE(SUM(CASE WHEN item_type = 'word' THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN item_type = 'phrase' THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN item_type = 'sentence' THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN familiarity = 0 THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN familiarity = 1 THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN familiarity = 2 THEN 1 ELSE 0 END), 0),
                    COALESCE(SUM(lookup_count), 0),
                    COALESCE(SUM(spoken_count), 0)
                FROM vocabulary_items;
            ";

            int total = 0, words = 0, phrases = 0, sentences = 0;
            int fresh = 0, learning = 0, mastered = 0, lookups = 0, spoken = 0;

            using (var command = new SqliteCommand(aggregateSql, connection))
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    total = reader.GetInt32(0);
                    words = reader.GetInt32(1);
                    phrases = reader.GetInt32(2);
                    sentences = reader.GetInt32(3);
                    fresh = reader.GetInt32(4);
                    learning = reader.GetInt32(5);
                    mastered = reader.GetInt32(6);
                    lookups = reader.GetInt32(7);
                    spoken = reader.GetInt32(8);
                }
            }

            var dates = new List<string>();
            using (var datesCommand = new SqliteCommand(
                "SELECT DISTINCT date_added FROM vocabulary_items;", connection))
            using (var reader = datesCommand.ExecuteReader())
            {
                while (reader.Read())
                    dates.Add(reader.GetString(0));
            }

            int due;
            using (var dueCommand = new SqliteCommand(
                "SELECT COUNT(*) FROM vocabulary_items WHERE next_review_date IS NULL OR next_review_date <= @today;",
                connection))
            {
                dueCommand.Parameters.AddWithValue("@today", today);
                due = Convert.ToInt32(dueCommand.ExecuteScalar());
            }

            var streak = StatisticsCalculator.CalculateStreak(dates, DateTime.Now);

            return new VocabularyStatistics(
                total, words, phrases, sentences,
                fresh, learning, mastered, lookups, spoken,
                dates.Count, streak, due);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DB GetStatistics error: {ex.Message}");
            return VocabularyStatistics.Empty;
        }
    }

    private static VocabularyItem ReadVocabularyItem(SqliteDataReader reader)
    {
        return new VocabularyItem
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Text = reader.GetString(reader.GetOrdinal("text")),
            NormalizedText = reader.IsDBNull(reader.GetOrdinal("normalized_text")) ? null : reader.GetString(reader.GetOrdinal("normalized_text")),
            ItemType = reader.IsDBNull(reader.GetOrdinal("item_type")) ? null : reader.GetString(reader.GetOrdinal("item_type")),
            Phonetic = TryGetString(reader, "phonetic"),
            MeaningZh = reader.GetString(reader.GetOrdinal("meaning_zh")),
            BriefExplanation = reader.IsDBNull(reader.GetOrdinal("brief_explanation")) ? null : reader.GetString(reader.GetOrdinal("brief_explanation")),
            DetailedExplanation = reader.IsDBNull(reader.GetOrdinal("detailed_explanation")) ? null : reader.GetString(reader.GetOrdinal("detailed_explanation")),
            ExampleEn = reader.IsDBNull(reader.GetOrdinal("example_en")) ? null : reader.GetString(reader.GetOrdinal("example_en")),
            ExampleZh = reader.IsDBNull(reader.GetOrdinal("example_zh")) ? null : reader.GetString(reader.GetOrdinal("example_zh")),
            KeyExpressionsJson = reader.IsDBNull(reader.GetOrdinal("key_expressions_json")) ? null : reader.GetString(reader.GetOrdinal("key_expressions_json")),
            RawAiResponse = reader.IsDBNull(reader.GetOrdinal("raw_ai_response")) ? null : reader.GetString(reader.GetOrdinal("raw_ai_response")),
            DateAdded = reader.GetString(reader.GetOrdinal("date_added")),
            CreatedAt = reader.GetString(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetString(reader.GetOrdinal("updated_at")),
            LookupCount = reader.GetInt32(reader.GetOrdinal("lookup_count")),
            Familiarity = reader.GetInt32(reader.GetOrdinal("familiarity")),
            SpokenCount = reader.GetInt32(reader.GetOrdinal("spoken_count")),
            LastSpokenAt = reader.IsDBNull(reader.GetOrdinal("last_spoken_at")) ? null : reader.GetString(reader.GetOrdinal("last_spoken_at")),
            ReviewRepetitions = TryGetInt(reader, "review_repetitions", 0),
            ReviewIntervalDays = TryGetInt(reader, "review_interval_days", 0),
            ReviewEaseFactor = TryGetDouble(reader, "review_ease_factor", 2.5),
            NextReviewDate = TryGetString(reader, "next_review_date"),
            LastReviewedAt = TryGetString(reader, "last_reviewed_at")
        };
    }

    private static int TryGetInt(SqliteDataReader reader, string column, int fallback)
    {
        try
        {
            var idx = reader.GetOrdinal(column);
            return reader.IsDBNull(idx) ? fallback : reader.GetInt32(idx);
        }
        catch { return fallback; }
    }

    private static double TryGetDouble(SqliteDataReader reader, string column, double fallback)
    {
        try
        {
            var idx = reader.GetOrdinal(column);
            return reader.IsDBNull(idx) ? fallback : reader.GetDouble(idx);
        }
        catch { return fallback; }
    }

    private static string EscapeLikePattern(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static string? TryGetString(SqliteDataReader reader, string column)
    {
        try
        {
            var idx = reader.GetOrdinal(column);
            return reader.IsDBNull(idx) ? null : reader.GetString(idx);
        }
        catch { return null; }
    }

    private static void AddParameters(SqliteCommand command, VocabularyItem item)
    {
        command.Parameters.AddWithValue("@text", item.Text);
        command.Parameters.AddWithValue("@phonetic", item.Phonetic ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@normalized_text", item.NormalizedText ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@item_type", item.ItemType ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@meaning_zh", item.MeaningZh);
        command.Parameters.AddWithValue("@brief_explanation", item.BriefExplanation ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@detailed_explanation", item.DetailedExplanation ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@example_en", item.ExampleEn ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@example_zh", item.ExampleZh ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@key_expressions_json", item.KeyExpressionsJson ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@raw_ai_response", item.RawAiResponse ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@date_added", item.DateAdded);
        command.Parameters.AddWithValue("@created_at", item.CreatedAt);
        command.Parameters.AddWithValue("@updated_at", item.UpdatedAt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@lookup_count", item.LookupCount);
        command.Parameters.AddWithValue("@familiarity", item.Familiarity);
        command.Parameters.AddWithValue("@spoken_count", item.SpokenCount);
        command.Parameters.AddWithValue("@last_spoken_at", item.LastSpokenAt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@review_repetitions", item.ReviewRepetitions);
        command.Parameters.AddWithValue("@review_interval_days", item.ReviewIntervalDays);
        command.Parameters.AddWithValue("@review_ease_factor", item.ReviewEaseFactor);
        command.Parameters.AddWithValue("@next_review_date", item.NextReviewDate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@last_reviewed_at", item.LastReviewedAt ?? (object)DBNull.Value);
    }
}

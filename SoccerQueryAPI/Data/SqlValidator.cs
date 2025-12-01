using System.Text.RegularExpressions;

namespace SoccerQueryAPI.Data
{
    public class SqlValidator
    {

        private readonly HashSet<string> _allowedTables;
        private readonly HashSet<string> _allowedColumns;
        private readonly int _maxRows;

        public SqlValidator(IConfiguration config)
        {
            _allowedTables = config.GetSection("AllowedSql:Tables").Get<string[]>()?.Select(x => x.ToLowerInvariant()).ToHashSet() ?? new();
            _allowedColumns = config.GetSection("AllowedSql:Columns").Get<string[]>()?.Select(x => x.ToLowerInvariant()).ToHashSet() ?? new();
            _maxRows = config.GetValue<int>("SqlExecution:MaxRows", 1000);
        }

        public bool IsSelectOnly(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return false; //extra check 
            var trimmed = sql.Trim();

            // Only allow statements that start with SELECT 
            return Regex.IsMatch(trimmed, @"^\s*SELECT\s", RegexOptions.IgnoreCase);
        }

        public bool ContainsOnlyAllowedTablesAndColumns(string sql, out string reason)
        {
            reason = string.Empty;
            var lowered = sql.ToLowerInvariant();


            string[] forbidden = { "insert ", "update ", "delete ", "drop ", "alter ", "create ", "attach ", "pragma ", ";" };
            foreach (var f in forbidden)
            {
                // allow semicolon only at end? For safety, forbid semicolon entirely here
                if (lowered.Contains(f)) { reason = $"Forbidden token detected: {f.Trim()}"; return false; }
            }


            //string[] forbidden = { "insert ", "update ", "delete ", "drop ", "alter ", "create ", "attach ", "pragma " };

            //foreach (var f in forbidden)
            //{
            //    if (lowered.Contains(f))
            //    {
            //        reason = $"Forbidden token detected: {f.Trim()}";
            //        return false;
            //    }
            //}

            //// Allow a single semicolon only at the end 
            //var trimmed = lowered.TrimEnd();
            //if (trimmed.Contains(";"))
            //{
            //    if (!trimmed.EndsWith(";"))
            //    {
            //        reason = "Semicolon found in middle of query — possible injection attempt.";
            //        return false;
            //    }
            //}

            // Extract identifiers roughly — this is heuristic: find words separated by whitespace, periods, commas, parentheses.
            var tokens = Regex.Matches(lowered, @"[a-zA-Z_][a-zA-Z0-9_]*")
                              .Select(m => m.Value)
                              .Distinct();

            // Check for at least one allowed table
            if (!tokens.Any(t => _allowedTables.Contains(t)))
            {
                reason = "No allowed table referenced in the SQL.";
                return false;
            }

            // Ensure all column-like tokens are allowed OR are SQL keywords (simple whitelist of SQL keywords)
            var sqlKeywords = new HashSet<string>(new[]
            {
                "select","from","where","join","inner","left","right","on","group","by","order","having","limit","as","and","or","count","avg","sum","min","max"
            });

            foreach (var t in tokens)
            {
                if (sqlKeywords.Contains(t)) continue;
                if (_allowedTables.Contains(t)) continue;
                if (_allowedColumns.Contains(t)) continue;
                // allow functions like date, datetime etc. (basic)
                if (t.Length > 0 && char.IsLetter(t[0]) && t.Length <= 10) continue;

            }

            return true;
        }

        public string EnforceRowLimit(string sql)
        {

            if (Regex.IsMatch(sql, @"\blimit\b", RegexOptions.IgnoreCase))
                return sql;
            return $"{sql.Trim().TrimEnd(';')} LIMIT {_maxRows};";
        }
    }
}


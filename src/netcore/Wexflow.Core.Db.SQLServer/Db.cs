﻿using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Xml.Linq;

namespace Wexflow.Core.Db.SQLServer
{
    public sealed class Db : Core.Db.Db
    {
        private static readonly object _padlock = new object();
        private const string DATE_TIME_FORMAT = "yyyyMMdd HH:mm:ss.fff";

        private static string _connectionString;

        public Db(string connectionString) : base(connectionString)
        {
            _connectionString = connectionString;

            var server = string.Empty;
            var trustedConnection = false;
            var encrypt = false;
            var userId = string.Empty;
            var password = string.Empty;
            var database = string.Empty;

            var connectionStringParts = ConnectionString.Split(';');

            foreach (var part in connectionStringParts)
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;

                var kvp = part.Split(new[] { '=' }, 2);
                if (kvp.Length != 2)
                    continue;

                var key = kvp[0].Trim().ToLowerInvariant();
                var value = kvp[1].Trim();

                switch (key.ToLowerInvariant())
                {
                    case "server":
                        server = value;
                        break;
                    case "trusted_connection":
                        trustedConnection = bool.TryParse(value, out var tc) && tc;
                        break;
                    case "encrypt":
                        encrypt = bool.TryParse(value, out var enc) && enc;
                        break;
                    case "user id":
                        userId = value;
                        break;
                    case "password":
                        password = value;
                        break;
                    case "database":
                        database = value;
                        break;
                }
            }

            var helper = new Helper(connectionString);
            Helper.CreateDatabaseIfNotExists(server, trustedConnection, encrypt, userId, password, database);
            helper.CreateTableIfNotExists(Core.Db.Entry.DOCUMENT_NAME, Entry.TABLE_STRUCT);
            helper.CreateTableIfNotExists(Core.Db.HistoryEntry.DOCUMENT_NAME, HistoryEntry.TABLE_STRUCT);
            helper.CreateTableIfNotExists(Core.Db.StatusCount.DOCUMENT_NAME, StatusCount.TABLE_STRUCT);
            helper.CreateTableIfNotExists(Core.Db.User.DOCUMENT_NAME, User.TABLE_STRUCT);
            helper.CreateTableIfNotExists(Core.Db.UserWorkflow.DOCUMENT_NAME, UserWorkflow.TABLE_STRUCT);
            helper.CreateTableIfNotExists(Core.Db.Workflow.DOCUMENT_NAME, Workflow.TABLE_STRUCT);
            helper.CreateTableIfNotExists(Core.Db.Version.DOCUMENT_NAME, Version.TABLE_STRUCT);
            helper.CreateTableIfNotExists(Core.Db.Record.DOCUMENT_NAME, Record.TABLE_STRUCT);
            helper.CreateTableIfNotExists(Core.Db.Notification.DOCUMENT_NAME, Notification.TABLE_STRUCT);
            helper.CreateTableIfNotExists(Core.Db.Approver.DOCUMENT_NAME, Approver.TABLE_STRUCT);
        }

        public override void Init()
        {
            // StatusCount
            ClearStatusCount();

            var statusCount = new StatusCount
            {
                PendingCount = 0,
                RunningCount = 0,
                DoneCount = 0,
                FailedCount = 0,
                WarningCount = 0,
                DisabledCount = 0,
                StoppedCount = 0
            };

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var command = new SqlCommand("INSERT INTO " + Core.Db.StatusCount.DOCUMENT_NAME + "("
                    + StatusCount.COLUMN_NAME_PENDING_COUNT + ", "
                    + StatusCount.COLUMN_NAME_RUNNING_COUNT + ", "
                    + StatusCount.COLUMN_NAME_DONE_COUNT + ", "
                    + StatusCount.COLUMN_NAME_FAILED_COUNT + ", "
                    + StatusCount.COLUMN_NAME_WARNING_COUNT + ", "
                    + StatusCount.COLUMN_NAME_DISABLED_COUNT + ", "
                    + StatusCount.COLUMN_NAME_STOPPED_COUNT + ", "
                    + StatusCount.COLUMN_NAME_REJECTED_COUNT + ") VALUES("
                    + statusCount.PendingCount + ", "
                    + statusCount.RunningCount + ", "
                    + statusCount.DoneCount + ", "
                    + statusCount.FailedCount + ", "
                    + statusCount.WarningCount + ", "
                    + statusCount.DisabledCount + ", "
                    + statusCount.StoppedCount + ", "
                    + statusCount.RejectedCount + ");"
                    , conn))
                {
                    _ = command.ExecuteNonQuery();
                }
            }

            // Entries
            ClearEntries();

            // Insert admin user if it does not exist
            // Backward compatibility: update admin password from MD5 hash to SHA256 hash of "wexflow2018"
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var command = new SqlCommand("SELECT " +
                    User.COLUMN_NAME_ID + ", " +
                    User.COLUMN_NAME_PASSWORD +
                    " FROM " + Core.Db.User.DOCUMENT_NAME +
                    " WHERE " + User.COLUMN_NAME_USERNAME + " = @username;", conn))
                {
                    command.Parameters.AddWithValue("@username", "admin");

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var id = Convert.ToInt64(reader[User.COLUMN_NAME_ID]);
                            var password = reader[User.COLUMN_NAME_PASSWORD]?.ToString();

                            if (IsMd5(password) || IsSha256(password))
                            {
                                var hashedPassword = HashPassword("wexflow2018"); // Set to default password

                                reader.Close(); // Close reader before running UPDATE

                                using (var updateCmd = new SqlCommand("UPDATE " +
                                    Core.Db.User.DOCUMENT_NAME +
                                    " SET " + User.COLUMN_NAME_PASSWORD + " = @hashedPassword " +
                                    "WHERE " + User.COLUMN_NAME_ID + " = @id;", conn))
                                {
                                    updateCmd.Parameters.AddWithValue("@hashedPassword", hashedPassword);
                                    updateCmd.Parameters.AddWithValue("@id", id);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }
                        else
                        {
                            InsertDefaultUser();
                        }
                    }
                }
            }

        }

        public override bool CheckUserWorkflow(string userId, string workflowId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT COUNT(*) FROM " + Core.Db.UserWorkflow.DOCUMENT_NAME
                        + " WHERE " + UserWorkflow.COLUMN_NAME_USER_ID + "=" + int.Parse(userId)
                        + " AND " + UserWorkflow.COLUMN_NAME_WORKFLOW_ID + "=" + int.Parse(workflowId)
                        + ";", conn))
                    {
                        var count = (int)command.ExecuteScalar();

                        return count > 0;
                    }
                }
            }
        }

        public override void ClearEntries()
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("DELETE FROM " + Core.Db.Entry.DOCUMENT_NAME + ";", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void ClearStatusCount()
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("DELETE FROM " + Core.Db.StatusCount.DOCUMENT_NAME + ";", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DeleteUser(string username, string password)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("DELETE FROM " + Core.Db.User.DOCUMENT_NAME
                        + " WHERE " + User.COLUMN_NAME_USERNAME + " = '" + username + "'"
                        + " AND " + User.COLUMN_NAME_PASSWORD + " = '" + password + "'"
                        + ";", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DeleteUserWorkflowRelationsByUserId(string userId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("DELETE FROM " + Core.Db.UserWorkflow.DOCUMENT_NAME
                        + " WHERE " + UserWorkflow.COLUMN_NAME_USER_ID + " = " + int.Parse(userId) + ";", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DeleteUserWorkflowRelationsByWorkflowId(string workflowDbId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("DELETE FROM " + Core.Db.UserWorkflow.DOCUMENT_NAME
                        + " WHERE " + UserWorkflow.COLUMN_NAME_WORKFLOW_ID + " = " + int.Parse(workflowDbId) + ";", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DeleteWorkflow(string id)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("DELETE FROM " + Core.Db.Workflow.DOCUMENT_NAME
                        + " WHERE " + Workflow.COLUMN_NAME_ID + " = " + int.Parse(id) + ";", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DeleteWorkflows(string[] ids)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var builder = new StringBuilder("(");

                    for (var i = 0; i < ids.Length; i++)
                    {
                        var id = ids[i];
                        _ = builder.Append(id);
                        _ = i < ids.Length - 1 ? builder.Append(", ") : builder.Append(')');
                    }

                    using (var command = new SqlCommand("DELETE FROM " + Core.Db.Workflow.DOCUMENT_NAME
                        + " WHERE " + Workflow.COLUMN_NAME_ID + " IN " + builder + ";", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override IEnumerable<Core.Db.User> GetAdministrators(string keyword, UserOrderBy uo)
        {
            lock (_padlock)
            {
                var admins = new List<User>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + User.COLUMN_NAME_ID + ", "
                        + User.COLUMN_NAME_USERNAME + ", "
                        + User.COLUMN_NAME_PASSWORD + ", "
                        + User.COLUMN_NAME_EMAIL + ", "
                        + User.COLUMN_NAME_USER_PROFILE + ", "
                        + User.COLUMN_NAME_CREATED_ON + ", "
                        + User.COLUMN_NAME_MODIFIED_ON
                        + " FROM " + Core.Db.User.DOCUMENT_NAME
                        + " WHERE " + "(LOWER(" + User.COLUMN_NAME_USERNAME + ")" + " LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " AND " + User.COLUMN_NAME_USER_PROFILE + " = " + (int)UserProfile.Administrator + ")"
                        + " ORDER BY " + User.COLUMN_NAME_USERNAME + (uo == UserOrderBy.UsernameAscending ? " ASC" : " DESC")
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var admin = new User
                                {
                                    Id = (int)reader[User.COLUMN_NAME_ID],
                                    Username = (string)reader[User.COLUMN_NAME_USERNAME],
                                    Password = (string)reader[User.COLUMN_NAME_PASSWORD],
                                    Email = (string)reader[User.COLUMN_NAME_EMAIL],
                                    UserProfile = (UserProfile)(int)reader[User.COLUMN_NAME_USER_PROFILE],
                                    CreatedOn = (DateTime)reader[User.COLUMN_NAME_CREATED_ON],
                                    ModifiedOn = reader[User.COLUMN_NAME_MODIFIED_ON] == DBNull.Value ? DateTime.MinValue : (DateTime)reader[User.COLUMN_NAME_MODIFIED_ON]
                                };

                                admins.Add(admin);
                            }
                        }
                    }

                    return admins;
                }
            }
        }

        public override IEnumerable<Core.Db.Entry> GetEntries()
        {
            lock (_padlock)
            {
                var entries = new List<Entry>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + Entry.COLUMN_NAME_ID + ", "
                        + Entry.COLUMN_NAME_NAME + ", "
                        + Entry.COLUMN_NAME_DESCRIPTION + ", "
                        + Entry.COLUMN_NAME_LAUNCH_TYPE + ", "
                        + Entry.COLUMN_NAME_STATUS + ", "
                        + Entry.COLUMN_NAME_STATUS_DATE + ", "
                        + Entry.COLUMN_NAME_WORKFLOW_ID + ", "
                        + Entry.COLUMN_NAME_JOB_ID
                        + " FROM " + Core.Db.Entry.DOCUMENT_NAME + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var entry = new Entry
                                {
                                    Id = (int)reader[Entry.COLUMN_NAME_ID],
                                    Name = (string)reader[Entry.COLUMN_NAME_NAME],
                                    Description = (string)reader[Entry.COLUMN_NAME_DESCRIPTION],
                                    LaunchType = (LaunchType)(int)reader[Entry.COLUMN_NAME_LAUNCH_TYPE],
                                    Status = (Status)(int)reader[Entry.COLUMN_NAME_STATUS],
                                    StatusDate = (DateTime)reader[Entry.COLUMN_NAME_STATUS_DATE],
                                    WorkflowId = (int)reader[Entry.COLUMN_NAME_WORKFLOW_ID],
                                    JobId = (string)reader[Entry.COLUMN_NAME_JOB_ID]
                                };

                                entries.Add(entry);
                            }
                        }

                        return entries;
                    }
                }
            }
        }

        public override IEnumerable<Core.Db.Entry> GetEntries(string keyword, DateTime from, DateTime to, int page, int entriesCount, EntryOrderBy eo)
        {
            lock (_padlock)
            {
                var entries = new List<Entry>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var sqlBuilder = new StringBuilder("SELECT "
                        + Entry.COLUMN_NAME_ID + ", "
                        + Entry.COLUMN_NAME_NAME + ", "
                        + Entry.COLUMN_NAME_DESCRIPTION + ", "
                        + Entry.COLUMN_NAME_LAUNCH_TYPE + ", "
                        + Entry.COLUMN_NAME_STATUS + ", "
                        + Entry.COLUMN_NAME_STATUS_DATE + ", "
                        + Entry.COLUMN_NAME_WORKFLOW_ID + ", "
                        + Entry.COLUMN_NAME_JOB_ID
                        + " FROM " + Core.Db.Entry.DOCUMENT_NAME
                        + " WHERE " + "(LOWER(" + Entry.COLUMN_NAME_NAME + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " OR " + "LOWER(" + Entry.COLUMN_NAME_DESCRIPTION + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%')"
                        + " AND (" + Entry.COLUMN_NAME_STATUS_DATE + " BETWEEN CONVERT(DATETIME, '" + from.ToString(DATE_TIME_FORMAT) + "') AND CONVERT(DATETIME, '" + to.ToString(DATE_TIME_FORMAT) + "'))"
                        + " ORDER BY ");

                    switch (eo)
                    {
                        case EntryOrderBy.StatusDateAscending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_STATUS_DATE).Append(" ASC");
                            break;

                        case EntryOrderBy.StatusDateDescending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_STATUS_DATE).Append(" DESC");
                            break;

                        case EntryOrderBy.WorkflowIdAscending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_WORKFLOW_ID).Append(" ASC");
                            break;

                        case EntryOrderBy.WorkflowIdDescending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_WORKFLOW_ID).Append(" DESC");
                            break;

                        case EntryOrderBy.NameAscending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_NAME).Append(" ASC");
                            break;

                        case EntryOrderBy.NameDescending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_NAME).Append(" DESC");
                            break;

                        case EntryOrderBy.LaunchTypeAscending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_LAUNCH_TYPE).Append(" ASC");
                            break;

                        case EntryOrderBy.LaunchTypeDescending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_LAUNCH_TYPE).Append(" DESC");
                            break;

                        case EntryOrderBy.DescriptionAscending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_DESCRIPTION).Append(" ASC");
                            break;

                        case EntryOrderBy.DescriptionDescending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_DESCRIPTION).Append(" DESC");
                            break;

                        case EntryOrderBy.StatusAscending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_STATUS).Append(" ASC");
                            break;

                        case EntryOrderBy.StatusDescending:

                            _ = sqlBuilder.Append(Entry.COLUMN_NAME_STATUS).Append(" DESC");
                            break;

                        default:
                            break;
                    }

                    _ = sqlBuilder
                        .Append(" OFFSET ").Append((page - 1) * entriesCount).Append(" ROWS")
                        .Append(" FETCH NEXT ").Append(entriesCount).Append("ROWS ONLY;");

                    using (var command = new SqlCommand(sqlBuilder.ToString(), conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var entry = new Entry
                                {
                                    Id = (int)reader[Entry.COLUMN_NAME_ID],
                                    Name = (string)reader[Entry.COLUMN_NAME_NAME],
                                    Description = (string)reader[Entry.COLUMN_NAME_DESCRIPTION],
                                    LaunchType = (LaunchType)(int)reader[Entry.COLUMN_NAME_LAUNCH_TYPE],
                                    Status = (Status)(int)reader[Entry.COLUMN_NAME_STATUS],
                                    StatusDate = (DateTime)reader[Entry.COLUMN_NAME_STATUS_DATE],
                                    WorkflowId = (int)reader[Entry.COLUMN_NAME_WORKFLOW_ID],
                                    JobId = (string)reader[Entry.COLUMN_NAME_JOB_ID]
                                };

                                entries.Add(entry);
                            }
                        }

                        return entries;
                    }
                }
            }
        }

        public override long GetEntriesCount(string keyword, DateTime from, DateTime to)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT COUNT(*)"
                        + " FROM " + Core.Db.Entry.DOCUMENT_NAME
                        + " WHERE " + "(LOWER(" + Entry.COLUMN_NAME_NAME + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " OR " + "LOWER(" + Entry.COLUMN_NAME_DESCRIPTION + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%')"
                        + " AND (" + Entry.COLUMN_NAME_STATUS_DATE + " BETWEEN CONVERT(DATETIME, '" + from.ToString(DATE_TIME_FORMAT) + "') AND CONVERT(DATETIME, '" + to.ToString(DATE_TIME_FORMAT) + "'));", conn))
                    {
                        var count = (int)command.ExecuteScalar();

                        return count;
                    }
                }
            }
        }

        public override Core.Db.Entry GetEntry(int workflowId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT TOP 1 "
                        + Entry.COLUMN_NAME_ID + ", "
                        + Entry.COLUMN_NAME_NAME + ", "
                        + Entry.COLUMN_NAME_DESCRIPTION + ", "
                        + Entry.COLUMN_NAME_LAUNCH_TYPE + ", "
                        + Entry.COLUMN_NAME_STATUS + ", "
                        + Entry.COLUMN_NAME_STATUS_DATE + ", "
                        + Entry.COLUMN_NAME_WORKFLOW_ID + ", "
                        + Entry.COLUMN_NAME_JOB_ID
                        + " FROM " + Core.Db.Entry.DOCUMENT_NAME
                        + " WHERE " + Entry.COLUMN_NAME_WORKFLOW_ID + " = " + workflowId
                        + " ORDER BY " + Entry.COLUMN_NAME_STATUS_DATE + " DESC;"
                        , conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var entry = new Entry
                                {
                                    Id = (int)reader[Entry.COLUMN_NAME_ID],
                                    Name = (string)reader[Entry.COLUMN_NAME_NAME],
                                    Description = (string)reader[Entry.COLUMN_NAME_DESCRIPTION],
                                    LaunchType = (LaunchType)(int)reader[Entry.COLUMN_NAME_LAUNCH_TYPE],
                                    Status = (Status)(int)reader[Entry.COLUMN_NAME_STATUS],
                                    StatusDate = (DateTime)reader[Entry.COLUMN_NAME_STATUS_DATE],
                                    WorkflowId = (int)reader[Entry.COLUMN_NAME_WORKFLOW_ID],
                                    JobId = (string)reader[Entry.COLUMN_NAME_JOB_ID]
                                };

                                return entry;
                            }
                        }
                    }
                }

                return null;
            }
        }

        public override Core.Db.Entry GetEntry(int workflowId, Guid jobId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                         + Entry.COLUMN_NAME_ID + ", "
                         + Entry.COLUMN_NAME_NAME + ", "
                         + Entry.COLUMN_NAME_DESCRIPTION + ", "
                         + Entry.COLUMN_NAME_LAUNCH_TYPE + ", "
                         + Entry.COLUMN_NAME_STATUS + ", "
                         + Entry.COLUMN_NAME_STATUS_DATE + ", "
                         + Entry.COLUMN_NAME_WORKFLOW_ID + ", "
                         + Entry.COLUMN_NAME_JOB_ID
                         + " FROM " + Core.Db.Entry.DOCUMENT_NAME
                         + " WHERE (" + Entry.COLUMN_NAME_WORKFLOW_ID + " = " + workflowId
                         + " AND " + Entry.COLUMN_NAME_JOB_ID + " = '" + jobId + "');", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var entry = new Entry
                                {
                                    Id = (int)reader[Entry.COLUMN_NAME_ID],
                                    Name = (string)reader[Entry.COLUMN_NAME_NAME],
                                    Description = (string)reader[Entry.COLUMN_NAME_DESCRIPTION],
                                    LaunchType = (LaunchType)(int)reader[Entry.COLUMN_NAME_LAUNCH_TYPE],
                                    Status = (Status)(int)reader[Entry.COLUMN_NAME_STATUS],
                                    StatusDate = (DateTime)reader[Entry.COLUMN_NAME_STATUS_DATE],
                                    WorkflowId = (int)reader[Entry.COLUMN_NAME_WORKFLOW_ID],
                                    JobId = (string)reader[Entry.COLUMN_NAME_JOB_ID]
                                };

                                return entry;
                            }
                        }
                    }
                }

                return null;
            }
        }

        public override DateTime GetEntryStatusDateMax()
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT TOP 1 " + Entry.COLUMN_NAME_STATUS_DATE
                        + " FROM " + Core.Db.Entry.DOCUMENT_NAME
                        + " ORDER BY " + Entry.COLUMN_NAME_STATUS_DATE + " DESC;", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var statusDate = (DateTime)reader[Entry.COLUMN_NAME_STATUS_DATE];

                                return statusDate;
                            }
                        }
                    }
                }

                return DateTime.Now;
            }
        }

        public override DateTime GetEntryStatusDateMin()
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT TOP 1 " + Entry.COLUMN_NAME_STATUS_DATE
                         + " FROM " + Core.Db.Entry.DOCUMENT_NAME
                         + " ORDER BY " + Entry.COLUMN_NAME_STATUS_DATE + " ASC;", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var statusDate = (DateTime)reader[Entry.COLUMN_NAME_STATUS_DATE];

                                return statusDate;
                            }
                        }
                    }
                }

                return DateTime.Now;
            }
        }

        public override IEnumerable<Core.Db.HistoryEntry> GetHistoryEntries()
        {
            lock (_padlock)
            {
                var entries = new List<HistoryEntry>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + HistoryEntry.COLUMN_NAME_ID + ", "
                        + HistoryEntry.COLUMN_NAME_NAME + ", "
                        + HistoryEntry.COLUMN_NAME_DESCRIPTION + ", "
                        + HistoryEntry.COLUMN_NAME_LAUNCH_TYPE + ", "
                        + HistoryEntry.COLUMN_NAME_STATUS + ", "
                        + HistoryEntry.COLUMN_NAME_STATUS_DATE + ", "
                        + HistoryEntry.COLUMN_NAME_WORKFLOW_ID
                        + " FROM " + Core.Db.HistoryEntry.DOCUMENT_NAME + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var entry = new HistoryEntry
                                {
                                    Id = (int)reader[HistoryEntry.COLUMN_NAME_ID],
                                    Name = (string)reader[HistoryEntry.COLUMN_NAME_NAME],
                                    Description = (string)reader[HistoryEntry.COLUMN_NAME_DESCRIPTION],
                                    LaunchType = (LaunchType)(int)reader[HistoryEntry.COLUMN_NAME_LAUNCH_TYPE],
                                    Status = (Status)(int)reader[HistoryEntry.COLUMN_NAME_STATUS],
                                    StatusDate = (DateTime)reader[HistoryEntry.COLUMN_NAME_STATUS_DATE],
                                    WorkflowId = (int)reader[HistoryEntry.COLUMN_NAME_WORKFLOW_ID]
                                };

                                entries.Add(entry);
                            }
                        }

                        return entries;
                    }
                }
            }
        }

        public override IEnumerable<Core.Db.HistoryEntry> GetHistoryEntries(string keyword)
        {
            lock (_padlock)
            {
                var entries = new List<HistoryEntry>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + HistoryEntry.COLUMN_NAME_ID + ", "
                        + HistoryEntry.COLUMN_NAME_NAME + ", "
                        + HistoryEntry.COLUMN_NAME_DESCRIPTION + ", "
                        + HistoryEntry.COLUMN_NAME_LAUNCH_TYPE + ", "
                        + HistoryEntry.COLUMN_NAME_STATUS + ", "
                        + HistoryEntry.COLUMN_NAME_STATUS_DATE + ", "
                        + HistoryEntry.COLUMN_NAME_WORKFLOW_ID
                        + " FROM " + Core.Db.HistoryEntry.DOCUMENT_NAME
                        + " WHERE " + "LOWER(" + HistoryEntry.COLUMN_NAME_NAME + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " OR " + "LOWER(" + HistoryEntry.COLUMN_NAME_DESCRIPTION + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%';", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var entry = new HistoryEntry
                                {
                                    Id = (int)reader[HistoryEntry.COLUMN_NAME_ID],
                                    Name = (string)reader[HistoryEntry.COLUMN_NAME_NAME],
                                    Description = (string)reader[HistoryEntry.COLUMN_NAME_DESCRIPTION],
                                    LaunchType = (LaunchType)(int)reader[HistoryEntry.COLUMN_NAME_LAUNCH_TYPE],
                                    Status = (Status)(int)reader[HistoryEntry.COLUMN_NAME_STATUS],
                                    StatusDate = (DateTime)reader[HistoryEntry.COLUMN_NAME_STATUS_DATE],
                                    WorkflowId = (int)reader[HistoryEntry.COLUMN_NAME_WORKFLOW_ID]
                                };

                                entries.Add(entry);
                            }
                        }

                        return entries;
                    }
                }
            }
        }

        public override IEnumerable<Core.Db.HistoryEntry> GetHistoryEntries(string keyword, int page, int entriesCount)
        {
            lock (_padlock)
            {
                var entries = new List<HistoryEntry>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + HistoryEntry.COLUMN_NAME_ID + ", "
                        + HistoryEntry.COLUMN_NAME_NAME + ", "
                        + HistoryEntry.COLUMN_NAME_DESCRIPTION + ", "
                        + HistoryEntry.COLUMN_NAME_LAUNCH_TYPE + ", "
                        + HistoryEntry.COLUMN_NAME_STATUS + ", "
                        + HistoryEntry.COLUMN_NAME_STATUS_DATE + ", "
                        + HistoryEntry.COLUMN_NAME_WORKFLOW_ID
                        + " FROM " + Core.Db.HistoryEntry.DOCUMENT_NAME
                        + " WHERE " + "LOWER(" + HistoryEntry.COLUMN_NAME_NAME + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " OR " + "LOWER(" + HistoryEntry.COLUMN_NAME_DESCRIPTION + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " OFFSET " + ((page - 1) * entriesCount) + " ROWS"
                        + " FETCH NEXT " + entriesCount + "ROWS ONLY;"

                        , conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var entry = new HistoryEntry
                                {
                                    Id = (int)reader[HistoryEntry.COLUMN_NAME_ID],
                                    Name = (string)reader[HistoryEntry.COLUMN_NAME_NAME],
                                    Description = (string)reader[HistoryEntry.COLUMN_NAME_DESCRIPTION],
                                    LaunchType = (LaunchType)(int)reader[HistoryEntry.COLUMN_NAME_LAUNCH_TYPE],
                                    Status = (Status)(int)reader[HistoryEntry.COLUMN_NAME_STATUS],
                                    StatusDate = (DateTime)reader[HistoryEntry.COLUMN_NAME_STATUS_DATE],
                                    WorkflowId = (int)reader[HistoryEntry.COLUMN_NAME_WORKFLOW_ID]
                                };

                                entries.Add(entry);
                            }
                        }

                        return entries;
                    }
                }
            }
        }

        public override IEnumerable<Core.Db.HistoryEntry> GetHistoryEntries(string keyword, DateTime from, DateTime to, int page, int entriesCount, EntryOrderBy heo)
        {
            lock (_padlock)
            {
                var entries = new List<HistoryEntry>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var sqlBuilder = new StringBuilder("SELECT "
                        + HistoryEntry.COLUMN_NAME_ID + ", "
                        + HistoryEntry.COLUMN_NAME_NAME + ", "
                        + HistoryEntry.COLUMN_NAME_DESCRIPTION + ", "
                        + HistoryEntry.COLUMN_NAME_LAUNCH_TYPE + ", "
                        + HistoryEntry.COLUMN_NAME_STATUS + ", "
                        + HistoryEntry.COLUMN_NAME_STATUS_DATE + ", "
                        + HistoryEntry.COLUMN_NAME_WORKFLOW_ID
                        + " FROM " + Core.Db.HistoryEntry.DOCUMENT_NAME
                        + " WHERE " + "(LOWER(" + HistoryEntry.COLUMN_NAME_NAME + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " OR " + "LOWER(" + HistoryEntry.COLUMN_NAME_DESCRIPTION + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%')"
                        + " AND (" + HistoryEntry.COLUMN_NAME_STATUS_DATE + " BETWEEN CONVERT(DATETIME, '" + from.ToString(DATE_TIME_FORMAT) + "') AND CONVERT(DATETIME, '" + to.ToString(DATE_TIME_FORMAT) + "'))"
                        + " ORDER BY ");

                    switch (heo)
                    {
                        case EntryOrderBy.StatusDateAscending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_STATUS_DATE).Append(" ASC");
                            break;

                        case EntryOrderBy.StatusDateDescending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_STATUS_DATE).Append(" DESC");
                            break;

                        case EntryOrderBy.WorkflowIdAscending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_WORKFLOW_ID).Append(" ASC");
                            break;

                        case EntryOrderBy.WorkflowIdDescending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_WORKFLOW_ID).Append(" DESC");
                            break;

                        case EntryOrderBy.NameAscending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_NAME).Append(" ASC");
                            break;

                        case EntryOrderBy.NameDescending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_NAME).Append(" DESC");
                            break;

                        case EntryOrderBy.LaunchTypeAscending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_LAUNCH_TYPE).Append(" ASC");
                            break;

                        case EntryOrderBy.LaunchTypeDescending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_LAUNCH_TYPE).Append(" DESC");
                            break;

                        case EntryOrderBy.DescriptionAscending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_DESCRIPTION).Append(" ASC");
                            break;

                        case EntryOrderBy.DescriptionDescending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_DESCRIPTION).Append(" DESC");
                            break;

                        case EntryOrderBy.StatusAscending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_STATUS).Append(" ASC");
                            break;

                        case EntryOrderBy.StatusDescending:

                            _ = sqlBuilder.Append(HistoryEntry.COLUMN_NAME_STATUS).Append(" DESC");
                            break;

                        default:
                            break;
                    }

                    _ = sqlBuilder
                        .Append(" OFFSET ").Append((page - 1) * entriesCount).Append(" ROWS")
                        .Append(" FETCH NEXT ").Append(entriesCount).Append("ROWS ONLY;");

                    using (var command = new SqlCommand(sqlBuilder.ToString(), conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var entry = new HistoryEntry
                                {
                                    Id = (int)reader[HistoryEntry.COLUMN_NAME_ID],
                                    Name = (string)reader[HistoryEntry.COLUMN_NAME_NAME],
                                    Description = (string)reader[HistoryEntry.COLUMN_NAME_DESCRIPTION],
                                    LaunchType = (LaunchType)(int)reader[HistoryEntry.COLUMN_NAME_LAUNCH_TYPE],
                                    Status = (Status)(int)reader[HistoryEntry.COLUMN_NAME_STATUS],
                                    StatusDate = (DateTime)reader[HistoryEntry.COLUMN_NAME_STATUS_DATE],
                                    WorkflowId = (int)reader[HistoryEntry.COLUMN_NAME_WORKFLOW_ID]
                                };

                                entries.Add(entry);
                            }
                        }

                        return entries;
                    }
                }
            }
        }

        public override long GetHistoryEntriesCount(string keyword)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT COUNT(*)"
                        + " FROM " + Core.Db.HistoryEntry.DOCUMENT_NAME
                        + " WHERE " + "LOWER(" + HistoryEntry.COLUMN_NAME_NAME + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " OR " + "LOWER(" + HistoryEntry.COLUMN_NAME_DESCRIPTION + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%';", conn))
                    {
                        var count = (int)command.ExecuteScalar();

                        return count;
                    }
                }
            }
        }

        public override long GetHistoryEntriesCount(string keyword, DateTime from, DateTime to)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT COUNT(*)"
                         + " FROM " + Core.Db.HistoryEntry.DOCUMENT_NAME
                         + " WHERE " + "(LOWER(" + HistoryEntry.COLUMN_NAME_NAME + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                         + " OR " + "LOWER(" + HistoryEntry.COLUMN_NAME_DESCRIPTION + ") LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%')"
                         + " AND (" + HistoryEntry.COLUMN_NAME_STATUS_DATE + " BETWEEN CONVERT(DATETIME, '" + from.ToString(DATE_TIME_FORMAT) + "') AND CONVERT(DATETIME, '" + to.ToString(DATE_TIME_FORMAT) + "'));", conn))
                    {
                        var count = (int)command.ExecuteScalar();

                        return count;
                    }
                }
            }
        }

        public override DateTime GetHistoryEntryStatusDateMax()
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT TOP 1 " + HistoryEntry.COLUMN_NAME_STATUS_DATE
                        + " FROM " + Core.Db.HistoryEntry.DOCUMENT_NAME
                        + " ORDER BY " + HistoryEntry.COLUMN_NAME_STATUS_DATE + " DESC;", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var statusDate = (DateTime)reader[HistoryEntry.COLUMN_NAME_STATUS_DATE];

                                return statusDate;
                            }
                        }
                    }

                    return DateTime.Now;
                }
            }
        }

        public override DateTime GetHistoryEntryStatusDateMin()
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT TOP 1 " + HistoryEntry.COLUMN_NAME_STATUS_DATE
                        + " FROM " + Core.Db.HistoryEntry.DOCUMENT_NAME
                        + " ORDER BY " + HistoryEntry.COLUMN_NAME_STATUS_DATE + " ASC;", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var statusDate = (DateTime)reader[HistoryEntry.COLUMN_NAME_STATUS_DATE];

                                return statusDate;
                            }
                        }
                    }
                }

                return DateTime.Now;
            }
        }

        public override string GetPassword(string username)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + User.COLUMN_NAME_PASSWORD
                        + " FROM " + Core.Db.User.DOCUMENT_NAME
                        + " WHERE " + User.COLUMN_NAME_USERNAME + " = '" + (username ?? "").Replace("'", "''") + "'"
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var password = (string)reader[User.COLUMN_NAME_PASSWORD];

                                return password;
                            }
                        }
                    }
                }

                return null;
            }
        }

        public override Core.Db.StatusCount GetStatusCount()
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + StatusCount.COLUMN_NAME_ID + ", "
                        + StatusCount.COLUMN_NAME_PENDING_COUNT + ", "
                        + StatusCount.COLUMN_NAME_RUNNING_COUNT + ", "
                        + StatusCount.COLUMN_NAME_DONE_COUNT + ", "
                        + StatusCount.COLUMN_NAME_FAILED_COUNT + ", "
                        + StatusCount.COLUMN_NAME_WARNING_COUNT + ", "
                        + StatusCount.COLUMN_NAME_DISABLED_COUNT + ", "
                        + StatusCount.COLUMN_NAME_STOPPED_COUNT + ", "
                        + StatusCount.COLUMN_NAME_REJECTED_COUNT
                        + " FROM " + Core.Db.StatusCount.DOCUMENT_NAME
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var statusCount = new StatusCount
                                {
                                    Id = (int)reader[StatusCount.COLUMN_NAME_ID],
                                    PendingCount = (int)reader[StatusCount.COLUMN_NAME_PENDING_COUNT],
                                    RunningCount = (int)reader[StatusCount.COLUMN_NAME_RUNNING_COUNT],
                                    DoneCount = (int)reader[StatusCount.COLUMN_NAME_DONE_COUNT],
                                    FailedCount = (int)reader[StatusCount.COLUMN_NAME_FAILED_COUNT],
                                    WarningCount = (int)reader[StatusCount.COLUMN_NAME_WARNING_COUNT],
                                    DisabledCount = (int)reader[StatusCount.COLUMN_NAME_DISABLED_COUNT],
                                    StoppedCount = (int)reader[StatusCount.COLUMN_NAME_STOPPED_COUNT],
                                    RejectedCount = (int)reader[StatusCount.COLUMN_NAME_REJECTED_COUNT]
                                };

                                return statusCount;
                            }
                        }
                    }
                }
                return null;
            }
        }

        public override Core.Db.User GetUser(string username)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + User.COLUMN_NAME_ID + ", "
                        + User.COLUMN_NAME_USERNAME + ", "
                        + User.COLUMN_NAME_PASSWORD + ", "
                        + User.COLUMN_NAME_EMAIL + ", "
                        + User.COLUMN_NAME_USER_PROFILE + ", "
                        + User.COLUMN_NAME_CREATED_ON + ", "
                        + User.COLUMN_NAME_MODIFIED_ON
                        + " FROM " + Core.Db.User.DOCUMENT_NAME
                        + " WHERE " + User.COLUMN_NAME_USERNAME + " = '" + (username ?? "").Replace("'", "''") + "'"
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var user = new User
                                {
                                    Id = (int)reader[User.COLUMN_NAME_ID],
                                    Username = (string)reader[User.COLUMN_NAME_USERNAME],
                                    Password = (string)reader[User.COLUMN_NAME_PASSWORD],
                                    Email = (string)reader[User.COLUMN_NAME_EMAIL],
                                    UserProfile = (UserProfile)(int)reader[User.COLUMN_NAME_USER_PROFILE],
                                    CreatedOn = (DateTime)reader[User.COLUMN_NAME_CREATED_ON],
                                    ModifiedOn = reader[User.COLUMN_NAME_MODIFIED_ON] == DBNull.Value ? DateTime.MinValue : (DateTime)reader[User.COLUMN_NAME_MODIFIED_ON]
                                };

                                return user;
                            }
                        }
                    }
                }

                return null;
            }
        }

        public override Core.Db.User GetUserById(string userId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + User.COLUMN_NAME_ID + ", "
                         + User.COLUMN_NAME_USERNAME + ", "
                         + User.COLUMN_NAME_PASSWORD + ", "
                         + User.COLUMN_NAME_EMAIL + ", "
                         + User.COLUMN_NAME_USER_PROFILE + ", "
                         + User.COLUMN_NAME_CREATED_ON + ", "
                         + User.COLUMN_NAME_MODIFIED_ON
                         + " FROM " + Core.Db.User.DOCUMENT_NAME
                         + " WHERE " + User.COLUMN_NAME_ID + " = '" + int.Parse(userId) + "'"
                         + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var user = new User
                                {
                                    Id = (int)reader[User.COLUMN_NAME_ID],
                                    Username = (string)reader[User.COLUMN_NAME_USERNAME],
                                    Password = (string)reader[User.COLUMN_NAME_PASSWORD],
                                    Email = (string)reader[User.COLUMN_NAME_EMAIL],
                                    UserProfile = (UserProfile)(int)reader[User.COLUMN_NAME_USER_PROFILE],
                                    CreatedOn = (DateTime)reader[User.COLUMN_NAME_CREATED_ON],
                                    ModifiedOn = reader[User.COLUMN_NAME_MODIFIED_ON] == DBNull.Value ? DateTime.MinValue : (DateTime)reader[User.COLUMN_NAME_MODIFIED_ON]
                                };

                                return user;
                            }
                        }
                    }
                }
                return null;
            }
        }

        public override IEnumerable<Core.Db.User> GetUsers()
        {
            lock (_padlock)
            {
                var users = new List<User>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + User.COLUMN_NAME_ID + ", "
                        + User.COLUMN_NAME_USERNAME + ", "
                        + User.COLUMN_NAME_PASSWORD + ", "
                        + User.COLUMN_NAME_EMAIL + ", "
                        + User.COLUMN_NAME_USER_PROFILE + ", "
                        + User.COLUMN_NAME_CREATED_ON + ", "
                        + User.COLUMN_NAME_MODIFIED_ON
                        + " FROM " + Core.Db.User.DOCUMENT_NAME
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var user = new User
                                {
                                    Id = (int)reader[User.COLUMN_NAME_ID],
                                    Username = (string)reader[User.COLUMN_NAME_USERNAME],
                                    Password = (string)reader[User.COLUMN_NAME_PASSWORD],
                                    Email = (string)reader[User.COLUMN_NAME_EMAIL],
                                    UserProfile = (UserProfile)(int)reader[User.COLUMN_NAME_USER_PROFILE],
                                    CreatedOn = (DateTime)reader[User.COLUMN_NAME_CREATED_ON],
                                    ModifiedOn = reader[User.COLUMN_NAME_MODIFIED_ON] == DBNull.Value ? DateTime.MinValue : (DateTime)reader[User.COLUMN_NAME_MODIFIED_ON]
                                };

                                users.Add(user);
                            }
                        }
                    }

                    return users;
                }
            }
        }

        public override IEnumerable<Core.Db.User> GetUsers(string keyword, UserOrderBy uo)
        {
            lock (_padlock)
            {
                var users = new List<User>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + User.COLUMN_NAME_ID + ", "
                        + User.COLUMN_NAME_USERNAME + ", "
                        + User.COLUMN_NAME_PASSWORD + ", "
                        + User.COLUMN_NAME_EMAIL + ", "
                        + User.COLUMN_NAME_USER_PROFILE + ", "
                        + User.COLUMN_NAME_CREATED_ON + ", "
                        + User.COLUMN_NAME_MODIFIED_ON
                        + " FROM " + Core.Db.User.DOCUMENT_NAME
                        + " WHERE " + "LOWER(" + User.COLUMN_NAME_USERNAME + ")" + " LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " ORDER BY " + User.COLUMN_NAME_USERNAME + (uo == UserOrderBy.UsernameAscending ? " ASC" : " DESC")
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var user = new User
                                {
                                    Id = (int)reader[User.COLUMN_NAME_ID],
                                    Username = (string)reader[User.COLUMN_NAME_USERNAME],
                                    Password = (string)reader[User.COLUMN_NAME_PASSWORD],
                                    Email = (string)reader[User.COLUMN_NAME_EMAIL],
                                    UserProfile = (UserProfile)(int)reader[User.COLUMN_NAME_USER_PROFILE],
                                    CreatedOn = (DateTime)reader[User.COLUMN_NAME_CREATED_ON],
                                    ModifiedOn = reader[User.COLUMN_NAME_MODIFIED_ON] == DBNull.Value ? DateTime.MinValue : (DateTime)reader[User.COLUMN_NAME_MODIFIED_ON]
                                };

                                users.Add(user);
                            }
                        }
                    }

                    return users;
                }
            }
        }

        public override IEnumerable<string> GetUserWorkflows(string userId)
        {
            lock (_padlock)
            {
                var workflowIds = new List<string>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + UserWorkflow.COLUMN_NAME_ID + ", "
                        + UserWorkflow.COLUMN_NAME_USER_ID + ", "
                        + UserWorkflow.COLUMN_NAME_WORKFLOW_ID
                        + " FROM " + Core.Db.UserWorkflow.DOCUMENT_NAME
                        + " WHERE " + UserWorkflow.COLUMN_NAME_USER_ID + " = " + int.Parse(userId)
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var workflowId = (int)reader[UserWorkflow.COLUMN_NAME_WORKFLOW_ID];

                                workflowIds.Add(workflowId.ToString());
                            }
                        }
                    }

                    return workflowIds;
                }
            }
        }

        public override Core.Db.Workflow GetWorkflow(string id)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + Workflow.COLUMN_NAME_ID + ", "
                        + Workflow.COLUMN_NAME_XML
                        + " FROM " + Core.Db.Workflow.DOCUMENT_NAME
                        + " WHERE " + Workflow.COLUMN_NAME_ID + " = " + int.Parse(id) + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var workflow = new Workflow
                                {
                                    Id = (int)reader[Workflow.COLUMN_NAME_ID],
                                    Xml = XDocument.Parse((string)reader[Workflow.COLUMN_NAME_XML]).ToString()
                                };

                                return workflow;
                            }
                        }
                    }
                }

                return null;
            }
        }

        public override IEnumerable<Core.Db.Workflow> GetWorkflows()
        {
            lock (_padlock)
            {
                var workflows = new List<Core.Db.Workflow>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + Workflow.COLUMN_NAME_ID + ", "
                        + Workflow.COLUMN_NAME_XML
                        + " FROM " + Core.Db.Workflow.DOCUMENT_NAME
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var workflow = new Workflow
                                {
                                    Id = (int)reader[Workflow.COLUMN_NAME_ID],
                                    Xml = XDocument.Parse((string)reader[Workflow.COLUMN_NAME_XML]).ToString()
                                };

                                workflows.Add(workflow);
                            }
                        }
                    }

                    return workflows;
                }
            }
        }

        private static void IncrementStatusCountColumn(string statusCountColumnName)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("UPDATE " + Core.Db.StatusCount.DOCUMENT_NAME + " SET " + statusCountColumnName + " = " + statusCountColumnName + " + 1;", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void IncrementDisabledCount()
        {
            IncrementStatusCountColumn(StatusCount.COLUMN_NAME_DISABLED_COUNT);
        }

        public override void IncrementRejectedCount()
        {
            IncrementStatusCountColumn(StatusCount.COLUMN_NAME_REJECTED_COUNT);
        }

        public override void IncrementDoneCount()
        {
            IncrementStatusCountColumn(StatusCount.COLUMN_NAME_DONE_COUNT);
        }

        public override void IncrementFailedCount()
        {
            IncrementStatusCountColumn(StatusCount.COLUMN_NAME_FAILED_COUNT);
        }

        public override void IncrementPendingCount()
        {
            IncrementStatusCountColumn(StatusCount.COLUMN_NAME_PENDING_COUNT);
        }

        public override void IncrementRunningCount()
        {
            IncrementStatusCountColumn(StatusCount.COLUMN_NAME_RUNNING_COUNT);
        }

        public override void IncrementStoppedCount()
        {
            IncrementStatusCountColumn(StatusCount.COLUMN_NAME_STOPPED_COUNT);
        }

        public override void IncrementWarningCount()
        {
            IncrementStatusCountColumn(StatusCount.COLUMN_NAME_WARNING_COUNT);
        }

        private static void DecrementStatusCountColumn(string statusCountColumnName)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("UPDATE " + Core.Db.StatusCount.DOCUMENT_NAME + " SET " + statusCountColumnName + " = " + statusCountColumnName + " - 1;", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DecrementPendingCount()
        {
            DecrementStatusCountColumn(StatusCount.COLUMN_NAME_PENDING_COUNT);
        }

        public override void DecrementRunningCount()
        {
            DecrementStatusCountColumn(StatusCount.COLUMN_NAME_RUNNING_COUNT);
        }

        public override void InsertEntry(Core.Db.Entry entry)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("INSERT INTO " + Core.Db.Entry.DOCUMENT_NAME + "("
                        + Entry.COLUMN_NAME_NAME + ", "
                        + Entry.COLUMN_NAME_DESCRIPTION + ", "
                        + Entry.COLUMN_NAME_LAUNCH_TYPE + ", "
                        + Entry.COLUMN_NAME_STATUS_DATE + ", "
                        + Entry.COLUMN_NAME_STATUS + ", "
                        + Entry.COLUMN_NAME_WORKFLOW_ID + ", "
                        + Entry.COLUMN_NAME_JOB_ID + ", "
                        + Entry.COLUMN_NAME_LOGS + ") VALUES("
                        + "'" + (entry.Name ?? "").Replace("'", "''") + "'" + ", "
                        + "'" + (entry.Description ?? "").Replace("'", "''") + "'" + ", "
                        + (int)entry.LaunchType + ", "
                        + "'" + entry.StatusDate.ToString(DATE_TIME_FORMAT) + "'" + ", "
                        + (int)entry.Status + ", "
                        + entry.WorkflowId + ", "
                        + "'" + (entry.JobId ?? "") + "', "
                        + "'" + (entry.Logs ?? "").Replace("'", "''") + "'" + ");"
                        , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void InsertHistoryEntry(Core.Db.HistoryEntry entry)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("INSERT INTO " + Core.Db.HistoryEntry.DOCUMENT_NAME + "("
                        + HistoryEntry.COLUMN_NAME_NAME + ", "
                        + HistoryEntry.COLUMN_NAME_DESCRIPTION + ", "
                        + HistoryEntry.COLUMN_NAME_LAUNCH_TYPE + ", "
                        + HistoryEntry.COLUMN_NAME_STATUS_DATE + ", "
                        + HistoryEntry.COLUMN_NAME_STATUS + ", "
                        + HistoryEntry.COLUMN_NAME_WORKFLOW_ID + ", "
                        + HistoryEntry.COLUMN_NAME_LOGS + ") VALUES("
                        + "'" + (entry.Name ?? "").Replace("'", "''") + "'" + ", "
                        + "'" + (entry.Description ?? "").Replace("'", "''") + "'" + ", "
                        + (int)entry.LaunchType + ", "
                        + "'" + entry.StatusDate.ToString(DATE_TIME_FORMAT) + "'" + ", "
                        + (int)entry.Status + ", "
                        + entry.WorkflowId + ", "
                        + "'" + (entry.Logs ?? "").Replace("'", "''") + "'" + ");"
                        , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void InsertUser(Core.Db.User user)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("INSERT INTO " + Core.Db.User.DOCUMENT_NAME + "("
                        + User.COLUMN_NAME_USERNAME + ", "
                        + User.COLUMN_NAME_PASSWORD + ", "
                        + User.COLUMN_NAME_USER_PROFILE + ", "
                        + User.COLUMN_NAME_EMAIL + ", "
                        + User.COLUMN_NAME_CREATED_ON + ", "
                        + User.COLUMN_NAME_MODIFIED_ON + ") VALUES("
                        + "'" + (user.Username ?? "").Replace("'", "''") + "'" + ", "
                        + "'" + (user.Password ?? "").Replace("'", "''") + "'" + ", "
                        + (int)user.UserProfile + ", "
                        + "'" + (user.Email ?? "").Replace("'", "''") + "'" + ", "
                        + "'" + DateTime.Now.ToString(DATE_TIME_FORMAT) + "'" + ", "
                        + (user.ModifiedOn == DateTime.MinValue ? "NULL" : "'" + user.ModifiedOn.ToString(DATE_TIME_FORMAT) + "'") + ");"
                        , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void InsertUserWorkflowRelation(Core.Db.UserWorkflow userWorkflow)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("INSERT INTO " + Core.Db.UserWorkflow.DOCUMENT_NAME + "("
                        + UserWorkflow.COLUMN_NAME_USER_ID + ", "
                        + UserWorkflow.COLUMN_NAME_WORKFLOW_ID + ") VALUES("
                        + int.Parse(userWorkflow.UserId) + ", "
                        + int.Parse(userWorkflow.WorkflowId) + ");"
                        , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override string InsertWorkflow(Core.Db.Workflow workflow)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("INSERT INTO " + Core.Db.Workflow.DOCUMENT_NAME + "("
                        + Workflow.COLUMN_NAME_XML + ") " + " OUTPUT INSERTED." + Workflow.COLUMN_NAME_ID + " VALUES("
                        + "@XML" + ");"
                        , conn))
                    {
                        command.Parameters.Add("@XML", SqlDbType.VarChar).Value = workflow.Xml;
                        var id = (int)command.ExecuteScalar();

                        return id.ToString();
                    }
                }
            }
        }

        public override void UpdateEntry(string id, Core.Db.Entry entry)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("UPDATE " + Core.Db.Entry.DOCUMENT_NAME + " SET "
                        + Entry.COLUMN_NAME_NAME + " = '" + (entry.Name ?? "").Replace("'", "''") + "', "
                        + Entry.COLUMN_NAME_DESCRIPTION + " = '" + (entry.Description ?? "").Replace("'", "''") + "', "
                        + Entry.COLUMN_NAME_LAUNCH_TYPE + " = " + (int)entry.LaunchType + ", "
                        + Entry.COLUMN_NAME_STATUS_DATE + " = '" + entry.StatusDate.ToString(DATE_TIME_FORMAT) + "', "
                        + Entry.COLUMN_NAME_STATUS + " = " + (int)entry.Status + ", "
                        + Entry.COLUMN_NAME_WORKFLOW_ID + " = " + entry.WorkflowId + ", "
                        + Entry.COLUMN_NAME_JOB_ID + " = '" + (entry.JobId ?? "") + "', "
                        + Entry.COLUMN_NAME_LOGS + " = '" + (entry.Logs ?? "").Replace("'", "''") + "'"
                        + " WHERE "
                        + Entry.COLUMN_NAME_ID + " = " + int.Parse(id) + ";"
                        , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void UpdatePassword(string username, string password)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("UPDATE " + Core.Db.User.DOCUMENT_NAME + " SET "
                        + User.COLUMN_NAME_PASSWORD + " = '" + (password ?? "").Replace("'", "''") + "'"
                        + " WHERE "
                        + User.COLUMN_NAME_USERNAME + " = '" + (username ?? "").Replace("'", "''") + "';"
                        , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void UpdateUser(string id, Core.Db.User user)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("UPDATE " + Core.Db.User.DOCUMENT_NAME + " SET "
                         + User.COLUMN_NAME_USERNAME + " = '" + (user.Username ?? "").Replace("'", "''") + "', "
                         + User.COLUMN_NAME_PASSWORD + " = '" + (user.Password ?? "").Replace("'", "''") + "', "
                         + User.COLUMN_NAME_USER_PROFILE + " = " + (int)user.UserProfile + ", "
                         + User.COLUMN_NAME_EMAIL + " = '" + (user.Email ?? "").Replace("'", "''") + "', "
                         + User.COLUMN_NAME_CREATED_ON + " = '" + user.CreatedOn.ToString(DATE_TIME_FORMAT) + "', "
                         + User.COLUMN_NAME_MODIFIED_ON + " = '" + DateTime.Now.ToString(DATE_TIME_FORMAT) + "'"
                         + " WHERE "
                         + User.COLUMN_NAME_ID + " = " + int.Parse(id) + ";"
                         , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void UpdateUsernameAndEmailAndUserProfile(string userId, string username, string email, UserProfile up)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("UPDATE " + Core.Db.User.DOCUMENT_NAME + " SET "
                        + User.COLUMN_NAME_USERNAME + " = '" + (username ?? "").Replace("'", "''") + "', "
                        + User.COLUMN_NAME_USER_PROFILE + " = " + (int)up + ", "
                        + User.COLUMN_NAME_EMAIL + " = '" + (email ?? "").Replace("'", "''") + "', "
                        + User.COLUMN_NAME_MODIFIED_ON + " = '" + DateTime.Now.ToString(DATE_TIME_FORMAT) + "'"
                        + " WHERE "
                        + User.COLUMN_NAME_ID + " = " + int.Parse(userId) + ";"
                        , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void UpdateWorkflow(string dbId, Core.Db.Workflow workflow)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("UPDATE " + Core.Db.Workflow.DOCUMENT_NAME + " SET "
                        + Workflow.COLUMN_NAME_XML + " = @XML"
                        + " WHERE "
                        + User.COLUMN_NAME_ID + " = " + int.Parse(dbId) + ";"
                        , conn))
                    {
                        command.Parameters.Add("@XML", SqlDbType.VarChar).Value = workflow.Xml;
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override string GetEntryLogs(string entryId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + Entry.COLUMN_NAME_LOGS
                        + " FROM " + Core.Db.Entry.DOCUMENT_NAME
                        + " WHERE "
                        + Entry.COLUMN_NAME_ID + " = " + int.Parse(entryId) + ";"
                        , conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var logs = (string)reader[Entry.COLUMN_NAME_LOGS];
                                return logs;
                            }
                        }
                    }
                }

                return null;
            }
        }

        public override string GetHistoryEntryLogs(string entryId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT " + HistoryEntry.COLUMN_NAME_LOGS
                        + " FROM " + Core.Db.HistoryEntry.DOCUMENT_NAME
                        + " WHERE "
                        + HistoryEntry.COLUMN_NAME_ID + " = " + int.Parse(entryId) + ";"
                        , conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var logs = (string)reader[HistoryEntry.COLUMN_NAME_LOGS];
                                return logs;
                            }
                        }
                    }
                }

                return null;
            }
        }

        public override IEnumerable<Core.Db.User> GetNonRestricedUsers()
        {
            lock (_padlock)
            {
                var users = new List<User>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + User.COLUMN_NAME_ID + ", "
                        + User.COLUMN_NAME_USERNAME + ", "
                        + User.COLUMN_NAME_PASSWORD + ", "
                        + User.COLUMN_NAME_EMAIL + ", "
                        + User.COLUMN_NAME_USER_PROFILE + ", "
                        + User.COLUMN_NAME_CREATED_ON + ", "
                        + User.COLUMN_NAME_MODIFIED_ON
                        + " FROM " + Core.Db.User.DOCUMENT_NAME
                        + " WHERE (" + User.COLUMN_NAME_USER_PROFILE + " = " + (int)UserProfile.SuperAdministrator
                        + " OR " + User.COLUMN_NAME_USER_PROFILE + " = " + (int)UserProfile.Administrator + ")"
                        + " ORDER BY " + User.COLUMN_NAME_USERNAME
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var admin = new User
                                {
                                    Id = (int)reader[User.COLUMN_NAME_ID],
                                    Username = (string)reader[User.COLUMN_NAME_USERNAME],
                                    Password = (string)reader[User.COLUMN_NAME_PASSWORD],
                                    Email = (string)reader[User.COLUMN_NAME_EMAIL],
                                    UserProfile = (UserProfile)(int)reader[User.COLUMN_NAME_USER_PROFILE],
                                    CreatedOn = (DateTime)reader[User.COLUMN_NAME_CREATED_ON],
                                    ModifiedOn = reader[User.COLUMN_NAME_MODIFIED_ON] == DBNull.Value ? DateTime.MinValue : (DateTime)reader[User.COLUMN_NAME_MODIFIED_ON]
                                };

                                users.Add(admin);
                            }
                        }
                    }
                }

                return users;
            }
        }

        public override string InsertRecord(Core.Db.Record record)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("INSERT INTO " + Core.Db.Record.DOCUMENT_NAME + "("
                        + Record.COLUMN_NAME_NAME + ", "
                        + Record.COLUMN_NAME_DESCRIPTION + ", "
                        + Record.COLUMN_NAME_APPROVED + ", "
                        + Record.COLUMN_NAME_START_DATE + ", "
                        + Record.COLUMN_NAME_END_DATE + ", "
                        + Record.COLUMN_NAME_COMMENTS + ", "
                        + Record.COLUMN_NAME_MANAGER_COMMENTS + ", "
                        + Record.COLUMN_NAME_CREATED_BY + ", "
                        + Record.COLUMN_NAME_CREATED_ON + ", "
                        + Record.COLUMN_NAME_MODIFIED_BY + ", "
                        + Record.COLUMN_NAME_MODIFIED_ON + ", "
                        + Record.COLUMN_NAME_ASSIGNED_TO + ", "
                        + Record.COLUMN_NAME_ASSIGNED_ON + ")"
                        + " OUTPUT INSERTED." + Record.COLUMN_NAME_ID
                        + " VALUES("
                        + "'" + (record.Name ?? "").Replace("'", "''") + "'" + ", "
                        + "'" + (record.Description ?? "").Replace("'", "''") + "'" + ", "
                        + (record.Approved ? "1" : "0") + ", "
                        + (record.StartDate == null ? "NULL" : "'" + record.StartDate.Value.ToString(DATE_TIME_FORMAT) + "'") + ", "
                        + (record.EndDate == null ? "NULL" : "'" + record.EndDate.Value.ToString(DATE_TIME_FORMAT) + "'") + ", "
                        + "'" + (record.Comments ?? "").Replace("'", "''") + "'" + ", "
                        + "'" + (record.ManagerComments ?? "").Replace("'", "''") + "'" + ", "
                        + int.Parse(record.CreatedBy) + ", "
                        + "'" + DateTime.Now.ToString(DATE_TIME_FORMAT) + "'" + ", "
                        + (string.IsNullOrEmpty(record.ModifiedBy) ? "NULL" : int.Parse(record.ModifiedBy).ToString()) + ", "
                        + (record.ModifiedOn == null ? "NULL" : "'" + record.ModifiedOn.Value.ToString(DATE_TIME_FORMAT) + "'") + ", "
                         + (string.IsNullOrEmpty(record.AssignedTo) ? "NULL" : int.Parse(record.AssignedTo).ToString()) + ", "
                        + (record.AssignedOn == null ? "NULL" : "'" + record.AssignedOn.Value.ToString(DATE_TIME_FORMAT) + "'") + ")"
                        + ";"
                        , conn))
                    {
                        var id = (int)command.ExecuteScalar();
                        return id.ToString();
                    }
                }
            }
        }

        public override void UpdateRecord(string recordId, Core.Db.Record record)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("UPDATE " + Core.Db.Record.DOCUMENT_NAME + " SET "
                        + Record.COLUMN_NAME_NAME + " = '" + (record.Name ?? "").Replace("'", "''") + "', "
                        + Record.COLUMN_NAME_DESCRIPTION + " = '" + (record.Description ?? "").Replace("'", "''") + "', "
                        + Record.COLUMN_NAME_APPROVED + " = " + (record.Approved ? "1" : "0") + ", "
                        + Record.COLUMN_NAME_START_DATE + " = " + (record.StartDate == null ? "NULL" : "'" + record.StartDate.Value.ToString(DATE_TIME_FORMAT) + "'") + ", "
                        + Record.COLUMN_NAME_END_DATE + " = " + (record.EndDate == null ? "NULL" : "'" + record.EndDate.Value.ToString(DATE_TIME_FORMAT) + "'") + ", "
                        + Record.COLUMN_NAME_COMMENTS + " = '" + (record.Comments ?? "").Replace("'", "''") + "', "
                        + Record.COLUMN_NAME_MANAGER_COMMENTS + " = '" + (record.ManagerComments ?? "").Replace("'", "''") + "', "
                        + Record.COLUMN_NAME_CREATED_BY + " = " + int.Parse(record.CreatedBy) + ", "
                        + Record.COLUMN_NAME_MODIFIED_BY + " = " + (string.IsNullOrEmpty(record.ModifiedBy) ? "NULL" : int.Parse(record.ModifiedBy).ToString()) + ", "
                        + Record.COLUMN_NAME_MODIFIED_ON + " = '" + DateTime.Now.ToString(DATE_TIME_FORMAT) + "', "
                        + Record.COLUMN_NAME_ASSIGNED_TO + " = " + (string.IsNullOrEmpty(record.AssignedTo) ? "NULL" : int.Parse(record.AssignedTo).ToString()) + ", "
                        + Record.COLUMN_NAME_ASSIGNED_ON + " = " + (record.AssignedOn == null ? "NULL" : "'" + record.AssignedOn.Value.ToString(DATE_TIME_FORMAT) + "'")
                        + " WHERE "
                        + Record.COLUMN_NAME_ID + " = " + int.Parse(recordId) + ";"
                        , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DeleteRecords(string[] recordIds)
        {
            lock (_padlock)
            {
                if (recordIds.Length > 0)
                {
                    using (var conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();

                        var builder = new StringBuilder("(");

                        for (var i = 0; i < recordIds.Length; i++)
                        {
                            var id = recordIds[i];
                            _ = builder.Append(id);
                            _ = i < recordIds.Length - 1 ? builder.Append(", ") : builder.Append(')');
                        }

                        using (var command = new SqlCommand("DELETE FROM " + Core.Db.Record.DOCUMENT_NAME
                            + " WHERE " + Record.COLUMN_NAME_ID + " IN " + builder + ";", conn))
                        {
                            _ = command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public override Core.Db.Record GetRecord(string id)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + Record.COLUMN_NAME_ID + ", "
                        + Record.COLUMN_NAME_NAME + ", "
                        + Record.COLUMN_NAME_DESCRIPTION + ", "
                        + Record.COLUMN_NAME_APPROVED + ", "
                        + Record.COLUMN_NAME_START_DATE + ", "
                        + Record.COLUMN_NAME_END_DATE + ", "
                        + Record.COLUMN_NAME_COMMENTS + ", "
                        + Record.COLUMN_NAME_MANAGER_COMMENTS + ", "
                        + Record.COLUMN_NAME_CREATED_BY + ", "
                        + Record.COLUMN_NAME_CREATED_ON + ", "
                        + Record.COLUMN_NAME_MODIFIED_BY + ", "
                        + Record.COLUMN_NAME_MODIFIED_ON + ", "
                        + Record.COLUMN_NAME_ASSIGNED_TO + ", "
                        + Record.COLUMN_NAME_ASSIGNED_ON
                        + " FROM " + Core.Db.Record.DOCUMENT_NAME
                        + " WHERE " + Record.COLUMN_NAME_ID + " = " + int.Parse(id)
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var record = new Record
                                {
                                    Id = (int)reader[Record.COLUMN_NAME_ID],
                                    Name = (string)reader[Record.COLUMN_NAME_NAME],
                                    Description = (string)reader[Record.COLUMN_NAME_DESCRIPTION],
                                    Approved = (bool)reader[Record.COLUMN_NAME_APPROVED],
                                    StartDate = reader[Record.COLUMN_NAME_START_DATE] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_START_DATE],
                                    EndDate = reader[Record.COLUMN_NAME_END_DATE] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_END_DATE],
                                    Comments = (string)reader[Record.COLUMN_NAME_COMMENTS],
                                    ManagerComments = (string)reader[Record.COLUMN_NAME_MANAGER_COMMENTS],
                                    CreatedBy = ((int)reader[Record.COLUMN_NAME_CREATED_BY]).ToString(),
                                    CreatedOn = (DateTime)reader[Record.COLUMN_NAME_CREATED_ON],
                                    ModifiedBy = reader[Record.COLUMN_NAME_MODIFIED_BY] == DBNull.Value ? string.Empty : ((int)reader[Record.COLUMN_NAME_MODIFIED_BY]).ToString(),
                                    ModifiedOn = reader[Record.COLUMN_NAME_MODIFIED_ON] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_MODIFIED_ON],
                                    AssignedTo = reader[Record.COLUMN_NAME_ASSIGNED_TO] == DBNull.Value ? string.Empty : ((int)reader[Record.COLUMN_NAME_ASSIGNED_TO]).ToString(),
                                    AssignedOn = reader[Record.COLUMN_NAME_ASSIGNED_ON] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_ASSIGNED_ON]
                                };

                                return record;
                            }
                        }
                    }
                }

                return null;
            }
        }

        public override IEnumerable<Core.Db.Record> GetRecords(string keyword)
        {
            lock (_padlock)
            {
                var records = new List<Record>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + Record.COLUMN_NAME_ID + ", "
                        + Record.COLUMN_NAME_NAME + ", "
                        + Record.COLUMN_NAME_DESCRIPTION + ", "
                        + Record.COLUMN_NAME_APPROVED + ", "
                        + Record.COLUMN_NAME_START_DATE + ", "
                        + Record.COLUMN_NAME_END_DATE + ", "
                        + Record.COLUMN_NAME_COMMENTS + ", "
                        + Record.COLUMN_NAME_MANAGER_COMMENTS + ", "
                        + Record.COLUMN_NAME_CREATED_BY + ", "
                        + Record.COLUMN_NAME_CREATED_ON + ", "
                        + Record.COLUMN_NAME_MODIFIED_BY + ", "
                        + Record.COLUMN_NAME_MODIFIED_ON + ", "
                        + Record.COLUMN_NAME_ASSIGNED_TO + ", "
                        + Record.COLUMN_NAME_ASSIGNED_ON
                        + " FROM " + Core.Db.Record.DOCUMENT_NAME
                        + " WHERE " + "LOWER(" + Record.COLUMN_NAME_NAME + ")" + " LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " OR " + "LOWER(" + Record.COLUMN_NAME_DESCRIPTION + ")" + " LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " ORDER BY " + Record.COLUMN_NAME_CREATED_ON + " DESC"
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var record = new Record
                                {
                                    Id = (int)reader[Record.COLUMN_NAME_ID],
                                    Name = (string)reader[Record.COLUMN_NAME_NAME],
                                    Description = (string)reader[Record.COLUMN_NAME_DESCRIPTION],
                                    Approved = (bool)reader[Record.COLUMN_NAME_APPROVED],
                                    StartDate = reader[Record.COLUMN_NAME_START_DATE] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_START_DATE],
                                    EndDate = reader[Record.COLUMN_NAME_END_DATE] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_END_DATE],
                                    Comments = (string)reader[Record.COLUMN_NAME_COMMENTS],
                                    ManagerComments = (string)reader[Record.COLUMN_NAME_MANAGER_COMMENTS],
                                    CreatedBy = ((int)reader[Record.COLUMN_NAME_CREATED_BY]).ToString(),
                                    CreatedOn = (DateTime)reader[Record.COLUMN_NAME_CREATED_ON],
                                    ModifiedBy = reader[Record.COLUMN_NAME_MODIFIED_BY] == DBNull.Value ? string.Empty : ((int)reader[Record.COLUMN_NAME_MODIFIED_BY]).ToString(),
                                    ModifiedOn = reader[Record.COLUMN_NAME_MODIFIED_ON] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_MODIFIED_ON],
                                    AssignedTo = reader[Record.COLUMN_NAME_ASSIGNED_TO] == DBNull.Value ? string.Empty : ((int)reader[Record.COLUMN_NAME_ASSIGNED_TO]).ToString(),
                                    AssignedOn = reader[Record.COLUMN_NAME_ASSIGNED_ON] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_ASSIGNED_ON]
                                };

                                records.Add(record);
                            }
                        }
                    }
                }

                return records;
            }
        }

        public override IEnumerable<Core.Db.Record> GetRecordsCreatedBy(string createdBy)
        {
            lock (_padlock)
            {
                var records = new List<Record>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + Record.COLUMN_NAME_ID + ", "
                        + Record.COLUMN_NAME_NAME + ", "
                        + Record.COLUMN_NAME_DESCRIPTION + ", "
                        + Record.COLUMN_NAME_APPROVED + ", "
                        + Record.COLUMN_NAME_START_DATE + ", "
                        + Record.COLUMN_NAME_END_DATE + ", "
                        + Record.COLUMN_NAME_COMMENTS + ", "
                        + Record.COLUMN_NAME_MANAGER_COMMENTS + ", "
                        + Record.COLUMN_NAME_CREATED_BY + ", "
                        + Record.COLUMN_NAME_CREATED_ON + ", "
                        + Record.COLUMN_NAME_MODIFIED_BY + ", "
                        + Record.COLUMN_NAME_MODIFIED_ON + ", "
                        + Record.COLUMN_NAME_ASSIGNED_TO + ", "
                        + Record.COLUMN_NAME_ASSIGNED_ON
                        + " FROM " + Core.Db.Record.DOCUMENT_NAME
                        + " WHERE " + Record.COLUMN_NAME_CREATED_BY + " = " + int.Parse(createdBy)
                        + " ORDER BY " + Record.COLUMN_NAME_NAME + " ASC"
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var record = new Record
                                {
                                    Id = (int)reader[Record.COLUMN_NAME_ID],
                                    Name = (string)reader[Record.COLUMN_NAME_NAME],
                                    Description = (string)reader[Record.COLUMN_NAME_DESCRIPTION],
                                    Approved = (bool)reader[Record.COLUMN_NAME_APPROVED],
                                    StartDate = reader[Record.COLUMN_NAME_START_DATE] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_START_DATE],
                                    EndDate = reader[Record.COLUMN_NAME_END_DATE] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_END_DATE],
                                    Comments = (string)reader[Record.COLUMN_NAME_COMMENTS],
                                    ManagerComments = (string)reader[Record.COLUMN_NAME_MANAGER_COMMENTS],
                                    CreatedBy = ((int)reader[Record.COLUMN_NAME_CREATED_BY]).ToString(),
                                    CreatedOn = (DateTime)reader[Record.COLUMN_NAME_CREATED_ON],
                                    ModifiedBy = reader[Record.COLUMN_NAME_MODIFIED_BY] == DBNull.Value ? string.Empty : ((int)reader[Record.COLUMN_NAME_MODIFIED_BY]).ToString(),
                                    ModifiedOn = reader[Record.COLUMN_NAME_MODIFIED_ON] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_MODIFIED_ON],
                                    AssignedTo = reader[Record.COLUMN_NAME_ASSIGNED_TO] == DBNull.Value ? string.Empty : ((int)reader[Record.COLUMN_NAME_ASSIGNED_TO]).ToString(),
                                    AssignedOn = reader[Record.COLUMN_NAME_ASSIGNED_ON] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_ASSIGNED_ON]
                                };

                                records.Add(record);
                            }
                        }
                    }
                }

                return records;
            }
        }

        public override IEnumerable<Core.Db.Record> GetRecordsCreatedByOrAssignedTo(string createdBy, string assingedTo, string keyword)
        {
            lock (_padlock)
            {
                var records = new List<Record>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + Record.COLUMN_NAME_ID + ", "
                        + Record.COLUMN_NAME_NAME + ", "
                        + Record.COLUMN_NAME_DESCRIPTION + ", "
                        + Record.COLUMN_NAME_APPROVED + ", "
                        + Record.COLUMN_NAME_START_DATE + ", "
                        + Record.COLUMN_NAME_END_DATE + ", "
                        + Record.COLUMN_NAME_COMMENTS + ", "
                        + Record.COLUMN_NAME_MANAGER_COMMENTS + ", "
                        + Record.COLUMN_NAME_CREATED_BY + ", "
                        + Record.COLUMN_NAME_CREATED_ON + ", "
                        + Record.COLUMN_NAME_MODIFIED_BY + ", "
                        + Record.COLUMN_NAME_MODIFIED_ON + ", "
                        + Record.COLUMN_NAME_ASSIGNED_TO + ", "
                        + Record.COLUMN_NAME_ASSIGNED_ON
                        + " FROM " + Core.Db.Record.DOCUMENT_NAME
                        + " WHERE " + "(LOWER(" + Record.COLUMN_NAME_NAME + ")" + " LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " OR " + "LOWER(" + Record.COLUMN_NAME_DESCRIPTION + ")" + " LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%')"
                        + " AND (" + Record.COLUMN_NAME_CREATED_BY + " = " + int.Parse(createdBy) + " OR " + Record.COLUMN_NAME_ASSIGNED_TO + " = " + int.Parse(assingedTo) + ")"
                        + " ORDER BY " + Record.COLUMN_NAME_CREATED_ON + " DESC"
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var record = new Record
                                {
                                    Id = (int)reader[Record.COLUMN_NAME_ID],
                                    Name = (string)reader[Record.COLUMN_NAME_NAME],
                                    Description = (string)reader[Record.COLUMN_NAME_DESCRIPTION],
                                    Approved = (bool)reader[Record.COLUMN_NAME_APPROVED],
                                    StartDate = reader[Record.COLUMN_NAME_START_DATE] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_START_DATE],
                                    EndDate = reader[Record.COLUMN_NAME_END_DATE] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_END_DATE],
                                    Comments = (string)reader[Record.COLUMN_NAME_COMMENTS],
                                    ManagerComments = (string)reader[Record.COLUMN_NAME_MANAGER_COMMENTS],
                                    CreatedBy = ((int)reader[Record.COLUMN_NAME_CREATED_BY]).ToString(),
                                    CreatedOn = (DateTime)reader[Record.COLUMN_NAME_CREATED_ON],
                                    ModifiedBy = reader[Record.COLUMN_NAME_MODIFIED_BY] == DBNull.Value ? string.Empty : ((int)reader[Record.COLUMN_NAME_MODIFIED_BY]).ToString(),
                                    ModifiedOn = reader[Record.COLUMN_NAME_MODIFIED_ON] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_MODIFIED_ON],
                                    AssignedTo = reader[Record.COLUMN_NAME_ASSIGNED_TO] == DBNull.Value ? string.Empty : ((int)reader[Record.COLUMN_NAME_ASSIGNED_TO]).ToString(),
                                    AssignedOn = reader[Record.COLUMN_NAME_ASSIGNED_ON] == DBNull.Value ? null : (DateTime?)reader[Record.COLUMN_NAME_ASSIGNED_ON]
                                };

                                records.Add(record);
                            }
                        }
                    }
                }

                return records;
            }
        }

        public override string InsertVersion(Core.Db.Version version)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("INSERT INTO " + Core.Db.Version.DOCUMENT_NAME + "("
                        + Version.COLUMN_NAME_RECORD_ID + ", "
                        + Version.COLUMN_NAME_FILE_PATH + ", "
                        + Version.COLUMN_NAME_CREATED_ON + ")"
                        + " OUTPUT INSERTED." + Version.COLUMN_NAME_ID
                        + " VALUES("
                        + int.Parse(version.RecordId) + ", "
                        + "'" + (version.FilePath ?? "").Replace("'", "''") + "'" + ", "
                        + "'" + DateTime.Now.ToString(DATE_TIME_FORMAT) + "'" + ")"
                        + ";"
                        , conn))
                    {
                        var id = (int)command.ExecuteScalar();
                        return id.ToString();
                    }
                }
            }
        }

        public override void UpdateVersion(string versionId, Core.Db.Version version)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("UPDATE " + Core.Db.Version.DOCUMENT_NAME + " SET "
                        + Version.COLUMN_NAME_RECORD_ID + " = " + int.Parse(version.RecordId) + ", "
                        + Version.COLUMN_NAME_FILE_PATH + " = '" + (version.FilePath ?? "").Replace("'", "''") + "'"
                        + " WHERE "
                        + Version.COLUMN_NAME_ID + " = " + int.Parse(versionId) + ";"
                        , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DeleteVersions(string[] versionIds)
        {
            lock (_padlock)
            {
                if (versionIds.Length > 0)
                {
                    using (var conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();

                        var builder = new StringBuilder("(");

                        for (var i = 0; i < versionIds.Length; i++)
                        {
                            var id = versionIds[i];
                            _ = builder.Append(id);
                            _ = i < versionIds.Length - 1 ? builder.Append(", ") : builder.Append(')');
                        }

                        using (var command = new SqlCommand("DELETE FROM " + Core.Db.Version.DOCUMENT_NAME
                            + " WHERE " + Version.COLUMN_NAME_ID + " IN " + builder + ";", conn))
                        {
                            _ = command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public override IEnumerable<Core.Db.Version> GetVersions(string recordId)
        {
            lock (_padlock)
            {
                var versions = new List<Version>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + Version.COLUMN_NAME_ID + ", "
                        + Version.COLUMN_NAME_RECORD_ID + ", "
                        + Version.COLUMN_NAME_FILE_PATH + ", "
                        + Version.COLUMN_NAME_CREATED_ON
                        + " FROM " + Core.Db.Version.DOCUMENT_NAME
                        + " WHERE " + Version.COLUMN_NAME_RECORD_ID + " = " + int.Parse(recordId)
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var version = new Version
                                {
                                    Id = (int)reader[Version.COLUMN_NAME_ID],
                                    RecordId = ((int)reader[Version.COLUMN_NAME_RECORD_ID]).ToString(),
                                    FilePath = (string)reader[Version.COLUMN_NAME_FILE_PATH],
                                    CreatedOn = (DateTime)reader[Version.COLUMN_NAME_CREATED_ON]
                                };

                                versions.Add(version);
                            }
                        }
                    }
                }

                return versions;
            }
        }

        public override Core.Db.Version GetLatestVersion(string recordId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT TOP 1 "
                        + Version.COLUMN_NAME_ID + ", "
                        + Version.COLUMN_NAME_RECORD_ID + ", "
                        + Version.COLUMN_NAME_FILE_PATH + ", "
                        + Version.COLUMN_NAME_CREATED_ON
                        + " FROM " + Core.Db.Version.DOCUMENT_NAME
                        + " WHERE " + Version.COLUMN_NAME_RECORD_ID + " = " + int.Parse(recordId)
                        + " ORDER BY " + Version.COLUMN_NAME_CREATED_ON + " DESC"
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var version = new Version
                                {
                                    Id = (int)reader[Version.COLUMN_NAME_ID],
                                    RecordId = ((int)reader[Version.COLUMN_NAME_RECORD_ID]).ToString(),
                                    FilePath = (string)reader[Version.COLUMN_NAME_FILE_PATH],
                                    CreatedOn = (DateTime)reader[Version.COLUMN_NAME_CREATED_ON]
                                };

                                return version;
                            }
                        }
                    }
                }

                return null;
            }
        }

        public override string InsertNotification(Core.Db.Notification notification)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("INSERT INTO " + Core.Db.Notification.DOCUMENT_NAME + "("
                        + Notification.COLUMN_NAME_ASSIGNED_BY + ", "
                        + Notification.COLUMN_NAME_ASSIGNED_ON + ", "
                        + Notification.COLUMN_NAME_ASSIGNED_TO + ", "
                        + Notification.COLUMN_NAME_MESSAGE + ", "
                        + Notification.COLUMN_NAME_IS_READ + ")"
                        + " OUTPUT INSERTED." + Notification.COLUMN_NAME_ID
                        + " VALUES("
                        + (!string.IsNullOrEmpty(notification.AssignedBy) ? int.Parse(notification.AssignedBy).ToString() : "NULL") + ", "
                        + "'" + notification.AssignedOn.ToString(DATE_TIME_FORMAT) + "'" + ", "
                        + (!string.IsNullOrEmpty(notification.AssignedTo) ? int.Parse(notification.AssignedTo).ToString() : "NULL") + ", "
                        + "'" + (notification.Message ?? "").Replace("'", "''") + "'" + ", "
                        + (notification.IsRead ? "1" : "0") + ")"
                        + ";"
                        , conn))
                    {
                        var id = (int)command.ExecuteScalar();
                        return id.ToString();
                    }
                }
            }
        }

        public override void MarkNotificationsAsRead(string[] notificationIds)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var builder = new StringBuilder("(");

                    for (var i = 0; i < notificationIds.Length; i++)
                    {
                        var id = notificationIds[i];
                        _ = builder.Append(id);
                        _ = i < notificationIds.Length - 1 ? builder.Append(", ") : builder.Append(')');
                    }

                    using (var command = new SqlCommand("UPDATE " + Core.Db.Notification.DOCUMENT_NAME
                        + " SET " + Notification.COLUMN_NAME_IS_READ + " = " + "1"
                        + " WHERE " + Notification.COLUMN_NAME_ID + " IN " + builder + ";", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void MarkNotificationsAsUnread(string[] notificationIds)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var builder = new StringBuilder("(");

                    for (var i = 0; i < notificationIds.Length; i++)
                    {
                        var id = notificationIds[i];
                        _ = builder.Append(id);
                        _ = i < notificationIds.Length - 1 ? builder.Append(", ") : builder.Append(')');
                    }

                    using (var command = new SqlCommand("UPDATE " + Core.Db.Notification.DOCUMENT_NAME
                        + " SET " + Notification.COLUMN_NAME_IS_READ + " = " + "0"
                        + " WHERE " + Notification.COLUMN_NAME_ID + " IN " + builder + ";", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DeleteNotifications(string[] notificationIds)
        {
            lock (_padlock)
            {
                if (notificationIds.Length > 0)
                {
                    using (var conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();

                        var builder = new StringBuilder("(");

                        for (var i = 0; i < notificationIds.Length; i++)
                        {
                            var id = notificationIds[i];
                            _ = builder.Append(id);
                            _ = i < notificationIds.Length - 1 ? builder.Append(", ") : builder.Append(')');
                        }

                        using (var command = new SqlCommand("DELETE FROM " + Core.Db.Notification.DOCUMENT_NAME
                            + " WHERE " + Notification.COLUMN_NAME_ID + " IN " + builder + ";", conn))
                        {
                            _ = command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public override IEnumerable<Core.Db.Notification> GetNotifications(string assignedTo, string keyword)
        {
            lock (_padlock)
            {
                var notifications = new List<Notification>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + Notification.COLUMN_NAME_ID + ", "
                        + Notification.COLUMN_NAME_ASSIGNED_BY + ", "
                        + Notification.COLUMN_NAME_ASSIGNED_ON + ", "
                        + Notification.COLUMN_NAME_ASSIGNED_TO + ", "
                        + Notification.COLUMN_NAME_MESSAGE + ", "
                        + Notification.COLUMN_NAME_IS_READ
                        + " FROM " + Core.Db.Notification.DOCUMENT_NAME
                        + " WHERE " + "(LOWER(" + Notification.COLUMN_NAME_MESSAGE + ")" + " LIKE '%" + (keyword ?? "").Replace("'", "''").ToLower() + "%'"
                        + " AND " + Notification.COLUMN_NAME_ASSIGNED_TO + " = " + int.Parse(assignedTo) + ")"
                        + " ORDER BY " + Notification.COLUMN_NAME_ASSIGNED_ON + " DESC"
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var notification = new Notification
                                {
                                    Id = (int)reader[Notification.COLUMN_NAME_ID],
                                    AssignedBy = ((int)reader[Notification.COLUMN_NAME_ASSIGNED_BY]).ToString(),
                                    AssignedOn = (DateTime)reader[Notification.COLUMN_NAME_ASSIGNED_ON],
                                    AssignedTo = ((int)reader[Notification.COLUMN_NAME_ASSIGNED_TO]).ToString(),
                                    Message = (string)reader[Notification.COLUMN_NAME_MESSAGE],
                                    IsRead = (bool)reader[Notification.COLUMN_NAME_IS_READ]
                                };

                                notifications.Add(notification);
                            }
                        }
                    }
                }

                return notifications;
            }
        }

        public override bool HasNotifications(string assignedTo)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT COUNT(*)"
                        + " FROM " + Core.Db.Notification.DOCUMENT_NAME
                        + " WHERE (" + Notification.COLUMN_NAME_ASSIGNED_TO + " = " + int.Parse(assignedTo)
                        + " AND " + Notification.COLUMN_NAME_IS_READ + " = " + "0" + ")"
                        + ";", conn))
                    {
                        var count = (int)command.ExecuteScalar();
                        var hasNotifications = count > 0;
                        return hasNotifications;
                    }
                }
            }
        }

        public override string InsertApprover(Core.Db.Approver approver)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("INSERT INTO " + Core.Db.Approver.DOCUMENT_NAME + "("
                        + Approver.COLUMN_NAME_USER_ID + ", "
                        + Approver.COLUMN_NAME_RECORD_ID + ", "
                        + Approver.COLUMN_NAME_APPROVED + ", "
                        + Approver.COLUMN_NAME_APPROVED_ON + ")"
                        + " OUTPUT INSERTED." + Approver.COLUMN_NAME_ID
                        + " VALUES("
                        + int.Parse(approver.UserId) + ", "
                        + int.Parse(approver.RecordId) + ", "
                        + (approver.Approved ? "1" : "0") + ", "
                        + (approver.ApprovedOn == null ? "NULL" : "'" + approver.ApprovedOn.Value.ToString(DATE_TIME_FORMAT) + "'") + ")"
                        + ";"
                        , conn))
                    {
                        var id = (int)command.ExecuteScalar();
                        return id.ToString();
                    }
                }
            }
        }

        public override void UpdateApprover(string approverId, Core.Db.Approver approver)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("UPDATE " + Core.Db.Approver.DOCUMENT_NAME + " SET "
                        + Approver.COLUMN_NAME_USER_ID + " = " + int.Parse(approver.UserId) + ", "
                        + Approver.COLUMN_NAME_RECORD_ID + " = " + int.Parse(approver.RecordId) + ", "
                        + Approver.COLUMN_NAME_APPROVED + " = " + (approver.Approved ? "1" : "0") + ", "
                        + Approver.COLUMN_NAME_APPROVED_ON + " = " + (approver.ApprovedOn == null ? "NULL" : "'" + approver.ApprovedOn.Value.ToString(DATE_TIME_FORMAT) + "'")
                        + " WHERE "
                        + Approver.COLUMN_NAME_ID + " = " + int.Parse(approverId) + ";"
                        , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DeleteApproversByRecordId(string recordId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("DELETE FROM " + Core.Db.Approver.DOCUMENT_NAME
                        + " WHERE " + Approver.COLUMN_NAME_RECORD_ID + " = " + int.Parse(recordId) + ";", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DeleteApprovedApprovers(string recordId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("DELETE FROM " + Core.Db.Approver.DOCUMENT_NAME
                        + " WHERE " + Approver.COLUMN_NAME_RECORD_ID + " = " + int.Parse(recordId)
                        + " AND " + Approver.COLUMN_NAME_APPROVED + " = " + "1"
                        + ";"
                        , conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void DeleteApproversByUserId(string userId)
        {
            lock (_padlock)
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("DELETE FROM " + Core.Db.Approver.DOCUMENT_NAME
                        + " WHERE " + Approver.COLUMN_NAME_USER_ID + " = " + int.Parse(userId) + ";", conn))
                    {
                        _ = command.ExecuteNonQuery();
                    }
                }
            }
        }

        public override IEnumerable<Core.Db.Approver> GetApprovers(string recordId)
        {
            lock (_padlock)
            {
                var approvers = new List<Approver>();

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    using (var command = new SqlCommand("SELECT "
                        + Approver.COLUMN_NAME_ID + ", "
                        + Approver.COLUMN_NAME_USER_ID + ", "
                        + Approver.COLUMN_NAME_RECORD_ID + ", "
                        + Approver.COLUMN_NAME_APPROVED + ", "
                        + Approver.COLUMN_NAME_APPROVED_ON
                        + " FROM " + Core.Db.Approver.DOCUMENT_NAME
                        + " WHERE " + Approver.COLUMN_NAME_RECORD_ID + " = " + int.Parse(recordId)
                        + ";", conn))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var approver = new Approver
                                {
                                    Id = (int)reader[Approver.COLUMN_NAME_ID],
                                    UserId = ((int)reader[Approver.COLUMN_NAME_USER_ID]).ToString(),
                                    RecordId = ((int)reader[Approver.COLUMN_NAME_RECORD_ID]).ToString(),
                                    Approved = (bool)reader[Approver.COLUMN_NAME_APPROVED],
                                    ApprovedOn = reader[Approver.COLUMN_NAME_APPROVED_ON] == DBNull.Value ? null : (DateTime?)reader[Approver.COLUMN_NAME_APPROVED_ON]
                                };

                                approvers.Add(approver);
                            }
                        }
                    }
                }

                return approvers;
            }
        }

        public override void Dispose()
        {
        }
    }
}

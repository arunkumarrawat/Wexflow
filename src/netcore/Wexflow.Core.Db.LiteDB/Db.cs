﻿using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Wexflow.Core.Db.LiteDB
{
    public sealed class Db : Core.Db.Db
    {
        private static readonly object _padlock = new object();
        private static LiteDatabase _db;

        public Db(string connectionString) : base(connectionString)
        {
            _db = new LiteDatabase(ConnectionString);
            _ = _db.Rebuild(new RebuildOptions { Collation = new Collation("/None") }); // /IgnoreCase, en-US/None, en-US/IgnoreCase, en-US/IgnoreCase,IgnoreSymbols
        }

        public override void Init()
        {
            // StatusCount
            ClearStatusCount();

            var statusCountCol = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);

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

            _ = statusCountCol.Insert(statusCount);

            // Entries
            ClearEntries();

            // Insert admin user if it does not exist
            // Backward compatibility: update admin password from MD5 hash to SHA256 hash of "wexflow2018"
            var usersCol = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
            var user = usersCol.FindOne(u => u.Username == "admin");
            if (user == null)
            {
                InsertDefaultUser();
            }
            else
            {
                if (IsMd5(user.Password) || IsSha256(user.Password))
                {
                    user.Password = HashPassword("wexflow2018");
                    _ = usersCol.Update(user);
                }
            }
        }

        public override void ClearStatusCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                _ = col.DeleteMany(_ => true);
            }
        }

        public override void ClearEntries()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Entry>(Core.Db.Entry.DOCUMENT_NAME);
                _ = col.DeleteMany(_ => true);
            }
        }

        public override Core.Db.StatusCount GetStatusCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                var statusCount = col.FindAll().FirstOrDefault();
                return statusCount;
            }
        }

        public override void IncrementPendingCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                var statusCount = col.FindAll().FirstOrDefault();
                if (statusCount != null)
                {
                    statusCount.PendingCount++;
                    _ = col.Update(statusCount);
                }
            }
        }

        public override void DecrementPendingCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                var statusCount = col.FindAll().FirstOrDefault();
                if (statusCount != null)
                {
                    statusCount.PendingCount--;
                    _ = col.Update(statusCount);
                }
            }
        }

        public override void IncrementRunningCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                var statusCount = col.FindAll().FirstOrDefault();
                if (statusCount != null)
                {
                    statusCount.RunningCount++;
                    _ = col.Update(statusCount);
                }
            }
        }

        public override void DecrementRunningCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                var statusCount = col.FindAll().FirstOrDefault();
                if (statusCount != null)
                {
                    statusCount.RunningCount--;
                    _ = col.Update(statusCount);
                }
            }
        }

        public override void IncrementDoneCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                var statusCount = col.FindAll().FirstOrDefault();
                if (statusCount != null)
                {
                    statusCount.DoneCount++;
                    _ = col.Update(statusCount);
                }
            }
        }

        /*
                public void DecrementDoneCount()
                {
                    lock (Padlock)
                    {
                        var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DocumentName);
                        var statusCount = col.FindAll().FirstOrDefault();
                        if (statusCount != null)
                        {
                            statusCount.DoneCount--;
                            _ = col.Update(statusCount);
                        }
                    }
                }
        */

        public override void IncrementFailedCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                var statusCount = col.FindAll().FirstOrDefault();
                if (statusCount != null)
                {
                    statusCount.FailedCount++;
                    _ = col.Update(statusCount);
                }
            }
        }

        public override void IncrementRejectedCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                var statusCount = col.FindAll().FirstOrDefault();
                if (statusCount != null)
                {
                    statusCount.RejectedCount++;
                    _ = col.Update(statusCount);
                }
            }
        }

        /*
                public void DecrementFailedCount()
                {
                    lock (Padlock)
                    {
                        var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DocumentName);
                        var statusCount = col.FindAll().FirstOrDefault();
                        if (statusCount != null)
                        {
                            statusCount.FailedCount--;
                            _ = col.Update(statusCount);
                        }
                    }
                }
        */

        public override void IncrementWarningCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                var statusCount = col.FindAll().FirstOrDefault();
                if (statusCount != null)
                {
                    statusCount.WarningCount++;
                    _ = col.Update(statusCount);
                }
            }
        }

        /*
                public void DecrementWarningCount()
                {
                    lock (Padlock)
                    {
                        var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DocumentName);
                        var statusCount = col.FindAll().FirstOrDefault();
                        if (statusCount != null)
                        {
                            statusCount.WarningCount--;
                            _ = col.Update(statusCount);
                        }
                    }
                }
        */

        public override void IncrementDisabledCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                var statusCount = col.FindAll().FirstOrDefault();
                if (statusCount != null)
                {
                    statusCount.DisabledCount++;
                    _ = col.Update(statusCount);
                }
            }
        }

        /*
                public void DecrementDisabledCount()
                {
                    lock (Padlock)
                    {
                        var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DocumentName);
                        var statusCount = col.FindAll().FirstOrDefault();
                        if (statusCount != null)
                        {
                            statusCount.DisabledCount--;
                            _ = col.Update(statusCount);
                        }
                    }
                }
        */

        public override void IncrementStoppedCount()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DOCUMENT_NAME);
                var statusCount = col.FindAll().FirstOrDefault();
                if (statusCount != null)
                {
                    statusCount.StoppedCount++;
                    _ = col.Update(statusCount);
                }
            }
        }

        /*
                public void DecrementStoppedCount()
                {
                    lock (Padlock)
                    {
                        var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DocumentName);
                        var statusCount = col.FindAll().FirstOrDefault();
                        if (statusCount != null)
                        {
                            statusCount.StoppedCount--;
                            _ = col.Update(statusCount);
                        }
                    }
                }
        */

        /*
                public void ResetStatusCount()
                {
                    lock (Padlock)
                    {
                        var col = _db.GetCollection<StatusCount>(Core.Db.StatusCount.DocumentName);
                        var statusCount = col.FindAll().FirstOrDefault();
                        if (statusCount != null)
                        {
                            statusCount.PendingCount = 0;
                            statusCount.RunningCount = 0;
                            statusCount.DoneCount = 0;
                            statusCount.FailedCount = 0;
                            statusCount.WarningCount = 0;
                            statusCount.DisabledCount = 0;
                            statusCount.RejectedCount = 0;
                            _ = col.Update(statusCount);
                        }
                    }
                }
        */

        public override IEnumerable<Core.Db.Entry> GetEntries()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Entry>(Core.Db.Entry.DOCUMENT_NAME);
                return col.FindAll();
            }
        }

        public override Core.Db.Entry GetEntry(int workflowId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Entry>(Core.Db.Entry.DOCUMENT_NAME);
                //return col.FindOne(e => e.WorkflowId == WorkflowId);
                var query = Query.All("StatusDate", Query.Descending);
                query.Where.Add(Query.EQ("WorkflowId", workflowId));

                return col.FindOne(query);
            }
        }

        public override Core.Db.Entry GetEntry(int workflowId, Guid jobId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Entry>(Core.Db.Entry.DOCUMENT_NAME);
                return col.FindOne(e => e.WorkflowId == workflowId && e.JobId == jobId.ToString());
            }
        }

        public override void InsertEntry(Core.Db.Entry entry)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Entry>(Core.Db.Entry.DOCUMENT_NAME);
                var ie = new Entry
                {
                    Description = entry.Description,
                    LaunchType = entry.LaunchType,
                    Name = entry.Name,
                    Status = entry.Status,
                    StatusDate = entry.StatusDate,
                    WorkflowId = entry.WorkflowId,
                    JobId = entry.JobId,
                    Logs = entry.Logs
                };
                _ = col.Insert(ie);
                _ = col.EnsureIndex(e => e.WorkflowId);
                _ = col.EnsureIndex(e => e.JobId);
                //col.EnsureIndex(e => e.Name, "LOWER($.Name)");
                _ = col.EnsureIndex(e => e.Name);
                _ = col.EnsureIndex(e => e.LaunchType);
                //col.EnsureIndex(e => e.Description, "LOWER($.Name)");
                _ = col.EnsureIndex(e => e.Description);
                _ = col.EnsureIndex(e => e.Status);
                _ = col.EnsureIndex(e => e.StatusDate);
            }
        }

        public override void UpdateEntry(string id, Core.Db.Entry entry)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Entry>(Core.Db.Entry.DOCUMENT_NAME);
                var e = new Entry
                {
                    Id = int.Parse(id),
                    Description = entry.Description,
                    LaunchType = entry.LaunchType,
                    Name = entry.Name,
                    Status = entry.Status,
                    StatusDate = entry.StatusDate,
                    WorkflowId = entry.WorkflowId,
                    JobId = entry.JobId,
                    Logs = entry.Logs
                };
                _ = col.Update(e);
            }
        }

        /*
                public void DeleteEntry(int workflowId)
                {
                    lock (Padlock)
                    {
                        var col = _db.GetCollection<Entry>(Core.Db.Entry.DocumentName);
                        _ = col.DeleteMany(e => e.WorkflowId == workflowId);
                    }
                }
        */

        public override void InsertUser(Core.Db.User user)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                user.CreatedOn = DateTime.Now;
                _ = col.Insert(new User
                {
                    CreatedOn = user.CreatedOn,
                    Email = user.Email,
                    ModifiedOn = user.ModifiedOn,
                    Password = user.Password,
                    Username = user.Username,
                    UserProfile = user.UserProfile
                });
                //col.EnsureIndex(u => u.Username, "LOWER($.Username)");
                _ = col.EnsureIndex(u => u.Username);
                _ = col.EnsureIndex(u => u.UserProfile);
            }
        }

        public override void UpdatePassword(string username, string password)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                var dbUser = col.FindOne(u => u.Username == username);
                dbUser.Password = password;
                _ = col.Update(dbUser);
            }
        }

        public override void UpdateUser(string id, Core.Db.User user)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                var i = int.Parse(id);
                var dbUser = col.FindOne(u => u.Id == i);
                dbUser.ModifiedOn = DateTime.Now;
                dbUser.Username = user.Username;
                dbUser.Password = user.Password;
                dbUser.UserProfile = user.UserProfile;
                dbUser.Email = user.Email;
                _ = col.Update(dbUser);
            }
        }

        public override void UpdateUsernameAndEmailAndUserProfile(string userId, string username, string email, UserProfile up)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                var i = int.Parse(userId);
                var dbUser = col.FindOne(u => u.Id == i);
                dbUser.ModifiedOn = DateTime.Now;
                dbUser.Username = username;
                dbUser.Email = email;
                dbUser.UserProfile = up;
                _ = col.Update(dbUser);
            }
        }

        public override void DeleteUser(string username, string password)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                var user = col.FindOne(u => u.Username == username);
                if (user != null && user.Password == password)
                {
                    _ = col.DeleteMany(u => u.Username == username);
                    DeleteUserWorkflowRelationsByUserId(user.Id.ToString());
                }
                else
                {
                    throw new Exception("The password is incorrect.");
                }
            }
        }

        public override Core.Db.User GetUser(string username)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                var user = col.FindOne(u => u.Username == username);
                return user;
            }
        }

        public override Core.Db.User GetUserById(string userId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                var id = int.Parse(userId);
                var user = col.FindOne(u => u.Id == id);
                return user;
            }
        }

        public override string GetPassword(string username)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                var user = col.FindOne(u => u.Username == username);
                return user.Password;
            }
        }

        public override IEnumerable<Core.Db.User> GetUsers()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                return col.FindAll();
            }
        }

        public override IEnumerable<Core.Db.User> GetUsers(string keyword, UserOrderBy uo)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                var keywordToLower = keyword.ToLower();
                BsonExpression query = null;

                if (!string.IsNullOrEmpty(keyword))
                {
                    query = Query.Contains("Username", keywordToLower);
                }

                switch (uo)
                {
                    case UserOrderBy.UsernameAscending:
                        if (query != null)
                        {
                            //return col.Find(Query.And(Query.All("Username"), query));

                            var q = Query.All("Username");
                            q.Where.Add(query);
                            return col.Find(q);
                        }
                        else
                        {
                            return col.Find(Query.All("Username"));
                        }

                    case UserOrderBy.UsernameDescending:

                        if (query != null)
                        {
                            //return col.Find(Query.And(Query.All("Username", Query.Descending), query));

                            var q = Query.All("Username", Query.Descending);
                            q.Where.Add(query);
                            return col.Find(q);
                        }
                        else
                        {
                            return col.Find(Query.All("Username", Query.Descending));
                        }

                    default:
                        break;
                }

                return Array.Empty<User>();
            }
        }

        public override IEnumerable<Core.Db.User> GetAdministrators(string keyword, UserOrderBy uo)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                var keywordToLower = keyword.ToLower();
                BsonExpression query = null;

                if (!string.IsNullOrEmpty(keyword))
                {
                    query = Query.And(Query.EQ("UserProfile", UserProfile.Administrator.ToString()), Query.Contains("Username", keywordToLower));
                }

                switch (uo)
                {
                    case UserOrderBy.UsernameAscending:
                        if (query != null)
                        {
                            //return col.Find(Query.And(Query.All("Username"), query));

                            var q = Query.All("Username");
                            q.Where.Add(query);
                            return col.Find(q);
                        }
                        else
                        {
                            //return col.Find(Query.And(Query.All("Username"), Query.EQ("UserProfile", UserProfile.Administrator.ToString())));

                            var q = Query.All("Username");
                            q.Where.Add(Query.EQ("UserProfile", UserProfile.Administrator.ToString()));
                            return col.Find(q);
                        }

                    case UserOrderBy.UsernameDescending:
                        if (query != null)
                        {
                            //return col.Find(Query.And(Query.All("Username", Query.Descending), query));

                            var q = Query.All("Username", Query.Descending);
                            q.Where.Add(query);
                            return col.Find(q);
                        }
                        else
                        {
                            //return col.Find(Query.And(Query.All("Username", Query.Descending), Query.EQ("UserProfile", UserProfile.Administrator.ToString())));

                            var q = Query.All("Username", Query.Descending);
                            q.Where.Add(Query.EQ("UserProfile", UserProfile.Administrator.ToString()));
                            return col.Find(q);
                        }

                    default:
                        break;
                }

                return Array.Empty<User>();
            }
        }

        public override void InsertHistoryEntry(Core.Db.HistoryEntry entry)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DOCUMENT_NAME);
                var he = new HistoryEntry
                {
                    Description = entry.Description,
                    LaunchType = entry.LaunchType,
                    Name = entry.Name,
                    Status = entry.Status,
                    StatusDate = entry.StatusDate,
                    WorkflowId = entry.WorkflowId,
                    Logs = entry.Logs
                };
                _ = col.Insert(he);
                _ = col.EnsureIndex(e => e.WorkflowId);
                //col.EnsureIndex(e => e.Name, "LOWER($.Name)");
                _ = col.EnsureIndex(e => e.Name);
                _ = col.EnsureIndex(e => e.LaunchType);
                //col.EnsureIndex(e => e.Description, "LOWER($.Name)");
                _ = col.EnsureIndex(e => e.Description);
                _ = col.EnsureIndex(e => e.Status);
                _ = col.EnsureIndex(e => e.StatusDate);
            }
        }

        /*
                public void UpdateHistoryEntry(HistoryEntry entry)
                {
                    lock (Padlock)
                    {
                        var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DocumentName);
                        _ = col.Update(entry);
                    }
                }
        */

        /*
                public void DeleteHistoryEntries(int workflowId)
                {
                    lock (Padlock)
                    {
                        var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DocumentName);
                        _ = col.DeleteMany(e => e.WorkflowId == workflowId);
                    }
                }
        */

        public override IEnumerable<Core.Db.HistoryEntry> GetHistoryEntries()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DOCUMENT_NAME);
                return col.FindAll();
            }
        }

        public override IEnumerable<Core.Db.HistoryEntry> GetHistoryEntries(string keyword)
        {
            lock (_padlock)
            {
                var keywordToUpper = keyword.ToUpper();
                var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DOCUMENT_NAME);
                return col.Find(e => e.Name.ToUpper().Contains(keywordToUpper) || e.Description.ToUpper().Contains(keywordToUpper));
            }
        }

        public override IEnumerable<Core.Db.HistoryEntry> GetHistoryEntries(string keyword, int page, int entriesCount)
        {
            lock (_padlock)
            {
                var keywordToUpper = keyword.ToUpper();
                var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DOCUMENT_NAME);
                return col.Find(e => e.Name.ToUpper().Contains(keywordToUpper) || e.Description.ToUpper().Contains(keywordToUpper), (page - 1) * entriesCount, entriesCount);
            }
        }

        public override IEnumerable<Core.Db.HistoryEntry> GetHistoryEntries(string keyword, DateTime from, DateTime to, int page, int entriesCount, EntryOrderBy heo)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DOCUMENT_NAME);
                var keywordToLower = keyword.ToLower();
                var skip = (page - 1) * entriesCount;
                var query = !string.IsNullOrEmpty(keyword)
                    ? Query.And(Query.Or(Query.Contains("Name", keywordToLower), Query.Contains("Description", keywordToLower))
                                    , Query.And(Query.GTE("StatusDate", from), Query.LTE("StatusDate", to)))
                    : Query.And(Query.GTE("StatusDate", from), Query.LTE("StatusDate", to));
                switch (heo)
                {
                    case EntryOrderBy.StatusDateAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("StatusDate")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q1 = Query.All("StatusDate");
                        q1.Where.Add(query);

                        return col.Find(
                            q1
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.StatusDateDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("StatusDate", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q2 = Query.All("StatusDate", Query.Descending);
                        q2.Where.Add(query);

                        return col.Find(
                            q2
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.WorkflowIdAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("WorkflowId")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q3 = Query.All("WorkflowId");
                        q3.Where.Add(query);

                        return col.Find(
                           q3
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.WorkflowIdDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("WorkflowId", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q4 = Query.All("WorkflowId", Query.Descending);
                        q4.Where.Add(query);

                        return col.Find(
                           q4
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.NameAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Name")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q5 = Query.All("Name");
                        q5.Where.Add(query);

                        return col.Find(
                            q5
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.NameDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Name", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q6 = Query.All("Name", Query.Descending);
                        q6.Where.Add(query);

                        return col.Find(
                            q6
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.LaunchTypeAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("LaunchType")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q7 = Query.All("LaunchType");
                        q7.Where.Add(query);

                        return col.Find(
                            q7
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.LaunchTypeDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("LaunchType", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q8 = Query.All("LaunchType", Query.Descending);
                        q8.Where.Add(query);

                        return col.Find(
                            q8
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.DescriptionAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Description")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q9 = Query.All("Description");
                        q9.Where.Add(query);

                        return col.Find(
                           q9
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.DescriptionDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Description", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q10 = Query.All("Description", Query.Descending);
                        q10.Where.Add(query);

                        return col.Find(
                            q10
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.StatusAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Status")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q11 = Query.All("Status");
                        q11.Where.Add(query);

                        return col.Find(
                            q11
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.StatusDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Status", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q12 = Query.All("Status", Query.Descending);
                        q12.Where.Add(query);

                        return col.Find(
                            q12
                            , skip
                            , entriesCount
                        );

                    default:
                        break;
                }

                return Array.Empty<HistoryEntry>();
            }
        }

        public override IEnumerable<Core.Db.Entry> GetEntries(string keyword, DateTime from, DateTime to, int page, int entriesCount, EntryOrderBy eo)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Entry>(Core.Db.Entry.DOCUMENT_NAME);
                var keywordToLower = keyword.ToLower();
                var skip = (page - 1) * entriesCount;
                var query = !string.IsNullOrEmpty(keyword)
                    ? Query.And(Query.Or(Query.Contains("Name", keywordToLower), Query.Contains("Description", keywordToLower))
                                    , Query.And(Query.GTE("StatusDate", from), Query.LTE("StatusDate", to)))
                    : Query.And(Query.GTE("StatusDate", from), Query.LTE("StatusDate", to));
                switch (eo)
                {
                    case EntryOrderBy.StatusDateAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("StatusDate")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q1 = Query.All("StatusDate");
                        q1.Where.Add(query);

                        return col.Find(
                            q1
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.StatusDateDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("StatusDate", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q2 = Query.All("StatusDate", Query.Descending);
                        q2.Where.Add(query);

                        return col.Find(
                            q2
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.WorkflowIdAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("WorkflowId")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q3 = Query.All("WorkflowId");
                        q3.Where.Add(query);

                        return col.Find(
                           q3
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.WorkflowIdDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("WorkflowId", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q4 = Query.All("WorkflowId", Query.Descending);
                        q4.Where.Add(query);

                        return col.Find(
                           q4
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.NameAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Name")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q5 = Query.All("Name");
                        q5.Where.Add(query);

                        return col.Find(
                            q5
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.NameDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Name", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q6 = Query.All("Name", Query.Descending);
                        q6.Where.Add(query);

                        return col.Find(
                            q6
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.LaunchTypeAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("LaunchType")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q7 = Query.All("LaunchType");
                        q7.Where.Add(query);

                        return col.Find(
                            q7
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.LaunchTypeDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("LaunchType", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q8 = Query.All("LaunchType", Query.Descending);
                        q8.Where.Add(query);

                        return col.Find(
                            q8
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.DescriptionAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Description")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q9 = Query.All("Description");
                        q9.Where.Add(query);

                        return col.Find(
                           q9
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.DescriptionDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Description", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q10 = Query.All("Description", Query.Descending);
                        q10.Where.Add(query);

                        return col.Find(
                            q10
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.StatusAscending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Status")
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q11 = Query.All("Status");
                        q11.Where.Add(query);

                        return col.Find(
                            q11
                            , skip
                            , entriesCount
                        );

                    case EntryOrderBy.StatusDescending:

                        //return col.Find(
                        //    Query.And(
                        //        Query.All("Status", Query.Descending)
                        //        , query
                        //    )
                        //    , skip
                        //    , entriesCount
                        //);

                        var q12 = Query.All("Status", Query.Descending);
                        q12.Where.Add(query);

                        return col.Find(
                            q12
                            , skip
                            , entriesCount
                        );

                    default:
                        break;
                }

                return Array.Empty<Entry>();
            }
        }

        public override long GetHistoryEntriesCount(string keyword)
        {
            lock (_padlock)
            {
                var keywordToUpper = keyword.ToUpper();
                var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DOCUMENT_NAME);
                return col.Find(e => e.Name.ToUpper().Contains(keywordToUpper) || e.Description.ToUpper().Contains(keywordToUpper)).LongCount();
            }
        }

        public override long GetHistoryEntriesCount(string keyword, DateTime from, DateTime to)
        {
            lock (_padlock)
            {
                var keywordToLower = keyword.ToLower();
                var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DOCUMENT_NAME);
                var query = !string.IsNullOrEmpty(keyword)
                    ? Query.And(Query.Or(Query.Contains("Name", keywordToLower), Query.Contains("Description", keywordToLower))
                        , Query.And(Query.GTE("StatusDate", from), Query.LTE("StatusDate", to)))
                    : Query.And(Query.GTE("StatusDate", from), Query.LTE("StatusDate", to));
                return col.Find(query).LongCount();
            }
        }

        public override long GetEntriesCount(string keyword, DateTime from, DateTime to)
        {
            lock (_padlock)
            {
                var keywordToLower = keyword.ToLower();
                var col = _db.GetCollection<Entry>(Core.Db.Entry.DOCUMENT_NAME);
                var query = !string.IsNullOrEmpty(keyword)
                    ? Query.And(Query.Or(Query.Contains("Name", keywordToLower), Query.Contains("Description", keywordToLower))
                        , Query.And(Query.GTE("StatusDate", from), Query.LTE("StatusDate", to)))
                    : Query.And(Query.GTE("StatusDate", from), Query.LTE("StatusDate", to));
                return col.Find(query).LongCount();
            }
        }

        public override DateTime GetHistoryEntryStatusDateMin()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DOCUMENT_NAME);
                var q = col.Find(Query.All("StatusDate")).ToArray();
                return q.Any() ? q.Select(e => e.StatusDate).First() : DateTime.Now;
            }
        }

        public override DateTime GetHistoryEntryStatusDateMax()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DOCUMENT_NAME);
                var q = col.Find(Query.All("StatusDate", Query.Descending)).ToArray();
                return q.Any() ? q.Select(e => e.StatusDate).First() : DateTime.Now;
            }
        }

        public override DateTime GetEntryStatusDateMin()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<HistoryEntry>(Core.Db.Entry.DOCUMENT_NAME);
                var q = col.Find(Query.All("StatusDate")).ToArray();
                return q.Any() ? q.Select(e => e.StatusDate).First() : DateTime.Now;
            }
        }

        public override DateTime GetEntryStatusDateMax()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<HistoryEntry>(Core.Db.Entry.DOCUMENT_NAME);
                var q = col.Find(Query.All("StatusDate", Query.Descending)).ToArray();
                return q.Any() ? q.Select(e => e.StatusDate).First() : DateTime.Now;
            }
        }

        public override string InsertWorkflow(Core.Db.Workflow workflow)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Workflow>(Core.Db.Workflow.DOCUMENT_NAME);
                var wf = new Workflow { Xml = workflow.Xml };
                var res = col.Insert(wf);
                return res.AsInt32.ToString();
            }
        }

        public override void UpdateWorkflow(string dbId, Core.Db.Workflow workflow)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Workflow>(Core.Db.Workflow.DOCUMENT_NAME);
                var wf = new Workflow { Id = int.Parse(dbId), Xml = workflow.Xml };
                _ = col.Update(wf);
            }
        }

        public override void DeleteWorkflow(string id)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Workflow>(Core.Db.Workflow.DOCUMENT_NAME);
                var i = int.Parse(id);
                _ = col.DeleteMany(e => e.Id == i);
            }
        }

        public override void DeleteWorkflows(string[] ids)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Workflow>(Core.Db.Workflow.DOCUMENT_NAME);
                _ = col.DeleteMany(e => ids.Contains(e.Id.ToString()));
            }
        }

        public override IEnumerable<Core.Db.Workflow> GetWorkflows()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Workflow>(Core.Db.Workflow.DOCUMENT_NAME);
                return col.FindAll();
            }
        }

        public override Core.Db.Workflow GetWorkflow(string id)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Workflow>(Core.Db.Workflow.DOCUMENT_NAME);
                return col.FindById(int.Parse(id));
            }
        }

        public override void InsertUserWorkflowRelation(Core.Db.UserWorkflow userWorkflow)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<UserWorkflow>(Core.Db.UserWorkflow.DOCUMENT_NAME);
                var uw = new UserWorkflow
                {
                    UserId = userWorkflow.UserId,
                    WorkflowId = userWorkflow.WorkflowId
                };
                _ = col.Insert(uw);
            }
        }

        public override void DeleteUserWorkflowRelationsByUserId(string userId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<UserWorkflow>(Core.Db.UserWorkflow.DOCUMENT_NAME);
                _ = col.DeleteMany(uw => uw.UserId == userId);
            }
        }

        public override void DeleteUserWorkflowRelationsByWorkflowId(string workflowId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<UserWorkflow>(Core.Db.UserWorkflow.DOCUMENT_NAME);
                _ = col.DeleteMany(uw => uw.WorkflowId == workflowId);
            }
        }

        public override IEnumerable<string> GetUserWorkflows(string userId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<UserWorkflow>(Core.Db.UserWorkflow.DOCUMENT_NAME);
                return col.Find(uw => uw.UserId == userId).Select(uw => uw.WorkflowId.ToString());
            }
        }

        public override bool CheckUserWorkflow(string userId, string workflowId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<UserWorkflow>(Core.Db.UserWorkflow.DOCUMENT_NAME);
                var res = col.FindOne(uw => uw.UserId == userId && uw.WorkflowId == workflowId);
                return res != null;
            }
        }

        public override string GetEntryLogs(string entryId)
        {
            lock (_padlock)
            {
                var id = int.Parse(entryId);
                var col = _db.GetCollection<Entry>(Core.Db.Entry.DOCUMENT_NAME);
                var entry = col.FindOne(e => e.Id == id);
                return entry.Logs;
            }
        }

        public override string GetHistoryEntryLogs(string entryId)
        {
            lock (_padlock)
            {
                var id = int.Parse(entryId);
                var col = _db.GetCollection<HistoryEntry>(Core.Db.HistoryEntry.DOCUMENT_NAME);
                var entry = col.FindOne(e => e.Id == id);
                return entry.Logs;
            }
        }

        public override IEnumerable<Core.Db.User> GetNonRestricedUsers()
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<User>(Core.Db.User.DOCUMENT_NAME);
                var users = col.Find(u => u.UserProfile == UserProfile.SuperAdministrator || u.UserProfile == UserProfile.Administrator).OrderBy(u => u.Username);
                return users;
            }
        }

        public override string InsertRecord(Core.Db.Record record)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Record>(Core.Db.Record.DOCUMENT_NAME);
                var r = new Record
                {
                    Approved = record.Approved,
                    AssignedOn = record.AssignedOn,
                    AssignedTo = record.AssignedTo,
                    Comments = record.Comments,
                    CreatedBy = record.CreatedBy,
                    CreatedOn = DateTime.Now,
                    Description = record.Description,
                    EndDate = record.EndDate,
                    ManagerComments = record.ManagerComments,
                    Name = record.Name,
                    StartDate = record.StartDate
                };
                var recordId = col.Insert(r).AsInt32.ToString();

                return recordId;
            }
        }

        public override void UpdateRecord(string recordId, Core.Db.Record record)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Record>(Core.Db.Record.DOCUMENT_NAME);
                var bsonId = int.Parse(recordId);
                var recordFromDb = col.FindById(bsonId);
                var r = new Record
                {
                    Id = bsonId,
                    Approved = record.Approved,
                    AssignedOn = record.AssignedOn,
                    AssignedTo = record.AssignedTo,
                    Comments = record.Comments,
                    CreatedBy = record.CreatedBy,
                    //CreatedOn = record.CreatedOn,
                    CreatedOn = recordFromDb.CreatedOn,
                    Description = record.Description,
                    EndDate = record.EndDate,
                    ManagerComments = record.ManagerComments,
                    Name = record.Name,
                    StartDate = record.StartDate,
                    ModifiedBy = record.ModifiedBy,
                    ModifiedOn = DateTime.Now
                };
                _ = col.Update(r);
            }
        }

        public override void DeleteRecords(string[] recordIds)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Record>(Core.Db.Record.DOCUMENT_NAME);
                _ = col.DeleteMany(r => recordIds.Contains(r.Id.ToString()));
            }
        }

        public override Core.Db.Record GetRecord(string id)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Record>(Core.Db.Record.DOCUMENT_NAME);
                var bsonId = int.Parse(id);
                var record = col.FindById(bsonId);
                return record;
            }
        }

        public override IEnumerable<Core.Db.Record> GetRecords(string keyword)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Record>(Core.Db.Record.DOCUMENT_NAME);
                var keywordToUpper = keyword.ToUpper();
                var records = col.Find(r => r.Name.ToUpper().Contains(keywordToUpper) || (!string.IsNullOrEmpty(r.Description) && r.Description.ToUpper().Contains(keywordToUpper))).OrderByDescending(r => r.CreatedOn).ToList();
                return records;
            }
        }

        public override IEnumerable<Core.Db.Record> GetRecordsCreatedBy(string createdBy)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Record>(Core.Db.Record.DOCUMENT_NAME);
                var records = col.Find(r => r.CreatedBy == createdBy).OrderBy(r => r.Name).ToList();
                return records;
            }
        }

        public override IEnumerable<Core.Db.Record> GetRecordsCreatedByOrAssignedTo(string createdBy, string assingedTo, string keyword)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Record>(Core.Db.Record.DOCUMENT_NAME);
                var keywordToUpper = keyword.ToUpper();
                var records = col.Find(r => (r.CreatedBy == createdBy || r.AssignedTo == assingedTo) && (r.Name.ToUpper().Contains(keywordToUpper) || (!string.IsNullOrEmpty(r.Description) && r.Description.ToUpper().Contains(keywordToUpper)))).OrderByDescending(r => r.CreatedOn).ToList();
                return records;
            }
        }

        public override string InsertVersion(Core.Db.Version version)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Version>(Core.Db.Version.DOCUMENT_NAME);
                var v = new Version
                {
                    RecordId = version.RecordId,
                    CreatedOn = DateTime.Now,
                    FilePath = version.FilePath
                };
                var versionId = col.Insert(v).AsInt32.ToString();
                return versionId;
            }
        }

        public override void UpdateVersion(string versionId, Core.Db.Version version)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Version>(Core.Db.Version.DOCUMENT_NAME);
                var bsonId = int.Parse(versionId);
                var versionFromDb = col.FindById(bsonId);
                var v = new Version
                {
                    Id = bsonId,
                    RecordId = version.RecordId,
                    //CreatedOn = version.CreatedOn,
                    CreatedOn = versionFromDb.CreatedOn,
                    FilePath = version.FilePath
                };
                _ = col.Update(v);
            }
        }

        public override void DeleteVersions(string[] versionIds)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Version>(Core.Db.Version.DOCUMENT_NAME);
                _ = col.DeleteMany(v => versionIds.Contains(v.Id.ToString()));
            }
        }

        public override IEnumerable<Core.Db.Version> GetVersions(string recordId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Version>(Core.Db.Version.DOCUMENT_NAME);
                var versions = col.Find(v => v.RecordId == recordId).OrderBy(v => v.CreatedOn).ToList();
                return versions;
            }
        }

        public override Core.Db.Version GetLatestVersion(string recordId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Version>(Core.Db.Version.DOCUMENT_NAME);
                var version = col.Find(v => v.RecordId == recordId).OrderByDescending(v => v.CreatedOn).FirstOrDefault();
                return version;
            }
        }

        public override string InsertNotification(Core.Db.Notification notification)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Notification>(Core.Db.Notification.DOCUMENT_NAME);
                var n = new Notification
                {
                    AssignedBy = notification.AssignedBy,
                    AssignedOn = notification.AssignedOn,
                    AssignedTo = notification.AssignedTo,
                    Message = notification.Message,
                    IsRead = notification.IsRead
                };
                var notificationId = col.Insert(n).AsInt32.ToString();
                return notificationId;
            }
        }

        public override void MarkNotificationsAsRead(string[] notificationIds)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Notification>(Core.Db.Notification.DOCUMENT_NAME);
                _ = col.UpdateMany(n => new Notification
                {
                    Id = n.Id,
                    AssignedBy = n.AssignedBy,
                    AssignedOn = n.AssignedOn,
                    AssignedTo = n.AssignedTo,
                    Message = n.Message,
                    IsRead = true
                }, n => notificationIds.Contains(n.Id.ToString()));
            }
        }

        public override void MarkNotificationsAsUnread(string[] notificationIds)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Notification>(Core.Db.Notification.DOCUMENT_NAME);
                _ = col.UpdateMany(n => new Notification
                {
                    Id = n.Id,
                    AssignedBy = n.AssignedBy,
                    AssignedOn = n.AssignedOn,
                    AssignedTo = n.AssignedTo,
                    Message = n.Message,
                    IsRead = false
                }, n => notificationIds.Contains(n.Id.ToString()));
            }
        }

        public override void DeleteNotifications(string[] notificationIds)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Notification>(Core.Db.Notification.DOCUMENT_NAME);
                _ = col.DeleteMany(n => notificationIds.Contains(n.Id.ToString()));
            }
        }

        public override IEnumerable<Core.Db.Notification> GetNotifications(string assignedTo, string keyword)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Notification>(Core.Db.Notification.DOCUMENT_NAME);
                var keywordToUpper = keyword.ToUpper();
                var notifications = col.Find(n => n.AssignedTo == assignedTo && n.Message.ToUpper().Contains(keywordToUpper)).OrderByDescending(n => n.AssignedOn).ToList();
                return notifications;
            }
        }

        public override bool HasNotifications(string assignedTo)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Notification>(Core.Db.Notification.DOCUMENT_NAME);
                var notifications = col.Find(n => n.AssignedTo == assignedTo && !n.IsRead);
                var hasNotifications = notifications.Any();
                return hasNotifications;
            }
        }

        public override string InsertApprover(Core.Db.Approver approver)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Approver>(Core.Db.Approver.DOCUMENT_NAME);
                var a = new Approver
                {
                    UserId = approver.UserId,
                    RecordId = approver.RecordId,
                    Approved = approver.Approved,
                    ApprovedOn = approver.ApprovedOn
                };
                var approverId = col.Insert(a).AsInt32.ToString();

                return approverId;
            }
        }

        public override void UpdateApprover(string approverId, Core.Db.Approver approver)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Approver>(Core.Db.Approver.DOCUMENT_NAME);
                var bsonId = int.Parse(approverId);

                var a = new Approver
                {
                    Id = bsonId,
                    UserId = approver.UserId,
                    RecordId = approver.RecordId,
                    Approved = approver.Approved,
                    ApprovedOn = approver.ApprovedOn
                };

                _ = col.Update(a);
            }
        }

        public override void DeleteApproversByRecordId(string recordId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Approver>(Core.Db.Approver.DOCUMENT_NAME);
                _ = col.DeleteMany(a => a.RecordId == recordId);
            }
        }

        public override void DeleteApprovedApprovers(string recordId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Approver>(Core.Db.Approver.DOCUMENT_NAME);
                _ = col.DeleteMany(a => a.Approved && a.RecordId == recordId);
            }
        }

        public override void DeleteApproversByUserId(string userId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Approver>(Core.Db.Approver.DOCUMENT_NAME);
                _ = col.DeleteMany(a => a.UserId == userId);
            }
        }

        public override IEnumerable<Core.Db.Approver> GetApprovers(string recordId)
        {
            lock (_padlock)
            {
                var col = _db.GetCollection<Approver>(Core.Db.Approver.DOCUMENT_NAME);
                var approvers = col.Find(a => a.RecordId == recordId).ToList();
                return approvers;
            }
        }

        public override void Dispose()
        {
            lock (_padlock)
            {
                _db.Dispose();
                _db = null;
            }
        }
    }
}

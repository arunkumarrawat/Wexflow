﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;
using Wexflow.Core;

namespace Wexflow.Tasks.ApprovalRecordsCreator
{
    public class ApprovalRecordsCreator : Task
    {
        public string CreatedBy { get; }

        public ApprovalRecordsCreator(XElement xe, Workflow wf) : base(xe, wf)
        {
            CreatedBy = GetSetting("createdBy");
        }

        public override TaskStatus Run()
        {
            Workflow.CancellationTokenSource.Token.ThrowIfCancellationRequested();
            Info("Importing records...");

            var success = true;
            var atLeastOneSuccess = false;

            try
            {
                List<string> recordIds = [];
                var files = SelectFiles();

                foreach (var file in files)
                {
                    Workflow.CancellationTokenSource.Token.ThrowIfCancellationRequested();
                    try
                    {
                        var recordId = Workflow.WexflowEngine.SaveRecordFromFile(file.Path, CreatedBy);

                        if (recordId != "-1")
                        {
                            Info($"Record inserted from file {file.Path}. RecordId: {recordId}");
                            recordIds.Add(recordId);
                            if (!atLeastOneSuccess)
                            {
                                atLeastOneSuccess = true;
                            }
                        }
                        else
                        {
                            Error($"An error occured while inserting a record from the file {file.Path}.");
                            success = false;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        ErrorFormat("An error occured while importing the record {0}.", e, file.Path);
                        success = false;
                    }
                    finally
                    {
                        if (!Workflow.CancellationTokenSource.Token.IsCancellationRequested)
                        {
                            WaitOne();
                        }
                    }
                }

                var smKey = "ApprovalRecordsCreator.RecordIds";
                if (SharedMemory.ContainsKey(smKey))
                {
                    SharedMemory.Remove(smKey);
                }
                SharedMemory.Add(smKey, recordIds.ToArray());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while importing records.", e);
                success = false;
            }

            var status = Status.Success;

            if (!success && atLeastOneSuccess)
            {
                status = Status.Warning;
            }
            else if (!success)
            {
                status = Status.Error;
            }

            Info("Task finished.");
            return new TaskStatus(status);
        }
    }
}

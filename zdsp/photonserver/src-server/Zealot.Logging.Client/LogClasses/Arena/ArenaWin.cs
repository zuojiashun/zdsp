﻿namespace Zealot.Logging.Client.LogClasses
{
    using Contracts.Requests;
    using System;
    using System.Collections.Generic;

    public class ArenaWin : LogClass
    {
        // WARNING!
        // Never re-arrange the order of the properties after initial creation.
        // Logging server uses SQL Prepared Statements for performance.
        // Altering the order will cause SQL exceptions and/or data corruption on
        // current/subsequent insertion of records. Always append new properties. 
        // Do not insert between existing properties.
        public int oldRank { get; set; }

        public int winRank { get; set; }

        public string enemy { get; set; } 

        public int usedEntry { get; set; }

        public override RequestInsertRecord GetRequestInsertRecord()
        {
            // Reminder: Change the class type identical to this class.
            RequestInsertRecord requestInsertRecord = CreateRequestInsertRecord();

            // WARNING!
            // Code all properties in the logFieldTypes and logFieldValues in the 
            // EXACT order in which they are declared above or risk potential SQL
            // exceptions and/or data corruption.
            requestInsertRecord.logFieldTypes = new List<TypeCode>()
            {
                oldRank.GetTypeCode(),
                winRank.GetTypeCode(),
                enemy.GetTypeCode(),
                usedEntry.GetTypeCode()
            };

            requestInsertRecord.logFieldValues = new List<object>()
            {
                oldRank,
                winRank,
                enemy,
                usedEntry
            };

            return requestInsertRecord;
        }
    }
}
﻿// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-01-23
// 
// Licensed under the MIT License.

using Shared;
using Shared.Data;
using System;
using System.Timers;
using FitbitTracker.Data;
using FitbitTracker.Model;
using System.Collections.Generic;
using FitbitTracker.Data.FitbitModel;

namespace FitbitTracker
{
    public sealed class Deamon : BaseTracker, ITracker
    {

        private Timer fitbitTimer;

        public Deamon()
        {
            Name = Settings.TRACKER_NAME;
        }

        public override string GetStatus()
        {
            return IsRunning ? (Name + " is running") : (Name + " is NOT running");
        }

        public override void CreateDatabaseTablesIfNotExist()
        {
            DatabaseConnector.CreateFitbitTables();
        }

        public override bool IsEnabled()
        {
            return Database.GetInstance().GetSettingsBool(Settings.TRACKER_ENEABLED_SETTING, true);
        }

        public override void Start()
        {
            Console.WriteLine("Start FitbitTracker");
            CreateFitbitPullTimer();
            IsRunning = true;

            OnPullFromFitbit(null, null);
        }

        private void CreateFitbitPullTimer()
        {
            fitbitTimer = new Timer();
            fitbitTimer.Elapsed += OnPullFromFitbit;
            fitbitTimer.Interval = Settings.SYNCHRONIZE_INTERVALL;
            fitbitTimer.Enabled = true;
        }

        private void OnPullFromFitbit(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Get data from fitbit");

            DateTimeOffset latestSync = FitbitConnector.GetLatestSyncDate();
            if (latestSync == DateTimeOffset.MinValue)
            {
                Console.WriteLine("Can't sync now.");
            }
            else
            {
                latestSync = latestSync.AddDays(-1);

                GetHRData(latestSync);
                GetSleepData(latestSync);
            }
        }

        private static void GetHRData(DateTimeOffset latestSync)
        {
            List<DateTimeOffset> days = DatabaseConnector.GetDaysToSynchronize(DataType.HR);

            foreach (DateTimeOffset day in days)
            {
                Console.WriteLine("Sync: " + day);
                Tuple<List<HeartRateDayData>, List<HeartrateIntraDayData>> hrData = FitbitConnector.GetHeartrateForDay(day);
                DatabaseConnector.SaveHRData(hrData.Item1);
                DatabaseConnector.SaveHRIntradayData(hrData.Item2);
                if (day < latestSync)
                {
                    Console.WriteLine("Finished syncing day: " + day);
                    DatabaseConnector.SetSynchronizedDay(day, DataType.HR);
                }
            }
        }

        private static void GetSleepData(DateTimeOffset latestSync)
        {
            List<DateTimeOffset> days = DatabaseConnector.GetDaysToSynchronize(DataType.SLEEP);

            foreach (DateTimeOffset day in days)
            {
                Console.WriteLine("Sync: " + day);
                SleepData sleepData = FitbitConnector.GetSleepDataForDay(day);
                DatabaseConnector.SaveSleepData(sleepData);
                if (day < latestSync)
                {
                    Console.WriteLine("Finished syncing day: " + day);
                    DatabaseConnector.SetSynchronizedDay(day, DataType.SLEEP);
                }
            }
        }

        public override void Stop()
        {
            if (fitbitTimer != null)
            {
                fitbitTimer.Enabled = false;
            }
            IsRunning = false;
        }

        public override void UpdateDatabaseTables(int version)
        {
            //No updates needed so far!
        }

    }
}
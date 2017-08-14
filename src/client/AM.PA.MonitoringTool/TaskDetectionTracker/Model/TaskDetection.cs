﻿// Created by Sebastian Müller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-03-15
// 
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TaskDetectionTracker.Model
{

    /// <summary>
    /// Model for the task detection. Returned by Katja's algorithm.
    /// </summary>
    public class TaskDetection : IComparable<TaskDetection>, INotifyPropertyChanged
    {
        private DateTime _start;
        private DateTime _end; //time of switch away
        private string _taskTypeProposed; //set at the beginning and then not changed anymore
        private string _taskTypeValidated; //empty at the beginning
        private bool _taskTypeProposedSet = false;
        private TaskDetectionCase _taskDetectionCase;
        private List<TaskDetectionInput> _timelineInfos;
        private bool _isMainTask = false;
        
        public DateTime Start { get { return _start; } set { _start = value; } }
        public DateTime End { get { return _end; } set { _end = value; } }
        public string TaskTypeProposed { get { return _taskTypeProposed; } set { if (!_taskTypeProposedSet) { _taskTypeProposed = value; _taskTypeProposedSet = true; _taskTypeValidated = value; } } }
        public string TaskTypeValidated { get { return _taskTypeValidated; } set { _taskTypeValidated = value; NotifyPropertyChanged("TaskTypeValidated"); } }
        public TaskDetectionCase TaskDetectionCase { get { return _taskDetectionCase; } set { _taskDetectionCase = value; } }
        public List<TaskDetectionInput> TimelineInfos { get { return _timelineInfos; } set { _timelineInfos = value; } }
        public bool IsMainTask { get { return _isMainTask; } set { _isMainTask = value; } }

        public override string ToString()
        {
            return "proposed: " + TaskTypeProposed + ", validated: " + TaskTypeValidated + " [" + Start.ToShortTimeString() + " - " + End.ToShortTimeString() + "] - " + TaskDetectionCase + " (Main task: " + IsMainTask + ")";
        }

        public int CompareTo(TaskDetection other)
        {
            return Start.CompareTo(other.Start);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
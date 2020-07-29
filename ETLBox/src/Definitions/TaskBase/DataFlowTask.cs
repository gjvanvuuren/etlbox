﻿using ETLBox.ControlFlow;
using System.Collections.Generic;

namespace ETLBox.DataFlow
{
    public abstract class DataFlowTask : GenericTask, ITask
    {
        public List<IDataFlowLinkSource> Predecessors { get; set; } = new List<IDataFlowLinkSource>();
        public List<IDataFlowLinkTarget> Successors { get; set; } = new List<IDataFlowLinkTarget>();

        protected int? _loggingThresholdRows;
        public virtual int? LoggingThresholdRows
        {
            get
            {
                if (DataFlow.HasLoggingThresholdRows)
                    return DataFlow.LoggingThresholdRows;
                else
                    return _loggingThresholdRows;
            }
            set
            {
                _loggingThresholdRows = value;
            }
        }

        public int ProgressCount { get; set; }

        protected bool HasLoggingThresholdRows => LoggingThresholdRows != null && LoggingThresholdRows > 0;
        protected int ThresholdCount { get; set; } = 1;

        public int MaxBufferSize
        {
            get
            {
                return _maxBufferSize > 0 ? _maxBufferSize : DataFlow.MaxBufferSize;
            }
            set {
                _maxBufferSize = value;
                InitBufferObjects();
            }
        }

        protected virtual void InitBufferObjects() { }

        protected void InitBuffersForCurrentAndSuccessors()
        {
            InitBufferObjects();
            foreach (var succesor in Successors)
            {
                var s = succesor as DataFlowTask; //TODO IDataFlowTask
                s.InitBuffersForCurrentAndSuccessors();
            }
        }

        internal virtual void LinkBuffers()
        {

        }


        protected int _maxBufferSize = -1;

        protected void NLogStart()
        {
            if (!DisableLogging)
                NLogger.Info(TaskName, TaskType, "START", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.Id);
        }

        protected void NLogFinish()
        {
            if (!DisableLogging && HasLoggingThresholdRows)
                NLogger.Info(TaskName + $" processed {ProgressCount} records in total.", TaskType, "LOG", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.Id);
            if (!DisableLogging)
                NLogger.Info(TaskName, TaskType, "END", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.Id);
        }

        protected void LogProgressBatch(int rowsProcessed)
        {
            ProgressCount += rowsProcessed;
            if (!DisableLogging && HasLoggingThresholdRows && ProgressCount >= (LoggingThresholdRows * ThresholdCount))
            {
                NLogger.Info(TaskName + $" processed {ProgressCount} records.", TaskType, "LOG", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.Id);
                ThresholdCount++;
            }
        }

        protected void LogProgress()
        {
            ProgressCount += 1;
            if (!DisableLogging && HasLoggingThresholdRows && (ProgressCount % LoggingThresholdRows == 0))
                NLogger.Info(TaskName + $" processed {ProgressCount} records.", TaskType, "LOG", TaskHash, ControlFlow.ControlFlow.STAGE, ControlFlow.ControlFlow.CurrentLoadProcess?.Id);
        }

    }

}

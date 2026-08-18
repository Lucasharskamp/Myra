using System;
using System.Collections.Generic;
using Microsoft.Build.Framework; 

namespace Myra.Xaml
{
    public sealed class MyraXamlTaskFactory : ITaskFactory
    {
        public string FactoryName => nameof(MyraXamlTaskFactory);

        public Type TaskType => typeof(MyraXamlCompileTask);

        private TaskPropertyInfo[] TaskParameters { get; set; } = [];

        public bool Initialize(
            string taskName,
            IDictionary<string, TaskPropertyInfo> taskParameters,
            string taskElementContents,
            IBuildEngine taskFactoryLoggingHost)
        {
            TaskParameters =
            [
                new TaskPropertyInfo(
                    nameof(MyraXamlCompileTask.TargetPath),
                    typeof(string),
                    output: false,
                    required: true),

                new TaskPropertyInfo(
                    nameof(MyraXamlCompileTask.XamlFiles),
                    typeof(ITaskItem[]),
                    output: false,
                    required: true),

                new TaskPropertyInfo(
                    nameof(MyraXamlCompileTask.RootNamespace),
                    typeof(string),
                    output: false,
                    required: false),

                new TaskPropertyInfo(
                    nameof(MyraXamlCompileTask.ProjectDirectory),
                    typeof(string),
                    output: false,
                    required: false),

                new TaskPropertyInfo(
                    nameof(MyraXamlCompileTask.Debug),
                    typeof(bool),
                    output: false,
                    required: false),

                new TaskPropertyInfo(
                    nameof(MyraXamlCompileTask.ReferenceAssemblies),
                    typeof(ITaskItem[]),
                    output: false,
                    required: true)
            ];

            return true;
        }

        public ITask CreateTask(IBuildEngine taskFactoryLoggingHost)
        {
            return new MyraXamlCompileTask();
        }

        public TaskPropertyInfo[] GetTaskParameters() => TaskParameters;

        public void CleanupTask(ITask task)
        { 
        }

        public bool TaskTypeIsTaskFactory => true;
    }
}
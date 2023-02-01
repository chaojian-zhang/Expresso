using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Expresso.Core
{
    #region Supporting Types
    public class DependencyStatistics
    {
        public string EntityType { get; set; }
        public int Count { get; set; }
    }
    public class DependencyReportStatus
    {
        public string Message { get; set; }

        public SolidColorBrush Color { get; set; }
    }
    public class DependencyItem
    {
        public string EntityType { get; set; }
        public string Referencer { get; set; }
        public string Referencee { get; set; }
        public object ReferencerObjectReference { get; set; }
        public DependencyReportStatus AdditionalStatus { get; set; }

        public SolidColorBrush Color { get; set; } = Brushes.Black;
    }
    public class GeneralMessage
    {
        public string Headline { get; set; }
        public string Subheadline { get; set; }
        public string Message { get; set; }

        public SolidColorBrush Color { get; internal set; } = Brushes.Black;
    }
    public class DependencyGraph
    {
        public List<DependencyStatistics> Stats { get; set; } = new List<DependencyStatistics>();
        public List<GeneralMessage> Messages { get; set; } = new List<GeneralMessage>();
        public List<DependencyItem> Dependencies { get; set; } = new List<DependencyItem>();
    }
    #endregion

    /// <summary>
    /// Reports entity dependencies and general issues
    /// </summary>
    internal class DependencyAnalyzer
    {
        #region Main
        public DependencyGraph Analyze(ApplicationData applicationData)
        {
            DependencyGraph dependency = new();

            // Report zero workflow (though this won't happen when Dependency Graph tab is available)
            if (applicationData.Workflows.Count == 0)
                dependency.Messages.Add(new GeneralMessage()
                {
                    Headline = "WARNING",
                    Message = "",
                    Color = Brushes.Red
                });

            // Report entity counts
            if (applicationData.Variables.Count != 0)
                dependency.Stats.Add(new DependencyStatistics()
                {
                    EntityType = "Variable",
                    Count = applicationData.Variables.Count
                });
            if (applicationData.Conditionals.Count != 0)
                dependency.Stats.Add(new DependencyStatistics()
                {
                    EntityType = "Condition",
                    Count = applicationData.Conditionals.Count
                });
            if (applicationData.DataReaders.Count != 0)
                dependency.Stats.Add(new DependencyStatistics()
                {
                    EntityType = "Reader",
                    Count = applicationData.DataReaders.Count
                });
            if (applicationData.OutputWriters.Count != 0)
                dependency.Stats.Add(new DependencyStatistics()
                {
                    EntityType = "Writer",
                    Count = applicationData.OutputWriters.Count
                });
            if (applicationData.Processors.Count != 0)
                dependency.Stats.Add(new DependencyStatistics()
                {
                    EntityType = "Row Processor",
                    Count = applicationData.Processors.Count
                });
            if (applicationData.Workflows.Count != 0)
                dependency.Stats.Add(new DependencyStatistics()
                {
                    EntityType = "Workflow",
                    Count = applicationData.Workflows.Count
                });

            // Name Conflicts
            FindNameConflicts(dependency, "Variable", applicationData.Variables.Select(r => r.Name));
            FindNameConflicts(dependency, "Condition", applicationData.Conditionals.Select(r => r.Name));
            FindNameConflicts(dependency, "Reader", applicationData.DataReaders.Select(r => r.Name));
            FindNameConflicts(dependency, "Writer", applicationData.OutputWriters.Select(r => r.Name));
            FindNameConflicts(dependency, "Row Processor", applicationData.Processors.Select(r => r.Name));
            FindNameConflicts(dependency, "Workflow", applicationData.Workflows.Select(r => r.Name));

            // Report unused entities (unused by any workflow).
            // ...

            // Report empty Readers as issue
            // ...
            // Report empty Reader Queries as issue
            // ...

            // Variable Dependancies on Readers
            foreach (var variable in applicationData.Variables)
            {
                if (variable.SourceType == VariableSourceType.Reader)
                    ReportReaderStatus(applicationData, dependency, "Variable", variable.Name, variable.Source, variable);
            }
            // Condition Dependancies on Readers
            foreach (var condition in applicationData.Conditionals)
                ReportReaderStatus(applicationData, dependency, "Condition", condition.Name, condition.ReaderName, condition);
            // Reader Dependancies on Readers
            foreach (var reader in applicationData.DataReaders)
            {
                foreach (var query in reader.DataQueries
                    .Where(q => q.ServiceProvider == ExpressorReaderDataQueryParameter.DisplayName))
                {
                    var parameter = query.Parameters as ExpressorReaderDataQueryParameter;
                    ReportReaderStatus(applicationData, dependency, "Reader Query", $"{reader.Name} - {query.Name}", parameter.ReaderName, query);
                }
            }
            // Workflow Dependancies on Everything Else Other Than Workflows
            foreach (var workflow in applicationData.Workflows)
            {
                dependency.Dependencies.Add(new DependencyItem()
                {
                    EntityType = "Workflow",
                    AdditionalStatus = new DependencyReportStatus()
                    {
                        Message = "Not implemented"
                    }
                });
            }
            // Workflow Dependancies on Workflows
            foreach (var workflow in applicationData.Workflows)
            {
                dependency.Dependencies.Add(new DependencyItem()
                {
                    EntityType = "Workflow",
                    AdditionalStatus = new DependencyReportStatus()
                    {
                        Message = "Not implemented"
                    }
                });
            }

            // Report and aggregate all external sources, and hardcoded paths.
            // ...

            return dependency;
        }
        #endregion

        #region Helpers
        private static void FindNameConflicts(DependencyGraph builder, string category, IEnumerable<string> names)
        {
            var repetition = names.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .ToArray();
            if (repetition.Length != 0)
                builder.Messages.Add(new GeneralMessage()
                {
                    Headline = "Naming Conflict",
                    Subheadline = category,
                    Message = string.Join(", ", repetition),

                    Color = Brushes.Goldenrod
                });
        }
        private static void ReportReaderStatus(ApplicationData applicationData, DependencyGraph builder, string entityType, string entityName, string readerName, object referencer)
        {
            if (string.IsNullOrWhiteSpace(readerName))
                builder.Dependencies.Add(new DependencyItem()
                {
                    EntityType = entityType,
                    Referencer = entityName,
                    Referencee = "Empty Reader Name",
                    ReferencerObjectReference = referencer,
                    AdditionalStatus = null,

                    Color = Brushes.Red
                });
            else
            {
                bool canBeFound = applicationData.DataReaders.Any(r => r.Name == readerName);
                string missingStatus = canBeFound ? null : " Cannot be found.";
                builder.Dependencies.Add(new DependencyItem()
                {
                    EntityType = entityType,
                    Referencer = entityName,
                    Referencee = readerName,
                    ReferencerObjectReference = referencer,
                    AdditionalStatus = missingStatus != null
                        ? new DependencyReportStatus()
                        {
                            Message = missingStatus,
                            Color = Brushes.Red
                        }
                        : null,

                    Color = Brushes.Blue
                });
            }
        }
        #endregion
    }
}

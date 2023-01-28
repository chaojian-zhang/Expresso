using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static Expresso.Core.ApplicationProcessorStep;

namespace Expresso.Core
{
    public static class Evaluation
    {
        #region Reader

        #endregion

        #region Writer

        #endregion

        #region Processor
        public static Dictionary<string, string> TestProcessor(ObservableCollection<ApplicationProcessorStep> startingSteps, Dictionary<string, string> mappedInputs)
        {
            Dictionary<string, string> outputs = new Dictionary<string, string>();
            foreach (ApplicationProcessorStep step in startingSteps)
                outputs.Merge(ProcessorStepActionEvaluator.EvaluateStep(step, mappedInputs));
            return outputs;
        }
        #endregion
    }

    public static class ProcessorStepActionEvaluator
    {
        #region Action Handler Mapping
        private static readonly Dictionary<string, Func<Dictionary<string, string>, Dictionary<string, string>>> ActionHandlers = new()
        {
            { "Print", HandlePrintAction }
        };
        #endregion

        #region Method
        public static Dictionary<string, string> EvaluateStep(ApplicationProcessorStep step, Dictionary<string, string> inputs)
        {
            if (!ActionHandlers.ContainsKey(step.Action))
                return null;

            var currentOutputs = step.Outputs.Pack(ActionHandlers[step.Action](inputs));
            if (step.IsFinalOutput)
                return currentOutputs;
            else
            {
                foreach (ApplicationProcessorStep nextStep in step.NextSteps)
                {
                    var childOutputs = EvaluateStep(nextStep, currentOutputs);
                    if (childOutputs != null)
                        currentOutputs.Merge(childOutputs);
                }
            }

            return currentOutputs;
        }
        #endregion

        #region Action Handlers
        public static Dictionary<string, string> HandlePrintAction(Dictionary<string, string> inputs)
        {
            MessageBox.Show(inputs["Message"]);
            return null;
        }
        #endregion
    }

    public static class ProcessorStepParametersHelper
    {
        public static void Merge(this Dictionary<string, string> currentParameters, Dictionary<string, string> mergeParameters)
        {
            foreach ((string Key, string Value) in mergeParameters)
                currentParameters[Key] = Value;
        }

        public static Dictionary<string, string> Pack(this ObservableCollection<ParameterMapping> parameterMappings, Dictionary<string, string> valuesToGather)
        {
            Dictionary<string, string> remappedValues = new Dictionary<string, string>();
            if (valuesToGather == null) return remappedValues;

            Dictionary<string, string> nameMapping = parameterMappings.ToDictionary(o => o.FromName, o => o.AsName);
            foreach (KeyValuePair<string, string> value in valuesToGather)
                if (nameMapping.TryGetValue(value.Key, out string newName))
                    remappedValues.Add(newName, value.Value);
            return remappedValues;
        }
    }
}

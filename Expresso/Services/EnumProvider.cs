using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expresso.Services
{
    /// <summary>
    /// Remark-cz: We do it this way because enum data binding with Object data provider returns Enums, which when bound to ComboBoxses cause erronous huge render size that's not configurable through combobox item styles
    /// </summary>
    public static class EnumProvider
    {
        public static string[] GetWebRequestMethodTypes() => Enum.GetValues<Core.SupportedWebRequestMethod>().Select(x => x.ToString()).ToArray();
        public static string[] GetConditionTypes() => Enum.GetValues<Core.ConditionType>().Select(x => x.ToString()).ToArray();
    }
}

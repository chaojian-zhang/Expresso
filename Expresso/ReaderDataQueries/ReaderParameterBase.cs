using Expresso.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expresso.ReaderDataQueries
{
    public class ReaderDataQueryParameterBase : BaseNotifyPropertyChanged
    {
        private string _Query = string.Empty;

        public string Query { get => _Query; set => SetField(ref _Query, value); }
    }
}

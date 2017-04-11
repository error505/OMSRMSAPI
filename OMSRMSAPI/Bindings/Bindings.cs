using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject.Modules;
using OMSRMSAPI.Services;

namespace OMSRMSAPI.Bindings
{
    /// <summary>
    /// Register Dependencies
    /// </summary>
    public class Bindings : NinjectModule
    {
        public override void Load()
        {
            Bind<IApiServices>().To<ApiServices>();
        }
    }
}

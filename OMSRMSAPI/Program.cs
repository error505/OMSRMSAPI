using System;
using System.Reflection;
using Ninject;
using OMSRMSAPI.Configuration;

namespace OMSRMSAPI
{
    class Program
    {
        private static readonly ConfigManager _cfgManager = new ConfigManager();
        static void Main(string[] args)
        {
            //Register Dependency
            var kernel = new StandardKernel();
            kernel.Load(Assembly.GetExecutingAssembly());
            var apiService = kernel.Get<IApiServices>();
            //XML URL for parsing
            const string sporUrl = "http://ema.europa.eu/schema/spor";

            Console.WriteLine("Please Write your URL " + _cfgManager.BaseUrl + ": ");

            var url = _cfgManager.BaseUrl + Console.ReadLine();

            if (url.Contains("organisations/ORG-"))
            {
                Console.WriteLine("Please Enter Method (POST,GET,PUT,DELETE): ");

                var meth = Console.ReadLine();
                //Call rest API method
                var response = apiService.CallRestMethod(url, meth);
                apiService.ExctractDataFromOmsXml(response, sporUrl);
            }
            else
            {
                Console.WriteLine("Please Enter Method (POST,GET,PUT,DELETE): ");

                var method = Console.ReadLine();
                //Call rest API method
                var details = apiService.CallRestMethod(url, method);

                var ids = apiService.ProcessResponse(details, sporUrl);

                apiService.CreateOmsXmlDocument(ids, url);
            }
            //Create URL for API

            Console.ReadLine();
        }

    }
}

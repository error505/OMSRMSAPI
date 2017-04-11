using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OMSRMSAPI
{
    public interface IApiServices
    {
        /// <summary>
        /// Call Rest API
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <returns>Return XML document</returns>
        XmlDocument CallRestMethod(string url, string method);
        /// <summary>
        /// Process provided XML file
        /// </summary>
        /// <param name="organisationResponse"></param>
        /// <param name="url"></param>
        List<string> ProcessResponse(XmlDocument organisationResponse, string url);
        /// <summary>
        ///
        /// </summary>
        /// <param name="orgIds"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        XmlDocument CreateOmsXmlDocument(List<string> orgIds, string url);
        /// <summary>
        ///
        /// </summary>
        /// <param name="organisationResponse"></param>
        /// <param name="sporUrl"></param>
        /// <returns></returns>
        XmlDocument ExctractDataFromOmsXml(XmlDocument organisationResponse, string sporUrl);
        /// <summary>
        /// Create XML Document for importing into drugTrack with dataloadTool
        /// </summary>
        /// <param name="xmlValues"></param>
        /// <returns></returns>
        XmlDocument CreateOrganisationXmlDocument(Dictionary<string, string> xmlValues);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using OMSRMSAPI.Configuration;

namespace OMSRMSAPI.Services
{
    /// <summary>
    /// Implementation of IApiServices
    /// </summary>
    public class ApiServices : IApiServices
    {
        //Instantiate Configuration manager
        private static readonly ConfigManager _cfgManager = new ConfigManager();
        private const string SporUrl = "http://ema.europa.eu/schema/spor";
        private const string LocUrl = "http://spor-uat.ema.europa.eu/v1/locations/";

        /// <summary>
        /// Create REST API Method and return result from the call
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <version no="01" date="2017-04-04" author="Igor Iric">
        /// 	<subject>Added ViewDetail</subject>
        /// </version>
        /// <returns>String / XML Response</returns>
        public XmlDocument CallRestMethod(string url, string method)
        {
            //Create web request with provided URL
            var webrequest = (HttpWebRequest)WebRequest.Create(url);

            //Concatenate user name and password
            var credentials = _cfgManager.UserName + ":" + _cfgManager.Password;
            //Basic Authorization
            var authorization = _cfgManager.AuthType + Convert.ToBase64String(Encoding.Default.GetBytes(credentials));
            //set web request method
            webrequest.Method = method;
            //Set Content Type to webRequest
            webrequest.ContentType = _cfgManager.ContType;
            //Set Basic Authentication
            webrequest.Headers["Authorization"] = authorization;
            //Ignore Unsigned Certificate on HTTPS
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            //Try to access rest API request
            try
            {
                //Execute web request
                var webresponse = (HttpWebResponse)webrequest.GetResponse();
                //set encoding
                var enc = Encoding.GetEncoding("utf-8");
                //read response
                var responseStream = new StreamReader(webresponse.GetResponseStream(), enc, true);
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(responseStream);
                return (xmlDoc);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

                Console.Read();

                return null;
            }
        }
        /// <summary>
        /// Process returned XML file
        /// </summary>
        /// <param name="organisationResponse"></param>
        /// <param name="url"></param>
        public List<string> ProcessResponse(XmlDocument organisationResponse, string url)
        {
            //Create name-space manager
            var nsmgr = new XmlNamespaceManager(organisationResponse.NameTable);
            nsmgr.AddNamespace("rest", url);
            //Take list of organizations ID's
            var listOfIds = new List<string>();
            //Select XML node organization
            var organisationElements = organisationResponse.SelectNodes("//rest:organisation", nsmgr);
            //Check if element is not null then loop trough XML doc and take organization ID's
            if (organisationElements == null) return listOfIds;
            foreach (XmlNode location in organisationElements)
            {
                listOfIds.AddRange(from XmlNode organisationChildNode in location.ChildNodes
                    where organisationChildNode.Name == "organisation-id"
                    where organisationChildNode.Attributes != null
                    select organisationChildNode.Attributes["id"].Value);
            }
            //Display all ID's in console window
            return listOfIds;
        }
        /// <summary>
        /// Call EMA OMS API and get and create XML file for organization and location
        /// </summary>
        /// <param name="orgIds"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public XmlDocument CreateOmsXmlDocument(List<string> orgIds, string url)
        {
            //Get XML document for all id's
            foreach (var orgId in orgIds)
            {
                //Call rest API for organization provided by ID
                var orgDoc = CallRestMethod(url + "/" + orgId, "GET");
                //Check if XML document exists
                if (orgDoc == null)
                {
                    continue;
                }
                //Prepare XML
                var nsmgr = new XmlNamespaceManager(orgDoc.NameTable);
                nsmgr.AddNamespace("rest", SporUrl);

                try
                {
                    //Select locations node
                    var organisationElements = orgDoc.SelectNodes("//rest:locations", nsmgr);
                    //If there is location node provided than send organization XML to service where it will extract location info.
                    if (organisationElements != null && organisationElements.Count != 0)
                        ExctractDataFromOmsXml(orgDoc, SporUrl);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);

                    Console.Read();

                    return null;
                }
            }
            return null;
        }
        /// <summary>
        /// Extract all necessary data from OMS XML response file and provide it to creation of company file for drugTrack
        /// </summary>
        /// <param name="organisationResponse"></param>
        /// <param name="sporUrl"></param>
        /// <returns></returns>
        public XmlDocument ExctractDataFromOmsXml(XmlDocument organisationResponse, string sporUrl)
        {
            //Prepare dictionary to store all values from XML
            var values = new Dictionary<string, string>();
            //Create XML Document for location
            var locationDocument = new XmlDocument();
            //Create name-space manager
            var nsmgr = new XmlNamespaceManager(organisationResponse.NameTable);
            nsmgr.AddNamespace("rest", sporUrl);
            var organisationCategoryElements = organisationResponse.SelectNodes("//rest:category-classifications", nsmgr);
            if (organisationCategoryElements != null)
                foreach (XmlNode node in organisationCategoryElements)
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        foreach (XmlNode childNodeChildNode in childNode.ChildNodes)
                        {
                            if (childNodeChildNode.Name != "category") continue;
                            var ch = childNodeChildNode.LastChild;
                            string cat;
                            if (!values.TryGetValue("category", out cat))
                            {
                                values.Add("category", ch.InnerText);
                            }
                        }
                    }
                }
            //Select XML node locations
            //Check if element is not null then loop trough XML doc and take organization ID's
            try
            {
                XmlNodeList organisationElements = organisationResponse.SelectNodes("//rest:locations", nsmgr);
                //If there is no location node exit
                if (organisationElements != null && organisationElements.Count != 0) {
                    foreach (XmlNode node in organisationElements)
                    {
                        foreach (XmlNode childNode in node.ChildNodes)
                        {
                            if (childNode.FirstChild.Name != "location-id") continue;
                            var ch = childNode.FirstChild;
                            if (ch.Attributes == null) continue;
                            var locationId = ch.Attributes["id"].Value;
                            locationDocument = CallRestMethod(LocUrl + locationId, "GET");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("There is no location provided for this organization!! Please provide organization Id that contains Location Id in it! XML will not be Created! Exiting now!");
                    Console.ReadLine();
                    Environment.Exit(10);
                }

                var locationOrgElements = locationDocument.SelectNodes("//rest:organisation", nsmgr);
                // Start with searching for information and extracting those that we need and add it in dictionary
                if (locationOrgElements != null)
                    foreach (XmlNode node in locationOrgElements)
                    {
                        foreach (XmlNode childNode in node.ChildNodes)
                        {
                            if (childNode.Name == "organisation-id")
                            {
                                XmlNode ch = childNode;
                                if (ch.Attributes != null)
                                {
                                    string orgId;
                                    if (!values.TryGetValue("organisationId", out orgId))
                                    {
                                        values.Add("organisationId", ch.Attributes["id"].Value);
                                    }
                                }
                            }
                            if (childNode.Name != "name") continue;
                            string orgName;
                            if (!values.TryGetValue("organisationName", out orgName))
                            {
                                values.Add("organisationName", childNode.InnerText);
                            }
                        }
                    }
                var locationOrgAddress = locationDocument.SelectNodes("//rest:address", nsmgr);
                if (locationOrgAddress != null)
                    foreach (XmlNode node in locationOrgAddress)
                    {
                        foreach (XmlNode childNode in node.ChildNodes)
                        {
                            if (childNode.Name == "postal-code")
                            {
                                string postCode;
                                if (!values.TryGetValue("postalCode", out postCode))
                                {
                                    values.Add("postalCode", childNode.InnerText);
                                }
                            }
                            if (childNode.Name == "country")
                            {
                                foreach (XmlNode childNodes in childNode.ChildNodes)
                                {
                                    if (childNodes.Name == "code")
                                    {
                                        string countryCode;
                                        if (!values.TryGetValue("countryCode", out countryCode))
                                        {
                                            values.Add("countryCode", childNode.FirstChild.InnerText);
                                        }
                                    }
                                    if (childNodes.Name != "display-name") continue;
                                    string countryName;
                                    if (!values.TryGetValue("countryName", out countryName))
                                    {
                                        values.Add("countryName", childNode.LastChild.InnerText);
                                    }
                                }
                            }
                            if (childNode.Name != "address-details") continue;
                            {
                                foreach (XmlNode childNodes in childNode.ChildNodes)
                                {
                                    foreach (XmlNode childChldNodes in childNodes)
                                    {
                                        if (childChldNodes.Name == "address-line-1")
                                        {
                                            string add1;
                                            if (!values.TryGetValue("addres1", out add1))
                                            {
                                                values.Add("addres1", childChldNodes.InnerText);
                                            }
                                        }
                                        if (childChldNodes.Name == "address-line-2")
                                        {
                                            string add2;
                                            if (!values.TryGetValue("addres2", out add2))
                                            {
                                                values.Add("addres2", childChldNodes.InnerText);
                                            }
                                        }
                                        if (childChldNodes.Name == "address-line-2")
                                        {
                                            string city;
                                            if (!values.TryGetValue("city", out city))
                                            {
                                                values.Add("city", childChldNodes.InnerText);
                                            }
                                        }
                                        if (childChldNodes.Name != "address-line-2") continue;
                                        string state;
                                        if (!values.TryGetValue("state", out state))
                                        {
                                            values.Add("state", childChldNodes.InnerText);
                                        }
                                    }
                                }
                            }
                        }
                    }
                // Call function for creation of XML documents and provide dictionary with all extracted values
                CreateOrganisationXmlDocument(values);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

                Console.Read();

                return null;
            }
            return null;
        }

        /// <summary>
        /// Creation of XML file for company that has been ready for import in drugTrack
        /// </summary>
        /// <param name="xmlValues"></param>
        /// <returns></returns>
        public XmlDocument CreateOrganisationXmlDocument(Dictionary<string, string> xmlValues)
        {
            try
            {
                //Initialize location Enumeration
                LocEnum location;
                var enumNum = "";
                string countryCode;
                string organisationId;
                string organisationName;
                string postalCode;
                string countryName;
                string addres1;
                string addres2;
                string city;
                string state;
                string category;

                //Get values from dictionary
                xmlValues.TryGetValue("countryCode", out countryCode);
                xmlValues.TryGetValue("organisationId", out organisationId);
                xmlValues.TryGetValue("organisationName", out organisationName);
                xmlValues.TryGetValue("postalCode", out postalCode);
                xmlValues.TryGetValue("countryName", out countryName);
                xmlValues.TryGetValue("addres1", out addres1);
                xmlValues.TryGetValue("addres2", out addres2);
                xmlValues.TryGetValue("city", out city);
                xmlValues.TryGetValue("state", out state);
                xmlValues.TryGetValue("category", out category);

                //Get Country code ID that exists in drugTrack Demo DataBase for provided country like "DE"
                if (Enum.TryParse<LocEnum>(countryCode, out location))
                {
                    enumNum = ((int)location).ToString();
                }

                //Take current Directory for .exe
                var dir = AppDomain.CurrentDomain.BaseDirectory;

                //Create new GUID for message header
                var messageId = Guid.NewGuid();
                var compAttrib= new XmlCompanyAttributes();
                // Create XML File
                var xmlDoc =
                    new XDocument(new XElement(compAttrib.Body,
                        new XElement(compAttrib.MessageHeader, new XElement(compAttrib.MessageId, messageId.ToString()),
                            new XElement(compAttrib.SenderId, _cfgManager.SenderId),
                            new XElement(compAttrib.TargetVersionNo, _cfgManager.TargetVersionNo),
                            new XElement(compAttrib.TargetPatchLevel, _cfgManager.TargetPatchLevel),
                            new XElement(compAttrib.TargetMetaVersionNo, _cfgManager.TargetMetaVersionNo),
                            new XElement(compAttrib.ContentCulture, _cfgManager.ContentCulture)),
                        new XElement(compAttrib.MessageContent,
                            new XElement(compAttrib.SUS_SUBSIDIARY,
                                new XAttribute(compAttrib.ExternalKey, organisationId),
                                new XElement(compAttrib.T_SUS_SUBSIDIARY_Name_STR, organisationName),
                                new XElement(compAttrib.T_SUS_SUBSIDIARY_CATEGORY_FK,
                                    new XAttribute(compAttrib.ResolutionMode, 3),
                                    new XElement(compAttrib.NewItem,
                                        new XAttribute(compAttrib.ExternalKey, "CAT" + "-" + category),
                                        new XElement(compAttrib.T_SUS_CATEGORY_Description_STR, category))),
                                new XElement(compAttrib.T_SUS_SUBSIDIARY_LOCATION1_FK,
                                    new XAttribute(compAttrib.ResolutionMode, 3),
                                    new XElement(compAttrib.NewItem,
                                        new XAttribute(compAttrib.ExternalKey, (city == null)
                                            ? enumNum + "-" + "EU" + "-" + postalCode
                                            : enumNum + "-" + city + "-" + postalCode
                                        ),
                                        new XElement(compAttrib.T_LOC_LOCATION_NAME_FK,
                                            new XAttribute(compAttrib.ResolutionMode, 3),
                                            new XElement(compAttrib.NewItem,
                                                new XAttribute(compAttrib.ExternalKey, (city == null)
                                                    ? enumNum + "-" + "EU"
                                                    : enumNum + "-" + city),
                                                new XElement(compAttrib.T_LOC_NAME_COUNTRY_FK,
                                                    new XAttribute(compAttrib.ResolutionMode, 2),
                                                    new XElement(compAttrib.Existing_InternalKey, enumNum)),
                                                new XElement(compAttrib.T_LOC_NAME_Name_STR, city ?? "EU"))),
                                        new XElement(compAttrib.T_LOC_LOCATION_Postcode_STR, postalCode))),
                                new XElement(compAttrib.T_SUS_SUBSIDIARY_Street_STR, addres1 + ", " + addres2),
                                new XElement(compAttrib.T_SUS_SUBSIDIARY_Phone_STR, "000-000"),
                                new XElement(compAttrib.T_SUS_SUBSIDIARY_EVCodeAsMAH_STR, organisationId),
                                new XElement(compAttrib.T_SUS_SUBSIDIARY_Building_STR, "Headquarter"),
                                new XElement(compAttrib.T_SUS_SUBSIDIARY_EVCodeAsMFL_STR, "MFLxxx"),
                                new XElement(compAttrib.T_SUS_SUBSIDIARY_SMESTATUS_FK,
                                    new XAttribute(compAttrib.ResolutionMode, 2),
                                    new XElement(compAttrib.Existing_InternalKey, 1))
                            ))));

                // Create XML File name
                var xmlFileName = "organization-" + organisationId + ".xml";
                //Save XML in CreatedXmlFiles folder file
                xmlDoc.Save(Path.Combine(dir, "CreatedXmlFiles", xmlFileName));
                Console.WriteLine(xmlFileName + " has been created. You can use this XML to import organization into drugTrack.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

                Console.Read();

                return null;
            }

            return null;
        }
    }
}

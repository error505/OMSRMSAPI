# OMSRMSAPI
Testing EMA SPOR API

#########################################################################################
# This tool is created to call EMA OMS RMS web API,
to extract all necessary information from response XML's and to create one XML file
that can be imported in other software.
# Version: 1.0
# Date: 04-Apr-2017
# Author: Igor Iric
#########################################################################################

In order to get response from EMA OMS RMS API you need to configure everything in app.config file.
When you run application it will ask you to provide proper URL to access the API.
For getting organization and creation of company XML you can provide URL like this "organisations/ORG-100000196" and to provide "GET" Method,
application will call API URL for organization and extract all necessary data and create XML for DLT.

It is able to take and save XML files for each company in organization list if it has location tag.
In order to get this XML for each organization you just need to type "organisations" and to provide "GET" Method.

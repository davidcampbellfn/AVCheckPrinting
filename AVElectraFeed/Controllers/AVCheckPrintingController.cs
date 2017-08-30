using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using AVCheckPrinting.Models;

using LINQtoCSV;
using AVElectraFeed.Models;

namespace AVCheckPrintingApp.Controllers
{
    public class AVCheckPrintingController : ApiController
    {
        //private string filePath = "C:\\ElectraTest\\electraTestFile.csv";

        //private string directoryPath = "C:\\FTP\\Electra\\ClientData\\";


        [Route("api/AVCheckPrinting")]
        [HttpGet]
        public string GetAVCheckPrinting([FromUri]AVCheckPrintingModel check)
        {
            if (check != null && ModelState.IsValid)
            {
                // Do something with the product (not shown). 

                return check.checkNumber;
            }


            return "error";
        }
    }
}

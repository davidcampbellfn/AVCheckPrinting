using System;
using System.Web.Http;
using AVCheckPrinting.Models;

namespace AVCheckPrintingApp.Controllers
{
    public class AVCheckPrintingController : ApiController
    {
        //private string filePath = "C:\\ElectraTest\\electraTestFile.csv";

        //private string directoryPath = "C:\\FTP\\Electra\\ClientData\\";


        [Route("api/AVCheckPrinting")]
        [HttpPost]
        public string AVCheckPrinting(AVCheckPrintingModel check)
        {
            if (check != null && ModelState.IsValid)
            {
                // Do something with the product (not shown). 
                var  cpws = new CheckPrintingWordService();
                cpws.PreviewChecks(check, Int32.Parse(check.TemplateNumber));
                return check.CheckNumber;
            }


            return "error";
        }
    }
}

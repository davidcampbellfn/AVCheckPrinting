using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using AVElectraApp.Models;

using LINQtoCSV;
using AVElectraFeed.Models;

namespace AVCheckPrintingApp.Controllers
{
    public class AVCheckPrintingController : ApiController
    {
        private string filePath = "C:\\ElectraTest\\electraTestFile.csv";

        private string directoryPath = "C:\\FTP\\Electra\\ClientData\\";

 
        };

        public IEnumerable<ElectraRow> GetAllAVElectra()
        {
            return this.OpenFile(this.filePath);
        }

        private IEnumerable<ElectraRow> ReadElectraRows(string customerId, string accountNumber, DateTime startDate, DateTime endDate)
        {
            return null;
        }

        public IHttpActionResult GetAVElectra(string customerId, string accountNumber, DateTime startDate, DateTime endDate)
        {
            var fileService = new FileService();
            var path = this.directoryPath + customerId;
            var files = fileService.GetDirectoryListing(path);
            var includedFiles = fileService.FilterFilesByDate(startDate, endDate, files, "TRN");
            var electraRows = new List<ElectraRow>();

            foreach(var file in includedFiles)
            {
                var dataRows = this.OpenFile(path + "\\" + file);
                var validRows = dataRows.Where((p) => p.CustomerId == customerId 
                                                && p.AccountNumber == accountNumber
                                                && (DateTime.ParseExact(p.SettlementDate, "yyyyMMdd", CultureInfo.InvariantCulture) >= startDate 
                                                    && DateTime.ParseExact(p.SettlementDate, "yyyyMMdd", CultureInfo.InvariantCulture) <= endDate))
                                        .ToList();
                if(validRows.Any())
                {
                    electraRows.AddRange(validRows);
                }
            }
            
            if (!electraRows.Any())
            {
                return NotFound();
            }
            return Ok(electraRows);
        }


        public IHttpActionResult GetAVElectra(string customerId)
        {
            var fileService = new FileService();
            var path = this.directoryPath + customerId;
            var files = fileService.GetDirectoryListing(path);
            var startDate = DateTime.MinValue;
            var endDate = DateTime.MaxValue;
            var includedFiles = fileService.FilterFilesByDate(startDate, endDate, files, "TRN");

            var accountRows = new List<AccountRow>();

            foreach (var file in includedFiles)
            {
                var dataRows = this.OpenFile(path + "\\" + file);
                var validRows = dataRows.Select(o => new { o.AccountNumber, o.Custodian })
                                       .Distinct().ToList();
                if (validRows.Any())
                {
                    foreach (var row in validRows )
                    {
                        var ac = new AccountRow {Custodian = row.Custodian, AccountNumber = row.AccountNumber };
            
                        if (!accountRows.Any(a => a.AccountNumber == ac.AccountNumber && a.Custodian == ac.Custodian))
                        {
                            accountRows.Add(ac);
                        }
                            
                    }

                }
            }
            accountRows = accountRows.OrderBy(r => r.Custodian).ToList();
            if (!accountRows.Any())
            {
                return NotFound();
            }
            return Ok(accountRows);
        }
        [HttpGet]
        [System.Web.Http.Route("api/GetCustomers")]
        public IHttpActionResult GetCustomers()
        {
            var customers = this.GetCustomersList();
            if (!customers.Any())
            {
                return NotFound();
            }
            {
                var customerRows = new List<CustomerRow>();
                foreach (var c in customers)
                {
                    customerRows.Add(new CustomerRow {Customer = c});
                }
                return Ok(customerRows);
            }
        }

        private List<string> GetCustomersList()
        {
            var fileService = new FileService();
            var path = this.directoryPath;
            return(fileService.GetDirectories(path));
        }

        [Route("api/GetHoldings/{customerId}/{date}")]
        [HttpGet]
        public IHttpActionResult GetHoldings(string customerId, string date)
        {
            var fileService = new FileService();
            var path = this.directoryPath + customerId;
            DateTime dateST = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var formattedDate = dateST.ToString("yyyyMMdd");

            //var files = fileService.GetDirectoryListing(path);
            var holdingsFile = path + "\\" + formattedDate + "_" + customerId + "_" + "HLD.csv";
            var holdingsRows = this.OpenHoldingsFile(holdingsFile);

            if (!holdingsRows.Any())
            {
                return NotFound();
            }
            {
                return Ok(holdingsRows);
            }
        }

        [Route("api/GetPrices/{customerId}/{date}")]
        [HttpGet]
        public IHttpActionResult GetPrices(string customerId, string date)
        {
            var fileService = new FileService();
            var path = this.directoryPath + customerId;
            DateTime dateST = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var formattedDate = dateST.ToString("yyyyMMdd");

            //var files = fileService.GetDirectoryListing(path);
            var pricesFile = path + "\\" + formattedDate + "_" + customerId + "_" + "PRC.csv";
            var PricesRows = this.OpenPricesFile(pricesFile);

            if (!PricesRows.Any())
            {
                return NotFound();
            }
            {
                return Ok(PricesRows);
            }
        }

        [Route("api/GetCash/{customerId}/{date}")]
        [HttpGet]
        public IHttpActionResult GetCash(string customerId, string date)
        {
            var fileService = new FileService();
            var path = this.directoryPath + customerId;
            DateTime dateST = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var formattedDate = dateST.ToString("yyyyMMdd");

            //var files = fileService.GetDirectoryListing(path);
            var cashFile = path + "\\" + formattedDate + "_" + customerId + "_" + "CSH.csv";
            var CashRows = this.OpenCashFile(cashFile);

            if (!CashRows.Any())
            {
                return NotFound();
            }
            {
                return Ok(CashRows);
            }
        }
        //Parses the csv file and returns a list of ElectraRow rows
        public List<ElectraRow> OpenFile(string fullFileName)
        {
            var rows = new List<ElectraRow>();
            try
            {
                //Count CSV Columns
                int columnsCount = 0;
                var tempFolder = string.Format(@"{0}temp\", AppDomain.CurrentDomain.BaseDirectory);
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                var readFile = tempFolder + Guid.NewGuid().ToString();

                System.IO.File.Copy(fullFileName, readFile, true);
                
                var fReader = new StreamReader(File.Open(readFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                string header = fReader.ReadLine();
                if (!String.IsNullOrEmpty(header))
                    columnsCount = header.Split(',').Count();

                //Reset CSV stream reader back to beginning
                fReader.DiscardBufferedData();
                fReader.BaseStream.Seek(0, SeekOrigin.Begin);

                //Define CSV context settings
                var cc = new CsvContext();
                IEnumerable<ElectraRow> tRows = null;
                var inputFileDescription = new CsvFileDescription
                {
                    SeparatorChar = ',',
                    FirstLineHasColumnNames = true
                };

                var newReadFile = tempFolder + Guid.NewGuid().ToString();
                System.IO.File.Copy(readFile, newReadFile, true);

                inputFileDescription.EnforceCsvColumnAttribute = true;
                tRows = cc.Read<ElectraRow>(fReader, inputFileDescription);
                rows = (from x in tRows select x).ToList();

            }
            catch (Exception)
            {
                //throw general file error with ImportTransactionError object as an argument
            }

            return rows;
        }
        //Parses the csv file and returns a list of Holdings rows
        public List<HoldingsRow> OpenHoldingsFile(string fullFileName)
        {
            var rows = new List<HoldingsRow>();
            try
            {
                //Count CSV Columns
                int columnsCount = 0;
                var tempFolder = string.Format(@"{0}temp\", AppDomain.CurrentDomain.BaseDirectory);
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                var readFile = tempFolder + Guid.NewGuid().ToString();

                System.IO.File.Copy(fullFileName, readFile, true);

                var fReader = new StreamReader(File.Open(readFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                string header = fReader.ReadLine();
                if (!String.IsNullOrEmpty(header))
                    columnsCount = header.Split(',').Count();

                //Reset CSV stream reader back to beginning
                fReader.DiscardBufferedData();
                fReader.BaseStream.Seek(0, SeekOrigin.Begin);

                //Define CSV context settings
                var cc = new CsvContext();
                IEnumerable<HoldingsRow> tRows = null;
                var inputFileDescription = new CsvFileDescription
                {
                    SeparatorChar = ',',
                    FirstLineHasColumnNames = true
                };

                var newReadFile = tempFolder + Guid.NewGuid().ToString();
                System.IO.File.Copy(readFile, newReadFile, true);

                inputFileDescription.EnforceCsvColumnAttribute = true;
                tRows = cc.Read<HoldingsRow>(fReader, inputFileDescription);
                rows = (from x in tRows select x).ToList();

            }
            catch (Exception)
            {
                //throw general file error with ImportTransactionError object as an argument
            }

            return rows;
        }
        //Parses the csv file and returns a list of PriceRow rows
        public List<PriceRow> OpenPricesFile(string fullFileName)
        {
            var rows = new List<PriceRow>();
            try
            {
                //Count CSV Columns
                int columnsCount = 0;
                var tempFolder = string.Format(@"{0}temp\", AppDomain.CurrentDomain.BaseDirectory);
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                var readFile = tempFolder + Guid.NewGuid().ToString();

                System.IO.File.Copy(fullFileName, readFile, true);

                var fReader = new StreamReader(File.Open(readFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                string header = fReader.ReadLine();
                if (!String.IsNullOrEmpty(header))
                    columnsCount = header.Split(',').Count();

                //Reset CSV stream reader back to beginning
                fReader.DiscardBufferedData();
                fReader.BaseStream.Seek(0, SeekOrigin.Begin);

                //Define CSV context settings
                var cc = new CsvContext();
                IEnumerable<PriceRow> tRows = null;
                var inputFileDescription = new CsvFileDescription
                {
                    SeparatorChar = ',',
                    FirstLineHasColumnNames = true
                };

                var newReadFile = tempFolder + Guid.NewGuid().ToString();
                System.IO.File.Copy(readFile, newReadFile, true);

                inputFileDescription.EnforceCsvColumnAttribute = true;
                tRows = cc.Read<PriceRow>(fReader, inputFileDescription);
                rows = (from x in tRows select x).ToList();

            }
            catch (Exception)
            {
                //throw general file error with ImportTransactionError object as an argument
            }

            return rows;
        }

        //Parses the csv file and returns a list of CashRow rows
        public List<CashRow> OpenCashFile(string fullFileName)
        {
            var rows = new List<CashRow>();
            try
            {
                //Count CSV Columns
                int columnsCount = 0;
                var tempFolder = string.Format(@"{0}temp\", AppDomain.CurrentDomain.BaseDirectory);
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                var readFile = tempFolder + Guid.NewGuid().ToString();

                System.IO.File.Copy(fullFileName, readFile, true);

                var fReader = new StreamReader(File.Open(readFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                string header = fReader.ReadLine();
                if (!String.IsNullOrEmpty(header))
                    columnsCount = header.Split(',').Count();

                //Reset CSV stream reader back to beginning
                fReader.DiscardBufferedData();
                fReader.BaseStream.Seek(0, SeekOrigin.Begin);

                //Define CSV context settings
                var cc = new CsvContext();
                IEnumerable<CashRow> tRows = null;
                var inputFileDescription = new CsvFileDescription
                {
                    SeparatorChar = ',',
                    FirstLineHasColumnNames = true
                };

                var newReadFile = tempFolder + Guid.NewGuid().ToString();
                System.IO.File.Copy(readFile, newReadFile, true);

                inputFileDescription.EnforceCsvColumnAttribute = true;
                tRows = cc.Read<CashRow>(fReader, inputFileDescription);
                rows = (from x in tRows select x).ToList();

            }
            catch (Exception)
            {
                //throw general file error with ImportTransactionError object as an argument
            }

            return rows;
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace AVCheckPrinting.Models
{
    public struct CheckDistributionItem
    {
        public string Dist { get; set; }

        public double Amount { get; set; }
    }
    public class AVCheckPrintingModel
    {
        public int? CheckNum { get; set; }

        public string RoutingNum { get; set; }

        public string AccountNum { get; set; }

        public string JournalName { get; set; }

        public string PayeeName { get; set; }

        public string PayFor { get; set; }

        public string BankName { get; set; }

        public string BankPhone { get; set; }

        public string BankAddress { get; set; }

        public string BankAddressLine1 { get; set; }

        public string BankAddressLine2 { get; set; }

        public string BankCity { get; set; }

        public string BankState { get; set; }

        public string BankZipCode { get; set; }

        public string BankCountry { get; set; }

        public string PayorName { get; set; }

        public string PayorPhone { get; set; }

        public string PayorAddress { get; set; }

        public string PayorAddressLine1 { get; set; }

        public string PayorAddressLine2 { get; set; }

        public string PayorCity { get; set; }

        public string PayorState { get; set; }

        public string PayorZipCode { get; set; }

        public string PayorCountry { get; set; }

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }

        public string AmountInWords
        {
            get
            {

                int integralPart = (int)Amount;
                int decimalPart = (int)(Amount * 100 % 100);
                var amountInWords = String.Format("{0} AND {1}/100", NumberToWords(integralPart).ToUpperInvariant(), decimalPart);
                return amountInWords;
            }
        }

        public string PayeeAddress { get; set; }

        public string Base64SignatureImage { set; get; }

        public string Memo { get; set; }

        public List<CheckDistributionItem> Distributions { get; set; }

        public string Summary
        { // originally used SmartFormat - see if we work around this extra package
            get
            {
                // This 'data' object is workaround for issue reported here https://github.com/scottrippey/SmartFormat.NET/issues/25
                // When fixed we can directly pass 'this' to FormatSmart method.
                var data = new
                {
                    // Property names here have significance in that they should match tokens in the string template.
                    CheckNum = this.CheckNum.HasValue ? this.CheckNum.ToString() : "",
                    JournalName = this.JournalName,
                    PayeeName = this.PayeeName,
                    PayFor = this.PayFor,
                    Date = this.Date,
                    Amount = this.Amount,
                    Memo = this.Memo,
                };

                return ("{JournalName} \n" +
                        "Chk #    DATE                  PAYEE                       NET AMOUNT \n" +
                        "{CheckNum,-8} {Date:MMM dd yy} {PayeeName,-38}  {Amount:N2} \n" +
                        this.GetDistributionsSummary() +
                        (!String.IsNullOrWhiteSpace(this.Memo)
                            ? " memo    {Memo} "
                            : !String.IsNullOrWhiteSpace(this.PayFor)
                            ? " For:    {PayFor} "
                            : " memo    ") + "\n")
                    .FormatSmart(data);
            }
        }

        private string GetDistributionsSummary()
        {
            var str = "";
            foreach (var dist in this.Distributions)
            {
                var desc = dist.Dist;
                var amount = dist.Amount;
                if (desc != this.JournalName)
                {
                    str += String.Format(amount >= 0
                        ? " dr      {0}{1,16:N2}"
                        : " cr      {0}{1,21:N2}",
                        desc.PadRight(50, '.'), Math.Abs(amount)) + "\n";
                }
            }
            return str;
        }

        private static string NumberToWords(int number)
        {
            if (number == 0)
                return "zero";

            if (number < 0)
                return "minus " + NumberToWords(Math.Abs(number));

            string words = "";

            if ((number / 1000000) > 0)
            {
                words += NumberToWords(number / 1000000) + " million ";
                number %= 1000000;
            }

            if ((number / 1000) > 0)
            {
                words += NumberToWords(number / 1000) + " thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += NumberToWords(number / 100) + " hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "")
                    words += "and ";

                var unitsMap = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
                var tensMap = new[] { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += "-" + unitsMap[number % 10];
                }
            }

            return words.TrimEnd();
        }

    }
}
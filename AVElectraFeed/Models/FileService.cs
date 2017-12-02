using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Office.Core;

using AVCheckPrinting.Utilities;

using Word = Microsoft.Office.Interop.Word;
using SmartFormat;

namespace AVCheckPrinting.Models
{
    public class CheckPrintingWordService
    {
        public AVCheckPrintingModel CreateCheckModel()
        {
            //Contract.Ensures(Contract.Result<ICheckModel>() != null);
            //Contract.Ensures(Contract.Result<ICheckModel>().Distributions != null);

            return new AVCheckPrintingModel
            {
                //Distributions = new List<CheckDistributionItem>()
            };
        }

        public void PreviewChecks(AVCheckPrintingModel checkInfo, int formNum)
        {
            if (checkInfo == null) { throw new ArgumentNullException("checkInfo"); }
            if (formNum < 1) { throw new ArgumentException("FormNum has to be greater than zero", "formNum"); }

            var checkForms = this.GetAvailableCheckForms();
            //Debug.Assert(checkForms.ContainsKey(formNum));
            var filePath = checkForms[formNum];

            var wordDocumentPath = new FileInfo(filePath).FullName;
            //Debug.Assert(wordDocumentPath == filePath);

            if (!File.Exists(wordDocumentPath))
            {
                throw new ArgumentException("Word Template not found: " + wordDocumentPath);
            }

            using (var wordAutomation = new WordAutomation())
            {
                wordAutomation.FillOutCheckTemplate(wordDocumentPath, checkInfo);
            }
        }

        public Dictionary<int, string> GetAvailableCheckForms()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var templatesDir = "c:/AVCheckPrintingTemplates";//Path.Combine(baseDir, "Templates");
            var templateFiles = Directory.EnumerateFiles(templatesDir, "*.dot?");

            var regex = new System.Text.RegularExpressions.Regex(@"^\d+(?=\.)", System.Text.RegularExpressions.RegexOptions.Compiled);
            return templateFiles.Select(p =>
            {
                var m = regex.Match(Path.GetFileName(p) ?? "");
                return new KeyValuePair<int, string>(m.Success ? Int32.Parse(m.Value) : -1, p);
            })
            .Where(p => p.Key > 0) // ignore no match (-1) and file names starting with 0
            .GroupBy(p => p.Key).Select(g => g.First()) // remove duplicates
            .ToDictionary(p => p.Key, p => p.Value);
        }

        private class WordAutomation : IDisposable
        {
            private Word.Application word = null;

            public void FillOutCheckTemplate(string wordDocumentPath, AVCheckPrintingModel checkInfo)
            {
                Func<string, bool> isTemplate = (str) => str.Contains('{') && str.Contains('}');

                Word.Document document = null;
                try
                {
                    if (word == null)
                    {
                        word = new Word.Application { Visible = false };
                    }

                    document = word.Documents.Add(wordDocumentPath, true);

                    const string PASSWORD = "AllowEdit10";
                    if (document.ProtectionType != Word.WdProtectionType.wdNoProtection)
                    {
                        document.Unprotect(PASSWORD);
                    }

                    var range = document.Range(Start: 0);
                    range.Copy(); // Copy all to clipboard

                    int count = 1;
                    checkInfo.Memo = "Marks 3 and 3";
                    checkInfo.Amount = 100.33m;
                    var data = new
                    {
                        // Property names here have significance in that they should match texbox tokens of same name in the Word template.
                        CheckNum = checkInfo.CheckNumber,
                        RoutingNum = checkInfo.RoutingNumber,
                        AccountNum = checkInfo.AccountNumber,
                        BankName = "Bank Of America",//checkInfo.BankName,
                        BankPhone = "(408) 444-4444",//checkInfo.BankPhone,
                        BankAddress = "123 Bank Lane",//checkInfo.BankAddress,
                        BankAddressLine1 = "",//checkInfo.BankAddressLine1,
                        BankAddressLine2 = "",//checkInfo.BankAddressLine2,
                        BankCity = "Pune",//checkInfo.BankCity,
                        BankState = "CA",//checkInfo.BankState,
                        BankZipCode = "95050",//checkInfo.BankZipCode,
                        BankCountry = "USA",//checkInfo.BankCountry,
                        JournalName = checkInfo.JournalName,
                        PayeeName = "InandOut",//checkInfo.PayeeName,
                        PayorName = "InandOut",//checkInfo.PayorName,
                        PayorPhone = "",//checkInfo.PayorPhone,
                        PayorAddress = "123 Burger Ave.",//checkInfo.PayorAddress,
                        PayorAddressLine1 = "",//checkInfo.PayorAddressLine1,
                        PayorAddressLine2 = "",//checkInfo.PayorAddressLine2,
                        PayorCity = "San Jose",//checkInfo.PayorCity,
                        PayorState = "CA",//checkInfo.PayorState,
                        PayorZipCode = "95123",//checkInfo.PayorZipCode,
                        PayorCountry = "USA",//checkInfo.PayorCountry,
                        Date = DateTime.Today,//checkInfo.Date,
                        Amount = 100.00,//checkInfo.Amount,
                        AmountInWords = checkInfo.AmountInWords,
                        PayeeAddress = checkInfo.PayeeAddress,
                        For = checkInfo.PayFor,
                        Memo = // memo with prefix
                                !String.IsNullOrWhiteSpace(checkInfo.Memo)
                                ? "Memo: " + checkInfo.Memo
                                : !String.IsNullOrWhiteSpace(checkInfo.PayFor)
                                ? "For: " + checkInfo.PayFor
                                : "",
                        MemoOnly = checkInfo.Memo,
                        Summary = checkInfo.Summary,
                        Signature = ""
                    };

                    foreach (Word.Shape shape in range.ShapeRange)
                    {
                        if (shape.Type != MsoShapeType.msoTextBox) { continue; }

                        var fieldTemplate = "";
                        if (isTemplate(shape.AlternativeText))
                        {
                            // Get field template from shape's alternative text. In Word 2013 it is at:
                            // Format Shape > Shape Options > Layout & Properties > ALT TEXT > Description
                            fieldTemplate = shape.AlternativeText;
                            // In Word 97-2003 there are no separate fields for Title and Description. So
                            // when opening a document that was created in newer version of Word with Title
                            // and Description, in Word 97-2003 the Alt Text may show as
                            // "Title: {title} - Description: {description}". In this case we extract the
                            // description part:
                            const string descToken = "Description: ";
                            if (fieldTemplate.StartsWith("Title: ") && fieldTemplate.Contains(descToken))
                            {
                                fieldTemplate = fieldTemplate.SubstringAfter(descToken);
                            }
                            // Use it only if shape's text is not empty, otherwise discard ALT TEXT. Shape
                            // has to have a dummy text in it for it to not become hidden in the template.
                            if (String.IsNullOrWhiteSpace(shape.TextFrame.ContainingRange.Text))
                            {
                                fieldTemplate = "";
                            }
                        }
                        else
                        {
                            // If ALT TEXT does not look like a template then look for it in the TextBox text.
                            fieldTemplate = shape.TextFrame.ContainingRange.Text;
                            if (!isTemplate(fieldTemplate))
                            {
                                continue;
                            }
                        }

                        // If current template contains "{AmountInWords}" token, check if it needs ***padding***.
                        const string AmountInWordsToken = "{AmountInWords"; // intentionally not closed with '}'
                        if (fieldTemplate.Contains(AmountInWordsToken))
                        {
                            var text = fieldTemplate.FormatSmart(data);
                            var trimmedTemplate = fieldTemplate.Trim();
                            // Pad with asterisks if any asterisk character appears around the template
                            if (trimmedTemplate.StartsWith("*") || trimmedTemplate.EndsWith("*"))
                            {
                                text = FixPadding(text);
                            }
                            shape.TextFrame.ContainingRange.Text = text;
                            continue;
                        }

                        //const string SignatureToken = "{Signature}";
                        //if (fieldTemplate.Contains(SignatureToken))
                        //{
                        //    const string imageMetaData = "data:image/png;base64,";
                        //    if (checkInfo.Base64SignatureImage.StartsWith(imageMetaData))
                        //    {
                        //        var base64Image = checkInfo.Base64SignatureImage.Substring(imageMetaData.Length);
                        //        var bytes = Convert.FromBase64String(base64Image);
                        //        Image image;
                        //        using (MemoryStream ms = new MemoryStream(bytes))
                        //        {
                        //            image = Image.FromStream(ms);
                        //        }
                        //        var imageFile = Path.GetTempPath() + "polaris_temp_image.png";
                        //        if (File.Exists(imageFile))
                        //        {
                        //            File.Delete(imageFile);
                        //        }
                        //        try
                        //        {
                        //            image.Save(imageFile);
                        //            var scaling = 1.0;
                        //            //points = pixels * 72 / 96
                        //            var w = scaling * image.Width * 0.75;
                        //            var h = scaling * image.Height * 0.75;
                        //            document.Shapes.AddPicture(
                        //                FileName: imageFile,
                        //                LinkToFile: false,
                        //                SaveWithDocument: true,
                        //                Left: shape.Left - (w - shape.Width) / 2,
                        //                Top: shape.Top - (h - shape.Height) / 2,
                        //                Width: w,
                        //                Height: h,
                        //                Anchor: range);
                        //        }
                        //        finally
                        //        {
                        //            if (File.Exists(imageFile))
                        //            {
                        //                File.Delete(imageFile);
                        //            }
                        //        }
                        //    }

                        //    shape.TextFrame.ContainingRange.Text = "";
                        //    continue;
                        //}

                        shape.TextFrame.ContainingRange.Text = fieldTemplate.FormatSmart(data);

                        // For some reason changing shape name will adversely affect the performance (makes it twice as slow). Commenting this out.
                        //// change shape's name to make sure it is not modified again
                        //shape.Name += "_" + count;
                    }


                    //word.Selection.EndKey(Word.WdUnits.wdStory); // Go to end of doc
                    //word.Selection.InsertBreak(Word.WdBreakType.wdPageBreak);
                    //document.Bookmarks[@"\EndOfDoc"].Range.Paste();

                    //range.SetRange(range.End, word.Selection.End);

                    //count++;
                    
                    document.SaveAs("c:/users/dcamp/documents/AVCheckPrintingTest/test.doc", Word.WdSaveFormat.wdFormatDocument);
                    document.Close();
                    // Protect document with password to restrict editing.
                    //document.Protect(Word.WdProtectionType.wdAllowOnlyReading, Password: PASSWORD);

                    // Clear template from clipboard
                    //ServiceLocator.Current.GetInstance<IClipboardManager>().ClearClipboard();
                    //ClipboardManager clipBoard = new ClipboardManager();
                    //clipBoard.ClearClipboard();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(string.Format("Error on file {0}", wordDocumentPath), ex);
                }

                finally
                {
                    if (document != null)
                    {
                        Marshal.ReleaseComObject(document);
                    }
                }
            }

            private static string FixPadding(string str, char paddingChar = '*', int totalLength = 64)
            {
                str = str.Trim(/*white-spaces*/).Trim(new[] { paddingChar }).Trim(/*white-spaces again*/);
                if (str.Length >= totalLength) { return str; }
                var padding = new String(paddingChar, (totalLength - str.Length) / 2);
                var strWithPadding = padding + str + padding;
                if (strWithPadding.Length < totalLength)
                {
                    strWithPadding += paddingChar;
                }
                return strWithPadding;
            }

            public void Dispose()
            {
                if (word != null)
                {
                    //word.Quit(false, Missing.Value, Missing.Value);
                    Marshal.ReleaseComObject(word);
                }
            }
        }

    }
    public class FileService
    {
        public enum FileTypes
        {
            transaction,
            holdings,
            price,
            cash   
        }
        public List<string> GetDirectories(string filePath)
        {
            var directoryEntries = Directory.GetDirectories(filePath).Select(Path.GetFileName).ToList();
            return directoryEntries;
        }

        public List<string> GetDirectoryListing(string filePath)
        {
            var fileEntries = Directory.GetFiles(filePath, "*.csv").Select(Path.GetFileName).ToList();
            return fileEntries;
        }

        public List<string> FilterFilesByDate(DateTime startDate, DateTime endDate, List<string> fileNames, string fileType)
        {
            var includedFiles = new List<string>();

            foreach(var file in fileNames)
            {
                var splitFileName = file.Split('_');

                if (file.Contains(fileType))
                {
                    var fileStartDate = splitFileName[0];
                    var fileEndDate = splitFileName[1];
                    var formattedStartDate = DateTime.ParseExact(fileStartDate, "yyyyMMdd", CultureInfo.InvariantCulture);
                    var formattedEndDate = DateTime.ParseExact(fileEndDate, "yyyyMMdd", CultureInfo.InvariantCulture);
                    if ((formattedStartDate >= startDate && formattedStartDate <= endDate) 
                        || (formattedEndDate >= startDate && formattedEndDate <= endDate))
                    {
                        includedFiles.Add(file);
                    }
                }
            }

            return includedFiles;
        } 
    }
}
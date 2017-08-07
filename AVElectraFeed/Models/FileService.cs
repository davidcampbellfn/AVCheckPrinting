using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace AVElectraFeed.Models
{
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
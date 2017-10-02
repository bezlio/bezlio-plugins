using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace bezlio.rdb.plugins
{
    public class ExcelMergeDataModel
    {
        public string Context { get; set; }
        public string[] FileLocations { get; set; }
        public FileDetailModel[] FileDetails { get; set; }
        //public string[] FirstRowColumnNames { get; set; }
        // public string AllStrings { get; set; }

        public ExcelMergeDataModel() { }
    }

    public class FileDetailModel
    {
        public string FileName { get; set; }
        public string UserName { get; set; }
        public string Login { get; set; }
        public string Logout { get; set; }
        public string Date { get; set; }
        public string Minutes { get; set; }

        public FileDetailModel() { }
    }

    public class ExcelMergePlugin
    {
        public static object GetArgs()
        {

            ExcelMergeDataModel model = new ExcelMergeDataModel();
            model.Context = GetFolderNames();

            return model;
            //model.FileNames = "An array of full paths to the Excel files";
            // model.SheetName = "Sheet name";
            // model.FirstRowColumnNames = "[Yes,No]";
            // model.SheetData = "Data to be written (only used for WriteFile)";
            //   model.AllStrings = "[Yes,No]";
        }

        public static List<FileLocation> GetLocations()
        {
            string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgPath = asmPath + @"\" + "FileSystem.dll.config";
            string strLocations = "";
            if (File.Exists(cfgPath))
            {
                // Load in the cfg file
                XDocument xConfig = XDocument.Load(cfgPath);

                // Get the setting for the debug log destination
                XElement xLocations = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "fileSystemLocations").FirstOrDefault();
                if (xLocations != null)
                {
                    strLocations = xLocations.Value;
                }
            }
            return JsonConvert.DeserializeObject<List<FileLocation>>(strLocations);
        }

        public static string GetFolderNames()
        {
            var result = "[";
            foreach (var location in GetLocations())
            {
                result += location.LocationName + ",";
            }
            result.TrimEnd(',');
            result += "]";
            return result;
        }

        public static async Task<RemoteDataBrokerResponse> GetFileList(RemoteDataBrokerRequest rdbRequest)
        {
            ExcelMergeDataModel request = JsonConvert.DeserializeObject<ExcelMergeDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // Settings do not seem to reflect in cleanly, we will read the settings directly
                string asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cfgPath = asmPath + @"\" + "FileSystem.dll.config";
                string strLocations = "";

                if (File.Exists(cfgPath))
                {
                    // Load in the cfg file
                    XDocument xConfig = XDocument.Load(cfgPath);

                    // Get the setting for the folder locations
                    XElement xLocations = xConfig.Descendants("bezlio.plugins.Properties.Settings").Descendants("setting").Where(a => (string)a.Attribute("name") == "fileSystemLocations").FirstOrDefault();
                    if (xLocations != null)
                    {
                        strLocations = xLocations.Value;
                    }
                }

                // Deserialize the values from Settings
                List<FileLocation> locations = JsonConvert.DeserializeObject<List<FileLocation>>(strLocations);
                string locationPath = locations.Where((l) => l.LocationName.Equals(request.Context)).FirstOrDefault().LocationPath;

                // Return the data table
                List<dynamic> result = new List<dynamic>();
                foreach (var f in Directory.GetFiles(locationPath, @"*", SearchOption.AllDirectories))
                {
                    FileInfo fi = new FileInfo(f);
                    result.Add(new
                    {
                        Attributes = fi.Attributes,
                        CreationTime = fi.CreationTime,
                        CreationTimeUtc = fi.CreationTimeUtc,
                        Directory = fi.Directory,
                        DirectoryName = fi.DirectoryName,
                        Exists = fi.Exists,
                        Extension = fi.Extension,
                        FullName = fi.FullName,
                        IsReadOnly = fi.IsReadOnly,
                        LastAccessTime = fi.LastAccessTime,
                        LastAccessTimeUtc = fi.LastAccessTimeUtc,
                        LastWriteTime = fi.LastWriteTime,
                        LastWriteTimeUtc = fi.LastWriteTimeUtc,
                        Length = fi.Length,
                        Name = fi.Name,
                        BaseName = Path.GetFileNameWithoutExtension(f)
                    });
                }
                response.Data = JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = Environment.MachineName + ": " + ex.Message;
            }

            // Return our response
            return response;
        }

        // This method uses ExcelDataReader (https://github.com/ExcelDataReader/ExcelDataReader) for high-performance
        // data reads from Excel
        public static async Task<RemoteDataBrokerResponse> GetData(RemoteDataBrokerRequest rdbRequest)
        {
            ExcelMergeDataModel request = JsonConvert.DeserializeObject<ExcelMergeDataModel>(rdbRequest.Data);
            //FileDetailModel[] fileDetails = JsonConvert.DeserializeObject<FileDetailModel>(request.FileDetails);

            // Form dictonary for dynamic switch state
            Dictionary<int, string> dict = new Dictionary<int, string>();
            for(var i = 0; i < request.FileDetails.Length; i++)
            {
                for(var j = 0; j < 5; j++)
                {
                    dict.Add(j, request.FileDetails[j].)
                }
            }


            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";


            DataTable final_dt = new DataTable();

            // Get final dataTable
            ExcelWorksheet finalWorksheet;
            using (ExcelPackage finalPackage = new ExcelPackage(new FileInfo(request.Context)))
            {
                finalWorksheet = finalPackage.Workbook.Worksheets[1];
                // calc formulas
                //finalWorksheet.Calculate();

                //add the columns to the datatable
                for (int j = finalWorksheet.Dimension.Start.Column; j <= finalWorksheet.Dimension.End.Column; j++)
                {
                    string columnName = "Column " + j;
                    var excelCell = finalWorksheet.Cells[1, j].Value;

                    if (excelCell != null)
                    {
                            var excelCellDataType = excelCell;

                            
                                excelCellDataType = finalWorksheet.Cells[2, j].Value;

                                columnName = excelCell.ToString();

                                //check if the column name already exists in the datatable, if so make a unique name
                               /* if (dt.Columns.Contains(columnName) == true)
                                {
                                    columnName = columnName + "_" + j;
                                }*/

                            //try to determine the datatype for the column (by looking at the next column if there is a header row)
                            if (excelCellDataType is DateTime)
                            {
                            final_dt.Columns.Add(columnName, typeof(DateTime));
                            }
                            else if (excelCellDataType is Boolean)
                            {
                            final_dt.Columns.Add(columnName, typeof(Boolean));
                            }
                            else if (excelCellDataType is Double)
                            {
                                //determine if the value is a decimal or int by looking for a decimal separator
                                //not the cleanest of solutions but it works since excel always gives a double
                                if (excelCellDataType.ToString().Contains(".") || excelCellDataType.ToString().Contains(","))
                                {
                                final_dt.Columns.Add(columnName, typeof(Decimal));
                                }
                                else
                                {
                                final_dt.Columns.Add(columnName, typeof(Int64));
                                }
                            }
                            else
                            {
                            final_dt.Columns.Add(columnName, typeof(String));
                            }
                    }
                    else
                    {
                        final_dt.Columns.Add(columnName, typeof(String));
                    }
                }

                //start adding data the datatable here by looping all rows and columns
                for (int i = finalWorksheet.Dimension.Start.Row + 1; i <= finalWorksheet.Dimension.End.Row; i++)
                {
                    //create a new datatable row
                    DataRow row = final_dt.NewRow();

                    //loop all columns
                    for (int j = finalWorksheet.Dimension.Start.Column; j <= finalWorksheet.Dimension.End.Column; j++)
                    {
                        var excelCell = finalWorksheet.Cells[i, j].Value;

                        //add cell value to the datatable
                        if (excelCell != null)
                        {
                            try
                            {
                                row[j - 1] = excelCell;
                            }
                            catch
                            {
                                response.Error = true;
                                response.ErrorText += "Row " + (i - 1) + ", Column " + j + ". Invalid " + final_dt.Columns[j - 1].DataType.ToString().Replace("System.", "") + " value:  " + excelCell.ToString() + "<br>";
                            }
                        }
                    }

                    //add the new row to the datatable
                    final_dt.Rows.Add(row);
                }
            }

            // Get all files, store in array
            var listOfStrings = new List<string>();
            List<ExcelWorksheet> worksheets = new List<ExcelWorksheet>();
            for (var j = 0; j < request.FileLocations.Length-2; j++)
            {
                using (ExcelPackage package = new ExcelPackage(new FileInfo(request.FileLocations[j])))
                {
                    worksheets.Insert(j, package.Workbook.Worksheets[1]);
                    //worksheets[j] = package.Workbook.Worksheets[1];
                    // calc formulas
                    worksheets[j].Calculate();

                    //check if the worksheet is completely empty
                    if (worksheets[j].Dimension == null)
                    {
                        // if worksheet is null, remove it
                        worksheets.RemoveAt(j);
                    }

                  //  if (package.File.Name.Contains("buzz"))
                   // {
                        DataTable temp_dt = new DataTable();
                        //start adding data the datatable here by looping all rows and columns
                        for (int i = worksheets[j].Dimension.Start.Row + 1/*+ Convert.ToInt32(request.FirstRowColumnNames[0] == "Yes")*/; i <= worksheets[j].Dimension.End.Row; i++)
                        {
                            //create a new datatable row
                            DataRow tempRow = final_dt.NewRow();

                            //loop all columns
                            for (int x = worksheets[j].Dimension.Start.Column; x <= worksheets[j].Dimension.End.Column; x++)
                            {
                                var excelCell = worksheets[j].Cells[i, x].Value;
                                var columnNumber = 0;

                                switch (worksheets[j].Cells[1, x].Value.ToString())
                                {

                                    case request.FileDetails[i].UserName.ToString():
                                        excelCell = worksheets[j].Cells[i, x].Value;
                                        columnNumber = 0;
                                        break;
                                    case "Login":
                                        excelCell = worksheets[j].Cells[i, x].Value;
                                        columnNumber = 1;
                                        break;
                                    case "Logout":
                                        excelCell = worksheets[j].Cells[i, x].Value;
                                        columnNumber = 2;
                                        break;
                                    default:
                                        excelCell = null;
                                        break;
                                }


                                //add cell value to the datatable
                                if (excelCell != null)
                                {
                                    try
                                    {
                                        tempRow[columnNumber] = excelCell;
                                    }
                                    catch
                                    { 
                                        response.Error = true;
                                        response.ErrorText += "Row " + (i - 1) + ", Column " + x + ". Invalid " + temp_dt.Columns[x - 1].DataType.ToString().Replace("System.", "") + " value:  " + excelCell.ToString() + "<br>";
                                    }
                                }
                            }

                            // Removes .xlsx from name
                            tempRow[3] = package.File.Name.Remove(package.File.Name.Length-5, 5);

                            // Figure out difference between DateTimes
                            tempRow[4] = (Convert.ToDateTime(tempRow[2].ToString()) - Convert.ToDateTime(tempRow[1].ToString())).TotalSeconds;

                            //add the new row to the datatable
                            final_dt.Rows.Add(tempRow);

                        }

                        try
                        {
                            // Now write this to an Excel file
                            using (ExcelPackage writePackage = new ExcelPackage(new FileInfo("C:\\Users\\nowens-local\\Desktop\\QDA_Final.xlsx")))
                            {
                                ExcelWorksheet worksheet;
                                var existingWs = writePackage.Workbook.Worksheets.Where(s => s.Name.Equals("Sheet1"));
                                if (existingWs.Count() == 0)
                                {
                                    worksheet = writePackage.Workbook.Worksheets.Add("Sheet1");
                                }
                                else
                                {
                                    worksheet = existingWs.First();
                                }
                                worksheet.Cells["A1"].LoadFromDataTable(final_dt, true);
                                writePackage.Save();
                            }
                        }
                        catch (Exception ex)
                        {
                            response.Error = true;
                            response.ErrorText = ex.Message;
                        }
                   // }
                    

                }
            }
            /*try
            {
                DataTable dt = new DataTable();

                // First use EPPlus to calculate all formulas so the user is getting fresh data
                using (ExcelPackage package = new ExcelPackage(new FileInfo(request.FileLocations[0])))
                {
                    //var wks = package.Workbook.Worksheets.Where(s => s.Name.Equals(request.SheetName));
                    ExcelWorksheet worksheet;
                    /* if (wks.Count() > 0)
                     {
                         worksheet = wks.First();
                     }
                     else
                     {
                         // Excel index starts at 1...
                         worksheet = package.Workbook.Worksheets[1];
                     }*/
                  /*  worksheet = package.Workbook.Worksheets[1];

                    worksheet.Calculate();

                    //check if the worksheet is completely empty
                    if (worksheet.Dimension == null)
                    {
                        response.Error = true;
                        response.ErrorText = "Empty Worksheet";
                    }

                    //add the columns to the datatable
                    for (int j = worksheet.Dimension.Start.Column; j <= worksheet.Dimension.End.Column; j++)
                    {
                        string columnName = "Column " + j;
                        var excelCell = worksheet.Cells[1, j].Value;

                        if (excelCell != null)
                        {
                            if (request.AllStrings == "Yes")
                            {
                                dt.Columns.Add(columnName, typeof(String));
                            }
                            else
                            {
                                var excelCellDataType = excelCell;

                                //if there is a headerrow, set the next cell for the datatype and set the column name
                                if (request.FirstRowColumnNames[0] == "Yes")
                                {
                                    excelCellDataType = worksheet.Cells[2, j].Value;

                                    columnName = excelCell.ToString();

                                    //check if the column name already exists in the datatable, if so make a unique name
                                    if (dt.Columns.Contains(columnName) == true)
                                    {
                                        columnName = columnName + "_" + j;
                                    }
                                }

                                //try to determine the datatype for the column (by looking at the next column if there is a header row)
                                if (excelCellDataType is DateTime)
                                {
                                    dt.Columns.Add(columnName, typeof(DateTime));
                                }
                                else if (excelCellDataType is Boolean)
                                {
                                    dt.Columns.Add(columnName, typeof(Boolean));
                                }
                                else if (excelCellDataType is Double)
                                {
                                    //determine if the value is a decimal or int by looking for a decimal separator
                                    //not the cleanest of solutions but it works since excel always gives a double
                                    if (excelCellDataType.ToString().Contains(".") || excelCellDataType.ToString().Contains(","))
                                    {
                                        dt.Columns.Add(columnName, typeof(Decimal));
                                    }
                                    else
                                    {
                                        dt.Columns.Add(columnName, typeof(Int64));
                                    }
                                }
                                else
                                {
                                    dt.Columns.Add(columnName, typeof(String));
                                }
                            }
                        }
                        else
                        {
                            dt.Columns.Add(columnName, typeof(String));
                        }
                    }

                    //start adding data the datatable here by looping all rows and columns
                    for (int i = worksheet.Dimension.Start.Row + Convert.ToInt32(request.FirstRowColumnNames[0] == "Yes"); i <= worksheet.Dimension.End.Row; i++)
                    {
                        //create a new datatable row
                        DataRow row = dt.NewRow();

                        //loop all columns
                        for (int j = worksheet.Dimension.Start.Column; j <= worksheet.Dimension.End.Column; j++)
                        {
                            var excelCell = worksheet.Cells[i, j].Value;

                            //add cell value to the datatable
                            if (excelCell != null)
                            {
                                try
                                {
                                    row[j - 1] = excelCell;
                                }
                                catch
                                {
                                    response.Error = true;
                                    response.ErrorText += "Row " + (i - 1) + ", Column " + j + ". Invalid " + dt.Columns[j - 1].DataType.ToString().Replace("System.", "") + " value:  " + excelCell.ToString() + "<br>";
                                }
                            }
                        }

                        //add the new row to the datatable
                        dt.Rows.Add(row);
                    }

                }

                response.Data = JsonConvert.SerializeObject(dt);

            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = ex.Message;
            }*/


            return response;
        }

        /*public static async Task<RemoteDataBrokerResponse> WriteFile(RemoteDataBrokerRequest rdbRequest)
        {
            ExcelDataModel request = JsonConvert.DeserializeObject<ExcelDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            // Make sure this is being saved as an xlsx, otherwise throw back an error
            if (!request.FileName.EndsWith("xlsx"))
            {
                response.Error = true;
                response.ErrorText = "Invalid file name.  Must be an xlsx file.";
            }
            else
            {
                try
                {
                    DataTable dt = JsonConvert.DeserializeObject<DataTable>(request.SheetData);

                    // Now write this to an Excel file
                    using (ExcelPackage package = new ExcelPackage(new FileInfo(request.FileName)))
                    {
                        ExcelWorksheet worksheet;
                        var existingWs = package.Workbook.Worksheets.Where(s => s.Name.Equals(request.SheetName));
                        if (existingWs.Count() == 0)
                        {
                            worksheet = package.Workbook.Worksheets.Add(request.SheetName);
                        }
                        else
                        {
                            worksheet = existingWs.First();
                        }
                        worksheet.Cells["A1"].LoadFromDataTable(dt, request.FirstRowColumnNames == "Yes");
                        package.Save();
                    }
                }
                catch (Exception ex)
                {
                    response.Error = true;
                    response.ErrorText = ex.Message;
                }
            }

            return response;
        }*/

    }
}

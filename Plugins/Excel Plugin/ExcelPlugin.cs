using Excel;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace bezlio.rdb.plugins
{
    public class ExcelDataModel
    {
        public string FileName { get; set; }
        public string SheetName { get; set; }
        public string FirstRowColumnNames { get; set; }
        public string SheetData { get; set; }

        public ExcelDataModel() { }
    }
    public class ExcelPlugin
    {
        public static object GetArgs()
        {

            ExcelDataModel model = new ExcelDataModel();

            model.FileName = "The full path to the Excel file";
            model.SheetName = "Sheet name";
            model.FirstRowColumnNames = "[Yes,No]";
            model.SheetData = "Data to be written (only used for WriteFile)";

            return model;
        }

        // This method uses ExcelDataReader (https://github.com/ExcelDataReader/ExcelDataReader) for high-performance
        // data reads from Excel
        public static async Task<RemoteDataBrokerResponse> GetData(RemoteDataBrokerRequest rdbRequest)
        {
            ExcelDataModel request = JsonConvert.DeserializeObject<ExcelDataModel>(rdbRequest.Data);

            // Declare the response object
            RemoteDataBrokerResponse response = new RemoteDataBrokerResponse();
            response.Compress = rdbRequest.Compress;
            response.RequestId = rdbRequest.RequestId;
            response.DataType = "applicationJSON";

            try
            {
                // First use EPPlus to calculate all formulas so the user is getting fresh data
                using (ExcelPackage package = new ExcelPackage(new FileInfo(request.FileName)))
                {
                    package.Workbook.Calculate();
                    package.Save();
                }

                // Now grab the data
                FileStream stream = File.Open(request.FileName, FileMode.Open, FileAccess.Read);
                IExcelDataReader excelReader;

                if (request.FileName.EndsWith("xls"))
                {
                    excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else
                {
                    excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }

                if (request.FirstRowColumnNames == "Yes")
                {
                    excelReader.IsFirstRowAsColumnNames = true;
                }

                int sheetNumber;
                bool sheetNameIsNumber = int.TryParse(request.SheetName.ToString(), out sheetNumber);

                if (string.IsNullOrEmpty(request.SheetName) && !sheetNameIsNumber)
                    response.Data = JsonConvert.SerializeObject(excelReader.AsDataSet().Tables[0]);
                else if (sheetNameIsNumber)
                    response.Data = JsonConvert.SerializeObject(excelReader.AsDataSet().Tables[sheetNumber]);
                else
                    response.Data = JsonConvert.SerializeObject(excelReader.AsDataSet().Tables[request.SheetName]);

            } catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = ex.Message;
            }


            return response;
        }

        public static async Task<RemoteDataBrokerResponse> WriteFile(RemoteDataBrokerRequest rdbRequest)
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
            } else
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
        }

    }
}

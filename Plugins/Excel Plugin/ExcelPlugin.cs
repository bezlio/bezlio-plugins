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

            try
            {
                // Convert the SheetData object to CSV
                DataTable dt = JsonConvert.DeserializeObject<DataTable>(request.SheetData);
                StringBuilder sb = new StringBuilder();

                // Define the format   
                var format = new ExcelTextFormat();
                format.Delimiter = '~';
                format.DataTypes = new eDataTypes[dt.Columns.Count - 1];
                format.EOL = "\r";              // DEFAULT IS "\r\n";
                                                // format.TextQualifier = '"';

                for (int i = 0; i < dt.Columns.Count - 1; i++)
                {
                    format.DataTypes[i] = eDataTypes.String;
                }

                // Column headers
                IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                    Select(column => column.ColumnName);

                if (request.FirstRowColumnNames == "Yes")
                {
                    sb.AppendLine(string.Join(",", columnNames));
                }


                // Rows
                foreach (DataRow row in dt.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                    sb.AppendLine(string.Join("~", fields));
                }

                // Now write this to an Excel file
                using (ExcelPackage package = new ExcelPackage(new FileInfo(request.FileName)))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(request.SheetName);
                    worksheet.Cells["A1"].LoadFromText(sb.ToString(), format, OfficeOpenXml.Table.TableStyles.None, request.FirstRowColumnNames == "Yes");
                    package.Save();
                }
            }
            catch (Exception ex)
            {
                response.Error = true;
                response.ErrorText = ex.Message;
            }


            return response;
        }

    }
}

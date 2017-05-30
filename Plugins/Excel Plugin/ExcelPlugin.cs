using Excel;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins
{
    public class ExcelDataModel
    {
        public string FileName { get; set; }
        public string SheetName { get; set; }
        public string FirstRowColumnNames { get; set; }

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

    }
}

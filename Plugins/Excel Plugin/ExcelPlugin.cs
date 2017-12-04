using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Data;
using System.IO;
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
        public string AllStrings { get; set; }

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
            model.AllStrings = "[Yes,No]";

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
            DataTable dt = new DataTable();

            // First use EPPlus to calculate all formulas so the user is getting fresh data
            using (ExcelPackage package = new ExcelPackage(new FileInfo(request.FileName)))
            {
                var wks = package.Workbook.Worksheets.Where(s => s.Name.Equals(request.SheetName));
                ExcelWorksheet worksheet;
                if (wks.Count() > 0)
                {
                    worksheet = wks.First();
                } else
                {
                     // Excel index starts at 1...
                        worksheet = package.Workbook.Worksheets[1];
                    }

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
                        }
                        else
                        {
                            dt.Columns.Add(columnName, typeof(String));
                        }
                    }

                    //start adding data the datatable here by looping all rows and columns
                    for (int i = worksheet.Dimension.Start.Row + Convert.ToInt32(request.FirstRowColumnNames == "Yes"); i <= worksheet.Dimension.End.Row; i++)
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
            }

            response.Data = JsonConvert.SerializeObject(dt);

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

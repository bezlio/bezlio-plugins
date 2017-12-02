// Decompiled with JetBrains decompiler
// Type: bezlio.rdb.plugins.HelperMethods.Quote.QuoteHelperMethods
// Assembly: Epicor10, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 89750A79-AA67-475D-BA1B-44E21E9977DA
// Assembly location: C:\Program Files (x86)\Bezlio Remote Data Broker\Plugins\Epicor10.dll

using bezlio.rdb;
using bezlio.rdb.plugins;
using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins.HelperMethods.Quote
{
    class Quote_NewQuoteByCustomerModel
    {
        public string Connection { get; set; }

        public string Company { get; set; }

        public string CustID { get; set; }
    }

    class Quote_SaveQuoteModel
    {
        public string Connection { get; set; }

        public string Company { get; set; }

        public int QuoteNum { get; set; }

        public string ds { get; set; }
    }

    public class QuoteHelperMethods
    {
        public static async Task<RemoteDataBrokerResponse> Quote_NewQuoteByCustomer(RemoteDataBrokerRequest rdbRequest)
        {
            Quote_NewQuoteByCustomerModel quoteByCustomerModel = JsonConvert.DeserializeObject<Quote_NewQuoteByCustomerModel>(rdbRequest.Data);
            RemoteDataBrokerResponse responseObject = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);
            object epicorConnection = Common.GetEpicorConnection(quoteByCustomerModel.Connection, quoteByCustomerModel.Company, ref responseObject);
            try
            {
                object bo = Common.GetBusinessObject(epicorConnection, "Quote", ref responseObject);
                var ds = JsonConvert.DeserializeObject("{}", bo.GetType().GetMethod("GetNewQuoteHed").GetParameters()[0].ParameterType, new JsonSerializerSettings());

                bo.GetType().GetMethod("GetNewQuoteHed").Invoke(bo, new object[1] { ds });
                ((DataSet)ds).Tables["QuoteHed"].Rows[0]["CustomerCustID"] = quoteByCustomerModel.CustID;

                bo.GetType().GetMethod("GetCustomerInfo").Invoke(bo, new object[1] { ds });
                bo.GetType().GetMethod("Update").Invoke(bo, new object[1] { ds });

                responseObject.Data = JsonConvert.SerializeObject(ds);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                {
                    RemoteDataBrokerResponse dataBrokerResponse = responseObject;
                    string str = dataBrokerResponse.ErrorText + ex.InnerException.ToString();
                    dataBrokerResponse.ErrorText = str;
                }
                responseObject.Error = true;
                RemoteDataBrokerResponse dataBrokerResponse1 = responseObject;
                string str1 = dataBrokerResponse1.ErrorText + ex.Message;
                dataBrokerResponse1.ErrorText = str1;
            }
            finally
            {
                Common.CloseEpicorConnection(epicorConnection, ref responseObject);
            }
            return responseObject;
        }

        public static async Task<RemoteDataBrokerResponse> Quote_ChangeCustomer(RemoteDataBrokerRequest rdbRequest)
        {
            Quote_SaveQuoteModel quoteSaveQuoteModel = JsonConvert.DeserializeObject<Quote_SaveQuoteModel>(rdbRequest.Data);
            RemoteDataBrokerResponse responseObject = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);
            object epicorConnection = Common.GetEpicorConnection(quoteSaveQuoteModel.Connection, quoteSaveQuoteModel.Company, ref responseObject);
            try
            {
                object bo = Common.GetBusinessObject(epicorConnection, "Quote", ref responseObject);
                Type update = bo.GetType().GetMethod("Update").GetParameters()[0].ParameterType;

                object ds = JsonConvert.DeserializeObject(quoteSaveQuoteModel.ds, update, new JsonSerializerSettings());
                bo.GetType().GetMethod("GetCustomerInfo").Invoke(bo, new object[1] { ds });

                Type updateExt = bo.GetType().GetMethod("UpdateExt").GetParameters()[0].ParameterType;
                object extDs = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(ds, updateExt, new JsonSerializerSettings()), updateExt, new JsonSerializerSettings());

                JsonConvert.DeserializeObject("{}", bo.GetType().GetMethod("UpdateExt").ReturnType, new JsonSerializerSettings());
                bool flag = false;
                bo.GetType().GetMethod("UpdateExt").Invoke(bo, new object[4] { extDs, true, false, flag });
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                {
                    RemoteDataBrokerResponse dataBrokerResponse = responseObject;
                    string str = dataBrokerResponse.ErrorText + ex.InnerException.ToString();
                    dataBrokerResponse.ErrorText = str;
                }
                responseObject.Error = true;
                responseObject.ErrorText = ex.Message;
            }
            finally
            {
                Common.CloseEpicorConnection(epicorConnection, ref responseObject);
            }
            return responseObject;
        }

        public static async Task<RemoteDataBrokerResponse> Quote_SaveQuote(RemoteDataBrokerRequest rdbRequest)
        {
            Quote_SaveQuoteModel quoteSaveQuoteModel = JsonConvert.DeserializeObject<Quote_SaveQuoteModel>(rdbRequest.Data);
            RemoteDataBrokerResponse responseObject = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);
            object epicorConnection = Common.GetEpicorConnection(quoteSaveQuoteModel.Connection, quoteSaveQuoteModel.Company, ref responseObject);
            try
            {
                object bo = Common.GetBusinessObject(epicorConnection, "Quote", ref responseObject);
                Type updateExt = bo.GetType().GetMethod("UpdateExt").GetParameters()[0].ParameterType;

                object ds = JsonConvert.DeserializeObject(quoteSaveQuoteModel.ds, updateExt, new JsonSerializerSettings());
                JsonConvert.DeserializeObject("{}", bo.GetType().GetMethod("UpdateExt").ReturnType, new JsonSerializerSettings());

                bool flag = false;
                bo.GetType().GetMethod("UpdateExt").Invoke(bo, new object[4] { ds, true, false, flag });
                responseObject.Data = JsonConvert.SerializeObject(ds);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                {
                    RemoteDataBrokerResponse dataBrokerResponse = responseObject;
                    string str = dataBrokerResponse.ErrorText + ex.InnerException.ToString();
                    dataBrokerResponse.ErrorText = str;
                }
                responseObject.Error = true;
                responseObject.ErrorText = ex.Message;
            }
            finally
            {
                Common.CloseEpicorConnection(epicorConnection, ref responseObject);
            }
            return responseObject;
        }

        public static async Task<RemoteDataBrokerResponse> Quote_DeleteQuote(RemoteDataBrokerRequest rdbRequest)
        {
            Quote_SaveQuoteModel quoteSaveQuoteModel = JsonConvert.DeserializeObject<Quote_SaveQuoteModel>(rdbRequest.Data);
            RemoteDataBrokerResponse responseObject = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);
            object epicorConnection = Common.GetEpicorConnection(quoteSaveQuoteModel.Connection, quoteSaveQuoteModel.Company, ref responseObject);
            try
            {
                object bo = Common.GetBusinessObject(epicorConnection, "Quote", ref responseObject);
                Type updateExt = bo.GetType().GetMethod("UpdateExt").GetParameters()[0].ParameterType;

                object ds = JsonConvert.DeserializeObject(quoteSaveQuoteModel.ds, updateExt, new JsonSerializerSettings());
                JsonConvert.DeserializeObject("{}", bo.GetType().GetMethod("UpdateExt").ReturnType, new JsonSerializerSettings());

                bool flag = false;
                bo.GetType().GetMethod("UpdateExt").Invoke(bo, new object[4] { ds, true, false, flag });
                responseObject.Data = JsonConvert.SerializeObject(ds);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                {
                    RemoteDataBrokerResponse dataBrokerResponse = responseObject;
                    string str = dataBrokerResponse.ErrorText + ex.InnerException.ToString();
                    dataBrokerResponse.ErrorText = str;
                }
                responseObject.Error = true;
                responseObject.ErrorText = ex.Message;
            }
            finally
            {
                Common.CloseEpicorConnection(epicorConnection, ref responseObject);
            }
            return responseObject;
        }
    }
}

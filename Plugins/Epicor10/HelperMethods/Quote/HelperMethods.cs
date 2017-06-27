using Newtonsoft.Json;
using System;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace bezlio.rdb.plugins.HelperMethods.Quote
{
    class Quote_NewQuoteByCustomerModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public string CustID { get; set; }

        public Quote_NewQuoteByCustomerModel() { }
    }

    class Quote_SaveQuoteModel
    {
        public string Connection { get; set; }
        public string Company { get; set; }
        public int QuoteNum { get; set; }
        public string ds { get; set; }

        public Quote_SaveQuoteModel() { }
    }

    public class QuoteHelperMethods
    {
        public static async Task<RemoteDataBrokerResponse> Quote_NewQuoteByCustomer(RemoteDataBrokerRequest rdbRequest)
        {
            Quote_NewQuoteByCustomerModel request = JsonConvert.DeserializeObject<Quote_NewQuoteByCustomerModel>(rdbRequest.Data);

            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            try
            {
                object bo = Common.GetBusinessObject(epicorConn, "Quote", ref response);

                Type t = bo.GetType().GetMethod("GetNewQuoteHed").GetParameters()[0].ParameterType;

                var ds = JsonConvert.DeserializeObject("{}", t, new JsonSerializerSettings());

                bo.GetType().GetMethod("GetNewQuoteHed").Invoke(bo, new object[] { ds });

                ((DataSet)ds).Tables["QuoteHed"].Rows[0]["CustomerCustID"] = request.CustID;

                bo.GetType().GetMethod("GetCustomerInfo").Invoke(bo, new object[] { ds });

                bo.GetType().GetMethod("Update").Invoke(bo, new object[] { ds });

                response.Data = JsonConvert.SerializeObject(ds);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText += ex.Message;
            }
            finally { Common.CloseEpicorConnection(epicorConn, ref response); }

            return response;
        }

        public static async Task<RemoteDataBrokerResponse> Quote_ChangeCustomer(RemoteDataBrokerRequest rdbRequest)
        {
            Quote_SaveQuoteModel request = JsonConvert.DeserializeObject<Quote_SaveQuoteModel>(rdbRequest.Data);

            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            try
            {
                object bo = Common.GetBusinessObject(epicorConn, "Quote", ref response);

                #region update header
                //get Update type and serialize/deserialize UpdateExt dataset
                Type t = bo.GetType().GetMethod("Update").GetParameters()[0].ParameterType;
                var newDs = JsonConvert.DeserializeObject(request.ds, t, new JsonSerializerSettings());

                //run GetCustomerInfo to update Customer if needed
                bo.GetType().GetMethod("GetCustomerInfo").Invoke(bo, new object[] { newDs });

                //convert QuoteDataSet into UpdExtQuoteDataSet so we can run UpdateExt
                Type updExt = bo.GetType().GetMethod("UpdateExt").GetParameters()[0].ParameterType;
                string extStr = JsonConvert.SerializeObject(newDs, updExt, new JsonSerializerSettings());
                var updDs = JsonConvert.DeserializeObject(extStr, updExt, new JsonSerializerSettings());

                //get BOUpdErr type and create empty dataset
                Type boUpd = bo.GetType().GetMethod("UpdateExt").ReturnType;
                var boDs = JsonConvert.DeserializeObject("{}", boUpd, new JsonSerializerSettings());

                //update QuoteHed
                bool more = false;
                boDs = bo.GetType().GetMethod("UpdateExt").Invoke(bo, new object[] { updDs, true, false, more });
                #endregion
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }
            finally { Common.CloseEpicorConnection(epicorConn, ref response); }

            return response;
        }

        public static async Task<RemoteDataBrokerResponse> Quote_SaveQuote(RemoteDataBrokerRequest rdbRequest)
        {
            Quote_SaveQuoteModel request = JsonConvert.DeserializeObject<Quote_SaveQuoteModel>(rdbRequest.Data);

            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            try
            {
                object bo = Common.GetBusinessObject(epicorConn, "Quote", ref response);

                #region update details
                //get UpdateExt type and deserialize incoming dataset
                Type updExt = bo.GetType().GetMethod("UpdateExt").GetParameters()[0].ParameterType;
                var updDs = JsonConvert.DeserializeObject(request.ds, updExt, new JsonSerializerSettings());

                //get BOUpdErr type and create empty dataset
                Type boUpd = bo.GetType().GetMethod("UpdateExt").ReturnType;
                var boDs = JsonConvert.DeserializeObject("{}", boUpd, new JsonSerializerSettings());

                //update QuoteDtl and QuoteQty
                bool more = false;
                boDs = bo.GetType().GetMethod("UpdateExt").Invoke(bo, new object[] { updDs, true, false, more });
                #endregion

                response.Data = JsonConvert.SerializeObject(updDs);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }
            finally { Common.CloseEpicorConnection(epicorConn, ref response); }

            return response;
        }

        public static async Task<RemoteDataBrokerResponse> Quote_DeleteQuote(RemoteDataBrokerRequest rdbRequest)
        {
            Quote_SaveQuoteModel request = JsonConvert.DeserializeObject<Quote_SaveQuoteModel>(rdbRequest.Data);

            RemoteDataBrokerResponse response = Common.GetResponseObject(rdbRequest.RequestId, rdbRequest.Compress);

            object epicorConn = Common.GetEpicorConnection(request.Connection, request.Company, ref response);

            try
            {
                object bo = Common.GetBusinessObject(epicorConn, "Quote", ref response);

                Type updExt = bo.GetType().GetMethod("UpdateExt").GetParameters()[0].ParameterType;
                var updDs = JsonConvert.DeserializeObject(request.ds, updExt, new JsonSerializerSettings());

                Type boUpd = bo.GetType().GetMethod("UpdateExt").ReturnType;
                var boDs = JsonConvert.DeserializeObject("{}", boUpd, new JsonSerializerSettings());

                bool more = false;
                boDs = bo.GetType().GetMethod("UpdateExt").Invoke(bo, new object[] { updDs, true, false, more });

                response.Data = JsonConvert.SerializeObject(updDs);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException.ToString()))
                    response.ErrorText += ex.InnerException.ToString();

                response.Error = true;
                response.ErrorText = ex.Message;
            }
            finally { Common.CloseEpicorConnection(epicorConn, ref response); }

            return response;
        }

        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        public static void SetPropertyValue(object obj, string propName, object value)
        {
            obj.GetType().GetProperty(propName).SetValue(obj, value, null);
        }
    }
}
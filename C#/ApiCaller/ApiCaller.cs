using System; 
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Insert.Your.Namespace.Here
{
    public class ApiCaller
    {
        private string _treatedEndpoint;
        public IDictionary<string, string> ParametersValue { get; }
        public string HostName { get; set; }
        public string EndpointName { get; set; }
        public string CompleteRequestPath { get { return _treatedEndpoint; } }
        public string TreatedParameters { get { return TreatParameters(); } }
        public ApiCallerHeader[] Header { get; set; }

        /// <summary>
        /// Construtor que indica o endpoint no qual deseja conectar-se e as propriedades da sua entidade
        /// </summary>
        /// <param name="Endpointname">Um endpoint válido</param>
        /// <param name="Hostname">Um endereço host válido</param>
        /// <param name="ParamValue">Dicionário apontando os nomes dos parametros e seus respectivos valores</param>
        public ApiCaller(string Hostname, string Endpointname, IDictionary<string, string> ParamValue = null)
        {
            ParametersValue = ParamValue;
            EndpointName = Endpointname;
            HostName = Hostname;
            _treatedEndpoint = TreatedParameters;
        }

        private string TreatParameters()
        {
            string result = string.Empty;
            result = HostName + EndpointName + '?';
            foreach( KeyValuePair<string, string> keyValuePair in ParametersValue)
            {
                result += keyValuePair.Key + "=" + keyValuePair.Value;
            }
            return result;
        }

        /// <summary>
        /// Cria uma HttpWebRequest do Tipo GET para a Url declarada na hora da instância
        /// </summary>
        /// <param name="headers">Lista de cabeçalhos a serem inclusos na requisição - Opcional</param>
        /// <param name="timeout">Tempo de timeout da requisição - Opcional</param>
        /// <returns>Retorna classe ApiHelperResponse com Status e Mensagem da Resposta</returns>
        public ApiCallerResponse Get(ApiCallerHeader[] headers = null, int? timeout = null)
        {
            return _Call(null, "GET", headers, timeout, null);
        }

        /// <summary>
        /// Cria uma HttpWebRequest do Tipo POST para a Url declarada na hora da instância
        /// </summary>
        /// <param name="body">Corpo da requisição (default Json)</param>
        /// <param name="headers">Lista de cabeçalhos a serem inclusos na requisição - Opcional</param>
        /// <param name="timeout">Tempo de timeout da requisição - Opcional</param>
        /// <returns>Retorna classe ApiCallerResponse com Status, Mensagem e um objeto serializado JSON</returns>
        public ApiCallerResponse Post(string body = null, ApiCallerHeader[] headers = null, int? timeout = null)
        {
            return _Call(body, "POST", headers, timeout, "application/json");
        }

        /// <summary>
        /// Cria uma HttpWebRequest com os parametros informados
        /// </summary>
        /// <param name="Body">Corpo da requisição - default Json</param>
        /// <param name="TipoRequest">Tipo de requisição - Opcional (default: GET)</param>
        /// <param name="headers">Lista de cabeçalhos a serem inclusos na requisição - Opcional</param>
        /// <param name="timeout">Tempo de timeout da requisição - Opcional</param>
        /// <param name="contentType">ContentType da requisição - Opcional (default: application/json)</param>
        /// <returns>Retorna classe ApiCallerResponse com Status, Mensagem e um objeto serializado JSON</returns>
        public ApiCallerResponse Call(string Body = "", string TipoRequest = "GET", ApiCallerHeader[] headers = null,
          int? timeout = null, string contentType = "application/json")
        {
            return _Call(Body, TipoRequest, headers, timeout, contentType);
        }

        /// <summary>
        /// Executa um WebRequest com os parametros informados.
        /// </summary>
        /// <param name="Body">Corpo da requisição - default Json</param>
        /// <param name="TipoRequest">Tipo de requisição - Opcional (default: GET)</param>
        /// <param name="headers">Lista de cabeçalhos a serem inclusos na requisição - Opcional</param>
        /// <param name="timeout">Tempo de timeout da requisição - Opcional</param>
        /// <param name="contentType">ContentType da requisição - Opcional (default: application/json)</param>
        /// <returns>Retorna classe ApiCallerResponse com Status, Mensagem e um objeto serializado JSON</returns>
        private ApiCallerResponse _Call(string Body = "", string TipoRequest = "GET", ApiCallerHeader[] headers = null,
              int? timeout = null, string contentType = "application/json")
        {
            HttpWebRequest httpWebRequest = WebRequest.Create(_treatedEndpoint) as HttpWebRequest;
            httpWebRequest.KeepAlive = true;
            httpWebRequest.Method = TipoRequest;
            httpWebRequest.ContentType = contentType;
            httpWebRequest.ContentLength = 0L;

            if (headers != null)
            {
                foreach (var item in headers)
                    httpWebRequest.Headers.Add(item.Key, item.Value);
            }

            if (timeout.HasValue)
                httpWebRequest.Timeout = timeout.Value;

            ApiCallerResponse apiHelperResponse = new ApiCallerResponse();
            try
            {
                if (httpWebRequest.Method != "GET" && Body != null)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(Body);
                    httpWebRequest.ContentLength = (long)bytes.Length;
                    using (Stream requestStream = httpWebRequest.GetRequestStream())
                        requestStream.Write(bytes, 0, bytes.Length);
                }
                using (HttpWebResponse response = httpWebRequest.GetResponse() as HttpWebResponse)
                {
                    if (response != null)
                    {
                        string message = string.Empty;
                        using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                            message = streamReader.ReadToEnd();

                        apiHelperResponse.ResponseText = message;
                        apiHelperResponse.StatusCode = response.StatusCode;
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (HttpWebResponse response = (HttpWebResponse)ex.Response)
                    {
                        using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                        {
                            apiHelperResponse.ResponseText = streamReader.ReadToEnd();
                            apiHelperResponse.StatusCode = HttpStatusCode.InternalServerError;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                apiHelperResponse.ResponseText = ex.Message;
                apiHelperResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return apiHelperResponse;
        }

        /// <summary>
        /// Executa um WebRequest com um path customizável
        /// </summary>
        /// <param name="path">Caminho do request (incluindo seus parametros)</param>
        /// <param name="TipoRequest">Tipo de requisição - Opcional (default: GET)</param>
        /// <param name="Body">Corpo da requisição - default Json</param>
        /// <param name="headers">Lista de cabeçalhos a serem inclusos na requisição - Opcional</param>
        /// <param name="timeout">Tempo de timeout da requisição - Opcional</param>
        /// <param name="contentType">ContentType da requisição - Opcional (default: application/json)</param>
        /// <returns>Retorna classe ApiCallerResponse com Status, Mensagem e um objeto serializado JSON</returns>
        public static ApiCallerResponse CallCustomPath(string path = "", string Body = "", string TipoRequest = "GET", ApiCallerHeader[] headers = null,
      int? timeout = null, string contentType = "application/json")
        {
            HttpWebRequest httpWebRequest = WebRequest.Create(path) as HttpWebRequest;
            httpWebRequest.KeepAlive = true;
            httpWebRequest.Method = TipoRequest;
            httpWebRequest.ContentType = contentType;
            httpWebRequest.ContentLength = 0L;

            if (headers != null)
            {
                foreach (var item in headers)
                    httpWebRequest.Headers.Add(item.Key, item.Value);
            }

            if (timeout.HasValue)
                httpWebRequest.Timeout = timeout.Value;

            ApiCallerResponse apiHelperResponse = new ApiCallerResponse();
            try
            {
                if (httpWebRequest.Method != "GET" && Body != null)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(Body);
                    httpWebRequest.ContentLength = (long)bytes.Length;
                    using (Stream requestStream = httpWebRequest.GetRequestStream())
                        requestStream.Write(bytes, 0, bytes.Length);
                }
                using (HttpWebResponse response = httpWebRequest.GetResponse() as HttpWebResponse)
                {
                    if (response != null)
                    {
                        string message = string.Empty;
                        using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                            message = streamReader.ReadToEnd();

                        apiHelperResponse.ResponseText = message;
                        apiHelperResponse.StatusCode = response.StatusCode;
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (HttpWebResponse response = (HttpWebResponse)ex.Response)
                    {
                        using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                        {
                            apiHelperResponse.ResponseText = streamReader.ReadToEnd();
                            apiHelperResponse.StatusCode = HttpStatusCode.InternalServerError;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                apiHelperResponse.ResponseText = ex.Message;
                apiHelperResponse.StatusCode = HttpStatusCode.InternalServerError;
            }

            return apiHelperResponse;
        }
    }
}

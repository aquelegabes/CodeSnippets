using Newtonsoft.Json;
using System.Net;

namespace Insert.Your.Namespace.Here
{
    public class ApiCallerResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string ResponseText { get; set; }
        /// <summary>
        /// Retorna um objeto serializado escolhido, caso falhe retornará o próprio objeto, mas seu valor será nulo.
        /// </summary>
        /// <typeparam name="T">Entity</typeparam>
        /// <returns>Entity</returns>
        public dynamic DeserializedObject<T>()
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(ResponseText);
            }
            catch
            {
                return default(T);
            }
        }

    }
}

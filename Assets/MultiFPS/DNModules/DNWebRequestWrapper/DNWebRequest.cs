using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DNWebRequest
{
    public enum DNRequestType
    {
        GET,
        POST,
    }
    public class DNWebRequests
    {
        public static UnityWebRequest CreateWebRequest(string endpoint, DNRequestType type, object data = null, int timeout = 5)
        {
            byte[] jsonToSend = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
            UnityWebRequest request = new(endpoint, type.ToString());
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = timeout;

            return request;
        }
    }
}
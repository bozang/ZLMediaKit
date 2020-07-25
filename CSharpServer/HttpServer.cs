using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Newtonsoft.Json;

namespace CSharpServer
{
    public class HttpServer:NancyModule
    {
        public HttpServer()
        {
            Get("/startplay", param =>
            {
                var code = Request.Query["code"];
                string ret = DoMedia.StartRealPlay(code);
                return ret;
            }
            );
            Get("/stopplay", param =>
            {
                var code = Request.Query["code"];
                string ret = DoMedia.StopRealPlay(code);
                return ret;
            }
            );
            //app:fh,stream:live1
            //mk_proxy_player_create("__defaultVhost__", "fh", "live1", 1, 0);
            //Get("/startplayrtsp", param =>
            //{
            //    var app = Request.Query["app"];
            //    var stream = Request.Query["stream"];
            //    var url = Request.Query["url"];
            //    //url = DoMedia.Escape(url);
            //    return DoMedia.StartRealPlayRtsp(app, stream, url);
            //}
            //);


            Post("/startplayrtsp", param =>
            {
                //var app = Request.Query["app"];
                //var stream = Request.Query["stream"];
                //var url = Request.Query["url"];
                PlayRtspModel playRtspModel = new PlayRtspModel();
                try
                {
                    byte[] data = new byte[Request.Body.Length];
                    int ret = Request.Body.Read(data, 0, data.Length);
                    string postData = Encoding.Default.GetString(data);
                    playRtspModel = JsonConvert.DeserializeObject<PlayRtspModel>(postData);

                }
                catch (Exception ex)
                {
                    return "Ê§°Ü:" + ex.Message;
                }
                return DoMedia.StartRealPlayRtsp(playRtspModel.App, playRtspModel.Stream, playRtspModel.Url);
            }
           );

            Get("/stopplayrtsp", param =>
            {
                var app = Request.Query["app"];
                var stream = Request.Query["stream"];
                return DoMedia.StopRealPlayRtsp(app, stream);
            }
            );
            Get("/startplaysdk", param =>
            {
                var app = Request.Query["app"];
                var stream = Request.Query["stream"];
                var device_ip = Request.Query["device_ip"];
                var device_username = Request.Query["device_username"];
                var device_password = Request.Query["device_password"];
                var device_port = Request.Query["device_port"];
                return DoMedia.StartPlaySDK(app, stream, "", device_ip, device_username, device_password, device_port);
            }
            );
            Get("/stopplaysdk", param =>
            {
                var app = Request.Query["app"];
                var stream = Request.Query["stream"];
                return DoMedia.StopPlaySDK(app, stream);
            }
            );
        }
    }
}

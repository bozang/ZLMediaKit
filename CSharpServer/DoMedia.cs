using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace CSharpServer
{
    public enum MediaSrcType
    {
        //国标源
        gb=0,
        //rtsp/rtmp源
        net_url=1,
        //SDKY=源
        sdk=2
    }
    public struct MediaCache
    {
        /// <summary>
        /// 流媒体句柄
        /// </summary>
        public IntPtr ctx;
        public MediaSrcType mediaSrcType;
        public int playHandle;
    }
    public class DoMedia
    {
        public static string Stop(string app,string stream)
        {
            string key = GetKey(app, stream);
            if (!dic_playProxy_media.ContainsKey(key))
            {
                return key + ":未找到";
            }
            switch (dic_playProxy_media[key].mediaSrcType)
            {
                case MediaSrcType.gb:
                    return StopRealPlay(stream);
                case MediaSrcType.net_url:
                    return StopRealPlayRtsp(app, stream);
                case MediaSrcType.sdk:
                    return StopPlaySDK(app, stream);
                default:
                    return "未知错误";
            }
        }
        public static string GetKey(string app,string stream)
        {
            return app + "_" + stream;
        }
        public static string GetKeyGb(string stream)
        {
            return "rtp" + "_" + stream;
        }
        static Dictionary<string, MediaCache> dic_playProxy_media = new Dictionary<string, MediaCache>();
        public static string StartRealPlayRtsp(string app,string stream,string url)
        {
            IntPtr playProxy = MediaServer.mk_proxy_player_create("__defaultVhost__", app, stream, 1, 0);
            MediaServer.mk_proxy_player_play(playProxy, url);
            string key = GetKey(app, stream);
            if (dic_playProxy_media.ContainsKey(key))
            {
                return key + ":已经在拉流";
            }
            dic_playProxy_media.Add(key,new MediaCache() { ctx= playProxy ,mediaSrcType = MediaSrcType.net_url});
            return key + "：开始拉流";
        }
        public static string StopRealPlayRtsp(string app, string stream)
        {
            string key = GetKey(app, stream);
            if (!dic_playProxy_media.ContainsKey(key))
            {
                return key + ":未找到";
            }
            IntPtr playProxy = dic_playProxy_media[key].ctx;
            MediaServer.mk_proxy_player_release(playProxy);
            dic_playProxy_media.Remove(key);


            if (RedisHelper.GetDatabase().HashExists("live", key))
            {
                bool ret1 = RedisHelper.GetDatabase().HashDelete("live", key);
                Console.WriteLine("删除" + key + "：" + ret1);
            }
            else
            {
                Console.WriteLine($"redis中未找到hash(live),HashFiled:{key}");
            }
            return key + "：停止拉流";
        }

        public static string StartRealPlay(string channelCode)
        {
            try
            {
                if (dic_playProxy_media.ContainsKey(GetKeyGb(channelCode)))
                {
                    return channelCode + ":已经在拉流";
                }
                dic_playProxy_media.Add(GetKeyGb(channelCode), new MediaCache() { ctx = IntPtr.Zero, mediaSrcType = MediaSrcType.gb });

                int[] port = SetMediaPort();
                ushort ret = MediaServer.mk_rtp_server_start((ushort)port[0], channelCode);
                return ret.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return 0.ToString();
            }
        }
        public static string StopRealPlay(string channelCode)
        {
            try
            {
                if (!dic_playProxy_media.ContainsKey(GetKeyGb(channelCode)))
                {
                    return channelCode + ":未找到";
                }
                dic_playProxy_media.Remove(GetKeyGb(channelCode));
                bool ret = MediaServer.mk_rtp_server_stop(channelCode);
                Console.WriteLine("停止收流接口："+ret);
                if (ret)
                {
                    if (RedisHelper.GetDatabase().HashExists("live",channelCode))
                    {
                        bool ret1 = RedisHelper.GetDatabase().HashDelete("live", channelCode);
                        Console.WriteLine("删除" + channelCode + "：" + ret1);
                    }
                    else
                    {
                        Console.WriteLine($"redis中未找到hash(live),HashFiled:{channelCode}");
                    }
                }
                return ret.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return false.ToString();
            }
        }

        //static bool input_data_flag = false;
        //static Dictionary<string, Queue<byte[]>> dic_data = new Dictionary<string, Queue<byte[]>>();
        //static List<Api_HIK> api_HIKs = new List<Api_HIK>();
        //static Queue<byte[]> queue = new Queue<byte[]>();
        public static string StartPlaySDK(string app, string stream,string device_type,string device_ip,string device_username,string device_password,string device_port)
        {
            string msg = $"app:{app},stream:{stream},,device_ip:{device_ip},device_username:{device_username},device_password:{device_password},device_port:{device_port}     ";
            IntPtr ctx = MediaServer.mk_media_create("__defaultVhost__", app, stream, 0, 1, 1, 1, 0);
            MediaServer.mk_media_init_video(ctx, 0, 1280, 720, 25);
            MediaServer.mk_media_init_complete(ctx);
            string key = GetKey(app, stream);
            if (dic_playProxy_media.ContainsKey(key))
            {
                return msg +"已经在推流：" + key;
            }
            
            Api_HIK api_HIK = new Api_HIK();
            api_HIK.SetDataCallBack(
                (byte[] psData, string callback_key) =>
            {
                if (dic_playProxy_media.ContainsKey(callback_key))
                {
                    MediaServer.mk_media_input_ps(dic_playProxy_media[callback_key].ctx, psData.ToArray(), psData.Length);
                }
            }
            );

            //Api_HIK.PSDATACALLBACK pSDATACALLBACK = test;
            //Api_HIK.Instance.SetDataCallBack(pSDATACALLBACK);
            //Task task = Task.Factory.StartNew(PsFrameInput);
            int playHandle = api_HIK.Play(device_ip, device_username, device_password, device_port, key);
            if (playHandle < 0)
            {
                return msg +"失败";
            }
            dic_playProxy_media.Add(key, new MediaCache() { ctx = ctx, mediaSrcType = MediaSrcType.sdk,playHandle=playHandle});
            return msg +"成功";
        }
        public static string StopPlaySDK(string app,string stream)
        {
            string key = GetKey(app, stream);
            if (!dic_playProxy_media.ContainsKey(key))
            {
                return key + ":未找到";
            }
            bool ret = CHCNetSDK.NET_DVR_StopRealPlay(dic_playProxy_media[key].playHandle);
            IntPtr playProxy = dic_playProxy_media[key].ctx;
            MediaServer.mk_media_release(playProxy);
            dic_playProxy_media.Remove(key);


            if (RedisHelper.GetDatabase().HashExists("live", key))
            {
                bool ret1 = RedisHelper.GetDatabase().HashDelete("live", key);
                Console.WriteLine("删除" + key + "：" + ret1);
            }
            else
            {
                Console.WriteLine($"redis中未找到hash(live),HashFiled:{key}");
            }
            return key + "：停止拉流";
        }
        static void test(byte[] psData, string callback_key)
        {
            
        }
        //static void PsFrameInput()
        //{
        //    while (!input_data_flag)
        //    {
        //        if (queue.Count > 0)
        //        {
        //            byte[] psData = queue.Dequeue();
        //            MediaServer.mk_media_input_ps(ctx, psData.ToArray(), psData.Count);
        //        }
        //    }
        //}
        public static int MEDIA_PORT_START_STATIC = 15000;
        public static int MEDIA_PORT_START = 15000;
        public static int MEDIA_PORT_END = 17000;
        public static int[] SetMediaPort()
        {
            //int[] port = new int[] {10000,10001 };
            //return port;
            var inUseUDPPorts = (from p in IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners() where p.Port >= MEDIA_PORT_START select p.Port).OrderBy(x => x).ToList();

            int rtpPort = 0;
            int rtcpPort = 0;

            if (inUseUDPPorts.Count > 0)
            {
                // Find the first two available for the RTP socket.
                for (int index = MEDIA_PORT_START; index <= MEDIA_PORT_END; index++)
                {
                    if (!inUseUDPPorts.Contains(index))
                    {
                        rtpPort = index;
                        break;
                    }
                }

                // Find the next available for the control socket.
                for (int index = rtpPort + 1; index <= MEDIA_PORT_END; index++)
                {
                    if (!inUseUDPPorts.Contains(index))
                    {
                        rtcpPort = index;
                        break;
                    }
                }
            }
            else
            {
                rtpPort = MEDIA_PORT_START;
                rtcpPort = MEDIA_PORT_START + 1;
            }

            if (MEDIA_PORT_START >= MEDIA_PORT_END)
            {
                MEDIA_PORT_START = MEDIA_PORT_START_STATIC;
            }
            MEDIA_PORT_START += 2;
            int[] mediaPort = new int[2];
            mediaPort[0] = rtpPort;
            mediaPort[1] = rtcpPort;
            return mediaPort;
        }

        //public static string Escape(string url)
        //{
        //    url = url.Replace("%", "%25");
        //    url = url.Replace("?", "%3f");
        //    url = url.Replace("#", "%23");
        //    url = url.Replace("&", "%26");
        //    url = url.Replace("@", "%40");
        //    return url;
        //}
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpServer
{
    public struct DataStruct
    {
        public string key;
        public byte[] data;
    }
    public class Api_HIK
    {
        private static Api_HIK _instance;
        public static Api_HIK Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Api_HIK();
                }
                return _instance;
            }
        }
        //PSDATACALLBACK ps_data_callback = null;
        //private string key;
        public Api_HIK()
        {
            bool m_bInitSDK = CHCNetSDK.NET_DVR_Init();
        }
        public delegate void PSDATACALLBACK(byte[] pBuffer,string key);
        public event PSDATACALLBACK psDtaEvent;
        //public void Init()
        //{
        //    bool m_bInitSDK = CHCNetSDK.NET_DVR_Init();
        //}
        public void SetDataCallBack(PSDATACALLBACK callbcak)
        {
            psDtaEvent += callbcak;
            //ps_data_callback = callbcak;
        }
        static CHCNetSDK.REALDATACALLBACK RealData = null;
        static Task task = null;
        public string Play(string device_ip, string device_username, string device_password, string device_port,string key)
        {
            try
            {
                //if (ps_data_callback == null)
                //{
                //    return "未设置数据回调的实体";
                //}
                CHCNetSDK.NET_DVR_SetLogToFile(0, "", true);
                CHCNetSDK.NET_DVR_USER_LOGIN_INFO struLogInfo = new CHCNetSDK.NET_DVR_USER_LOGIN_INFO();
                //设备IP地址或者域名
                byte[] byIP = System.Text.Encoding.Default.GetBytes(device_ip);
                struLogInfo.sDeviceAddress = new byte[129];
                byIP.CopyTo(struLogInfo.sDeviceAddress, 0);

                //设备用户名
                byte[] byUserName = System.Text.Encoding.Default.GetBytes(device_username);
                struLogInfo.sUserName = new byte[64];
                byUserName.CopyTo(struLogInfo.sUserName, 0);

                //设备密码
                byte[] byPassword = System.Text.Encoding.Default.GetBytes(device_password);
                struLogInfo.sPassword = new byte[64];
                byPassword.CopyTo(struLogInfo.sPassword, 0);

                struLogInfo.wPort = ushort.Parse(device_port);//设备服务端口号
                CHCNetSDK.NET_DVR_DEVICEINFO_V40 DeviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V40();
                int m_lUserID = CHCNetSDK.NET_DVR_Login_V40(ref struLogInfo, ref DeviceInfo);

                if (m_lUserID == -1)
                {
                    uint err = CHCNetSDK.NET_DVR_GetLastError();
                }

                if (RealData == null)
                {
                    RealData = new CHCNetSDK.REALDATACALLBACK(RealDataCallBack);//预览实时流回调函数

                }
                IntPtr pUser = Marshal.StringToBSTR(key);
                if (task == null)
                {
                    task = Task.Factory.StartNew(HIKDataInput);
                }
                //Task task = Task.Factory.StartNew(HIKDataInput);

                CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO();
                lpPreviewInfo.lChannel = 1;//预te览的设备通道
                lpPreviewInfo.dwStreamType = 0;//码流类型：0-主码流，1-子码流，2-码流3，3-码流4，以此类推
                lpPreviewInfo.dwLinkMode = 0;//连接方式：0- TCP方式，1- UDP方式，2- 多播方式，3- RTP方式，4-RTP/RTSP，5-RSTP/HTTP 
                lpPreviewInfo.bBlocked = true; //0- 非阻塞取流，1- 阻塞取流
                lpPreviewInfo.dwDisplayBufNum = 1; //播放库播放缓冲区最大缓冲帧数
                lpPreviewInfo.byProtoType = 0;
                lpPreviewInfo.byPreviewMode = 0;
                
                int m_lRealHandle = CHCNetSDK.NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, /*null*/ RealData, pUser);
                if (m_lRealHandle < 0)//播放失败
                {
                    input_data_flag = true;
                    //eSCallBack = new CHCNetSDK.PlayESCallBack(ESCallBack);
                    //bool ret = CHCNetSDK.NET_DVR_SetESRealPlayCallBack(m_lRealHandle, eSCallBack, IntPtr.Zero);

                }
                return default(string);
            }
            catch (FormatException ex)
            {
                return "端口不合法";
            }
        }
        public void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            if (dwBufSize > 0 && dwDataType != 1)
            {
                byte[] sData = new byte[dwBufSize];
                Marshal.Copy(pBuffer, sData, 0, (Int32)dwBufSize);
                string key = Marshal.PtrToStringBSTR(pUser);
                if (key == null|| sData==null)
                {
                    int i = 0;
                }
                if (!dic_cache.ContainsKey(key))
                {
                    dic_cache.Add(key, new List<byte>());
                }
                queue.Enqueue(new DataStruct() { key=key,data=sData});
                //string str = "实时流数据.ps";
                //FileStream fs = new FileStream(str, FileMode.Append);
                //int iLen = (int)dwBufSize;
                //fs.Write(sData, 0, iLen);
                //fs.Close();
            }
        }
        bool input_data_flag = false;
        //Dictionary<string, Queue<byte[]>> dic_data = new Dictionary<string, Queue<byte[]>>();
        Queue<DataStruct> queue = new Queue<DataStruct>();
        //List<byte> cache = new List<byte>();
        Dictionary<string, List<byte>> dic_cache = new Dictionary<string, List<byte>>();
        static int num = 0;
        void HIKDataInput()
        {
            num++;
            Console.WriteLine("HIKDataInput：当前线程ID：" + System.Threading.Thread.CurrentThread.ManagedThreadId);
            while (!input_data_flag)
            {
                try
                {
                    if (queue.Count > 0)
                    {
                        
                        DataStruct dataStruct = queue.Dequeue();
                        string key = dataStruct.key;
                        byte[] psData = dataStruct.data;
                        if (psData == null||psData.Length < 4)
                        {

                        }
                        if (psData[0] == 0 && psData[1] == 0 && psData[2] == 1 && psData[3] == 186)
                        {
                            if (dic_cache[key].Count != 0)
                            {
                                psDtaEvent(dic_cache[key].ToArray(), key);
                                //MediaServer.mk_media_input_ps(ctx, cache.ToArray(), cache.Count);
                                dic_cache[key].Clear();
                            }
                            dic_cache[key].AddRange(psData);
                        }
                        else if (psData[0] == 0 && psData[1] == 0 && psData[2] == 1 && psData[3] == 223)
                        {
                            if (dic_cache[key].Count != 0)
                                dic_cache[key].AddRange(psData);
                        }
                        else if (psData[0] == 0 && psData[1] == 0 && psData[2] == 1 && psData[3] == 224)
                        {
                            if (dic_cache[key].Count != 0)
                                dic_cache[key].AddRange(psData);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
    }
}

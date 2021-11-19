#region Namespace
using Google.Protobuf;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using IGG;
using IGG.Framework.Cache;
using IGG.Framework.Utils;
using IGG.Game.Data.Cache;
using UnityEngine;
using UnityEngine.Networking;
using zlib;
using IGG.Framework;

#endregion

/// <summary>
/// 本地数据存储 特别注意：无法序列化dic字典
/// </summary>
public class DataStorageHelper
{
    public const string Default_Url_Path = "https://statics.riabox.com/storage/123";

    //默认战报日志路径
    private static readonly string DefaultBattleLogPath =
        Application.persistentDataPath + "/" + AppCache.Player.PlayerId.ToString() + "/BattleLogDetail/";
    public static string GetBattleMailLogDetailSavePath(string strFileName)
    {
        return StrUtil.Concat(new[] { DefaultBattleLogPath, strFileName, ".gz" });
    }

    //邮件存储路径
    public static string GetChatMailFilePath(string strFileName)
    {
        return StrUtil.Concat(Application.persistentDataPath, "/common/", strFileName, ".gz");
    }

    private static JsonSerializerSettings settings = new JsonSerializerSettings();

    private const string VersionKey = "_version";

    // 所有的本地存储数据KEY都提供在这里
    public const string DsChatStorage = "DsChatStorage";
    public const string DsChatCountStorage = "DsChatCountStorage";
    public const string DsChatChatPrivate = "DsChatChatPrivate";
    public const string DsChatClientVersionCode = "DsChatClientVersionCode";

    public const string DsFriendGroupSortIndex = "DsFriendGroupSortIndex";  // 好友自定义分组的排序顺序 
        
    public const string DsIMChatShowGroup = "DsIMChatShowGroup";            // 窗口左侧展示缓存
    public const string DsIMChatSelectSize = "DsIMChatSelectSize";          // 窗口大小缓存

    public const string DsLabelLocalData = "DsLabelLocalData";
    public const string DsLabelGuildReadId = "DsLabelGuildReadId";

    public const string DsExpeDeployArmyData = "DsExpeDeployArmyData";
    public const string DsExpePlotDeployArmyData = "DsExpePlotDeployArmyData";
    public const string DsExpeMistyDeployArmyData = "DsExpeMistyDeployArmyData";
    public const string DsExpeEpisodePlayData = "DsExpeEpisodePlayData";

    public const string DsTaskBarShow = "DsTaskBarShow";

    public const string DsLimitActivitySkipAnima = "DsLimitActivitySkipAnima";
    public const string DsMail = "DsMail";

    public const string DsBuildQueue = "DsBuildQueue";
    public const string DsActivityOpenMaxPanel = "DsActivityOpenMaxPanel";
    public const string DsActivityBtnPlayAni = "DsActivityBtnPlayAni";
    public const string DsVipGiftDealRed = "DsVipGiftDealRed";

    public const string DsDeleteBattleLogTimeInterval = "DsDeleteBattleLogTimeInterval";

    public const string DsResNotification = "DsResNotification";
    public const string DsRookieBuffRed = "DsRookieBuffRed";
    public static T LoadDataFromFile<T>(string key)
    {
        string data = IGG.FileUtils.ReadTextFromFile(Application.persistentDataPath + "/" + key + ".bc2");
        if (string.IsNullOrEmpty(data))
        {
            return default(T);
        }

        T ret = JsonConvert.DeserializeObject<T>(data);
        return ret;
    }

    public static void SaveDataToFile<T>(string key, T src)
    {
        string txt = JsonConvert.SerializeObject(src);
        if (!IGG.FileUtils.SaveTextToFile(txt, Application.persistentDataPath + "/" + key + ".bc2"))
        {

        }
    }

    public static T LoadData<T>(string key)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            return default(T);
        }

        try
        {
            T ret;
            if (typeof(T) == typeof(int) || typeof(T) == typeof(uint))
            {
                ret = (T) Convert.ChangeType(PlayerPrefs.GetInt(key), typeof(T));
            }
            else if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                ret = (T) Convert.ChangeType(PlayerPrefs.GetFloat(key), typeof(T));
            }
            else if (typeof(T) == typeof(string))
            {
                ret = (T) Convert.ChangeType(PlayerPrefs.GetString(key), typeof(T));
            }
            else
            {
                settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                string txt = PlayerPrefs.GetString(key);
                ret = JsonConvert.DeserializeObject<T>(txt, settings);
            }

            return ret;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("读取配置错误, key = " + key + ", " + e);
        }

        return default(T);
    }

    /// <summary>
    /// 反序列化对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static T DeserializeObject<T>(string source)
    {
        settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        return JsonConvert.DeserializeObject<T>(source, settings);
    }

    public static bool SaveData<T>(string key, T source, bool immedSave = true, bool versionSave = true)
    {
        Type sourceType = typeof(T);
        try
        {
            string valueStr;
            if (sourceType == typeof(int) || sourceType == typeof(uint))
            {
                int value = (int) Convert.ChangeType(source, typeof(int));
/*                valueStr = value.ToString();*/
                PlayerPrefs.SetInt(key, value);
            }
            else if (sourceType == typeof(float) || sourceType == typeof(double))
            {
                float value = (float) Convert.ChangeType(source, typeof(float));
/*                valueStr = value.ToString(CultureInfo.InvariantCulture);*/
                PlayerPrefs.SetFloat(key, value);
            }
            else if (sourceType == typeof(string))
            {
                valueStr = (string) Convert.ChangeType(source, typeof(string));
                PlayerPrefs.SetString(key, valueStr);
            }
            else
            {
                settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                valueStr = JsonConvert.SerializeObject(source, settings);
                PlayerPrefs.SetString(key, valueStr);
            }

            if (versionSave)
            {
                PlayerPrefs.SetString(string.Concat(key, VersionKey), FrameworkConfig.Inst.MainVersion);
            }

            if (immedSave)
            {
                PlayerPrefs.Save();
            }
            //Logger.Log(string.Format("SaveData, key={0}, immedSave={1}, type={2}, value={3}",
            //                         key, immedSave, sourceType.ToString(), valueStr),
            //           "DataStorageHelper.SaveData");
            return true;
        }
        catch 
        {

        }

        return false;
    }

    /// <summary>
    /// 序列化对象
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static string SerializeObject(object source)
    {
        settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        return JsonConvert.SerializeObject(source, settings);
    }

    /// <summary>
    /// 判断是否有这个key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    /// <summary>
    /// 判断是否有这个key 附加玩家ID
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool HasKeyByAccount(string key)
    {
        return PlayerPrefs.HasKey(AppCache.Player.PlayerId + key);
    }

    /// <summary>
    /// 清楚数据 附加玩家ID
    /// </summary>
    /// <param name="key"></param>
    public static void ClearDataByAccount(string key)
    {
        PlayerPrefs.DeleteKey(AppCache.Player.PlayerId + key);
    }

    /// <summary>
    /// 根据账号获取
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T LoadDataByAccount<T>(string key)
    {
        var playerId = AppCache.Player.PlayerId;
        var newKey = playerId + key;
        return LoadData<T>(newKey);
    }

    /// <summary>
    /// 根据账号和王国获取
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T LoadDataByAccountAndKingdom<T>(string key)
    {
        var playerId = AppCache.Player.PlayerId + AppCache.Player.KingdomId;
        var newKey = playerId + key;
        return LoadData<T>(newKey);
    }

    /// <summary>
    /// 根据账号存储
    /// </summary>
    /// <param name="key"></param>
    /// <param name="source"></param>
    /// <param name="immedSave"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool SaveDataByAccount<T>(string key, T source, bool immedSave = true)
    {
        var playerId = AppCache.Player.PlayerId;
        var newKey = playerId + key;
        return SaveData(newKey, source, immedSave);
    }

    public static bool SaveDataByAccountAndKindom<T>(string key, T source, bool immedSave = true)
    {
        var playerId = AppCache.Player.PlayerId + AppCache.Player.KingdomId;
        var newKey = playerId + key;
        return SaveData(newKey, source, immedSave);
    }

    public static void ClearData(string key)
    {
        //Logger.Log("ClearData: " + key, "DataStorageHelper.ClearData");
        PlayerPrefs.DeleteKey(key);
    }


    public static void ClearAllData()
    {
        ClearData(DsChatStorage);
        ClearData(DsChatCountStorage);
        ClearData(DsChatChatPrivate);
        ClearData(DsLabelGuildReadId);
        ClearData(DsLabelLocalData);
        ClearData(DsExpeDeployArmyData);
        ClearData(DsExpePlotDeployArmyData);
        ClearData(DsExpeMistyDeployArmyData);
        ClearData(DsExpeEpisodePlayData);
        ClearData(DsTaskBarShow);
        ClearData(DsMail);
    }

    /// <summary>
    /// 保存proto消息到本地
    /// </summary>
    /// <typeparam name="T">消息</typeparam>
    /// <param name="path">保存的文件路径</param>
    /// <param name="obj">消息实例对象</param>
    /// <param name="completeCallBack">完成回调</param>
    public static async void SaveProtoDataAsync<T>(string path, T obj, Action completeCallBack = null) where T : IMessage
    {
        if (null == obj || string.IsNullOrEmpty(path))
        {
            completeCallBack?.Invoke();
            return;
        }

        FileUtils.CreateDirectoryFromFile(path);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        using (var sourceStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            var bytes = obj.ToByteArray();
            sourceStream.Seek(0, SeekOrigin.End);

            // 压缩byte数据
            using (var compressStream = new GZipStream(sourceStream, CompressionMode.Compress, true))
            {
                await compressStream.WriteAsync(bytes, 0, bytes.Length);
                completeCallBack?.Invoke();
            }
        }
    }

    /// <summary>
    /// 从web下载文件 根据url
    /// </summary>
    /// <param name="url"></param>
    /// <param name="callBackComplete">返回下载的byte数据</param>
    /// <returns></returns>
    public static IEnumerator DownloadFile(string url, Action<byte[]> callBackComplete)
    {
        byte[] resultData = null;
        if (string.IsNullOrEmpty(url))
        {
            callBackComplete?.Invoke(resultData);
            Debug.LogError("web下载文件的Url为空，调用堆栈：" + GetStackTraceModel());
            yield break;
        }
        using (var webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();
           
            if (webRequest.isNetworkError)
            {
                Debug.LogError(webRequest.error);
                callBackComplete?.Invoke(resultData);
            }
            else
            {
                var fileHandler = webRequest.downloadHandler;
                resultData = fileHandler.data;
                callBackComplete?.Invoke(resultData);
            }
        }
    }

    /// <summary>
    /// 是否低版本
    /// </summary>
    /// <param name="strKey">本地key</param>
    /// <param name="strCompareVersion">与之前保存的版本号对比的版本号</param>
    /// <returns></returns>
    public static bool IsLowVersionByKey(string strKey, string strCompareVersion)
    {
        string strLocalVersion =  PlayerPrefs.GetString(string.Concat(strKey, VersionKey), "");
        return IsLowVersion(strLocalVersion, strCompareVersion);
    }

    /// <summary>
    /// 版本号对比
    /// </summary>
    /// <param name="strLocalVersion">本地记录的版本号</param>
    /// <param name="strCompareVersion">程序写死要对比的版本号</param>
    /// <returns></returns>
    public static bool IsLowVersion(string strLocalVersion, string strCompareVersion)
    {
        if (string.IsNullOrEmpty(strLocalVersion))
        {
            return true;
        }
        string[] arrCompare = strCompareVersion.Split('.');
        string[] arrLocal = strLocalVersion.Split('.');
        if (arrCompare.Length != arrLocal.Length || arrCompare.Length != 3)
        {
            return false;
        }

        long lCompare = 0;
        long lLocal = 0;
        for (int i = 0; i < arrCompare.Length; i++)
        {
            long lTemp = 0;
            if (!long.TryParse(arrCompare[i], out lTemp))
            {
                return false;
            }

            lCompare += (long)Math.Pow(10000, arrCompare.Length - i) * lTemp;

            if (!long.TryParse(arrLocal[i], out lTemp))
            {
                return false;
            }

            lLocal += (long)Math.Pow(10000, arrLocal.Length - i) * lTemp;
        }

        return lLocal < lCompare;
    }

    /// <summary>
    /// 打印堆栈信息(打印错误信息的时候可以调用)
    /// </summary>
    /// <returns></returns>
    public static string GetStackTraceModel()
    {
        var st = new System.Diagnostics.StackTrace();
        var sfs = st.GetFrames();
        var sb = SbPool.Get();
        var methodName = string.Empty;
        for (var i = 1; i < sfs.Length; i++)
        {
            if (System.Diagnostics.StackFrame.OFFSET_UNKNOWN == sfs[i].GetILOffset())
                break;
            methodName = sfs[i].GetMethod().Name;
            sb.Insert(0, methodName + "()->");
        }
        st = null;
        sfs = null;
        
        return SbPool.PutAndToStr(sb).TrimEnd('-', '>');
    }

    /// <summary>
    /// 加载已保存的本地proto消息
    /// </summary>
    /// <typeparam name="T">消息</typeparam>
    /// <param name="path">加载的文件夹路径</param>
    /// <param name="fileName">文件名</param>
    /// <param name="callBack">加载完成之后回调函数</param>
    public static async void LoadProtoDataAsync<T>(string path, Action<T> callBack) where T : IMessage
    {
        if (!File.Exists(path))
        {
            callBack.Invoke(default(T));
            return;
        }

        using (var fileStream = File.Open(path, FileMode.Open))
        {
            var result = new byte[fileStream.Length];
            await fileStream.ReadAsync(result, 0, (int)fileStream.Length);
            DeCompress(result, bytes =>
            {
                try
                {
                    var message = Activator.CreateInstance(typeof(T));
                    ((IMessage)message).MergeFrom(bytes, 0, bytes.Length);
                    callBack?.Invoke((T)message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });
        }
    }

    /// <summary>
    /// 解压byte数据GZip形式（主要用于客户端本地压缩保存和上传web）
    /// </summary>
    /// <param name="oriBytes">原始压缩数据</param>
    /// <param name="callBack">完成解压之后数据回调</param>
    public static async void DeCompress(byte[] oriBytes, Action<byte[]> callBack)
    {
        using (var ms = new MemoryStream(oriBytes))
        {
            using (var deCompressStream = new GZipStream(ms, CompressionMode.Decompress))
            {
                using (var outBuffer = new MemoryStream())
                {
                    var block = new byte[1024 * 1024];
                    while (true)
                    {
                        var bytesRead = await deCompressStream.ReadAsync(block, 0, block.Length);
                        if (bytesRead <= 0) break;
                        await outBuffer.WriteAsync(block, 0, bytesRead);
                    }
                    callBack?.Invoke(outBuffer.ToArray());
                }
            }
        }
    }

    /// <summary>
    /// 解压byte数据Zip形式（与服务器上传到web的相同）
    /// </summary>
    /// <param name="bytes">原始压缩数据</param>
    /// <param name="callBack">完成解压之后数据回调</param>
    public static async void DecompressByZip(byte[] bytes, Action<byte[]> callBack, string strUrlOrLocalPath = "")
    {
        if (bytes.Length <= sizeof(int))
        {
            callBack?.Invoke(null);
            throw new Exception("DecompressByZip Error");
        }

        using(var outMemoryStream = new MemoryStream())
        {
            using (var outZStream = new ZOutputStream(outMemoryStream))
            {
                using (Stream inMemoryStream = new MemoryStream(bytes))
                {
                    inMemoryStream.Position = sizeof(int);
                    var block = new byte[1024 * 1024];
                    while (true)
                    {
                        var bytesRead = await inMemoryStream.ReadAsync(block, 0, block.Length);
                        if (bytesRead <= 0) break;
                        try
                        {
                            outZStream.Write(block, 0, bytesRead);
                        }
                        catch (Exception e)
                        {
                            callBack?.Invoke(null);
                            Debug.LogWarning("DecompressByZip Error, 服务器给的Zip压缩数据不对！Url：" + strUrlOrLocalPath);
                            return;
                        }
                    }
                    outZStream.finish();
                    var outData = new byte[outMemoryStream.Length];
                    outMemoryStream.Position = 0;
                    await outMemoryStream.ReadAsync(outData, 0, (int)outMemoryStream.Length);
                    callBack?.Invoke(outMemoryStream.ToArray());
                }
            }
        }
    }

    /// <summary>
    /// 将文件夹下的所有文件压缩成zip
    /// </summary>
    /// <param name="zipPath">需要压缩成zip文件夹路径</param>
    /// <param name="fileZipPath">需要压缩成zip文件的绝对路径</param>
    /// <param name="filesPath">文件路径</param>
    /// <param name="callBack">创建成功返回true，失败false</param>
    /// <returns></returns>
    public static async void CreateZipFile(string zipPath, string fileZipPath, string filesPath, Action<bool> callBack)
    {
        try
        {
            FileUtils.CreateFileDirectory(zipPath);
            string[] fileNames = Directory.GetFiles(filesPath);
            FileStream fileStream = File.Create(fileZipPath);
            using (ZipOutputStream outputStream = new ZipOutputStream(fileStream))
            {
                outputStream.SetLevel(9);
                byte[] buffer = new byte[1024*200];
                var fileList = fileNames.ToList();
                foreach (string file in fileList)
                {
                    ZipEntry entry = new ZipEntry(Path.GetFileName(file));
                    entry.DateTime = DateTime.Now;
                    outputStream.PutNextEntry(entry);
                    using (FileStream fs = File.OpenRead(file))
                    {
                        while (true)
                        {
                            int readBytes = await fs.ReadAsync(buffer, 0, buffer.Length);
                            if (readBytes <= 0) break;
                            await outputStream.WriteAsync(buffer, 0, readBytes);
                        }
                        fs.Close();
                        fs.Dispose();
                    }
                }
                outputStream.Finish();
                outputStream.Close();
                callBack?.Invoke(true);
            }
        }
        catch (Exception ex)
        {
            callBack?.Invoke(false);
        }
    }

    /// <summary>
    /// 删除过期的战斗日志（本地保存90天）
    /// </summary>
    /// <param name="utcStemp">这个UTC时间之前的日志都删掉</param>
    public static void DeleteOutOfDataUtcBattleLogFile()
    {
        var nowUtcSecond = (System.DateTime.UtcNow.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        var timeInterval = LoadDataByAccount<string>(DsDeleteBattleLogTimeInterval);
        if (string.IsNullOrEmpty(timeInterval))
        {
            SaveDataByAccount(DsDeleteBattleLogTimeInterval, nowUtcSecond.ToString());
            return;
        }

        if (!long.TryParse(timeInterval, out var lastUtcSaveTime))
        {
            return;
        }

        if (nowUtcSecond - lastUtcSaveTime < 48 * 60 * 60)
        {
            return;
        }

        if (!Directory.Exists(DefaultBattleLogPath))
        {
            return;
        }
        var files = Directory.GetFiles(DefaultBattleLogPath, "*.gz");
        foreach (var fileItem in files)
        {
            var fileInfo = new FileInfo(fileItem);
            var now = DateTime.UtcNow;
            var timeSpan = now - fileInfo.CreationTimeUtc;
            if (timeSpan.TotalSeconds > 90 * 24 * 60 * 60)
            {
                File.Delete(fileItem);
            }
        }
        SaveDataByAccount(DsDeleteBattleLogTimeInterval, nowUtcSecond.ToString());
    }
}

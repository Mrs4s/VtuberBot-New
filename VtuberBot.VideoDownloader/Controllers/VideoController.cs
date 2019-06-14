using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using VtuberBot.Core.Entities.Bilibili;
using VtuberBot.Core.Extensions;
using Newtonsoft.Json.Linq;
using StreamDownloader.Core.Network;
using StreamDownloader.Core.Processor;
using VideoRecordService;
using VideoRecordService.Models;
using VideoRecordService.Models.UploadCore;
using VtuberBot.Core;

namespace VtuberBot.VideoDownloader.Controllers
{
    [Route("video")]
    public class VideoController : Controller
    {
        private string BilibiliCookies => System.IO.File.ReadAllText("BCookies.txt");

        [HttpGet("bilibili/{aid}/transform")]
        public IActionResult TransformBilibiliVideo([FromRoute] long aid)
        {
            new Thread(async () =>
            {
                try
                {
                    LogHelper.Info("Got bilibili video av" + aid);
                    var baseVideoInfo = await GetBaseBilibiliVideoInfoAsync(aid);
                    var process = UrlDispatcher.DispatchVideoUrl("https://www.bilibili.com/video/av" + aid);
                    process.Cookies = BilibiliCookies;
                    process.Initialize();
                    LogHelper.Info($"Find {process.Videos.Length} videos, create download task.");
                    var partNum = 1;
                    foreach (var videoInfo in process.Videos)
                    {
                        var streamVideoInfo =
                            (StreamDownloader.Core.Processor.CustomProcessor.Bilibili.BilibiliVideoInfo)videoInfo;
                        var bilibiliVideoInfo = baseVideoInfo.Clone();
                        bilibiliVideoInfo.Cid = streamVideoInfo.Cid;
                        bilibiliVideoInfo.PartNum = partNum++;
                        if ((await Program.VideoCollection.FindAsync(v => v.Cid == streamVideoInfo.Cid)).Any())
                        {
                            LogHelper.Info($"Video {streamVideoInfo.Cid} already exists.");
                            continue;
                        }

                        var downloader = process
                            .Process(videoInfo.VideoStreams.OrderByDescending(v => v.Quality).First(),
                                "Videos/" + aid + "-" + bilibiliVideoInfo.PartNum).First();
                        LogHelper.Info("Start download " + downloader.DownloadPath);
                        downloader.BeginDownload();
                        while (downloader.Status == DownloadStateEnum.Downloading)
                            Thread.Sleep(1000);
                        LogHelper.Info("Download completed " + downloader.DownloadPath);
                        var md5 = FileTools.GetMd5HashFromFile(downloader.DownloadPath);
                        var fileInfo = new FileInfo(downloader.DownloadPath);
                        var url = AcceleriderApi.CreateUploadTask(md5, fileInfo.Length, fileInfo.Name);
                        if (url == null)
                        {
                            bilibiliVideoInfo.FileName = fileInfo.Name;
                            bilibiliVideoInfo.FileHash = md5;
                            Program.VideoCollection.InsertOne(bilibiliVideoInfo);
                            System.IO.File.Delete(downloader.DownloadPath);
                            return;
                        }

                        var info = new OnedriveUploadInfo()
                        {
                            FilePath = downloader.DownloadPath,
                            UploadUrl = url
                        };
                        info.Init();
                        var upload = OnedriveUpload.CreateTaskByInfo(info);
                        upload.UploadStateChangedEvent += (sender, e) =>
                        {
                            if (e.NewState == UploadStatusEnum.Completed)
                            {
                                LogHelper.Info("Upload completed: " + md5);
                                AcceleriderApi.ConfirmUpload(md5);
                                bilibiliVideoInfo.FileName = fileInfo.Name;
                                bilibiliVideoInfo.FileHash = md5;
                                Program.VideoCollection.InsertOne(bilibiliVideoInfo);
                                System.IO.File.Delete(downloader.DownloadPath);
                            }
                        };
                        upload.Start();
                        LogHelper.Info("Start upload" + downloader.DownloadPath);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error("Error to process video " + aid, ex: ex);
                }

            })
            { IsBackground = true }.Start();
            return Ok();
        }




        private async Task<BilibiliVideoInfo> GetBaseBilibiliVideoInfoAsync(long aid)
        {
            using (var client = HttpClientExtensions.CreateClient(useGZip: true, referer: "https://www.bilibili.com/video/av" + aid, cookies: BilibiliCookies, cookiesDomain: ".bilibili.com"))
            {
                var json = JToken.Parse(
                    await client.GetStringAsync("https://api.bilibili.com/x/web-interface/view?aid=" + aid));
                if (json.Value<int>("code") != 0)
                    return null;
                var data = json["data"];
                return new BilibiliVideoInfo()
                {
                    Aid = data["aid"].ToObject<long>(),
                    Title = data["title"].ToString(),
                    Cover = data["pic"].ToString(),
                    CreatedTime = data["ctime"].ToObject<long>(),
                    Description = data["desc"].ToString(),
                    Uploader = data["owner"]["mid"].ToObject<long>()
                };
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using HtmlAgilityPack;

namespace VtuberBot.Spider.Services.Twitcasting
{
    public class TwitcastingApi
    {
        public static TwitcastingChannel GetChannel(string channelId)
        {
            var web = new HtmlWeb();
            var doc = web.Load("https://twitcasting.tv/" + channelId);
            var onlineTag = doc.DocumentNode.SelectSingleNode("//*[@id=\"mainwrapper\"]/div[2]/div/div[2]/h2/span[1]");
            return new TwitcastingChannel()
            {
                Username = doc.DocumentNode.SelectSingleNode("//*[@id=\"mainwrapper\"]/div[1]/div/h2/span[1]")
                    .InnerText,
                ChannelId = channelId,
                OnLive = onlineTag.Attributes.Count == 1,
                LiveTitle = doc.DocumentNode.SelectSingleNode("//*[@id=\"movietitle\"]/a")?.InnerText
            };
        }
    }
}

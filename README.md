# 简介
VtuberBot重构版本, 可自行部署。
由于TX封号严重，我决定将重构版本开源并接受第三方BOT的推送服务
如果不会部署或条件不允许，可在公网服务器设置好酷Q的HTTP API插件将接口信息发送至mrs4sxiaoshi@gmail.com，我会将其接入公共数据库(260+Vtuber)。

# 特性
- 原生支持多Bot同时推送/负载均衡
- 支持多服务切换(酷Q/Light QQ) 推荐使用酷Q的HTTP API插件接入本bot
- 爬虫自动留档数据，以便统计(所有Vtuber的Youtube/Bilibili评论数据 / 直播记录 / 推特记录等)
- 支持简单的插件系统(只是我用来热更新代码用的)

# 项目说明
- VtuberBot -与bot对接的服务
- VtuberBot.Core -公共代码库
- VtuberBot.Spider -爬虫服务
- VtuberBot.ReplaySpider -专门分离出来的回放评论爬取服务

# 简要部署说明
> 只是简单说说如何部署，具体操作以后再写文档

> 推荐使用两台以上的服务器部署

- 0.申请Youtube Data Api访问权限以及Twitter Dev访问权限以供爬虫使用
- 1.Clone本项目并编译
- 2.安装MongoDB并创建库vtuber-bot-data
- 3.启动VtuberBot项目和VtuberBot.Spider项目并设置好配置即可

VtuberBot的配置文件说明如下
```
{
  "DatabaseUrl": 数据库连接字串,
  "CallbackSign": 爬虫Callback签名，需要与爬虫配置相同,
  "ListenUrl": Webapi以及CallbackApi监听地址，默认http://0.0.0.0:80,
  "GroupConfigs": 群订阅配置，无需修改,
  "Clients":[ 服务客户端，支持多服务共存
    {
      "ClientId": 客户端ID，唯一标识,
      "Services":[ 服务列表，原计划支持多服务共存，但还未开发完成
        {
          "Level": 优先级，尚未开发完成,
          "ServiceType": 服务类型，支持CoolQ/LightQQ(酷Q和LightQQ),
          "ListenUrl": Api回调地址，命名错误请无视,
          "WsUrl": Websocket事件上报地址,
          "ListenPort": 保留参数，无视,
          "AccessToken": token
        }
      ]
    }
  ]
}
```

VtuberBot.Spider的配置文件说明如下
```
{
  "DatabaseUrl": 连接字串，需要和Bot服务连接同一个库,
  "Callbacks": [ 爬虫回调，支持一个爬虫回调多个子服务
    "回调地址":"回调签名"
  ],
  "YoutubeAccessToken": Youtube Data api的token,
  "TwitterAccessToken": Twitter Dev的token
}
```

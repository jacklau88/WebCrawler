# Web Crawler
这是一个轻量级、快速、多线程、多管道、灵活配置的网络爬虫。
### 架构设计
WebCrawler 采用的是一个多管道、多调度器的设计与处理模型，任何事情通过管道处理，默认提供了一些常用的管道，开发者可自由扩展管道，组装成一个强大的爬虫。

### 多管道
在一个爬虫里，通常会多个动作。  
比如爬取某网站的文章数据，通常会有一下这几个操作：
- 从无数 url 中，分析确定需要爬取的文章url；
- 分析文章页面数据，提取需要的信息；
- 持久化数据，保存到数据库或者导出到Excel中。  

为了更方便维护，代码结构更简单，我们可以为每一个操作编写独立管道（*每个管道职责尽可能单一并且耦合性极低*），多个管道协同工作，最终完成一个页面的抓取工作。在实际编写爬虫中，开发者只需专注于编写业务逻辑，其余的事情框架内部已经帮你处理好了。  
在 WebCrawler 里 Pipeline 有两种运行方式：  

**管道链模式：**  
![chain mode](chain.png)  

链条模式类似于“搭积木”，将多个管道拼接组装在一起，管道连着管道，形成一个闭合的处理管道链。我们推荐在编写具有连续性任务爬虫的时候，采用此模式。

**并行模式：**  
![chain mode](parallel.png)  

并行模式，顾名思义，也就是说 N 个管道同时运行，没有了链条关系，它们通过调度器协同工作。

### 示例
请参阅 Crawler.Simple 项目，从简单到复杂都有很好的示例。

﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Logger;
using Crawler.Pipelines;
using Crawler.Scheduler;

namespace Crawler
{
    public class Crawler : ICrawler
    {
        private readonly IScheduler _scheduler;
        private readonly IEnumerable<Site> _sites;
        private DateTime _beginTime;
        private DateTime _endTime;
        private IPipeline _pipelin;
        private int _threadNum;

        public Crawler()
        {
            _scheduler = new SiteScheduler();
            Logger = new SimpleLogger();
        }

        public Crawler(string name, IEnumerable<Site> sites, IPipeline pipeline) : this()
        {
            Name = name;
            _sites = sites ?? throw new ArgumentNullException(nameof(sites));
            _pipelin = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        }

        public Crawler(IEnumerable<Site> sites, IPipeline pipeline) : this(Guid.NewGuid().ToString("N"), sites,
            pipeline)
        {
        }

        public string Name { get; set; }

        public int ThreadNum
        {
            get => _threadNum;
            set
            {
                if (CheckState(CrawlerState.Running))
                    throw new InvalidOperationException("爬虫正在运行。");

                if (value < 0)
                    throw new ArgumentException("爬虫线程数量不能小于0。");
                _threadNum = value;
            }
        }

        public CrawlerState CrawlerState { get; protected set; }

        public ILogger Logger { get; }

        public IPipeline Pipeline
        {
            get => _pipelin;
            set
            {
                if (CheckState(CrawlerState.Running))
                    throw new InvalidOperationException("爬虫正在运行。");
                _pipelin = value;
            }
        }

        public void Pause()
        {
            if (CrawlerState == CrawlerState.Running)
                CrawlerState = CrawlerState.Stopped;
        }

        public void Continue()
        {
            if (CrawlerState == CrawlerState.Stopped)
                CrawlerState = CrawlerState.Running;
        }

        public void Exit()
        {
            CrawlerState = CrawlerState.Exited;
        }

        public void Run()
        {
            if (CrawlerState == CrawlerState.Running)
                return;

            foreach (var site in _sites)
                _scheduler.Push(site);

            CrawlerState = CrawlerState.Running;
            _beginTime = DateTime.Now;

            while (CrawlerState == CrawlerState.Running || CrawlerState == CrawlerState.Stopped)
            {
                if (CrawlerState == CrawlerState.Stopped)
                {
                    Thread.Sleep(500);
                    continue;
                }

                Parallel.For(0, ThreadNum, new ParallelOptions
                {
                    MaxDegreeOfParallelism = ThreadNum
                }, i =>
                {
                    while (CrawlerState == CrawlerState.Running)
                    {
                        Page page = null;

                        if (_scheduler is SiteScheduler)
                        {
                            var site = (Site) _scheduler.Pop();
                            if (site == null)
                            {
                                CrawlerState = CrawlerState.Finished;
                                break;
                            }

                            page = new Downloader().GetPage(site);
                        }


                        var context = new PipelineContext
                        {
                            Crawler = this,
                            Page = page,
                            Configuration = new CrawlerConfiguration
                            {
                                Pipeline = Pipeline,
                                Sites = _sites,
                                ThreadNum = _threadNum
                            }
                        };

                        Pipeline.ExecuteAsync(context).GetAwaiter().GetResult();
                        Logger.Write("1", null, LogLevel.Debug);
                        Thread.Sleep(1000);
                    }
                });
            }

            _endTime = DateTime.Now;
            Logger.Write("总耗时（s）：" + (_endTime - _beginTime).TotalSeconds, null, LogLevel.Info);
        }

        public Task RunAsync()
        {
            return Task.Factory.StartNew(Run);
        }

        private bool CheckState(CrawlerState state)
        {
            return CrawlerState == state;
        }
    }
}
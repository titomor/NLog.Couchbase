using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;

namespace NLog.Couchbase.Test
{
    [TestClass]
    public class CouchbaseTargetTest
    {
        [ExpectedException(typeof(NLogConfigurationException))]
        [TestMethod]        
        public void TestInitializationInvalidBucketName()
        {
            var couchbaseTarget = new CouchbaseTargetMock();

            couchbaseTarget.PublicInitializeTarget();
        }

        [ExpectedException(typeof(NLogConfigurationException))]
        [TestMethod]
        public void TestInitializationInvalidServers()
        {
            var couchbaseTarget = new CouchbaseTargetMock();
            couchbaseTarget.Bucket = "testBucket";
            couchbaseTarget.PublicInitializeTarget();
        }

        [ExpectedException(typeof(NLogConfigurationException))]
        [TestMethod]
        public void TestInitializationInvalidServers2()
        {
            var couchbaseTarget = new CouchbaseTargetMock();
            couchbaseTarget.Bucket = "testBucket";
            couchbaseTarget.Servers = new List<Server>{};
            couchbaseTarget.PublicInitializeTarget();
        }


        [ExpectedException(typeof(NLogConfigurationException))]
        [TestMethod]
        public void TestInitializationInvalidServerURI()
        {
            var couchbaseTarget = new CouchbaseTargetMock();
            couchbaseTarget.Bucket = "testBucket";
            couchbaseTarget.Servers = new List<Server> { new Server() };
            couchbaseTarget.PublicInitializeTarget();
        }

    
        [TestMethod]
        public void TestInitializationValidProperties()
        {
            var couchbaseTarget = new CouchbaseTargetMock();
            couchbaseTarget.Bucket = "testBucket";
            couchbaseTarget.Servers = new List<Server> 
            {
                new Server()
                {
                    Uri = "http://localhost"
                }
            };
            couchbaseTarget.PublicInitializeTarget();
        }


        

        //[TestMethod]
        //public void TestInitializationKeyRenderers()
        //{
        //    var logger = LogManager.GetCurrentClassLogger();

        //    logger.Log<MyLog>(LogLevel.Info, new MyLog
        //    {
        //        Date = DateTime.Now,
        //        CorrelationUID = Guid.NewGuid().ToString(),
        //        MethodName = "UnitTest",
        //        OutputParams = new 
        //        {
        //            EchoToken = "e8d5fe78-9999-4c45-bef9-8524ea8912a0",
        //            TimeStamp = new DateTime(2015,04,24),
        //            Target = 0,
        //            Version = "1.0",
        //            Success = true,
        //            PropertiesType = "Properties"                
        //        },
        //        InputParams = new 
        //        { 
        //            Authorization = "1af538baa9045a84c0e889f672baf83ff24",
        //            Token = "e8d5fe78-9999-4c45-bef9-8524ea8912a0"
        //        }
        //    });

            
        //}

    }

    //public class MyLog
    //{
    //    public DateTime Date { get; set; }
    //    public string MethodName { get; set; }
    //    public object InputParams { get; set; }
    //    public object OutputParams { get;set;}
    //    public string CorrelationUID { get; set; }
    //}
}

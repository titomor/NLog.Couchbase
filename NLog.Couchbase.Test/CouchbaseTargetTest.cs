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

    }
}

# NLog Couchbase Target

This project implements a NLog target that supports Couchbase NoSql.

Nuget package "NLog.Couchbase".

Make sure you have a reference to Couchbase.dll and Enyim.Caching.dll version 1.3 (at least) in your project otherwise it doesn't work!
These assemblies can be found in the Lib subfolder of this project or you can download the Couchbase .NET Client library from http://packages.couchbase.com.s3.amazonaws.com/clients/net/1.3/Couchbase-Net-Client-1.3.10.zip and extract the necessary assemblies.

Usage:

In your app.config or web.config in the nlog configuration section add the <extensions> element that points to the NLog.Couchbase assembly.

Make sure you have a bucket (aka database name) created in couchbase with a password set up. 

Specify in the configuration source attribute what's the NLog source for the document (<b><i>Parameters, Properties, All, None = Layout</i></b>) to store in the Couchbase bucket. If Parameters is used for the DocumentSource, the LogEventInfo.Parameters is used to create a JSON object that is stored in Couchbase. If Properties is used instead, the LogEventInfo.Properties dictionary is used. If All is used then , the entire LogEventInfo object is converted to Json and stored in the couchbase bucket.

So you have:




     Parameters    -> LogEventInfo.Parameters
     Properties    -> LogEventInfo.Properties
     All           -> LogEventInfo
     None          -> LogEventInfo.FormattedMessage

You can use the following NLog method if you intend to log your messages as JSON objects (MyBucketLog class):

```C#
     logger.Log<MyBucketLog>(LogLevel.Info, myBucketLogInstance);
```

Specify the document format that will be used to serialize the data into Couchbase bucket, e.g., if it should be stored as JSON or Default (text).

Example:


     
    <nlog>
        <extensions>
          <add assembly="NLog.Couchbase" />
        </extensions>
        <targets>
          <target name="test" xsi:type="Couchbase" bucket="myBucket" bucketPassword="password" documentSource="Parameters"  documentFormat="JSON">
            <server uri="http://192.168.56.101:8091/pools" />
            <server uri="http://192.168.56.102:8091/pools" />      
          </target>
        </targets>
        <rules>
          <logger name="*" minLevel="Debug" appendTo="couchbase" />
        </rules>
      </nlog>

# NLog Couchbase Target

This project implements a NLog target that supports Couchbase NoSql.

Nuget package coming soon!

Usage:
Download the source code and build.
In your app.config or web.config in the nlog configuration section add the <extensions> element that points to the NLog.Couchbase assembly.
Make sure you have a bucket (aka database name) created in couchbase with a password set. 
Specify in the Source attribute what's the source of the information to store in the Couuchbase bucket.
Specify the format of  the source data, e.g., if it should be stored as JSON or Default (text).

Example:

&lt;nlog&gt;
    &lt;extensions&gt;
      &lt;add assembly="NLog.Couchbase" /&gt;
    &lt;/extensions&gt;
    &lt;targets&gt;
      &lt;target name="test" xsi:type="Couchbase" bucket="system_logging" bucketPassword="vagrant" source="Parameters" format="JSON"&gt;
        &lt;server uri="http://192.168.56.101:8091/pools" /&gt;
        &lt;server uri="http://192.168.56.102:8091/pools" /&gt;  
      &lt;/target&gt;
    &lt;/targets&gt;
    &lt;rules&gt;
      &lt;logger name="*" minLevel="Debug" appendTo="couchbase" /&gt;
    &lt;/rules&gt;
&lt;/nlog&gt;

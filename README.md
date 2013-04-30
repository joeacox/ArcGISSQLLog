ArcGISSQLLog
============
This code is an expansion on a great article written by Trevor Hart.
his blog is:
https://gdbgeek.wordpress.com/ 

and the article I am working from is:
http://gdbgeek.wordpress.com/2013/01/16/exploring-arcgis-server-10-1-logs-part-1/

Original Source Code Download:
http://www.arcgis.com/home/item.html?id=dc2f18833a6a48139a19aad11a5a93af

Added Features:

Added the ability to detect the last log entry time in the database so that only new records records 
are queried. This was added so that server logs do not have to be purged.

Automatic detection of Spatial Reference for each individual Web Service. Supported have services with varying
spatial references

Command Line Parameters:
-logsurl = URL to the REST logs API eg http://localhost:6080/arcgis/admin/logs 
-filter = Query filter eg "{'server' : '*','services' : '*','machines' : '*'}" (must be quote encased, if you require inner quotes use single quotes) 
-user = User name of a user that can query the logs eg agsadmin 
-password = Password of user that can query the logs eg spat1al 
-tokenurl = URL of REST toen service eg http://localhost:6080/arcgis/tokens 
-cleanlogs = Set to "Y" to clear out logs eg Y 
-debug = Set debug mode on or off eg N 
-dbpassword = Database password eg adm1n 
-dbuser = Database user name eg sdeadmin 
-dbname = Database name eg Performance 
-dbserver = Database server name eg mercator.gis.local 
-srid = Spatial Reference ID for features eg 2193

Added:
-servicesurl = URL to the rest service page eg http://localhost:6080/arcgis/rest/services
-requesterpublicip = public IP address for token generation
-dbschema = owner schema for the RawLogs Database table

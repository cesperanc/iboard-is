Requirements:
-> Web server with PHP support
-> PHP version 5.2+
-> Moodle 1.9.10+ and its requisites
-> php_soap extension enabled

Instructions:
To install this extension, the folders admin, auth and their contents should be copied to the root of a Moodle installation.
To the system recognize and install the plugins, an user with administration privileges should click on the "Notifications" option under the "Administration" block and follow the installation instructions.
The installation will be completed when the authentication extension "UEDWS Authentication" is enabled on the "Manage Authentication" section on the "Administration->Users->Authentication->Manage Authentication" option.
The webservice should be available on the URL http://[moodle_root]/admin/report/uedws/ with the service descriptor at http://[moodle_root]/admin/report/uedws/?wsdl, were [moodle_root] is web address of the Moodle instance.